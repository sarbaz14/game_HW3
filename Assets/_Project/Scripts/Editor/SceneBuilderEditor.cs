using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ChickenShooter.EditorTools
{
    /// <summary>Builds the four scenes (MainMenu, Level1, Level2, Level3) and registers build settings.</summary>
    public static class SceneBuilderEditor
    {
        private static Sprite _uiSprite;
        private static Sprite _backgroundSprite;
        private static Sprite _knobSprite;

        private static Sprite UISprite => _uiSprite != null
            ? _uiSprite
            : _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        private static Sprite BackgroundSprite => _backgroundSprite != null
            ? _backgroundSprite
            : _backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

        private static Sprite KnobSprite => _knobSprite != null
            ? _knobSprite
            : _knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        [MenuItem("ChickenShooter/5. Build Scenes & Build Settings", false, 50)]
        public static void BuildAllScenes()
        {
            EditorSceneManager.SaveOpenScenes();
            ProceduralArt.EnsureFolder(EditorAssets.SceneDir);

            BuildMainMenuScene();
            BuildGameplayScene("Level1", LoadLevel("Level1Data"));
            BuildGameplayScene("Level2", LoadLevel("Level2Data"));
            BuildGameplayScene("Level3", LoadLevel("Level3Data"));
            RegisterBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("[Scenes] MainMenu, Level1, Level2 and Level3 built and registered in Build Settings.");
        }

        private static LevelDataSO LoadLevel(string assetName)
        {
            var level = AssetDatabase.LoadAssetAtPath<LevelDataSO>(EditorAssets.LevelDir + "/" + assetName + ".asset");
            if (level == null) Debug.LogError("[Scenes] Missing level data: " + assetName);
            return level;
        }

        public static void RegisterBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(EditorAssets.SceneDir + "/MainMenu.unity", true),
                new EditorBuildSettingsScene(EditorAssets.SceneDir + "/Level1.unity", true),
                new EditorBuildSettingsScene(EditorAssets.SceneDir + "/Level2.unity", true),
                new EditorBuildSettingsScene(EditorAssets.SceneDir + "/Level3.unity", true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Scenes] Build Settings: " + string.Join(", ", scenes.ConvertAll(s => s.path)));
        }

        // ---------------------------------------------------------------- shared scene parts

        private static Camera CreateCamera(Color background, bool gameplay)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraShake));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = gameplay;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;

            var additional = cameraObject.GetComponent<UniversalAdditionalCameraData>();
            if (additional == null) additional = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additional.renderPostProcessing = true;
            additional.antialiasing = AntialiasingMode.None;
            additional.renderShadows = false;

            return camera;
        }

        private static void CreateGlobalVolume()
        {
            var volumeObject = new GameObject("Global Volume");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = DataAuthoringEditor.LoadVolumeProfile();
        }

        private static ParallaxStarfield CreateParallax(LevelDataSO level, Color fallbackBackground)
        {
            var parallaxObject = new GameObject("ParallaxBackground");
            var parallax = parallaxObject.AddComponent<ParallaxStarfield>();

            if (level != null)
            {
                parallax.primaryTint = level.secondaryBgTint;
                parallax.secondaryTint = Color.Lerp(level.primaryBgTint, Color.white, 0.55f);
            }
            else
            {
                parallax.primaryTint = new Color(0.4f, 0.7f, 1f);
                parallax.secondaryTint = fallbackBackground;
            }

            parallax.starsPerLayer = 46;
            parallax.verticalSpan = 16f;
            parallax.horizontalSpan = 26f;
            parallax.nebulaCount = 4;
            return parallax;
        }

        private static void CreateBootstrap()
        {
            var bootstrapObject = new GameObject("Bootstrap");
            bootstrapObject.AddComponent<SceneBootstrap>();
        }

        private static Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = eventSystemObject.GetComponent<InputSystemUIInputModule>();

            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsGenerator.AssetPath);
            if (inputAsset != null)
            {
                module.actionsAsset = inputAsset;
                module.AssignDefaultActions();
            }
            else
            {
                Debug.LogWarning("[Scenes] UI input actions asset missing - EventSystem will use defaults.");
            }
        }

        // ---------------------------------------------------------------- main menu

        private static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var background = new Color(0.02f, 0.03f, 0.08f);
            CreateCamera(background, false);
            CreateGlobalVolume();
            CreateParallax(null, new Color(0.25f, 0.5f, 1f));
            CreateBootstrap();

            var canvas = CreateCanvas();
            CreateEventSystem();

            var root = canvas.transform;

            var title = CreateText(root, "Title", "CHICKEN SHOOTER", 78, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.97f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f),
                new Vector2(1600f, 110f));
            title.fontStyle = FontStyle.Bold;

            var subtitle = CreateText(root, "Subtitle", "MULTI-STAGE SPACE DEFENSE", 26, TextAnchor.MiddleCenter,
                new Color(0.55f, 0.72f, 0.95f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -196f),
                new Vector2(1400f, 40f));
            subtitle.fontStyle = FontStyle.Bold;

            var saveStatus = CreateText(root, "SaveStatus", "SAVE FOUND", 20, TextAnchor.MiddleCenter,
                UIStyle.Accent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-380f, 210f),
                new Vector2(460f, 32f));
            saveStatus.fontStyle = FontStyle.Bold;

            var newGame = CreateButton(root, "NewGameButton", "NEW GAME", new Vector2(-380f, 90f), new Vector2(460f, 92f),
                new Color(0.16f, 0.42f, 0.62f));
            var continueButton = CreateButton(root, "ContinueButton", "CONTINUE", new Vector2(-380f, -20f),
                new Vector2(460f, 92f), new Color(0.18f, 0.32f, 0.45f));
            var quitButton = CreateButton(root, "QuitButton", "QUIT", new Vector2(-380f, -130f), new Vector2(460f, 92f),
                new Color(0.22f, 0.2f, 0.28f));

            var continueInfo = CreateText(root, "ContinueInfo", "Start a run to create a save file", 19,
                TextAnchor.MiddleCenter, new Color(0.62f, 0.7f, 0.82f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-380f, -200f), new Vector2(560f, 60f));

            var audioTitle = CreateText(root, "AudioTitle", "AUDIO", 24, TextAnchor.MiddleCenter,
                new Color(0.7f, 0.85f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380f, 210f),
                new Vector2(420f, 34f));
            audioTitle.fontStyle = FontStyle.Bold;

            CreateText(root, "MasterLabel", "MASTER", 20, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.92f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 150f), new Vector2(300f, 28f));
            var masterSlider = CreateSlider(root, "MasterSlider", new Vector2(380f, 112f), new Vector2(420f, 24f),
                UIStyle.Accent);

            CreateText(root, "BgmLabel", "MUSIC", 20, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.92f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 60f), new Vector2(300f, 28f));
            var bgmSlider = CreateSlider(root, "BgmSlider", new Vector2(380f, 22f), new Vector2(420f, 24f),
                new Color(0.45f, 1f, 0.72f));

            CreateText(root, "SfxLabel", "SFX", 20, TextAnchor.MiddleLeft, new Color(0.7f, 0.8f, 0.92f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, -30f), new Vector2(300f, 28f));
            var sfxSlider = CreateSlider(root, "SfxSlider", new Vector2(380f, -68f), new Vector2(420f, 24f),
                new Color(1f, 0.72f, 0.32f));

            var hint = CreateText(root, "ControlsHint",
                "WASD / ARROWS  MOVE        SPACE  FIRE        B  SUPER MISSILE        ESC / P  PAUSE",
                21, TextAnchor.MiddleCenter, new Color(0.55f, 0.68f, 0.85f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1700f, 34f));

            var menu = canvas.gameObject.AddComponent<MainMenuUI>();
            menu.newGameButton = newGame;
            menu.continueButton = continueButton;
            menu.quitButton = quitButton;
            menu.masterSlider = masterSlider;
            menu.bgmSlider = bgmSlider;
            menu.sfxSlider = sfxSlider;
            menu.continueInfoText = continueInfo;
            menu.saveStatusText = saveStatus;

            EditorSceneManager.SaveScene(scene, EditorAssets.SceneDir + "/MainMenu.unity");
        }

        // ---------------------------------------------------------------- gameplay levels

        private static void BuildGameplayScene(string sceneName, LevelDataSO level)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Color background = level != null ? level.primaryBgTint : new Color(0.03f, 0.05f, 0.12f);
            CreateCamera(background, true);
            CreateGlobalVolume();
            CreateParallax(level, background);
            CreateBootstrap();

            var managerObject = new GameObject("WaveManager");
            var waveManager = managerObject.AddComponent<WaveManager>();
            waveManager.levelData = level;

            var playerPrefab = EditorAssets.Prefab("Player/Player");
            if (playerPrefab != null)
            {
                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.name = "Player";
                player.transform.position = new Vector3(0f, -3.7f, 0f);
            }
            else
            {
                Debug.LogError("[Scenes] Player prefab missing - run 'Build Prefabs' first.");
            }

            var canvas = CreateCanvas();
            CreateEventSystem();
            BuildHud(canvas.transform);
            BuildPauseMenu(canvas.transform);
            BuildGameOver(canvas.transform);
            BuildVictory(canvas.transform);

            EditorSceneManager.SaveScene(scene, EditorAssets.SceneDir + "/" + sceneName + ".unity");
        }

        private static void BuildHud(Transform root)
        {
            // Lives icons
            var livesRoot = CreateRect(root, "Lives", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38f, -30f), new Vector2(260f, 52f));
            var lifeIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var icon = CreateImage(livesRoot, "Life_" + i, EditorAssets.Sprite("PlayerShip"), Color.white,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * 54f, 0f), new Vector2(48f, 48f));
                icon.preserveAspect = true;
                lifeIcons[i] = icon;
            }

            var stageText = CreateText(root, "StageText", "LEVEL 1  -  ROUND 1", 32, TextAnchor.MiddleCenter,
                new Color(0.88f, 0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                new Vector2(900f, 46f));
            stageText.fontStyle = FontStyle.Bold;

            var scoreText = CreateText(root, "ScoreText", "SCORE\n0", 30, TextAnchor.UpperRight,
                new Color(0.9f, 0.96f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-38f, -28f),
                new Vector2(380f, 92f));

            var missileIcon = CreateImage(root, "MissileIcon", EditorAssets.Sprite("SuperMissile"),
                new Color(0.9f, 0.95f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-148f, -132f),
                new Vector2(34f, 54f));
            missileIcon.preserveAspect = true;

            var missileText = CreateText(root, "MissileText", "x2", 30, TextAnchor.MiddleRight,
                new Color(1f, 0.8f, 0.45f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -128f),
                new Vector2(96f, 46f));

            var weaponText = CreateText(root, "WeaponText", "POWER I", 24, TextAnchor.LowerRight,
                new Color(0.65f, 0.85f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-38f, 28f),
                new Vector2(420f, 32f));

            var foodLabel = CreateText(root, "FoodLabel", "MISSILE CHARGE 0%", 20, TextAnchor.LowerLeft,
                new Color(0.65f, 0.85f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(38f, 72f),
                new Vector2(480f, 30f));
            var foodSlider = CreateSlider(root, "FoodSlider", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(38f, 32f), new Vector2(460f, 26f), new Color(0.45f, 0.9f, 1f));
            foodSlider.interactable = false;

            // Boss bar
            var bossPanel = CreateRect(root, "BossPanel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(1120f, 92f));
            var bossBackground = bossPanel.gameObject.AddComponent<Image>();
            bossBackground.sprite = UISprite;
            bossBackground.type = Image.Type.Sliced;
            bossBackground.color = new Color(0.05f, 0.07f, 0.12f, 0.72f);
            bossBackground.raycastTarget = false;

            var bossNameText = CreateText(bossPanel, "BossNameText", "GIANT CHICKEN BOSS", 26, TextAnchor.MiddleCenter,
                new Color(1f, 0.62f, 0.55f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f),
                new Vector2(1000f, 32f));
            bossNameText.fontStyle = FontStyle.Bold;

            var bossSlider = CreateSlider(bossPanel, "BossSlider", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(1000f, 22f), new Color(1f, 0.32f, 0.36f));
            bossSlider.interactable = false;

            // Stage banner
            var bannerPanel = CreateRect(root, "BannerPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1500f, 200f));
            var bannerBackground = bannerPanel.gameObject.AddComponent<Image>();
            bannerBackground.sprite = UISprite;
            bannerBackground.type = Image.Type.Sliced;
            bannerBackground.color = new Color(0.03f, 0.06f, 0.12f, 0.82f);
            bannerBackground.raycastTarget = false;

            var bannerText = CreateText(bannerPanel, "BannerText", "STAGE 1 COMPLETE!", 42, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.95f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(1380f, 170f));
            bannerText.fontStyle = FontStyle.Bold;

            var hud = root.gameObject.AddComponent<HUDController>();
            hud.stageText = stageText;
            hud.scoreText = scoreText;
            hud.missileText = missileText;
            hud.weaponText = weaponText;
            hud.foodLabel = foodLabel;
            hud.foodSlider = foodSlider;
            hud.bossSlider = bossSlider;
            hud.lifeIcons = lifeIcons;
            hud.bossPanel = bossPanel.gameObject;
            hud.bossNameText = bossNameText;
            hud.bannerPanel = bannerPanel.gameObject;
            hud.bannerText = bannerText;

            bossPanel.gameObject.SetActive(false);
            bannerPanel.gameObject.SetActive(false);
        }

        private static void BuildPauseMenu(Transform root)
        {
            var overlay = CreateImage(root, "PausePanel", null, new Color(0.02f, 0.03f, 0.06f, 0.78f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform);

            var inner = CreateImage(overlay.transform, "PauseInner", UISprite, new Color(0.05f, 0.09f, 0.15f, 0.97f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 460f));
            inner.type = Image.Type.Sliced;

            var title = CreateText(inner.transform, "PauseTitle", "PAUSED", 52, TextAnchor.MiddleCenter,
                new Color(0.8f, 0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                new Vector2(600f, 70f));
            title.fontStyle = FontStyle.Bold;

            CreateText(inner.transform, "PauseHint", "The battle is frozen.", 20, TextAnchor.MiddleCenter,
                new Color(0.55f, 0.68f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f),
                new Vector2(600f, 30f));

            var resume = CreateButton(inner.transform, "ResumeButton", "RESUME", new Vector2(0f, 10f),
                new Vector2(460f, 88f), new Color(0.16f, 0.45f, 0.4f));
            var mainMenu = CreateButton(inner.transform, "PauseMainMenuButton", "QUIT TO MAIN MENU",
                new Vector2(0f, -100f), new Vector2(460f, 88f), new Color(0.28f, 0.2f, 0.28f));

            var pauseMenu = root.gameObject.AddComponent<PauseMenuUI>();
            pauseMenu.panel = overlay.gameObject;
            pauseMenu.resumeButton = resume;
            pauseMenu.mainMenuButton = mainMenu;

            overlay.gameObject.SetActive(false);
        }

        private static void BuildGameOver(Transform root)
        {
            var overlay = CreateImage(root, "GameOverPanel", null, new Color(0.06f, 0.01f, 0.03f, 0.82f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform);

            var inner = CreateImage(overlay.transform, "GameOverInner", UISprite,
                new Color(0.08f, 0.04f, 0.08f, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820f, 540f));
            inner.type = Image.Type.Sliced;

            var title = CreateText(inner.transform, "GameOverTitle", "GAME OVER", 58, TextAnchor.MiddleCenter,
                new Color(1f, 0.38f, 0.42f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f),
                new Vector2(700f, 80f));
            title.fontStyle = FontStyle.Bold;

            var message = CreateText(inner.transform, "GameOverMessage", "Progress reset to Level 1 - Round 1", 21,
                TextAnchor.MiddleCenter, new Color(0.9f, 0.7f, 0.72f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(720f, 30f));

            var score = CreateText(inner.transform, "GameOverScore", "FINAL SCORE\n0", 34, TextAnchor.MiddleCenter,
                new Color(0.95f, 0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f),
                new Vector2(700f, 100f));

            var retry = CreateButton(inner.transform, "RetryButton", "RETRY LEVEL", new Vector2(0f, -74f),
                new Vector2(460f, 88f), new Color(0.45f, 0.2f, 0.24f));
            var mainMenu = CreateButton(inner.transform, "GameOverMainMenuButton", "MAIN MENU",
                new Vector2(0f, -180f), new Vector2(460f, 88f), new Color(0.24f, 0.24f, 0.32f));

            var gameOver = root.gameObject.AddComponent<GameOverUI>();
            gameOver.panel = overlay.gameObject;
            gameOver.scoreText = score;
            gameOver.messageText = message;
            gameOver.retryButton = retry;
            gameOver.mainMenuButton = mainMenu;

            overlay.gameObject.SetActive(false);
        }

        private static void BuildVictory(Transform root)
        {
            var overlay = CreateImage(root, "VictoryPanel", null, new Color(0.01f, 0.05f, 0.07f, 0.86f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            overlay.raycastTarget = true;
            Stretch(overlay.rectTransform);

            var inner = CreateImage(overlay.transform, "VictoryInner", UISprite,
                new Color(0.03f, 0.1f, 0.12f, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(960f, 580f));
            inner.type = Image.Type.Sliced;

            var title = CreateText(inner.transform, "VictoryTitle", "VICTORY", 66, TextAnchor.MiddleCenter,
                new Color(0.55f, 1f, 0.85f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                new Vector2(800f, 90f));
            title.fontStyle = FontStyle.Bold;

            CreateText(inner.transform, "VictorySubtitle", "The Galactic Emperor has fallen.\nThe galaxy is safe.",
                24, TextAnchor.MiddleCenter, new Color(0.7f, 0.92f, 0.92f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(820f, 80f));

            var score = CreateText(inner.transform, "VictoryScore", "FINAL SCORE\n0", 36, TextAnchor.MiddleCenter,
                new Color(0.95f, 1f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -240f),
                new Vector2(820f, 110f));

            var mainMenu = CreateButton(inner.transform, "VictoryMainMenuButton", "MAIN MENU",
                new Vector2(0f, -130f), new Vector2(480f, 92f), new Color(0.15f, 0.4f, 0.42f));

            var victory = root.gameObject.AddComponent<VictoryUI>();
            victory.panel = overlay.gameObject;
            victory.scoreText = score;
            victory.mainMenuButton = mainMenu;

            overlay.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- ui factory

        private static RectTransform CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);

            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize,
            TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition,
            Vector2 size)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax,
                new Vector2(anchorMin.x, anchorMin.y), anchoredPosition, size);

            var text = rect.gameObject.AddComponent<Text>();
            text.font = UIStyle.Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            return text;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, new Vector2(anchorMin.x, anchorMin.y),
                anchoredPosition, size);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition,
            Vector2 size, Color color)
        {
            var rect = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), anchoredPosition, size);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UISprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.8f, 0.95f, 1f);
            colors.pressedColor = new Color(0.6f, 0.8f, 0.95f);
            colors.selectedColor = new Color(0.9f, 1f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
            colors.colorMultiplier = 1.05f;
            button.colors = colors;

            var labelRect = CreateRect(rect, "Label", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            Stretch(labelRect);

            var text = labelRect.gameObject.AddComponent<Text>();
            text.font = UIStyle.Font;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.99f, 1f);
            text.text = label;
            text.raycastTarget = false;
            text.fontStyle = FontStyle.Bold;

            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size,
            Color fillColor)
        {
            return CreateSlider(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), anchoredPosition, size, fillColor);
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 size, Color fillColor)
        {
            var rect = CreateRect(parent, name, anchorMin, anchorMax, pivot, anchoredPosition, size);

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.ColorTint;

            var background = CreateImage(rect, "Background", UISprite, new Color(0.08f, 0.11f, 0.18f, 0.95f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(background.rectTransform);

            var fillArea = CreateRect(rect, "Fill Area", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            fillArea.offsetMin = new Vector2(6f, 6f);
            fillArea.offsetMax = new Vector2(-6f, -6f);

            var fill = CreateImage(fillArea, "Fill", UISprite, fillColor, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            Stretch(fill.rectTransform);

            var handleArea = CreateRect(rect, "Handle Slide Area", Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            var handle = CreateImage(handleArea, "Handle", KnobSprite, Color.white, new Vector2(0f, 0f),
                new Vector2(0f, 1f), Vector2.zero, new Vector2(26f, 26f));
            handle.preserveAspect = true;
            handle.rectTransform.sizeDelta = new Vector2(26f, 26f);
            handle.raycastTarget = true;
            background.raycastTarget = true;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.wholeNumbers = false;

            return slider;
        }
    }
}
