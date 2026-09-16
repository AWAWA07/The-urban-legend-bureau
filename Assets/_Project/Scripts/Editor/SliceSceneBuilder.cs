using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Systems;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 첫 Vertical Slice 씬(01_Title)을 코드로 구성한다.
    ///
    /// 씬 전환 서비스가 아직 없고 이번 단계에서 만들지 않기로 했으므로,
    /// 타이틀/브리핑/인터넷/결과는 기존 UIService의 화면 스택으로,
    /// 현장은 같은 씬의 월드 오브젝트로 구성한다.
    /// </summary>
    public static class SliceSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/01_Title.unity";
        private const string ActionAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string CatalogPath = "Assets/_Project/Data/Config/GameDataCatalog.asset";
        private const string FontFolder = "Assets/_Project/UI/Fonts/Pretendard/";
        private const string ActionFolder = "Assets/_Project/Data/Actions/";

        private static readonly Color BackColor = new Color(0.06f, 0.06f, 0.09f);
        private static readonly Color PanelColor = new Color(0.11f, 0.12f, 0.17f, 0.97f);
        private static readonly Color PopupDim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color PopupBox = new Color(0.13f, 0.11f, 0.14f, 0.99f);
        private static readonly Color ButtonColor = new Color(0.23f, 0.27f, 0.36f, 1f);
        private static readonly Color TextColor = new Color(0.93f, 0.93f, 0.96f);
        private static readonly Color DimTextColor = new Color(0.68f, 0.70f, 0.78f);
        private static readonly Color AccentColor = new Color(0.86f, 0.74f, 0.48f);
        private static readonly Color WarnColor = new Color(0.92f, 0.55f, 0.50f);

        [MenuItem("UrbanLegendBureau/Dev/Build 01_Title Slice Scene")]
        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.4f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BackColor;
            }

            // --- GameRoot ---
            var rootGo = new GameObject("GameRoot");
            var gameRoot = rootGo.AddComponent<GameRoot>();
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(ActionAssetPath);
            var catalog = AssetDatabase.LoadAssetAtPath<UrbanLegendBureau.Data.GameDataCatalogSO>(CatalogPath);
            var rootSo = new SerializedObject(gameRoot);
            rootSo.Update();
            rootSo.FindProperty("_inputActions").objectReferenceValue = actions;
            rootSo.FindProperty("_gameDataCatalog").objectReferenceValue = catalog;
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 현장 (월드) ---
            var fieldRoot = new GameObject("FieldRoot_Legend1");
            BuildFieldBackground(fieldRoot.transform);
            var desk = BuildPoint(fieldRoot.transform, "InvestigationPoint_Desk", new Vector2(-4.2f, -1.2f),
                new Vector2(3.0f, 2.0f), new Color(0.55f, 0.42f, 0.30f),
                "field.test.desk", "field.test.desk.result", null);
            var phone = BuildPoint(fieldRoot.transform, "InvestigationPoint_Phone", new Vector2(0f, -1.6f),
                new Vector2(1.1f, 1.9f), new Color(0.35f, 0.60f, 0.72f),
                "field.test.phone", "field.test.phone.result", "clue_test_001");
            var wall = BuildPoint(fieldRoot.transform, "InvestigationPoint_Wall", new Vector2(4.2f, 1.4f),
                new Vector2(2.6f, 1.6f), new Color(0.48f, 0.30f, 0.34f),
                "field.test.wall", "field.test.wall.result", "clue_test_002");   // 오답 규칙의 근거

            // --- 두 번째 사건의 현장 ---
            var fieldRoot2 = new GameObject("FieldRoot_Legend2");
            BuildFieldBackground(fieldRoot2.transform);
            var panel = BuildPoint(fieldRoot2.transform, "InvestigationPoint_Panel", new Vector2(-3.6f, -0.6f),
                new Vector2(1.6f, 2.6f), new Color(0.42f, 0.46f, 0.52f),
                "field.test.panel", "field.test.panel.result", "clue_test_003");
            var mirror = BuildPoint(fieldRoot2.transform, "InvestigationPoint_Mirror", new Vector2(3.4f, 0.4f),
                new Vector2(2.4f, 3.2f), new Color(0.30f, 0.38f, 0.42f),
                "field.test.mirror", "field.test.mirror.result", null);

            // --- 지점별 조사 방법과 해금 조건 (17단계) ---
            // 지점이 어떤 조사 방법을 허용하는지는 여기서 정한다. 모든 지점에서 모든 행동을 할 수 없다.
            ConfigurePoint(desk, "point_desk",
                new[] { "action_test_002", "action_test_006" }, null, false, CaseStep.Started);
            ConfigurePoint(phone, "point_phone",
                new[] { "action_test_006" }, null, false, CaseStep.Started);
            // 벽은 책상에서 얻은 단서가 있어야 조사할 수 있다.
            ConfigurePoint(wall, "point_wall",
                new[] { "action_test_003" }, new[] { "clue_test_001" }, false, CaseStep.Started);
            ConfigurePoint(panel, "point_panel",
                new[] { "action_test_005" }, null, false, CaseStep.Started);
            ConfigurePoint(mirror, "point_mirror",
                new[] { "action_test_006" }, null, false, CaseStep.Started);

            // --- 실제 사건 1: 막차의 빈자리 (20단계) ---
            var fieldRootSubway = new GameObject("FieldRoot_Subway");
            BuildFieldBackground(fieldRootSubway.transform);
            var seat = BuildPoint(fieldRootSubway.transform, "InvestigationPoint_SubwaySeat", new Vector2(-5.0f, -1.4f),
                new Vector2(2.6f, 1.8f), new Color(0.32f, 0.36f, 0.48f),
                "field.subway.seat", "field.subway.seat", null);
            var window = BuildPoint(fieldRootSubway.transform, "InvestigationPoint_SubwayWindow", new Vector2(-1.4f, 1.6f),
                new Vector2(3.2f, 2.0f), new Color(0.26f, 0.42f, 0.46f),
                "field.subway.window", "field.subway.window", null);
            var cctv = BuildPoint(fieldRootSubway.transform, "InvestigationPoint_SubwayCctv", new Vector2(2.6f, 2.6f),
                new Vector2(1.2f, 1.0f), new Color(0.46f, 0.40f, 0.30f),
                "field.subway.cctv", "field.subway.cctv", null);
            var platform = BuildPoint(fieldRootSubway.transform, "InvestigationPoint_SubwayPlatform", new Vector2(4.6f, -1.8f),
                new Vector2(3.4f, 1.6f), new Color(0.38f, 0.32f, 0.36f),
                "field.subway.platform", "field.subway.platform", null);

            // 좌석에서 얻은 진술이 있어야 영상과 대조할 마음이 든다.
            ConfigurePoint(seat, "point_subway_seat",
                new[] { "action_subway_seat_search", "action_subway_photo" }, null, false, CaseStep.Started);
            ConfigurePoint(window, "point_subway_window",
                new[] { "action_subway_photo", "action_subway_window_trace" }, null, false, CaseStep.Started);
            ConfigurePoint(cctv, "point_subway_cctv",
                new[] { "action_subway_cctv_inspect", "action_subway_photo" },
                new[] { "clue_subway_001" }, false, CaseStep.Started);
            ConfigurePoint(platform, "point_subway_platform",
                new[] { "action_subway_platform_search", "action_subway_platform_trace" }, null, false, CaseStep.Started);

            var fieldGo = new GameObject("FieldController");
            var field = fieldGo.AddComponent<FieldController>();
            var fieldSo = new SerializedObject(field);
            fieldSo.Update();
            var groups = fieldSo.FindProperty("_fieldGroups");
            groups.arraySize = 3;
            var g0 = groups.GetArrayElementAtIndex(0);
            g0.FindPropertyRelative("legendId").stringValue = "legend_test_001";
            g0.FindPropertyRelative("root").objectReferenceValue = fieldRoot;
            var g1 = groups.GetArrayElementAtIndex(1);
            g1.FindPropertyRelative("legendId").stringValue = "legend_test_002";
            g1.FindPropertyRelative("root").objectReferenceValue = fieldRoot2;
            var g2 = groups.GetArrayElementAtIndex(2);
            g2.FindPropertyRelative("legendId").stringValue = "legend_subway_last_train";
            g2.FindPropertyRelative("root").objectReferenceValue = fieldRootSubway;
            fieldSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 화면 ---
            var title = BuildPanelScreen("Screen_Title", UILayer.Screen, out var titleButtons, true);
            var bureau = BuildPanelScreen("Screen_Bureau", UILayer.Screen, out var bureauButtons, true);
            var caseList = BuildCaseListScreen("Screen_CaseList");
            var actionList = BuildActionListScreen("Screen_Actions", out var actionButtons);
            var ruleList = BuildRuleListScreen("Screen_Rules", out var ruleScreenButtons);
            var internetList = BuildInternetListScreen("Screen_InternetList", out var internetButtons);
            var internetPage = BuildInternetPageScreen("Screen_InternetPage", out var pageButtons);
            var fieldHud = BuildFieldHudScreen("Screen_FieldHud", out var fieldButtons);
            var exorcism = BuildPanelScreen("Screen_Exorcism", UILayer.Screen, out var exorcismButtons, true);
            var result = BuildPanelScreen("Screen_Result", UILayer.Screen, out var resultButtons, true);
            var help = BuildPanelScreen("Screen_Help", UILayer.Screen, out var helpButtons, true);
            var settings = BuildSettingsScreen("Screen_Settings", out var settingsButtons);
            var dialogue = BuildDialogueScreen("Screen_Dialogue", true);
            var talk = BuildDialogueScreen("Screen_TutorialTalk", false);
            var desktop = BuildDesktopScreen("Screen_Desktop");
            var community = BuildCommunityScreen("Screen_Community");
            var cluePopup = BuildPopupScreen("Popup_Clue", out var clueButtons);
            var rulePopup = BuildPopupScreen("Popup_Rule", out var ruleButtons);
            var warningPopup = BuildPopupScreen("Popup_SpreadWarning", out var warningButtons);

            var btnStart = CreateButton(titleButtons, "Btn_Start", "ui.title.start");
            var btnHelp = CreateButton(titleButtons, "Btn_Help", "ui.title.help");
            var btnSettings = CreateButton(titleButtons, "Btn_Settings", "ui.title.settings");
            var btnQuit = CreateButton(titleButtons, "Btn_Quit", "ui.title.quit");
            // 설명 화면에서 튜토리얼을 다시 돌릴 수 있게 한다. 저장본을 지우지 않아도 처음부터 볼 수 있다.
            var btnHelpTutorial = CreateButton(helpButtons, "Btn_HelpTutorial", "ui.title.tutorial_replay");
            var btnHelpBack = CreateButton(helpButtons, "Btn_HelpBack", "ui.common.back");
            var btnSettingsBack = CreateButton(settingsButtons, "Btn_SettingsBack", "ui.common.back");
            // 타이틀은 버튼이 네 개라 기본 버튼 폭(400)으로는 줄을 넘는다.
            // 폭을 줄이고 줄 자체를 넓혀 1920 기준 가운데에 모두 들어오게 한다.
            StyleTitleScreen(title, new[] { btnStart, btnHelp, btnSettings, btnQuit }, titleButtons);
            var btnActions = CreateButton(bureauButtons, "Btn_Actions", "ui.action.btn_actions");
            var btnActionsBack = CreateButton(actionButtons, "Btn_ActionsBack", "ui.action.btn_back");
            var btnInternet = CreateButton(bureauButtons, "Btn_Internet", "ui.slice.btn_internet");
            var btnField = CreateButton(internetButtons, "Btn_EnterField", "ui.slice.btn_enter_field");
            var btnCensor = CreateButton(pageButtons, "Btn_Censor", "ui.net.btn_censor");
            var btnPageBack = CreateButton(pageButtons, "Btn_PageBack", "ui.net.btn_back");
            var btnDeduce = CreateButton(fieldButtons, "Btn_Deduce", "ui.rule.btn_deduce");
            var btnRulesBack = CreateButton(ruleScreenButtons, "Btn_RulesBack", "ui.rule.btn_back");
            var btnFieldDone = CreateButton(fieldButtons, "Btn_FieldDone", "ui.field.btn_done");
            var btnSeal = CreateButton(exorcismButtons, "Btn_Seal", "ui.seal.btn_seal");
            var btnSealOk = CreateButton(exorcismButtons, "Btn_SealConfirm", "ui.common.ok");
            var btnBack = CreateButton(resultButtons, "Btn_BackToTitle", "ui.slice.btn_back_to_title");
            var btnClueOk = CreateButton(clueButtons, "Btn_ClueOk", "ui.common.ok");
            var btnRuleOk = CreateButton(ruleButtons, "Btn_RuleOk", "ui.common.ok");
            var btnWarningOk = CreateButton(warningButtons, "Btn_WarningOk", "ui.common.ok");

            // --- 진행 담당 ---
            var directorGo = new GameObject("CaseDirector");
            var director = directorGo.AddComponent<CaseDirector>();
            var dso = new SerializedObject(director);
            dso.Update();
            dso.FindProperty("_titleScreen").objectReferenceValue = title;
            dso.FindProperty("_helpScreen").objectReferenceValue = help;
            dso.FindProperty("_settingsScreen").objectReferenceValue = settings;
            dso.FindProperty("_caseListScreen").objectReferenceValue = caseList;
            dso.FindProperty("_bureauScreen").objectReferenceValue = bureau;
            dso.FindProperty("_actionListScreen").objectReferenceValue = actionList;
            dso.FindProperty("_internetListScreen").objectReferenceValue = internetList;
            dso.FindProperty("_internetPageScreen").objectReferenceValue = internetPage;
            dso.FindProperty("_fieldHudScreen").objectReferenceValue = fieldHud;
            dso.FindProperty("_cluePopupScreen").objectReferenceValue = cluePopup;
            dso.FindProperty("_rulePopupScreen").objectReferenceValue = rulePopup;
            dso.FindProperty("_ruleListScreen").objectReferenceValue = ruleList;
            dso.FindProperty("_warningPopupScreen").objectReferenceValue = warningPopup;
            dso.FindProperty("_exorcismScreen").objectReferenceValue = exorcism;
            dso.FindProperty("_sealButton").objectReferenceValue = btnSeal;
            dso.FindProperty("_sealConfirmButton").objectReferenceValue = btnSealOk;
            dso.FindProperty("_resultScreen").objectReferenceValue = result;
            dso.FindProperty("_field").objectReferenceValue = field;
            dso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(btnStart.GetComponent<Button>().onClick, director.OnStartClicked);
            UnityEventTools.AddPersistentListener(btnHelp.GetComponent<Button>().onClick, director.OnOpenHelpClicked);
            UnityEventTools.AddPersistentListener(btnSettings.GetComponent<Button>().onClick, director.OnOpenSettingsClicked);
            UnityEventTools.AddPersistentListener(btnQuit.GetComponent<Button>().onClick, director.OnQuitClicked);
            UnityEventTools.AddPersistentListener(btnHelpTutorial.GetComponent<Button>().onClick, director.OnReplayTutorialClicked);
            UnityEventTools.AddPersistentListener(btnHelpBack.GetComponent<Button>().onClick, director.OnBackToTitleFromMenuClicked);
            UnityEventTools.AddPersistentListener(btnSettingsBack.GetComponent<Button>().onClick, director.OnBackToTitleFromMenuClicked);

            // --- 튜토리얼 ---
            var tutorialGo = new GameObject("TutorialDirector");
            var tutorial = tutorialGo.AddComponent<TutorialDirector>();
            var tso = new SerializedObject(tutorial);
            tso.Update();
            tso.FindProperty("_dialogueScreen").objectReferenceValue = dialogue;
            tso.FindProperty("_talkScreen").objectReferenceValue = talk;
            tso.FindProperty("_desktopScreen").objectReferenceValue = desktop;
            tso.FindProperty("_communityScreen").objectReferenceValue = community;
            tso.FindProperty("_caseDirector").objectReferenceValue = director;
            tso.FindProperty("_tutorialPage").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<UrbanLegendBureau.Data.WebPageSO>(
                    "Assets/_Project/Data/WebPages/web_subway_001.asset");
            tso.ApplyModifiedPropertiesWithoutUndo();

            // 만드는 동안 같은 흐름을 몇 번이고 다시 보게 되므로 단축키를 하나 둔다.
            var hotkey = tutorialGo.AddComponent<TutorialHotkey>();
            var hso = new SerializedObject(hotkey);
            hso.Update();
            hso.FindProperty("_tutorial").objectReferenceValue = tutorial;
            hso.ApplyModifiedPropertiesWithoutUndo();

            dso.Update();
            dso.FindProperty("_tutorial").objectReferenceValue = tutorial;
            dso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(btnActions.GetComponent<Button>().onClick, director.OnOpenActionsClicked);
            UnityEventTools.AddPersistentListener(btnActionsBack.GetComponent<Button>().onClick, director.OnActionsBackClicked);
            UnityEventTools.AddPersistentListener(btnInternet.GetComponent<Button>().onClick, director.OnInternetResearchClicked);
            UnityEventTools.AddPersistentListener(btnField.GetComponent<Button>().onClick, director.OnEnterFieldClicked);
            // 상세 화면이 검열 버튼을 직접 숨기고 보여야 하므로 참조를 넘겨 둔다.
            var pso = new SerializedObject(internetPage);
            pso.Update();
            pso.FindProperty("_censorButton").objectReferenceValue = btnCensor.GetComponent<Button>();
            pso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(btnCensor.GetComponent<Button>().onClick, director.OnCensorClicked);
            UnityEventTools.AddPersistentListener(btnPageBack.GetComponent<Button>().onClick, director.OnPageBackClicked);
            UnityEventTools.AddPersistentListener(btnDeduce.GetComponent<Button>().onClick, director.OnOpenRulesClicked);
            UnityEventTools.AddPersistentListener(btnRulesBack.GetComponent<Button>().onClick, director.OnRulesBackClicked);
            UnityEventTools.AddPersistentListener(btnFieldDone.GetComponent<Button>().onClick, director.OnFieldDoneClicked);
            UnityEventTools.AddPersistentListener(btnSeal.GetComponent<Button>().onClick, director.OnSealClicked);
            UnityEventTools.AddPersistentListener(btnSealOk.GetComponent<Button>().onClick, director.OnExorcismConfirmClicked);
            UnityEventTools.AddPersistentListener(btnBack.GetComponent<Button>().onClick, director.OnBackToTitleClicked);
            UnityEventTools.AddPersistentListener(btnClueOk.GetComponent<Button>().onClick, director.OnCluePopupConfirmClicked);
            UnityEventTools.AddPersistentListener(btnRuleOk.GetComponent<Button>().onClick, director.OnRulePopupConfirmClicked);
            UnityEventTools.AddPersistentListener(btnWarningOk.GetComponent<Button>().onClick, director.OnSpreadWarningConfirmClicked);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            var msg = "01_Title 슬라이스 씬 생성 완료: " + ScenePath +
                      "\n  화면 6개 / 조사 지점 3개 (" + desk.name + ", " + phone.name + ", " + wall.name + ")";
            Debug.Log(msg);
            return msg;
        }

        // ------------------------------------------------------------- 현장

        /// <summary>조사 지점에 조사 방법과 해금 조건을 붙인다. 행동 데이터는 ID로 찾아 직접 참조로 넣는다.</summary>
        private static void ConfigurePoint(GameObject pointGo, string pointId,
            string[] actionIds, string[] requiredClueIds, bool requireStep, CaseStep requiredStep)
        {
            var point = pointGo.GetComponent<InvestigationPoint>();
            if (point == null) return;

            var so = new SerializedObject(point);
            so.Update();
            so.FindProperty("_pointId").stringValue = pointId;

            var actions = so.FindProperty("_actions");
            actions.arraySize = actionIds != null ? actionIds.Length : 0;
            for (int i = 0; i < actions.arraySize; i++)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UrbanLegendBureau.Data.InvestigationActionSO>(
                    ActionFolder + actionIds[i] + ".asset");
                if (asset == null) Debug.LogError("[SliceSceneBuilder] 조사 행동을 찾지 못했다: " + actionIds[i]);
                actions.GetArrayElementAtIndex(i).objectReferenceValue = asset;
            }

            var clues = so.FindProperty("_requiredClueIds");
            clues.arraySize = requiredClueIds != null ? requiredClueIds.Length : 0;
            for (int i = 0; i < clues.arraySize; i++)
            {
                clues.GetArrayElementAtIndex(i).stringValue = requiredClueIds[i];
            }

            so.FindProperty("_requireStep").boolValue = requireStep;
            so.FindProperty("_requiredStep").enumValueIndex = (int)requiredStep;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildFieldBackground(Transform parent)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BuiltinSprite();
            sr.color = new Color(0.14f, 0.13f, 0.18f);
            sr.drawMode = SpriteDrawMode.Sliced;
            // 넉넉히 넓게 둔다. 현장 화면은 카메라가 위쪽만 쓰므로 가로가 그만큼 넓어진다.
            // 배경이 좁으면 양옆이 비어 방이 떠 있는 것처럼 보인다.
            sr.size = new Vector2(44f, 10.8f);
            sr.sortingOrder = -10;
        }

        private static GameObject BuildPoint(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, string nameTextId, string resultTextId, string clueId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BuiltinSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = 0;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var point = go.AddComponent<InvestigationPoint>();
            var so = new SerializedObject(point);
            so.Update();
            so.FindProperty("_nameTextId").stringValue = nameTextId;
            so.FindProperty("_resultTextId").stringValue = resultTextId;
            so.FindProperty("_clueId").stringValue = clueId ?? string.Empty;
            so.FindProperty("_renderer").objectReferenceValue = sr;
            so.FindProperty("_normalColor").colorValue = color;
            so.FindProperty("_pressedColor").colorValue = Color.Lerp(color, Color.white, 0.45f);
            so.FindProperty("_investigatedColor").colorValue = Color.Lerp(color, Color.black, 0.45f);
            so.ApplyModifiedPropertiesWithoutUndo();

            // 지점 이름 라벨 (월드 스페이스 TMP)
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, size.y * 0.5f + 0.45f, 0f);
            var label = labelGo.AddComponent<TextMeshPro>();
            label.font = LoadFont(UIFontWeight.Medium);
            label.fontSize = 3.2f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = TextColor;
            label.rectTransform.sizeDelta = new Vector2(6f, 1f);
            label.sortingOrder = 1;

            var localized = labelGo.AddComponent<LocalizedText>();
            var lso = new SerializedObject(localized);
            lso.Update();
            lso.FindProperty("_textId").stringValue = nameTextId;
            lso.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        // ------------------------------------------------------------- 화면

        private static TextPanelScreen BuildPanelScreen(string name, UILayer layer, out Transform buttonRow, bool fullScreen)
        {
            var go = CreatePanel(null, name, fullScreen ? PanelColor : Color.clear);
            StretchFull(go);

            var screen = go.AddComponent<TextPanelScreen>();
            ConfigureScreen(screen, name, layer, true, true);

            var titleText = AddText(go.transform, "Title", 64f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 300f), new Vector2(1500f, 110f), TextAlignmentOptions.Center);
            var bodyText = AddText(go.transform, "Body", 34f, UIFontWeight.Regular, TextColor,
                new Vector2(0f, 40f), new Vector2(1400f, 420f), TextAlignmentOptions.Top);
            var footerText = AddText(go.transform, "Footer", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, -230f), new Vector2(1400f, 70f), TextAlignmentOptions.Center);

            BindScreenTexts(screen, titleText, bodyText, footerText);

            var row = new GameObject("Buttons", typeof(RectTransform));
            row.transform.SetParent(go.transform, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -350f);
            rt.sizeDelta = new Vector2(900f, 110f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            // 버튼이 자기 크기를 유지하게 둔다. childControl을 켜면 preferred 크기가 0이라 납작해진다.
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            buttonRow = row.transform;
            return screen;
        }

        /// <summary>사건 목록 화면.</summary>
        private static CaseListScreen BuildCaseListScreen(string name)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<CaseListScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            var titleText = AddText(go.transform, "Title", 60f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 380f), new Vector2(1500f, 100f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 300f), new Vector2(1500f, 60f), TextAlignmentOptions.Center);

            var listRt = BuildListRoot(go.transform, new Vector2(0f, 230f), new Vector2(1200f, 440f));
            var templateButton = BuildItemTemplate(listRt, new Vector2(1100f, 140f), 28f);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_footerText").objectReferenceValue = footerText;
            so.FindProperty("_listRoot").objectReferenceValue = listRt;
            so.FindProperty("_itemTemplate").objectReferenceValue = templateButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return screen;
        }

        /// <summary>세로 목록 영역을 만든다.</summary>
        private static RectTransform BuildListRoot(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(parent, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = anchoredPosition;
            listRt.sizeDelta = size;

            var layout = listGo.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return listRt;
        }

        /// <summary>복제용 항목 버튼을 만든다. 비활성 상태로 둔다.</summary>
        private static Button BuildItemTemplate(Transform parent, Vector2 size, float fontSize)
        {
            var template = CreatePanel(parent, "ItemTemplate", ButtonColor);
            var button = template.AddComponent<Button>();
            button.targetGraphic = template.GetComponent<Image>();

            var rt = (RectTransform)template.transform;
            rt.sizeDelta = size;

            var label = AddText(template.transform, "ItemLabel", fontSize, UIFontWeight.Medium, TextColor,
                Vector2.zero, size, TextAlignmentOptions.Center);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(20f, 8f);
            lrt.offsetMax = new Vector2(-20f, -8f);

            template.SetActive(false);
            return button;
        }

        /// <summary>인터넷 게시글 목록 화면. 항목 버튼은 템플릿을 복제해 런타임에 만든다.</summary>
        /// <summary>조사 행동 선택 화면. 목록 구성은 인터넷 목록과 같고 결과 표시줄이 하나 더 있다.</summary>
        private static ActionListScreen BuildActionListScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<ActionListScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            var titleText = AddText(go.transform, "Title", 60f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 400f), new Vector2(1500f, 100f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 330f), new Vector2(1500f, 60f), TextAlignmentOptions.Center);
            var statsText = AddText(go.transform, "Stats", 30f, UIFontWeight.SemiBold, AccentColor,
                new Vector2(0f, -250f), new Vector2(1500f, 90f), TextAlignmentOptions.Center);
            var resultText = AddText(go.transform, "Result", 30f, UIFontWeight.Medium, WarnColor,
                new Vector2(0f, -330f), new Vector2(1500f, 80f), TextAlignmentOptions.Center);

            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(go.transform, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = new Vector2(0f, 270f);
            listRt.sizeDelta = new Vector2(1200f, 480f);
            var listLayout = listGo.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 14f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = false;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = false;
            listLayout.childForceExpandHeight = false;

            var template = CreatePanel(listGo.transform, "ItemTemplate", ButtonColor);
            var templateButton = template.AddComponent<Button>();
            templateButton.targetGraphic = template.GetComponent<Image>();
            var trt = (RectTransform)template.transform;
            trt.sizeDelta = new Vector2(1100f, 110f);
            var tLabel = AddText(template.transform, "ItemLabel", 26f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(1060f, 90f), TextAlignmentOptions.Center);
            var tlrt = (RectTransform)tLabel.transform;
            tlrt.anchorMin = Vector2.zero;
            tlrt.anchorMax = Vector2.one;
            tlrt.offsetMin = new Vector2(20f, 6f);
            tlrt.offsetMax = new Vector2(-20f, -6f);
            template.SetActive(false);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_footerText").objectReferenceValue = footerText;
            so.FindProperty("_statsText").objectReferenceValue = statsText;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.FindProperty("_listRoot").objectReferenceValue = listRt;
            so.FindProperty("_itemTemplate").objectReferenceValue = templateButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -420f), new Vector2(900f, 110f));
            return screen;
        }

        /// <summary>
        /// 타이틀 화면을 다듬는다.
        ///
        /// 고치는 것 두 가지:
        ///  1. 버튼 네 개(각 400 + 간격)가 줄 폭 900을 넘어 오른쪽으로 밀려 잘리던 문제.
        ///     HorizontalLayoutGroup은 자식이 줄보다 넓으면 가운데로 모으지 못하고 왼쪽부터 늘어놓는다.
        ///     그래서 버튼을 좁히고 줄을 넓혀 실제로 들어가게 만든다.
        ///  2. 타이틀 아래 문구 제거와 어두운 배경. 본문/꼬리말은 CaseDirector가 비워서 넘긴다.
        /// </summary>
        private static void StyleTitleScreen(TextPanelScreen title, GameObject[] buttons, Transform buttonRow)
        {
            // --- 배경 ---
            var image = title.GetComponent<Image>();
            if (image != null)
            {
                var bg = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/title_background.png");
                if (bg != null)
                {
                    image.sprite = bg;
                    image.color = Color.white;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                }
                else
                {
                    image.color = new Color(0.03f, 0.035f, 0.06f, 1f);
                }
            }

            // --- 제목 ---
            var titleText = title.transform.Find("Text_Title") as RectTransform;
            if (titleText != null)
            {
                titleText.anchoredPosition = new Vector2(0f, 200f);
                titleText.sizeDelta = new Vector2(1600f, 200f);

                var tmp = titleText.GetComponent<TMP_Text>();
                tmp.fontSize = 132f;
                tmp.font = LoadFont(UIFontWeight.Bold);
                tmp.characterSpacing = 6f;

                // 색 번짐(글리치) 흉내. 같은 글자를 청록/붉은색으로 살짝 어긋나게 깔아 둔다.
                CreateTitleGhost(title.transform, "Text_TitleGhostCyan", titleText,
                    new Vector2(-7f, 3f), new Color(0.35f, 0.85f, 1f, 0.34f), -2);
                CreateTitleGhost(title.transform, "Text_TitleGhostRed", titleText,
                    new Vector2(7f, -3f), new Color(1f, 0.28f, 0.34f, 0.30f), -1);
                titleText.SetAsLastSibling();
            }

            // --- 버튼 ---
            var row = (RectTransform)buttonRow;
            row.anchoredPosition = new Vector2(0f, -330f);
            row.sizeDelta = new Vector2(1400f, 110f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 28f;
                layout.childAlignment = TextAnchor.MiddleCenter;
            }

            foreach (var button in buttons)
            {
                ((RectTransform)button.transform).sizeDelta = new Vector2(300f, 96f);
            }

            // 버튼 줄을 마지막으로 올려 배경/유령 글자가 덮지 않게 한다.
            row.SetAsLastSibling();
        }

        /// <summary>제목 뒤에 깔리는 색 번짐 글자. 같은 String ID를 쓰므로 언어가 바뀌어도 따라간다.</summary>
        private static void CreateTitleGhost(Transform parent, string name, RectTransform source,
            Vector2 offset, Color color, int siblingOffset)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = source.anchorMin;
            rt.anchorMax = source.anchorMax;
            rt.pivot = source.pivot;
            rt.anchoredPosition = source.anchoredPosition + offset;
            rt.sizeDelta = source.sizeDelta;

            var src = source.GetComponent<TMP_Text>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = src.font;
            tmp.fontSize = src.fontSize;
            tmp.characterSpacing = src.characterSpacing;
            tmp.alignment = src.alignment;
            tmp.color = color;
            tmp.raycastTarget = false;

            // 표시 문구는 Localization이 채운다. 제목과 같은 ID를 쓴다.
            var localized = go.AddComponent<LocalizedText>();
            var lso = new SerializedObject(localized);
            lso.Update();
            lso.FindProperty("_textId").stringValue = "ui.slice.title";
            lso.ApplyModifiedPropertiesWithoutUndo();

            go.transform.SetSiblingIndex(Mathf.Max(0, source.GetSiblingIndex() + siblingOffset));
        }

        /// <summary>설정 화면. 슬라이더 두 개뿐이다.</summary>
        private static SettingsScreen BuildSettingsScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<SettingsScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            var titleText = AddText(go.transform, "Title", 60f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 300f), new Vector2(1200f, 100f), TextAlignmentOptions.Center);
            var bgmLabel = AddText(go.transform, "BgmLabel", 34f, UIFontWeight.Medium, TextColor,
                new Vector2(-260f, 120f), new Vector2(560f, 60f), TextAlignmentOptions.Left);
            var sfxLabel = AddText(go.transform, "SfxLabel", 34f, UIFontWeight.Medium, TextColor,
                new Vector2(-260f, -40f), new Vector2(560f, 60f), TextAlignmentOptions.Left);

            var bgmSlider = CreateSlider(go.transform, "Slider_Bgm", new Vector2(240f, 120f));
            var sfxSlider = CreateSlider(go.transform, "Slider_Sfx", new Vector2(240f, -40f));

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_bgmLabel").objectReferenceValue = bgmLabel;
            so.FindProperty("_sfxLabel").objectReferenceValue = sfxLabel;
            so.FindProperty("_bgmSlider").objectReferenceValue = bgmSlider;
            so.FindProperty("_sfxSlider").objectReferenceValue = sfxSlider;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -300f), new Vector2(900f, 110f));
            return screen;
        }

        /// <summary>볼륨 슬라이더. UI 기본 구성 요소만 쓴다.</summary>
        private static Slider CreateSlider(Transform parent, string name, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(600f, 40f);

            var slider = go.AddComponent<Slider>();

            var background = CreatePanel(go.transform, "Background", new Color(0.18f, 0.19f, 0.25f, 1f));
            var bgRt = (RectTransform)background.transform;
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var faRt = (RectTransform)fillArea.transform;
            faRt.anchorMin = new Vector2(0f, 0.25f);
            faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.offsetMin = new Vector2(10f, 0f);
            faRt.offsetMax = new Vector2(-10f, 0f);

            var fill = CreatePanel(fillArea.transform, "Fill", AccentColor);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = new Vector2(20f, 0f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var haRt = (RectTransform)handleArea.transform;
            haRt.anchorMin = new Vector2(0f, 0f);
            haRt.anchorMax = new Vector2(1f, 1f);
            haRt.offsetMin = new Vector2(10f, 0f);
            haRt.offsetMax = new Vector2(-10f, 0f);

            var handle = CreatePanel(handleArea.transform, "Handle", TextColor);
            var handleRt = (RectTransform)handle.transform;
            handleRt.sizeDelta = new Vector2(36f, 40f);

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        /// <summary>
        /// 튜토리얼 대화 화면.
        ///
        /// fullScreen이면 검은 배경 위에 두 인물을 세우는 단독 화면이다.
        /// 아니면 아래 화면(커뮤니티)을 가리지 않는 겹침 대화가 된다. 구성은 같다.
        /// </summary>
        private static DialogueScreen BuildDialogueScreen(string name, bool fullScreen)
        {
            var go = CreatePanel(null, name,
                fullScreen ? new Color(0.02f, 0.02f, 0.03f, 1f) : new Color(0f, 0f, 0f, 0f));
            StretchFull(go);

            var screen = go.AddComponent<DialogueScreen>();
            ConfigureScreen(screen, name,
                fullScreen ? UILayer.Screen : UILayer.Popup,
                fullScreen,     // 겹침 대화는 아래 화면을 가리지 않는다
                false,
                !fullScreen);   // 겹침 대화 중에도 아래 화면(괴담넷)을 그대로 쓸 수 있어야 한다

            // 진행 버튼. 마우스 클릭과 터치가 같은 경로로 들어온다.
            //
            // 대화만 있는 화면은 어디를 눌러도 넘어가게 화면 전체를 덮는다.
            // 겹침 대화는 아래 화면(예: 괴담넷)을 만질 수 있어야 하므로 대사 상자 자리만 덮는다.
            // 전체를 덮으면 아래 화면의 누름과 끌기를 전부 가로채 굴러가지 않는다.
            var advanceGo = CreatePanel(go.transform, "Btn_Advance", new Color(0f, 0f, 0f, 0f));
            if (fullScreen)
            {
                StretchFull(advanceGo);
            }
            else
            {
                var advanceRt = (RectTransform)advanceGo.transform;
                advanceRt.anchorMin = new Vector2(0.5f, 0.5f);
                advanceRt.anchorMax = new Vector2(0.5f, 0.5f);
                advanceRt.anchoredPosition = new Vector2(0f, -340f);   // 대사 상자와 같은 자리
                advanceRt.sizeDelta = new Vector2(1600f, 300f);
            }
            var advance = advanceGo.AddComponent<Button>();
            var advanceImage = advanceGo.GetComponent<Image>();
            advance.targetGraphic = advanceImage;

            // CreatePanel은 투명한 판을 클릭 대상에서 빼 둔다. 이 버튼은 투명해도 눌려야 한다.
            advanceImage.raycastTarget = true;

            // 인물 배치는 겹침 대화에서도 처음 튜토리얼과 똑같이 둔다.
            // 배경만 투명할 뿐 대화 자체는 같은 모습이어야 한다.
            var left = CreateCharacterImage(go.transform, "Char_Left", -520f, "placeholder_hanyoung");
            var right = CreateCharacterImage(go.transform, "Char_Right", 520f, "placeholder_chajihan");

            var box = CreatePanel(go.transform, "Box", new Color(0.09f, 0.09f, 0.12f, 0.96f));
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = new Vector2(0f, -340f);
            boxRt.sizeDelta = new Vector2(1600f, 300f);

            // 상자 안쪽 여백을 기준으로 붙인다. 좌표를 손으로 계산하면 상자 밖으로 나간다.
            // 상자 높이가 달라져도 세 줄이 겹치지 않도록 높이에서 되짚어 계산한다.
            float boxH = boxRt.sizeDelta.y;
            const float nameH = 50f, hintH = 28f, pad = 20f;

            const float textLeft = 48f;

            var nameText = AddText(box.transform, "Name", 36f, UIFontWeight.Bold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(nameText.rectTransform, textLeft, 48f, pad, boxH - pad - nameH);

            var lineText = AddText(box.transform, "Line", 34f, UIFontWeight.Regular, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            StretchInside(lineText.rectTransform, textLeft, 48f, pad + nameH + 10f, pad + hintH + 8f);
            lineText.textWrappingMode = TMPro.TextWrappingModes.Normal;   // 긴 대사는 상자 안에서 줄바꿈
            lineText.overflowMode = TextOverflowModes.Truncate;

            var hintText = AddText(box.transform, "Hint", 24f, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomRight);
            StretchInside(hintText.rectTransform, textLeft, 48f, boxH - pad - hintH, pad - 6f);

            // 대사 상자를 눌러도 넘어가야 하므로 진행 버튼을 맨 위로 올린다.
            // 상자가 클릭을 가로채면 플레이어가 가장 자연스럽게 누르는 자리가 먹통이 된다.
            advanceGo.transform.SetAsLastSibling();

            // 등급을 설명할 때 오른쪽에 펴는 쪽지. 종이에 적어 둔 것처럼 보이게 한다.
            var note = CreatePanel(go.transform, "Note", new Color(0.96f, 0.95f, 0.90f, 1f));
            var noteRt = (RectTransform)note.transform;
            noteRt.anchorMin = new Vector2(0.5f, 0.5f);
            noteRt.anchorMax = new Vector2(0.5f, 0.5f);
            noteRt.pivot = new Vector2(0.5f, 1f);
            noteRt.anchoredPosition = new Vector2(400f, 480f);
            noteRt.sizeDelta = new Vector2(660f, 630f);

            var noteInk = new Color(0.14f, 0.13f, 0.12f);

            var noteTitle = AddText(note.transform, "NoteTitle", 40f, UIFontWeight.Bold, noteInk,
                Vector2.zero, new Vector2(580f, 56f), TextAlignmentOptions.Left);
            var noteTitleRt = noteTitle.rectTransform;
            noteTitleRt.anchorMin = new Vector2(0.5f, 1f);
            noteTitleRt.anchorMax = new Vector2(0.5f, 1f);
            noteTitleRt.pivot = new Vector2(0.5f, 1f);
            noteTitleRt.anchoredPosition = new Vector2(0f, -44f);
            noteTitleRt.sizeDelta = new Vector2(580f, 56f);
            noteTitle.raycastTarget = false;

            var noteRule = CreatePanel(note.transform, "NoteRule", new Color(0.72f, 0.69f, 0.62f, 1f));
            var noteRuleRt = (RectTransform)noteRule.transform;
            noteRuleRt.anchorMin = new Vector2(0.5f, 1f);
            noteRuleRt.anchorMax = new Vector2(0.5f, 1f);
            noteRuleRt.pivot = new Vector2(0.5f, 1f);
            noteRuleRt.anchoredPosition = new Vector2(0f, -112f);
            noteRuleRt.sizeDelta = new Vector2(580f, 2f);
            noteRule.GetComponent<Image>().raycastTarget = false;
            AddCrisp(noteRule, 2f);

            var noteBody = AddText(note.transform, "NoteBody", 28f, UIFontWeight.Regular, noteInk,
                Vector2.zero, new Vector2(580f, 460f), TextAlignmentOptions.TopLeft);
            var noteBodyRt = noteBody.rectTransform;
            noteBodyRt.anchorMin = new Vector2(0.5f, 1f);
            noteBodyRt.anchorMax = new Vector2(0.5f, 1f);
            noteBodyRt.pivot = new Vector2(0.5f, 1f);
            noteBodyRt.anchoredPosition = new Vector2(0f, -140f);
            noteBodyRt.sizeDelta = new Vector2(580f, 460f);
            noteBody.textWrappingMode = TMPro.TextWrappingModes.Normal;
            noteBody.lineSpacing = 22f;
            noteBody.raycastTarget = false;

            note.SetActive(false);

            // 고를 것이 있을 때만 켜지는 자리.
            // 대사 상자 위, 오른쪽 끝에 맞춰 짧게 쌓는다. 말하는 쪽(왼쪽 인물)을 가리지 않는다.
            // 진행 버튼보다 뒤에 만들어야 선택지가 위로 올라와 눌린다.
            const float ChoiceWidth = 620f;
            const float BoxRightEdge = 800f;      // 대사 상자(폭 1600)의 오른쪽 끝

            var choiceRoot = CreateVerticalList(go.transform, "DialogueChoices",
                new Vector2(BoxRightEdge - ChoiceWidth * 0.5f, 40f),
                new Vector2(ChoiceWidth, 220f), 12f);

            var choiceTemplate = CreatePanel(choiceRoot, "ChoiceTemplate", new Color(0.16f, 0.17f, 0.22f, 0.98f));
            var dialogueChoice = choiceTemplate.AddComponent<Button>();
            dialogueChoice.targetGraphic = choiceTemplate.GetComponent<Image>();

            var dcColors = dialogueChoice.colors;
            dcColors.normalColor = Color.white;
            dcColors.highlightedColor = new Color(1.35f, 1.35f, 1.45f, 1f);
            dcColors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            dcColors.selectedColor = Color.white;
            dcColors.disabledColor = Color.white;
            dialogueChoice.colors = dcColors;

            var dcRt = (RectTransform)choiceTemplate.transform;
            dcRt.sizeDelta = new Vector2(ChoiceWidth, 92f);
            var dcLabel = AddText(choiceTemplate.transform, "Label", 24f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(ChoiceWidth - 48f, 76f), TextAlignmentOptions.Left);
            StretchInside(dcLabel.rectTransform, 24f, 24f, 8f, 8f);
            dcLabel.textWrappingMode = TMPro.TextWrappingModes.Normal;   // 짧은 칸이라 두 줄로 접힌다
            dcLabel.raycastTarget = false;
            choiceTemplate.SetActive(false);

            choiceRoot.gameObject.SetActive(false);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_left").FindPropertyRelative("nameTextId").stringValue = "tutorial.char.hanyoung";
            so.FindProperty("_left").FindPropertyRelative("image").objectReferenceValue = left;
            so.FindProperty("_right").FindPropertyRelative("nameTextId").stringValue = "tutorial.char.chajihan";
            so.FindProperty("_right").FindPropertyRelative("image").objectReferenceValue = right;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.FindProperty("_lineText").objectReferenceValue = lineText;
            so.FindProperty("_hintText").objectReferenceValue = hintText;
            so.FindProperty("_advanceButton").objectReferenceValue = advance;
            so.FindProperty("_choiceRoot").objectReferenceValue = choiceRoot;
            so.FindProperty("_choiceTemplate").objectReferenceValue = dialogueChoice;
            so.FindProperty("_noteRoot").objectReferenceValue = note;
            so.FindProperty("_noteTitle").objectReferenceValue = noteTitle;
            so.FindProperty("_noteBody").objectReferenceValue = noteBody;
            so.ApplyModifiedPropertiesWithoutUndo();

            return screen;
        }

        /// <summary>임시 캐릭터 이미지. 스프라이트 참조만 갈아 끼우면 실제 아트로 바뀐다.</summary>
        private static Image CreateCharacterImage(Transform parent, string name, float x, string spriteName)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 60f);
            rt.sizeDelta = new Vector2(420f, 840f);

            var image = go.AddComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/Sprites/" + spriteName + ".png");
            image.preserveAspect = true;
            image.raycastTarget = false;     // 진행 버튼을 가리지 않게 한다
            return image;
        }

        /// <summary>
        /// 차지한의 컴퓨터 바탕화면.
        /// 아이콘 다섯 개 중 지금 열리는 것은 괴담넷뿐이다. 나머지는 자리만 잡아 둔다.
        /// </summary>
        /// <summary>바탕화면 작업 표시줄의 높이. 괴담넷 창이 이만큼 자리를 비운다.</summary>
        private const float DesktopTaskbarHeight = 56f;

        /// <summary>믿음도를 적는 붉은 글자색. 목록과 글 화면이 같은 색을 쓴다.</summary>
        private static readonly Color BeliefMarkColor = new Color(0.78f, 0.16f, 0.16f);

        private static DesktopScreen BuildDesktopScreen(string name)
        {
            var go = CreatePanel(null, name, new Color(0.10f, 0.13f, 0.20f, 1f));
            StretchFull(go);

            var screen = go.AddComponent<DesktopScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            // 작업 표시줄. 괴담넷 창은 이 자리를 비워 두므로 창을 열어도 계속 보인다.
            var taskbar = CreatePanel(go.transform, "Taskbar", new Color(0.07f, 0.09f, 0.14f, 1f));
            var tbRt = (RectTransform)taskbar.transform;
            tbRt.anchorMin = new Vector2(0f, 0f);
            tbRt.anchorMax = new Vector2(1f, 0f);
            tbRt.pivot = new Vector2(0.5f, 0f);
            tbRt.anchoredPosition = Vector2.zero;
            tbRt.sizeDelta = new Vector2(0f, DesktopTaskbarHeight);

            var clock = AddText(taskbar.transform, "Clock", 24f, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            StretchInside(clock.rectTransform, 40f, 40f, 8f, 8f);

            // 왼쪽에는 지금 이 괴담을 얼마나 믿고 있는지를 띄운다. 게임의 핵심 숫자다.
            var belief = AddText(taskbar.transform, "Belief", 24f, UIFontWeight.Medium, new Color(0.92f, 0.44f, 0.42f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(belief.rectTransform, 40f, 40f, 8f, 8f);

            // 아이콘은 왼쪽 위에서부터 한 줄로 늘어놓는다.
            var apps = new[]
            {
                new[] { "gwedamnet", "ui.desktop.app_net" },
                new[] { "memo", "ui.desktop.app_memo" },
                new[] { "archive", "ui.desktop.app_archive" },
                new[] { "kikitalk", "ui.desktop.app_talk" },
                new[] { "gallery", "ui.desktop.app_gallery" },
            };

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_clockText").objectReferenceValue = clock;
            so.FindProperty("_beliefText").objectReferenceValue = belief;

            var iconList = so.FindProperty("_icons");
            iconList.arraySize = apps.Length;

            for (int i = 0; i < apps.Length; i++)
            {
                var icon = BuildDesktopIcon(go.transform, apps[i][0], new Vector2(-780f, 380f - i * 150f));

                var element = iconList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("appId").stringValue = apps[i][0];
                element.FindPropertyRelative("labelTextId").stringValue = apps[i][1];
                element.FindPropertyRelative("button").objectReferenceValue = icon.GetComponent<Button>();
                element.FindPropertyRelative("label").objectReferenceValue = icon.GetComponentInChildren<TMP_Text>(true);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            ClearDisabledTint(go);
            return screen;
        }

        /// <summary>
        /// 잠긴 버튼이 흐려지지 않게 한다.
        /// 유니티 기본값은 반투명 회색이라, 화면을 잠글 때마다 전체가 어두워진 것처럼 보인다.
        /// 지금은 잠금을 연출이 아니라 순서 지키기에만 쓰므로 모습은 그대로 두는 것이 맞다.
        /// </summary>
        private static void ClearDisabledTint(GameObject root)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var colors = button.colors;
                colors.disabledColor = colors.normalColor;
                button.colors = colors;
            }
        }

        /// <summary>바탕화면 아이콘 하나. 네모 하나와 이름표로 둔다.</summary>
        private static GameObject BuildDesktopIcon(Transform parent, string id, Vector2 position)
        {
            var go = new GameObject("Icon_" + id, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(200f, 130f);

            var button = go.AddComponent<Button>();

            var box = CreatePanel(go.transform, "Box", new Color(0.24f, 0.30f, 0.42f, 1f));
            var boxRt = (RectTransform)box.transform;
            boxRt.anchoredPosition = new Vector2(0f, 24f);
            boxRt.sizeDelta = new Vector2(84f, 68f);
            button.targetGraphic = box.GetComponent<Image>();

            var label = AddText(go.transform, "Label", 24f, UIFontWeight.Medium, TextColor,
                new Vector2(0f, -44f), new Vector2(200f, 40f), TextAlignmentOptions.Center);
            label.raycastTarget = false;

            return go;
        }

        /// <summary>인터넷 커뮤니티 게시글 화면.</summary>
        private static CommunityPageScreen BuildCommunityScreen(string name)
        {
            // 화면 자체는 투명하다. UIService 가 화면 뿌리를 레이어 전체로 늘려 버리기 때문에
            // 여기에 색을 칠하면 아래 바탕화면이 통째로 가려진다.
            var go = CreatePanel(null, name, new Color(0f, 0f, 0f, 0f));
            StretchFull(go);

            var screen = go.AddComponent<CommunityPageScreen>();

            // 아래 화면(바탕화면)을 숨기지 않는다. 작업 표시줄이 계속 보여야 하기 때문이다.
            ConfigureScreen(screen, name, UILayer.Screen, false, true);

            var ink = new Color(0.12f, 0.12f, 0.14f);
            var dim = new Color(0.42f, 0.44f, 0.48f);

            // --- 창 제목 표시줄. 진짜 브라우저 창처럼 보이게 한다. ---
            // 괴담넷은 바탕화면 위에 뜬 창이다. 아래쪽 작업 표시줄 자리는 비워 둔다.
            // 그래야 창을 열어도 컴퓨터를 쓰고 있다는 것이 계속 보인다.
            var window = CreatePanel(go.transform, "Window", new Color(0.94f, 0.94f, 0.95f, 1f));
            StretchInside((RectTransform)window.transform, 0f, 0f, 0f, DesktopTaskbarHeight);

            var titleBar = CreatePanel(window.transform, "TitleBar", new Color(0.13f, 0.14f, 0.18f, 1f));
            var barRt = (RectTransform)titleBar.transform;
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0f, 56f);

            var windowTitle = AddText(titleBar.transform, "WindowTitle", 26f, UIFontWeight.Medium, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(windowTitle.rectTransform, 110f, 260f, 8f, 8f);

            var closeButton = CreatePanel(titleBar.transform, "Btn_CloseWindow", new Color(0.62f, 0.22f, 0.24f, 1f));
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-70f, 0f);
            closeRt.sizeDelta = new Vector2(56f, 40f);
            var close = closeButton.AddComponent<Button>();
            close.targetGraphic = closeButton.GetComponent<Image>();
            var closeLabel = AddText(closeButton.transform, "Label", 26f, UIFontWeight.Bold, TextColor,
                Vector2.zero, new Vector2(56f, 40f), TextAlignmentOptions.Center);
            closeLabel.text = "X";
            closeLabel.raycastTarget = false;

            // 글자를 안쪽으로 들이는 만큼. 글 화면과 같은 선에 선다.
            const int BoardInset = 150;

            // --- 게시판 목록 보기 ---
            var boardView = new GameObject("BoardView", typeof(RectTransform));
            boardView.transform.SetParent(window.transform, false);
            StretchFull(boardView);

            // 대사 상자가 떠 있는 동안 화면 전체를 잠그는 무리. 끌기까지 함께 막힌다.
            var boardControls = boardView.AddComponent<CanvasGroup>();

            // 목록 화면도 글 화면과 똑같이 만든다.
            // 창 제목 표시줄만 남고 머리말부터 글 줄까지 한 장으로 끌려 내려간다.
            const float BoardBarHeight = 56f;

            var boardPaper = CreatePanel(boardView.transform, "Paper", new Color(1f, 1f, 1f, 1f));
            StretchInside((RectTransform)boardPaper.transform, 0f, 0f, BoardBarHeight, 0f);

            var boardViewport = new GameObject("PageViewport", typeof(RectTransform));
            boardViewport.transform.SetParent(boardView.transform, false);
            var bvRt = (RectTransform)boardViewport.transform;
            StretchInside(bvRt, 0f, 0f, BoardBarHeight, 0f);
            boardViewport.AddComponent<RectMask2D>();

            var boardGrab = boardViewport.AddComponent<Image>();
            boardGrab.color = new Color(1f, 1f, 1f, 0f);
            boardGrab.raycastTarget = true;

            var boardPage = new GameObject("Page", typeof(RectTransform));
            boardPage.transform.SetParent(boardViewport.transform, false);
            var bpRt = (RectTransform)boardPage.transform;
            bpRt.anchorMin = new Vector2(0f, 1f);
            bpRt.anchorMax = new Vector2(1f, 1f);
            bpRt.pivot = new Vector2(0.5f, 1f);
            bpRt.anchoredPosition = Vector2.zero;
            bpRt.sizeDelta = Vector2.zero;
            AddStack(boardPage, 0f, new RectOffset(0, 0, 0, 0));



            var boardScroll = boardViewport.AddComponent<ScrollRect>();
            boardScroll.viewport = bvRt;
            boardScroll.content = bpRt;
            boardScroll.horizontal = false;
            boardScroll.vertical = true;
            boardScroll.movementType = ScrollRect.MovementType.Clamped;
            boardScroll.scrollSensitivity = 40f;

            var boardBar = BuildVerticalScrollbar(boardView.transform, BoardBarHeight,
                out var boardNudge, out var boardUp, out var boardDown);
            boardScroll.verticalScrollbar = boardBar;
            boardScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var boardNudgeSo = new SerializedObject(boardNudge);
            boardNudgeSo.Update();
            boardNudgeSo.FindProperty("_target").objectReferenceValue = boardScroll;
            boardNudgeSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(boardUp.GetComponent<Button>().onClick, boardNudge.StepUp);
            UnityEventTools.AddPersistentListener(boardDown.GetComponent<Button>().onClick, boardNudge.StepDown);

            var boardHeader = BuildCommunityHeader(boardPage.transform, BoardInset,
                out var boardSiteText, out var boardBoardText);
            var boardHeaderElement = boardHeader.AddComponent<LayoutElement>();
            boardHeaderElement.minHeight = 90f;
            boardHeaderElement.preferredHeight = 90f;
            boardHeaderElement.flexibleHeight = 0f;

            // 글 줄을 담는 칸. 위아래로 여백을 두고 그 안에서 줄이 쌓인다.
            var boardList = new GameObject("Posts", typeof(RectTransform));
            boardList.transform.SetParent(boardPage.transform, false);
            var boardRoot = (RectTransform)boardList.transform;
            var boardListLayout = boardList.AddComponent<VerticalLayoutGroup>();
            boardListLayout.spacing = 0f;
            boardListLayout.padding = new RectOffset(BoardInset, BoardInset, 28, 40);
            boardListLayout.childAlignment = TextAnchor.UpperCenter;
            boardListLayout.childControlWidth = true;
            boardListLayout.childControlHeight = false;
            boardListLayout.childForceExpandWidth = true;
            boardListLayout.childForceExpandHeight = false;

            // 줄 자체는 흰 종이와 같은 색이라 평소에는 보이지 않는다.
            // 마우스를 올린 줄만 옅은 회색이 된다. 눌러야 할 곳은 그것으로 안다.
            var boardTemplate = CreatePanel(boardRoot, "PostTemplate", new Color(1f, 1f, 1f, 1f));
            var boardButton = boardTemplate.AddComponent<Button>();
            boardButton.targetGraphic = boardTemplate.GetComponent<Image>();

            var boardColors = boardButton.colors;
            boardColors.normalColor = Color.white;
            boardColors.highlightedColor = new Color(0.93f, 0.93f, 0.94f, 1f);
            boardColors.pressedColor = new Color(0.87f, 0.87f, 0.89f, 1f);
            boardColors.selectedColor = Color.white;
            boardColors.disabledColor = Color.white;          // 눌리지 않는 줄도 평소 모습 그대로
            boardColors.fadeDuration = 0.08f;
            boardButton.colors = boardColors;
            var btRt = (RectTransform)boardTemplate.transform;
            btRt.sizeDelta = new Vector2(0f, 104f);
            var btLabel = AddText(boardTemplate.transform, "Label", 26f, UIFontWeight.Medium, ink,
                Vector2.zero, new Vector2(1640f, 88f), TextAlignmentOptions.Left);
            StretchInside(btLabel.rectTransform, 12f, 12f, 12f, 18f);

            // 들어가야 하는 글 왼쪽에 붙는 파란 세모. 열 수 있는 글에만 켜진다.
            var btMark = AddText(boardTemplate.transform, "Mark", 26f, UIFontWeight.Bold,
                new Color(0.18f, 0.38f, 0.78f),
                Vector2.zero, new Vector2(40f, 40f), TextAlignmentOptions.Center);
            btMark.text = "▶";
            btMark.raycastTarget = false;
            var btMarkRt = btMark.rectTransform;
            btMarkRt.anchorMin = new Vector2(0f, 0.5f);
            btMarkRt.anchorMax = new Vector2(0f, 0.5f);
            btMarkRt.pivot = new Vector2(1f, 0.5f);
            btMarkRt.anchoredPosition = new Vector2(-8f, 6f);
            btMarkRt.sizeDelta = new Vector2(40f, 40f);

            // 오른쪽에는 이 글이 괴담의 믿음에 얼마나 보태고 있는지를 붉게 적는다.
            var btBelief = AddText(boardTemplate.transform, "Belief", 24f, UIFontWeight.SemiBold, BeliefMarkColor,
                Vector2.zero, new Vector2(320f, 40f), TextAlignmentOptions.Right);
            btBelief.raycastTarget = false;
            var btBeliefRt = btBelief.rectTransform;
            btBeliefRt.anchorMin = new Vector2(1f, 0.5f);
            btBeliefRt.anchorMax = new Vector2(1f, 0.5f);
            btBeliefRt.pivot = new Vector2(1f, 0.5f);
            btBeliefRt.anchoredPosition = new Vector2(-16f, 6f);
            btBeliefRt.sizeDelta = new Vector2(320f, 40f);

            // 글 사이를 가르는 가는 선. 댓글과 같은 방식이다.
            var btRule = CreatePanel(boardTemplate.transform, "Rule", new Color(0.86f, 0.87f, 0.90f, 1f));
            var btRuleRt = (RectTransform)btRule.transform;
            btRuleRt.anchorMin = new Vector2(0f, 0f);
            btRuleRt.anchorMax = new Vector2(1f, 0f);
            btRuleRt.pivot = new Vector2(0.5f, 0f);
            btRuleRt.anchoredPosition = Vector2.zero;
            btRuleRt.sizeDelta = new Vector2(0f, 2f);
            btRule.GetComponent<Image>().raycastTarget = false;
            AddCrisp(btRule, 1f);
            boardTemplate.SetActive(false);

            // --- 글 하나를 펼친 보기 ---
            var postView = new GameObject("PostView", typeof(RectTransform));
            postView.transform.SetParent(window.transform, false);
            StretchFull(postView);

            var postControls = postView.AddComponent<CanvasGroup>();

            // 실제 커뮤니티 글 화면처럼 화면 전체가 한 장으로 굴러간다.
            // 글과 댓글은 회색 틈이 아니라 굵은 가로선으로 나눈다. 실제 화면이 그렇게 나눈다.
            const float pageWidth = 1920f;   // 화면 폭을 그대로 쓴다. 좌우에 회색이 비치지 않는다

            // 글이 가장자리에 붙지 않도록 안쪽으로 들이는 만큼. 머리말과 본문이 같은 선에 선다.
            const int Inset = 150;
            const float ContentInset = Inset;

            // 흰 종이와 굴러가는 자리는 창 제목 표시줄 아래를 전부 차지한다.
            // 좌우와 아래는 부모에 앵커로 맞춘다. 숫자로 폭을 주면 화면 크기에 따라
            // 가장자리에 머리카락 같은 틈이 남는다.
            const float TitleBarHeight = 56f;

            var paper = CreatePanel(postView.transform, "Paper", new Color(1f, 1f, 1f, 1f));
            StretchInside((RectTransform)paper.transform, 0f, 0f, TitleBarHeight, 0f);

            // 굴러가는 자리. 창 제목 표시줄만 남기고 그 아래는 머리말까지 전부 함께 내려간다.
            // 대화 상자는 그 위에 얹히지만 내용이 함께 굴러가므로 가려진 곳도 올려서 볼 수 있다.
            var pageViewport = new GameObject("PageViewport", typeof(RectTransform));
            pageViewport.transform.SetParent(postView.transform, false);
            var cvRt = (RectTransform)pageViewport.transform;
            StretchInside(cvRt, 0f, 0f, TitleBarHeight, 0f);
            pageViewport.AddComponent<RectMask2D>();

            // 끄는 손을 받는 판. 보이지는 않지만 눌림은 받는다.
            // 이것이 없으면 글자나 칸이 없는 빈 곳을 잡았을 때 아무 일도 일어나지 않는다.
            var grab = pageViewport.AddComponent<Image>();
            grab.color = new Color(1f, 1f, 1f, 0f);
            grab.raycastTarget = true;

            // 굴러가는 내용 전체.
            var page = new GameObject("Page", typeof(RectTransform));
            page.transform.SetParent(pageViewport.transform, false);
            var pageRt = (RectTransform)page.transform;
            pageRt.anchorMin = new Vector2(0f, 1f);
            pageRt.anchorMax = new Vector2(1f, 1f);
            pageRt.pivot = new Vector2(0.5f, 1f);
            pageRt.anchoredPosition = Vector2.zero;
            pageRt.sizeDelta = new Vector2(0f, 0f);   // 폭은 굴러가는 자리에 맞춘다
            AddStack(page, 0f, new RectOffset(0, 0, 0, 0));



            var pageScroll = pageViewport.AddComponent<ScrollRect>();
            pageScroll.viewport = cvRt;
            pageScroll.content = pageRt;
            pageScroll.horizontal = false;
            pageScroll.vertical = true;
            pageScroll.movementType = ScrollRect.MovementType.Clamped;
            pageScroll.scrollSensitivity = 40f;

            // 오른쪽 끝의 막대. 지금 어디쯤 보고 있는지 알려 주고, 화살표로도 움직인다.
            var bar = BuildVerticalScrollbar(postView.transform, TitleBarHeight,
                out var scrollNudge, out var scrollUp, out var scrollDown);
            pageScroll.verticalScrollbar = bar;
            pageScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            var nudgeSo = new SerializedObject(scrollNudge);
            nudgeSo.Update();
            nudgeSo.FindProperty("_target").objectReferenceValue = pageScroll;
            nudgeSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(scrollUp.GetComponent<Button>().onClick, scrollNudge.StepUp);
            UnityEventTools.AddPersistentListener(scrollDown.GetComponent<Button>().onClick, scrollNudge.StepDown);

            // 머리말도 함께 굴러간다. 남는 것은 맨 위 창 제목 표시줄뿐이다.
            var pageHeader = BuildCommunityHeader(page.transform, ContentInset, out var siteText, out var boardText);
            var pageHeaderElement = pageHeader.AddComponent<LayoutElement>();
            pageHeaderElement.minHeight = 90f;
            pageHeaderElement.preferredHeight = 90f;
            pageHeaderElement.flexibleHeight = 0f;

            // --- 글 묶음 ---
            var postBlock = new GameObject("PostBlock", typeof(RectTransform));
            postBlock.transform.SetParent(page.transform, false);
            AddStack(postBlock, 0f, new RectOffset(0, 0, 0, 0));

            // 믿음도를 작성자 정보 줄에 맞추려면 띠 안쪽 자리를 알아야 한다. 먼저 정해 둔다.
            const float TitleBandPadTop = 44f;
            const float TitleHeight = 50f;
            const float TitleBandSpacing = 18f;
            const float MetaHeight = 29f;

            // 제목과 작성자 정보는 옅은 띠 위에 둔다. 본문과 눈에 띄게 갈린다.
            var titleBand = CreatePanel(postBlock.transform, "TitleBand", new Color(0.955f, 0.958f, 0.97f, 1f));
            AddStack(titleBand, TitleBandSpacing, new RectOffset(Inset, Inset, (int)TitleBandPadTop, 52));

            var titleText = AddText(titleBand.transform, "PostTitle", 42f, UIFontWeight.Bold, ink,
                Vector2.zero, new Vector2(pageWidth - 64f, 54f), TextAlignmentOptions.Left);

            // 믿음도는 작성자 정보 줄(조회 / 댓글 / 시각)의 오른쪽 끝에 맞춘다.
            // 배치에서 빼내고 자리를 직접 잡는다. 위 여백 + 제목 + 사이 간격만큼 내려온 자리다.
            var postBelief = AddText(titleBand.transform, "PostBelief", 26f, UIFontWeight.SemiBold, BeliefMarkColor,
                Vector2.zero, new Vector2(400f, MetaHeight), TextAlignmentOptions.Right);
            postBelief.raycastTarget = false;
            var postBeliefElement = postBelief.gameObject.AddComponent<LayoutElement>();
            postBeliefElement.ignoreLayout = true;
            var postBeliefRt = postBelief.rectTransform;
            postBeliefRt.anchorMin = new Vector2(1f, 1f);
            postBeliefRt.anchorMax = new Vector2(1f, 1f);
            postBeliefRt.pivot = new Vector2(1f, 1f);
            postBeliefRt.anchoredPosition = new Vector2(-Inset, -(TitleBandPadTop + TitleHeight + TitleBandSpacing));
            postBeliefRt.sizeDelta = new Vector2(400f, MetaHeight);

            var metaText = AddText(titleBand.transform, "PostMeta", 24f, UIFontWeight.Regular, dim,
                Vector2.zero, new Vector2(pageWidth - 64f, 30f), TextAlignmentOptions.Left);

            var bodyArea = new GameObject("BodyArea", typeof(RectTransform));
            bodyArea.transform.SetParent(postBlock.transform, false);
            AddStack(bodyArea, 0f, new RectOffset(Inset, Inset, 52, 68));

            var bodyText = AddText(bodyArea.transform, "PostBody", 28f, UIFontWeight.Regular, ink,
                Vector2.zero, new Vector2(pageWidth - 64f, 110f), TextAlignmentOptions.TopLeft);

            // 본문 아래 반응. 실제 커뮤니티가 본문과 댓글 사이에 두는 자리다.
            // 지금은 숫자만 보여준다. 누르는 기능은 나중에 붙인다.
            var reactionRow = new GameObject("Reactions", typeof(RectTransform));
            reactionRow.transform.SetParent(bodyArea.transform, false);
            var reactionLayout = reactionRow.AddComponent<HorizontalLayoutGroup>();
            reactionLayout.spacing = 20f;
            reactionLayout.padding = new RectOffset(0, 0, 140, 0);   // 글 칸 맨 아래에 붙인다
            reactionLayout.childAlignment = TextAnchor.MiddleCenter;
            reactionLayout.childControlWidth = false;
            reactionLayout.childControlHeight = false;
            reactionLayout.childForceExpandWidth = false;
            reactionLayout.childForceExpandHeight = false;

            var likeText = AddReactionChip(reactionRow.transform, "Like", ink, out var likeButton);
            var dislikeText = AddReactionChip(reactionRow.transform, "Dislike", ink, out var dislikeButton);

            // 글과 댓글을 가르는 굵은 선.
            AddStackRule(page.transform, new Color(0.62f, 0.64f, 0.68f, 1f), 2f);

            // --- 댓글 묶음 ---
            var commentBlock = new GameObject("CommentBlock", typeof(RectTransform));
            commentBlock.transform.SetParent(page.transform, false);
            AddStack(commentBlock, 18f, new RectOffset(Inset, Inset, 44, 40));

            var commentHeader = AddText(commentBlock.transform, "CommentHeader", 26f, UIFontWeight.SemiBold, ink,
                Vector2.zero, new Vector2(pageWidth - 64f, 32f), TextAlignmentOptions.Left);

            AddStackRule(commentBlock.transform, new Color(0.86f, 0.87f, 0.90f, 1f), 1f);

            // 댓글 덩어리. 칸 높이는 카드가 정한다.
            var comments = new GameObject("Comments", typeof(RectTransform));
            comments.transform.SetParent(commentBlock.transform, false);
            var commentRoot = (RectTransform)comments.transform;
            commentRoot.sizeDelta = new Vector2(pageWidth - 64f, 0f);
            var commentLayout = comments.AddComponent<VerticalLayoutGroup>();
            commentLayout.spacing = 0f;
            commentLayout.childAlignment = TextAnchor.UpperCenter;
            commentLayout.childControlWidth = true;
            commentLayout.childControlHeight = false;
            commentLayout.childForceExpandWidth = true;
            commentLayout.childForceExpandHeight = false;

            // 댓글 한 줄. 실제 커뮤니티처럼 칸을 나누는 것은 배경색이 아니라 아래쪽 가는 선이다.
            // 선 두께는 실제 화면 픽셀로 지킨다(CrispRule). 캔버스 배율에 맡기면 작은 창에서 사라진다.
            var commentTemplate = CreatePanel(commentRoot, "CommentTemplate", new Color(1f, 1f, 1f, 0f));
            var comRt = (RectTransform)commentTemplate.transform;
            comRt.sizeDelta = new Vector2(pageWidth - 64f, 82f);
            var comLabel = AddText(commentTemplate.transform, "Label", 22f, UIFontWeight.Regular, ink,
                Vector2.zero, new Vector2(pageWidth - 88f, 68f), TextAlignmentOptions.TopLeft);
            StretchInside(comLabel.rectTransform, 12f, 12f, 10f, 16f);
            var comRule = CreatePanel(commentTemplate.transform, "Rule", new Color(0.86f, 0.87f, 0.90f, 1f));
            var comRuleRt = (RectTransform)comRule.transform;
            comRuleRt.anchorMin = new Vector2(0f, 0f);
            comRuleRt.anchorMax = new Vector2(1f, 0f);
            comRuleRt.pivot = new Vector2(0.5f, 0f);
            comRuleRt.anchoredPosition = Vector2.zero;
            comRuleRt.sizeDelta = new Vector2(0f, 2f);
            comRule.GetComponent<Image>().raycastTarget = false;
            AddCrisp(comRule, 1f);
            commentTemplate.SetActive(false);

            // --- 댓글 쓰기 칸. 댓글 목록 맨 아래에 붙는다. ---
            var writeBox = CreatePanel(commentBlock.transform, "WriteBox", new Color(0.955f, 0.958f, 0.97f, 1f));
            AddStack(writeBox, 12f, new RectOffset(22, 22, 18, 22));

            var choiceHeader = AddText(writeBox.transform, "ChoiceHeader", 26f, UIFontWeight.SemiBold, ink,
                Vector2.zero, new Vector2(pageWidth - 108f, 32f), TextAlignmentOptions.Left);

            // 안내는 쓰는 칸 안, 머리말 바로 아래. 무엇과도 겹치지 않는다.
            var noticeText = AddText(writeBox.transform, "Notice", 24f, UIFontWeight.Medium, new Color(0.62f, 0.24f, 0.24f),
                Vector2.zero, new Vector2(pageWidth - 108f, 30f), TextAlignmentOptions.Left);

            var choices = new GameObject("Choices", typeof(RectTransform));
            choices.transform.SetParent(writeBox.transform, false);
            var choiceRoot = (RectTransform)choices.transform;
            choiceRoot.sizeDelta = new Vector2(pageWidth - 108f, 0f);
            var choiceLayout = choices.AddComponent<VerticalLayoutGroup>();
            choiceLayout.spacing = 12f;
            choiceLayout.childAlignment = TextAnchor.UpperCenter;
            choiceLayout.childControlWidth = true;
            choiceLayout.childControlHeight = false;
            choiceLayout.childForceExpandWidth = true;
            choiceLayout.childForceExpandHeight = false;

            var choiceTemplate = CreatePanel(choiceRoot, "ChoiceTemplate", new Color(0.88f, 0.90f, 0.94f, 1f));
            var choiceButton = choiceTemplate.AddComponent<Button>();
            choiceButton.targetGraphic = choiceTemplate.GetComponent<Image>();
            var ctRt = (RectTransform)choiceTemplate.transform;
            ctRt.sizeDelta = new Vector2(pageWidth - 108f, 76f);
            var ctLabel = AddText(choiceTemplate.transform, "Label", 22f, UIFontWeight.Medium, ink,
                Vector2.zero, new Vector2(pageWidth - 156f, 46f), TextAlignmentOptions.Left);
            StretchInside(ctLabel.rectTransform, 24f, 24f, 8f, 8f);

            // 현장 조사가 필요하다는 표시. 댓글 글자에 붙이지 않고 칸 오른쪽 아래에 따로 둔다.
            var ctNote = AddText(choiceTemplate.transform, "Note", 19f, UIFontWeight.Medium,
                new Color(0.62f, 0.24f, 0.24f),
                Vector2.zero, new Vector2(360f, 26f), TextAlignmentOptions.BottomRight);
            var ctNoteRt = ctNote.rectTransform;
            ctNoteRt.anchorMin = new Vector2(1f, 0f);
            ctNoteRt.anchorMax = new Vector2(1f, 0f);
            ctNoteRt.pivot = new Vector2(1f, 0f);
            ctNoteRt.anchoredPosition = new Vector2(-24f, 8f);
            ctNoteRt.sizeDelta = new Vector2(360f, 26f);
            ctNote.raycastTarget = false;

            choiceTemplate.SetActive(false);

            var commentScroll = pageScroll;

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_windowTitleText").objectReferenceValue = windowTitle;
            so.FindProperty("_closeButton").objectReferenceValue = close;
            so.FindProperty("_boardView").objectReferenceValue = boardView;
            so.FindProperty("_postView").objectReferenceValue = postView;
            so.FindProperty("_boardRoot").objectReferenceValue = boardRoot;
            so.FindProperty("_boardScroll").objectReferenceValue = boardScroll;
            so.FindProperty("_boardEntryTemplate").objectReferenceValue = boardButton;
            SetTextArray(so.FindProperty("_siteTexts"), boardSiteText, siteText);
            so.FindProperty("_boardListText").objectReferenceValue = boardBoardText;
            so.FindProperty("_boardPostText").objectReferenceValue = boardText;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_metaText").objectReferenceValue = metaText;
            so.FindProperty("_bodyText").objectReferenceValue = bodyText;
            so.FindProperty("_postBeliefText").objectReferenceValue = postBelief;
            so.FindProperty("_likeText").objectReferenceValue = likeText;
            so.FindProperty("_dislikeText").objectReferenceValue = dislikeText;
            so.FindProperty("_likeButton").objectReferenceValue = likeButton;
            so.FindProperty("_dislikeButton").objectReferenceValue = dislikeButton;
            so.FindProperty("_commentHeaderText").objectReferenceValue = commentHeader;
            so.FindProperty("_commentRoot").objectReferenceValue = commentRoot;
            so.FindProperty("_commentTemplate").objectReferenceValue = commentTemplate;
            so.FindProperty("_commentScroll").objectReferenceValue = commentScroll;
            so.FindProperty("_choiceHeaderText").objectReferenceValue = choiceHeader;
            so.FindProperty("_choiceRoot").objectReferenceValue = choiceRoot;
            so.FindProperty("_choiceTemplate").objectReferenceValue = choiceButton;
            so.FindProperty("_noticeText").objectReferenceValue = noticeText;
            so.FindProperty("_postControls").objectReferenceValue = postControls;
            so.FindProperty("_boardControls").objectReferenceValue = boardControls;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 잠겼을 때 흐려지지 않게 한다.
            // 한영이 말하는 동안 화면을 잠그는데, 기본값대로 두면 그때마다 창 전체가 어두워진다.
            ClearDisabledTint(go);

            // 버튼 줄은 두지 않는다. 튜토리얼을 임의로 끝낼 수 없고, 흐름이 알아서 다음으로 넘어간다.
            return screen;
        }

        /// <summary>세로로 쌓이는 목록 영역.</summary>
        private static RectTransform CreateVerticalList(Transform parent, string name, Vector2 position,
            Vector2 size, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rt;
        }

        /// <summary>규칙 추론 화면. 왼쪽에 확보한 단서, 가운데에 규칙 후보 목록.</summary>
        private static RuleListScreen BuildRuleListScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<RuleListScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            var titleText = AddText(go.transform, "Title", 60f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 420f), new Vector2(1500f, 100f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 355f), new Vector2(1500f, 60f), TextAlignmentOptions.Center);
            var clueText = AddText(go.transform, "Clues", 26f, UIFontWeight.Regular, AccentColor,
                new Vector2(-620f, 40f), new Vector2(560f, 500f), TextAlignmentOptions.TopLeft);
            var resultText = AddText(go.transform, "Result", 30f, UIFontWeight.Medium, WarnColor,
                new Vector2(0f, -330f), new Vector2(1500f, 80f), TextAlignmentOptions.Center);

            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(go.transform, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = new Vector2(180f, 290f);
            listRt.sizeDelta = new Vector2(1000f, 520f);
            var listLayout = listGo.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 14f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = false;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = false;
            listLayout.childForceExpandHeight = false;

            var template = CreatePanel(listGo.transform, "ItemTemplate", ButtonColor);
            var templateButton = template.AddComponent<Button>();
            templateButton.targetGraphic = template.GetComponent<Image>();
            var trt = (RectTransform)template.transform;
            trt.sizeDelta = new Vector2(960f, 150f);
            var tLabel = AddText(template.transform, "ItemLabel", 26f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(920f, 130f), TextAlignmentOptions.Left);
            var tlrt = (RectTransform)tLabel.transform;
            tlrt.anchorMin = Vector2.zero;
            tlrt.anchorMax = Vector2.one;
            tlrt.offsetMin = new Vector2(24f, 8f);
            tlrt.offsetMax = new Vector2(-24f, -8f);
            template.SetActive(false);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_footerText").objectReferenceValue = footerText;
            so.FindProperty("_clueText").objectReferenceValue = clueText;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.FindProperty("_listRoot").objectReferenceValue = listRt;
            so.FindProperty("_itemTemplate").objectReferenceValue = templateButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -420f), new Vector2(900f, 110f));
            return screen;
        }

        private static InternetListScreen BuildInternetListScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<InternetListScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            var titleText = AddText(go.transform, "Title", 60f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 380f), new Vector2(1500f, 100f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 300f), new Vector2(1500f, 60f), TextAlignmentOptions.Center);
            var statsText = AddText(go.transform, "Stats", 30f, UIFontWeight.SemiBold, AccentColor,
                new Vector2(0f, -300f), new Vector2(1500f, 60f), TextAlignmentOptions.Center);

            // 목록 영역
            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(go.transform, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0.5f, 1f);
            listRt.anchoredPosition = new Vector2(0f, 230f);
            listRt.sizeDelta = new Vector2(1200f, 400f);
            var listLayout = listGo.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 16f;
            listLayout.childAlignment = TextAnchor.UpperCenter;
            listLayout.childControlWidth = false;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = false;
            listLayout.childForceExpandHeight = false;

            // 복제용 템플릿 (비활성 상태로 둔다)
            var template = CreatePanel(listGo.transform, "ItemTemplate", ButtonColor);
            var templateButton = template.AddComponent<Button>();
            templateButton.targetGraphic = template.GetComponent<Image>();
            var trt = (RectTransform)template.transform;
            trt.sizeDelta = new Vector2(1100f, 120f);
            var tLabel = AddText(template.transform, "ItemLabel", 28f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(1060f, 100f), TextAlignmentOptions.Center);
            var tlrt = (RectTransform)tLabel.transform;
            tlrt.anchorMin = Vector2.zero;
            tlrt.anchorMax = Vector2.one;
            tlrt.offsetMin = new Vector2(20f, 8f);
            tlrt.offsetMax = new Vector2(-20f, -8f);
            template.SetActive(false);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_footerText").objectReferenceValue = footerText;
            so.FindProperty("_statsText").objectReferenceValue = statsText;
            so.FindProperty("_listRoot").objectReferenceValue = listRt;
            so.FindProperty("_itemTemplate").objectReferenceValue = templateButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -380f), new Vector2(900f, 110f));
            return screen;
        }

        /// <summary>게시글 상세 화면.</summary>
        private static InternetPageScreen BuildInternetPageScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<InternetPageScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            var titleText = AddText(go.transform, "Title", 54f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 330f), new Vector2(1500f, 100f), TextAlignmentOptions.Center);
            var bodyText = AddText(go.transform, "Body", 32f, UIFontWeight.Regular, TextColor,
                new Vector2(0f, 110f), new Vector2(1300f, 300f), TextAlignmentOptions.Top);
            var statusText = AddText(go.transform, "Status", 30f, UIFontWeight.SemiBold, DimTextColor,
                new Vector2(0f, -120f), new Vector2(1300f, 70f), TextAlignmentOptions.Center);
            var statsText = AddText(go.transform, "Stats", 30f, UIFontWeight.SemiBold, AccentColor,
                new Vector2(0f, -180f), new Vector2(1300f, 60f), TextAlignmentOptions.Center);
            var feedbackText = AddText(go.transform, "Feedback", 28f, UIFontWeight.Medium, WarnColor,
                new Vector2(0f, -240f), new Vector2(1300f, 60f), TextAlignmentOptions.Center);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_bodyText").objectReferenceValue = bodyText;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_statsText").objectReferenceValue = statsText;
            so.FindProperty("_feedbackText").objectReferenceValue = feedbackText;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -330f), new Vector2(1000f, 110f));
            return screen;
        }

        /// <summary>버튼을 가로로 배치하는 행을 만든다.</summary>
        private static Transform CreateButtonRow(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var row = new GameObject("Buttons", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row.transform;
        }

        /// <summary>
        /// 현장 조사 화면.
        /// 위는 장면이 쓰고 아래는 검은 대사 띠가 쓴다. 띠 왼쪽 끝에 초상 자리를 둔다.
        /// </summary>
        private static FieldHudScreen BuildFieldHudScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, Color.clear);
            StretchFull(go);

            var screen = go.AddComponent<FieldHudScreen>();
            ConfigureScreen(screen, name, UILayer.HUD, false, false);

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(go.transform, false);
            StretchFull(safe);
            safe.AddComponent<SafeAreaFitter>();

            // 아래 1/3 은 대사 띠가 쓴다. 위 2/3 은 카메라가 장면을 그리는 자리라 비워 둔다.
            const float BandHeight = 1080f / 3f;     // = 360

            // --- 장면 맨 위 가운데의 한 줄 ---
            // 장면 위에 얹히므로 글자가 묻히지 않게 어두운 판을 깔아 준다.
            var tickerPlate = CreatePanel(safe.transform, "TickerPlate", new Color(0.04f, 0.04f, 0.06f, 0.72f));
            var plateRt = (RectTransform)tickerPlate.transform;
            plateRt.anchorMin = new Vector2(0.5f, 1f);
            plateRt.anchorMax = new Vector2(0.5f, 1f);
            plateRt.pivot = new Vector2(0.5f, 1f);
            plateRt.anchoredPosition = new Vector2(0f, -24f);
            plateRt.sizeDelta = new Vector2(1100f, 52f);
            tickerPlate.GetComponent<Image>().raycastTarget = false;

            var tickerText = AddText(tickerPlate.transform, "Ticker", 26f, UIFontWeight.Medium, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(tickerText.rectTransform, 24f, 24f, 6f, 6f);
            tickerText.raycastTarget = false;

            // --- 아래 대사 띠 ---
            var band = CreatePanel(safe.transform, "Band", new Color(0.03f, 0.03f, 0.05f, 1f));
            var bandRt = (RectTransform)band.transform;
            bandRt.anchorMin = new Vector2(0f, 0f);
            bandRt.anchorMax = new Vector2(1f, 0f);
            bandRt.pivot = new Vector2(0.5f, 0f);
            bandRt.anchoredPosition = Vector2.zero;
            bandRt.sizeDelta = new Vector2(0f, BandHeight);

            // 띠 맨 위의 가는 선. 장면과 띠를 가른다.
            var bandEdge = CreatePanel(band.transform, "Edge", new Color(0.30f, 0.30f, 0.36f, 1f));
            var edgeRt = (RectTransform)bandEdge.transform;
            edgeRt.anchorMin = new Vector2(0f, 1f);
            edgeRt.anchorMax = new Vector2(1f, 1f);
            edgeRt.pivot = new Vector2(0.5f, 1f);
            edgeRt.anchoredPosition = Vector2.zero;
            edgeRt.sizeDelta = new Vector2(0f, 2f);
            bandEdge.GetComponent<Image>().raycastTarget = false;
            AddCrisp(bandEdge, 2f);

            // 초상 자리. 지금은 빈 네모다. 실제 그림이 생기면 이 Image만 갈아 끼운다.
            const float PortraitSize = 240f;
            var portrait = CreatePanel(band.transform, "Portrait", new Color(0.16f, 0.17f, 0.22f, 1f));
            var portraitRt = (RectTransform)portrait.transform;
            portraitRt.anchorMin = new Vector2(0f, 0.5f);
            portraitRt.anchorMax = new Vector2(0f, 0.5f);
            portraitRt.pivot = new Vector2(0f, 0.5f);
            portraitRt.anchoredPosition = new Vector2(60f, 0f);
            portraitRt.sizeDelta = new Vector2(PortraitSize, PortraitSize);

            var portraitEdge = CreatePanel(portrait.transform, "Edge", new Color(0.42f, 0.44f, 0.52f, 1f));
            StretchInside((RectTransform)portraitEdge.transform, -2f, -2f, -2f, -2f);
            portraitEdge.transform.SetAsFirstSibling();   // 테두리처럼 뒤에 깔린다
            portraitEdge.GetComponent<Image>().raycastTarget = false;

            // 초상 오른쪽에 이름과 대사.
            const float TextLeft = 60f + PortraitSize + 40f;   // 초상 오른쪽 끝에서 띄운다

            var speakerText = AddText(band.transform, "Speaker", 30f, UIFontWeight.Bold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            speakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
            speakerText.rectTransform.anchorMax = new Vector2(0f, 1f);
            speakerText.rectTransform.pivot = new Vector2(0f, 1f);
            speakerText.rectTransform.anchoredPosition = new Vector2(TextLeft, -48f);
            speakerText.rectTransform.sizeDelta = new Vector2(700f, 42f);
            speakerText.raycastTarget = false;

            var lineText = AddText(band.transform, "Line", 34f, UIFontWeight.Regular, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            lineText.rectTransform.anchorMin = new Vector2(0f, 1f);
            lineText.rectTransform.anchorMax = new Vector2(0f, 1f);
            lineText.rectTransform.pivot = new Vector2(0f, 1f);
            lineText.rectTransform.anchoredPosition = new Vector2(TextLeft, -100f);
            lineText.rectTransform.sizeDelta = new Vector2(1240f, 130f);
            lineText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            lineText.raycastTarget = false;

            // 버튼은 띠 오른쪽 아래에 세운다. 장면도 대사도 가리지 않는다.
            buttonRow = CreateButtonRow(band.transform, new Vector2(420f, -120f), new Vector2(900f, 110f));

            // 튜토리얼이 현장에서 말할 때만 켜지는 진행 버튼. 화면 전체를 덮는다.
            // 맨 나중에 만들어야 대사 띠와 버튼보다 위에 올라와 그 둘까지 막는다.
            var fieldAdvance = CreatePanel(go.transform, "Btn_Advance", new Color(0f, 0f, 0f, 0f));
            StretchFull(fieldAdvance);
            var fieldAdvanceButton = fieldAdvance.AddComponent<Button>();
            var fieldAdvanceImage = fieldAdvance.GetComponent<Image>();
            fieldAdvanceButton.targetGraphic = fieldAdvanceImage;

            // CreatePanel 은 투명한 판을 클릭 대상에서 빼 둔다. 이 버튼은 투명해도 눌려야 한다.
            fieldAdvanceImage.raycastTarget = true;
            fieldAdvance.SetActive(false);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_advanceRoot").objectReferenceValue = fieldAdvance;
            so.FindProperty("_advanceButton").objectReferenceValue = fieldAdvanceButton;
            so.FindProperty("_tickerText").objectReferenceValue = tickerText;
            so.FindProperty("_portrait").objectReferenceValue = portrait.GetComponent<Image>();
            so.FindProperty("_speakerText").objectReferenceValue = speakerText;
            so.FindProperty("_lineText").objectReferenceValue = lineText;
            so.ApplyModifiedPropertiesWithoutUndo();

            ClearDisabledTint(go);
            return screen;
        }

        private static TextPanelScreen BuildPopupScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PopupDim);
            StretchFull(go);

            var screen = go.AddComponent<TextPanelScreen>();
            // 팝업은 아래 화면을 가리지 않는다. 뒤로가기로 닫히지 않게 해 확인 버튼을 거치게 한다.
            ConfigureScreen(screen, name, UILayer.Popup, false, false);

            var box = CreatePanel(go.transform, "Box", PopupBox);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(1000f, 460f);

            var titleText = AddText(box.transform, "Title", 46f, UIFontWeight.SemiBold, TextColor,
                new Vector2(0f, 140f), new Vector2(900f, 80f), TextAlignmentOptions.Center);
            var bodyText = AddText(box.transform, "Body", 32f, UIFontWeight.Regular, TextColor,
                new Vector2(0f, 10f), new Vector2(880f, 190f), TextAlignmentOptions.Top);

            BindScreenTexts(screen, titleText, bodyText, null);

            var row = new GameObject("Buttons", typeof(RectTransform));
            row.transform.SetParent(box.transform, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(360f, 100f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            buttonRow = row.transform;
            return screen;
        }

        // ------------------------------------------------------------- 헬퍼

        private static void ConfigureScreen(UIScreen screen, string id, UILayer layer,
            bool hidesUnderlying, bool closableByBack, bool keepsUnderlyingUsable = false)
        {
            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_screenId").stringValue = id;
            so.FindProperty("_layer").enumValueIndex = (int)layer;
            so.FindProperty("_hidesUnderlying").boolValue = hidesUnderlying;
            so.FindProperty("_closableByBack").boolValue = closableByBack;
            so.FindProperty("_keepsUnderlyingUsable").boolValue = keepsUnderlyingUsable;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindScreenTexts(TextPanelScreen screen, TMP_Text title, TMP_Text body, TMP_Text footer)
        {
            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_bodyText").objectReferenceValue = body;
            so.FindProperty("_footerText").objectReferenceValue = footer;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = color.a > 0.01f;
            return go;
        }

        /// <summary>
        /// 오른쪽 끝에 세우는 막대. 실제 브라우저의 그것과 같은 자리다.
        /// 굴러갈 것이 없으면 스스로 사라진다(AutoHide).
        /// </summary>
        private static Scrollbar BuildVerticalScrollbar(Transform parent, float topInset, out ScrollNudge nudge,
            out GameObject upButton, out GameObject downButton)
        {
            const float Width = 34f;       // 실제 창의 막대처럼 화살표가 들어갈 만큼
            const float ArrowSize = 34f;

            var track = CreatePanel(parent, "Scrollbar", new Color(0.965f, 0.965f, 0.975f, 1f));
            var rt = (RectTransform)track.transform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, -topInset);
            rt.sizeDelta = new Vector2(Width, -topInset);

            // 왼쪽 가장자리의 가는 선. 내용과 막대를 가른다.
            var edge = CreatePanel(track.transform, "Edge", new Color(0.86f, 0.87f, 0.90f, 1f));
            var edgeRt = (RectTransform)edge.transform;
            edgeRt.anchorMin = new Vector2(0f, 0f);
            edgeRt.anchorMax = new Vector2(0f, 1f);
            edgeRt.pivot = new Vector2(0f, 0.5f);
            edgeRt.anchoredPosition = Vector2.zero;
            edgeRt.sizeDelta = new Vector2(1f, 0f);
            edge.GetComponent<Image>().raycastTarget = false;

            upButton = BuildScrollArrow(track.transform, "Btn_ScrollUp", "▲", 1f, ArrowSize);
            downButton = BuildScrollArrow(track.transform, "Btn_ScrollDown", "▼", 0f, ArrowSize);

            // 손잡이가 오르내리는 자리. 위아래 화살표만큼 비켜 둔다.
            var slide = new GameObject("SlidingArea", typeof(RectTransform));
            slide.transform.SetParent(track.transform, false);
            StretchInside((RectTransform)slide.transform, 6f, 6f, ArrowSize + 4f, ArrowSize + 4f);

            var handle = CreatePanel(slide.transform, "Handle", new Color(0.55f, 0.56f, 0.60f, 1f));
            var handleRt = (RectTransform)handle.transform;
            handleRt.sizeDelta = Vector2.zero;

            var bar = track.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = handleRt;
            bar.targetGraphic = handle.GetComponent<Image>();

            nudge = track.AddComponent<ScrollNudge>();
            return bar;
        }

        /// <summary>막대 끝의 화살표 한 개. 위는 pivotY 1, 아래는 0 으로 붙인다.</summary>
        private static GameObject BuildScrollArrow(Transform parent, string name, string glyph,
            float pivotY, float size)
        {
            var go = CreatePanel(parent, name, new Color(0.92f, 0.92f, 0.94f, 1f));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, pivotY);
            rt.anchorMax = new Vector2(0.5f, pivotY);
            rt.pivot = new Vector2(0.5f, pivotY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);

            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var label = AddText(go.transform, "Label", 18f, UIFontWeight.Bold, new Color(0.35f, 0.36f, 0.40f),
                Vector2.zero, new Vector2(size, size), TextAlignmentOptions.Center);
            label.text = glyph;
            label.raycastTarget = false;
            return go;
        }

        /// <summary>
        /// 본문 아래 반응 하나. 옅은 칸에 표시와 숫자를 함께 둔다.
        /// 누르면 눌린 상태가 되고 한 번 더 누르면 풀린다. 그 판정은 화면 쪽이 한다.
        /// </summary>
        private static TextMeshProUGUI AddReactionChip(Transform parent, string name, Color textColor,
            out Button button)
        {
            var chip = CreatePanel(parent, name, new Color(0.955f, 0.958f, 0.97f, 1f));
            var rt = (RectTransform)chip.transform;
            rt.sizeDelta = new Vector2(250f, 76f);

            button = chip.AddComponent<Button>();
            button.targetGraphic = chip.GetComponent<Image>();

            // 눌린 상태를 배경색으로 보여줄 것이라 버튼 자체의 색 변화는 끈다.
            // 그러지 않으면 두 색이 서로 덮어써 눌렸는지 알 수 없게 된다.
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var label = AddText(chip.transform, "Label", 24f, UIFontWeight.Medium, textColor,
                Vector2.zero, new Vector2(250f, 76f), TextAlignmentOptions.Center);
            label.raycastTarget = false;
            StretchInside(label.rectTransform, 12f, 12f, 8f, 8f);
            return label;
        }

        /// <summary>같은 문구를 쓰는 칸 여러 개를 배열 속성에 한 번에 넣는다.</summary>
        private static void SetTextArray(SerializedProperty property, params TextMeshProUGUI[] texts)
        {
            if (property == null) return;

            property.arraySize = texts != null ? texts.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = texts[i];
            }
        }

        /// <summary>
        /// 괴담넷 머리말 한 줄. 목록 화면과 글 화면이 하나씩 따로 쓴다.
        /// 자리(고정이냐 함께 굴러가느냐)는 부르는 쪽이 정한다.
        ///
        /// 사이트 이름과 게시판 이름은 가로 배치에 맡긴다.
        /// 언어가 바뀌어 글자 길이가 달라져도 간격이 유지되기 때문이다.
        /// </summary>
        private static GameObject BuildCommunityHeader(Transform parent, float sideMargin,
            out TextMeshProUGUI site, out TextMeshProUGUI board)
        {
            var header = CreatePanel(parent, "Header", new Color(0.20f, 0.24f, 0.34f, 1f));

            var row = new GameObject("HeaderRow", typeof(RectTransform));
            row.transform.SetParent(header.transform, false);
            var rowRt = (RectTransform)row.transform;
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.offsetMin = new Vector2(sideMargin, 10f);
            rowRt.offsetMax = new Vector2(-sideMargin, -10f);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 20f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            site = AddText(row.transform, "Site", 40f, UIFontWeight.Bold, TextColor,
                Vector2.zero, new Vector2(0f, 60f), TextAlignmentOptions.Left);
            board = AddText(row.transform, "Board", 28f, UIFontWeight.Regular, new Color(0.78f, 0.82f, 0.9f),
                Vector2.zero, new Vector2(0f, 60f), TextAlignmentOptions.Left);
            return header;
        }

        /// <summary>
        /// 자식을 위에서 아래로 쌓고, 쌓인 만큼 제 키를 잡는다.
        /// 글 길이에 따라 칸이 늘어나야 하는 곳에 쓴다. 자리를 숫자로 박지 않아도 된다.
        /// </summary>
        private static VerticalLayoutGroup AddStack(GameObject go, float spacing, RectOffset padding)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return layout;
        }

        /// <summary>쌓아 놓은 칸 사이에 끼우는 가로 선. 높이를 직접 알려 줘야 한다.</summary>
        private static void AddStackRule(Transform parent, Color color, float height)
        {
            var rule = CreatePanel(parent, "Rule", color);
            var img = rule.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;

            var element = rule.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
            AddCrisp(rule, height);
        }

        /// <summary>선이 작은 창에서 사라지지 않도록 실제 픽셀 두께를 지키게 한다.</summary>
        private static void AddCrisp(GameObject rule, float pixels)
        {
            var crisp = rule.AddComponent<CrispRule>();
            var so = new SerializedObject(crisp);
            so.Update();
            so.FindProperty("_pixels").floatValue = pixels;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 부모 안쪽에 여백만큼 띄워 붙인다.
        /// 앵커를 부모에 맞추므로 부모 크기가 바뀌어도 안쪽에 남는다.
        /// </summary>
        private static void StretchInside(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void StretchFull(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, float size, UIFontWeight weight,
            Color color, Vector2 position, Vector2 sizeDelta, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text_" + name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = LoadFont(weight);
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = sizeDelta;
            return text;
        }

        private static GameObject CreateButton(Transform parent, string name, string textId)
        {
            var go = CreatePanel(parent, name, ButtonColor);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var brt = (RectTransform)go.transform;
            brt.sizeDelta = new Vector2(400f, 100f);

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = LoadFont(UIFontWeight.Medium);
            label.fontSize = 30f;
            label.color = TextColor;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var lrt = (RectTransform)labelGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(12f, 8f);
            lrt.offsetMax = new Vector2(-12f, -8f);

            var localized = labelGo.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.Update();
            so.FindProperty("_textId").stringValue = textId;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static TMP_FontAsset LoadFont(UIFontWeight weight)
        {
            var path = FontFolder + "Pretendard-" + weight + " SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) Debug.LogError("[SliceSceneBuilder] 폰트를 찾지 못했다: " + path);
            return font;
        }
    }
}
