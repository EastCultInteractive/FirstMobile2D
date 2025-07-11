using System;
using Resources.Scripts.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Resources.Scripts.Entity.Fairy
{
    public class FairyController : EntityController
    {
        [Header("MoveDirection Settings")]
        [SerializeField, Range(1f, 10f)] private float changeDirectionDelay = 5f;
        
        private Timer _timer;

        private void Start()
        {
            _timer = new Timer(3f, 7f);
            _timer.OnTimerFinished += UpdateTargetPosition;
        }

        private void Update()
        {
            _timer.Tick(Time.deltaTime);
        }
        
        private void UpdateTargetPosition(object sender, EventArgs e)
        {
            MoveDirection = Random.insideUnitCircle;
        }
    }
}
