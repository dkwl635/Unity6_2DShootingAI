// Attach to: DifficultyManager GameObject (Game scene only — no DontDestroyOnLoad)
using System;
using UnityEngine;

namespace ShooterGame.Core
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        // ── Spawn interval: exponential decay ───────────────────
        [SerializeField] private float baseInterval = 2.0f;
        [SerializeField] private float minInterval  = 0.4f;
        [SerializeField] private float k            = 0.02f;

        // ── Speed multiplier: linear ramp ────────────────────────
        [SerializeField] private float speedGain    = 0.01f;
        [SerializeField] private float maxSpeedMult = 3.0f;

        // ── HP multiplier: linear ramp ───────────────────────────
        [SerializeField] private float hpGain       = 0.005f;
        [SerializeField] private float maxHpMult    = 5.0f;

        // ── Mini-boss timer ──────────────────────────────────────
        [SerializeField] private float bossInterval = 120f;

        public event Action OnMiniBossSpawn;
        private float _bossTimer;

        public float SpawnInterval        { get; private set; }
        public float EnemySpeedMultiplier { get; private set; }
        public float EnemyHpMultiplier    { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SpawnInterval        = baseInterval;
            EnemySpeedMultiplier = 1f;
            EnemyHpMultiplier    = 1f;
        }

        private void Start()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart += ResetBossTimer;
        }

        private void ResetBossTimer()
        {
            _bossTimer = 0f;
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

            float t = InGameManager.Instance.ElapsedTime;

            SpawnInterval        = Mathf.Clamp(baseInterval * Mathf.Exp(-k * t), minInterval, baseInterval);
            EnemySpeedMultiplier = Mathf.Clamp(1f + speedGain * t, 1f, maxSpeedMult);
            EnemyHpMultiplier    = Mathf.Clamp(1f + hpGain * t,    1f, maxHpMult);

            _bossTimer += Time.deltaTime;
            if (_bossTimer >= bossInterval)
            {
                _bossTimer = 0f;
                OnMiniBossSpawn?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (InGameManager.Instance != null)
                InGameManager.Instance.OnGameStart -= ResetBossTimer;
            if (Instance == this) Instance = null;
        }
    }
}
