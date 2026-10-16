using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RiverRush.UI;

namespace RiverRush.EditorTools
{
    public static class RiverRushSceneBuilder
    {
        private static readonly string TexturesUIPath = "Assets/_Project/Textures/UI";
        private static readonly string TexturesEnvPath = "Assets/_Project/Textures/Environment";
        private static readonly string ScenePath = "Assets/_Project/Scenes/RiverRush_MainScene.unity";

        [MenuItem("River Rush/Build Complete UI & Scene")]
        public static void BuildScene()
        {
            Debug.Log("[RiverRushSceneBuilder] Starting scene build...");
            ConfigureTextures();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Scene Lighting & Camera
            SetupCameraAndLighting();

            // 2. 3D Environment (Water, Boat, Riverbanks, Obstacles)
            GameObject envParent = Setup3DEnvironment();

            // 3. UI Canvas & EventSystem
            GameObject canvasGO = SetupCanvas(out Canvas canvas, out CanvasScaler scaler);

            // 4. UI Screens
            GameObject uiManagerGO = new GameObject("UIManager");
            UIManager uiManager = uiManagerGO.AddComponent<UIManager>();

            GameObject mainMenuPanel = BuildMainMenu(canvas.transform);
            GameObject boatSelectPanel = BuildBoatSelection(canvas.transform);
            GameObject mapSelectPanel = BuildMapSelection(canvas.transform);
            GameObject hudPanel = BuildHUD(canvas.transform);
            GameObject pauseModal = BuildPauseModal(canvas.transform);
            GameObject gameOverModal = BuildGameOverModal(canvas.transform);

            // 5. Wire up UIManager via SerializedObject
            SerializedObject soManager = new SerializedObject(uiManager);
            soManager.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            soManager.FindProperty("boatSelectionPanel").objectReferenceValue = boatSelectPanel;
            soManager.FindProperty("mapSelectionPanel").objectReferenceValue = mapSelectPanel;
            soManager.FindProperty("hudPanel").objectReferenceValue = hudPanel;
            soManager.FindProperty("pauseModal").objectReferenceValue = pauseModal;
            soManager.FindProperty("gameOverModal").objectReferenceValue = gameOverModal;

            soManager.FindProperty("mainMenuController").objectReferenceValue = mainMenuPanel.GetComponent<UI.MainMenuController>();
            soManager.FindProperty("boatSelectionController").objectReferenceValue = boatSelectPanel.GetComponent<UI.BoatSelectionController>();
            soManager.FindProperty("mapSelectionController").objectReferenceValue = mapSelectPanel.GetComponent<UI.EnvironmentSelectionController>();
            soManager.FindProperty("hudController").objectReferenceValue = hudPanel.GetComponent<UI.HUDController>();
            soManager.FindProperty("pauseMenuController").objectReferenceValue = pauseModal.GetComponent<UI.PauseMenuController>();
            soManager.FindProperty("gameOverController").objectReferenceValue = gameOverModal.GetComponent<UI.GameOverController>();

            soManager.FindProperty("inGameRiverEnvironment").objectReferenceValue = envParent;
            soManager.ApplyModifiedProperties();

            // Set Initial Active State
            mainMenuPanel.SetActive(true);
            boatSelectPanel.SetActive(false);
            mapSelectPanel.SetActive(false);
            hudPanel.SetActive(false);
            pauseModal.SetActive(false);
            gameOverModal.SetActive(false);

            // Save Scene
            Directory.CreateDirectory("Assets/_Project/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RiverRushSceneBuilder] Scene successfully built and saved to: {ScenePath}");
        }

