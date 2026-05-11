// Attach to: LinearDescent Enemy Prefab
using UnityEngine;

namespace ShooterGame.Enemy
{
    public class LinearDescentEnemy : EnemyBase
    {
        protected override void Move()
        {
            transform.Translate(Vector3.down * CurrentSpeed * Time.deltaTime);
        }
    }
}
