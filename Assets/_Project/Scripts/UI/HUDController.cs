using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiverRush.UI
{
    /// <summary>
    /// Controls the In-Game HUD elements: distance, coins, health, speedometer, and steering controls.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Status Displays")]
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI speedValueText;

        [Header("Top Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Steering Controls")]
        [SerializeField] private Button steerLeftButton;
        [SerializeField] private Button steerRightButton;

        [Header("Runtime Simulation State")]
        private float distance = 1250f;
        private int coins = 236;
        private int health = 3;
        private float currentSpeed = 128f;
        private bool isSimulating = false;

        private void Start()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(OnPauseClicked);

            if (steerLeftButton != null)
            {
                steerLeftButton.onClick.AddListener(() => OnSteer(-1f));
            }

            if (steerRightButton != null)
            {
                steerRightButton.onClick.AddListener(() => OnSteer(1f));
            }

            UpdateDisplay();
        }

        private void Update()
        {
            if (isSimulating && Time.timeScale > 0)
            {
                distance += currentSpeed * 0.28f * Time.deltaTime;
                UpdateDisplay();
            }
        }

        public void ResetHUD()
        {
            distance = 1250f;
            coins = 236;
            health = 3;
            currentSpeed = 128f;
            isSimulating = true;
            UpdateDisplay();
        }

        public void AddCoins(int amount)
        {
            coins += amount;
            UpdateDisplay();
        }

        public void TakeDamage(int damage)
        {
            health = Mathf.Max(0, health - damage);
            UpdateDisplay();
            if (health <= 0)
            {
                UIManager.Instance.ShowGameOver(Mathf.RoundToInt(distance), coins, true);
            }
        }

        private void OnSteer(float direction)
        {
            Debug.Log($"[HUD] Steering direction: {(direction < 0 ? "LEFT" : "RIGHT")}");
        }

        private void OnPauseClicked()
        {
            UIManager.Instance.ShowPauseMenu();
        }

        private void UpdateDisplay()
        {
            if (distanceText != null)
                distanceText.text = $"{Mathf.RoundToInt(distance):N0} m";

            if (coinsText != null)
                coinsText.text = $"{coins:N0}";

            if (healthText != null)
                healthText.text = $"{health}";

            if (speedValueText != null)
                speedValueText.text = $"{Mathf.RoundToInt(currentSpeed)}";
        }
    }
}