        public static void ConfigureTextures()
        {
            string[] dirs = { TexturesUIPath, TexturesEnvPath };
            foreach (string dir in dirs)
            {
                if (!Directory.Exists(dir)) continue;
                string[] files = Directory.GetFiles(dir, "*.png");
                foreach (string file in files)
                {
                    string assetPath = file.Replace('\\', '/');
                    TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null)
                    {
                        bool changed = false;
                        if (importer.textureType != TextureImporterType.Sprite)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            changed = true;
                        }
                        if (!importer.alphaIsTransparency)
                        {
                            importer.alphaIsTransparency = true;
                            changed = true;
                        }
                        if (importer.mipmapEnabled)
                        {
                            importer.mipmapEnabled = false;
                            changed = true;
                        }
                        if (changed)
                        {
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        private static void SetupCameraAndLighting()
        {
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.45f, 0.65f); // Tropical Sky Blue
            cam.fieldOfView = 60f;
            camGO.transform.position = new Vector3(0f, 3.2f, -6.5f);
            camGO.transform.rotation = Quaternion.Euler(16f, 0f, 0f);

            GameObject lightGO = new GameObject("Directional Light");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static GameObject Setup3DEnvironment()
        {
            GameObject envRoot = new GameObject("Environment_River");

            // 1. Water Plane
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "RiverWater";
            water.transform.parent = envRoot.transform;
            water.transform.position = new Vector3(0f, 0f, 20f);
            water.transform.localScale = new Vector3(3f, 1f, 8f);
            Renderer waterRend = water.GetComponent<Renderer>();
            Material waterMat = new Material(Shader.Find("Standard"));
            waterMat.color = new Color(0.05f, 0.58f, 0.68f, 0.95f); // Vibrant Turquoise Water
            waterMat.SetFloat("_Glossiness", 0.92f);
            waterMat.SetFloat("_Metallic", 0.1f);
            waterRend.material = waterMat;

            // 2. Speedboat Model (Stylized primitives)
            GameObject boat = new GameObject("Speedboat");
            boat.transform.parent = envRoot.transform;
            boat.transform.position = new Vector3(0f, 0.2f, -1.5f);

            // Hull
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.parent = boat.transform;
            hull.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            hull.transform.localScale = new Vector3(1.4f, 0.5f, 3.2f);
            Material redMat = new Material(Shader.Find("Standard"));
            redMat.color = new Color(0.85f, 0.18f, 0.15f); // Red Speedster
            redMat.SetFloat("_Glossiness", 0.85f);
            hull.GetComponent<Renderer>().material = redMat;

            // Bow (front wedge)
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bow.name = "Bow";
            bow.transform.parent = boat.transform;
            bow.transform.localPosition = new Vector3(0f, 0.15f, 1.4f);
            bow.transform.localScale = new Vector3(1.35f, 0.25f, 1.35f);
            bow.GetComponent<Renderer>().material = redMat;

            // Cockpit / Deck
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.parent = boat.transform;
            deck.transform.localPosition = new Vector3(0f, 0.35f, -0.2f);
            deck.transform.localScale = new Vector3(1.1f, 0.25f, 1.8f);
            Material darkMat = new Material(Shader.Find("Standard"));
            darkMat.color = new Color(0.18f, 0.2f, 0.25f);
            deck.GetComponent<Renderer>().material = darkMat;

            // Windshield
            GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windshield.name = "Windshield";
            windshield.transform.parent = boat.transform;
            windshield.transform.localPosition = new Vector3(0f, 0.6f, 0.5f);
            windshield.transform.localScale = new Vector3(1.05f, 0.35f, 0.2f);
            windshield.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            Material glassMat = new Material(Shader.Find("Standard"));
            glassMat.color = new Color(0.2f, 0.7f, 0.9f, 0.7f);
            glassMat.SetFloat("_Glossiness", 0.95f);
            windshield.GetComponent<Renderer>().material = glassMat;

            // Seats
            for (int i = 0; i < 2; i++)
            {
                GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seat.name = $"Seat_{i}";
                seat.transform.parent = boat.transform;
                seat.transform.localPosition = new Vector3((i == 0 ? -0.3f : 0.3f), 0.55f, -0.3f);
                seat.transform.localScale = new Vector3(0.35f, 0.4f, 0.35f);
                Material leatherMat = new Material(Shader.Find("Standard"));
                leatherMat.color = new Color(0.55f, 0.25f, 0.15f);
                seat.GetComponent<Renderer>().material = leatherMat;
            }

            // 3. Riverbanks (Left and Right green terrain/rock)
            Material bankMat = new Material(Shader.Find("Standard"));
            bankMat.color = new Color(0.22f, 0.55f, 0.2f); // Lush Jungle Green

            GameObject leftBank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftBank.name = "LeftRiverBank";
            leftBank.transform.parent = envRoot.transform;
            leftBank.transform.position = new Vector3(-16f, 1f, 25f);
            leftBank.transform.localScale = new Vector3(14f, 2.5f, 90f);
            leftBank.GetComponent<Renderer>().material = bankMat;

            GameObject rightBank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBank.name = "RightRiverBank";
            rightBank.transform.parent = envRoot.transform;
            rightBank.transform.position = new Vector3(16f, 1f, 25f);
            rightBank.transform.localScale = new Vector3(14f, 2.5f, 90f);
            rightBank.GetComponent<Renderer>().material = bankMat;

            // 4. Floating Coins (Collectible trail)
            Material goldMat = new Material(Shader.Find("Standard"));
            goldMat.color = new Color(1f, 0.82f, 0.15f);
            goldMat.SetFloat("_Metallic", 0.85f);
            goldMat.SetFloat("_Glossiness", 0.9f);

            for (int i = 0; i < 6; i++)
            {
                GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = $"Coin_{i}";
                coin.transform.parent = envRoot.transform;
                coin.transform.position = new Vector3(0f, 0.45f, 4f + i * 4.5f);
                coin.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
                coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                coin.GetComponent<Renderer>().material = goldMat;
            }

            // 5. Floating Obstacle Logs
            Material logMat = new Material(Shader.Find("Standard"));
            logMat.color = new Color(0.42f, 0.28f, 0.15f);

            GameObject log1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log1.name = "ObstacleLog_Left";
            log1.transform.parent = envRoot.transform;
            log1.transform.position = new Vector3(-3.5f, 0.15f, 12f);
            log1.transform.localScale = new Vector3(0.7f, 2.8f, 0.7f);
            log1.transform.rotation = Quaternion.Euler(0f, 25f, 90f);
            log1.GetComponent<Renderer>().material = logMat;

            GameObject log2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log2.name = "ObstacleLog_Right";
            log2.transform.parent = envRoot.transform;
            log2.transform.position = new Vector3(3.8f, 0.15f, 16f);
            log2.transform.localScale = new Vector3(0.7f, 2.8f, 0.7f);
            log2.transform.rotation = Quaternion.Euler(0f, -20f, 90f);
            log2.GetComponent<Renderer>().material = logMat;

            return envRoot;
        }

        private static GameObject SetupCanvas(out Canvas canvas, out CanvasScaler scaler)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject esGO = new GameObject("EventSystem");
                esGO.AddComponent<EventSystem>();
                esGO.AddComponent<StandaloneInputModule>();
            }

            return canvasGO;
        }

        // ==========================================
        // SCREEN 1: MAIN MENU
        // ==========================================
        private static GameObject BuildMainMenu(Transform parent)
        {
            GameObject panel = CreateUIObject("MainMenuPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            SetStretchFull(rt);

            // Background
            CreateImage("Background", panel.transform, LoadSprite(TexturesEnvPath + "/BG_MainMenu.png"), SetStretchFull);

            // Logo (Top-Left)
            GameObject logo = CreateImage("Logo", panel.transform, LoadSprite(TexturesUIPath + "/RiverRush_Logo.png"), r =>
            {
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(60f, -40f);
                r.sizeDelta = new Vector2(480f, 320f);
            });

            // Side Buttons (Settings, Leaderboard, More Games: Vertical Stack on Left)
            GameObject sideGroup = CreateUIObject("SideUtilityButtons", panel.transform);
            RectTransform sideRt = sideGroup.GetComponent<RectTransform>();
            sideRt.anchorMin = new Vector2(0f, 0.5f);
            sideRt.anchorMax = new Vector2(0f, 0.5f);
            sideRt.pivot = new Vector2(0f, 0.5f);
            sideRt.anchoredPosition = new Vector2(50f, -60f);
            sideRt.sizeDelta = new Vector2(100f, 320f);

            Button btnSettings = CreateSquareIconButton("Btn_Settings", sideGroup.transform, 0, "Icon_Settings", "Settings");
            Button btnLeaderboard = CreateSquareIconButton("Btn_Leaderboard", sideGroup.transform, -100, "Icon_Leaderboard", "Leaderboard");
            Button btnMoreGames = CreateSquareIconButton("Btn_MoreGames", sideGroup.transform, -200, "Icon_Gamepad", "More Games");

            // Main Action Buttons (Right Column)
            GameObject actionGroup = CreateUIObject("ActionButtons", panel.transform);
            RectTransform actionRt = actionGroup.GetComponent<RectTransform>();
            actionRt.anchorMin = new Vector2(1f, 0.5f);
            actionRt.anchorMax = new Vector2(1f, 0.5f);
            actionRt.pivot = new Vector2(1f, 0.5f);
            actionRt.anchoredPosition = new Vector2(-60f, -60f);
            actionRt.sizeDelta = new Vector2(380f, 440f);

            Button btnPlay = CreatePillButton("Btn_Play", actionGroup.transform, 130f, "Button_Green_Large", "PLAY", "Icon_Play", 42, new Vector2(360f, 100f));
            Button btnBoats = CreatePillButton("Btn_Boats", actionGroup.transform, 20f, "Button_Blue", "BOATS", "Icon_Boat", 32, new Vector2(320f, 80f));
            Button btnMaps = CreatePillButton("Btn_Maps", actionGroup.transform, -80f, "Button_Blue", "MAPS", "Icon_Map", 32, new Vector2(320f, 80f));
            Button btnStore = CreatePillButton("Btn_Store", actionGroup.transform, -180f, "Button_Blue", "STORE", "Icon_Store", 32, new Vector2(320f, 80f));

            UI.MainMenuController controller = panel.AddComponent<UI.MainMenuController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("playButton").objectReferenceValue = btnPlay;
            so.FindProperty("boatsButton").objectReferenceValue = btnBoats;
            so.FindProperty("mapsButton").objectReferenceValue = btnMaps;
            so.FindProperty("storeButton").objectReferenceValue = btnStore;
            so.FindProperty("settingsButton").objectReferenceValue = btnSettings;
            so.FindProperty("leaderboardButton").objectReferenceValue = btnLeaderboard;
            so.FindProperty("moreGamesButton").objectReferenceValue = btnMoreGames;
            so.ApplyModifiedProperties();

            return panel;
        }

        // ==========================================
        // SCREEN 2: BOAT SELECTION
        // ==========================================
        private static GameObject BuildBoatSelection(Transform parent)
        {
            GameObject panel = CreateUIObject("BoatSelectionPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            SetStretchFull(rt);

            CreateImage("Background", panel.transform, LoadSprite(TexturesEnvPath + "/BG_BoatSelect.png"), SetStretchFull);

            // Header Row
            Button btnBack = CreateSquareButton("Btn_Back", panel.transform, new Vector2(50f, -40f), "Icon_Pause", r =>
            {
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(70f, 70f);
            });

            // Title Pill
            CreateHeaderPill("HeaderTitle", panel.transform, "SELECT BOAT", new Vector2(0f, -40f));

            // Coin Indicator Pill (Top-Right)
            TextMeshProUGUI coinsText = CreateCoinCounterPill("CoinCounter", panel.transform, "12,350", new Vector2(-60f, -40f));

            // Carousel Arrows (Left & Right)
            Button btnPrev = CreateCircleIconButton("Btn_PrevBoat", panel.transform, new Vector2(60f, 0f), "Steer_Button_Left", 100f);
            Button btnNext = CreateCircleIconButton("Btn_NextBoat", panel.transform, new Vector2(-60f, 0f), "Steer_Button_Right", 100f);

            // Stats Card (Bottom-Center above thumbnails)
            GameObject statsCard = CreateImage("StatsCard", panel.transform, LoadSprite(TexturesUIPath + "/Panel_Stats_Card.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0f);
                r.anchorMax = new Vector2(0.5f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, 175f);
                r.sizeDelta = new Vector2(860f, 160f);
            });

            // Boat Name
            TextMeshProUGUI boatName = CreateTMPText("BoatName", statsCard.transform, "SPEEDSTER", 34, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.anchoredPosition = new Vector2(40f, -20f);
                r.sizeDelta = new Vector2(300f, 40f);
            });

            // Stats Bars
            List<Image> speedSegs = CreateStatRow("SpeedRow", statsCard.transform, "Speed", -65f, 3);
            List<Image> handlingSegs = CreateStatRow("HandlingRow", statsCard.transform, "Handling", -95f, 4);
            List<Image> durabilitySegs = CreateStatRow("DurabilityRow", statsCard.transform, "Durability", -125f, 3);

            // Select Button (Right side of stats card)
            Button btnSelect = CreatePillButton("Btn_Select", statsCard.transform, 0f, "Button_Yellow", "SELECT", null, 32, new Vector2(220f, 75f));
            RectTransform selRt = btnSelect.GetComponent<RectTransform>();
            selRt.anchorMin = new Vector2(1f, 0.5f);
            selRt.anchorMax = new Vector2(1f, 0.5f);
            selRt.pivot = new Vector2(1f, 0.5f);
            selRt.anchoredPosition = new Vector2(-35f, 0f);
            TextMeshProUGUI selectText = btnSelect.GetComponentInChildren<TextMeshProUGUI>();

            // Bottom Boat Thumbnail Strip
            GameObject strip = CreateUIObject("BoatThumbStrip", panel.transform);
            RectTransform stripRt = strip.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0.5f, 0f);
            stripRt.anchorMax = new Vector2(0.5f, 0f);
            stripRt.pivot = new Vector2(0.5f, 0f);
            stripRt.anchoredPosition = new Vector2(0f, 25f);
            stripRt.sizeDelta = new Vector2(900f, 130f);

            HorizontalLayoutGroup hlg = strip.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            string[] thumbSprites = { "Boat_Thumb_Red", "Boat_Thumb_Blue", "Boat_Thumb_Yellow", "Boat_Thumb_Green", "Boat_Thumb_Locked" };
            List<Button> thumbButtons = new List<Button>();
            for (int i = 0; i < thumbSprites.Length; i++)
            {
                Button tb = CreateThumbnailButton($"Thumb_{i}", strip.transform, thumbSprites[i], new Vector2(160f, 115f));
                thumbButtons.Add(tb);
            }

            // Glowing Selection Border
            GameObject border = CreateImage("SelectionBorder", thumbButtons[0].transform, LoadSprite(TexturesUIPath + "/Selection_Border_Boat.png"), r =>
            {
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(-6f, -6f);
                r.offsetMax = new Vector2(6f, 6f);
            });

            UI.BoatSelectionController controller = panel.AddComponent<UI.BoatSelectionController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = btnBack;
            so.FindProperty("coinsText").objectReferenceValue = coinsText;
            so.FindProperty("boatNameText").objectReferenceValue = boatName;
            so.FindProperty("selectButton").objectReferenceValue = btnSelect;
            so.FindProperty("selectButtonText").objectReferenceValue = selectText;
            so.FindProperty("prevBoatButton").objectReferenceValue = btnPrev;
            so.FindProperty("nextBoatButton").objectReferenceValue = btnNext;
            so.FindProperty("selectionBorder").objectReferenceValue = border.GetComponent<RectTransform>();

            SetListProperty(so.FindProperty("speedSegments"), speedSegs);
            SetListProperty(so.FindProperty("handlingSegments"), handlingSegs);
            SetListProperty(so.FindProperty("durabilitySegments"), durabilitySegs);
            SetListProperty(so.FindProperty("boatThumbnails"), thumbButtons);

            so.ApplyModifiedProperties();

            return panel;
        }

        // ==========================================
        // SCREEN 3: MAP SELECTION
        // ==========================================
        private static GameObject BuildMapSelection(Transform parent)
        {
            GameObject panel = CreateUIObject("MapSelectionPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            SetStretchFull(rt);

            CreateImage("Background", panel.transform, LoadSprite(TexturesEnvPath + "/BG_MapSelect.png"), SetStretchFull);

            // Header Back Button
            Button btnBack = CreateSquareButton("Btn_Back", panel.transform, new Vector2(50f, -40f), "Icon_Pause", r =>
            {
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(70f, 70f);
            });

            CreateHeaderPill("HeaderTitle", panel.transform, "SELECT MAP", new Vector2(0f, -40f));

            // 2x2 Grid Container
            GameObject grid = CreateUIObject("MapGrid", panel.transform);
            RectTransform gridRt = grid.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.5f, 0.5f);
            gridRt.anchorMax = new Vector2(0.5f, 0.5f);
            gridRt.pivot = new Vector2(0.5f, 0.5f);
            gridRt.anchoredPosition = new Vector2(0f, -20f);
            gridRt.sizeDelta = new Vector2(880f, 620f);

            GridLayoutGroup glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(410f, 280f);
            glg.spacing = new Vector2(40f, 30f);
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.childAlignment = TextAnchor.MiddleCenter;

            string[] mapNames = { "TROPICAL RIVER", "FOREST RIVER", "MOUNTAIN RIVER", "CANYON RIVER" };
            string[] mapTextures = { "Map_Tropical", "Map_Forest", "Map_Mountain", "Map_Canyon" };

            List<UI.MapCard> cards = new List<UI.MapCard>();
            GameObject selectionBorder = null;

            for (int i = 0; i < 4; i++)
            {
                GameObject cardGO = CreateUIObject($"Card_{i}_{mapNames[i]}", grid.transform);
                Image img = cardGO.AddComponent<Image>();
                img.sprite = LoadSprite($"{TexturesUIPath}/{mapTextures[i]}.png");
                img.type = Image.Type.Simple;

                Button btn = cardGO.AddComponent<Button>();
                cardGO.AddComponent<UI.ButtonFeedback>();

                // Name Pill at bottom of card
                GameObject namePill = CreateImage("NamePill", cardGO.transform, LoadSprite(TexturesUIPath + "/Header_Pill.png"), r =>
                {
                    r.anchorMin = new Vector2(0.5f, 0f);
                    r.anchorMax = new Vector2(0.5f, 0f);
                    r.pivot = new Vector2(0.5f, 0f);
                    r.anchoredPosition = new Vector2(0f, 12f);
                    r.sizeDelta = new Vector2(260f, 44f);
                });

                CreateTMPText("NameText", namePill.transform, mapNames[i], 20, FontStyles.Bold, Color.white, SetStretchFull);

                if (i == 0)
                {
                    // Yellow glowing selection border on Tropical River
                    selectionBorder = CreateImage("SelectionBorder", cardGO.transform, LoadSprite(TexturesUIPath + "/Selection_Border_Gold.png"), r =>
                    {
                        r.anchorMin = Vector2.zero;
                        r.anchorMax = Vector2.one;
                        r.offsetMin = new Vector2(-8f, -8f);
                        r.offsetMax = new Vector2(8f, 8f);
                    });
                }

                cards.Add(new UI.MapCard
                {
                    mapName = mapNames[i],
                    cardButton = btn,
                    mapImage = img
                });
            }

            UI.EnvironmentSelectionController controller = panel.AddComponent<UI.EnvironmentSelectionController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("backButton").objectReferenceValue = btnBack;
            so.FindProperty("selectionBorder").objectReferenceValue = selectionBorder.GetComponent<RectTransform>();

            SerializedProperty listProp = so.FindProperty("mapCards");
            listProp.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++)
            {
                SerializedProperty elem = listProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("mapName").stringValue = cards[i].mapName;
                elem.FindPropertyRelative("cardButton").objectReferenceValue = cards[i].cardButton;
                elem.FindPropertyRelative("mapImage").objectReferenceValue = cards[i].mapImage;
            }
            so.ApplyModifiedProperties();

            return panel;
        }

