// Attach to: 각 카드 GameObject (LevelUpPanel의 자식)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpCard : MonoBehaviour
    {
        [SerializeField] private Button   _button;
        [SerializeField] private Image    _iconImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;

        public Button Button => _button;

        public void Setup(UpgradeData data, int currentLevel)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = data.Icon;
                _iconImage.enabled = data.Icon != null;
            }

            if (_nameText != null)
                _nameText.text = data.Name;

            if (_levelText != null)
                _levelText.text = BuildLevelLabel(data.MaxLevel, currentLevel);
        }

        private string BuildLevelLabel(int maxLevel, int currentLevel)
        {
            if (maxLevel <= 0) return string.Empty;

            string next = currentLevel + 1 >= maxLevel ? "MAX" : $"Lv{currentLevel + 1}";
            string from = currentLevel == 0 ? "Lv0" : $"Lv{currentLevel}";
            return $"{from} → {next}";
        }
    }
}
