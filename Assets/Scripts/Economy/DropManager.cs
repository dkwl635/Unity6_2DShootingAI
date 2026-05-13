// Attach to: DropManager GameObject (Game scene only — no DontDestroyOnLoad)
// Holds ObjectPool<CoinDrop> and ObjectPool<PowerDrop>.
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
        [SerializeField] private PowerDrop  powerDropPrefab;

        private ObjectPool<CoinDrop> _coinPool;
        private ObjectPool<PowerDrop>  _powerPool;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _coinPool = new ObjectPool<CoinDrop>(coinDropPrefab, Constants.POOL_SIZE_COIN, transform);
            _powerPool  = new ObjectPool<PowerDrop>(powerDropPrefab,   Constants.POOL_SIZE_POWER,  transform);
        }

        private void OnEnable()  => EnemyBase.OnEnemyDied += HandleEnemyDied;
        private void OnDisable() => EnemyBase.OnEnemyDied -= HandleEnemyDied;

        private void HandleEnemyDied(Vector3 pos, int coin, int power)
        {
            if (coin  > 0) SpawnCoin(pos, coin);
            if (power > 0) SpawnPower(pos, power);
        }

        private void SpawnCoin(Vector3 pos, int value)
        {
            if (_coinPool == null) return;
            CoinDrop drop = _coinPool.Get();
            drop.SetValue(value);
            drop.Initialize(pos + RandomOffset(), () => _coinPool.Release(drop));
        }

        private void SpawnPower(Vector3 pos, int value)
        {
            if (_powerPool == null) return;
            PowerDrop drop = _powerPool.Get();
            drop.SetValue(value);
            drop.Initialize(pos + RandomOffset(), () => _powerPool.Release(drop));
        }


        private static Vector3 RandomOffset(float radius = 0.3f)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(0f, radius);
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * dist;
        }

        private void OnDestroy()
        {
            // Return all active drops to pool before scene unload
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                    child.gameObject.SetActive(false);
            }
            if (Instance == this) Instance = null;
        }
    }
}
