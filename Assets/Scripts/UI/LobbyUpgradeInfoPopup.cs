// Attach to: LobbyUpgradeInfoPopup GameObject (Canvas child, Lobby scene)
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class LobbyUpgradeInfoPopup : MonoBehaviour
    {
        public static LobbyUpgradeInfoPopup Instance { get; private set; }

        [Header("Info")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text  _nameText;
        [SerializeField] private TMP_Text _descText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _statText;
        [SerializeField] private TMP_Text _costText;

        [Header("Button")]
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TMP_Text _upgradeButtonText;

        public LobbyUpgradeType CurrentType => _currentType;
        public bool             IsShowing   => _isShowing;

        private LobbyUpgradeType  _currentType;
        private bool              _isShowing;
        private readonly StringBuilder _sb = new StringBuilder(64);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            gameObject.SetActive(false);
            _upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnEnable()
        {
            if (LobbyUpgradeManager.Instance != null)
                LobbyUpgradeManager.Instance.OnUpgradeChanged += Refresh;
        }

        private void OnDisable()
        {
            if (LobbyUpgradeManager.Instance != null)
                LobbyUpgradeManager.Instance.OnUpgradeChanged -= Refresh;
        }

        public void Hide()
        {
            _isShowing = false;
            gameObject.SetActive(false);
        }

        public void Show(LobbyUpgradeType type)
        {
            _currentType = type;
            _isShowing   = true;
            gameObject.SetActive(true);
            Refresh();
        }

        private void Refresh()
        {
            if (!_isShowing) return;

            LobbyUpgradeData data = LobbyUpgradeManager.Instance.GetData(_currentType);
            int currentLevel      = SaveManager.Instance.GetUpgradeLevel(_currentType);
            int coins             = SaveManager.Instance.TotalCoins;
            bool isMax            = currentLevel >= data.MaxLevel;

            if (_iconImage != null)
            {
                _iconImage.sprite  = data.Icon;
                _iconImage.enabled = data.Icon != null;
            }

            _nameText.text  = data.DisplayName;
            _descText.text  = data.Description;
            _levelText.text = BuildLevelDots(currentLevel, data.MaxLevel);

            // 스탯 표시
            _sb.Clear();
            float current = data.GetTotalGain(currentLevel);
            if (isMax)
            {
                _sb.Append("현재: +").Append(current).Append(data.StatUnit).Append(" (MAX)");
            }
            else
            {
                float next = data.GetTotalGain(currentLevel + 1);
                _sb.Append("현재: +").Append(current).Append(data.StatUnit)
                   .Append("\n다음: +").Append(next).Append(data.StatUnit);
            }
            _statText.text = _sb.ToString();

            // 버튼
            if (isMax)
            {
                _costText.text              = "";
                _upgradeButtonText.text     = "MAX";
                _upgradeButton.interactable = false;
            }
            else
            {
                int cost = data.GetCostForLevel(currentLevel);
                _sb.Clear();
                _sb.Append(cost).Append(" 코인");
                _costText.text              = _sb.ToString();
                _upgradeButtonText.text     = "업그레이드";
                _upgradeButton.interactable = coins >= cost;
            }
        }

        private string BuildLevelDots(int current, int max)
        {
            _sb.Clear();
            _sb.Append("Lv.");

            if(current == max)
            {
                _sb.Append("Max");
            }
            else
            {
                _sb.Append(current.ToString());
            }
          
            return _sb.ToString();
        }

        private void OnUpgradeClicked()
        {
            LobbyUpgradeManager.Instance?.TryPurchase(_currentType);
        }

        private void OnDestroy()
        {
            if (_upgradeButton != null)
                _upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            if (Instance == this) Instance = null;
        }
    }
}
