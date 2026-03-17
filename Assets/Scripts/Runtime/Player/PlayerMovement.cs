using Runtime.Core.Input;
using UnityEngine;

namespace Runtime.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        #region Fields

        #region References

        [Header("References")] public PlayerMovementStats moveStats;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private Collider2D featCollider;

        private Rigidbody2D _rb;

        #endregion

        #region Runtime Fields

        #region Horizental Movement

        private Vector2 _moveVelocity;
        private bool _isFacingRight;

        #endregion

        #region Collision Checks

        private RaycastHit2D _groundHit;
        private RaycastHit2D _headHit;
        private bool _isGrounded;
        private bool _bumpedHead;

        #endregion

        #region Jump

        public float VerticalVelocity { get; private set; }
        private bool _isJumping;
        private bool _isFastFalling;
        private bool _isFalling;
        private float _fastFallTime;
        private float _fastFallReleaseSpeed;
        private int _numberOfJumpsUsed;

        private float _apexPoint;
        private float _timePastApexThreshold;
        private bool _isPastApexThreshold;

        private float _jumpBufferTimer;
        private bool _jumpReleasedDuringBuffer;

        private float _coyoteTimer;

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Unity Event Methods

        private void Awake()
        {
            _isFacingRight = true;
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            CountTimers();
            JumpChecks();
        }

        private void FixedUpdate()
        {
            // 1. Check collision
            CollisionChecks();

            // 2. Jump
            Jump();

            // 3. Move using right acceleration and deceleration
            if (_isGrounded)
            {
                Move(moveStats.groundAcceleration, moveStats.groundDeceleration, InputManager.Movement);
            }
            else
            {
                Move(moveStats.airAcceleration, moveStats.airDeceleration, InputManager.Movement);
            }
        }

        #endregion

        #region Movement & Turn

        private void Move(float acceleration, float deceleration, Vector2 moveInput)
        {
            if (moveInput != Vector2.zero)
            {
                // 1. Check if it needs to turn around
                TurnCheck(moveInput);

                // 2. Calculate move velocity
                Vector2 targetVelocity;
                if (InputManager.RunIsHeld)
                {
                    targetVelocity = new Vector2(moveInput.x, 0f) * moveStats.maxRunSpeed;
                }
                else
                {
                    targetVelocity = new Vector2(moveInput.x, 0f) * moveStats.maxWalkSpeed;
                }

                _moveVelocity = Vector2.Lerp(_moveVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

                // 3. Apply move velocity
                _rb.linearVelocityX = _moveVelocity.x;
            }
            else if (moveInput == Vector2.zero)
            {
                // 1. Calculate move velocity
                _moveVelocity = Vector2.Lerp(_moveVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);

                // 2. Apply move velocity
                _rb.linearVelocityX = _moveVelocity.x;
            }
        }

        private void TurnCheck(Vector2 moveInput)
        {
            if (_isFacingRight && moveInput.x < 0)
            {
                // Turn left
                Turn(false);
            }
            else if (!_isFacingRight && moveInput.x > 0)
            {
                // Turn right
                Turn(true);
            }
        }

        private void Turn(bool turnRight)
        {
            if (turnRight)
            {
                _isFacingRight = true;
                transform.Rotate(0f, 180f, 0f);
            }
            else
            {
                _isFacingRight = false;
                transform.Rotate(0f, -180f, 0f);
            }
        }

        #endregion

        #region Collision Check

        private void IsGrounded()
        {
            // 1. Set box cast size
            Vector2 boxCastOrigin = new Vector2(featCollider.bounds.center.x, featCollider.bounds.center.y);
            Vector2 boxCastSize = new Vector2(featCollider.bounds.size.x, moveStats.groundDetectionRayLength);

            // 2. Do box cast
            _groundHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.down,
                moveStats.groundDetectionRayLength, moveStats.groundLayer);

            // 3. Check if grounded
            _isGrounded = (bool)_groundHit.collider;

#if UNITY_EDITOR
            Color rayColor;
            if (_isGrounded)
            {
                rayColor = Color.darkOliveGreen;
            }
            else
            {
                rayColor = Color.crimson;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y),
                Vector2.down * moveStats.groundDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2, boxCastOrigin.y),
                Vector2.down * moveStats.groundDetectionRayLength, rayColor);
            Debug.DrawRay(
                new Vector2(boxCastOrigin.x - boxCastSize.x / 2, boxCastOrigin.y - moveStats.groundDetectionRayLength),
                Vector2.right * boxCastSize.x, rayColor);
