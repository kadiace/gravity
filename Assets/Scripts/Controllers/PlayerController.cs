using Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions _inputActions;
    private Rigidbody _rb;
    private Transform _body;
    private CapsuleCollider _collider;
    private Transform _wing;

    private CameraController _camera;

    private Quaternion _bodyDefaultRotation;
    private Vector3 _wingDefaultScale;

    private GravityZone _gravityZone;

    private Vector2 _moveInput;
    private bool _isGliding;

    public GravityZone GravityZone { get { return _gravityZone; } set { _gravityZone = value; } }

    public Vector3 GravityDir
    {
        get
        {
            if (_gravityZone == null)
                return Vector3.down;

            return _gravityZone.GetGravityDir(transform.position);
        }
    }

    [Tooltip("Move")]
    [SerializeField] private float _moveSpeed = 5f;

    [Tooltip("Gravity")]
    [SerializeField] private float _gravityAcceleration = 9.8f;
    [SerializeField] private float _gravityRotationSpeed = 5f;
    [SerializeField] private float _maxFallSpeed = 50f;

    [Tooltip("Check Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;

    [Tooltip("Jump")]
    [SerializeField] private float _jumpForce = 8f;

    [Tooltip("Glide")]
    [SerializeField] private float _glideVisualSpeed = 5f;
    [SerializeField] private float _glideTurnSpeed = 90f;
    [SerializeField] private float _glideFallSpeed = 2f;
    [SerializeField] private float _glideConversionSpeed = 10f;
    [SerializeField] private float _glideAcceleration = 100f;

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    void Awake()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Player.Jump.performed += OnJumpPerformed;
        _inputActions.Player.Jump.canceled += OnJumpCanceled;

        _rb = gameObject.GetorAddComponent<Rigidbody>();
        _body = transform.Find("Body");
        _wing = transform.Find("Body/Wing");
        _bodyDefaultRotation = _body.localRotation;
        _wingDefaultScale = _wing.localScale;
        _collider = _body.GetComponent<CapsuleCollider>();

        _camera = Camera.main.GetComponent<CameraController>();
    }

    void Start()
    {

    }

    void Update()
    {
        _moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
        UpdateGlideVisual();
    }

    void FixedUpdate()
    {
        Vector2 moveInput = _moveInput;
        Vector3 gravityDir = GravityDir;

        AlignToGravity(gravityDir);
        if (_isGliding)
        {
            Glide(moveInput, gravityDir);
            return;
        }
        Move(moveInput, gravityDir);
        ApplyGravity(gravityDir, _gravityAcceleration);
    }

    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (context.interaction is TapInteraction)
            Jump();
        else if (context.interaction is HoldInteraction && !IsGrounded())
            _isGliding = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        if (context.interaction is HoldInteraction)
            _isGliding = false;
    }

    private void Jump()
    {
        Vector3 velocity = _rb.linearVelocity;

        float gravityVelocity = Vector3.Dot(velocity, GravityDir);

        if (gravityVelocity > 0f)
        {
            velocity -= GravityDir * gravityVelocity;
            _rb.linearVelocity = velocity;
        }

        _rb.AddForce(-GravityDir * _jumpForce, ForceMode.Impulse);
    }

    private void Glide(Vector2 moveInput, Vector3 gravityDir)
    {
        // No Inputs
        if (moveInput.sqrMagnitude < 0.001f)
        {
            ApplyGravity(gravityDir, _glideAcceleration);
            return;
        }

        // Get camera dir, move dir
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(_camera.transform.forward, gravityDir).normalized;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(_camera.transform.up, gravityDir);
        cameraForward.Normalize();
        Vector3 gravityUp = -gravityDir;
        Vector3 cameraRight =
            Vector3.Cross(gravityUp, cameraForward).normalized;
        Vector3 targetDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;
        targetDirection.Normalize();

        // Extract base speed.
        Vector3 velocity = _rb.linearVelocity;
        Vector3 horizontalVelocity =
            Vector3.ProjectOnPlane(velocity, gravityDir);
        float gravitySpeed =
            Vector3.Dot(velocity, gravityDir);

        // Convert plane velocity to target dir.
        if (horizontalVelocity.sqrMagnitude > 0.001f)
        {
            float horizontalSpeed = horizontalVelocity.magnitude;

            Vector3 horizontalDirection =
                Vector3.RotateTowards(
                    horizontalVelocity.normalized,
                    targetDirection,
                    _glideTurnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime,
                    0f
                );

            horizontalVelocity =
                horizontalDirection * horizontalSpeed;
        }

        // Convert gravity dir velocity to plane velocity.
        float convertibleSpeed =
            Mathf.Max(0f, gravitySpeed - _glideFallSpeed);
        float convertedSpeed = Mathf.Min(
            convertibleSpeed,
            _glideConversionSpeed * Time.fixedDeltaTime
        );

        horizontalVelocity +=
            targetDirection * convertedSpeed;
        gravitySpeed -= convertedSpeed;

        _rb.linearVelocity =
            horizontalVelocity +
            gravityDir * gravitySpeed;
    }

    private void AlignToGravity(Vector3 gravityDir)
    {
        Vector3 targetUp = -gravityDir;

        Vector3 targetForward =
            Vector3.ProjectOnPlane(transform.forward, targetUp);

        if (targetForward.sqrMagnitude < 0.001f)
            targetForward = Vector3.ProjectOnPlane(transform.right, targetUp);

        Quaternion targetRotation =
            Quaternion.LookRotation(targetForward.normalized, targetUp);

        Quaternion rotation = Quaternion.Slerp(
            _rb.rotation,
            targetRotation,
            _gravityRotationSpeed * Time.fixedDeltaTime
        );

        _rb.MoveRotation(rotation);
    }

    private void Move(Vector2 moveInput, Vector3 gravityDir)
    {
        // Get camera dir, move dir
        Vector3 cameraForward =
            Vector3.ProjectOnPlane(_camera.transform.forward, gravityDir).normalized;
        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = Vector3.ProjectOnPlane(_camera.transform.up, gravityDir);
        cameraForward.Normalize();
        Vector3 gravityUp = -gravityDir;
        Vector3 cameraRight =
            Vector3.Cross(gravityUp, cameraForward).normalized;
        Vector3 targetDirection =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;
        targetDirection.Normalize();
    }

    private void ApplyGravity(Vector3 gravityDir, float acceleration)
    {
        float gravitySpeed =
            Vector3.Dot(_rb.linearVelocity, gravityDir);

        if (gravitySpeed >= _maxFallSpeed)
            return;

        float maxAcceleration =
            (_maxFallSpeed - gravitySpeed) / Time.fixedDeltaTime;

        float appliedAcceleration =
            Mathf.Min(acceleration, maxAcceleration);

        _rb.AddForce(
            gravityDir * appliedAcceleration,
            ForceMode.Acceleration
        );
    }

    private bool IsGrounded()
    {
        float radius = _collider.radius * 0.9f;

        Vector3 center = transform.TransformPoint(_collider.center);

        float bottomOffset =
            _collider.height * 0.5f - _collider.radius;

        Vector3 origin =
            center + GravityDir * bottomOffset;

        return Physics.SphereCast(
            origin,
            radius,
            GravityDir,
            out _,
            _groundCheckDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateGlideVisual()
    {
        Quaternion targetBodyRotation;
        Vector3 targetWingScale;

        if (_isGliding)
        {
            Vector3 gravityDir = GravityDir;

            Vector3 horizontalVelocity =
                Vector3.ProjectOnPlane(_rb.linearVelocity, gravityDir);

            if (horizontalVelocity.sqrMagnitude > 0.001f)
            {
                Vector3 glideDirection =
                    horizontalVelocity.normalized;

                Quaternion worldDirectionRotation =
                    Quaternion.LookRotation(
                        glideDirection,
                        -gravityDir
                    );

                Quaternion localDirectionRotation =
                    Quaternion.Inverse(transform.rotation) *
                    worldDirectionRotation;

                targetBodyRotation =
                    localDirectionRotation *
                    _bodyDefaultRotation *
                    Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                targetBodyRotation =
                    _bodyDefaultRotation *
                    Quaternion.Euler(90f, 0f, 0f);
            }

            targetWingScale = _wingDefaultScale;
            targetWingScale.x *= 3f;
        }
        else
        {
            targetBodyRotation = _bodyDefaultRotation;
            targetWingScale = _wingDefaultScale;
        }

        _body.localRotation = Quaternion.Slerp(
            _body.localRotation,
            targetBodyRotation,
            _glideVisualSpeed * Time.deltaTime
        );

        _wing.localScale = Vector3.Lerp(
            _wing.localScale,
            targetWingScale,
            _glideVisualSpeed * Time.deltaTime
        );
    }
}
