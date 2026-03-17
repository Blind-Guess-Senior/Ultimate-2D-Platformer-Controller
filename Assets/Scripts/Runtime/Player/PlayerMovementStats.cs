using UnityEngine;

namespace Runtime.Player
{
    [CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "Player/Player Movement Stats")]
    public class PlayerMovementStats : ScriptableObject
    {
        #region Fields

        #region Walk

        [Header("Settings")] [Header("Walk")] [Range(0f, 100f)]
        public float maxWalkSpeed = 12.5f;

        [Range(0.01f, 50f)] public float groundAcceleration = 5f;
        [Range(0.01f, 50f)] public float groundDeceleration = 20f;
        [Range(0.01f, 50f)] public float airAcceleration = 5f;
        [Range(0.01f, 50f)] public float airDeceleration = 20f;

        #endregion

        #region Run

        [Header("Run")] [Range(0f, 100f)] public float maxRunSpeed = 20f;

        #endregion

        #region Jump

        [Header("Jump")] [Range(0f, 20f)] public float jumpHeight = 6.5f;
        [Range(1f, 1.1f)] public float jumpHeightCompensationFactor = 1.054f;
        public float timeTillJumpApex = 0.35f;
        [Range(0.01f, 5f)] public float gravityOnReleaseMultiplier = 2f;
        public float maxFallSpeed = 26f;
        [Range(1, 5)] public int numberOfJumpsAllowed = 1;

        [Header("Jump Cut")] [Range(0.02f, 0.03f)]
        public float timeForUpwardsCancel = 0.027f;

        [Header("Jump Apex (in ratio)")] [Range(0.5f, 1f)]
        public float apexThreshold = 0.97f;

        [Range(0.01f, 1f)] public float apexHangTime = 0.075f;

        [Header("Jump Buffer")] [Range(0f, 1f)]
        public float jumpBufferTime = 0.125f;

        [Header("Jump Coyote Time")] [Range(0f, 1f)]
        public float jumpCoyoteTime = 0.1f;

        [Header("JumpVisualization Tool")] public bool showWalkJumpArc = false;
        public bool showRunJumpArc = false;
        public bool stopOnCollision = true;
        public bool drawRight = false;
        [Range(5, 100)] public int arcResolution = 20;
        [Range(0, 500)] public int visualizationSteps = 90;

        #endregion

        #region Collision Checks

        [Header("Grounded / Collision Checks")]
        public LayerMask groundLayer;

        public float groundDetectionRayLength = 0.02f;
        public float headDetectionRayLength = 0.02f;
        [Range(0f, 1f)] public float headWidth = 0.75f; // In ratio

        #endregion

        #region Gravity

        [Header("Gravity")] public float Gravity { get; private set; }
        public float InitialJumpVelocity { get; private set; }

        public float AdjustedJumpHeight { get; private set; }

        #endregion

        #endregion

        #region Methods

        #region Unity Event Methods

        private void OnValidate()
        {
            CalculateValues();
        }

        private void OnEnable()
        {
            CalculateValues();
        }

        #endregion

        private void CalculateValues()
        {
            AdjustedJumpHeight = jumpHeight * jumpHeightCompensationFactor;
            Gravity = -(2f * AdjustedJumpHeight) / Mathf.Pow(timeTillJumpApex, 2f);
            InitialJumpVelocity = Mathf.Abs(Gravity) * timeTillJumpApex;
        }

        #endregion
    }
}