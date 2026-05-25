using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public enum CelestialBody { Sun, Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune, Pluto }
    
    [Header("Target Selection")]
    [Tooltip("The planet the camera should focus on by default.")]
    public CelestialBody targetPlanet = CelestialBody.Earth;
    
    [Header("Autopilot Settings")]
    [Tooltip("The maximum speed the camera travels during autopilot flight.")]
    public float autopilotSpeed = 2000.0f; 
    [Tooltip("How smoothly the camera rotates to face a new target selection.")]
    public float rotationSmoothness = 2.0f;
    [Tooltip("How far the camera stops from the planet, relative to the planet's scale.")]
    public float arrivalDistanceMultiplier = 5.0f;

    [Header("Orbital Control Settings")]
    [Tooltip("Sensitivity of the camera rotation when right-clicking and dragging.")]
    public float orbitSensitivity = 3.0f;
    [Tooltip("Sensitivity of the scroll wheel zoom.")]
    public float zoomSensitivity = 5.0f;
    [Tooltip("Smoothing factor for orbital movement and zoom transitions.")]
    public float smoothSpeed = 10f;
    [Tooltip("Minimum allowed zoom distance relative to the planet's scale.")]
    public float minDistanceMultiplier = 1.5f;
    [Tooltip("Maximum allowed zoom distance relative to the planet's scale.")]
    public float maxDistanceMultiplier = 50.0f;

    [Header("Manual Flight Settings")]
    [Tooltip("Base speed for manual movement (WASD/QE).")]
    public float flySpeed = 5000.0f; 
    [Tooltip("Speed multiplier applied when holding Left Shift.")]
    public float fastFlyMultiplier = 10.0f;

    [Header("Events")]
    [Tooltip("Event fired when the autopilot successfully reaches a target.")]
    public UnityEngine.Events.UnityEvent OnAutopilotArrived;

    [Header("Mobile Virtual Inputs (Internal)")]
    [HideInInspector] public Vector2 virtualMoveInput;
    [HideInInspector] public float virtualAltitudeInput;
    [HideInInspector] public Vector2 virtualOrbitInput;
    [HideInInspector] public float virtualZoomInput;
    [HideInInspector] public bool virtualBoost;
    [HideInInspector] public bool isVirtualOrbiting;

    // Internal State
    private Transform activeTarget;
    private bool isAutopilotActive = false;
    private bool isLockedToTarget = false;
    
    // Spherical Coordinates for Orbiting
    private float currentYaw = 0f;
    private float currentPitch = 20f;
    private float currentDistance = 10f;
    private float targetDistance = 10f;

    private float currentSpeed;
    private Vector3 lastTargetPos;
    private Vector3 targetVelocity;
    private float smoothVelocityDist;
    private Vector3 approachDirection;
    private float smoothTime = 0.5f; 

    private void Start()
    {
        autopilotSpeed = 152.4f; // Calibrated for 635,000 km/h (Parker Solar Probe speed)
        UpdateTargetFromDropdown();
        if (activeTarget != null) 
        {
            lastTargetPos = activeTarget.position;
            approachDirection = (transform.position - activeTarget.position).normalized;
            if (approachDirection == Vector3.zero) approachDirection = Vector3.forward;
            currentDistance = Vector3.Distance(transform.position, activeTarget.position);
        }
    }

    private void LateUpdate()
    {
        if (activeTarget != null)
        {
            targetVelocity = (activeTarget.position - lastTargetPos) / Time.deltaTime;
            lastTargetPos = activeTarget.position;
        }

        if (isAutopilotActive && activeTarget != null)
        {
            HandleAutopilot();
        }
        else if (isLockedToTarget && activeTarget != null)
        {
            HandleLockedOrbit();
        }
        else
        {
            HandleManualFlight();
        }
    }

    private void HandleManualFlight()
    {
        // 1. Rotation
        float orbitX = 0;
        float orbitY = 0;

        if (isVirtualOrbiting)
        {
            orbitX = virtualOrbitInput.x;
            orbitY = virtualOrbitInput.y;
        }
        else if (Input.GetMouseButton(1))
        {
            orbitX = Input.GetAxis("Mouse X") * orbitSensitivity;
            orbitY = -Input.GetAxis("Mouse Y") * orbitSensitivity;
        }

        if (orbitX != 0 || orbitY != 0)
        {
            transform.Rotate(Vector3.up, orbitX, Space.World);
            transform.Rotate(Vector3.right, orbitY, Space.Self);
        }

        // 2. Movement
        bool boost = virtualBoost || Input.GetKey(KeyCode.LeftShift);
        float speed = flySpeed * (boost ? fastFlyMultiplier : 1.0f);

        Vector3 moveDir = new Vector3(
            virtualMoveInput.x != 0 ? virtualMoveInput.x : Input.GetAxis("Horizontal"),
            0,
            virtualMoveInput.y != 0 ? virtualMoveInput.y : Input.GetAxis("Vertical")
        );
        
        Vector3 velocity = transform.TransformDirection(moveDir) * speed * Time.deltaTime;
        
        float alt = virtualAltitudeInput != 0 ? virtualAltitudeInput : 0;
        if (Input.GetKey(KeyCode.E)) alt = 1;
        if (Input.GetKey(KeyCode.Q)) alt = -1;

        velocity += Vector3.up * alt * speed * Time.deltaTime;

        if (velocity.magnitude > 0.001f)
        {
            isLockedToTarget = false;
            transform.position += velocity;
        }
        
        currentSpeed = velocity.magnitude / Time.deltaTime;
    }

    private void HandleAutopilot()
    {
        Vector3 targetPos = activeTarget.position;
        float stopDist = activeTarget.localScale.x * arrivalDistanceMultiplier;
        
        // 1. Smoothly decrease distance
        currentDistance = Mathf.SmoothDamp(currentDistance, stopDist, ref smoothVelocityDist, smoothTime, autopilotSpeed);
        
        // Update currentSpeed for HUD (using the absolute value of the distance velocity)
        currentSpeed = Mathf.Abs(smoothVelocityDist);

        // 2. Position the camera along the STABLE approach direction
        // This eliminates the feedback loop jitter because position no longer depends on rotation
        transform.position = targetPos + (approachDirection * currentDistance);

        // 3. Smoothly rotate to look at the planet's center
        // We look at the target position from our NEW stable position
        Vector3 lookDir = (targetPos - transform.position).normalized;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            // We use a slightly faster rotation factor for a more responsive feel
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, (rotationSmoothness * 2f) * Time.deltaTime);
        }

        // 4. Continuously update Yaw and Pitch from the actual position 
        // so the orbital system is perfectly synced at the moment of handover
        Vector3 relativePos = transform.position - targetPos;
        currentYaw = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg + 180f;
        float horizontalDist = new Vector2(relativePos.x, relativePos.z).magnitude;
        currentPitch = Mathf.Atan2(relativePos.y, horizontalDist) * Mathf.Rad2Deg;
        currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);

        // 5. Arrival Check
        // We transition when the distance is reached and rotation is aligned
        if (Mathf.Abs(currentDistance - stopDist) < 0.05f && Vector3.Angle(transform.forward, lookDir) < 1.0f)
        {
            isAutopilotActive = false; 
            isLockedToTarget = true;
            
            targetDistance = currentDistance;
            smoothVelocityDist = 0;

            if (OnAutopilotArrived != null) OnAutopilotArrived.Invoke();
        }
    }

    private void HandleLockedOrbit()
    {
        bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (isVirtualOrbiting)
        {
            currentYaw += virtualOrbitInput.x;
            currentPitch -= virtualOrbitInput.y;
            currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
        }
        else if (Input.GetMouseButton(1))
        {
            currentYaw += Input.GetAxis("Mouse X") * orbitSensitivity;
            currentPitch -= Input.GetAxis("Mouse Y") * orbitSensitivity;
            currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
        }

        if (!isOverUI)
        {
            float scroll = virtualZoomInput != 0 ? virtualZoomInput : Input.GetAxis("Mouse ScrollWheel");
            targetDistance -= scroll * zoomSensitivity * (targetDistance * 0.5f);
            
            float minZoom = activeTarget.localScale.x * minDistanceMultiplier;
            float maxZoom = activeTarget.localScale.x * maxDistanceMultiplier;
            targetDistance = Mathf.Clamp(targetDistance, minZoom, maxZoom);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, smoothSpeed * Time.deltaTime);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);
        
        transform.position = activeTarget.position + offset;
        transform.rotation = rotation;

        if (Mathf.Abs(virtualMoveInput.x) > 0.1f || Mathf.Abs(virtualMoveInput.y) > 0.1f ||
            Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || 
            Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f || 
            Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) || virtualAltitudeInput != 0)
        {
            isLockedToTarget = false;
        }
        
        currentSpeed = 0;
    }

    public void SetAutopilotTarget(Transform target)
    {
        activeTarget = target;
        isAutopilotActive = true;
        isLockedToTarget = false;
        smoothVelocityDist = 0;
        
        if (activeTarget != null) 
        {
            lastTargetPos = activeTarget.position;
            // Capture the stable approach vector immediately
            approachDirection = (transform.position - activeTarget.position).normalized;
            if (approachDirection == Vector3.zero) approachDirection = Vector3.forward;
            currentDistance = Vector3.Distance(transform.position, activeTarget.position);
        }
    }

    public void SetFreeFlight()
    {
        isAutopilotActive = false;
        isLockedToTarget = false;
        activeTarget = null;
    }

    private void UpdateTargetFromDropdown()
    {
        Transform target = SolarSystemRegistry.FindBodyTransform(targetPlanet.ToString());
        if (target == null)
        {
            GameObject obj = GameObject.Find(targetPlanet.ToString());
            if (obj != null) target = obj.transform;
        }

        if (target != null) SetAutopilotTarget(target);
    }

    public float GetCurrentSpeed() => currentSpeed;
    public string GetTargetName() => activeTarget != null ? activeTarget.name : "DEEP SPACE";
    public Transform CurrentTarget => activeTarget;
    public bool IsAutopilot() => isAutopilotActive || isLockedToTarget;
}
