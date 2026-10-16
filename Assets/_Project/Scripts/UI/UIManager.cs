using System;
using UnityEngine;

namespace RiverRush.UI
{
    public enum ScreenState
    {
        MainMenu,
        BoatSelection,
        MapSelection,
        InGameHUD,
        Paused,
        GameOver
    }

    /// <summary>
    /// Central UI orchestrator managing screens, popups, and transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject boatSelectionPanel;
        [SerializeField] private GameObject mapSelectionPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseModal;
        [SerializeField] private GameObject gameOverModal;

        [Header("Screen Controllers")]
        [SerializeField] private MainMenuController mainMenuController;
        [SerializeField] private BoatSelectionController boatSelectionController;
        [SerializeField] private EnvironmentSelectionController mapSelectionController;
        [SerializeField] private HUDController hudController;
        [SerializeField] private PauseMenuController pauseMenuController;
        [SerializeField] private GameOverController gameOverController;

        [Header("Environment Controllers")]
        [SerializeField] private GameObject cinematicBackground;
        [SerializeField] private GameObject inGameRiverEnvironment;

        public ScreenState CurrentState { get; private set; } = ScreenState.MainMenu;

        public event Action<ScreenState> OnScreenChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            ShowMainMenu();
        }

        public void ShowMainMenu()
        {
            CurrentState = ScreenState.MainMenu;
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(boatSelectionPanel, false);
            SetPanelActive(mapSelectionPanel, false);
            SetPanelActive(hudPanel, false);
            SetPanelActive(pauseModal, false);
            SetPanelActive(gameOverModal, false);

            if (cinematicBackground != null) cinematicBackground.SetActive(true);
            if (inGameRiverEnvironment != null) inGameRiverEnvironment.SetActive(false);

            Time.timeScale = 1f;
            OnScreenChanged?.Invoke(CurrentState);
        }

        public void ShowBoatSelection()
        {
            CurrentState = ScreenState.BoatSelection;
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(boatSelectionPanel, true);
            SetPanelActive(mapSelectionPanel, false);
            SetPanelActive(hudPanel, false);
            SetPanelActive(pauseModal, false);
            SetPanelActive(gameOverModal, false);

            if (boatSelectionController != null)
            {
                boatSelectionController.RefreshUI();
            }

            OnScreenChanged?.Invoke(CurrentState);
        }

        public void ShowMapSelection()
        {
            CurrentState = ScreenState.MapSelection;
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(boatSelectionPanel, false);
            SetPanelActive(mapSelectionPanel, true);
            SetPanelActive(hudPanel, false);
            SetPanelActive(pauseModal, false);
            SetPanelActive(gameOverModal, false);

            if (mapSelectionController != null)
            {
                mapSelectionController.RefreshUI();
            }

            OnScreenChanged?.Invoke(CurrentState);
        }

        public void StartGame()
        {
            CurrentState = ScreenState.InGameHUD;
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(boatSelectionPanel, false);
            SetPanelActive(mapSelectionPanel, false);
            SetPanelActive(hudPanel, true);
            SetPanelActive(pauseModal, false);
            SetPanelActive(gameOverModal, false);

            if (cinematicBackground != null) cinematicBackground.SetActive(false);
            if (inGameRiverEnvironment != null) inGameRiverEnvironment.SetActive(true);

            if (hudController != null)
            {
                hudController.ResetHUD();
            }

            Time.timeScale = 1f;
            OnScreenChanged?.Invoke(CurrentState);
        }

        public void ShowPauseMenu()
        {
            CurrentState = ScreenState.Paused;
            SetPanelActive(pauseModal, true);
            Time.timeScale = 0f;
            OnScreenChanged?.Invoke(CurrentState);
        }

        public void ResumeGame()
        {
            CurrentState = ScreenState.InGameHUD;
            SetPanelActive(pauseModal, false);
            Time.timeScale = 1f;
            OnScreenChanged?.Invoke(CurrentState);
        }

        public void RestartGame()
        {
            SetPanelActive(pauseModal, false);
            SetPanelActive(gameOverModal, false);
            StartGame();
        }

        public void ShowGameOver(int distance = 1250, int coins = 236, bool newBest = true)
        {
            CurrentState = ScreenState.GameOver;
            SetPanelActive(gameOverModal, true);

            if (gameOverController != null)
            {
                gameOverController.SetResults(distance, coins, newBest);
            }

            Time.timeScale = 0f;
            OnScreenChanged?.Invoke(CurrentState);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}