#endif
        }

        private void BumpedHead()
        {
            // 1. Set box cast size
            Vector2 boxCastOrigin = new Vector2(featCollider.bounds.center.x, bodyCollider.bounds.max.y);
            Vector2 boxCastSize = new Vector2(featCollider.bounds.size.x * moveStats.headWidth,
                moveStats.headDetectionRayLength);

            // 2. Do box cast
            _headHit = Physics2D.BoxCast(boxCastOrigin, boxCastSize, 0f, Vector2.up,
                moveStats.headDetectionRayLength, moveStats.groundLayer);

            // 3. Check if grounded
            _bumpedHead = (bool)_headHit.collider;

#if UNITY_EDITOR
            float headWidth = moveStats.headWidth;
            Color rayColor;
            if (_bumpedHead)
            {
                rayColor = Color.darkSeaGreen;
            }
            else
            {
                rayColor = Color.crimson;
            }

            Debug.DrawRay(new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth, boxCastOrigin.y),
                Vector2.up * moveStats.headDetectionRayLength, rayColor);
            Debug.DrawRay(new Vector2(boxCastOrigin.x + boxCastSize.x / 2 * headWidth, boxCastOrigin.y),
                Vector2.up * moveStats.headDetectionRayLength, rayColor);
            Debug.DrawRay(
                new Vector2(boxCastOrigin.x - boxCastSize.x / 2 * headWidth,
                    boxCastOrigin.y + moveStats.headDetectionRayLength),
                Vector2.right * (boxCastSize.x * headWidth), rayColor);
