// Attach to: MiniBoss Enemy prefab root
using UnityEngine;
using ShooterGame.Utils;

namespace ShooterGame.Enemy
{
    public class MiniBossEnemy : EnemyBase
    {
        [SerializeField] private float sweepFrequency = 1.5f;
        [SerializeField] private float sweepAmplitude = 3f;

        private bool _reachedCenter;

        protected override void OnEnable()
        {
            base.OnEnable();
            _reachedCenter = false;
        }

        protected override void Move()
        {
            if (!_reachedCenter)
            {
                Vector3 target     = new Vector3(transform.position.x, Constants.BOSS_CENTER_Y, 0f);
                transform.position = Vector3.MoveTowards(transform.position, target, CurrentSpeed * Time.deltaTime);

                if (Mathf.Abs(transform.position.y - Constants.BOSS_CENTER_Y) < 0.05f)
                    _reachedCenter = true;
            }
            else
            {
                float newX         = Mathf.Sin(Time.time * sweepFrequency) * sweepAmplitude;
                transform.position = new Vector3(newX, Constants.BOSS_CENTER_Y, 0f);
            }
        }
    }
}
