// Attach to: Meteor Enemy prefab root
using UnityEngine;

namespace ShooterGame.Enemy
{
    public class MeteorEnemy : EnemyBase
    {
        protected override void Move()
        {
            transform.Translate(Vector3.down * CurrentSpeed * Time.deltaTime);
        }

        public override void TakeDamage(int dmg) { }
    }
}
