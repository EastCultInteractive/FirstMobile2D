using System.Collections;
using UnityEngine;

namespace Resources.Scripts.Entity.SpellMode.Skills
{
    /// <summary>
    /// ----------------------------------------------------------------------------
    /// Base class for skills with cooldown logic.
    /// ----------------------------------------------------------------------------
    /// </summary>
    public abstract class SkillBase : MonoBehaviour
    {
        [Header("Cooldown Settings")]
        [Tooltip("Cooldown duration in seconds.")]
        public float cooldownDuration = 5f;

        // Protected fields for managing cooldown state.
        protected bool IsOnCooldown;
        protected float CooldownTimer;

        /// <summary>
        /// Attempts to activate the skill if the cooldown has finished.
        /// </summary>
        public void TryActivateSkill()
        {
            if (IsOnCooldown)
            {
                Debug.Log("Skill is on cooldown.");
                return;
            }

            ActivateSkill();
            StartCoroutine(CooldownRoutine());
        }

        /// <summary>
        /// Abstract method to execute the skill's effect.
        /// </summary>
        protected abstract void ActivateSkill();

        /// <summary>
        /// Coroutine that handles the cooldown countdown.
        /// </summary>
        private IEnumerator CooldownRoutine()
        {
            IsOnCooldown = true;
            CooldownTimer = cooldownDuration;
            while (CooldownTimer > 0f)
            {
                CooldownTimer -= Time.deltaTime;
                yield return null;
            }
            IsOnCooldown = false;
            CooldownTimer = 0f;
        }

        /// <summary>
        /// Indicates whether the skill is currently on cooldown.
        /// </summary>
        public bool GetIsOnCooldown() => IsOnCooldown;

        /// <summary>
        /// Returns the remaining cooldown time.
        /// </summary>
        public float GetCooldownTimer() => CooldownTimer;
    }
}
