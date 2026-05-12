// Attach to: DropManager GameObject (Game scene only — no DontDestroyOnLoad)
// Holds ObjectPool<CoinDrop> and ObjectPool<ExpDrop>.
// Subscribes to EnemyBase.OnEnemyDied and spawns drops at death position.
using UnityEngine;
using ShooterGame.Enemy;
using ShooterGame.Utils;

namespace ShooterGame.Economy
{
    public class DropManager : MonoBehaviour
    {
        public static DropManager Instance { get; private set; }

        [SerializeField] private CoinDrop coinDropPrefab;
        [SerializeField] private ExpDrop  expDropPrefab;

        private ObjectPool<CoinDrop> _coinPool;
        private ObjectPool<ExpDrop>  _expPool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _coinPool = new ObjectPool<CoinDrop>(coinDropPrefab, Constants.POOL_SIZE_COIN, transform);
            _expPool  = new ObjectPool<ExpDrop>(expDropPrefab,   Constants.POOL_SIZE_EXP,  transform);
        }

        private void OnEnable()  => EnemyBase.OnEnemyDied += HandleEnemyDied;
        private void OnDisable() => EnemyBase.OnEnemyDied -= HandleEnemyDied;

        private void HandleEnemyDied(Vector3 pos, int coin, int exp)
        {
            if (coin > 0) SpawnCoin(pos, coin);
            if (exp  > 0) SpawnExp(pos, exp);
        }

        private void SpawnCoin(Vector3 pos, int value)
        {
            CoinDrop drop = _coinPool.Get();
            drop.SetValue(value);
            drop.Initialize(pos, () => _coinPool.Release(drop));
        }

        private void SpawnExp(Vector3 pos, int value)
        {
            ExpDrop drop = _expPool.Get();
            drop.SetValue(value);
            drop.Initialize(pos, () => _expPool.Release(drop));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
