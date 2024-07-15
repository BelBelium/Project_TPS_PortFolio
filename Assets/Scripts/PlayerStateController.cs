using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class StateShareData
{
    public PlayerController MyController;
    
    public float MoveSpeed = 0.0f;

    public bool IsCrounching = false;
    public bool IsJumping = false;
    public bool IsSliding = false;
    public bool IsAirCondition = false;
    public bool IsAiming = false;
    
    public Vector3 MoveDir = Vector3.zero;
    public Vector3 LookDir = Vector3.zero;
    public Vector3 MovePos = Vector3.zero;
    public Vector3 GravityDir = Vector3.zero;
}

//플레이어의 이동,방향 제어 스크립트.
public class PlayerStateController 
{
    public StateShareData ShareData;
    
    private const float Gravity = -5.81f;
    public const float GravityAcc = 3.0f;

    //현재 상태 플레이를 자식클래스의 StatePlay의 함수를 사용하기 위함.
    public virtual void StatePlay()
    {
        MoveApply();
        LookApply();
        GravityApply();
        AimApply();
    }

    private void AimApply()
    {
        if (Input.GetMouseButtonDown(1) && ShareData.IsAiming)
        {
            ShareData.MyController.aimCamera.SetActive(false);
            ShareData.IsAiming = false;
            ShareData.MyController.aim.weight = 0.0f;
            ShareData.MyController.bodyAim.weight = 0.0f;
        }
        else if(Input.GetMouseButtonDown(1) && !ShareData.IsAiming)
        {
            ShareData.MyController.aimCamera.SetActive(true);
            ShareData.IsAiming = true;
            ShareData.MyController.aim.weight = 1.0f;
            ShareData.MyController.bodyAim.weight = 1.0f;
        }
        ReleaseCameraDistance();
        ShareData.MyController.animator.SetBool(PlayerAnimHashingTable.AimingMode,ShareData.IsAiming);
    }
    private void MoveApply()
    {
        Vector3 prevDir = ShareData.MoveDir;

        ShareData.MoveDir = !ShareData.IsJumping
            ? new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"))
            : new Vector3(Input.GetAxis("Horizontal") * 0.5f, 0.0f, Input.GetAxis("Vertical") * 0.5f);

        if (ShareData.IsJumping && ShareData.MoveDir == Vector3.zero)
        {
            ShareData.MoveDir = prevDir;
        }

        ShareData.MovePos = ShareData.MyController.cameraController.camArm.TransformDirection(ShareData.MoveDir);
        ShareData.MovePos.y = 0.0f;
    }
    private void LookApply()
    {
        float rotSpeed = 10.0f;

        //평상시에 캐릭터의 이동방향
        if (!ShareData.IsAiming)
        {
            ShareData.LookDir = ShareData.MyController.cameraController.camArm.TransformDirection(ShareData.MoveDir);

            ShareData.LookDir.y = 0.0f;
        }

        // 문제점 : Rig Animation으로 돌려놓은 회전 값에 아래에 있는 코드로 캐릭터를 회전 시키면 회전값이 더해져서 상체가 더 돌아가게 된다.
        // 해결방안 : Rig Animation을 모델링 오브젝트가 들어있는 자식 오브젝트의 컴포넌트로 붙혀준다.
        // //에임 조준시에 캐릭터의 조준방향
        else
        {
            Vector3 worldAimTarget = ShareData.MyController.playerAimingController.debugTransform.position;
            worldAimTarget.y = ShareData.MyController.charBody.transform.position.y;
            ShareData.LookDir = (worldAimTarget - ShareData.MyController.charBody.transform.position).normalized;
        }

        ShareData.MyController.charBody.transform.forward = Vector3.Slerp(ShareData.MyController.charBody.transform.forward,
            ShareData.LookDir, rotSpeed * Time.deltaTime);
    }
    private void GravityApply()
    {
        //캐릭터가 지상에 있고 점프 상태가 아닐 경우, 캐릭터의 지형에 있는 판정을 높이기 위함.
        if (ShareData.MyController.character.isGrounded && !ShareData.IsJumping)
        {
            ShareData.GravityDir.y = -0.8f;
        }

        ShareData.GravityDir.y += Gravity * Time.deltaTime;
    }

