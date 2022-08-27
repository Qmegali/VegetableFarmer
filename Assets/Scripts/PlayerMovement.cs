using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input Action References")]

    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction, _crouchAction;

    // ^ Consider replacement for the action reference list? Move inputs to an input manager and subscribe to them from this class? That would require it present in every scene; too much?

    [Space, Header("Physics References")]

    [SerializeField] private LayerMask _ground;
    [SerializeField] private BoxCollider2D _boxCollider2D;
    [SerializeField] private Rigidbody2D _rb;

    //

    [Space, Header("Animation References")]

    [SerializeField] private Animator _animator;

    //

    [Space, Header("Movement Parameters")]

    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float moveAccel = 20f, jumpMinHeight = 1.30f, jumpExtraHeight = 3.00f;

    private bool _isGrounded = true, _wasGrounded = true, _isCrouched = false;

    private UnityAction _LandEvent;
    private IEnumerator _jumpExtensionCoroutine;

    //

    #region Unity Methods

    private void Awake()
    {
        _jumpAction.action.started += QueueJump;
        _jumpAction.action.canceled += CancelJump;
        _crouchAction.action.started += StartCrouch;
        _crouchAction.action.canceled += CancelCrouch;

        //_boxCollider2D = GetComponent<BoxCollider2D>();
        //_rb = GetComponent<Rigidbody2D>();
    }

    private void OnDestroy()
    {
        _jumpAction.action.started -= QueueJump;
        _jumpAction.action.canceled -= CancelJump;
        _crouchAction.action.started -= StartCrouch;
        _crouchAction.action.canceled -= CancelCrouch;
    }

    void Update()
    {
        _wasGrounded = _isGrounded;
        _isGrounded = IsGrounded();
        if (!_wasGrounded && _isGrounded)
        {
            //Debug.Log("Landed!");
            _LandEvent?.Invoke();
        }
    }

    private void FixedUpdate()
    {
        Move();
    }

    #endregion

    //

    #region Movement Methods

    private void Move()
    {
        float movementInput = _moveAction.action.ReadValue<float>();

        float xVelocity = _rb.velocity.x;
        float xSpeed = Mathf.Abs(xVelocity);

        float xInputDirection = movementInput/Mathf.Abs(movementInput);
        float xDirection = xVelocity/xSpeed;
        if (_isCrouched && _isGrounded) { movementInput = 0f; }

        float xSpeedInInputDirection = xInputDirection * xVelocity;
        float xSpeedToAdd = moveAccel * Time.deltaTime;

        if (movementInput != 0f) // If the player is trying to move
        {
            if (xSpeedInInputDirection <= moveSpeed) // And if the player is not past max horizontal speed already
            {
                // Then apply any acceleration modifiers...
                if (!_isGrounded) { xSpeedToAdd *= 0.6f; } // (If not on the ground, accelerate less effectively)
                if (xSpeedInInputDirection < 0f) { xSpeedToAdd *= 2f; } // (If turning around, become even more effective at doing so)

                // (If about to hit max speed, accelerate just the right amount)
                if (xSpeedInInputDirection + xSpeedToAdd > moveSpeed) { xSpeedToAdd = moveSpeed - xSpeedInInputDirection; }

                // And apply player input movement change!
                _rb.AddForce(xSpeedToAdd / Time.deltaTime * _rb.mass * xInputDirection * Vector2.right);
            }
        }
        else // If the player is not trying to move
        {
            if (_isGrounded && xSpeed != 0f) // And is on the ground and sliding,
            {
                // Make the acceleration appropriate for skidding to stop... (more effective than accelerating but less effective than turning)
                xSpeedToAdd *= 1.5f;

                // (If about to stop, decelerate just the right amount!)
                if (xSpeed < xSpeedToAdd) { xSpeedToAdd = xSpeed; }

                // And apply player input movement change!
                _rb.AddForce(xSpeedToAdd / Time.deltaTime * _rb.mass * -xDirection * Vector2.right);
            }
        }
    }

    //

    void QueueJump(InputAction.CallbackContext context)
    {
        if (_isGrounded)
        {
            Jump();
        }
        else
        {
            _LandEvent += Jump;
            //Debug.Log("Waiting to jump...");
        }
    }

    void Jump()
    {
        float jumpSpeed = Mathf.Sqrt(-2f * jumpMinHeight * Physics2D.gravity.y);
        _rb.AddForce(jumpSpeed * _rb.mass * Vector2.up, ForceMode2D.Impulse);
        //Debug.Log("Boing!");

        _LandEvent -= Jump;
        
        _rb.gravityScale = 0;
        float jumpBonusHeight = Mathf.Abs(_rb.velocity.x) / 12f;
        float extensionTime = (jumpExtraHeight + jumpBonusHeight) / jumpSpeed;
        //Debug.Log(extensionTime);
        _jumpExtensionCoroutine = IJumpExtension(extensionTime);
        StartCoroutine(_jumpExtensionCoroutine);
    }

    IEnumerator IJumpExtension(float extensionTime)
    {
        yield return new WaitForSeconds(extensionTime);
        _rb.gravityScale = 1;
        //Debug.Log("Jump maxed! Falling!");
    }

    void CancelJump(InputAction.CallbackContext context)
    {
        _LandEvent -= Jump;
        _rb.gravityScale = 1;
        StopCoroutine(_jumpExtensionCoroutine);
        //Debug.Log("Jump canceled.");
    }

    //

    void StartCrouch(InputAction.CallbackContext context)
    {
        _isCrouched = true;
    }

    void CancelCrouch(InputAction.CallbackContext context)
    {
        _isCrouched = false;
    }

    //

    bool IsGrounded()
    {
        return Physics2D.OverlapArea(_boxCollider2D.bounds.center + _boxCollider2D.bounds.extents - _boxCollider2D.bounds.extents.x * 0.1f * Vector3.right, _boxCollider2D.bounds.center - _boxCollider2D.bounds.extents + _boxCollider2D.bounds.extents.x * 0.1f * Vector3.right, _ground);
    }

    #endregion

    //

    #region Animation Methods



    #endregion
}
