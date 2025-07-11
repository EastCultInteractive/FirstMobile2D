using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Resources.Scripts.Utils
{
    public class Timer
    {
        private readonly float _min;
        private readonly float _max;
        private float _timer;

        public event EventHandler OnTimerFinished;

        public Timer(float min, float max)
        {
            _min = min;
            _max = max;
            _timer = Random.Range(min, max);
        }

        public void Tick(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;
            
            OnTimerFinished?.Invoke(this, EventArgs.Empty);
            _timer = Random.Range(_min, _max);
        }
    }
}