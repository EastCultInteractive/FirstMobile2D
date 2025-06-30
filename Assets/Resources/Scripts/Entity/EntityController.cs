using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace Resources.Scripts.Entity
{
	public class EntityController : MonoBehaviour
	{
		[SerializeField] private Color damageFlashColor = Color.red;	
		[SerializeField, Range(0.1f, 1f)] private float damageFlashDuration = 0.3f;
		
		protected Rigidbody2D RigidBodyInstance;
		private Skeleton skeleton;
		
		protected SkeletonAnimation SkeletonAnimation;
		protected EntityStats Stats;

		public bool IsDead => Stats.Health <= 0;

		private void Awake()
		{
			InitEntity();
			InitAnimations();
		}

		#region Init

		private void InitEntity()
		{
			RigidBodyInstance = GetComponent<Rigidbody2D>();
			Stats = GetComponent<EntityStats>();
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

            Stats.Health -= from.Stats.Damage;
            ApplyDamageFlash();
            ApplyPush((transform.position - from.transform.position) * from.Stats.PushDistance);
            
            if (Stats.Health <= 0) Die();
        }
        
        #region Damage and Evasion
        private void ApplyDamageFlash() => StartCoroutine(DamageFlash());
        private IEnumerator DamageFlash()
        {
            skeleton.SetColor(damageFlashColor);
            yield return new WaitForSeconds(damageFlashDuration);
            skeleton.SetColor(Color.white);
        }
        #endregion

        #region Other Effects
		public void ApplyPush(Vector2 force) => RigidBodyInstance.AddForce(force, ForceMode2D.Impulse);
        public void ApplyDash(Transform from) => transform.Translate((transform.position - from.position).normalized);
        public void ApplySlow(float factor, float duration) => StartCoroutine(SlowEffect(factor, duration));
        public void ApplyStun(float duration) => StartCoroutine(SlowEffect(0f, duration));
        
        private IEnumerator SlowEffect(float factor, float duration)
        {
            var baseSlow = Stats.SlowMultiplier;
            Stats.SlowMultiplier = factor;
            yield return new WaitForSeconds(duration);
            Stats.SlowMultiplier = baseSlow;
        }
        #endregion
        
        #region Protected methods
        protected virtual void Die() {}
        #endregion
	}
}