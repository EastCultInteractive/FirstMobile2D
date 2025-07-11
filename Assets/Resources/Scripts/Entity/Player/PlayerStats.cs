using UnityEngine;

namespace Resources.Scripts.Entity.Player
{
    public class PlayerStats : EntityStats
    {
        [Header("Mana Settings")]
        [SerializeField] private float maxMana = 100f;
        [SerializeField] private float currentMana;
        [SerializeField] private float manaRegenRate = 10f;
        [SerializeField, Range(1f, 5f)] private float manaRegenDelay;

        [Header("Evasion Settings")]
        [SerializeField, Range(0f, 100f)] private float baseEvasionChance = 10f;
        [SerializeField] private float evasionCooldown = 1f;

        private float _manaRegenTimer;

        private void Start()
        {
            _manaRegenTimer = manaRegenDelay;
        }
        
        private void Update()
        {
            ManaRegenTimer();
        }

        private void ManaRegenTimer()
        {
            if (_manaRegenTimer > 0f)
            {
                _manaRegenTimer -= Time.deltaTime;
                return;
            }
            
            currentMana = Mathf.Min(currentMana + manaRegenRate * Time.deltaTime, maxMana);
            _manaRegenTimer = manaRegenDelay;
        }

        public bool UseMana(float amount)
        {
            currentMana = Mathf.Max(currentMana - amount, 0f);
            return true;
        }


        public float CurrentMana => currentMana;
        public float MaxMana => maxMana;
    }
}
