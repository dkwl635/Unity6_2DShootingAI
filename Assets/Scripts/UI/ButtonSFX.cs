// Attach to: any Button GameObject
using UnityEngine;
using UnityEngine.UI;
using ShooterGame.Core;

namespace ShooterGame.UI
{
    [RequireComponent(typeof(Button))]
    public class ButtonSFX : MonoBehaviour
    {
        [SerializeField] private SfxType _sfxType = SfxType.ButtonClick;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Play);
        }

        private void OnDestroy()
        {
            GetComponent<Button>().onClick.RemoveListener(Play);
        }

        public void Play()
        {
            AudioManager.Instance?.PlaySFX(_sfxType);
        }
    }
}