#endif
        }

        private void CollisionChecks()
        {
            IsGrounded();
            BumpedHead();
        }

        #endregion

        #region Jump

        private void JumpChecks()
        {
            // 1. Pressed jump
            if (InputManager.JumpWasPressed)
            {
                // 1.1 Reset jump buffer
                _jumpBufferTimer = moveStats.jumpBufferTime;
                // 1.2 No release during jump buffer
                _jumpReleasedDuringBuffer = false;
            }

            // 2. Released jump
            if (InputManager.JumpWasReleased)
            {
                // 2.1 Cancel buffer jump
                if (_jumpBufferTimer > 0f)
                {
                    _jumpReleasedDuringBuffer = true;
                }

                // 2.2 Release when jumping
                if (_isJumping && VerticalVelocity > 0f)
                {
                    // 2.2.1 Past jump apex
                    if (_isPastApexThreshold)
                    {
                        // 2.2.1.1 Reset apex past
                        _isPastApexThreshold = false;
                        // 2.2.1.2 Fast fall if release
                        _isFastFalling = true;
                        // 2.2.1.3 Set that we have pass the time threshold of jump cut / jump cancel
                        _fastFallTime = moveStats.timeForUpwardsCancel;
                        // 2.2.1.4 Reset vertical velocity
                        VerticalVelocity = 0f; // Otherwise change direction may take too long
                    }
                    // 2.2.2 No past jump apex
                    else
                    {
                        // 2.2.2.1 Fast fall if release
                        _isFastFalling = true;
                        // 2.2.2.2 Set initial fast fall speed
                        _fastFallReleaseSpeed = VerticalVelocity;
                    }
                }
            }

            // 3. Init jump with jump buffering and coyote time
            // Jump buffer timer > 0 means all conditions are met in the time window after jump pressed last time
            // If is jumping, it will not be an initiate jump
            // Player needs grounded, or coyote is available
            if (_jumpBufferTimer > 0f && !_isJumping && (_isGrounded || _coyoteTimer > 0f))
            {
                // 3.1 Init jump
                InitiateJump(1);
                // 3.2 A corner case: we press jump and release jump both in air and both in window of current buffer.
                //      Then we should jump but not for a complete jump
                if (_jumpReleasedDuringBuffer)
                {
                    // 3.2.1 Using fast falling to reduce jump height to make a "bunny jump"
                    _isFastFalling = true;
                    _fastFallReleaseSpeed = VerticalVelocity;
                }
            }

            // 4. Double jump
            // Parallel case of init jump
            else if (_jumpBufferTimer > 0f && _isJumping && _numberOfJumpsUsed < moveStats.numberOfJumpsAllowed)
            {
                // 4.1 Reset fast fall
                _isFastFalling = false;
                // 4.2 Jump
                InitiateJump(1);
            }

            // 5. Air jump after coyote time
            // Parallel case of init jump & double jump
            // It is not a coyote jump, since we are out of coyote time (coyote time is in init jump case)
            // So it would take 2 jumps since it will be treated as an air double jump
            else if (_jumpBufferTimer > 0f && _isFalling && _numberOfJumpsUsed < moveStats.numberOfJumpsAllowed - 1)
            {
                // 4.1 Reset fast fall
                _isFastFalling = false;
                // 4.2 Jump
                InitiateJump(2);
            }

            // 6. Check landed
            // If we are jumping or falling but got grounded and landed (velocity < 0)
            if ((_isJumping || _isFalling) && _isGrounded && VerticalVelocity < 0f)
            {
                // 6.1 Clear all vars
                _isJumping = false;
                _isFalling = false;
                _isFastFalling = false;
                _fastFallTime = 0f;
                _isPastApexThreshold = false;
                _numberOfJumpsUsed = 0;
                // 6.2 Reset gravity
                VerticalVelocity = Physics2D.gravity.y;
            }
        }

        /// <summary>
        /// Make an initiate jump.
        /// </summary>
        /// <param name="numberOfJumpsUsed">The number of jumps that this jump will use.</param>
        private void InitiateJump(int numberOfJumpsUsed)
        {
            // 1. Set isJumping
            if (!_isJumping)
            {
                _isJumping = true;
            }

            // 2. Reset jump buffer as we had met condition, no more jump allowed in this window
            _jumpBufferTimer = 0f;

            // 3. Increase jumps used
            _numberOfJumpsUsed += numberOfJumpsUsed;

            // 4. Set initial jump speed
            VerticalVelocity = moveStats.InitialJumpVelocity;
        }

        private void Jump()
        {
            // 1. Apply gravity when jumping
            if (_isJumping)
            {
                // 1.1 Check for head bump
                if (_bumpedHead)
                {
                    // 1.1.1 Set fast fall to behave like bumped
                    _isFastFalling = true;
                }

                // 1.2 Gravity on ascending
                if (VerticalVelocity > 0f)
                {
                    // 1.2.1 Calculate current velocity's ratio in apex
                    //          If our speed get close to 0, it means we are about to reach the apex
                    _apexPoint = Mathf.InverseLerp(moveStats.InitialJumpVelocity, 0f, VerticalVelocity);

                    // 1.2.2 Apex control, if we are ascending but reach apex
                    if (_apexPoint > moveStats.apexThreshold)
                    {
                        // 1.2.2.1 Check if we had reach apex before
                        //              If not
                        if (!_isPastApexThreshold)
                        {
                            // 1.2.2.1.1 Set we had reach apex
                            _isPastApexThreshold = true;
                            // 1.2.2.1.2 Set we reach apex for 0 sec
                            _timePastApexThreshold = 0f;
                        }
                        // 1.2.2.2 Check if we had reach apex before
                        //              If so
                        else
                        {
                            // 1.2.2.2.1 Update time after reach apex
                            _timePastApexThreshold += Time.fixedDeltaTime;
                            // 1.2.2.2.2 Check if we need hang
                            if (_timePastApexThreshold < moveStats.apexHangTime)
                            {
                                // Set to zero to behave like hanging
                                VerticalVelocity = 0f;
                            }
                            else
                            {
                                // Set to small negative value to start falling
                                VerticalVelocity = -0.01f;
                            }
                        }
                    }

                    // 1.2.3 Gravity on ascending, if we are ascending and not reach apex
                    //          Parallel case with 1.2.2
                    else
                    {
                        // 1.2.3.1 Reduce vertical velocity by gravity, gravity is negative
                        VerticalVelocity += moveStats.Gravity * Time.fixedDeltaTime;
                        // 1.2.3.2 Reset past apex
                        if (_isPastApexThreshold)
                        {
                            _isPastApexThreshold = false;
                        }
                    }
                }
                // 1.3 Gravity on descending while we are still upping and not fast falling
                //      Parallel case with 1.2
                else if (!_isFastFalling)
                {
                    // 1.3.1 Apply gravity to vertical velocity with a fix constant, gravity is negative
                    VerticalVelocity += moveStats.Gravity * moveStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }

                // 1.4 Gravity on descending while we are falling
                //      Parallel case with 1.2 1.3
                else if (VerticalVelocity < 0f)
                {
                    // 1.4.1 Set we are falling
                    if (!_isFalling)
                    {
                        _isFalling = true;
                    }
                }
            }

            // 2. Handle jump cut
            //      Which is a quick, low and nearly horizontal jump
            //      Happen when we press and release jump quickly
            //      When we release, fast fall is true
            //      And if we release in a low level (no reaching apex), fast fall time is 0, so we will down very fast
            if (_isFastFalling)
            {
                // 2.1 Check if we had fast fall for enough time
                //      If so, velocity reduce in normal way
                if (_fastFallTime >= moveStats.timeForUpwardsCancel)
                {
                    VerticalVelocity += moveStats.Gravity * moveStats.gravityOnReleaseMultiplier * Time.fixedDeltaTime;
                }
                // 2.2 Check if we have not fast fall enough time
                //      If so, velocity reduce very fast
                else if (_fastFallTime < moveStats.timeForUpwardsCancel)
                {
                    VerticalVelocity = Mathf.Lerp(_fastFallReleaseSpeed, 0f,
                        _fastFallTime / moveStats.timeForUpwardsCancel);
                }

                // 2.3 Add fast fall time
                _fastFallTime += Time.fixedDeltaTime;
            }

            // 3. Handle gravity when natural falling
            if (!_isGrounded && !_isJumping)
            {
                // 3.1 Set falling state
                if (!_isFalling)
                {
                    _isFalling = true;
                }

                // 3.2 Calculate falling speed
                VerticalVelocity += moveStats.Gravity * Time.fixedDeltaTime;
            }

            // 4. Clamp fall speed
            //      50f is a random value that means fastest up speed, it is unreached without bug
            VerticalVelocity = Mathf.Clamp(VerticalVelocity, -moveStats.maxFallSpeed, 50f);

            //  5. Apply fall speed
            _rb.linearVelocityY = VerticalVelocity;
        }

        #endregion

        #region Timers

        private void CountTimers()
        {
            _jumpBufferTimer -= Time.deltaTime;

            if (!_isGrounded)
            {
                _coyoteTimer -= Time.deltaTime;
            }
            else
            {
                _coyoteTimer = moveStats.jumpCoyoteTime;
            }
        }

        #endregion

        #endregion
    }
}