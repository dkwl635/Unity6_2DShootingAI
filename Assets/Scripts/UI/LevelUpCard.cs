// Attach to: 각 카드 GameObject (LevelUpPanel의 자식)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Upgrade;

namespace ShooterGame.UI
{
    public class LevelUpCard : MonoBehaviour
    {
        [SerializeField] private Button    _button;
        [SerializeField] private Image     _iconImage;
        [SerializeField] private TMP_Text  _nameText;
        [SerializeField] private Transform _levelBoxContainer;

        [Header("Level Box Style")]
        [SerializeField] private Vector2 _boxSize        = new Vector2(18f, 18f);
        [SerializeField] private float   _boxSpacing     = 4f;
        [SerializeField] private Color   _activeColor    = new Color(1f, 0.85f, 0f, 1f);  // 노란색
        [SerializeField] private Color   _inactiveColor  = Color.white;

        public Button Button => _button;

        private readonly List<Image> _boxes = new List<Image>();

        public void Setup(UpgradeData data, int currentLevel)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite  = data.Icon;
                _iconImage.enabled = data.Icon != null;
            }

            if (_nameText != null)
                _nameText.text = data.Name;

            BuildBoxes(data.MaxLevel, currentLevel);
        }

        private void BuildBoxes(int maxLevel, int currentLevel)
        {
            foreach (var b in _boxes)
                if (b != null) Destroy(b.gameObject);
            _boxes.Clear();

            if (_levelBoxContainer == null || maxLevel <= 0) return;

            for (int i = 0; i < maxLevel; i++)
            {
                var go  = new GameObject($"LvBox_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_levelBoxContainer, false);
                var rt  = (RectTransform)go.transform;
                rt.sizeDelta = _boxSize;
                var img = go.GetComponent<Image>();
                img.color = i < currentLevel ? _activeColor : _inactiveColor;
                _boxes.Add(img);
            }
        }
    }
}
