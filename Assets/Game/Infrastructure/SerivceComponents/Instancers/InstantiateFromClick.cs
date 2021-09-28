using System;
using System.Collections;
using System.Collections.Generic;
using Infrastructure.Interfaces.UI.Input;
using UnityEngine;

public class InstantiateFromClick : MonoBehaviour
{
    [SerializeField] private GameObject _instantiate;
    [SerializeField] private Transform _instantiatePoint;
    [SerializeField] private Transform _hierarchyContainer;
    [SerializeField] private InputClick _clickInteraction;

    public void Start()
    {
        _clickInteraction = gameObject.GetComponent<InputClick>();
        _clickInteraction.SubscribeClick(InstantiateObject);
    }

    private void InstantiateObject()
    {
        GameObject instance = Instantiate(
            _instantiate,
            _instantiatePoint.position,
            _instantiatePoint.rotation);

        instance.transform.parent = _hierarchyContainer.transform;
    }
}
