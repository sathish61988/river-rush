using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiverRush.UI
{
    /// <summary>
    /// Controls Game Over screen modal: displays stats, best score badge, Home, and Play Again buttons.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("Stat Displays")]
        [SerializeField] private TextMeshProUGUI distanceValueText;
        [SerializeField] private TextMeshProUGUI coinsValueText;
        [SerializeField] private GameObject newBestScoreGroup;

        [Header("Buttons")]
        [SerializeField] private Button homeButton;
        [SerializeField] private Button playAgainButton;

        private void Start()
        {
            if (homeButton != null)
                homeButton.onClick.AddListener(OnHomeClicked);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        public void SetResults(int distance, int coins, bool newBest = true)
        {
            if (distanceValueText != null)
                distanceValueText.text = $"{distance:N0} m";

            if (coinsValueText != null)
                coinsValueText.text = $"{coins:N0}";

            if (newBestScoreGroup != null)
                newBestScoreGroup.SetActive(newBest);
        }

        private void OnHomeClicked()
        {
            UIManager.Instance.ShowMainMenu();
        }

        private void OnPlayAgainClicked()
        {
            UIManager.Instance.RestartGame();
        }
    }
}
