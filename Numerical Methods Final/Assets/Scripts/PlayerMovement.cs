using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float mouseSens = 40f;
    [SerializeField] float sprintSpeed = 4f;

    float xRotation = 0f;
    float yRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        //WASD Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 direction = transform.forward * vertical + transform.right * horizontal;
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        //Camera Rotation        
        if (Input.GetMouseButtonDown(1) && Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            xRotation -= mouseY * Time.deltaTime * mouseSens;
            yRotation += mouseX * Time.deltaTime * mouseSens;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.localEulerAngles = new Vector3(xRotation, yRotation, 0f);
        }

        if (Input.GetMouseButtonUp(1))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            speed *= sprintSpeed;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            speed /= sprintSpeed;
        }

        if(Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E))
        {
            transform.position += new Vector3(0f, transform.up.y * speed * Time.deltaTime, 0f);
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q))
        {
            transform.position += new Vector3(0f, -transform.up.y * speed * Time.deltaTime, 0f);
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
}
