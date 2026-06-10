#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;

namespace DualBeat.Editor
{
    public class UIBuilder : EditorWindow
    {
        private static DefaultControls.Resources uiResources = new DefaultControls.Resources();

        [MenuItem("Rhythm Game/Build UI Hierarchies")]
        public static void BuildAll()
        {
            Debug.Log("Starting UI Hierarchy Orchestration...");
            
            // Create Prefabs directory if missing
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            BuildIntroScene();
            BuildLobbyScene();
            BuildRoomLobbyScene();
            BuildInGameScene();

            AssetDatabase.SaveAssets();
            Debug.Log("UI Hierarchy Orchestration fully completed! All scenes saved.");
        }

        #region Helpers

        private static GameObject GetOrCreateCanvas(string sceneName)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas.gameObject;
            }

            GameObject canvasObj = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Create Event System
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            return canvasObj;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color bgColor)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = panel.GetComponent<Image>();
            img.color = bgColor;

            return panel;
        }

        private static TextMeshProUGUI CreateText(string name, string text, Transform parent, float fontSize, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);

            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = TMP_Settings.defaultFontAsset;

            return tmp;
        }

        private static Button CreateButton(string name, string labelText, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject btnObj = DefaultControls.CreateButton(uiResources);
            btnObj.name = name;
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            // Replace legacy text with TMP
            Text legacyText = btnObj.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                DestroyImmediate(legacyText.gameObject);
            }

            CreateText("Text (TMP)", labelText, btnObj.transform, 22, Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Button btn = btnObj.GetComponent<Button>();
            return btn;
        }

        private static TMP_InputField CreateInputField(string name, string placeholderText, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject inputObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(parent, false);

            RectTransform rect = inputObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            Image img = inputObj.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.9f); // Dark theme input field

            // Text Areas
            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textRect = textArea.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-10, -10);

            TextMeshProUGUI placeholder = CreateText("Placeholder", placeholderText, textArea.transform, 20, new Color(0.5f, 0.5f, 0.6f, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholder.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI textDisplay = CreateText("Text", "", textArea.transform, 20, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            textDisplay.alignment = TextAlignmentOptions.Left;

            TMP_InputField inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.textViewport = textRect;
            inputField.textComponent = textDisplay;
            inputField.placeholder = placeholder;

            return inputField;
        }

        private static Slider CreateSlider(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject sliderObj = DefaultControls.CreateSlider(uiResources);
            sliderObj.name = name;
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            return sliderObj.GetComponent<Slider>();
        }

        private static TMP_Dropdown CreateDropdown(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject dropdownObj = DefaultControls.CreateDropdown(uiResources);
            dropdownObj.name = name;
            dropdownObj.transform.SetParent(parent, false);

            RectTransform rect = dropdownObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            // Replace legacy Dropdown with TMP_Dropdown
            DestroyImmediate(dropdownObj.GetComponent<Dropdown>());
            
            // Clean legacy components inside Template
            Transform template = dropdownObj.transform.Find("Template");
            if (template != null)
            {
                Transform viewport = template.Find("Viewport");
                if (viewport != null)
                {
                    Transform content = viewport.Find("Content");
                    if (content != null)
                    {
                        Transform item = content.Find("Item");
                        if (item != null)
                        {
                            Text itemText = item.GetComponentInChildren<Text>();
                            if (itemText != null)
                            {
                                GameObject itemTextObj = itemText.gameObject;
                                DestroyImmediate(itemText);
                                TextMeshProUGUI itemTmp = itemTextObj.AddComponent<TextMeshProUGUI>();
                                itemTmp.font = TMP_Settings.defaultFontAsset;
                                itemTmp.fontSize = 20;
                            }
                        }
                    }
                }
            }

            // Clean legacy label text
            Transform label = dropdownObj.transform.Find("Label");
            if (label != null)
            {
                Text labelText = label.GetComponent<Text>();
                if (labelText != null)
                {
                    GameObject labelObj = labelText.gameObject;
                    DestroyImmediate(labelText);
                    TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
                    labelTmp.font = TMP_Settings.defaultFontAsset;
                    labelTmp.fontSize = 20;
                }
            }

            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            return dropdown;
        }

        private static ScrollRect CreateScrollRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject scrollObj = DefaultControls.CreateScrollView(uiResources);
            scrollObj.name = name;
            scrollObj.transform.SetParent(parent, false);

            RectTransform rect = scrollObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = sizeDelta;

            // Remove default image component and add our own if needed
            Image img = scrollObj.GetComponent<Image>();
            if (img != null) img.color = new Color(0.1f, 0.1f, 0.12f, 0.8f);

            return scrollObj.GetComponent<ScrollRect>();
        }

        #endregion

        #region Scene UI Generators

        private static void BuildIntroScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/IntroScene.unity");
            
            // 1. Create IntroManager GameObject
            GameObject managerObj = Object.FindAnyObjectByType<UI.IntroManager>()?.gameObject;
            if (managerObj == null)
            {
                managerObj = new GameObject("IntroManager", typeof(UI.IntroManager));
            }
            UI.IntroManager manager = managerObj.GetComponent<UI.IntroManager>();

            // 2. Set up Canvas
            GameObject canvasObj = GetOrCreateCanvas("IntroScene");
            
            // Main Panel
            GameObject mainPanel = CreatePanel("MainPanel", canvasObj.transform, new Color(0.08f, 0.08f, 0.12f, 1f));
            
            // Title Text
            CreateText("TitleText", "DUAL BEAT", mainPanel.transform, 72, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(600, 100));

            // Button Container Panel
            GameObject btnContainer = new GameObject("ButtonGroup", typeof(RectTransform), typeof(VerticalLayoutGroup));
            btnContainer.transform.SetParent(mainPanel.transform, false);
            RectTransform btnRect = btnContainer.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0, -100);
            btnRect.sizeDelta = new Vector2(300, 250);

            VerticalLayoutGroup vlg = btnContainer.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Buttons
            Button playBtn = CreateButton("StartGameButton", "Start Game", btnContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, 60));
            Button settingsBtn = CreateButton("SettingsButton", "Settings", btnContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, 60));
            Button exitBtn = CreateButton("ExitButton", "Exit Game", btnContainer.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, 60));

            // 3. Settings Panel
            GameObject settingsPanel = CreatePanel("SettingsPanel", canvasObj.transform, new Color(0.05f, 0.05f, 0.08f, 0.95f));
            settingsPanel.SetActive(false);
            RectTransform settingsRect = settingsPanel.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.5f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.5f);
            settingsRect.sizeDelta = new Vector2(500, 450);

            CreateText("PanelTitle", "SETTINGS", settingsPanel.transform, 32, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(400, 50));

            // Volume Section
            CreateText("VolumeLabel", "Sound Volume", settingsPanel.transform, 20, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(400, 30));
            Slider volSlider = CreateSlider("VolumeSlider", settingsPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(350, 20));

            // Resolution Section
            CreateText("ResolutionLabel", "Window Resolution", settingsPanel.transform, 20, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(400, 30));
            TMP_Dropdown resDropdown = CreateDropdown("ResolutionDropdown", settingsPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -270), new Vector2(350, 40));

            // Close Button
            Button closeBtn = CreateButton("CloseSettingsButton", "Save & Close", settingsPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(250, 50));

            // 4. Bind manager variables via Reflection/SerializedProperties
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("startGameButton").objectReferenceValue = playBtn;
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            so.FindProperty("closeSettingsButton").objectReferenceValue = closeBtn;
            so.FindProperty("exitButton").objectReferenceValue = exitBtn;
            so.FindProperty("volumeSlider").objectReferenceValue = volSlider;
            so.FindProperty("resolutionDropdown").objectReferenceValue = resDropdown;
            so.ApplyModifiedProperties();

            // Bind Settings toggle persistently in Editor
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(settingsBtn.onClick, settingsPanel.SetActive, true);
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(closeBtn.onClick, settingsPanel.SetActive, false);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("IntroScene UI built successfully!");
        }

        private static void BuildLobbyScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");

            // 1. Get or Create LobbyManager GameObject
            GameObject managerObj = Object.FindAnyObjectByType<UI.LobbyManager>()?.gameObject;
            if (managerObj == null)
            {
                managerObj = new GameObject("LobbyManager", typeof(UI.LobbyManager));
            }
            UI.LobbyManager manager = managerObj.GetComponent<UI.LobbyManager>();

            // 2. Set up Canvas
            GameObject canvasObj = GetOrCreateCanvas("LobbyScene");
            GameObject mainPanel = CreatePanel("MainPanel", canvasObj.transform, new Color(0.08f, 0.08f, 0.12f, 1f));

            // 3. Profile Nickname Panel (Top Aligned)
            GameObject profilePanel = CreatePanel("ProfilePanel", mainPanel.transform, new Color(0.12f, 0.12f, 0.16f, 0.8f));
            RectTransform profileRect = profilePanel.GetComponent<RectTransform>();
            profileRect.anchorMin = new Vector2(0f, 1f);
            profileRect.anchorMax = new Vector2(1f, 1f);
            profileRect.anchoredPosition = new Vector2(0, -60);
            profileRect.sizeDelta = new Vector2(0, 100);

            CreateText("NicknameLabel", "PLAYER PROFILE", profilePanel.transform, 20, Color.cyan, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(100, 0), new Vector2(250, 40));
            TMP_InputField nickInput = CreateInputField("NicknameInputField", "Enter nickname...", profilePanel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(380, 0), new Vector2(400, 50));

            // 4. Matchmaking Controls Panel (Center Alignment)
            GameObject matchPanel = CreatePanel("MatchmakingPanel", mainPanel.transform, new Color(0.12f, 0.12f, 0.16f, 0.5f));
            RectTransform matchRect = matchPanel.GetComponent<RectTransform>();
            matchRect.anchorMin = new Vector2(0f, 0.5f);
            matchRect.anchorMax = new Vector2(1f, 0.5f);
            matchRect.anchoredPosition = new Vector2(0, 150);
            matchRect.sizeDelta = new Vector2(0, 150);

            CreateText("CreateLabel", "ROOM SEARCH & MATCHMAKING", matchPanel.transform, 20, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(500, 30));
            TMP_InputField roomInput = CreateInputField("RoomNameInputField", "Enter Room Name...", matchPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150, -20), new Vector2(400, 50));
            
            Button createRoomBtn = CreateButton("CreateRoomButton", "Create Room", matchPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, -20), new Vector2(180, 50));
            Button randomJoinBtn = CreateButton("RandomJoinButton", "Quick Match", matchPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320, -20), new Vector2(180, 50));

            // 5. Room List Grid Panel (Bottom Half)
            GameObject roomListPanel = CreatePanel("RoomListPanel", mainPanel.transform, new Color(0.1f, 0.1f, 0.14f, 0.7f));
            RectTransform listRect = roomListPanel.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.5f, 0f);
            listRect.anchorMax = new Vector2(0.5f, 0f);
            listRect.anchoredPosition = new Vector2(0, 240);
            listRect.sizeDelta = new Vector2(900, 350);

            CreateText("ListTitle", "ACTIVE ROOM LOBBIES", roomListPanel.transform, 22, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(500, 30));

            ScrollRect scrollRect = CreateScrollRect("ScrollRect", roomListPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0, -30), new Vector2(-40, -100));
            Transform container = scrollRect.content;
            
            // Adjust scroll content layout
            VerticalLayoutGroup contentVlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 10;
            contentVlg.padding = new RectOffset(10, 10, 10, 10);
            contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            
            ContentSizeFitter csf = container.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text emptyText = CreateText("EmptyListText", "No active game sessions. Launch one above!", scrollRect.transform, 20, new Color(0.6f, 0.6f, 0.7f, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Button backBtn = CreateButton("BackToMenuButton", "Main Menu", mainPanel.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(100, 60), new Vector2(200, 50));

            // Create RoomListItem Entry Prefab
            GameObject prefabRoot = new GameObject("RoomEntryPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UI.RoomListItem));
            prefabRoot.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.95f);
            RectTransform pr = prefabRoot.GetComponent<RectTransform>();
            pr.sizeDelta = new Vector2(800, 70);

            TMP_Text roomNameTxt = CreateText("RoomNameText", "My Room", prefabRoot.transform, 20, Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120, 0), new Vector2(200, 40));
            roomNameTxt.alignment = TextAlignmentOptions.Left;

            TMP_Text hostNameTxt = CreateText("MasterNameText", "Host: Guest", prefabRoot.transform, 18, new Color(0.7f, 0.7f, 0.8f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(350, 0), new Vector2(200, 40));
            hostNameTxt.alignment = TextAlignmentOptions.Left;

            TMP_Text playerCntTxt = CreateText("PlayerCountText", "1/2", prefabRoot.transform, 18, Color.white, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220, 0), new Vector2(80, 40));

            Button joinBtn = CreateButton("JoinButton", "JOIN", prefabRoot.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-70, 0), new Vector2(100, 45));

            // Bind RoomListItem script fields
            UI.RoomListItem itemScript = prefabRoot.GetComponent<UI.RoomListItem>();
            SerializedObject itemSo = new SerializedObject(itemScript);
            itemSo.FindProperty("roomNameText").objectReferenceValue = roomNameTxt;
            itemSo.FindProperty("masterNameText").objectReferenceValue = hostNameTxt;
            itemSo.FindProperty("playerCountText").objectReferenceValue = playerCntTxt;
            itemSo.FindProperty("joinButton").objectReferenceValue = joinBtn;
            itemSo.ApplyModifiedProperties();

            // Save Prefab
            GameObject entryPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, "Assets/Prefabs/RoomEntryPrefab.prefab");
            DestroyImmediate(prefabRoot);

            // Bind LobbyManager fields
            SerializedObject lobbySo = new SerializedObject(manager);
            lobbySo.FindProperty("nicknameInputField").objectReferenceValue = nickInput;
            lobbySo.FindProperty("roomNameInputField").objectReferenceValue = roomInput;
            lobbySo.FindProperty("createRoomButton").objectReferenceValue = createRoomBtn;
            lobbySo.FindProperty("randomJoinButton").objectReferenceValue = randomJoinBtn;
            lobbySo.FindProperty("roomListContainer").objectReferenceValue = container;
            lobbySo.FindProperty("roomListItemPrefab").objectReferenceValue = entryPrefab;
            lobbySo.FindProperty("emptyListText").objectReferenceValue = emptyText;
            lobbySo.FindProperty("backToMenuButton").objectReferenceValue = backBtn;
            lobbySo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("LobbyScene UI built successfully!");
        }

        private static void BuildRoomLobbyScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/RoomLobbyScene.unity");

            // 1. Get or Create RoomLobbyManager
            GameObject managerObj = Object.FindAnyObjectByType<UI.RoomLobbyManager>()?.gameObject;
            if (managerObj == null)
            {
                managerObj = new GameObject("RoomLobbyManager", typeof(UI.RoomLobbyManager), typeof(PhotonView));
            }
            UI.RoomLobbyManager manager = managerObj.GetComponent<UI.RoomLobbyManager>();

            // 2. Set up Canvas
            GameObject canvasObj = GetOrCreateCanvas("RoomLobbyScene");
            GameObject mainPanel = CreatePanel("MainPanel", canvasObj.transform, new Color(0.08f, 0.08f, 0.12f, 1f));

            // Room Title Banner
            TMP_Text roomTitle = CreateText("RoomTitle", "Room: Room_1234", mainPanel.transform, 32, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(600, 50));

            // 3. Layout Split Containers
            GameObject layoutContainer = CreatePanel("LayoutContainer", mainPanel.transform, Color.clear);
            RectTransform layoutRect = layoutContainer.GetComponent<RectTransform>();
            layoutRect.anchorMin = new Vector2(0f, 0f);
            layoutRect.anchorMax = new Vector2(1f, 1f);
            layoutRect.sizeDelta = new Vector2(-160, -320); // Border padding
            layoutRect.anchoredPosition = new Vector2(0, -30);

            // Columns
            GameObject leftCol = CreatePanel("LeftColumn_Master", layoutContainer.transform, new Color(0.12f, 0.12f, 0.18f, 0.8f));
            RectTransform leftRect = leftCol.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0f, 0.5f);
            leftRect.anchorMax = new Vector2(0.25f, 0.5f);
            leftRect.anchoredPosition = new Vector2(200 - 960/4, 0); // Correct layout split
            leftRect.sizeDelta = new Vector2(0, 400);

            CreateText("HostLabel", "HOST", leftCol.transform, 22, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(200, 30));
            TMP_Text masterNick = CreateText("MasterNicknameText", "Searching host...", leftCol.transform, 20, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject rightCol = CreatePanel("RightColumn_Guest", layoutContainer.transform, new Color(0.12f, 0.12f, 0.18f, 0.8f));
            RectTransform rightRect = rightCol.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.75f, 0.5f);
            rightRect.anchorMax = new Vector2(1f, 0.5f);
            rightRect.anchoredPosition = new Vector2(-200 + 960/4, 0);
            rightRect.sizeDelta = new Vector2(0, 400);

            CreateText("GuestLabel", "GUEST", rightCol.transform, 22, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(200, 30));
            TMP_Text guestNick = CreateText("GuestNicknameText", "Waiting for Player...", rightCol.transform, 20, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(200, 40));
            TMP_Text guestReady = CreateText("GuestReadyText", "NOT READY", rightCol.transform, 20, Color.red, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(200, 40));

            // 4. Center Area Panels
            GameObject centerArea = CreatePanel("CenterArea", layoutContainer.transform, Color.clear);
            RectTransform centerRect = centerArea.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.28f, 0f);
            centerRect.anchorMax = new Vector2(0.72f, 1f);
            centerRect.sizeDelta = Vector2.zero;

            // Song Selection Panel
            GameObject songSelectionPanel = CreatePanel("SongSelectionPanel", centerArea.transform, new Color(0.1f, 0.1f, 0.12f, 0.8f));
            RectTransform songRect = songSelectionPanel.GetComponent<RectTransform>();
            songRect.anchorMin = new Vector2(0f, 0.5f);
            songRect.anchorMax = new Vector2(1f, 1f);
            songRect.anchoredPosition = new Vector2(0, 20);
            songRect.sizeDelta = new Vector2(0, -40);

            CreateText("SongTitleLabel", "TRACK LIST", songSelectionPanel.transform, 20, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -25), new Vector2(300, 30));
            ScrollRect songScroll = CreateScrollRect("SongScroll", songSelectionPanel.transform, new Vector2(0f, 0f), new Vector2(0.6f, 1f), new Vector2(15, -45), new Vector2(-40, -100));
            
            // Adjust song list layouts
            VerticalLayoutGroup songVlg = songScroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
            songVlg.spacing = 8;
            songVlg.childAlignment = TextAnchor.UpperCenter;
            songVlg.childForceExpandWidth = true;
            songVlg.childForceExpandHeight = false;

            ContentSizeFitter songCsf = songScroll.content.gameObject.AddComponent<ContentSizeFitter>();
            songCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text songDetails = CreateText("SelectedSongDetailsText", "Track Info Panel", songSelectionPanel.transform, 18, Color.white, new Vector2(0.6f, 0f), new Vector2(1f, 1f), new Vector2(-15, -45), new Vector2(-20, -100));
            songDetails.alignment = TextAlignmentOptions.TopLeft;

            // Song ListItem Prefab setup
            GameObject songListItem = new GameObject("SongListItemPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            songListItem.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            songListItem.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 45);
            CreateText("Text", "Song - Artist", songListItem.transform, 16, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject songEntryPrefab = PrefabUtility.SaveAsPrefabAsset(songListItem, "Assets/Prefabs/SongListItemPrefab.prefab");
            DestroyImmediate(songListItem);

            // Chat Panel (Bottom half of Center Panel)
            GameObject chatPanel = CreatePanel("ChatPanel", centerArea.transform, new Color(0.1f, 0.1f, 0.12f, 0.8f));
            RectTransform chatRect = chatPanel.GetComponent<RectTransform>();
            chatRect.anchorMin = new Vector2(0f, 0f);
            chatRect.anchorMax = new Vector2(1f, 0.5f);
            chatRect.anchoredPosition = new Vector2(0, -20);
            chatRect.sizeDelta = new Vector2(0, -40);

            ScrollRect chatScroll = CreateScrollRect("ChatScroll", chatPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0, 10), new Vector2(-30, -90));
            TMP_Text chatLog = CreateText("ChatLogText", "--- Chat Joined ---", chatScroll.content, 18, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            chatLog.alignment = TextAlignmentOptions.BottomLeft;
            
            // Adjust chat log alignments
            ContentSizeFitter chatCsf = chatScroll.content.gameObject.AddComponent<ContentSizeFitter>();
            chatCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_InputField chatInput = CreateInputField("ChatInputField", "Type a message...", chatPanel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-50, 30), new Vector2(-120, 40));

            // Bottom Command Actions
            GameObject actionsPanel = CreatePanel("BottomActionBar", mainPanel.transform, Color.clear);
            RectTransform actionRect = actionsPanel.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.5f, 0f);
            actionRect.anchorMax = new Vector2(0.5f, 0f);
            actionRect.anchoredPosition = new Vector2(0, 80);
            actionRect.sizeDelta = new Vector2(1000, 80);

            Button leaveBtn = CreateButton("LeaveRoomButton", "Leave Room", actionsPanel.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120, 0), new Vector2(220, 60));
            Button readyBtn = CreateButton("ReadyButton", "Ready", actionsPanel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120, 0), new Vector2(220, 60));
            Button startBtn = CreateButton("StartGameButton", "START GAME", actionsPanel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120, 0), new Vector2(220, 60));

            // Create some sample mock SongData scriptable objects so the list loads properly!
            List<Gameplay.SongData> mockSongs = new List<Gameplay.SongData>();
            string[] guids = AssetDatabase.FindAssets("t:SongData");
            foreach (string g in guids)
            {
                var sd = AssetDatabase.LoadAssetAtPath<Gameplay.SongData>(AssetDatabase.GUIDToAssetPath(g));
                if (sd != null) mockSongs.Add(sd);
            }

            // Bind RoomLobbyManager fields
            SerializedObject lobbySo = new SerializedObject(manager);
            lobbySo.FindProperty("roomNameText").objectReferenceValue = roomTitle;
            lobbySo.FindProperty("masterNicknameText").objectReferenceValue = masterNick;
            lobbySo.FindProperty("guestNicknameText").objectReferenceValue = guestNick;
            lobbySo.FindProperty("guestReadyStatusText").objectReferenceValue = guestReady;
            lobbySo.FindProperty("songListContainer").objectReferenceValue = songScroll.content;
            lobbySo.FindProperty("songListItemPrefab").objectReferenceValue = songEntryPrefab;
            lobbySo.FindProperty("selectedSongDetailsText").objectReferenceValue = songDetails;
            lobbySo.FindProperty("chatInputField").objectReferenceValue = chatInput;
            lobbySo.FindProperty("chatLogText").objectReferenceValue = chatLog;
            lobbySo.FindProperty("chatScrollRect").objectReferenceValue = chatScroll;
            lobbySo.FindProperty("leaveButton").objectReferenceValue = leaveBtn;
            lobbySo.FindProperty("readyButton").objectReferenceValue = readyBtn;
            lobbySo.FindProperty("startButton").objectReferenceValue = startBtn;

            // Populate sample list if empty
            if (mockSongs.Count > 0)
            {
                var songsProp = lobbySo.FindProperty("availableSongs");
                songsProp.arraySize = mockSongs.Count;
                for (int idx = 0; idx < mockSongs.Count; idx++)
                {
                    songsProp.GetArrayElementAtIndex(idx).objectReferenceValue = mockSongs[idx];
                }
            }
            lobbySo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("RoomLobbyScene UI built successfully!");
        }

        private static void BuildInGameScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/InGameScene.unity");

            // 1. Get or Create Managers
            GameObject gameManagerObj = Object.FindAnyObjectByType<Gameplay.GameSyncManager>()?.gameObject;
            if (gameManagerObj == null)
            {
                gameManagerObj = new GameObject("GameSyncManager", typeof(Gameplay.GameSyncManager), typeof(PhotonView));
            }
            Gameplay.GameSyncManager syncMgr = gameManagerObj.GetComponent<Gameplay.GameSyncManager>();

            GameObject rhythmObj = Object.FindAnyObjectByType<Gameplay.RhythmGameplay>()?.gameObject;
            if (rhythmObj == null)
            {
                rhythmObj = new GameObject("RhythmGameplay", typeof(Gameplay.RhythmGameplay), typeof(AudioSource));
            }
            Gameplay.RhythmGameplay gameplay = rhythmObj.GetComponent<Gameplay.RhythmGameplay>();

            // 2. Canvas & Playfield Panels
            GameObject canvasObj = GetOrCreateCanvas("InGameScene");
            GameObject mainPanel = CreatePanel("MainPanel", canvasObj.transform, new Color(0.05f, 0.05f, 0.08f, 1f));

            // Side-by-Side Playfields
            GameObject playfield = CreatePanel("PlayfieldContainer", mainPanel.transform, Color.clear);
            
            // Left Playfield (My side)
            GameObject p1Field = CreatePanel("Player1Field", playfield.transform, new Color(0.1f, 0.1f, 0.15f, 0.6f));
            RectTransform p1Rect = p1Field.GetComponent<RectTransform>();
            p1Rect.anchorMin = new Vector2(0f, 0f);
            p1Rect.anchorMax = new Vector2(0.5f, 1f);
            p1Rect.sizeDelta = new Vector2(-40, -100);
            p1Rect.anchoredPosition = new Vector2(20, 0);

            // Right Playfield (Opponent side)
            GameObject p2Field = CreatePanel("Player2Field", playfield.transform, new Color(0.1f, 0.1f, 0.15f, 0.3f)); // Slightly dimmer
            RectTransform p2Rect = p2Field.GetComponent<RectTransform>();
            p2Rect.anchorMin = new Vector2(0.5f, 0f);
            p2Rect.anchorMax = new Vector2(1f, 1f);
            p2Rect.sizeDelta = new Vector2(-40, -100);
            p2Rect.anchoredPosition = new Vector2(-20, 0);

            // Add Judgment lines at the bottom of both playfields
            GameObject judgmentLineP1 = CreatePanel("JudgmentLine", p1Field.transform, Color.cyan);
            RectTransform jl1 = judgmentLineP1.GetComponent<RectTransform>();
            jl1.anchorMin = new Vector2(0f, 0f);
            jl1.anchorMax = new Vector2(1f, 0f);
            jl1.anchoredPosition = new Vector2(0, 100); // 100px from bottom (relates to judgmentYPosition = -4)
            jl1.sizeDelta = new Vector2(0, 8);

            GameObject judgmentLineP2 = CreatePanel("JudgmentLine", p2Field.transform, Color.gray);
            RectTransform jl2 = judgmentLineP2.GetComponent<RectTransform>();
            jl2.anchorMin = new Vector2(0f, 0f);
            jl2.anchorMax = new Vector2(1f, 0f);
            jl2.anchoredPosition = new Vector2(0, 100);
            jl2.sizeDelta = new Vector2(0, 8);

            // Spawning visual lanes
            for (int i = 0; i < 6; i++)
            {
                // Drawing lane division marks
                GameObject lP1 = CreatePanel($"LaneLine_{i}", p1Field.transform, new Color(0.3f, 0.3f, 0.4f, 0.5f));
                RectTransform lr1 = lP1.GetComponent<RectTransform>();
                lr1.anchorMin = new Vector2((float)i / 6f, 0f);
                lr1.anchorMax = new Vector2((float)i / 6f, 1f);
                lr1.sizeDelta = new Vector2(2, 0);

                GameObject lP2 = CreatePanel($"LaneLine_{i}", p2Field.transform, new Color(0.3f, 0.3f, 0.4f, 0.2f));
                RectTransform lr2 = lP2.GetComponent<RectTransform>();
                lr2.anchorMin = new Vector2((float)i / 6f, 0f);
                lr2.anchorMax = new Vector2((float)i / 6f, 1f);
                lr2.sizeDelta = new Vector2(2, 0);
            }

            // Setup note sprites (colored circles)
            GameObject p1Note = new GameObject("MyNotePrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            p1Note.GetComponent<Image>().color = Color.cyan;
            p1Note.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

            GameObject p2Note = new GameObject("OpponentNotePrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            p2Note.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.8f, 0.8f);
            p2Note.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

            GameObject p1NotePrefab = PrefabUtility.SaveAsPrefabAsset(p1Note, "Assets/Prefabs/MyNotePrefab.prefab");
            GameObject p2NotePrefab = PrefabUtility.SaveAsPrefabAsset(p2Note, "Assets/Prefabs/OpponentNotePrefab.prefab");
            DestroyImmediate(p1Note);
            DestroyImmediate(p2Note);

            // 3. Realtime Score Panel (Top Center)
            GameObject scorePanel = CreatePanel("RealtimeScorePanel", mainPanel.transform, new Color(0.12f, 0.12f, 0.16f, 0.8f));
            RectTransform scoreRect = scorePanel.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0.5f, 1f);
            scoreRect.anchorMax = new Vector2(0.5f, 1f);
            scoreRect.anchoredPosition = new Vector2(0, -60);
            scoreRect.sizeDelta = new Vector2(500, 100);

            TMP_Text myScoreTxt = CreateText("MyScoreText", "Player 1\n<b>0</b>", scorePanel.transform, 18, Color.cyan, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80, 0), new Vector2(150, 80));
            TMP_Text oppScoreTxt = CreateText("OpponentScoreText", "Player 2\n<b>0</b>", scorePanel.transform, 18, Color.red, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-80, 0), new Vector2(150, 80));
            Slider scoreComp = CreateSlider("ScoreComparisonSlider", scorePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(160, 15));
            scoreComp.interactable = false;

            TMP_Text countdown = CreateText("CountdownText", "READY", mainPanel.transform, 80, Color.yellow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 200));

            // Judgment Rating UI (Center overlay)
            TMP_Text comboText = CreateText("ComboText", "0 COMBO", mainPanel.transform, 36, Color.white, new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0, 50), new Vector2(300, 80));
            TMP_Text ratingText = CreateText("RatingText", "PERFECT", mainPanel.transform, 42, Color.cyan, new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(0, 120), new Vector2(300, 80));

            // 4. Result Panel Overlay (Initially Inactive)
            GameObject resultOverlay = CreatePanel("ResultPanelOverlay", canvasObj.transform, new Color(0.04f, 0.04f, 0.06f, 0.98f));
            resultOverlay.SetActive(false);
            UI.ResultManager resultMgr = resultOverlay.AddComponent<UI.ResultManager>();

            CreateText("ResultTitleText", "MATCH RESULT", resultOverlay.transform, 40, Color.cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(500, 60));
            TMP_Text victoryHeader = CreateText("VictoryHeaderText", "YOU WIN", resultOverlay.transform, 64, Color.yellow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180), new Vector2(500, 80));

            // Side-by-side Score Summaries
            TMP_Text myFinalScore = CreateText("MyFinalScoreText", "Host\n<b>1,000,000</b>", resultOverlay.transform, 24, Color.cyan, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180, 20), new Vector2(250, 100));
            TMP_Text oppFinalScore = CreateText("OpponentFinalScoreText", "Guest\n<b>950,000</b>", resultOverlay.transform, 24, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, 20), new Vector2(250, 100));

            // Rematch statuses
            TMP_Text myRevenge = CreateText("MyRevengeStatusText", "Wants Rematch?", resultOverlay.transform, 18, Color.yellow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180, -80), new Vector2(250, 40));
            TMP_Text oppRevenge = CreateText("OpponentRevengeStatusText", "Wants Rematch?", resultOverlay.transform, 18, Color.yellow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(180, -80), new Vector2(250, 40));

            // Action Buttons
            Button revengeBtn = CreateButton("RevengeButton", "Request Revenge", resultOverlay.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150, 80), new Vector2(220, 55));
            Button exitLobbyBtn = CreateButton("ExitToLobbyButton", "Return Lobby", resultOverlay.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150, 80), new Vector2(220, 55));

            // Create local dummy songs to attach to database
            List<Gameplay.SongData> mockSongs = new List<Gameplay.SongData>();
            string[] guids = AssetDatabase.FindAssets("t:SongData");
            foreach (string g in guids)
            {
                var sd = AssetDatabase.LoadAssetAtPath<Gameplay.SongData>(AssetDatabase.GUIDToAssetPath(g));
                if (sd != null) mockSongs.Add(sd);
            }

            // Bind GameSyncManager fields
            SerializedObject syncSo = new SerializedObject(syncMgr);
            syncSo.FindProperty("myScoreText").objectReferenceValue = myScoreTxt;
            syncSo.FindProperty("opponentScoreText").objectReferenceValue = oppScoreTxt;
            syncSo.FindProperty("scoreComparisonSlider").objectReferenceValue = scoreComp;
            syncSo.FindProperty("countdownText").objectReferenceValue = countdown;
            syncSo.FindProperty("resultPanel").objectReferenceValue = resultOverlay;
            
            if (mockSongs.Count > 0)
            {
                var database = syncSo.FindProperty("songDatabase");
                database.arraySize = mockSongs.Count;
                for (int idx = 0; idx < mockSongs.Count; idx++)
                {
                    database.GetArrayElementAtIndex(idx).objectReferenceValue = mockSongs[idx];
                }
            }
            syncSo.ApplyModifiedProperties();

            // Bind RhythmGameplay fields
            SerializedObject rhythmSo = new SerializedObject(gameplay);
            rhythmSo.FindProperty("myNotePrefab").objectReferenceValue = p1NotePrefab;
            rhythmSo.FindProperty("opponentNotePrefab").objectReferenceValue = p2NotePrefab;
            rhythmSo.FindProperty("comboText").objectReferenceValue = comboText;
            rhythmSo.FindProperty("ratingText").objectReferenceValue = ratingText;
            rhythmSo.ApplyModifiedProperties();

            // Bind ResultManager fields
            SerializedObject resSo = new SerializedObject(resultMgr);
            resSo.FindProperty("myFinalScoreText").objectReferenceValue = myFinalScore;
            resSo.FindProperty("opponentFinalScoreText").objectReferenceValue = oppFinalScore;
            resSo.FindProperty("victoryHeaderText").objectReferenceValue = victoryHeader;
            resSo.FindProperty("myRevengeStatusText").objectReferenceValue = myRevenge;
            resSo.FindProperty("opponentRevengeStatusText").objectReferenceValue = oppRevenge;
            resSo.FindProperty("revengeButton").objectReferenceValue = revengeBtn;
            resSo.FindProperty("exitButton").objectReferenceValue = exitLobbyBtn;
            resSo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("InGameScene UI and result overlay built successfully!");
        }

        #endregion
    }
}
#endif
