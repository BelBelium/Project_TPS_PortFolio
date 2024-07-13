using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


public static class PlayerAnimHashingTable
{
    public static readonly int MoveSpeedParam = Animator.StringToHash("MoveSpeedParam");
    public static readonly int Sliding = Animator.StringToHash("Sliding");
    public static readonly int JumpStart = Animator.StringToHash("RifleJumpStart");
    public static readonly int JumpLoop = Animator.StringToHash("RifleJumpLoop");
    public static readonly int JumpEnd = Animator.StringToHash("RifleJumpDown");
    public static readonly int AimingMode = Animator.StringToHash("AimingMode");
    public static readonly int InputX = Animator.StringToHash("InputX");
    public static readonly int InputZ = Animator.StringToHash("InputZ");
}

public enum PlayerState
{
    Idle,
    Walk,
    Run,
    Jump,
    Crouch,
    Sliding,
    Aiming,
    End
}

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public Animator animator;
    public GameObject aimCamera;
    public CameraController cameraController;
    public Transform charBody;
    public CharacterController character;
    public MultiAimConstraint bodyAim;
    public MultiAimConstraint aim;
    public PlayerAimingController playerAimingController;
    
    private readonly Dictionary<PlayerState, PlayerStateController> _playerStateGroup = new Dictionary<PlayerState, PlayerStateController>();
    private PlayerStateController _curPlayerState;
    private Camera _camera;
    
    

    void Awake()
    {

        character = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerAimingController = GetComponent<PlayerAimingController>();
        
        //CharacterController skinWidth 최적화.
        character.skinWidth = character.radius * 0.1f;

        StateShareData data = new StateShareData
        {
            MyController = GetComponent<PlayerController>()
        };
        
        //플레이어 상태 클래스들 초기화.
        for (var i = 0; i < (int)PlayerState.End; i++)
        {
            _playerStateGroup.Add((PlayerState)i, PlayerStateController.InitState((PlayerState)i));
            _playerStateGroup[(PlayerState)i].ShareData = data;
            
        }
        
        _curPlayerState = _playerStateGroup[PlayerState.Idle];
    }

    void Update()
    {
        
        //캐릭터의 현재 상태 실행.
        _curPlayerState.StatePlay();
        
        //캐릭터의 상태에 따른 speed값 영향을 받음.
        character.Move(_curPlayerState.ShareData.MoveSpeed * Time.deltaTime * _curPlayerState.ShareData.MovePos.normalized +
                        PlayerStateController.GravityAcc * Time.deltaTime * _curPlayerState.ShareData.GravityDir);
        animator.SetFloat(PlayerAnimHashingTable.MoveSpeedParam,_curPlayerState.ShareData.MoveSpeed);
        //Debug.Log(_curPlayerState);
    }
    
    
    //state : 현재 상태 / nextState : 다음 상태 / airCondition : 점프에 관여하는 함수가 아니라면 default = false를 가진다.
    public void ChangeState(PlayerState nextState, bool airCondition = false)
    {
        _curPlayerState = _playerStateGroup[nextState];
        
        //점프가 아닌 상황에 공중에 뜨게 된다면 점프 준비동작 애니메이션 없이 점프루프로 이동.
        if (airCondition)
        {
            _curPlayerState.ShareData.IsAirCondition = true;
        }
    }
    //매개변수 : 플레이어가 점프로 공중상태가 되었는지, 그냥 위에서 떨어지다가 공중상태가 되었는지 체크.
    public IEnumerator JumpLoopCoroutine(bool isJumpingCheck)
    {
        bool isDoubleJumping = false;
        
        //달리기 선입력 
        bool enterJump = false;
        
        //플레이어가 점프를 해서 코루틴에 들어왔다면...
        if (isJumpingCheck)
        {
            _curPlayerState.ShareData.GravityDir.y = 3.0f;
            animator.CrossFade(PlayerAnimHashingTable.JumpStart,0.0f);

        }
        //플레이어가 점프가 아닌 떨어지다 코루틴에 들어왔다면...
        else
        {
            animator.CrossFade(PlayerAnimHashingTable.JumpLoop,0.5f);
        }
        
        //더블 점프 실행
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
                enterJump = true;
            
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("RifleJumpStart"))
            {
                float animTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;

                if (animTime >= 0.9f)
                {
                    animator.CrossFade(PlayerAnimHashingTable.JumpLoop,0.5f);
                }
            }
            if (Input.GetKeyDown(KeyCode.Space) && !isDoubleJumping)
            {
                _curPlayerState.ShareData.GravityDir.y = 3.0f;
                isDoubleJumping = true;
            }
            
            //isGrounded의 반환값을 한프레임 늦춰서 받아오기 위함.
            yield return null;
            
            
            if (character.isGrounded && _curPlayerState.ShareData.IsJumping)
            {
                break;
            }
        }
        
        animator.CrossFade(PlayerAnimHashingTable.JumpEnd,0.0f);
        
        //땅에 착지할 경우 모든 점프 관련 bool값 초기화.
        _curPlayerState.ShareData.IsJumping = false;
        _curPlayerState.ShareData.IsAirCondition = false;
            
        //점프 도중에 달리기 선입력이 있을 경우 Idle이 아닌 run으로 바로 상태변환.
        ChangeState(enterJump ? PlayerState.Run : PlayerState.Idle);
    }
    //매개변수 : 고정된 위치로의 슬라이딩을 위한 매개변수
    public IEnumerator SlidingCoroutine(Vector3 moveDir)
    {
        float slidingSpeed = 10.0f;
        float slidingAccTime = 1.5f;
        bool enterRun = false;
        bool enterJump = false;
        animator.CrossFade(PlayerAnimHashingTable.Sliding,0.0f);
        
        while (true)
        {
            //도중에 jump를 하여 sliding을 중도에 벗어날 경우 루프탈출.
            if (_curPlayerState != _playerStateGroup[PlayerState.Sliding])
            {
                enterJump = true;
                break;
            }
            
            //슬라이딩 중 방향 전환 x
            _curPlayerState.ShareData.MovePos = moveDir;

            slidingSpeed = Mathf.Lerp(slidingSpeed, 0.0f, slidingAccTime * Time.deltaTime);
            _curPlayerState.ShareData.MoveSpeed = slidingSpeed;

            if (slidingSpeed <= 1.0f)
            {
                break;
            }

            //달리기 선입력시 슬라이딩 이후 바로 달리기 상태.
            if (Input.GetKeyDown(KeyCode.LeftShift))
                enterRun = true;
            
            yield return null;
        }

        ChangeState(enterRun ? PlayerState.Run : PlayerState.Walk);

        if (enterJump)
        {
            ChangeState(PlayerState.Jump);
        }
        _curPlayerState.ShareData.IsSliding = false;

    }
    
}