        // ==========================================
        // SCREEN 4: IN-GAME HUD
        // ==========================================
        private static GameObject BuildHUD(Transform parent)
        {
            GameObject panel = CreateUIObject("HUDPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            SetStretchFull(rt);

            // Top-Left Pause Button
            Button btnPause = CreateSquareButton("Btn_Pause", panel.transform, new Vector2(50f, -40f), "Icon_Pause", r =>
            {
                r.anchorMin = new Vector2(0f, 1f);
                r.anchorMax = new Vector2(0f, 1f);
                r.pivot = new Vector2(0f, 1f);
                r.sizeDelta = new Vector2(70f, 70f);
            });

            // Top-Left Status Cluster (Distance, Coins, Hearts)
            GameObject statusCluster = CreateUIObject("StatusCluster", panel.transform);
            RectTransform clustRt = statusCluster.GetComponent<RectTransform>();
            clustRt.anchorMin = new Vector2(0f, 1f);
            clustRt.anchorMax = new Vector2(0f, 1f);
            clustRt.pivot = new Vector2(0f, 1f);
            clustRt.anchoredPosition = new Vector2(140f, -40f);
            clustRt.sizeDelta = new Vector2(240f, 150f);

            VerticalLayoutGroup vlg = statusCluster.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            TextMeshProUGUI distText = CreateHUDChip("DistanceChip", statusCluster.transform, "Icon_Distance", "1,250 m");
            TextMeshProUGUI coinsText = CreateHUDChip("CoinsChip", statusCluster.transform, "Icon_Coin", "236");
            TextMeshProUGUI healthText = CreateHUDChip("HealthChip", statusCluster.transform, "Icon_Heart", "3");

            // Top-Right Speedometer Dial
            GameObject speedo = CreateImage("Speedometer", panel.transform, LoadSprite(TexturesUIPath + "/Speedometer_Dial.png"), r =>
            {
                r.anchorMin = new Vector2(1f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(1f, 1f);
                r.anchoredPosition = new Vector2(-50f, -30f);
                r.sizeDelta = new Vector2(170f, 170f);
            });

            TextMeshProUGUI speedVal = CreateTMPText("SpeedDigits", speedo.transform, "128", 44, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, 4f);
                r.sizeDelta = new Vector2(120f, 50f);
            });

            CreateTMPText("SpeedUnit", speedo.transform, "KM/H", 16, FontStyles.Bold, new Color(0.6f, 0.8f, 1f), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, -28f);
                r.sizeDelta = new Vector2(100f, 25f);
            });

