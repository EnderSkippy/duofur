using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("World")] [Range(-15, 10)] public float gravity = -9.81f;

    [Header("Player")] [Range(0.1f, 2)] public float height = 1.3f;

    [Header("Movement")] [Range(1, 10)] public float walkSpeed = 3;

    [Range(0.1f, 2)] public float jumpHeight;

    [Header("Camera")] public Camera mainCamera;

    [Range(0.1f, 3)] public float sensitivity;
    public bool lockCamera;
    public bool enableCamSmooth;


    [Header("Interaction")] public InteractionMode interactionMode = InteractionMode.None;

    [HideInInspector] public GameObject selection;
    [HideInInspector] public string[] selectionTags;

    [Header("UI")] public GameObject cursor;

    public GameObject controlUx;
    public TMP_Text cursorText;
    public LayerMask uiLayerMask;

    private Vector2 _camAcceleration;
    private float _camHeight;
    private bool _clickGamepad;

    //Other
    private CharacterController _controller;
    private InputAction _crouchAction;
    private bool _crouchBool;
    private Vector2 _cStick;
    private float _currentSpeed;
    private InputAction _exitAction;
    private bool _fixedUpdatelowerFPS;
    private bool _flashEnabled;

    // Flashlight
    private Light _flashlight;
    private InputAction _flashlightAction;
    private InputAction _interactAction;


    private bool _isJumping;
    private InputAction _jumpAction;

    //New Input
    private int _jumpFrames;
    private Outline _lastOutline;
    private InputAction _lookAction;
    private InputAction _menuAction;

    // Input Actions
    private InputAction _moveAction;

    private PlaybackUI _playbackUI;
    private InputAction _rollAction;

    private Vector3 _rotation = Vector3.zero;

    private bool _runGamepad;
    private InputAction _scrollY;
    private float _speedVelocity;
    private InputAction _sprintAction;
    private float _targetFOV;
    private float _targetRoll;

    private float _verticalVelocity;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        Cursor.lockState = CursorLockMode.Locked;

        _playbackUI = controlUx.GetComponentInChildren<PlaybackUI>();
        _playbackUI.ToggleUI(false);

        //Initialize Variables
        _controller = GetComponent<CharacterController>();
        _flashlight = gameObject.GetComponentInChildren<Light>();

        // Get Actions
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");
        _rollAction = InputSystem.actions.FindAction("Roll");
        _interactAction = InputSystem.actions.FindAction("Interact");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _crouchAction = InputSystem.actions.FindAction("Crouch");
        _flashlightAction = InputSystem.actions.FindAction("Flashlight");
        _menuAction = InputSystem.actions.FindAction("Menu");
        _exitAction = InputSystem.actions.FindAction("Exit");
        _scrollY = InputSystem.actions.FindAction("ScrollY");
    }

    private void Update()
    {
        cursor.SetActive(false);

        if (!lockCamera)
        {
            Cursor.lockState = CursorLockMode.Locked;
            HandleLocomotion();
            HandleCamera();
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        HandleUI();

        if (interactionMode == InteractionMode.Selection)
            HandleSelectMode(selectionTags);
    }


    private void FixedUpdate()
    {
        RayCastClick();
    }


    /// <summary>
    ///     Handles everything to do with player locomotion
    ///     Walking, sprinting, crouching, etc
    ///     Jetbrains Rider was a lifesaver when writing this script, saved my ass.
    /// </summary>
    private void HandleLocomotion()
    {
        // Get movement input
        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        float moveForward = moveInput.y;
        float moveRight = moveInput.x;

        // Calculate movement direction relative to the camera's rotation
        Vector3 forward = mainCamera.transform.forward;
        Vector3 right = mainCamera.transform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        float targetSpeed = Mathf.Clamp01(moveInput.magnitude) * walkSpeed;

        // Sprint
        if (_sprintAction.ReadValue<float>() > 0.5f)
            targetSpeed *= 2;

        // Smoothen the movement out
        _currentSpeed = Mathf.SmoothDamp(_currentSpeed, targetSpeed, ref _speedVelocity, 0.5f);

        // Calculate movement
        Vector3 moveDirection = (forward * moveForward + right * moveRight).normalized * _currentSpeed;

        // Jumping
        if (_controller.isGrounded && _verticalVelocity < 0)
            _verticalVelocity = 0f;

        if (_jumpAction.triggered && _controller.isGrounded)
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Gravity
        _verticalVelocity += gravity * Time.deltaTime;

        // Crouching
        float targetHeight = _crouchAction.ReadValue<float>() > 0.5f ? height / 2 : height;
        _controller.height = Mathf.Lerp(_controller.height, targetHeight, Time.deltaTime * 5);

        // Apply movement
        Vector3 velocity = moveDirection + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }


    /// <summary>
    ///     Handles everything to do with the player camera
    ///     Looking, zooming, roll, flashlight, etc
    /// </summary>
    private void HandleCamera()
    {
        Vector2 look = -_lookAction.ReadValue<Vector2>();
        float roll = _rollAction.ReadValue<float>();
        float scroll = _scrollY.ReadValue<float>();

        // Camera Zoom
        _targetFOV -= scroll * 10f;
        _targetFOV = Mathf.Clamp(_targetFOV, 10f, 110f);

        mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, _targetFOV, Time.deltaTime * 2f);

        // Flashlight
        if (_flashlightAction.WasPressedThisFrame())
            _flashEnabled = !_flashEnabled;

        _flashlight.intensity = Mathf.Lerp(_flashlight.intensity, _flashEnabled ? 20 : 0, Time.deltaTime * 10);

        // Camera Rotation
        _rotation.x += look.x * sensitivity;
        _rotation.y += look.y * sensitivity;
        _rotation.y = Mathf.Clamp(_rotation.y, -90f, 90f);

        // Accumulate roll input over time into target roll
        _targetRoll -= roll * Time.deltaTime * 15f;
        _targetRoll = Mathf.Clamp(_targetRoll, -90f, 90f); // Limit max roll angle

        // Lerp actual rotation.z towards the target roll for smooth movement
        _rotation.z = Mathf.Lerp(_rotation.z, _targetRoll, Time.deltaTime * 2f);

        Quaternion targetRotation = Quaternion.Euler(_rotation.y, -_rotation.x, _rotation.z);
        mainCamera.transform.rotation =
            Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    /// <summary>
    ///     Handles everything to do with the player UI
    ///     Control menu, etc
    /// </summary>
    private void HandleUI()
    {
        if (_menuAction.triggered)
        {
            lockCamera = !lockCamera;
            _playbackUI.ToggleUI(lockCamera);
        }

        if (_exitAction.triggered)
            SceneManager.LoadScene("Launcher");
    }

    private void RayCastClick()
    {
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, 10f,
                uiLayerMask))
        {
            cursor.SetActive(true);
            Button3D hitcol = hit.collider.GetComponent<Button3D>();
            if (hitcol != null)
            {
                cursor.SetActive(true);
                cursorText.text = hitcol.buttonText;

                if (Input.GetMouseButtonDown(0)) hitcol.StartClick(gameObject.name);
                if (Input.GetMouseButtonUp(0)) hitcol.EndClick(gameObject.name);
            }
        }
    }

    // Messy as hell code, god forbid anyone who tries working on this :sobs:
    public void HandleSelectMode(string[] tags = null)
    {
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, 20f))
        {
            GameObject targetObject = GetOutlineCandidate(hit.collider.gameObject);

            // Shit code, but for some reason 64th nestled in the models a lot in an animatronic prefab so we gotta do this.
            if (targetObject == null || (tags != null && !tags.Contains(targetObject.tag) &&
                                         !tags.Contains(targetObject.transform.parent.tag) &&
                                         !tags.Contains(targetObject.transform.parent.tag)))
            {
                ClearOutline();
                return;
            }

            cursor.SetActive(true);

            Outline outline = targetObject.GetComponent<Outline>();

            if (outline == null)
                outline = targetObject.AddComponent<Outline>();

            outline.OutlineColor = targetObject == selection ? new Color(244, 119, 0) : Color.white;

            if (_lastOutline != outline)
            {
                ClearOutline();
                _lastOutline = outline;
            }

            if (_interactAction.triggered)
            {
                if (selection != null)
                    Destroy(selection.GetComponent<Outline>());

                selection = targetObject;
            }
        }
        else
        {
            ClearOutline();
        }
    }

    private GameObject GetOutlineCandidate(GameObject hitObject)
    {
        if (hitObject != null)
            return hitObject;

        Transform parent = hitObject.transform.parent;
        while (parent != null)
        {
            if (parent.gameObject != null)
                return parent.gameObject;
            parent = parent.parent;
        }

        return null;
    }

    private void ClearOutline()
    {
        if (_lastOutline != null && _lastOutline.gameObject != selection)
        {
            Destroy(_lastOutline);
            _lastOutline = null;
        }
    }
}

public enum InteractionMode
{
    None,
    Selection
}