using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiverRush.UI
{
    [System.Serializable]
    public class BoatData
    {
        public string boatName;
        public int speed;      // 1 to 5
        public int handling;   // 1 to 5
        public int durability; // 1 to 5
        public bool isUnlocked;
        public int unlockCost;
        public Sprite thumbnail;
        public Color hullColor;
    }

    /// <summary>
    /// Controls the Boat Selection screen: carousel, 3D preview, stats, thumbnails, and selection.
    /// </summary>
    public class BoatSelectionController : MonoBehaviour
    {
        [Header("Header Elements")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Boat Info")]
        [SerializeField] private TextMeshProUGUI boatNameText;
        [SerializeField] private Button selectButton;
        [SerializeField] private TextMeshProUGUI selectButtonText;

        [Header("Carousel Controls")]
        [SerializeField] private Button prevBoatButton;
        [SerializeField] private Button nextBoatButton;

        [Header("Stat Bar Segments")]
        [SerializeField] private List<Image> speedSegments = new List<Image>();
        [SerializeField] private List<Image> handlingSegments = new List<Image>();
        [SerializeField] private List<Image> durabilitySegments = new List<Image>();

        [Header("Thumbnail Buttons")]
        [SerializeField] private List<Button> boatThumbnails = new List<Button>();
        [SerializeField] private RectTransform selectionBorder;

        [Header("Boat Preview 3D / Target")]
        [SerializeField] private Transform boatPreviewAnchor;

        [Header("Boat Database")]
        [SerializeField] private List<BoatData> boats = new List<BoatData>();

        private int currentBoatIndex = 0;
        private int selectedBoatIndex = 0;
        private int userCoins = 12350;

        private readonly Color activeSegmentColor = new Color(0.35f, 0.88f, 0.18f, 1f); // Vibrant Lime
        private readonly Color inactiveSegmentColor = new Color(0.12f, 0.18f, 0.25f, 0.7f); // Dark Slate

        private void Awake()
        {
            InitializeDefaultBoats();
        }

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);

            if (prevBoatButton != null)
                prevBoatButton.onClick.AddListener(OnPrevBoatClicked);

            if (nextBoatButton != null)
                nextBoatButton.onClick.AddListener(OnNextBoatClicked);

            for (int i = 0; i < boatThumbnails.Count; i++)
            {
                int index = i;
                boatThumbnails[i].onClick.AddListener(() => SelectBoatByIndex(index));
            }

            RefreshUI();
        }

        private void InitializeDefaultBoats()
        {
            if (boats.Count == 0)
            {
                boats.Add(new BoatData { boatName = "SPEEDSTER", speed = 3, handling = 4, durability = 3, isUnlocked = true, unlockCost = 0, hullColor = new Color(0.85f, 0.15f, 0.15f) });
                boats.Add(new BoatData { boatName = "HYDRO WAVE", speed = 4, handling = 3, durability = 3, isUnlocked = true, unlockCost = 0, hullColor = new Color(0.15f, 0.5f, 0.9f) });
                boats.Add(new BoatData { boatName = "THUNDERBOLT", speed = 5, handling = 2, durability = 4, isUnlocked = true, unlockCost = 0, hullColor = new Color(0.95f, 0.75f, 0.1f) });
                boats.Add(new BoatData { boatName = "VIPER JET", speed = 4, handling = 5, durability = 2, isUnlocked = true, unlockCost = 0, hullColor = new Color(0.2f, 0.8f, 0.25f) });
                boats.Add(new BoatData { boatName = "PHANTOM X", speed = 5, handling = 5, durability = 5, isUnlocked = false, unlockCost = 25000, hullColor = new Color(0.15f, 0.15f, 0.18f) });
            }
        }

        public void RefreshUI()
        {
            if (coinsText != null)
                coinsText.text = $"{userCoins:N0}";

            if (currentBoatIndex < 0 || currentBoatIndex >= boats.Count) return;

            BoatData current = boats[currentBoatIndex];

            if (boatNameText != null)
                boatNameText.text = current.boatName;

            UpdateSegments(speedSegments, current.speed);
            UpdateSegments(handlingSegments, current.handling);
            UpdateSegments(durabilitySegments, current.durability);

            if (selectButtonText != null)
            {
                if (!current.isUnlocked)
                {
                    selectButtonText.text = $"UNLOCK ({current.unlockCost:N0})";
                    selectButton.interactable = userCoins >= current.unlockCost;
                }
                else if (currentBoatIndex == selectedBoatIndex)
                {
                    selectButtonText.text = "SELECTED";
                    selectButton.interactable = false;
                }
                else
                {
                    selectButtonText.text = "SELECT";
                    selectButton.interactable = true;
                }
            }

            // Move selection border to active thumbnail
            if (selectionBorder != null && currentBoatIndex < boatThumbnails.Count)
            {
                selectionBorder.SetParent(boatThumbnails[currentBoatIndex].transform, false);
                selectionBorder.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateSegments(List<Image> segments, int count)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i] != null)
                {
                    segments[i].color = (i < count) ? activeSegmentColor : inactiveSegmentColor;
                }
            }
        }

        public void SelectBoatByIndex(int index)
        {
            if (index >= 0 && index < boats.Count)
            {
                currentBoatIndex = index;
                RefreshUI();
            }
        }

        private void OnPrevBoatClicked()
        {
            currentBoatIndex = (currentBoatIndex - 1 + boats.Count) % boats.Count;
            RefreshUI();
        }

        private void OnNextBoatClicked()
        {
            currentBoatIndex = (currentBoatIndex + 1) % boats.Count;
            RefreshUI();
        }

        private void OnSelectClicked()
        {
            BoatData current = boats[currentBoatIndex];
            if (!current.isUnlocked)
            {
                if (userCoins >= current.unlockCost)
                {
                    userCoins -= current.unlockCost;
                    current.isUnlocked = true;
                    selectedBoatIndex = currentBoatIndex;
                    RefreshUI();
                    Debug.Log($"[BoatSelect] Unlocked {current.boatName}!");
                }
            }
            else
            {
                selectedBoatIndex = currentBoatIndex;
                RefreshUI();
                Debug.Log($"[BoatSelect] Selected {current.boatName}!");
            }
        }

        private void OnBackClicked()
        {
            UIManager.Instance.ShowMainMenu();
        }
    }
}
