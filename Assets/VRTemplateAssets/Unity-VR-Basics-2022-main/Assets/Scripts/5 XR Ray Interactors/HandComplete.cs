using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class HandComplete : MonoBehaviour
{
    //Stores handPrefab to be Instantiated
    public GameObject handPrefab;
    
    //Allows for hiding of hand prefab if set to true
    public bool hideHandOnSelect = false;
    
    //Stores what kind of characteristics we're looking for with our Input Device when we search for it later
    public InputDeviceCharacteristics inputDeviceCharacteristics;

    // Stamina settings
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f; // Stamina lost per second while holding
    public float staminaRegenRate = 5f; // Stamina gained per second while not holding (slower than drain)
    public float cooldownDuration = 2f; // How long before hand can be used again after giving up
    
    [Header("Visual Feedback")]
    public float flashThreshold = 20f; // When stamina drops below this, start flashing
    public float flashSpeed = 5f; // How fast the hand flashes
    
    // Private stamina variables
    private float _currentStamina;
    private bool _isHoldingClimbable = false;
    private bool _isOnCooldown = false;
    private float _cooldownTimer = 0f;
    
    // Hand visual references
    private Color _originalHandColor;
    private bool _isFlashing = false;
    private float _flashTimer = 0f;

    //Stores the InputDevice that we're Targeting once we find it in InitializeHand()
    private InputDevice _targetDevice;
    private Animator _handAnimator;
    private SkinnedMeshRenderer _handMesh;

    // Public properties to check stamina state
    public bool IsOnCooldown => _isOnCooldown;
    public float CurrentStamina => _currentStamina;
    public float StaminaPercentage => _currentStamina / maxStamina;

    public void HideHandOnSelect()
    {
        if (hideHandOnSelect)
        {
            _handMesh.enabled = !_handMesh.enabled;
        }
    }
    
    private void Start()
    {
        InitializeHand();
        _currentStamina = maxStamina;
    }

    private void InitializeHand()
    {
        List<InputDevice> devices = new List<InputDevice>();
        //Call InputDevices to see if it can find any devices with the characteristics we're looking for
        InputDevices.GetDevicesWithCharacteristics(inputDeviceCharacteristics, devices);

        //Our hands might not be active and so they will not be generated from the search.
        //We check if any devices are found here to avoid errors.
        if (devices.Count > 0)
        {
            
            _targetDevice = devices[0];

            GameObject spawnedHand = Instantiate(handPrefab, transform);
            _handAnimator = spawnedHand.GetComponent<Animator>();
            _handMesh = spawnedHand.GetComponentInChildren<SkinnedMeshRenderer>();
            
            // Store original hand color
            if (_handMesh != null && _handMesh.material != null)
            {
                _originalHandColor = _handMesh.material.color;
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        //Since our target device might not register at the start of the scene, we continously check until one is found.
        if(!_targetDevice.isValid)
        {
            InitializeHand();
        }
        else
        {
            UpdateHand();
        }
        
        UpdateStamina();
        UpdateHandVisuals();
        UpdateCooldown();
    }

    private void UpdateHand()
    {
        //This will get the value for our trigger from the target device and output a flaot into triggerValue
        if (_targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
        {
            _handAnimator.SetFloat("Trigger", triggerValue);
        }
        else
        {
            _handAnimator.SetFloat("Trigger", 0);
        }
        //This will get the value for our grip from the target device and output a flaot into gripValue
        if (_targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
        {
            _handAnimator.SetFloat("Grip", gripValue);
        }
        else
        {
            _handAnimator.SetFloat("Grip", 0);
        }
    }
    
    private void UpdateStamina()
    {
        if (_isOnCooldown)
        {
            // Don't update stamina during cooldown
            return;
        }
        
        if (_isHoldingClimbable)
        {
            // Drain stamina while holding
            _currentStamina -= staminaDrainRate * Time.deltaTime;
            
            // Check if stamina depleted
            if (_currentStamina <= 0)
            {
                _currentStamina = 0;
                ForceRelease();
            }
        }
        else
        {
            // Regenerate stamina when not holding
            _currentStamina += staminaRegenRate * Time.deltaTime;
            _currentStamina = Mathf.Min(_currentStamina, maxStamina);
        }
    }
    
    private void UpdateHandVisuals()
    {
        if (_handMesh == null || _handMesh.material == null)
            return;
        
        if (_isOnCooldown)
        {
            // During cooldown, keep hand dark/gray
            _handMesh.material.color = Color.gray;
            return;
        }
        
        // Calculate how tired the hand is (0 = full stamina, 1 = exhausted)
        float fatigueLevel = 1f - StaminaPercentage;
        
        // Interpolate from original color to red based on fatigue
        Color targetColor = Color.Lerp(_originalHandColor, Color.red, fatigueLevel);
        
        // Check if we should flash
        if (_currentStamina <= flashThreshold && _currentStamina > 0)
        {
            // Flash between current fatigue color and a brighter red
            _flashTimer += Time.deltaTime * flashSpeed;
            float flashIntensity = (Mathf.Sin(_flashTimer) + 1f) / 2f; // Oscillates between 0 and 1
            _handMesh.material.color = Color.Lerp(targetColor, Color.red * 1.5f, flashIntensity);
        }
        else
        {
            // Normal color transition
            _handMesh.material.color = targetColor;
            _flashTimer = 0f;
        }
    }
    
    private void UpdateCooldown()
    {
        if (_isOnCooldown)
        {
            _cooldownTimer -= Time.deltaTime;
            
            if (_cooldownTimer <= 0)
            {
                // Cooldown finished, hand is usable again
                _isOnCooldown = false;
                
                // Restore hand color
                if (_handMesh != null && _handMesh.material != null)
                {
                    _handMesh.material.color = _originalHandColor;
                }
            }
        }
    }
    
    private void ForceRelease()
    {
        // This will be called from XRDirectClimbInteractor
        _isOnCooldown = true;
        _cooldownTimer = cooldownDuration;
        _isHoldingClimbable = false;
        _currentStamina = 0;
    }
    
    // Called by XRDirectClimbInteractor when grabbing a climbable object
    public void StartClimbing()
    {
        if (!_isOnCooldown)
        {
            _isHoldingClimbable = true;
        }
    }
    
    // Called by XRDirectClimbInteractor when releasing a climbable object
    public void StopClimbing()
    {
        _isHoldingClimbable = false;
    }
    
    // Check if this hand can grab (not on cooldown and has stamina)
    public bool CanGrab()
    {
        return !_isOnCooldown && _currentStamina > 0;
    }
}