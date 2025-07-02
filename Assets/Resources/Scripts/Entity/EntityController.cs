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
		
		private Skeleton skeleton;
		private EntityStats stats;
		
		protected SkeletonAnimation SkeletonAnimation;
		protected Rigidbody2D RigidBodyInstance;
		
		private float currentAnimationSpeed;
		private const float AnimationSmoothing = 5f;
		private TrackEntry currentTrack;

		public bool IsDead => stats.Health <= 0;

		private void Awake()
		{
			InitEntity();
			InitAnimations();
		}

		#region Init

		private void InitEntity()
		{
			RigidBodyInstance = GetComponent<Rigidbody2D>();
			stats = GetComponent<EntityStats>();
		}
		
		private void InitAnimations()
		{
            var spineChild = transform.Find("SpineVisual");
            SkeletonAnimation = spineChild.GetComponent<SkeletonAnimation>();
            skeleton = SkeletonAnimation.Skeleton;
		}
		#endregion

        public void TakeDamage(EntityController from)
        {
            // if (isImmortal || isRolling || IsDead || drawingManager.IsDrawing || playerStats.TryEvade(transform.position))
            //     return;

            stats.Health -= from.stats.Damage;
            ApplyDamageFlash();
            ApplyPush((transform.position - from.transform.position) * from.stats.PushDistance);
            
            if (stats.Health <= 0) Die();
        }
        
        #region Other Effects
		public void ApplyPush(Vector2 force) => RigidBodyInstance.AddForce(force, ForceMode2D.Impulse);
        public void ApplyDash(Transform from) => transform.Translate((transform.position - from.position).normalized);
        public void ApplySlow(float factor, float duration) => StartCoroutine(MoveSpeedEffect(factor, duration));
        public void ApplySpeedBoost(float factor, float duration) => StartCoroutine(MoveSpeedEffect(factor, duration));
        public void ApplyStun(float duration) => StartCoroutine(MoveSpeedEffect(0f, duration));
        private void ApplyDamageFlash() => StartCoroutine(DamageFlash());
        
        private IEnumerator MoveSpeedEffect(float factor, float duration)
        {
            var baseSlow = stats.SlowMultiplier;
            stats.SlowMultiplier = factor;
            yield return new WaitForSeconds(duration);
            stats.SlowMultiplier = baseSlow;
        }
        

        private IEnumerator DamageFlash()
        {
            skeleton.SetColor(damageFlashColor);
            yield return new WaitForSeconds(damageFlashDuration);
            skeleton.SetColor(Color.white);
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
	        currentTrack = SkeletonAnimation.state.GetCurrent(0);
	        return currentTrack == null ? default : GetAnimationByName(animations, currentTrack.Animation.Name);
        }

        protected T GetAnimationByName<T>(SerializedDictionary<T, string> animations, string animName) where T : IConvertible
        {
            return animations.FirstOrDefault(pair => pair.Value == animName).Key;
        }

        
        protected void UpdateAnimationSpeed(float velocity)
        {
	        currentAnimationSpeed = Mathf.Lerp(currentAnimationSpeed, velocity, Time.deltaTime * AnimationSmoothing);
    
	        if (currentTrack != null)
	        {
		        currentTrack.TimeScale = currentAnimationSpeed;
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
        #endregion
	}
}