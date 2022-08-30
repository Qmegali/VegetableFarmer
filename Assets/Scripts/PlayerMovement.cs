using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input Action References")]

    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction, _crouchAction;
    [SerializeField] private InputActionReference _specialAction, _restartAction;
    private float movementInput = 0f;

    // ^ Consider replacement for the action reference list? Move inputs to an input manager and subscribe to them from this class? That would require it present in every scene; too much?

    [Space, Header("Physics References")]

    [SerializeField] private LayerMask _ground;
    [SerializeField] private BoxCollider2D boxCollider;
    [SerializeField] private Rigidbody2D rb;

    //

    [Space, Header("Animation References")]

    [SerializeField] private Animator _animator;

    //

    [Space, Header("Movement Parameters")]

    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float moveAccel = 20f, airControl = 0.5f, jumpMinHeight = 1.30f, jumpExtraHeight = 3.00f;
    [SerializeField] private float wallSlideSpeed = 1f;

    private bool isGrounded = true, wasGrounded = true, isCrouched = false;
    private float wallSlidingDirection = 0f; 

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
        _specialAction.action.performed += CycleSpecial;
        _restartAction.action.performed += Restart;

        //_boxCollider2D = GetComponent<BoxCollider2D>();
        //_rb = GetComponent<Rigidbody2D>();
    }

    private void OnDestroy()
    {
        _jumpAction.action.started -= QueueJump;
        _jumpAction.action.canceled -= CancelJump;
        _crouchAction.action.started -= StartCrouch;
        _crouchAction.action.canceled -= CancelCrouch;
        _specialAction.action.performed -= CycleSpecial;
        _restartAction.action.performed -= Restart;
    }

    void Update()
    {
        movementInput = _moveAction.action.ReadValue<float>();

        wasGrounded = isGrounded;
        isGrounded = IsGrounded();
        if (!wasGrounded && isGrounded)
        {
            //Debug.Log("Landed!");
            _LandEvent?.Invoke();
        }
        wallSlidingDirection = WallSliding(movementInput);
    }

    private void LateUpdate()
    {
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (rb.gravityScale == 0 && rb.velocity.y < 0.2f * Mathf.Sqrt(-2f * jumpMinHeight * Physics2D.gravity.y)) // If you bump your head while jumping
        {
            CancelJump();

            // Note: at some point, a head-bumping animation MUST be added.
        }
    }

    #endregion

    //

    #region Movement Methods

    private void Move()
    {
        // Horizontal movement

        float xVelocity = rb.velocity.x;
        float xSpeed = Mathf.Abs(xVelocity);

        float xInputDirection = movementInput == 0f ? 0f : Mathf.Sign(movementInput);
        float xDirection = Mathf.Sign(xVelocity);
        if (isCrouched && isGrounded) { movementInput = 0f; }

        float xSpeedInInputDirection = xInputDirection * xVelocity;
        float xSpeedToAdd = moveAccel * Time.deltaTime;

        if (movementInput != 0f) // If the player is trying to move
        {
            if (xSpeedInInputDirection <= moveSpeed) // And if the player is not past max horizontal speed already
            {
                // Then apply any acceleration modifiers...
                if (!isGrounded) { xSpeedToAdd *= airControl; } // (If not on the ground, accelerate less effectively)
                if (xSpeedInInputDirection < 0f) { xSpeedToAdd *= 2f; } // (If turning around, become even more effective at doing so)

                // (If about to hit max speed, accelerate just the right amount)
                if (xSpeedInInputDirection + xSpeedToAdd > moveSpeed) { xSpeedToAdd = moveSpeed - xSpeedInInputDirection; }

                // And apply player input movement change!
                rb.AddForce(xSpeedToAdd / Time.deltaTime * rb.mass * xInputDirection * Vector2.right);
            }
        }
        else // If the player is not trying to move
        {
            if (isGrounded && xSpeed != 0f) // And is on the ground and sliding,
            {
                // Make the acceleration appropriate for skidding to stop... (more effective than accelerating but less effective than turning)
                xSpeedToAdd *= 1.5f;

                // (If about to stop, decelerate just the right amount!)
                if (xSpeed < xSpeedToAdd) { xSpeedToAdd = xSpeed; }

                // And apply player input movement change!
                rb.AddForce(xSpeedToAdd / Time.deltaTime * rb.mass * -xDirection * Vector2.right);
            }
        }

        // Vertical movement
        if (wallSlidingDirection != 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, 100f));
        }
    }

    //

    void QueueJump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            Jump();
        }
        else if (wallSlidingDirection != 0)
        {
            if (rb.velocity.y <= -wallSlideSpeed)
            {
                WallJump(wallSlidingDirection);
            }
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
        rb.AddForce(jumpSpeed * rb.mass * Vector2.up, ForceMode2D.Impulse);
        //Debug.Log("Boing!");

        _LandEvent -= Jump;
        
        rb.gravityScale = 0;
        float jumpBonusHeight = Mathf.Abs(rb.velocity.x) / 12f;
        float extensionTime = (jumpExtraHeight + jumpBonusHeight) / jumpSpeed;
        //Debug.Log(extensionTime);
        _jumpExtensionCoroutine = IJumpExtension(extensionTime);
        StartCoroutine(_jumpExtensionCoroutine);
    }

    void WallJump(float wallDirection)
    {
        float jumpSpeed = Mathf.Sqrt(-2f * jumpMinHeight * Physics2D.gravity.y);
        rb.AddForce(new Vector2(moveSpeed * -wallDirection * rb.mass, (jumpSpeed - rb.velocity.y) * rb.mass), ForceMode2D.Impulse);
        //Debug.Log("Wall boing!");

        rb.gravityScale = 0;
        float extensionTime = jumpExtraHeight / jumpSpeed;
        extensionTime /= 2;
        //Debug.Log(extensionTime);
        _jumpExtensionCoroutine = IJumpExtension(extensionTime);
        StartCoroutine(_jumpExtensionCoroutine);
    }

    IEnumerator IJumpExtension(float extensionTime)
    {
        yield return new WaitForSeconds(extensionTime);
        rb.gravityScale = 1;
        //Debug.Log("Jump maxed! Falling!");
    }

    void CancelJump(InputAction.CallbackContext context)
    {
        CancelJump();
    }

    void CancelJump()
    {
        _LandEvent -= Jump;
        rb.gravityScale = 1;
        if (_jumpExtensionCoroutine != null)
        {
            StopCoroutine(_jumpExtensionCoroutine);
        }
        //Debug.Log("Jump canceled.");
    }

    //

    void StartCrouch(InputAction.CallbackContext context)
    {
        isCrouched = true;
    }

    void CancelCrouch(InputAction.CallbackContext context)
    {
        isCrouched = false;
    }

    //

    bool IsGrounded()
    {
        float sideBuffer = 0.03f;
        Vector2 startPoint = boxCollider.bounds.min + sideBuffer * boxCollider.bounds.extents.x * Vector3.right;
        Vector2 endPoint = startPoint + (1 - sideBuffer) * boxCollider.size.x * Vector2.right;
        Debug.DrawLine(startPoint, endPoint, Color.yellow);
        return Physics2D.OverlapArea(startPoint, endPoint, _ground);
    }

    int WallSliding(float inputDirection) // Returns -1f if pressing into wall on left, 1f if pressing into wall on right, 0f if not pressing against wall
    {
        if (isGrounded || inputDirection == 0f)
        {
            return 0;
        }
        if (inputDirection < 0f)
        {
            float sideBuffer = 0.03f;
            Vector2 startPoint = boxCollider.bounds.min + sideBuffer * boxCollider.bounds.extents.y * Vector3.up;
            Vector2 endPoint = startPoint + (1 - sideBuffer) * boxCollider.size.y * Vector2.up;
            Debug.DrawLine(startPoint, endPoint, Color.red);
            return Physics2D.OverlapArea(startPoint, endPoint, _ground) ? -1 : 0;
        }
        else if (inputDirection > 0f)
        {
            float sideBuffer = 0.03f;
            Vector2 startPoint = boxCollider.bounds.max - sideBuffer * boxCollider.bounds.extents.y * Vector3.up;
            Vector2 endPoint = startPoint - (1 - sideBuffer) * boxCollider.size.y * Vector2.up;
            Debug.DrawLine(startPoint, endPoint, Color.magenta);
            return Physics2D.OverlapArea(startPoint, endPoint, _ground) ? 1 : 0;
        }
        else
        {
            return 0;
        }
    }

    #endregion

    //

    #region Animation Methods

    private void UpdateAnimation()
    {
        if (_moveAction.action.ReadValue<float>() * transform.localScale.x < 0f)
        {
            transform.localScale = new Vector2(-transform.localScale.x, transform.localScale.y);
        }

        _animator.SetBool("Landing", !wasGrounded && isGrounded);

        _animator.SetFloat("RelativeSpeed", Mathf.Abs(rb.velocity.x) / moveSpeed);
        _animator.SetFloat("VerticalSpeed", 1f / (1f + Mathf.Exp(rb.velocity.y / 8f)));
        _animator.SetBool("Crouching", isCrouched);
        _animator.SetBool("Airborne", !isGrounded);
    }

    #endregion

    //

    #region Special Actions

    private void CycleSpecial(InputAction.CallbackContext context)
    {
        if (Time.timeScale == 1)
        {
            Time.timeScale = 0.3f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void Restart(InputAction.CallbackContext context)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion
}
