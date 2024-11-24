using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;

//[ExecuteInEditMode()]
public class SplineExtrude : MonoBehaviour
{
    MeshFilter meshFilter;

    [SerializeField] List<GameObject> points = new List<GameObject>();
    [SerializeField] int splineSubdivisions = 10;
    [SerializeField] int aroundSubdivisions = 5;
    [SerializeField] float width = 1;

    List<Vector3> positions = new List<Vector3>();
    int prevSub = 10;
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
        width = math.max(0, width);
        if (splineSubdivisions != prevSub || width != prevWidth)
        {
            prevSub = splineSubdivisions; prevWidth = width;

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

    void Evaluate(float time, out Vector3 position, out Vector3 forward, out Vector3 up)
    {
        //this is the next thing to finish
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
        List<Vector3> normals = new List<Vector3>();
        List<int> indices = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        for (float j = 0; j < splineSubdivisions; j++)
        {
            Vector3 pos1, forward1, up1;
            Vector3 pos2, forward2, up2;
            float time1 = j / splineSubdivisions;
            float time2 = (j + 1) / splineSubdivisions;

            Evaluate(time1, out pos1, out forward1, out up1);
            Evaluate(time2, out pos2, out forward2, out up2);

            Debug.DrawLine(pos1, pos2, Color.red, Time.deltaTime);
            
            Vector3 right1 = Vector3.Cross(forward1, up1).normalized;
            right1 = Quaternion.AngleAxis(0, forward1) * right1;
            Vector3 normal1 = Quaternion.AngleAxis(90, forward1) * right1;

            Vector3 right2 = Vector3.Cross(forward2, up2).normalized;
            right2 = Quaternion.AngleAxis(0, forward2) * right2;
            Vector3 normal2 = Quaternion.AngleAxis(90, forward2) * right2;

            Vector3 p1 = pos1 + right1 * width;
            Vector3 p2 = pos1 - right1 * width;
            Vector3 p3 = pos2 + right2 * width;
            Vector3 p4 = pos2 - right2 * width;

            verts.Add(p1);
            verts.Add(p2);
            verts.Add(p3);
            verts.Add(p4);

            normals.Add(normal1);
            normals.Add(normal1);
            normals.Add(normal2);
            normals.Add(normal2);

            uvs.Add(new Vector2(time1, 1));
            uvs.Add(new Vector2(time1, 0));
            uvs.Add(new Vector2(time2, 1));
            uvs.Add(new Vector2(time2, 0));

            indices.Add((int)j * 4 + 2);
            indices.Add((int)j * 4 + 1);
            indices.Add((int)j * 4);

            indices.Add((int)j * 4 + 1);
            indices.Add((int)j * 4 + 2);
            indices.Add((int)j * 4 + 3);
        }
        
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        meshFilter.mesh = mesh;
    }
}

