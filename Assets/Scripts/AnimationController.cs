using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HashingAnimTable
{
    public static readonly int Param01 = Animator.StringToHash("Parameter01");
}

public enum StateGroup
{
    IDLE,
    WALK,
    RUN,
    JUMP,
    CROUCH,
    NORMAL
};

[RequireComponent(typeof(Animator))]
public class AnimationController : MonoBehaviour
{
    private int _param01;
    private Animator _animator;
    private StateGroup _state;
    private bool param = false;
    public StateGroup ChangeState(StateGroup state)
    {
        _state = state;
        return _state;
    }
    //0.0f, 3.0f * Time.deltaTime
    public void PlayAnimation()
    {
        switch (_state)
        {
            case StateGroup.IDLE:
                _animator.SetFloat(_param01, (float)_state,0.3f, 5.0f * Time.deltaTime);
                break;
            case StateGroup.RUN:
                _animator.SetFloat(_param01, (float)_state,0.5f, 5.0f * Time.deltaTime);
                break;
            case StateGroup.WALK:
                _animator.SetFloat(_param01, (float)_state,0.5f, 5.0f * Time.deltaTime);
                break;
            case StateGroup.NORMAL:
                _animator.SetFloat(_param01, (float)_state,0.5f, 5.0f * Time.deltaTime);
                break;
            case StateGroup.JUMP:
                if (!param)
                {
                    StartCoroutine(JumpCor());
                }
                break;
        }
    }
    
    void Awake()
    {
        _param01 = HashingAnimTable.Param01;
        _animator = GetComponent<Animator>();
        _state = StateGroup.IDLE;
    }

    IEnumerator JumpCor()
    {
        param = true;
        _animator.CrossFade("jump",0.3f);
        //_animator.SetBool("JumpLoop",true);
        
        while (true)
        {
            _animator.SetBool("JumpLoop",true);
            
            if (_state != StateGroup.JUMP)
            {
                _animator.SetBool("JumpLoop",false);
                break;
            }
            yield return null;
        }
        
        param = false;
    }
}
