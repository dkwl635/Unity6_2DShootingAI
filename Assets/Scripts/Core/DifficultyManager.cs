// Attach to: DifficultyManager GameObject (Game scene only — no DontDestroyOnLoad)
using UnityEngine;

namespace ShooterGame.Core
{
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [SerializeField] private float baseInterval = 2.0f;
        [SerializeField] private float minInterval  = 0.4f;
        [SerializeField] private float k            = 0.02f;

        [SerializeField] private float speedGain    = 0.01f;
        [SerializeField] private float maxSpeedMult = 3.0f;

        [SerializeField] private float hpGain       = 0.005f;
        [SerializeField] private float maxHpMult    = 5.0f;

        public float SpawnInterval        { get; private set; }
        public float EnemySpeedMultiplier { get; private set; }
        public float EnemyHpMultiplier    { get; private set; }

        private float _stageBaseMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            SpawnInterval        = baseInterval;
            EnemySpeedMultiplier = 1f;
            EnemyHpMultiplier    = 1f;
        }

        private void Update()
        {
            if (InGameManager.Instance == null || !InGameManager.Instance.IsGameRunning) return;

            float t = InGameManager.Instance.ElapsedTime;

            SpawnInterval        = Mathf.Clamp(baseInterval * Mathf.Exp(-k * t), minInterval, baseInterval);
            EnemySpeedMultiplier = Mathf.Clamp(1f + _stageBaseMultiplier + speedGain * t, 1f, maxSpeedMult);
            EnemyHpMultiplier    = Mathf.Clamp(1f + _stageBaseMultiplier + hpGain * t,    1f, maxHpMult);
        }

        // Called by StageManager on each stage transition
        // Stage 2 → +0.3 base; each additional full loop → +0.2 on top
        public void SetStage(int stage, int loopCount)
        {
            _stageBaseMultiplier = (stage - 1) * 0.3f + loopCount * 0.2f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
