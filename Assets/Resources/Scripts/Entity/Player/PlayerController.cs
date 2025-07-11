using System.Collections;
using AYellowpaper.SerializedCollections;
using Resources.Scripts.Entity.GameManagers;
using Resources.Scripts.Entity.Player.Enum;
using Resources.Scripts.Entity.SpellMode;
using Spine;
using UnityEngine;

namespace Resources.Scripts.Entity.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : EntityController
    {
        [Header("Animations")]
        [SerializedDictionary("Animation Code", "Value")]
        public SerializedDictionary<EPlayerAnimationName, string> animations;

        [Header("Movement Settings")]
        [SerializeField] private PlayerJoystick joystick;

        [Header("Dodge Roll Settings")]
        [SerializeField, Range(0.1f, 3f)] private float rollSpeedMultiplier = 2f;


        #region Private Fields
        private PlayerStats _playerStats;
        private DrawingManager _drawingManager;
        #endregion

        #region Unity Methods
        
        private void Start()
        {
            InitComponents();
            InitAnimations();
        }
        
        private void Update()
        {
            UpdateInput();
            UpdateAnimations();
        }
        
        #region Init
        private void InitComponents()
        {
            _playerStats = GetComponent<PlayerStats>();
            _drawingManager = GetComponent<DrawingManager>();
        }

        private void InitAnimations()
        {
            SkeletonAnimation.state.Complete += HandleAnimationComplete;
        }
        #endregion

        private void UpdateInput()
        {
            var h = joystick ? joystick.InputVector.x : 0f;
            var v = joystick ? joystick.InputVector.y : 0f;

            if (Mathf.Approximately(h + v, 0f))
            {
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
            MoveDirection = new Vector2(h, v);
        }
        #endregion

        #region Movement & Animation Helpers
        private void UpdateAnimations()
        {
            if (_drawingManager.IsDrawing) PlayAnimation(animations, EPlayerAnimationName.Draw);
            else if (GetCurrentVelocity() > 0f) PlayAnimation(animations, EPlayerAnimationName.Run, true);
            else if (Mathf.Approximately(GetCurrentVelocity(), 0f)) PlayAnimation(animations, EPlayerAnimationName.Idle, true);
        }
        #endregion

        #region Dodge Roll
        public void TryRoll()
        {
            StartCoroutine(RollCoroutine());
        }

        private IEnumerator RollCoroutine()
        {
            PlayAnimation(animations, EPlayerAnimationName.Jump);

            ApplyPush(MoveDirection * rollSpeedMultiplier);
            yield return null;
        }
        #endregion

        #region Other Effects
        protected override void Die()
        {
            PlayAnimation(animations, EPlayerAnimationName.Death);
        }
        #endregion

        #region Spine Helper
        private void HandleAnimationComplete(TrackEntry entry)
        {
            if (entry.Animation.Name != animations[EPlayerAnimationName.Death]) return;
            StageProgressionManager.Instance.ShowGameOver();
            Destroy(gameObject);
        }
        #endregion
    }
}