            // Bottom Steering Controls (Left & Right)
            Button steerL = CreateCircleIconButton("Steer_Left", panel.transform, new Vector2(80f, 80f), "Steer_Button_Left", 140f, r =>
            {
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(0f, 0f);
                r.pivot = new Vector2(0f, 0f);
                r.anchoredPosition = new Vector2(60f, 50f);
                r.sizeDelta = new Vector2(140f, 140f);
            });

            Button steerR = CreateCircleIconButton("Steer_Right", panel.transform, new Vector2(-80f, 80f), "Steer_Button_Right", 140f, r =>
            {
                r.anchorMin = new Vector2(1f, 0f);
                r.anchorMax = new Vector2(1f, 0f);
                r.pivot = new Vector2(1f, 0f);
                r.anchoredPosition = new Vector2(-60f, 50f);
                r.sizeDelta = new Vector2(140f, 140f);
            });

            UI.HUDController controller = panel.AddComponent<UI.HUDController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("pauseButton").objectReferenceValue = btnPause;
            so.FindProperty("distanceText").objectReferenceValue = distText;
            so.FindProperty("coinsText").objectReferenceValue = coinsText;
            so.FindProperty("healthText").objectReferenceValue = healthText;
            so.FindProperty("speedValueText").objectReferenceValue = speedVal;
            so.FindProperty("steerLeftButton").objectReferenceValue = steerL;
            so.FindProperty("steerRightButton").objectReferenceValue = steerR;
            so.ApplyModifiedProperties();

