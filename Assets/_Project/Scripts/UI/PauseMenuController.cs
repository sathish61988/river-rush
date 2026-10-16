using UnityEngine;
using UnityEngine.UI;

namespace RiverRush.UI
{
    /// <summary>
    /// Controls Pause Menu modal: Resume, Restart, Main Menu, and audio toggles.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Modal Actions")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button closeButton;

        [Header("Audio & Settings Toggles")]
        [SerializeField] private Button musicToggleButton;
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private Button settingsButton;

        private bool isMusicOn = true;
        private bool isSoundOn = true;

        private void Start()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);

            if (closeButton != null)
                closeButton.onClick.AddListener(OnResumeClicked);

            if (musicToggleButton != null)
                musicToggleButton.onClick.AddListener(ToggleMusic);

            if (soundToggleButton != null)
                soundToggleButton.onClick.AddListener(ToggleSound);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnResumeClicked()
        {
            UIManager.Instance.ResumeGame();
        }

        private void OnRestartClicked()
        {
            UIManager.Instance.RestartGame();
        }

        private void OnMainMenuClicked()
        {
            UIManager.Instance.ShowMainMenu();
        }

        private void ToggleMusic()
        {
            isMusicOn = !isMusicOn;
            Debug.Log($"[PauseMenu] Music toggled: {(isMusicOn ? "ON" : "OFF")}");
        }

        private void ToggleSound()
        {
            isSoundOn = !isSoundOn;
            Debug.Log($"[PauseMenu] Sound SFX toggled: {(isSoundOn ? "ON" : "OFF")}");
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[PauseMenu] Settings clicked (Placeholder)");
        }
    }
}
