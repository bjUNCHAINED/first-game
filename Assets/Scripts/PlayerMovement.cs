using System;
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
    private float maxSpeed = 6f;

    [SerializeField]
    private float acceleration = 3f;

    [SerializeField]
    private float decceleration = 30f;

    [SerializeField]
    private float maxAccelerationMultiplier = 4f;

    [SerializeField]
    private float gravity = -9.81f;

    [SerializeField]
    private float jumpForce = 4.5f;

    [SerializeField]
    private float windDrag = 0.7f;

    [SerializeField]
    private float gravityMultiplier = 1.5f;

    [SerializeField]
    private float lowJumpMultiplier = 2f;

    [SerializeField]
    private float coyoteTime = 0.1f;

    [SerializeField]
    private float wallRunningDetectDistance = 0.5f;

    [SerializeField]
    private float wallRunningSpeedMultiplier = 1.5f;

    [SerializeField]
    private float wallRunningAccelerationMultiplier = 1.5f;

    [SerializeField]
    private float wallRunningDecelerationMultiplier = 0.5f;

    [SerializeField]
    private float wallRunningJumpForceMultiplier = 0.5f;

    private CharacterController controller;
    private Vector2 moveInput;
    private float jumpInput;
    private Vector3 moveDirection;
    private Vector3 currentMoveVelocity;
    private float currentGravityVelocity;
    private bool isGrounded = false;
    private float currentGravityMultiplier = 1.0f;
    private float coyoteTimer;
    private bool jumpHeld = false;
    private bool wallRunning = false;
    private bool isFacingWall = false;
    private Vector3 wallNormal;
    private Vector3 intendedHorizontalVelocity;
    private Vector3 actualHorizontalVelocity;
    private float velocityDifference = 0f;

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
        SetWallRunning();

        HandleMovement();
        HandleRotation();
        HandleJump();
        HandleGravity();
        HandleCoyoteTime();

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

    private void SetWallRunning()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        isFacingWall = Physics.Raycast(ray, out hit, wallRunningDetectDistance);

        if (
            !wallRunning
            && isFacingWall
            && !isGrounded
            && jumpHeld == true
            && currentGravityVelocity > 0
        )
        {
            wallRunning = true;
            wallNormal = hit.normal;
            currentGravityVelocity += jumpForce * wallRunningJumpForceMultiplier;
        }
        if (isGrounded)
        {
            wallRunning = false;
            wallNormal = Vector3.zero;
        }
    }

    private void HandleMovement()
    {
        if (wallRunning)
        {
            // If wall running, redirect the movement direction to go along the wall
            moveDirection = Vector3.Cross(wallNormal, Vector3.up);

            if (
                (transform.forward - moveDirection).magnitude
                > (transform.forward + moveDirection).magnitude
            )
            {
                moveDirection = -moveDirection;
            }

            // If moving upwards, accelerate, otherwise decelerate
            if (currentGravityVelocity > 0)
            {
                currentMoveVelocity = Vector3.MoveTowards(
                    currentMoveVelocity,
                    moveDirection * maxSpeed * wallRunningSpeedMultiplier,
                    acceleration * wallRunningAccelerationMultiplier * Time.deltaTime
                );
            }
            else
            {
                currentMoveVelocity = Vector3.MoveTowards(
                    currentMoveVelocity,
                    Vector3.zero,
                    decceleration * wallRunningDecelerationMultiplier * Time.deltaTime
                );
            }
        }
        else if (isGrounded)
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
        // Player should jump
        if (jumpInput > 0 && coyoteTimer > 0f && currentGravityVelocity <= 0 && !jumpHeld)
        {
            currentGravityMultiplier = gravityMultiplier;
            currentGravityVelocity = jumpForce;
        }

        // Fast fall after jump
        if (jumpInput == 0 && !isGrounded)
        {
            currentGravityMultiplier = gravityMultiplier * lowJumpMultiplier;
        }

        if (jumpInput > 0)
        {
            jumpHeld = true;
        }
        else
        {
            jumpHeld = false;
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

    private void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    private void MoveController()
    {
        // Move the player according to the intended movement
        Vector3 intendedMovement =
            new Vector3(currentMoveVelocity.x, currentGravityVelocity, currentMoveVelocity.z)
            * Time.deltaTime;

        Vector3 positionBeforeMove = transform.position;
        CollisionFlags collisionFlags = controller.Move(intendedMovement);
        Vector3 positionAfterMove = transform.position;

        // Calculate actual movement that occurred
        Vector3 actualMovement = positionAfterMove - positionBeforeMove;

        // Use a minimum deltaTime threshold to prevent division by very small values
        // This prevents instability at high frame rates
        float safeDeltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 actualVelocity = actualMovement / safeDeltaTime;

        // Extract horizontal velocity (ignore Y component)
        intendedHorizontalVelocity = new Vector3(currentMoveVelocity.x, 0, currentMoveVelocity.z);
        actualHorizontalVelocity = new Vector3(actualVelocity.x, 0, actualVelocity.z);
        velocityDifference = Vector3.Distance(intendedHorizontalVelocity, actualHorizontalVelocity);

        // Only update velocity if:
        // 1. We have significant intended velocity (player is trying to move)
        // 2. There's a significant difference between intended and actual (wall collision)
        // 3. The actual velocity is not near zero (prevent stopping when not intended)
        // 4. We actually hit something (Sides collision flag indicates wall hit)
        bool hitWall = (collisionFlags & CollisionFlags.Sides) != 0;

        if (
            intendedHorizontalVelocity.magnitude > 0.1f
            && hitWall
            && velocityDifference > 0.5f
            && actualHorizontalVelocity.magnitude > 0.01f // Prevent setting to near-zero
        )
        {
            currentMoveVelocity = actualHorizontalVelocity;
        }
    }
}
