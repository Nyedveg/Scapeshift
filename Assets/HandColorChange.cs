using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class HandColorChanger : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty grabAction;  // assign "Select" or "Grip" action here

    [Header("Color Settings")]
    public Color grabColor = Color.red;
    public Color defaultColor = Color.white;
    public float fadeSpeed = 2f;

    private Renderer handRenderer;
    private Material handMaterial;
    private Color currentColor;
    private bool isGrabbing = false;

    void Start()
    {
        handRenderer = GetComponent<Renderer>();
        // Make sure to instantiate the material so changes don’t affect all instances
        handMaterial = handRenderer.material;
        currentColor = defaultColor;
    }

    void Update()
    {
        // Read the grab input value (0 = released, 1 = pressed)
        float grabValue = grabAction.action.ReadValue<float>();
        isGrabbing = grabValue > 0.1f;

        // Target color depends on whether grabbing
        Color targetColor = isGrabbing ? grabColor : defaultColor;

        // Smoothly interpolate color
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * fadeSpeed);
        handMaterial.color = currentColor;
    }
}