    //상태가 추가되면 switch에 추가.
    public static PlayerStateController InitState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                return new PlayerIdle();
            case PlayerState.Run:
                return new PlayerRun();
            case PlayerState.Walk:
                return new PlayerWalk();
            case PlayerState.Jump:
                return new PlayerJump();
            case PlayerState.Sliding:
                return new PlayerSliding();
            case PlayerState.Crouch:
                return new PlayerCrouch();
        }

        return null;
    }

    protected void InputJump()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReleaseAim();
            ShareData.MyController.ChangeState(PlayerState.Jump);
        }
    }
    protected void InputCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) && ShareData.IsCrounching)
        {
            ShareData.IsCrounching = false;
            ShareData.MyController.ChangeState(PlayerState.Idle);
            ShareData.MyController.animator.SetBool(PlayerAnimHashingTable.Crouch,ShareData.IsCrounching);
        }
        else if (Input.GetKeyDown(KeyCode.LeftControl) && !ShareData.IsCrounching)
        {
            ShareData.IsCrounching = true;
            ShareData.MyController.ChangeState(PlayerState.Crouch);
            ShareData.MyController.animator.SetBool(PlayerAnimHashingTable.Crouch,ShareData.IsCrounching);
        }
    }
    protected void InputSprint()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            ReleaseCrouch();
            ReleaseAim();
            ShareData.MyController.ChangeState(PlayerState.Run);
        }
    }
    protected void InputSliding()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            ShareData.MyController.ChangeState(PlayerState.Sliding);
        }
    }
    protected void InputIdle()
    {
        ShareData.MyController.ChangeState(PlayerState.Idle);
    }
    protected void InputWalk()
    {
        ShareData.MyController.ChangeState(PlayerState.Walk);
    }
    //aiming 상태가 가능한 때 : idle, walk, crouch, jump
    protected void InputAiming()
    {
        if (ShareData.IsAiming)
        {
            ShareData.MyController.ChangeState(PlayerState.Idle);
        }
    }
    protected void AirCheck()
    {
        if (!ShareData.MyController.character.isGrounded && !ShareData.IsAirCondition)
        {
            ShareData.MyController.ChangeState(PlayerState.Jump, true);
        }
    }
    
    
    private void ReleaseCameraDistance()
    {
        const float blendTime = 5.0f;
        ShareData.MyController.cameraController._distance.CameraDistance = ShareData.IsAiming
            ? Mathf.Lerp(ShareData.MyController.cameraController._distance.CameraDistance, 3.0f,
                blendTime * Time.deltaTime)
            : Mathf.Lerp(ShareData.MyController.cameraController._distance.CameraDistance, 2.0f,
                blendTime * Time.deltaTime);
    }
    private void ReleaseAim()
    {
        if (ShareData.IsAiming)
        {
            ShareData.MyController.aimCamera.SetActive(false);
            ShareData.IsAiming = false;
            ShareData.MyController.aim.weight = 0.0f;
            ShareData.MyController.bodyAim.weight = 0.0f;
        }
    }
    private void ReleaseCrouch()
    {
        if (ShareData.IsCrounching)
        {
            ShareData.IsCrounching = false;
            ShareData.MyController.animator.SetBool(PlayerAnimHashingTable.Crouch,ShareData.IsCrounching);
        }
    }
}

    //Idle에서 변경 가능한 상태 : run,sliding을 제외한 모든 상태 가능. run은 walk를 거쳐서 감.
    class PlayerIdle : PlayerStateController
    {
        private const float IdleSpeed = 0.0f;
        private const float BlendTime = 5.0f;

        public override void StatePlay()
        {
            base.StatePlay();
            
            //다른 상태에서 idle 상태로의 속도 블렌딩
            ShareData.MoveSpeed = Mathf.Lerp(ShareData.MoveSpeed, IdleSpeed, BlendTime * Time.deltaTime);

            if (ShareData.MoveDir.magnitude != 0)
            {
                InputWalk();
            }
            InputJump();
            InputCrouch();

            AirCheck();
        }
    }
    //Walk에서 변경 가능한 상태 : walk -> idle / walk -> jump / walk -> run / walk -> crouch
    class PlayerWalk : PlayerStateController
    {
        private const float WalkSpeed = 3.0f;
        private const float AimingWalkSpeed = 2.0f;
        private const float BlendTime = 5.0f;
        public override void StatePlay()
        {
            base.StatePlay();
            //다른 상태에서 walk 상태로의 속도 블렌딩
            if (ShareData.IsAiming)
            {
                ShareData.MoveSpeed = Mathf.Lerp(ShareData.MoveSpeed,AimingWalkSpeed,BlendTime*Time.deltaTime);
            }
            else
            {
                ShareData.MoveSpeed = Mathf.Lerp(ShareData.MoveSpeed,WalkSpeed,BlendTime*Time.deltaTime);
            }
            
            //이동값의 값이 존재한다면 walk 혹은 shift가 눌렸다면 run으로 상태변화.
            if (ShareData.MoveDir.magnitude != 0)     
            {
                InputSprint();
                InputCrouch();
                InputJump();
            }
            
            //이동값이 없다면 idle로 변경.
            else
            {
                InputIdle();
            }
            AirCheck();
        }
    }
    //Run에서 변경 가능한 상태 : run -> walk / run -> jump / run -> sliding
    class PlayerRun : PlayerStateController
    {
        private const float SprintSpeed = 5.0f;
        private const float BlendTime = 5.0f;
        public override void StatePlay()
        {
            base.StatePlay();

            ShareData.MyController.cameraController._distance.CameraDistance = Mathf.Lerp(ShareData.MyController.cameraController._distance.CameraDistance,3.0f,BlendTime*Time.deltaTime);
            //다른 상태에서 run 상태로의 속도 블렌딩
            ShareData.MoveSpeed = Mathf.Lerp(ShareData.MoveSpeed , SprintSpeed , BlendTime * Time.deltaTime);
            //ShareData.MyController.cameraController;
            
            if (ShareData.MoveDir.magnitude != 0)
            {
                InputSliding();
                InputJump();
                InputAiming();
            }
            else
            {
                InputWalk();
            }
            
            AirCheck();
        }
    }
    //Jump에서 변경 가능한 상태 : Jump -> Idle
    class PlayerJump : PlayerStateController
    {
        public override void StatePlay()
        {
            base.StatePlay();
            //플레이어가 자체적으로 점프를 하였을 경우 실행.
            if (!ShareData.IsJumping && !ShareData.IsAirCondition)
            {
                ShareData.IsJumping = true;
                ShareData.IsAirCondition = true;
                ShareData.MyController.StartCoroutine(ShareData.MyController.JumpLoopCoroutine(ShareData.IsJumping));
            }
            
            //플레이어 자체적으로 점프를 누른게 아닌 떨어질 경우 실행.
            if (ShareData.IsAirCondition && !ShareData.IsJumping)
            {
                ShareData.IsJumping = true;
                ShareData.MyController.StartCoroutine(ShareData.MyController.JumpLoopCoroutine(!ShareData.IsJumping));
            }
        }
    }
    //Crouch에서 변경 가능한 상태 : Crouch -> Idle / Crouch -> Walk / Crouch -> Run
    class PlayerCrouch : PlayerStateController
    {
        private const float IdleValue = 0.0f;
        private const float CrouchSpeed = 2.0f;
        private const float AimingCrouchSpeed = 1.5f;
        private const float BlendTime = 7.0f;
        public override void StatePlay()
        {
            base.StatePlay();

            if (ShareData.MoveDir.magnitude != 0)
            {
                ShareData.MoveSpeed = ShareData.IsAiming
                    ? Mathf.Lerp(ShareData.MoveSpeed, AimingCrouchSpeed, BlendTime * Time.deltaTime)
                    : Mathf.Lerp(ShareData.MoveSpeed, CrouchSpeed, BlendTime * Time.deltaTime);
            }
            else
            {
                ShareData.MoveSpeed = Mathf.Lerp(ShareData.MoveSpeed,IdleValue,BlendTime*Time.deltaTime);
            }
            InputSprint();
            InputCrouch();
            
            AirCheck();
        }
    }
    //Sliding에서 변경 가능한 상태 : Sliding -> Walk / Sliding -> Jump
    class PlayerSliding : PlayerStateController
    {
        public override void StatePlay()
        {
            if (!ShareData.IsSliding)
            {
                ShareData.MyController.StartCoroutine(ShareData.MyController.SlidingCoroutine(ShareData.MovePos));
                ShareData.IsSliding = true;
            }
            InputJump();
            AirCheck();
        }
    }
    