            return panel;
        }

        // ==========================================
        // SCREEN 5: PAUSE MENU MODAL
        // ==========================================
        private static GameObject BuildPauseModal(Transform parent)
        {
            GameObject modal = CreateUIObject("PauseModal", parent);
            RectTransform rt = modal.GetComponent<RectTransform>();
            SetStretchFull(rt);

            // Dark semi-transparent backdrop
            GameObject backdrop = CreateUIObject("Backdrop", modal.transform);
            SetStretchFull(backdrop.GetComponent<RectTransform>());
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0.05f, 0.12f, 0.72f);

            // Center Dialog Card
            GameObject card = CreateImage("DialogCard", modal.transform, LoadSprite(TexturesUIPath + "/Panel_Modal_Dark.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(520f, 580f);
            });

            // Title "PAUSED"
            CreateTMPText("Title", card.transform, "PAUSED", 42, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -30f);
                r.sizeDelta = new Vector2(300f, 50f);
            });

            // Red Close 'X' Button at top-right
            Button btnClose = CreateSquareButton("Btn_Close", card.transform, new Vector2(-25f, -25f), "Button_Red_Close", r =>
            {
                r.anchorMin = new Vector2(1f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(1f, 1f);
                r.sizeDelta = new Vector2(64f, 64f);
            });

            // Action Buttons (Resume, Restart, Main Menu)
            Button btnResume = CreatePillButton("Btn_Resume", card.transform, 60f, "Button_Green", "RESUME", "Icon_Play", 32, new Vector2(380f, 85f));
            Button btnRestart = CreatePillButton("Btn_Restart", card.transform, -40f, "Button_Blue", "RESTART", "Icon_Restart", 32, new Vector2(380f, 85f));
            Button btnMainMenu = CreatePillButton("Btn_MainMenu", card.transform, -140f, "Button_Blue", "MAIN MENU", "Icon_Home", 32, new Vector2(380f, 85f));

            // Bottom Audio & Settings Row (Music, Sound, Settings)
            GameObject audioRow = CreateUIObject("AudioRow", card.transform);
            RectTransform aRt = audioRow.GetComponent<RectTransform>();
            aRt.anchorMin = new Vector2(0.5f, 0f);
            aRt.anchorMax = new Vector2(0.5f, 0f);
            aRt.pivot = new Vector2(0.5f, 0f);
            aRt.anchoredPosition = new Vector2(0f, 35f);
            aRt.sizeDelta = new Vector2(300f, 80f);

            HorizontalLayoutGroup aHlg = audioRow.AddComponent<HorizontalLayoutGroup>();
            aHlg.spacing = 25f;
            aHlg.childAlignment = TextAnchor.MiddleCenter;
            aHlg.childControlWidth = false;
            aHlg.childControlHeight = false;

            Button btnMusic = CreateCircleButtonWithIcon("Btn_Music", audioRow.transform, "Button_Circle_Dark", "Icon_Music", 72f);
            Button btnSound = CreateCircleButtonWithIcon("Btn_Sound", audioRow.transform, "Button_Circle_Dark", "Icon_Sound", 72f);
            Button btnSettings = CreateCircleButtonWithIcon("Btn_Settings", audioRow.transform, "Button_Circle_Dark", "Icon_Settings", 72f);

            UI.PauseMenuController controller = modal.AddComponent<UI.PauseMenuController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("resumeButton").objectReferenceValue = btnResume;
            so.FindProperty("restartButton").objectReferenceValue = btnRestart;
            so.FindProperty("mainMenuButton").objectReferenceValue = btnMainMenu;
            so.FindProperty("closeButton").objectReferenceValue = btnClose;
            so.FindProperty("musicToggleButton").objectReferenceValue = btnMusic;
            so.FindProperty("soundToggleButton").objectReferenceValue = btnSound;
            so.FindProperty("settingsButton").objectReferenceValue = btnSettings;
            so.ApplyModifiedProperties();

            return modal;
        }

        // ==========================================
        // SCREEN 6: GAME OVER MODAL
        // ==========================================
        private static GameObject BuildGameOverModal(Transform parent)
        {
            GameObject modal = CreateUIObject("GameOverModal", parent);
            RectTransform rt = modal.GetComponent<RectTransform>();
            SetStretchFull(rt);

            // Dark semi-transparent backdrop
            GameObject backdrop = CreateUIObject("Backdrop", modal.transform);
            SetStretchFull(backdrop.GetComponent<RectTransform>());
            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0.05f, 0.12f, 0.72f);

            // Container Group
            GameObject container = CreateUIObject("GameOverContainer", modal.transform);
            RectTransform cRt = container.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = Vector2.zero;
            cRt.sizeDelta = new Vector2(620f, 620f);

            // Dark Results Card
            GameObject card = CreateImage("ResultsCard", container.transform, LoadSprite(TexturesUIPath + "/Panel_Modal_Dark.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, -20f);
                r.sizeDelta = new Vector2(560f, 440f);
            });

            // Wooden Banner at top: "GAME OVER"
            CreateImage("WoodenBanner", container.transform, LoadSprite(TexturesUIPath + "/Banner_GameOver.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, 25f);
                r.sizeDelta = new Vector2(520f, 150f);
            });

            // DISTANCE label & value
            CreateTMPText("DistLabel", card.transform, "DISTANCE", 20, FontStyles.Bold, new Color(0.7f, 0.85f, 1f), r =>
            {
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -40f);
                r.sizeDelta = new Vector2(300f, 30f);
            });

            TextMeshProUGUI distVal = CreateTMPText("DistValue", card.transform, "1,250 m", 48, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), r =>
            {
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -70f);
                r.sizeDelta = new Vector2(400f, 60f);
            });

            // COINS COLLECTED label & value
            CreateTMPText("CoinsLabel", card.transform, "COINS COLLECTED", 20, FontStyles.Bold, new Color(0.7f, 0.85f, 1f), r =>
            {
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = new Vector2(0f, -145f);
                r.sizeDelta = new Vector2(300f, 30f);
            });

            GameObject coinRow = CreateUIObject("CoinRow", card.transform);
            RectTransform coinRowRt = coinRow.GetComponent<RectTransform>();
            coinRowRt.anchorMin = new Vector2(0.5f, 1f);
            coinRowRt.anchorMax = new Vector2(0.5f, 1f);
            coinRowRt.pivot = new Vector2(0.5f, 1f);
            coinRowRt.anchoredPosition = new Vector2(0f, -180f);
            coinRowRt.sizeDelta = new Vector2(250f, 60f);

            CreateImage("CoinIcon", coinRow.transform, LoadSprite(TexturesUIPath + "/Icon_Coin.png"), r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(25f, 0f);
                r.sizeDelta = new Vector2(50f, 50f);
            });

            TextMeshProUGUI coinsVal = CreateTMPText("CoinsValue", coinRow.transform, "236", 46, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(90f, 0f);
                r.sizeDelta = new Vector2(150f, 50f);
            });

            // NEW BEST SCORE! Crown Row
            GameObject bestGroup = CreateUIObject("NewBestScoreGroup", card.transform);
            RectTransform bRt = bestGroup.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 35f);
            bRt.sizeDelta = new Vector2(360f, 50f);

            CreateImage("CrownIcon", bestGroup.transform, LoadSprite(TexturesUIPath + "/Icon_Crown.png"), r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(10f, 0f);
                r.sizeDelta = new Vector2(40f, 40f);
            });

            CreateTMPText("BestScoreText", bestGroup.transform, "NEW BEST SCORE!", 26, FontStyles.Bold, new Color(1f, 0.85f, 0.2f), r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(60f, 0f);
                r.sizeDelta = new Vector2(280f, 40f);
            });

            // Bottom Buttons (HOME and PLAY AGAIN)
            GameObject btnRow = CreateUIObject("ButtonRow", container.transform);
            RectTransform bRowRt = btnRow.GetComponent<RectTransform>();
            bRowRt.anchorMin = new Vector2(0.5f, 0f);
            bRowRt.anchorMax = new Vector2(0.5f, 0f);
            bRowRt.pivot = new Vector2(0.5f, 0f);
            bRowRt.anchoredPosition = new Vector2(0f, -80f);
            bRowRt.sizeDelta = new Vector2(560f, 90f);

            Button btnHome = CreatePillButton("Btn_Home", btnRow.transform, 0f, "Button_Blue", "HOME", "Icon_Home", 30, new Vector2(250f, 85f));
            RectTransform homeRt = btnHome.GetComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(0f, 0.5f);
            homeRt.anchorMax = new Vector2(0f, 0.5f);
            homeRt.pivot = new Vector2(0f, 0.5f);
            homeRt.anchoredPosition = new Vector2(10f, 0f);

            Button btnPlayAgain = CreatePillButton("Btn_PlayAgain", btnRow.transform, 0f, "Button_Green", "PLAY AGAIN", "Icon_Restart", 30, new Vector2(270f, 85f));
            RectTransform paRt = btnPlayAgain.GetComponent<RectTransform>();
            paRt.anchorMin = new Vector2(1f, 0.5f);
            paRt.anchorMax = new Vector2(1f, 0.5f);
            paRt.pivot = new Vector2(1f, 0.5f);
            paRt.anchoredPosition = new Vector2(-10f, 0f);

            UI.GameOverController controller = modal.AddComponent<UI.GameOverController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("homeButton").objectReferenceValue = btnHome;
            so.FindProperty("playAgainButton").objectReferenceValue = btnPlayAgain;
            so.FindProperty("distanceValueText").objectReferenceValue = distVal;
            so.FindProperty("coinsValueText").objectReferenceValue = coinsVal;
            so.FindProperty("newBestScoreGroup").objectReferenceValue = bestGroup;
            so.ApplyModifiedProperties();

            return modal;
        }

        // ==========================================
        // UI BUILDER HELPERS
        // ==========================================
        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, System.Action<RectTransform> layout)
        {
            GameObject go = CreateUIObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            layout?.Invoke(go.GetComponent<RectTransform>());
            return go;
        }

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float size, FontStyles style, Color color, System.Action<RectTransform> layout)
        {
            GameObject go = CreateUIObject(name, parent);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            layout?.Invoke(go.GetComponent<RectTransform>());
            return tmp;
        }

        private static Button CreatePillButton(string name, Transform parent, float yPos, string spriteName, string text, string iconName, float fontSize, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite($"{TexturesUIPath}/{spriteName}.png");
            img.type = Image.Type.Simple;

            Button btn = go.AddComponent<Button>();
            go.AddComponent<UI.ButtonFeedback>();

            // Icon
            if (!string.IsNullOrEmpty(iconName))
            {
                CreateImage("Icon", go.transform, LoadSprite($"{TexturesUIPath}/{iconName}.png"), r =>
                {
                    r.anchorMin = new Vector2(0f, 0.5f);
                    r.anchorMax = new Vector2(0f, 0.5f);
                    r.pivot = new Vector2(0f, 0.5f);
                    r.anchoredPosition = new Vector2(25f, 0f);
                    r.sizeDelta = new Vector2(40f, 40f);
                });
            }

            // Text
            CreateTMPText("Text", go.transform, text, fontSize, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(string.IsNullOrEmpty(iconName) ? 10f : 55f, 0f);
                r.offsetMax = new Vector2(-10f, 0f);
            });

            return btn;
        }

        private static Button CreateSquareButton(string name, Transform parent, Vector2 pos, string spriteName, System.Action<RectTransform> layout)
        {
            GameObject go = CreateUIObject(name, parent);
            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite($"{TexturesUIPath}/{spriteName}.png");
            Button btn = go.AddComponent<Button>();
            go.AddComponent<UI.ButtonFeedback>();
            layout?.Invoke(go.GetComponent<RectTransform>());
            return btn;
        }

        private static Button CreateSquareIconButton(string name, Transform parent, float yPos, string iconName, string label)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, yPos);
            rt.sizeDelta = new Vector2(80f, 80f);

            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite($"{TexturesUIPath}/Button_Square_Dark.png");

            Button btn = go.AddComponent<Button>();
            go.AddComponent<UI.ButtonFeedback>();

            CreateImage("Icon", go.transform, LoadSprite($"{TexturesUIPath}/{iconName}.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = new Vector2(0f, 6f);
                r.sizeDelta = new Vector2(36f, 36f);
            });

            CreateTMPText("Label", go.transform, label, 11, FontStyles.Normal, new Color(0.8f, 0.9f, 1f), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0f);
                r.anchorMax = new Vector2(0.5f, 0f);
                r.pivot = new Vector2(0.5f, 0f);
                r.anchoredPosition = new Vector2(0f, 6f);
                r.sizeDelta = new Vector2(90f, 18f);
            });

            return btn;
        }

        private static Button CreateCircleIconButton(string name, Transform parent, Vector2 pos, string spriteName, float size, System.Action<RectTransform> customLayout = null)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);

            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite($"{TexturesUIPath}/{spriteName}.png");

            Button btn = go.AddComponent<Button>();
            go.AddComponent<UI.ButtonFeedback>();

            customLayout?.Invoke(rt);
            return btn;
        }

        private static Button CreateCircleButtonWithIcon(string name, Transform parent, string bgSprite, string iconSprite, float size)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);

            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite($"{TexturesUIPath}/{bgSprite}.png");

            Button btn = go.AddComponent<Button>();
            go.AddComponent<UI.ButtonFeedback>();

            CreateImage("Icon", go.transform, LoadSprite($"{TexturesUIPath}/{iconSprite}.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 0.5f);
                r.anchorMax = new Vector2(0.5f, 0.5f);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(38f, 38f);
            });

            return btn;
        }

        private static Button CreateThumbnailButton(string name, Transform parent, string spriteName, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            Image img = go.AddComponent<Image>();
            img.sprite = LoadSprite($"{TexturesUIPath}/{spriteName}.png");

            Button btn = go.AddComponent<Button>();
            go.AddComponent<UI.ButtonFeedback>();

            return btn;
        }

        private static GameObject CreateHeaderPill(string name, Transform parent, string title, Vector2 pos)
        {
            GameObject go = CreateImage(name, parent, LoadSprite(TexturesUIPath + "/Header_Pill.png"), r =>
            {
                r.anchorMin = new Vector2(0.5f, 1f);
                r.anchorMax = new Vector2(0.5f, 1f);
                r.pivot = new Vector2(0.5f, 1f);
                r.anchoredPosition = pos;
                r.sizeDelta = new Vector2(360f, 65f);
            });

            CreateTMPText("TitleText", go.transform, title, 28, FontStyles.Bold, Color.white, SetStretchFull);
            return go;
        }

        private static TextMeshProUGUI CreateCoinCounterPill(string name, Transform parent, string amount, Vector2 pos)
        {
            GameObject go = CreateImage(name, parent, LoadSprite(TexturesUIPath + "/Counter_Pill.png"), r =>
            {
                r.anchorMin = new Vector2(1f, 1f);
                r.anchorMax = new Vector2(1f, 1f);
                r.pivot = new Vector2(1f, 1f);
                r.anchoredPosition = pos;
                r.sizeDelta = new Vector2(210f, 60f);
            });

            CreateImage("CoinIcon", go.transform, LoadSprite(TexturesUIPath + "/Icon_Coin.png"), r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(12f, 0f);
                r.sizeDelta = new Vector2(38f, 38f);
            });

            TextMeshProUGUI tmp = CreateTMPText("AmountText", go.transform, amount, 24, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = new Vector2(0f, 0f);
                r.anchorMax = new Vector2(1f, 1f);
                r.offsetMin = new Vector2(55f, 0f);
                r.offsetMax = new Vector2(-40f, 0f);
            });

            // Plus icon / button
            CreateTMPText("PlusText", go.transform, "+", 26, FontStyles.Bold, new Color(0.3f, 0.9f, 0.3f), r =>
            {
                r.anchorMin = new Vector2(1f, 0.5f);
                r.anchorMax = new Vector2(1f, 0.5f);
                r.pivot = new Vector2(1f, 0.5f);
                r.anchoredPosition = new Vector2(-15f, 0f);
                r.sizeDelta = new Vector2(30f, 30f);
            });

            return tmp;
        }

        private static TextMeshProUGUI CreateHUDChip(string name, Transform parent, string iconName, string text)
        {
            GameObject go = CreateImage(name, parent, LoadSprite(TexturesUIPath + "/Counter_Pill.png"), r =>
            {
                r.sizeDelta = new Vector2(190f, 48f);
            });

            CreateImage("Icon", go.transform, LoadSprite($"{TexturesUIPath}/{iconName}.png"), r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(12f, 0f);
                r.sizeDelta = new Vector2(32f, 32f);
            });

            return CreateTMPText("Value", go.transform, text, 22, FontStyles.Bold, Color.white, r =>
            {
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(50f, 0f);
                r.offsetMax = new Vector2(-12f, 0f);
            });
        }

        private static List<Image> CreateStatRow(string name, Transform parent, string statName, float yPos, int defaultActive)
        {
            GameObject row = CreateUIObject(name, parent);
            RectTransform rRt = row.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0f, 1f);
            rRt.anchorMax = new Vector2(0f, 1f);
            rRt.pivot = new Vector2(0f, 1f);
            rRt.anchoredPosition = new Vector2(40f, yPos);
            rRt.sizeDelta = new Vector2(420f, 26f);

            CreateTMPText("Label", row.transform, statName, 18, FontStyles.Normal, new Color(0.8f, 0.9f, 1f), r =>
            {
                r.anchorMin = new Vector2(0f, 0.5f);
                r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.sizeDelta = new Vector2(110f, 24f);
            });

            GameObject bar = CreateUIObject("Bar", row.transform);
            RectTransform barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0f, 0.5f);
            barRt.anchorMax = new Vector2(0f, 0.5f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.anchoredPosition = new Vector2(115f, 0f);
            barRt.sizeDelta = new Vector2(190f, 22f);

            HorizontalLayoutGroup hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            List<Image> segments = new List<Image>();
            for (int i = 0; i < 5; i++)
            {
                GameObject seg = CreateUIObject($"Seg_{i}", bar.transform);
                RectTransform sRt = seg.GetComponent<RectTransform>();
                sRt.sizeDelta = new Vector2(30f, 20f);
                Image img = seg.AddComponent<Image>();
                img.sprite = LoadSprite(TexturesUIPath + "/StatBar_Segment.png");
                img.color = (i < defaultActive) ? new Color(0.35f, 0.88f, 0.18f) : new Color(0.12f, 0.18f, 0.25f, 0.7f);
                segments.Add(img);
            }

            return segments;
        }

        private static void SetStretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetListProperty<T>(SerializedProperty listProp, List<T> items) where T : Object
        {
            listProp.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }
    }
}
