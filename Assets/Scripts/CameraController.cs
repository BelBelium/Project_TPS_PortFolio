using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    public Transform camArm;

    public CinemachineVirtualCamera _Virtual;
    public Cinemachine3rdPersonFollow _distance;

    void Awake()
    {
        _distance = _Virtual.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
    }
    
    void LateUpdate()
    {
        LookAround();
    }


    
    void LookAround()
    {
        Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        
        Vector3 camAngle = camArm.transform.rotation.eulerAngles;

        float clampX = camAngle.x - mouseDelta.y;

        if (clampX < 90.0f)
        {
            clampX = Mathf.Clamp(clampX, -1.0f, 60.0f);
        }
        else
        {
            clampX = Mathf.Clamp(clampX, 320.0f, 361.0f);
        }
        //clampX = Mathf.Clamp(clampX, -60.0f, 40.0f);
        
        camArm.transform.rotation = Quaternion.Euler(clampX, camAngle.y + mouseDelta.x, camAngle.z);
        
    }

}
