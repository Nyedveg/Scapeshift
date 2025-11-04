using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbProvider : MonoBehaviour
{
    public static event Action ClimbActive;
    public static event Action ClimbInActive;

    public CharacterController characterController;
    public Transform rightHandTransform;
    public Transform leftHandTransform;

    private bool _rightActive = false;
    private bool _leftActive = false;
    
    private Vector3 _previousHandPosition;

    private void Start()
    {
        XRDirectClimbInteractor.ClimbHandActivated += HandActivated;
        XRDirectClimbInteractor.ClimbHandDeactivated += HandDeactivated;
    }

    private void OnDestroy()
    {
        XRDirectClimbInteractor.ClimbHandActivated -= HandActivated;
        XRDirectClimbInteractor.ClimbHandDeactivated -= HandDeactivated;
    }

    private void HandActivated(string _controllerName)
    {
        if(_controllerName == "LeftHand Controller")
        {
            _leftActive = true;
            _rightActive = false;
            _previousHandPosition = leftHandTransform.position;
        }
        else
        {
            _leftActive = false;
            _rightActive = true;
            _previousHandPosition = rightHandTransform.position;
        }

        ClimbActive?.Invoke();
    }
    
    private void HandDeactivated(string _controllerName)
    {
        if (_rightActive && _controllerName == "RightHand Controller")
        {
            _rightActive = false;
            ClimbInActive?.Invoke();
        }
        else if (_leftActive && _controllerName == "LeftHand Controller")
        {
            _leftActive = false;
            ClimbInActive?.Invoke();
        }
    }

    private void FixedUpdate()
    {
        if (_rightActive || _leftActive)
        {
            Climb();
        }
    }

    private void Climb()
    {
        Transform activeHand = _leftActive ? leftHandTransform : rightHandTransform;
        
        // Calculate hand movement since last frame
        Vector3 handMovement = activeHand.position - _previousHandPosition;
        
        // Move character controller in opposite direction of hand movement
        // This creates the climbing effect
        characterController.Move(-handMovement);
        
        // Store current position for next frame
        _previousHandPosition = activeHand.position;
    }
}