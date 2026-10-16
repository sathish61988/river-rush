using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RiverRush.UI
{
    [System.Serializable]
    public class MapCard
    {
        public string mapName;
        public Button cardButton;
        public Image mapImage;
    }

    /// <summary>
    /// Controls the Environment Selection screen with 4 map cards: Tropical, Forest, Mountain, and Canyon.
    /// </summary>
    public class EnvironmentSelectionController : MonoBehaviour
    {
        [Header("Header Elements")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Selection Border")]
        [SerializeField] private RectTransform selectionBorder;

        [Header("Map Cards")]
        [SerializeField] private List<MapCard> mapCards = new List<MapCard>();

        private int selectedMapIndex = 0;

        private void Start()
        {
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);

            for (int i = 0; i < mapCards.Count; i++)
            {
                int index = i;
                if (mapCards[i].cardButton != null)
                {
                    mapCards[i].cardButton.onClick.AddListener(() => OnMapSelected(index));
                }
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            if (selectionBorder != null && selectedMapIndex >= 0 && selectedMapIndex < mapCards.Count)
            {
                if (mapCards[selectedMapIndex].cardButton != null)
                {
                    selectionBorder.SetParent(mapCards[selectedMapIndex].cardButton.transform, false);
                    selectionBorder.anchoredPosition = Vector2.zero;
                    selectionBorder.gameObject.SetActive(true);
                }
            }
        }

        private void OnMapSelected(int index)
        {
            selectedMapIndex = index;
            RefreshUI();
            Debug.Log($"[MapSelect] Selected map: {mapCards[index].mapName}");
        }

        private void OnBackClicked()
        {
            UIManager.Instance.ShowMainMenu();
        }
    }
}
