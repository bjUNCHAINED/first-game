using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTransform;

    [SerializeField]
    private bool shouldFaceMoveDirection = true;

    [SerializeField]
    private float maxSpeed = 30f;

    [SerializeField]
    private float acceleration = 30f;

    [SerializeField]
    private float decceleration = 30f;

    [SerializeField]
    private float maxAccelerationMultiplier = 2f;

    [SerializeField]
    private float gravity = -9.81f;

    [SerializeField]
    private float jumpForce = 10f;

    [SerializeField]
    private float windDrag = 0.1f;

    [SerializeField]
    private float gravityMultiplier = 1.0f;

    [SerializeField]
    private float lowJumpMultiplier = 2f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float jumpInput;
    private Vector3 moveDirection;
    private Vector3 currentMoveVelocity;
    private float currentGravityVelocity;
    private bool isGrounded = false;
    private float currentGravityMultiplier = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        SetMoveDirection();
        SetIsGrounded();

        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleGravity();

        MoveController();
    }

    void FixedUpdate() { }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        jumpInput = value.Get<float>();
    }

    public void OnLook(InputValue value) { }

    private void SetMoveDirection()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        moveDirection = forward * moveInput.y + right * moveInput.x;
    }

    private void SetIsGrounded()
    {
        isGrounded = controller.isGrounded;
    }

    private void HandleMovement()
    {
        if (isGrounded)
        {
            // Movement on ground
            if (moveDirection.magnitude >= 0.1f)
            {
                // Accelerate
                Vector3 targetVelocity = moveDirection * maxSpeed;

                // Calculate the difference between current and target velocity
                float velocityDifference = Vector3.Distance(currentMoveVelocity, targetVelocity);

                // Scale acceleration based on the difference (larger difference = higher acceleration)
                // Normalize by maxSpeed to get a 0-1 range, then multiply by maxAccelerationMultiplier
                float accelerationMultiplier =
                    Mathf.Clamp01(velocityDifference / maxSpeed) * maxAccelerationMultiplier + 1f;
                float dynamicAcceleration = acceleration * accelerationMultiplier;

                currentMoveVelocity = Vector3.MoveTowards(
                    currentMoveVelocity,
                    targetVelocity,
                    dynamicAcceleration * Time.deltaTime
                );
            }
            else
            {
                // Decelerate
                currentMoveVelocity = Vector3.MoveTowards(
                    currentMoveVelocity,
                    Vector3.zero,
                    decceleration * Time.deltaTime
                );
            }
        }
        else
        {
            // Movement in air
            Vector3 targetVelocity = moveDirection * maxSpeed;

            currentMoveVelocity = Vector3.MoveTowards(
                currentMoveVelocity,
                targetVelocity,
                acceleration * windDrag * Time.deltaTime
            );
        }
    }

    private void HandleRotation()
    {
        if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                toRotation,
                10f * Time.deltaTime
            );
        }
    }

    private void HandleJump()
    {
        if (jumpInput > 0 && isGrounded)
        {
            currentGravityVelocity = jumpForce;
        }
        if (jumpInput == 0 && !isGrounded)
        {
            currentGravityMultiplier = gravityMultiplier * lowJumpMultiplier;
        }
    }

    private void HandleGravity()
    {
        if (!isGrounded)
        {
            currentGravityVelocity += gravity * currentGravityMultiplier * Time.deltaTime;
        }
        else if (isGrounded && currentGravityVelocity <= 0f)
        {
            // Player is on ground
            currentGravityMultiplier = gravityMultiplier;
            currentGravityVelocity = -1f;
        }
    }

    private void MoveController()
    {
        controller.Move(
            new Vector3(currentMoveVelocity.x, currentGravityVelocity, currentMoveVelocity.z)
                * Time.deltaTime
        );
    }
}
