// Attach to: StageStartPanel GameObject (Canvas child, Game scene — initially inactive)
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

namespace ShooterGame.UI
{
    public class StageStartPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text    _stageText;
      

      

     

        public void Show(int stage)
        {
            if (_stageText != null)
                _stageText.text = $"Stage {stage}";

            
            
            gameObject.SetActive(true);
            StartCoroutine(Hide_Co());
     

        }
        private IEnumerator Hide_Co()
        {
            yield return new WaitForSeconds(1.5f);
            Hide();
        }



        private void Hide() => gameObject.SetActive(false);

        private void OnDestroy()
        {
           
        }
    }
}
