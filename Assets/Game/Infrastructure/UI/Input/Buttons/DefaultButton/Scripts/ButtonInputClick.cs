using System;
using Infrastructure.Constants;
using Infrastructure.Interfaces.UI.Input;
using UnityEngine;

public class ButtonInputClick : MonoBehaviour, InputClick
{
    private Animator _animator;
    private Action clickEvent;
    
    private void Start() => _animator = gameObject.GetComponent<Animator>();

    public void OnMouseDown()
    {
        _animator.SetTrigger(UIConstants.CLICK_STATE);
        InvokeInteractions();
    }

    private void InvokeInteractions() => clickEvent?.Invoke();

    public void SubscribeClick(Action action) => clickEvent += action;
}
