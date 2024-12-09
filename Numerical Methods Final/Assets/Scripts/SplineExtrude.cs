using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System;

[ExecuteInEditMode()]
public class SplineExtrude : MonoBehaviour
{
    public enum RingFunction
    {
        Circle,
        Rose,
        Cardioid,
        Star
    }
    public enum SplineFunction
    {
        Flat,
        Sin2,
        Quadratic,
        Zigzag
    }

    MeshFilter meshFilter;

    [SerializeField] RingFunction currentRingFunction = RingFunction.Circle;
    [SerializeField] SplineFunction currentSplineFunction = SplineFunction.Flat;
    [SerializeField] List<GameObject> points = new List<GameObject>();
    [SerializeField] int splineSubdivisions = 10;
    [SerializeField] int aroundSubdivisions = 5;
    [SerializeField] float width = 1;

    RingFunction prevRingFunction = RingFunction.Circle;
    SplineFunction prevSplineFunction = SplineFunction.Flat;
    List<Vector3> positions = new List<Vector3>();
    int prevSub = 10, prevAround = 5;
    float prevWidth = 1f;

    void Awake()
    {
        foreach (GameObject point in points)
            positions.Add(point.transform.position);

        meshFilter = GetComponent<MeshFilter>();
        GenerateMesh();
    }

    private void Update()
    {
        splineSubdivisions = math.max(10, splineSubdivisions);
        aroundSubdivisions = math.max(2, aroundSubdivisions);
        width = math.max(0, width);
        if (currentRingFunction != prevRingFunction || currentSplineFunction != prevSplineFunction || splineSubdivisions != prevSub || aroundSubdivisions != prevAround || width != prevWidth)
        {
            prevRingFunction = currentRingFunction; prevSub = splineSubdivisions; prevAround = aroundSubdivisions ; prevWidth = width;
            prevSplineFunction = currentSplineFunction;

            positions = new List<Vector3>();
            foreach (GameObject point in points)
                positions.Add(point.transform.position);

            GenerateMesh();
        }
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].transform.position != positions[i])
                {
                    prevSub = splineSubdivisions; prevWidth = width;

                    positions = new List<Vector3>();
                    foreach (GameObject point in points)
                        positions.Add(point.transform.position);

                    GenerateMesh();
                    break;
                }
            }
        }
    }

    static Vector3 Interpolate(Vector3 a, Vector3 b, float t)
    {
        return (1 - t) * a + t * b;
    }

    float ApplyRingFunction(float theta)
    {
        float thetaRadians = theta * Mathf.Deg2Rad;
        if (currentRingFunction == RingFunction.Circle)
            return 1f;
        else if (currentRingFunction == RingFunction.Rose)
            return Mathf.Sin(2 * thetaRadians);
        else if (currentRingFunction == RingFunction.Cardioid)
            return 1f + Mathf.Sin(thetaRadians);
        else if (currentRingFunction == RingFunction.Star)
            return 2f - Mathf.Abs(Mathf.Sin(2 * thetaRadians));
        return 1f;
    }
    float ApplySplineFunction(float t)
    {
        switch (currentSplineFunction)
        {
            case SplineFunction.Flat:
                return 1f;
            case SplineFunction.Sin2:
                return Mathf.Pow(Mathf.Sin(8*t), 2f);
            case SplineFunction.Quadratic:
                return -4f * Mathf.Pow(t - 0.5f, 2) + 1;
            case SplineFunction.Zigzag:
                return Mathf.Abs((t % .5f)*4f-1f);
            default:
                return 1f;
        }
    }

    void Evaluate(float time, out Vector3 position, out Vector3 forward, out Vector3 up)
    {
        List<Vector3> iter = new List<Vector3>();
        List<Vector3> iter2 = new List<Vector3>();
        foreach (GameObject obj in points)
            iter.Add(obj.transform.position);
        
        while (true)
        {

            if (iter.Count == 1)
                break;
            List<Vector3> test = new List<Vector3>();
            List<Vector3> test2 = new List<Vector3>();
            for(int i = 0; i < iter.Count - 1; i++)
            {
                test.Add(Interpolate(iter[i], iter[i+1], time));
                test2.Add(Interpolate(iter[i], iter[i+1], time + 0.01f));
            }
            iter = test;
            iter2 = test2;
        }
        position = iter[0];
        forward = (iter2[0] - iter[0]).normalized;
        if(forward != Vector3.up)
            up = Vector3.Cross(Vector3.Cross(Vector3.up, forward), forward) * -1;
        else
        {
            iter = new List<Vector3>();
            iter2 = new List<Vector3>();
            while (true)
            {
                if (iter.Count == 1)
                    break;
                List<Vector3> test = new List<Vector3>();
                List<Vector3> test2 = new List<Vector3>();
                for (int i = 0; i < iter.Count - 1; i++)
                {
                    test.Add(Interpolate(iter[i], iter[i + 1], time - 0.01f));
                    test2.Add(Interpolate(iter[i], iter[i + 1], time));
                }
                iter = test;
                iter2 = test2;
            }
            up = Vector3.Cross(Vector3.Cross(Vector3.up, (iter2[0] - iter[0]).normalized), (iter2[0] - iter[0]).normalized) * -1;
        }
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> verts = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float inStep = 360f / aroundSubdivisions;

        for (float i = 0; i <= splineSubdivisions; i++)
        {
            Vector3 pos, forward, up;
            float time = i / splineSubdivisions;

            Evaluate(time, out pos, out forward, out up);
            
            Vector3 right = Vector3.Cross(forward, up).normalized;

            for(float j = 0; j <= aroundSubdivisions; j++)
            {
                float theta = j * inStep;
                Vector3 ringPos = Quaternion.AngleAxis(theta, forward) * right;
                verts.Add(pos + ringPos * ApplyRingFunction(theta) * ApplySplineFunction(time) * width);
                uvs.Add(new Vector2(j / aroundSubdivisions, i / splineSubdivisions));
            }
        }

        int columns = aroundSubdivisions + 1;

        for (int i = 0; i < splineSubdivisions; i++)
        {
            for (int j = 0; j < aroundSubdivisions; j++)
            {
                int start = i * columns + j;
                
                indices.Add(start);
                indices.Add(start + 1);
                indices.Add(start + columns + 1);

                indices.Add(start + columns);
                indices.Add(start);
                indices.Add(start + columns + 1);
            }
        }


        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);

        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }
}

