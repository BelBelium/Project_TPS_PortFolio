using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAimingController : MonoBehaviour
{
    public Transform debugTransform;
    
    private Camera _camera;
    
    [SerializeField] private LayerMask collisionMask;
    
    void Start()
    {
        _camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);

        if (_camera)
        {
            Ray ray = _camera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 999f,collisionMask))
            {
                debugTransform.position = hitInfo.point;
            }
        }
    }
}
