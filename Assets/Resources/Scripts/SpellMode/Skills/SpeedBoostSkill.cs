using Resources.Scripts.Entity.Player;
using UnityEngine;

namespace Resources.Scripts.SpellMode.Skills
{
    public class SpeedBoostSkill : SkillBase
    {
        [Header("Speed Boost Settings")]
        public float boostMultiplier = 1.5f;
        public float boostDuration = 5f;

        protected override void ActivateSkill()
        {
            var player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            
            player.ApplySpeedBoost(boostMultiplier, boostDuration);
        }
    }
}