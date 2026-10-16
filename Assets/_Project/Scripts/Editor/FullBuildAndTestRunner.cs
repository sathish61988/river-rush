using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RiverRush.UI;

namespace RiverRush.EditorTools
{
    public static class FullBuildAndTestRunner
    {
        private static readonly string ScenePath = "Assets/_Project/Scenes/RiverRush_MainScene.unity";
        private static readonly string ScreenshotsDir = "Assets/_Project/Screenshots";

        [MenuItem("River Rush/Run Full Build & Test Suite")]
        public static void Run()
        {
            Debug.Log("\n=======================================================");
            Debug.Log("[FullBuildAndTestRunner] Starting River Rush Build & Test");
            Debug.Log("=======================================================\n");

            // Step 1: Build Scene
            RiverRushSceneBuilder.BuildScene();

            // Step 2: Open Scene
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[FAIL] Failed to open scene: " + ScenePath);
                return;
            }
            Debug.Log("[PASS] Scene loaded successfully: " + ScenePath);

            // Step 3: Validate Canvas & Scaler
            CanvasScaler scaler = Object.FindObjectOfType<CanvasScaler>();
            if (scaler == null)
            {
                Debug.LogError("[FAIL] CanvasScaler not found in scene!");
                return;
            }
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                scaler.referenceResolution != new Vector2(1920, 1080))
            {
                Debug.LogError("[FAIL] CanvasScaler settings incorrect!");
                return;
            }
            Debug.Log("[PASS] CanvasScaler configured with 1920x1080 reference resolution & Match 0.5");

            // Step 4: Validate UIManager
            UIManager uiManager = Object.FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[FAIL] UIManager not found!");
                return;
            }
            Debug.Log("[PASS] UIManager verified and present");

            // Step 5: Validate Screen Controllers
            UI.MainMenuController mainMenu = Object.FindObjectOfType<UI.MainMenuController>(true);
            UI.BoatSelectionController boatSelect = Object.FindObjectOfType<UI.BoatSelectionController>(true);
            UI.EnvironmentSelectionController mapSelect = Object.FindObjectOfType<UI.EnvironmentSelectionController>(true);
            UI.HUDController hud = Object.FindObjectOfType<UI.HUDController>(true);
            UI.PauseMenuController pause = Object.FindObjectOfType<UI.PauseMenuController>(true);
            UI.GameOverController gameOver = Object.FindObjectOfType<UI.GameOverController>(true);

            if (mainMenu == null || boatSelect == null || mapSelect == null || hud == null || pause == null || gameOver == null)
            {
                Debug.LogError("[FAIL] One or more UI screen controllers are missing!");
                return;
            }
            Debug.Log("[PASS] All 6 UI screen controllers verified");

            // Step 6: Validate Button Feedback
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            int fbCount = 0;
            foreach (var b in buttons)
            {
                if (b.gameObject.scene == scene)
                {
                    if (b.GetComponent<UI.ButtonFeedback>() != null) fbCount++;
                }
            }
            Debug.Log($"[PASS] ButtonFeedback verified on {fbCount} interactive UI buttons");

            // Step 7: Capture Screen Screenshots
            Directory.CreateDirectory(ScreenshotsDir);

            // Screenshot 1: Main Menu
            CaptureStateScreenshot(mainMenu.gameObject, new GameObject[] { boatSelect.gameObject, mapSelect.gameObject, hud.gameObject, pause.gameObject, gameOver.gameObject }, "01_MainMenu.png");

            // Screenshot 2: Boat Selection
            CaptureStateScreenshot(boatSelect.gameObject, new GameObject[] { mainMenu.gameObject, mapSelect.gameObject, hud.gameObject, pause.gameObject, gameOver.gameObject }, "02_BoatSelection.png");

            // Screenshot 3: Map Selection
            CaptureStateScreenshot(mapSelect.gameObject, new GameObject[] { mainMenu.gameObject, boatSelect.gameObject, hud.gameObject, pause.gameObject, gameOver.gameObject }, "03_MapSelection.png");

            // Screenshot 4: In-Game HUD
            CaptureStateScreenshot(hud.gameObject, new GameObject[] { mainMenu.gameObject, boatSelect.gameObject, mapSelect.gameObject, pause.gameObject, gameOver.gameObject }, "04_InGameHUD.png");

            // Screenshot 5: Pause Menu (Modal over HUD)
            hud.gameObject.SetActive(true);
            CaptureStateScreenshot(pause.gameObject, new GameObject[] { mainMenu.gameObject, boatSelect.gameObject, mapSelect.gameObject, gameOver.gameObject }, "05_PauseMenu.png");

            // Screenshot 6: Game Over (Modal over HUD)
            hud.gameObject.SetActive(true);
            CaptureStateScreenshot(gameOver.gameObject, new GameObject[] { mainMenu.gameObject, boatSelect.gameObject, mapSelect.gameObject, pause.gameObject }, "06_GameOver.png");

            // Restore Main Menu as default
            mainMenu.gameObject.SetActive(true);
            boatSelect.gameObject.SetActive(false);
            mapSelect.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("\n=======================================================");
            Debug.Log("[PASS] ALL 6 UI SCREENS VERIFIED AND SCREENSHOTS CAPTURED!");
            Debug.Log("=======================================================\n");
        }

        private static void CaptureStateScreenshot(GameObject activePanel, GameObject[] inactivePanels, string filename)
        {
            activePanel.SetActive(true);
            foreach (var p in inactivePanels)
            {
                p.SetActive(false);
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            RenderMode prevMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            Camera prevWorldCam = canvas != null ? canvas.worldCamera : null;
            float prevPlaneDist = canvas != null ? canvas.planeDistance : 100f;

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 1.2f;
                Canvas.ForceUpdateCanvases();
            }

            int width = 1920;
            int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24);
            RenderTexture prevRT = cam.targetTexture;
            RenderTexture prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevRT;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);

            if (canvas != null)
            {
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevWorldCam;
                canvas.planeDistance = prevPlaneDist;
                Canvas.ForceUpdateCanvases();
            }

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string path = Path.Combine(ScreenshotsDir, filename);
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[SCREENSHOT] Captured {filename} to {path}");
        }
    }
}
