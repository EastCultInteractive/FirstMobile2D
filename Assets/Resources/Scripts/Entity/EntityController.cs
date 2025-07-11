using System;
using System.Collections;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Resources.Scripts.Entity
{
	public class EntityController : MonoBehaviour
	{
		[SerializeField] private Color damageFlashColor = Color.red;	
		[SerializeField, Range(0.1f, 1f)] private float damageFlashDuration = 0.3f;
		
		private Skeleton _skeleton;
		private EntityStats _stats;
		
		protected SkeletonAnimation SkeletonAnimation;
		protected Rigidbody2D RigidBodyInstance;
		protected Vector3 MoveDirection = Vector3.zero;
		
		private float _currentAnimationSpeed;
		private TrackEntry _currentTrack;
		
		private const float AnimationSmoothing = 5f;

		public bool IsDead => _stats.Health <= 0;

		private void Awake()
		{
			InitEntity();
			InitAnimations();
		}

		private void FixedUpdate()
		{
			UpdateMove();
		}

		#region Init

		private void InitEntity()
		{
			RigidBodyInstance = GetComponent<Rigidbody2D>();
			_stats = GetComponent<EntityStats>();
		}
		
		private void InitAnimations()
		{
            var spineChild = transform.Find("SpineVisual");
            SkeletonAnimation = spineChild.GetComponent<SkeletonAnimation>();
            _skeleton = SkeletonAnimation.Skeleton;
		}
		#endregion

		#region Updates
		private void UpdateMove()
		{
			var speed = _stats.MovementSpeed * _stats.SlowMultiplier;
            
			RigidBodyInstance.AddForce(MoveDirection.normalized * (speed * Time.deltaTime), ForceMode2D.Force);
			TurnToDirection(MoveDirection);
            
			UpdateAnimationSpeed(GetCurrentVelocity());
		}
		#endregion
		
		#region Public Methods
		public void TakeDamage(EntityController from)
		{
			// if (isImmortal || isRolling || IsDead || drawingManager.IsDrawing || playerStats.TryEvade(transform.position))
			//     return;

			_stats.Health -= from._stats.Damage;
			ApplyDamageFlash();
			ApplyPush((transform.position - from.transform.position) * from._stats.PushDistance);
            
			if (_stats.Health <= 0) Die();
		}
		#endregion
        
        #region Other Effects
		public void ApplyPush(Vector2 force) => RigidBodyInstance.AddForce(force, ForceMode2D.Impulse);
        public void ApplyDash(Transform from) => transform.Translate((transform.position - from.position).normalized);
        public void ApplySlow(float factor, float duration) => StartCoroutine(MoveSpeedEffect(factor, duration));
        public void ApplySpeedBoost(float factor, float duration) => StartCoroutine(MoveSpeedEffect(factor, duration));
        public void ApplyStun(float duration) => StartCoroutine(MoveSpeedEffect(0f, duration));
        private void ApplyDamageFlash() => StartCoroutine(DamageFlash());
        
        private IEnumerator MoveSpeedEffect(float factor, float duration)
        {
            var baseSlow = _stats.SlowMultiplier;
            _stats.SlowMultiplier = factor;
            yield return new WaitForSeconds(duration);
            _stats.SlowMultiplier = baseSlow;
        }
        

        private IEnumerator DamageFlash()
        {
            _skeleton.SetColor(damageFlashColor);
            yield return new WaitForSeconds(damageFlashDuration);
            _skeleton.SetColor(Color.white);
        }

        #endregion
		
        #region Animations
        protected void PlayAnimation<T>(
	        SerializedDictionary<T, string> animations,
	        T newAnimation,
	        bool loop = false
	        ) where T : IConvertible
        {
	        if (newAnimation.Equals(GetCurrentAnimation(animations))) return;
            SkeletonAnimation.state.SetAnimation(0, animations[newAnimation], loop);
        }

        protected T GetCurrentAnimation<T>(SerializedDictionary<T, string> animations) where T : IConvertible
        {
	        _currentTrack = SkeletonAnimation.state.GetCurrent(0);
	        return _currentTrack == null ? default : GetAnimationByName(animations, _currentTrack.Animation.Name);
        }

        protected T GetAnimationByName<T>(SerializedDictionary<T, string> animations, string animName) where T : IConvertible
        {
            return animations.FirstOrDefault(pair => pair.Value == animName).Key;
        }

        
        protected void UpdateAnimationSpeed(float velocity)
        {
	        _currentAnimationSpeed = Mathf.Lerp(_currentAnimationSpeed, velocity, Time.deltaTime * AnimationSmoothing);
    
	        if (_currentTrack != null)
	        {
		        _currentTrack.TimeScale = _currentAnimationSpeed;
	        }
        }
        #endregion
        
        #region Skeleton

        protected void TurnToDirection(Vector2 direction)
        {
	        if (direction.magnitude < 0.1f) return;

	        SkeletonAnimation.Skeleton.ScaleX = -Mathf.Abs(SkeletonAnimation.Skeleton.ScaleX) * Mathf.Sign(direction.x);
        }
        #endregion
        
        #region Protected methods
        protected virtual void Die() {}
        
        protected float GetCurrentVelocity()
        {
	        return RigidBodyInstance.linearVelocity.magnitude;
        }
        #endregion
	}
}