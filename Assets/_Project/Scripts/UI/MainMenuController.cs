using UnityEngine;
using UnityEngine.UI;

namespace RiverRush.UI
{
    /// <summary>
    /// Handles Main Menu buttons and user actions.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main Navigation Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button boatsButton;
        [SerializeField] private Button mapsButton;
        [SerializeField] private Button storeButton;

        [Header("Side Utility Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button moreGamesButton;

        private void Start()
        {
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);

            if (boatsButton != null)
                boatsButton.onClick.AddListener(OnBoatsClicked);

            if (mapsButton != null)
                mapsButton.onClick.AddListener(OnMapsClicked);

            if (storeButton != null)
                storeButton.onClick.AddListener(OnStoreClicked);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);

            if (leaderboardButton != null)
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);

            if (moreGamesButton != null)
                moreGamesButton.onClick.AddListener(OnMoreGamesClicked);
        }

        private void OnPlayClicked()
        {
            UIManager.Instance.StartGame();
        }

        private void OnBoatsClicked()
        {
            UIManager.Instance.ShowBoatSelection();
        }

        private void OnMapsClicked()
        {
            UIManager.Instance.ShowMapSelection();
        }

        private void OnStoreClicked()
        {
            Debug.Log("[MainMenu] Store button clicked (Placeholder)");
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings button clicked (Placeholder)");
        }

        private void OnLeaderboardClicked()
        {
            Debug.Log("[MainMenu] Leaderboard button clicked (Placeholder)");
        }

        private void OnMoreGamesClicked()
        {
            Debug.Log("[MainMenu] More Games button clicked (Placeholder)");
        }
    }
}
