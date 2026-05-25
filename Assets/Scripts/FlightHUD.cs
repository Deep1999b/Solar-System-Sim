using UnityEngine;
using TMPro;

public class FlightHUD : MonoBehaviour
{
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI etaText; // NEW
    public TextMeshProUGUI modeText;
    public RectTransform velocityBar;
    
    private CameraFollow shipCam;
    private float maxSpeedDisplay = 1000000f; // Scale bar for 1M KM/H

    void Start()
    {
        if (Camera.main != null)
        {
            shipCam = Camera.main.GetComponent<CameraFollow>();
        }
    }

    void Update()
    {
        if (shipCam == null && Camera.main != null)
        {
            shipCam = Camera.main.GetComponent<CameraFollow>();
        }

        if (shipCam == null) return;

        // Convert Unity Units/sec to KM/H
        // Math: (Units * 100,000 km) / 24 hours = Units * 4166.67
        float unitsPerSec = shipCam.GetCurrentSpeed();
        float kmh = unitsPerSec * 4166.67f; 
        
        speedText.text = $"VELOCITY: {kmh:N0} KM/H";
        
        string targetName = shipCam.GetTargetName();
        targetText.text = $"TARGET: {targetName}";

        // Calculate ETA using Unity Units (Dist / Speed)
        // Since 1 second = 1 Earth Day, the result is in Earth Days.
        if (etaText != null)
        {
            Transform target = shipCam.CurrentTarget;

            if (target != null && targetName != "NONE" && targetName != "DEEP SPACE")
            {
                float distUnits = Vector3.Distance(Camera.main.transform.position, target.position);
                
                // ETA (Days) = Distance (Units) / Speed (Units/sec)
                // Use 152.4 units/sec as the reference speed for the Parker Solar Probe setting
                float refSpeed = unitsPerSec > 1f ? unitsPerSec : 152.4f;
                double travelDays = distUnits / refSpeed;
                
                etaText.text = $"ETA: {travelDays:F1} EARTH DAYS";
                etaText.gameObject.SetActive(true);
            }
            else
            {
                etaText.gameObject.SetActive(false);
            }
        }

        if (shipCam.IsAutopilot())
        {
            modeText.text = "MODE: AUTOPILOT";
            modeText.color = Color.cyan;
        }
        else
        {
            modeText.text = "MODE: MANUAL FLIGHT";
            modeText.color = Color.white;
        }

        // Velocity Bar Animation
        if (velocityBar != null)
        {
            float fill = Mathf.Clamp01(kmh / maxSpeedDisplay);
            velocityBar.anchorMax = new Vector2(fill, 1);
        }
    }
}
