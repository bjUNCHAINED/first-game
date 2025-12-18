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

    private Vector2 moveInput;
    private Vector3 moveDirection;
    private CharacterController controller;

    private Vector3 currentVelocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        SetMoveDirection();
        HandleMovement();
        HandleRotation();
    }

    void FixedUpdate()
    {
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
    }

    private void SetMoveDirection()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        moveDirection = forward*moveInput.y + right * moveInput.x;
    }

    private void HandleMovement()
    {
        if (moveDirection.magnitude >= 0.1f)
        {
            // Accelerate
            Vector3 targetVelocity = moveDirection * maxSpeed;
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        }
        else
        {
            // Decelerate
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, decceleration * Time.deltaTime );
        }

        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (shouldFaceMoveDirection && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion toRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, toRotation, 10f * Time.deltaTime);
        }
    }
}
