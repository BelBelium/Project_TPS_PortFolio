using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Serialization;
using Vector3 = UnityEngine.Vector3;

[RequireComponent(typeof(CharacterController))]
public class PlayerController_BackUp : MonoBehaviour
{
    class PlayerCharacterControllerProcess
    {
        private readonly CharacterController _characterController;
        private const float Gravity = -6.81f;
        
        private float _jumpPower;
        private float _moveSpeed;
        private float _prevSpeed;
        private float _sprintSpeed = 6.0f;

        private bool _isJumping = false;
        private bool _isSecondJumping = false;
        private bool _isSliding = false;
        private bool _isSprint = false;
        
        private Vector3 _moveDir;
        PlayerCharacterControllerProcess(CharacterController myChar, float moveSpeed, float jumpPower)
        {
            _moveSpeed = moveSpeed;
            _prevSpeed = moveSpeed;
            _jumpPower = jumpPower;
            _characterController = myChar;
            myChar.skinWidth = myChar.radius * 0.1f;                                //CharacterController의 skinWidth를 최적화 값인 radius의 10%로 설정.
        }
        public static PlayerCharacterControllerProcess MovementSetUp(CharacterController myChar,float baseMoveSpeed,float baseJumpPower)     //생성자를 다른곳에서 쉽게 만들지 못하게 함.
        {
            return new PlayerCharacterControllerProcess(myChar,baseMoveSpeed,baseJumpPower);
        }

        public void InputKeyMove()
        {
            _moveDir = _isJumping
                ? new Vector3(Input.GetAxis("Horizontal"), _moveDir.y, Input.GetAxis("Vertical"))
                : new Vector3(Input.GetAxisRaw("Horizontal"), _moveDir.y, Input.GetAxisRaw("Vertical"));
        }
        
        public void PlayerMove()                     //플레이어의 이동의 모든 기능 수행.
        {
            if (_characterController.isGrounded)
            {
                _isJumping = false;
                _isSecondJumping = false;
                _moveDir.y = -1.0f;
                
                if (Input.GetKeyDown(KeyCode.LeftShift) && !_isSprint)
                {
                    _moveSpeed = _sprintSpeed;
                    _isSprint = true;
                }
                
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    Jumping(_jumpPower);
                }

                if (Input.GetKeyDown(KeyCode.LeftControl) && _moveSpeed > 6.0f)
                {
                    Sliding();
                }
                
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    Crouch();
                }

                if (Input.GetKeyUp(KeyCode.LeftControl))
                {
                    CrouchUp();
                }
                
            }

            if (_moveDir is { x: 0, z: 0 })
            {
                _moveSpeed = _prevSpeed;
                _isSprint = false;
            }

            if (Input.GetKeyDown(KeyCode.Space) && !_isSecondJumping && !_characterController.isGrounded)
            {
                Jumping(_jumpPower);
                _isSecondJumping = true;
            }
            
            
            _moveDir.y += Gravity * Time.deltaTime;


            _characterController.Move(_moveSpeed * Time.deltaTime * _moveDir);

        }
        
        
        private void Jumping(float jumpPower)
        {
            _isJumping = true;
            _moveDir.y = _jumpPower;
        }

        private void Sliding()
        {
            
        }
        
        private void Crouch()
        {
            _characterController.height = 0.7f;
            _characterController.center = new Vector3(0.0f, 0.35f, 0.0f);
            _moveSpeed = 2.0f;

        }
        
        private void CrouchUp()
        {
            _characterController.height *= 2.0f;
            _characterController.center = new Vector3(0.0f, 0.7f, 0.0f);
            _moveSpeed = 3.0f;
        }
        
    }
    private PlayerCharacterControllerProcess _playerCharacterControllerProcess;

    public float moveSpeed;
    public float jumpPower;
    public float moveSprintSpeed;
    public float rotSpeed;
    
    private CameraController _cameraController;
    private AnimationController _animationController;
    
    [SerializeField] private GameObject charBodyParent;

    
    void Awake()
    {
        _playerCharacterControllerProcess = PlayerCharacterControllerProcess.MovementSetUp(gameObject?.GetComponent<CharacterController>(),moveSpeed,jumpPower);
        
        
        _animationController = GetComponent<AnimationController>();
        if (!_animationController)
        {
            Debug.Log("Not Found AnimationController");
        }
    } 
    
    void Update()
    {
        
        //Vector3 forwardDir = _cameraController.camArm.TransformDirection(movedir);
        //forwardDir.y = 0.0f; 
        
        _playerCharacterControllerProcess.InputKeyMove();
        _playerCharacterControllerProcess.PlayerMove();

        
    }

    
}
