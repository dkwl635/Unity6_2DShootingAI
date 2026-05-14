// Attach to: StageClearPanel (child of HUD Canvas, initially inactive)
using UnityEngine;
using TMPro;

namespace ShooterGame.UI
{
    public class StageClearUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text stageLabel;

        public void ShowStageClear(int stageNum)
        {
            if (stageLabel != null)
                stageLabel.text = $"Stage {stageNum} Clear!";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
