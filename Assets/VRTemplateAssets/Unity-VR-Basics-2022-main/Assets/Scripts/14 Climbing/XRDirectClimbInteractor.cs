using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;

public class XRDirectClimbInteractor : UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor
{
    public static event Action<string> ClimbHandActivated;
    public static event Action<string> ClimbHandDeactivated;

    private string _controllerName;
    private HandComplete _handComplete;
    private bool _isGrabbingClimbable = false;

    protected override void Start()
    {
        base.Start();
        _controllerName = gameObject.name;
        _handComplete = GetComponent<HandComplete>();
        
        if (_handComplete == null)
        {
            Debug.LogError("HandComplete component not found on " + gameObject.name);
        }
    }
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Check if hand can grab before allowing selection
        if (args.interactableObject.transform.gameObject.tag == "Climbable")
        {
            if (_handComplete != null && !_handComplete.CanGrab())
            {
                // Hand is on cooldown or has no stamina, don't allow grab
                return;
            }
        }
        
        base.OnSelectEntered(args);

        if(args.interactableObject.transform.gameObject.tag == "Climbable")
        {
            _isGrabbingClimbable = true;
            _handComplete?.StartClimbing();
            ClimbHandActivated?.Invoke(_controllerName);  
        }
    }
    
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        if (_isGrabbingClimbable)
        {
            _isGrabbingClimbable = false;
            _handComplete?.StopClimbing();
            ClimbHandDeactivated?.Invoke(_controllerName);
        }
    }
    
    private void Update()
    {
        // Check if hand should force release due to stamina depletion
        if (_isGrabbingClimbable && _handComplete != null)
        {
            if (_handComplete.CurrentStamina <= 0 || _handComplete.IsOnCooldown)
            {
                // Force release the grab
                if (hasSelection)
                {
                    interactionManager.SelectExit(this, firstInteractableSelected);
                }
            }
        }
    }
}