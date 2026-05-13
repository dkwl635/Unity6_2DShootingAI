// Attach to: LobbyUpgradePanel GameObject
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Meta;

namespace ShooterGame.UI
{
    public class LobbyUpgradePanel : MonoBehaviour
    {
        [SerializeField] private Text               _coinText;
        // 슬롯 배열: 인덱스 0=MaxHp, 1=Damage, 2=AttackSpeed, 3=MagnetRange
        [SerializeField] private LobbyUpgradeSlot[] _slots;

        private LobbyUpgradeData[] _datas;

        private void Start()
        {
            _datas = new LobbyUpgradeData[4];
            for (int i = 0; i < 4; i++)
            {
                var type = (LobbyUpgradeType)i;
                _datas[i] = LobbyUpgradeManager.Instance.GetData(type);
                _slots[i].Initialize(type);
            }

            LobbyUpgradeManager.Instance.OnUpgradeChanged += RefreshAll;
            RefreshAll();
        }

        private void RefreshAll()
        {
            int coins      = SaveManager.Instance.TotalCoins;
            _coinText.text = "보유 코인: " + coins;

            for (int i = 0; i < 4; i++)
            {
                int level = SaveManager.Instance.GetUpgradeLevel((LobbyUpgradeType)i);
                _slots[i].Render(_datas[i], level, coins);
            }
        }

        private void OnDestroy()
        {
            if (LobbyUpgradeManager.Instance != null)
                LobbyUpgradeManager.Instance.OnUpgradeChanged -= RefreshAll;
        }
    }
}
