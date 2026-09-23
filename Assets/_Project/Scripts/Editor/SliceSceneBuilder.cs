using System.Collections.Generic;
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

            // 시험용 현장에도 같은 두 사람을 세운다. 어느 현장에 들어가도 걸을 수 있어야 한다.
            BuildFieldActors(fieldRoot.transform, -3.4f, -2.0f, 2.4f, -12f, 12f);

            // --- 두 번째 사건의 현장 ---
            var fieldRoot2 = new GameObject("FieldRoot_Legend2");
            BuildFieldBackground(fieldRoot2.transform);
            var panel = BuildPoint(fieldRoot2.transform, "InvestigationPoint_Panel", new Vector2(-3.6f, -0.6f),
                new Vector2(1.6f, 2.6f), new Color(0.42f, 0.46f, 0.52f),
                "field.test.panel", "field.test.panel.result", "clue_test_003");
            var mirror = BuildPoint(fieldRoot2.transform, "InvestigationPoint_Mirror", new Vector2(3.4f, 0.4f),
                new Vector2(2.4f, 3.2f), new Color(0.30f, 0.38f, 0.42f),
                "field.test.mirror", "field.test.mirror.result", null);

            BuildFieldActors(fieldRoot2.transform, -3.4f, -2.0f, 2.4f, -12f, 12f);

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

            // 타기 전에 보이는 승강장. 여기에는 조사할 것이 없다. 열차를 기다리는 자리다.
            var platformOutside = BuildPlatformOutside(fieldRootSubway.transform);

            // 열차 안. 조사 지점은 모두 이 안에 들어 있다.
            // 타기 전에는 이 묶음이 통째로 꺼져 있으므로 아무것도 눌리지 않는다.
            var trainInside = BuildTrainInside(fieldRootSubway.transform);

            // 지점을 열차 배치에 맞춘다.
            //   빈자리 - 왼쪽 긴 의자의 한 칸.
            //   창문   - 오른쪽 긴 의자 위의 창.
            //   CCTV   - 왼쪽 위 천장 모서리.
            //   승강장 - 오른쪽 열린 문 너머.
            var seat = BuildPoint(trainInside.transform, "InvestigationPoint_SubwaySeat", new Vector2(-6.53f, -1.6f),
                new Vector2(1.5f, 2.35f), new Color(0.32f, 0.36f, 0.48f),
                "field.subway.seat", "field.subway.seat", null);
            var window = BuildPoint(trainInside.transform, "InvestigationPoint_SubwayWindow", new Vector2(5.0f, 0.9f),
                new Vector2(4.4f, 2.2f), new Color(0.26f, 0.42f, 0.46f),
                "field.subway.window", "field.subway.window", null);
            // CCTV 는 천장 모서리에 달렸지만, 걸어가 닿을 수 있는 자리여야 한다.
            // 걸을 수 있는 왼쪽 끝이 -12 이라 그보다 바깥에 두면 영영 닿지 못한다.
            var cctv = BuildPoint(trainInside.transform, "InvestigationPoint_SubwayCctv", new Vector2(-10.6f, 2.6f),
                new Vector2(1.6f, 1.15f), new Color(0.46f, 0.40f, 0.30f),
                "field.subway.cctv", "field.subway.cctv", null);
            var platform = BuildPoint(trainInside.transform, "InvestigationPoint_SubwayPlatform", new Vector2(10.0f, -0.9f),
                new Vector2(3.1f, 4.8f), new Color(0.38f, 0.32f, 0.36f),
                "field.subway.platform", "field.subway.platform", null);

            // 좌석에서 얻은 진술이 있어야 영상과 대조할 마음이 든다.
            ConfigurePoint(seat, "point_subway_seat",
                new[] { "action_subway_seat_search", "action_subway_photo" }, null, false, CaseStep.Started);
            ConfigurePoint(window, "point_subway_window",
                new[] { "action_subway_photo", "action_subway_window_trace" }, null, false, CaseStep.Started);
            ConfigurePoint(cctv, "point_subway_cctv",
                new[] { "action_subway_cctv_inspect", "action_subway_photo" },
                new[] { "clue_subway_001" }, false, CaseStep.Started);
            // 글끼리 견줘 보는 조사는 여기에 둔다. 승강장 기록을 뒤지는 자리라 견줄 거리가 있다.
            // 이 방법이 주는 단서(clue_subway_004)가 없으면 맞는 규칙을 세울 수 없다. 어디에도 걸려 있지 않았다.
            ConfigurePoint(platform, "point_subway_platform",
                new[] { "action_subway_platform_search", "action_subway_platform_trace", "action_subway_compare" },
                null, false, CaseStep.Started);

            // 열차가 들어오면 승강장 대신 열차 안이 보인다.
            // 장소를 새로 만들지 않고 보이는 것만 갈아 끼운다.
            var swap = fieldRootSubway.AddComponent<FieldSceneSwap>();
            var swapSo = new SerializedObject(swap);
            swapSo.Update();
            SetObjectArray(swapSo.FindProperty("_beforeRoots"), platformOutside);
            SetObjectArray(swapSo.FindProperty("_afterRoots"), trainInside);
            swapSo.ApplyModifiedPropertiesWithoutUndo();

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

            // 조사할 것 위에 뜨는 말풍선. 현장마다 두지 않고 하나를 옮겨 쓴다.
            fieldSo.FindProperty("_prompt").objectReferenceValue = BuildFieldPrompt(fieldGo.transform);
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
            var memo = BuildMemoScreen("Screen_Memo");
            var toast = BuildToastScreen("Screen_Toast");
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

            // 현장 버튼은 장면을 가리지 않게 작게 줄인다.
            ResizeButton(btnDeduce, new Vector2(280f, 84f), 26f);
            ResizeButton(btnFieldDone, new Vector2(280f, 84f), 26f);
            var btnSeal = CreateButton(exorcismButtons, "Btn_Seal", "ui.seal.btn_seal");
            var btnWithdraw = CreateButton(exorcismButtons, "Btn_Withdraw", "ui.seal.btn_withdraw");
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
            dso.FindProperty("_withdrawButton").objectReferenceValue = btnWithdraw;
            dso.FindProperty("_resultScreen").objectReferenceValue = result;
            dso.FindProperty("_field").objectReferenceValue = field;
            // 괴담넷은 하나뿐이다. 컴퓨터도 휴대폰도 이 화면을 연다. 메모장도 마찬가지다.
            dso.FindProperty("_communityScreen").objectReferenceValue = community;
            dso.FindProperty("_memoScreen").objectReferenceValue = memo;
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
            tso.FindProperty("_memoScreen").objectReferenceValue = memo;
            tso.FindProperty("_toastScreen").objectReferenceValue = toast;
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
            UnityEventTools.AddPersistentListener(btnWithdraw.GetComponent<Button>().onClick, director.OnWithdrawClicked);
            UnityEventTools.AddPersistentListener(btnBack.GetComponent<Button>().onClick, director.OnBackToTitleClicked);
            UnityEventTools.AddPersistentListener(btnClueOk.GetComponent<Button>().onClick, director.OnCluePopupConfirmClicked);
            UnityEventTools.AddPersistentListener(btnRuleOk.GetComponent<Button>().onClick, director.OnRulePopupConfirmClicked);
            UnityEventTools.AddPersistentListener(btnWarningOk.GetComponent<Button>().onClick, director.OnSpreadWarningConfirmClicked);

            // 화면마다 손으로 잡아 둔 글자 크기를 한 규칙으로 맞춘다.
            // 문구가 길어져 칸을 넘치던 곳들이 여기서 한꺼번에 정리된다.
            var screens = new UIScreen[]
            {
                title, bureau, caseList, actionList, ruleList, internetList, internetPage, fieldHud,
                exorcism, result, help, settings, dialogue, talk, desktop, community, memo, toast,
                cluePopup, rulePopup, warningPopup,
            };
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] != null) TidyTexts(screens[i].gameObject);
            }

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

        /// <summary>같은 물건 여럿을 배열 속성에 넣는다.</summary>
        private static void SetObjectArray(SerializedProperty property, params GameObject[] items)
        {
            if (property == null) return;

            property.arraySize = items != null ? items.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        /// <summary>열차 문이 서는 자리. 스크린도어의 트인 곳과 같은 자리다.</summary>
        private static readonly float[] TrainDoorX = { -10f, 0f, 10f };

        /// <summary>문 하나가 트인 폭. 문짝 둘이 반씩 나눠 막는다.</summary>
        private const float DoorGap = 3.6f;

        /// <summary>
        /// 타기 전의 승강장. 열차가 들어오기 전까지만 보인다.
        ///
        /// 오른쪽 어둠에서 열차가 미끄러져 들어와 서고, 스크린도어와 열차 문이 함께 열린다.
        /// 문이 열려야 탈 자리가 켜진다. 그 전에는 눌러 넘어갈 수 없다.
        /// </summary>
        private static GameObject BuildPlatformOutside(Transform parent)
        {
            var root = new GameObject("PlatformOutside");
            root.transform.SetParent(parent, false);

            // 화면 맨 위 한 줄이 세계 좌표 y 3.8 위를 가린다. 보여야 할 것은 모두 그 아래에 둔다.

            // 선로 쪽은 캄캄하다. 그 위로 역사 벽이 얹힌다.
            AddFieldRect(root.transform, "TunnelDark", new Vector2(0f, 0.4f), new Vector2(44f, 6.2f),
                new Color(0.05f, 0.05f, 0.08f), -9);
            AddFieldRect(root.transform, "StationWall", new Vector2(0f, 4.5f), new Vector2(44f, 2.4f),
                new Color(0.19f, 0.19f, 0.24f), -8);
            AddFieldRect(root.transform, "StationSoffit", new Vector2(0f, 3.4f), new Vector2(44f, 0.2f),
                new Color(0.28f, 0.29f, 0.35f), -7);

            // --- 들어오는 열차 ---
            // 스크린도어보다 뒤, 터널 어둠보다 앞이다. 통째로 오른쪽에서 미끄러져 들어온다.
            var leftLeaves = new List<Transform>();
            var rightLeaves = new List<Transform>();

            var train = new GameObject("TrainExterior");
            train.transform.SetParent(root.transform, false);

            AddFieldRect(train.transform, "Body", new Vector2(0f, 0.5f), new Vector2(60f, 5.2f),
                new Color(0.30f, 0.32f, 0.38f), -8);
            AddFieldRect(train.transform, "Underframe", new Vector2(0f, -2.3f), new Vector2(60f, 1.0f),
                new Color(0.14f, 0.15f, 0.19f), -8);
            AddFieldRect(train.transform, "Stripe", new Vector2(0f, 2.55f), new Vector2(60f, 0.36f),
                new Color(0.55f, 0.45f, 0.24f), -7);

            // 문과 문 사이의 창. 안이 훤히 보이지는 않는다.
            float[] windowX = { -15f, -5f, 5f, 15f };
            for (int i = 0; i < windowX.Length; i++)
            {
                AddFieldRect(train.transform, "CarWindowFrame_" + i, new Vector2(windowX[i], 1.0f),
                    new Vector2(2.9f, 2.3f), new Color(0.38f, 0.40f, 0.46f), -7);
                AddFieldRect(train.transform, "CarWindow_" + i, new Vector2(windowX[i], 1.0f),
                    new Vector2(2.6f, 2.0f), new Color(0.07f, 0.08f, 0.12f), -6);
            }

            // 열차 문. 열리면 그 너머로 객실 안이 드러난다.
            // 안쪽은 평평한 빛이 아니라 천장 / 벽 / 바닥으로 나눈다. 그래야 들여다본 것처럼 보인다.
            for (int i = 0; i < TrainDoorX.Length; i++)
            {
                float x = TrainDoorX[i];

                AddFieldRect(train.transform, "DoorInsideWall_" + i, new Vector2(x, 0.5f),
                    new Vector2(DoorGap, 5.0f), new Color(0.22f, 0.23f, 0.28f), -8);
                AddFieldRect(train.transform, "DoorInsideCeiling_" + i, new Vector2(x, 2.5f),
                    new Vector2(DoorGap, 1.0f), new Color(0.44f, 0.43f, 0.37f), -7);
                AddFieldRect(train.transform, "DoorInsideFloor_" + i, new Vector2(x, -1.6f),
                    new Vector2(DoorGap, 1.8f), new Color(0.15f, 0.15f, 0.19f), -7);

                leftLeaves.Add(AddFieldRect(train.transform, "CarDoor_L" + i,
                    new Vector2(x - DoorGap * 0.25f, 0.5f), new Vector2(DoorGap * 0.5f, 5.0f),
                    new Color(0.26f, 0.28f, 0.33f), -5).transform);
                rightLeaves.Add(AddFieldRect(train.transform, "CarDoor_R" + i,
                    new Vector2(x + DoorGap * 0.25f, 0.5f), new Vector2(DoorGap * 0.5f, 5.0f),
                    new Color(0.26f, 0.28f, 0.33f), -5).transform);
            }

            // --- 승강장 쪽 스크린도어 ---
            // 허리 높이의 낮은 것이라 그 너머로 열차가 그대로 보인다.
            AddFieldRect(root.transform, "ScreenDoorRail", new Vector2(0f, -0.52f), new Vector2(44f, 0.22f),
                new Color(0.34f, 0.35f, 0.42f), -4);

            for (int i = 0; i < TrainDoorX.Length; i++)
            {
                float x = TrainDoorX[i];

                // 트인 곳을 막는 유리 문짝 둘. 열차 문과 나란히 물러난다.
                leftLeaves.Add(AddFieldRect(root.transform, "ScreenDoor_L" + i,
                    new Vector2(x - DoorGap * 0.25f, -1.6f), new Vector2(DoorGap * 0.5f, 2.0f),
                    new Color(0.24f, 0.27f, 0.32f), -4).transform);
                rightLeaves.Add(AddFieldRect(root.transform, "ScreenDoor_R" + i,
                    new Vector2(x + DoorGap * 0.25f, -1.6f), new Vector2(DoorGap * 0.5f, 2.0f),
                    new Color(0.24f, 0.27f, 0.32f), -4).transform);

                // 트인 곳 양옆의 기둥. 문짝보다 앞에 서서 물러난 문짝을 가린다.
                AddFieldRect(root.transform, "ScreenDoorPost_A" + i, new Vector2(x - DoorGap * 0.5f, -1.6f),
                    new Vector2(0.34f, 2.2f), new Color(0.34f, 0.35f, 0.42f), -2);
                AddFieldRect(root.transform, "ScreenDoorPost_B" + i, new Vector2(x + DoorGap * 0.5f, -1.6f),
                    new Vector2(0.34f, 2.2f), new Color(0.34f, 0.35f, 0.42f), -2);
            }

            // 문 사이를 잇는 고정 칸막이. 문짝은 이 뒤로 물러난다.
            float[] panelX = { -20f, -15f, -5f, 5f, 15f, 20f };
            for (int i = 0; i < panelX.Length; i++)
            {
                AddFieldRect(root.transform, "ScreenPanel_" + i, new Vector2(panelX[i], -1.6f),
                    new Vector2(6.4f, 2.0f), new Color(0.21f, 0.23f, 0.28f), -3);
            }

            // 발밑. 노란 안전선이 끝에 그어져 있다.
            AddFieldRect(root.transform, "PlatformFloor", new Vector2(0f, -4.1f), new Vector2(44f, 2.9f),
                new Color(0.23f, 0.23f, 0.27f), -1);
            AddFieldRect(root.transform, "SafetyLine", new Vector2(0f, -2.72f), new Vector2(44f, 0.26f),
                new Color(0.72f, 0.62f, 0.26f), 0);

            // 역 이름표. 열차와 겹치지 않게 가운데를 피해 건다.
            AddFieldRect(root.transform, "SignHanger", new Vector2(-5f, 3.05f), new Vector2(0.18f, 0.7f),
                new Color(0.30f, 0.31f, 0.38f), -3);
            AddFieldRect(root.transform, "Sign", new Vector2(-5f, 2.2f), new Vector2(5.2f, 1.1f),
                new Color(0.16f, 0.20f, 0.30f), -3);

            // --- 탈 자리 ---
            // 가운데 문 앞. 문이 다 열린 뒤에만 켜진다.
            var boarding = BuildPoint(root.transform, "InvestigationPoint_SubwayBoard", new Vector2(0f, 0.4f),
                new Vector2(DoorGap - 0.4f, 4.6f), new Color(0.40f, 0.44f, 0.36f),
                "field.subway.board", "field.subway.board", null);
            ConfigurePoint(boarding, CaseDirector.BoardingPointId, null, null, false, CaseStep.Started);
            boarding.SetActive(false);

            // 승강장에 선 두 사람. 발은 안전선 안쪽, 바닥 위에 놓는다.
            // 가운데 문 앞(x 0)은 타는 자리라 비워 두고 왼쪽에 선다.
            BuildFieldActors(root.transform, -2.78f, -3.2f, 2.4f, -12f, 12f);

            var arrival = root.AddComponent<TrainArrival>();
            var aso = new SerializedObject(arrival);
            aso.Update();
            aso.FindProperty("_train").objectReferenceValue = train.transform;
            aso.FindProperty("_doorSlide").floatValue = DoorGap * 0.5f;
            aso.FindProperty("_boardingPoint").objectReferenceValue = boarding;
            SetTransformArray(aso.FindProperty("_leftLeaves"), leftLeaves);
            SetTransformArray(aso.FindProperty("_rightLeaves"), rightLeaves);
            aso.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return root;
        }

        /// <summary>물건 여럿을 배열 속성에 넣는다. 종류를 가리지 않는다.</summary>
        private static void SetObjectList(SerializedProperty property, params UnityEngine.Object[] items)
        {
            if (property == null) return;

            property.arraySize = items != null ? items.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        /// <summary>문짝 목록을 배열 속성에 넣는다.</summary>
        private static void SetTransformArray(SerializedProperty property, List<Transform> items)
        {
            if (property == null) return;

            property.arraySize = items != null ? items.Count : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        /// <summary>
        /// 열차 안. 열차가 들어온 뒤로 계속 보이는 자리다.
        ///
        /// 옆에서 본 객실이다. 아래에 긴 의자 둘, 그 위에 창 둘,
        /// 가운데와 오른쪽에 문, 왼쪽 위 모서리에 CCTV 자리가 온다.
        /// 그림은 아직 없다. 네모로만 잡아 둔다.
        /// </summary>
        private static GameObject BuildTrainInside(Transform parent)
        {
            var root = new GameObject("TrainInside");
            root.transform.SetParent(parent, false);

            var wall = new Color(0.19f, 0.20f, 0.25f);
            var trim = new Color(0.28f, 0.29f, 0.35f);
            var dark = new Color(0.12f, 0.12f, 0.16f);

            // 객실 껍데기. 벽 / 천장 / 바닥.
            // 화면 맨 위 한 줄이 세계 좌표 y 3.8 위를 가리므로, 천장은 그보다 아래에서 시작한다.
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), wall, -9);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 4.45f), new Vector2(44f, 1.5f),
                new Color(0.15f, 0.15f, 0.19f), -8);
            AddFieldRect(root.transform, "CeilingEdge", new Vector2(0f, 3.62f), new Vector2(44f, 0.16f), trim, -7);

            // 천장 형광등. 객실 안이 왜 이 색인지 눈에 잡히게 한다.
            for (int i = -3; i <= 3; i++)
            {
                AddFieldRect(root.transform, "CeilingLamp_" + (i + 3), new Vector2(i * 4.4f, 4.0f),
                    new Vector2(3.0f, 0.24f), new Color(0.58f, 0.58f, 0.52f), -7);
            }

            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f),
                new Color(0.14f, 0.14f, 0.18f), -8);
            AddFieldRect(root.transform, "FloorEdge", new Vector2(0f, -3.42f), new Vector2(44f, 0.16f), trim, -7);

            // 문. 가운데와 양 끝. 오른쪽 문만 열려 있어 승강장이 보인다.
            BuildTrainDoor(root.transform, "Door_Center", 0f, false);
            BuildTrainDoor(root.transform, "Door_Left", -10.0f, false);
            BuildTrainDoor(root.transform, "Door_Right", 10.0f, true);

            // 긴 의자 둘. 문 사이에 하나씩 들어간다.
            BuildTrainBench(root.transform, "Bench_Left", -5.0f, 4.6f);
            BuildTrainBench(root.transform, "Bench_Right", 5.0f, 4.6f);

            // 의자 위의 창. 바깥은 캄캄한 터널이다.
            BuildTrainWindow(root.transform, "Window_Left", -5.0f, 4.6f);
            BuildTrainWindow(root.transform, "Window_Right", 5.0f, 4.6f);

            // 손잡이 봉과 거기 매달린 고리들. 봉은 천장에 세운 기둥 둘이 받친다.
            AddFieldRect(root.transform, "Handrail", new Vector2(0f, 3.0f), new Vector2(17.0f, 0.16f),
                new Color(0.36f, 0.37f, 0.43f), -5);
            AddFieldRect(root.transform, "HandrailPost_L", new Vector2(-8.3f, 3.3f), new Vector2(0.14f, 0.8f),
                new Color(0.34f, 0.35f, 0.41f), -5);
            AddFieldRect(root.transform, "HandrailPost_R", new Vector2(8.3f, 3.3f), new Vector2(0.14f, 0.8f),
                new Color(0.34f, 0.35f, 0.41f), -5);
            for (int i = -4; i <= 4; i++)
            {
                if (i == 0) continue;   // 가운데 문 위는 비운다
                AddFieldRect(root.transform, "Strap_" + (i + 4), new Vector2(i * 1.9f, 2.42f),
                    new Vector2(0.1f, 1.0f), new Color(0.30f, 0.31f, 0.36f), -5);
                AddFieldRect(root.transform, "StrapRing_" + (i + 4), new Vector2(i * 1.9f, 1.82f),
                    new Vector2(0.44f, 0.44f), new Color(0.34f, 0.31f, 0.24f), -5);
            }

            // 왼쪽 위 모서리의 CCTV. 천장에서 내려온 팔에 매달린다.
            // 이름표가 맨 위 한 줄에 가리지 않도록 조사 지점을 y 2.6 에 둔다.
            // 걸어가서 닿는 자리여야 한다. 걸을 수 있는 왼쪽 끝(-12)보다 안쪽에 둔다.
            AddFieldRect(root.transform, "CctvArm", new Vector2(-10.6f, 3.25f), new Vector2(0.28f, 1.0f), trim, -5);
            AddFieldRect(root.transform, "CctvShade", new Vector2(-10.6f, 2.6f), new Vector2(2.2f, 1.7f), dark, -6);

            // 객실 안에 선 두 사람. 바닥 위에 놓는다. 의자와 봉 사이를 오간다.
            BuildFieldActors(root.transform, -3.86f, -2.0f, 2.4f, -12f, 12f);

            root.SetActive(false);
            return root;
        }

        /// <summary>객실 문 한 짝. 열린 문은 안쪽이 승강장 빛으로 밝다.</summary>
        private static void BuildTrainDoor(Transform parent, string name, float x, bool open)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);

            // 문틀.
            AddFieldRect(go.transform, "Frame", new Vector2(0f, -0.6f), new Vector2(4.6f, 6.0f),
                new Color(0.24f, 0.25f, 0.31f), -7);

            if (open)
            {
                // 열린 문. 문짝은 양옆으로 물러나고 가운데는 승강장 불빛이다.
                AddFieldRect(go.transform, "Opening", new Vector2(0f, -0.6f), new Vector2(3.6f, 5.6f),
                    new Color(0.30f, 0.29f, 0.27f), -6);
                AddFieldRect(go.transform, "Leaf_L", new Vector2(-2.1f, -0.6f), new Vector2(0.6f, 5.6f),
                    new Color(0.20f, 0.21f, 0.26f), -5);
                AddFieldRect(go.transform, "Leaf_R", new Vector2(2.1f, -0.6f), new Vector2(0.6f, 5.6f),
                    new Color(0.20f, 0.21f, 0.26f), -5);
                return;
            }

            // 닫힌 문. 문짝 둘과 각각의 작은 창.
            for (int i = 0; i < 2; i++)
            {
                float leafX = i == 0 ? -1.05f : 1.05f;
                AddFieldRect(go.transform, "Leaf_" + i, new Vector2(leafX, -0.6f), new Vector2(2.0f, 5.6f),
                    new Color(0.21f, 0.22f, 0.27f), -6);
                AddFieldRect(go.transform, "LeafGlass_" + i, new Vector2(leafX, 0.9f), new Vector2(1.5f, 2.2f),
                    new Color(0.08f, 0.10f, 0.14f), -5);
            }
        }

        /// <summary>긴 의자 하나. 등받이와 앉는 자리와 아래 받침으로 나눈다.</summary>
        private static void BuildTrainBench(Transform parent, string name, float x, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0f, 0f);

            var fabric = new Color(0.26f, 0.28f, 0.36f);

            AddFieldRect(go.transform, "Back", new Vector2(0f, -1.35f), new Vector2(width, 1.9f), fabric, -6);
            AddFieldRect(go.transform, "Seat", new Vector2(0f, -2.45f), new Vector2(width, 0.6f),
                Color.Lerp(fabric, Color.white, 0.12f), -5);
            AddFieldRect(go.transform, "Skirt", new Vector2(0f, -3.1f), new Vector2(width, 0.8f),
                new Color(0.17f, 0.18f, 0.22f), -6);

            // 한 사람 자리를 가르는 금. 빈자리가 어디인지 눈에 들어오게 한다.
            int slots = Mathf.RoundToInt(width / 1.65f);
            for (int i = 1; i < slots; i++)
            {
                float lineX = -width * 0.5f + i * (width / slots);
                AddFieldRect(go.transform, "SeatLine_" + i, new Vector2(lineX, -2.0f), new Vector2(0.06f, 2.9f),
                    new Color(0.19f, 0.20f, 0.26f), -4);
            }

            // 양 끝의 칸막이.
            AddFieldRect(go.transform, "Divider_L", new Vector2(-width * 0.5f - 0.2f, -1.7f),
                new Vector2(0.4f, 3.6f), new Color(0.31f, 0.32f, 0.38f), -4);
            AddFieldRect(go.transform, "Divider_R", new Vector2(width * 0.5f + 0.2f, -1.7f),
                new Vector2(0.4f, 3.6f), new Color(0.31f, 0.32f, 0.38f), -4);
        }

        /// <summary>의자 위의 창 하나. 유리와 테두리와 가운데 세로살.</summary>
        private static void BuildTrainWindow(Transform parent, string name, float x, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 0.9f, 0f);

            AddFieldRect(go.transform, "Frame", Vector2.zero, new Vector2(width + 0.4f, 2.7f),
                new Color(0.30f, 0.31f, 0.37f), -7);
            AddFieldRect(go.transform, "Glass", Vector2.zero, new Vector2(width, 2.3f),
                new Color(0.07f, 0.09f, 0.13f), -6);
            AddFieldRect(go.transform, "Mullion", Vector2.zero, new Vector2(0.16f, 2.3f),
                new Color(0.30f, 0.31f, 0.37f), -5);
        }

        /// <summary>현장 배경에 까는 네모 하나. 조사 지점이 아니라 그냥 그림이다.</summary>
        private static GameObject AddFieldRect(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BuiltinSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        /// <summary>
        /// 휴대폰 옆면의 단추 하나. 껍데기 밖으로 살짝 튀어나온다.
        /// 눌리지는 않는다. 판때기가 아니라 손에 쥔 물건으로 보이게 하는 것이 전부다.
        /// </summary>
        private static void AddPhoneSideKey(Transform phone, string name, float side, float y, float height)
        {
            var key = CreatePanel(phone, name, new Color(0.26f, 0.27f, 0.33f, 1f));
            var rt = (RectTransform)key.transform;
            rt.anchorMin = new Vector2(side, 1f);
            rt.anchorMax = new Vector2(side, 1f);
            rt.pivot = new Vector2(side, 1f);
            // 폭의 반만 껍데기 안에 걸치게 해서 옆으로 튀어나온 것처럼 보이게 한다.
            rt.anchoredPosition = new Vector2(side > 0.5f ? 5f : -5f, y);
            rt.sizeDelta = new Vector2(10f, height);
            key.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>휴대폰 안의 앱 하나. 네모와 이름표로 둔다. 컴퓨터 아이콘과 같은 모양이다.</summary>
        private static Button BuildPhoneIcon(Transform parent, string id, Vector2 position, float size,
            out TMP_Text label)
        {
            var go = new GameObject("App_" + id, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(size, size + 26f);

            var button = go.AddComponent<Button>();

            var box = CreatePanel(go.transform, "Box", new Color(0.24f, 0.26f, 0.34f, 1f));
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 1f);
            boxRt.anchorMax = new Vector2(0.5f, 1f);
            boxRt.pivot = new Vector2(0.5f, 1f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(size, size);
            button.targetGraphic = box.GetComponent<Image>();

            // 아이콘이 작으므로 이름표도 같이 줄인다. 이름이 길어도 한 줄에 들어가게 넉넉히 넓힌다.
            label = AddText(go.transform, "Label", 16f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(size + 30f, 26f), TextAlignmentOptions.Center);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0f);
            labelRt.anchorMax = new Vector2(0.5f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(size + 30f, 26f);
            label.raycastTarget = false;

            return button;
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

        /// <summary>
        /// 현장에 세우는 인물 둘. 앞의 하나를 걷게 하고, 뒤의 하나가 그를 따라간다.
        ///
        /// 그림은 아직 없다. 머리와 몸과 다리를 네모로만 잡아 둔 임시 모습이다.
        /// 실제 그림이 생기면 이 네모들만 갈아 끼우면 된다. 걷고 따라가는 일은 그대로 둔다.
        ///
        /// groundY 는 발이 닿는 높이다. 현장마다 바닥 높이가 달라 밖에서 받는다.
        /// </summary>
        /// <remarks>
        /// 인물은 짜 놓은 크기(키 1.8)에서 이만큼 키워 세운다.
        /// 씬에서 직접 늘려 보고 정한 값이라 여기 한 곳에만 둔다. 크기를 바꾸려면 이 숫자만 고친다.
        /// 좌우를 뒤집을 때 절댓값을 쓰므로 키운 크기는 뒤집어도 그대로 남는다.
        /// </remarks>
        private const float ActorScale = 1.5f;

        /// <summary>
        /// 조사할 것 위에 뜨는 말풍선 하나.
        ///
        /// 월드에 놓이는 물건이라 UI 캔버스가 아니라 3D TextMeshPro 를 쓴다.
        /// 장면의 어떤 네모보다도 앞에 서야 하므로 그리는 순서를 넉넉히 높게 둔다.
        /// 현장에 하나만 두고 가까이 간 지점으로 옮겨 다닌다.
        /// </summary>
        private static FieldPrompt BuildFieldPrompt(Transform parent)
        {
            const int Order = 30;

            var go = new GameObject("FieldPrompt");
            go.transform.SetParent(parent, false);

            // 3D TextMeshPro 의 글자 크기는 월드 단위의 열 배쯤이다.
            // 4.2 가 0.42 단위, 화면에서 26픽셀 남짓 된다. 읽히면서 장면을 가리지 않는 크기다.
            //
            // 판은 글자를 따라 커진다. 크기를 다시 잡을 일이 생기면 FontSize 하나만 고친다.
            const float FontSize = 4.2f;
            const float PlateWidth = FontSize * 1.17f;    // = 4.91
            const float PlateHeight = FontSize * 0.2f;    // = 0.84

            var plate = AddFieldRect(go.transform, "Plate", Vector2.zero, new Vector2(PlateWidth, PlateHeight),
                new Color(0.06f, 0.06f, 0.09f, 0.92f), Order);

            // 말풍선 아래쪽의 뾰족한 끝. 어느 것을 가리키는지 알게 한다.
            var tail = AddFieldRect(go.transform, "Tail", new Vector2(0f, -PlateHeight * 0.55f),
                new Vector2(0.26f, 0.26f), new Color(0.06f, 0.06f, 0.09f, 0.92f), Order);

            var edge = AddFieldRect(plate.transform, "Edge", new Vector2(0f, 0f),
                new Vector2(PlateWidth + 0.1f, PlateHeight + 0.1f),
                new Color(0.86f, 0.74f, 0.48f, 0.9f), Order - 1);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);

            var label = textGo.AddComponent<TextMeshPro>();
            label.font = LoadFont(UIFontWeight.SemiBold);
            label.fontSize = FontSize;
            label.color = TextColor;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;

            // 긴 이름은 글자를 조금 줄여 담는다. 줄을 바꾸지는 않는다.
            label.enableAutoSizing = true;
            label.fontSizeMin = FontSize * 0.7f;
            label.fontSizeMax = FontSize;
            label.rectTransform.sizeDelta = new Vector2(PlateWidth - 0.28f, PlateHeight - 0.14f);

            var mesh = textGo.GetComponent<MeshRenderer>();
            if (mesh != null) mesh.sortingOrder = Order + 1;

            var prompt = go.AddComponent<FieldPrompt>();
            var pso = new SerializedObject(prompt);
            pso.Update();
            pso.FindProperty("_label").objectReferenceValue = label;
            pso.FindProperty("_plate").objectReferenceValue = plate.GetComponent<SpriteRenderer>();
            pso.FindProperty("_tail").objectReferenceValue = tail.GetComponent<SpriteRenderer>();
            pso.FindProperty("_edge").objectReferenceValue = edge.GetComponent<SpriteRenderer>();
            pso.ApplyModifiedPropertiesWithoutUndo();

            return prompt;
        }

        private static void BuildFieldActors(Transform parent, float groundY, float leadX, float gap,
            float minX, float maxX)
        {
            var lead = BuildFieldActor(parent, "Actor_Chajihan", new Vector2(leadX, groundY),
                new Color(0.42f, 0.52f, 0.72f), new Color(0.86f, 0.78f, 0.68f));
            lead.transform.localScale = Vector3.one * ActorScale;

            var walker = lead.AddComponent<FieldWalker>();
            var wso = new SerializedObject(walker);
            wso.Update();
            wso.FindProperty("_minX").floatValue = minX;
            wso.FindProperty("_maxX").floatValue = maxX;
            wso.ApplyModifiedPropertiesWithoutUndo();

            var mate = BuildFieldActor(parent, "Actor_Hanyoung", new Vector2(leadX - gap, groundY),
                new Color(0.62f, 0.44f, 0.40f), new Color(0.88f, 0.80f, 0.70f));
            mate.transform.localScale = Vector3.one * ActorScale;

            var follower = mate.AddComponent<FieldFollower>();
            var fso = new SerializedObject(follower);
            fso.Update();
            fso.FindProperty("_target").objectReferenceValue = lead.transform;
            fso.FindProperty("_gap").floatValue = gap;
            fso.FindProperty("_minX").floatValue = minX;
            fso.FindProperty("_maxX").floatValue = maxX;
            fso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 임시 인물 하나. 발이 닿는 자리를 기준으로 위로 쌓는다.
        ///
        /// 2등신이다. 키 1.8 중 위의 0.9 가 머리고 아래 0.9 에 몸과 다리가 들어간다.
        /// 눈 둘을 보는 쪽으로 몰아 찍는다. 좌우를 뒤집었을 때 어디를 보는지 그것으로 안다.
        /// </summary>
        private static GameObject BuildFieldActor(Transform parent, string name, Vector2 footPosition,
            Color cloth, Color skin)
        {
            const int Order = 3;   // 승강장 문짝과 안전선보다 앞이다

            // 2등신의 기준. 키의 절반이 머리다.
            const float Height = 1.8f;
            const float HeadSize = Height * 0.5f;      // = 0.9
            const float BodyTop = Height - HeadSize;   // = 0.9

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = footPosition;

            var leg = new Color(0.17f, 0.18f, 0.23f);

            // 다리 둘. 짧고 굵다. 발끝이 바닥에 닿는다.
            AddFieldRect(go.transform, "Leg_L", new Vector2(-0.13f, 0.16f), new Vector2(0.20f, 0.32f), leg, Order);
            AddFieldRect(go.transform, "Leg_R", new Vector2(0.13f, 0.16f), new Vector2(0.20f, 0.32f), leg, Order);

            // 몸통. 머리를 받치는 자리라 작다.
            AddFieldRect(go.transform, "Body", new Vector2(0f, 0.61f), new Vector2(0.58f, 0.58f), cloth, Order);

            // 팔 둘. 몸통 옆으로 살짝 나온다.
            AddFieldRect(go.transform, "Arm_L", new Vector2(-0.36f, 0.62f), new Vector2(0.14f, 0.42f), cloth, Order);
            AddFieldRect(go.transform, "Arm_R", new Vector2(0.36f, 0.62f), new Vector2(0.14f, 0.42f), cloth, Order);

            // 머리. 키의 절반을 차지한다.
            AddFieldRect(go.transform, "Head", new Vector2(0f, BodyTop + HeadSize * 0.5f),
                new Vector2(HeadSize * 0.94f, HeadSize), skin, Order);

            // 앞머리. 머리 위쪽을 덮는다.
            AddFieldRect(go.transform, "Hair", new Vector2(0f, Height - 0.16f), new Vector2(HeadSize, 0.32f),
                new Color(0.13f, 0.12f, 0.14f), Order + 1);

            // 눈 둘. 보는 쪽으로 몰아 찍는다.
            var pupil = new Color(0.12f, 0.11f, 0.13f);
            AddFieldRect(go.transform, "Eye_A", new Vector2(-0.02f, 1.28f), new Vector2(0.11f, 0.15f), pupil, Order + 1);
            AddFieldRect(go.transform, "Eye_B", new Vector2(0.24f, 1.28f), new Vector2(0.11f, 0.15f), pupil, Order + 1);

            return go;
        }

        private static GameObject BuildPoint(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, string nameTextId, string resultTextId, string clueId)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            // 평소에는 보이지 않는다. 조사할 곳을 미리 칠해 두면 장면이 문제집처럼 보인다.
            // 걸어가 곁에 섰을 때에만 그 자리가 밝아진다. 색은 InvestigationPoint 가 갈아 끼운다.
            var hidden = new Color(color.r, color.g, color.b, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BuiltinSprite();
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = hidden;
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
            so.FindProperty("_normalColor").colorValue = hidden;

            // 곁에 섰을 때. 원래 색을 밝힌 것을 반쯤 비쳐 보이게 덮는다. 아래 그림이 죽지 않는다.
            var near = Color.Lerp(color, Color.white, 0.45f);
            so.FindProperty("_pressedColor").colorValue = new Color(near.r, near.g, near.b, 0.42f);

            // 조사를 마치면 다시 숨는다. 떠난 자리가 계속 빛날 이유가 없다.
            so.FindProperty("_investigatedColor").colorValue = hidden;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 이름표는 두지 않는다. 무엇을 조사하는지는 가까이 갔을 때 말풍선이 알려 준다.
            return go;
        }

        private static Sprite BuiltinSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        /// <summary>
        /// 휴대폰 위쪽 가운데의 작은 동그란 카메라. 요즘 휴대폰은 노치 대신 구멍 하나다.
        /// 휴대폰 화면에도, 그 위를 덮는 앱의 맨 윗줄에도 같은 자리에 하나씩 둔다.
        /// </summary>
        private static GameObject BuildPhoneCamera(Transform parent, float size, float top)
        {
            var camera = CreatePanel(parent, "Camera", new Color(0.02f, 0.02f, 0.03f, 1f));
            var image = camera.GetComponent<Image>();
            image.sprite = RoundSprite();
            image.raycastTarget = false;

            var rt = (RectTransform)camera.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, top);
            rt.sizeDelta = new Vector2(size, size);

            // 렌즈. 구멍만 있으면 얼룩으로 보인다. 가운데에 한 점 빛이 있어야 렌즈가 된다.
            var lens = CreatePanel(camera.transform, "Lens", new Color(0.26f, 0.30f, 0.44f, 1f));
            var lensImage = lens.GetComponent<Image>();
            lensImage.sprite = RoundSprite();
            lensImage.raycastTarget = false;
            StretchInside((RectTransform)lens.transform, 4f, 4f, 4f, 4f);

            return camera;
        }

        /// <summary>동그란 것에 쓰는 기본 그림. 유니티가 들고 있는 손잡이 그림이 원이다.</summary>
        private static Sprite RoundSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        // ------------------------------------------------------------- 화면

        private static TextPanelScreen BuildPanelScreen(string name, UILayer layer, out Transform buttonRow, bool fullScreen)
        {
            var go = CreatePanel(null, name, fullScreen ? PanelColor : Color.clear);
            StretchFull(go);

            var screen = go.AddComponent<TextPanelScreen>();
            ConfigureScreen(screen, name, layer, true, true);

            // 위에서부터 자리를 나눠 준다. 제목 360~260, 본문 230~-170, 안내 -195~-265.
            var titleText = AddText(go.transform, "Title", 64f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 310f), new Vector2(1500f, 100f), TextAlignmentOptions.Center);
            var bodyText = AddText(go.transform, "Body", 34f, UIFontWeight.Regular, TextColor,
                new Vector2(0f, 30f), new Vector2(1400f, 400f), TextAlignmentOptions.Top);
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

            // 위에서부터 자리를 나눠 준다. 제목 435~365, 안내 355~305, 목록, 현황 -215~-275, 결과 -295~-345.
            var titleText = AddText(go.transform, "Title", 56f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 400f), new Vector2(1500f, 70f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 330f), new Vector2(1500f, 50f), TextAlignmentOptions.Center);
            var statsText = AddText(go.transform, "Stats", 30f, UIFontWeight.SemiBold, AccentColor,
                new Vector2(0f, -245f), new Vector2(1500f, 60f), TextAlignmentOptions.Center);
            var resultText = AddText(go.transform, "Result", 30f, UIFontWeight.Medium, WarnColor,
                new Vector2(0f, -320f), new Vector2(1500f, 50f), TextAlignmentOptions.Center);

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

            // 현장의 대사 띠와 같은 규칙을 쓴다. 두 곳의 대사가 같은 모습으로 보여야 한다.
            ConfigureBodyText(lineText, 34f);

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

            // 좁은 칸이라 두 줄까지 접힌다. 그래도 넘치면 글자를 줄여 칸 안에 담는다.
            ConfigureBodyText(dcLabel, 24f, 0.75f);
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

        /// <summary>어두운 상태 줄 위에 얹히는 믿음도. 흰 종이 위보다 밝은 붉은색이어야 읽힌다.</summary>
        private static readonly Color PhoneBeliefColor = new Color(0.92f, 0.34f, 0.32f);

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
                var icon = BuildDesktopIcon(go.transform, apps[i][0], new Vector2(-780f, 380f - i * 150f),
                    out var iconLabel, out var iconHint, out var iconBadge, out var iconBadgeCount);

                var element = iconList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("appId").stringValue = apps[i][0];
                element.FindPropertyRelative("labelTextId").stringValue = apps[i][1];
                element.FindPropertyRelative("button").objectReferenceValue = icon.GetComponent<Button>();
                element.FindPropertyRelative("label").objectReferenceValue = iconLabel;
                element.FindPropertyRelative("hint").objectReferenceValue = iconHint;
                element.FindPropertyRelative("badge").objectReferenceValue = iconBadge;
                element.FindPropertyRelative("badgeCount").objectReferenceValue = iconBadgeCount;
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

        /// <summary>
        /// 바탕화면 아이콘 하나. 네모 하나와 이름표로 둔다.
        ///
        /// 표시 두 개가 더 붙는다.
        ///   배지 - 네모 오른쪽 위에 얹는 작은 딱지. 새로 올라온 것의 개수를 적는다. 셀 것이 없으면 꺼진다.
        ///   손짓 - 네모 오른쪽에서 아이콘을 가리키는 세모. 지금 눌러야 할 때만 켜진다.
        /// 그림 파일을 두지 않고 네모와 글자로 그린다. 실제 아이콘 그림이 생기면 네모만 갈아 끼우면 된다.
        /// </summary>
        private static GameObject BuildDesktopIcon(Transform parent, string id, Vector2 position,
            out TMP_Text label, out GameObject hint, out GameObject badge, out TMP_Text badgeCount)
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

            label = AddText(go.transform, "Label", 24f, UIFontWeight.Medium, TextColor,
                new Vector2(0f, -44f), new Vector2(200f, 40f), TextAlignmentOptions.Center);
            label.raycastTarget = false;

            // --- 새 글 개수 배지 ---
            // 네모 오른쪽 위에 걸친다. 숫자를 채우고 켜는 것은 DesktopScreen 이 한다.
            var count = CreatePanel(go.transform, "Badge", new Color(0.80f, 0.22f, 0.24f, 1f));
            var countRt = (RectTransform)count.transform;
            countRt.anchoredPosition = new Vector2(46f, 48f);
            countRt.sizeDelta = new Vector2(46f, 36f);
            count.GetComponent<Image>().raycastTarget = false;

            badgeCount = AddText(count.transform, "Count", 24f, UIFontWeight.Bold, Color.white,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(badgeCount.rectTransform, 4f, 4f, 2f, 2f);
            ConfigureOneLineText(badgeCount, 24f, 0.6f);

            badge = count;
            badge.SetActive(false);

            // --- 가리키는 세모 ---
            // 네모 오른쪽에 서서 왼쪽을 가리킨다. 켜고 끄는 것은 DesktopScreen 이 한다.
            //
            // 위를 보는 세모 글자를 눕혀 쓴다. 왼쪽을 보는 글자를 따로 쓰면
            // 글꼴에 그 글자가 없을 때 네모로 뜬다. 한 글자만 쓰면 그럴 일이 없다.
            // 아이콘 곁에 붙는 표시다. 아이콘보다 커 보이면 안 된다.
            var point = AddText(go.transform, "Hint", 34f, UIFontWeight.Bold, AccentColor,
                new Vector2(103f, 24f), new Vector2(48f, 48f), TextAlignmentOptions.Center);
            point.text = "▲";
            point.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            point.raycastTarget = false;

            // 가만히 서 있으면 무늬로 보인다. 조금씩 좌우로 움직여 가리키게 한다.
            point.gameObject.AddComponent<HintNudge>();

            hint = point.gameObject;
            hint.SetActive(false);

            return go;
        }

        /// <summary>
        /// 메모장.
        ///
        /// 괴담넷과 같은 짜임이다. 창 하나를 두고 컴퓨터에서는 늘리고 휴대폰에서는 줄여 쓴다.
        /// 안은 제목 줄과 적는 자리와 아래 한 줄뿐이다. 실제 메모장이 그만큼만 가지고 있다.
        /// </summary>
        private static MemoScreen BuildMemoScreen(string name)
        {
            // 화면 뿌리는 투명하다. 창 바깥은 아래 화면(바탕화면이나 현장)이 그대로 보여야 한다.
            var go = CreatePanel(null, name, new Color(0f, 0f, 0f, 0f));
            StretchFull(go);

            var screen = go.AddComponent<MemoScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, false, true);

            var paper = new Color(0.97f, 0.96f, 0.93f, 1f);
            var ink = new Color(0.12f, 0.12f, 0.14f, 1f);
            var faded = new Color(0.55f, 0.54f, 0.50f, 1f);

            // 창은 화면을 다 덮지 않는다. 가운데에 이만큼만 뜨고, 제목 줄을 잡아 옮길 수 있다.
            var window = CreatePanel(go.transform, "Window", paper);
            var windowRt = (RectTransform)window.transform;
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.anchoredPosition = Vector2.zero;
            windowRt.sizeDelta = new Vector2(1180f, 760f);

            const float BarHeight = 56f;
            const float TabHeight = 46f;
            const float FooterHeight = 40f;

            // --- 제목 줄 ---
            var titleBar = CreatePanel(window.transform, "TitleBar", new Color(0.13f, 0.14f, 0.18f, 1f));
            var barRt = (RectTransform)titleBar.transform;
            barRt.anchorMin = new Vector2(0f, 1f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = Vector2.zero;
            barRt.sizeDelta = new Vector2(0f, BarHeight);

            var windowTitle = AddText(titleBar.transform, "WindowTitle", 28f, UIFontWeight.Medium, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(windowTitle.rectTransform, 24f, 100f, 8f, 8f);
            windowTitle.textWrappingMode = TextWrappingModes.NoWrap;

            var closeGo = CreatePanel(titleBar.transform, "Btn_CloseMemo", new Color(0.62f, 0.22f, 0.24f, 1f));
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(1f, 0.5f);
            closeRt.anchorMax = new Vector2(1f, 0.5f);
            closeRt.pivot = new Vector2(1f, 0.5f);
            closeRt.anchoredPosition = new Vector2(-12f, 0f);
            closeRt.sizeDelta = new Vector2(56f, 40f);

            var closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeGo.GetComponent<Image>();

            var closeLabel = AddText(closeGo.transform, "Label", 26f, UIFontWeight.Bold, TextColor,
                Vector2.zero, new Vector2(56f, 40f), TextAlignmentOptions.Center);
            closeLabel.text = "X";
            closeLabel.raycastTarget = false;

            // 제목 줄을 잡으면 창이 끌린다. 실제 창이 그렇다.
            // 단추들은 제 클릭을 먼저 가져가므로 X 나 + 를 눌러도 창이 끌리지 않는다.
            var drag = titleBar.AddComponent<WindowDrag>();
            var dso2 = new SerializedObject(drag);
            dso2.Update();
            dso2.FindProperty("_target").objectReferenceValue = windowRt;
            dso2.ApplyModifiedPropertiesWithoutUndo();

            // 메모를 한 장 더 만드는 단추. 닫기 왼쪽에 둔다.
            var newGo = CreatePanel(titleBar.transform, "Btn_NewNote", new Color(0.24f, 0.30f, 0.42f, 1f));
            var newRt = (RectTransform)newGo.transform;
            newRt.anchorMin = new Vector2(1f, 0.5f);
            newRt.anchorMax = new Vector2(1f, 0.5f);
            newRt.pivot = new Vector2(1f, 0.5f);
            newRt.anchoredPosition = new Vector2(-80f, 0f);
            newRt.sizeDelta = new Vector2(180f, 40f);

            var newButton = newGo.AddComponent<Button>();
            newButton.targetGraphic = newGo.GetComponent<Image>();

            var newLabel = AddText(newGo.transform, "Label", 22f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(180f, 40f), TextAlignmentOptions.Center);
            newLabel.raycastTarget = false;
            newLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // --- 메모 장 탭 ---
            // 제목 줄 바로 아래. 적어 둔 장들이 여기에 늘어선다.
            var tabStrip = CreatePanel(window.transform, "TabStrip", new Color(0.88f, 0.87f, 0.83f, 1f));
            var tabStripRt = (RectTransform)tabStrip.transform;
            tabStripRt.anchorMin = new Vector2(0f, 1f);
            tabStripRt.anchorMax = new Vector2(1f, 1f);
            tabStripRt.pivot = new Vector2(0.5f, 1f);
            tabStripRt.anchoredPosition = new Vector2(0f, -BarHeight);
            tabStripRt.sizeDelta = new Vector2(0f, TabHeight);

            // 좁은 화면(휴대폰)에서는 탭이 줄을 넘는다. 잘라서 보여주고 끌어서 옆으로 넘긴다.
            // 막대는 두지 않는다. 손으로 미는 것이 휴대폰에서 더 자연스럽다.
            var tabScroll = tabStrip.AddComponent<ScrollRect>();
            tabScroll.horizontal = true;
            tabScroll.vertical = false;
            tabScroll.movementType = ScrollRect.MovementType.Clamped;
            tabScroll.inertia = false;
            tabScroll.scrollSensitivity = 30f;
            tabScroll.horizontalScrollbar = null;
            tabScroll.verticalScrollbar = null;

            var tabView = new GameObject("Viewport", typeof(RectTransform));
            tabView.transform.SetParent(tabStrip.transform, false);
            var tabViewRt = (RectTransform)tabView.transform;
            StretchInside(tabViewRt, 10f, 10f, 5f, 5f);
            tabView.AddComponent<RectMask2D>();

            var tabRoot = new GameObject("Tabs", typeof(RectTransform));
            tabRoot.transform.SetParent(tabView.transform, false);
            var tabRootRt = (RectTransform)tabRoot.transform;

            // 왼쪽 끝에 매달아 오른쪽으로 자란다. 늘어난 만큼만 밀린다.
            tabRootRt.anchorMin = new Vector2(0f, 0f);
            tabRootRt.anchorMax = new Vector2(0f, 1f);
            tabRootRt.pivot = new Vector2(0f, 0.5f);
            tabRootRt.anchoredPosition = Vector2.zero;
            tabRootRt.sizeDelta = new Vector2(0f, 0f);

            tabScroll.viewport = tabViewRt;
            tabScroll.content = tabRootRt;

            var tabLayout = tabRoot.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 6f;
            tabLayout.childAlignment = TextAnchor.MiddleLeft;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = false;
            tabLayout.childForceExpandHeight = true;

            // 탭이 늘어난 만큼 이 칸도 넓어진다. 그래야 끌어서 넘길 자리가 생긴다.
            var tabFitter = tabRoot.AddComponent<ContentSizeFitter>();
            tabFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            tabFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var tabTemplate = CreatePanel(tabRoot.transform, "TabTemplate", new Color(0.78f, 0.77f, 0.73f, 1f));
            ((RectTransform)tabTemplate.transform).sizeDelta = new Vector2(150f, 36f);

            // 칸 너비는 여기서 정한다. 줄이 스스로 넓어지려면 한 칸의 너비를 알아야 한다.
            var tabSize = tabTemplate.AddComponent<LayoutElement>();
            tabSize.preferredWidth = 150f;
            tabSize.minWidth = 150f;
            tabSize.flexibleWidth = 0f;

            var tabButton = tabTemplate.AddComponent<Button>();
            tabButton.targetGraphic = tabTemplate.GetComponent<Image>();

            var tabLabel = AddText(tabTemplate.transform, "Label", 22f, UIFontWeight.Medium, ink,
                Vector2.zero, new Vector2(150f, 36f), TextAlignmentOptions.Center);
            StretchInside(tabLabel.rectTransform, 8f, 8f, 4f, 4f);
            tabLabel.raycastTarget = false;
            tabLabel.textWrappingMode = TextWrappingModes.NoWrap;
            tabTemplate.SetActive(false);

            // --- 적는 자리 ---
            // 누르면 여기에 글자가 들어간다. 이 판 자체가 InputField 의 바탕이다.
            var body = CreatePanel(window.transform, "Body", paper);
            StretchInside((RectTransform)body.transform, 0f, 0f, BarHeight + TabHeight, FooterHeight);

            var input = body.AddComponent<TMP_InputField>();
            input.targetGraphic = body.GetComponent<Image>();

            // 글자가 창 밖으로 새지 않게 잘라 주는 칸. InputField 가 이 칸을 기준으로 굴린다.
            var area = new GameObject("TextArea", typeof(RectTransform));
            area.transform.SetParent(body.transform, false);
            var areaRt = (RectTransform)area.transform;
            StretchInside(areaRt, 22f, 22f, 16f, 16f);
            area.AddComponent<RectMask2D>();

            var placeholder = AddText(area.transform, "Placeholder", 30f, UIFontWeight.Regular, faded,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            StretchInside(placeholder.rectTransform, 0f, 0f, 0f, 0f);
            placeholder.raycastTarget = false;

            var typed = AddText(area.transform, "Text", 30f, UIFontWeight.Regular, ink,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            StretchInside(typed.rectTransform, 0f, 0f, 0f, 0f);
            typed.raycastTarget = false;

            input.textViewport = areaRt;
            input.textComponent = typed;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            input.richText = false;

            // ESC 는 화면을 닫는 데 쓴다. 닫으면서 적은 것을 되돌리면 안 된다.
            input.restoreOriginalTextOnEscape = false;
            input.onFocusSelectAll = false;

            input.customCaretColor = true;
            input.caretColor = ink;
            input.caretWidth = 2;
            input.selectionColor = new Color(0.36f, 0.52f, 0.78f, 0.45f);
            input.text = string.Empty;

            // --- 아래 한 줄 ---
            var footer = AddText(window.transform, "Footer", 22f, UIFontWeight.Regular, faded,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            var footerRt = footer.rectTransform;
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = Vector2.zero;
            footerRt.sizeDelta = new Vector2(-44f, FooterHeight);
            footer.textWrappingMode = TextWrappingModes.NoWrap;
            footer.raycastTarget = false;

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_window").objectReferenceValue = windowRt;
            so.FindProperty("_titleText").objectReferenceValue = windowTitle;
            so.FindProperty("_input").objectReferenceValue = input;
            so.FindProperty("_footerText").objectReferenceValue = footer;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.FindProperty("_tabRoot").objectReferenceValue = tabRootRt;
            so.FindProperty("_tabScroll").objectReferenceValue = tabScroll;
            so.FindProperty("_tabTemplate").objectReferenceValue = tabButton;
            so.FindProperty("_newButton").objectReferenceValue = newButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            ClearDisabledTint(go);
            return screen;
        }

        /// <summary>
        /// 잠깐 떴다 사라지는 알림 한 줄.
        /// 누를 것이 없으므로 아래 화면을 가리지도 막지도 않는다.
        /// </summary>
        private static ToastScreen BuildToastScreen(string name)
        {
            var go = CreatePanel(null, name, new Color(0f, 0f, 0f, 0f));
            StretchFull(go);
            go.GetComponent<Image>().raycastTarget = false;

            var screen = go.AddComponent<ToastScreen>();

            // 맨 위 레이어. 무엇이 떠 있든 그 위에 얹힌다. 뒤로가기로 닫히지도 않는다.
            ConfigureScreen(screen, name, UILayer.System, false, false, true);

            var box = CreatePanel(go.transform, "Box", new Color(0.09f, 0.10f, 0.14f, 0.94f));
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 1f);
            boxRt.anchorMax = new Vector2(0.5f, 1f);
            boxRt.pivot = new Vector2(0.5f, 1f);
            boxRt.anchoredPosition = new Vector2(0f, -120f);
            boxRt.sizeDelta = new Vector2(760f, 92f);
            box.GetComponent<Image>().raycastTarget = false;

            var edge = CreatePanel(box.transform, "Edge", AccentColor);
            var edgeRt = (RectTransform)edge.transform;
            edgeRt.anchorMin = new Vector2(0f, 0f);
            edgeRt.anchorMax = new Vector2(0f, 1f);
            edgeRt.pivot = new Vector2(0f, 0.5f);
            edgeRt.anchoredPosition = Vector2.zero;
            edgeRt.sizeDelta = new Vector2(6f, 0f);
            edge.GetComponent<Image>().raycastTarget = false;

            var line = AddText(box.transform, "Line", 30f, UIFontWeight.SemiBold, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(line.rectTransform, 36f, 24f, 10f, 10f);
            ConfigureOneLineText(line, 30f);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_text").objectReferenceValue = line;
            so.ApplyModifiedPropertiesWithoutUndo();

            return screen;
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
            // 휴대폰으로 볼 때만 창 뒤에 깔리는 껍데기. 창보다 조금 크게 둘러 테두리처럼 보인다.
            var phoneShell = CreatePanel(go.transform, "PhoneShell", new Color(0.05f, 0.05f, 0.07f, 1f));
            var shellRt = (RectTransform)phoneShell.transform;
            shellRt.anchorMin = new Vector2(0.5f, 0f);
            shellRt.anchorMax = new Vector2(0.5f, 1f);
            shellRt.pivot = new Vector2(0.5f, 0.5f);
            shellRt.anchoredPosition = Vector2.zero;
            shellRt.sizeDelta = new Vector2(668f, 0f);

            var shellHome = CreatePanel(phoneShell.transform, "HomeBar", new Color(0.42f, 0.43f, 0.50f, 1f));
            var shellHomeRt = (RectTransform)shellHome.transform;
            shellHomeRt.anchorMin = new Vector2(0.5f, 0f);
            shellHomeRt.anchorMax = new Vector2(0.5f, 0f);
            shellHomeRt.pivot = new Vector2(0.5f, 0f);
            shellHomeRt.anchoredPosition = new Vector2(0f, 10f);
            shellHomeRt.sizeDelta = new Vector2(180f, 6f);
            shellHome.GetComponent<Image>().raycastTarget = false;

            AddPhoneSideKey(phoneShell.transform, "Key_Power", 1f, -260f, 120f);
            AddPhoneSideKey(phoneShell.transform, "Key_VolumeUp", 0f, -240f, 78f);
            AddPhoneSideKey(phoneShell.transform, "Key_VolumeDown", 0f, -334f, 78f);
            phoneShell.SetActive(false);

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

            // 휴대폰으로 볼 때만 켜지는 시각. 실제 휴대폰처럼 맨 윗줄 오른쪽 끝에 선다.
            var statusClock = AddText(titleBar.transform, "StatusClock", 19f, UIFontWeight.Medium, TextColor,
                Vector2.zero, new Vector2(150f, 36f), TextAlignmentOptions.Right);
            var statusClockRt = statusClock.rectTransform;
            statusClockRt.anchorMin = new Vector2(1f, 0.5f);
            statusClockRt.anchorMax = new Vector2(1f, 0.5f);
            statusClockRt.pivot = new Vector2(1f, 0.5f);
            // 시각과 믿음도는 오른쪽에 위아래로 겹쳐 세운다. 시각이 위, 믿음도가 아래다.
            statusClockRt.anchoredPosition = new Vector2(-11.4f, 10f);
            statusClockRt.sizeDelta = new Vector2(150f, 36f);
            statusClock.raycastTarget = false;
            statusClock.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            statusClock.gameObject.AddComponent<ClockLabel>();
            statusClock.gameObject.SetActive(false);

            // 믿음도는 시각 바로 아래. 둘이 같은 오른쪽 선에 맞춰 서야 한 묶음으로 읽힌다.
            // 그래서 시각과 같은 방식으로 오른쪽 끝에 매단다. 들여쓴 만큼도 같다.
            // 시각보다 한 급 작게 둔다. 곁가지 숫자라 시각보다 앞서 보이면 안 된다.
            var statusBelief = AddText(titleBar.transform, "StatusBelief", 17f, UIFontWeight.SemiBold, PhoneBeliefColor,
                Vector2.zero, new Vector2(150f, 36f), TextAlignmentOptions.Right);
            var statusBeliefRt = statusBelief.rectTransform;
            statusBeliefRt.anchorMin = new Vector2(1f, 0.5f);
            statusBeliefRt.anchorMax = new Vector2(1f, 0.5f);
            statusBeliefRt.pivot = new Vector2(1f, 0.5f);
            statusBeliefRt.anchoredPosition = new Vector2(-11.4f, -10f);
            statusBeliefRt.sizeDelta = new Vector2(150f, 36f);
            statusBelief.raycastTarget = false;
            statusBelief.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            // 이 칸은 BeliefLabel 을 달지 않는다. 목록이냐 글이냐에 따라 보여줄 숫자가 달라서
            // 괴담넷 화면이 직접 써 넣는다.
            statusBelief.gameObject.SetActive(false);

            // 휴대폰 카메라 구멍. 괴담넷이 휴대폰 화면을 덮으므로 여기에도 같은 자리에 하나 둔다.
            // 앱을 열어도 카메라는 그대로 있어야 한 대의 휴대폰으로 보인다.
            //
            // 이 창은 휴대폰 화면에 맞추며 통째로 줄어든다. 줄어든 뒤에 같은 크기로 보이도록
            // 그만큼 미리 키워 둔다. 휴대폰 쪽 18 / 창 쪽 25 가 화면에서 같은 크기다.
            var statusCamera = BuildPhoneCamera(titleBar.transform, 25f, -18f);
            statusCamera.SetActive(false);

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
                out var boardSiteText, out var boardBoardText, out var boardHeaderRow);
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

            // 줄 높이는 글마다 다르다. 제목이 길어 두 줄이 되면 그만큼 키가 자란다.
            // 안쪽 여백은 이 배치가 들고 있다. 좁은 화면에서는 그 여백만 갈아 끼우면 된다.
            var btLayout = boardTemplate.AddComponent<VerticalLayoutGroup>();
            btLayout.padding = new RectOffset(12, 12, 12, 18);
            btLayout.spacing = 0f;
            btLayout.childAlignment = TextAnchor.UpperLeft;
            btLayout.childControlWidth = true;
            btLayout.childControlHeight = true;
            btLayout.childForceExpandWidth = true;
            btLayout.childForceExpandHeight = false;

            var btFitter = boardTemplate.AddComponent<ContentSizeFitter>();
            btFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var btLabel = AddText(boardTemplate.transform, "Label", 26f, UIFontWeight.Medium, ink,
                Vector2.zero, new Vector2(1640f, 88f), TextAlignmentOptions.TopLeft);

            // 들어가야 하는 글 왼쪽에 붙는 파란 세모. 열 수 있는 글에만 켜진다.
            var btMark = AddText(boardTemplate.transform, "Mark", 26f, UIFontWeight.Bold,
                new Color(0.18f, 0.38f, 0.78f),
                Vector2.zero, new Vector2(40f, 40f), TextAlignmentOptions.Center);
            btMark.text = "▶";
            btMark.raycastTarget = false;
            // 세모와 믿음도와 아래 선은 줄 높이에 끼지 않는다. 제 자리에 그대로 붙어 있어야 한다.
            btMark.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var btMarkRt = btMark.rectTransform;
            btMarkRt.anchorMin = new Vector2(0f, 0.5f);
            btMarkRt.anchorMax = new Vector2(0f, 0.5f);
            btMarkRt.pivot = new Vector2(1f, 0.5f);
            btMarkRt.anchoredPosition = new Vector2(-8f, 6f);
            btMarkRt.sizeDelta = new Vector2(40f, 40f);

            // 바탕화면의 세모와 같이 조금씩 좌우로 움직인다. 가리키는 것은 둘 다 같은 일이다.
            btMark.gameObject.AddComponent<HintNudge>();

            // 오른쪽에는 이 글이 괴담의 믿음에 얼마나 보태고 있는지를 붉게 적는다.
            var btBelief = AddText(boardTemplate.transform, "Belief", 24f, UIFontWeight.SemiBold, BeliefMarkColor,
                Vector2.zero, new Vector2(320f, 40f), TextAlignmentOptions.Right);
            btBelief.raycastTarget = false;
            btBelief.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
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
            btRule.AddComponent<LayoutElement>().ignoreLayout = true;
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
            var pageHeader = BuildCommunityHeader(page.transform, ContentInset, out var siteText, out var boardText, out var pageHeaderRow);
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
            var titleBandStack = AddStack(titleBand, TitleBandSpacing, new RectOffset(Inset, Inset, (int)TitleBandPadTop, 52));

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
            var bodyAreaStack = AddStack(bodyArea, 0f, new RectOffset(Inset, Inset, 52, 68));

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
            var commentBlockStack = AddStack(commentBlock, 18f, new RectOffset(Inset, Inset, 44, 40));

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
            // 댓글 칸 높이도 글마다 다르다. 내용이 길면 그만큼 자란다.
            // 안쪽 여백은 이 배치가 들고 있다. 좁은 화면에서는 그 여백만 갈아 끼우면 된다.
            var commentTemplate = CreatePanel(commentRoot, "CommentTemplate", new Color(1f, 1f, 1f, 0f));
            var comRt = (RectTransform)commentTemplate.transform;
            comRt.sizeDelta = new Vector2(pageWidth - 64f, 82f);

            var comLayout = commentTemplate.AddComponent<VerticalLayoutGroup>();
            comLayout.padding = new RectOffset(12, 12, 12, 16);
            comLayout.spacing = 0f;
            comLayout.childAlignment = TextAnchor.UpperLeft;
            comLayout.childControlWidth = true;
            comLayout.childControlHeight = true;
            comLayout.childForceExpandWidth = true;
            comLayout.childForceExpandHeight = false;

            var comFitter = commentTemplate.AddComponent<ContentSizeFitter>();
            comFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var comLabel = AddText(commentTemplate.transform, "Label", 22f, UIFontWeight.Regular, ink,
                Vector2.zero, new Vector2(pageWidth - 88f, 68f), TextAlignmentOptions.TopLeft);

            var comRule = CreatePanel(commentTemplate.transform, "Rule", new Color(0.86f, 0.87f, 0.90f, 1f));
            comRule.AddComponent<LayoutElement>().ignoreLayout = true;
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

            // --- 컴퓨터 창 / 휴대폰 두 모양 ---
            // 괴담넷은 하나뿐이다. 창 크기와 좌우 여백과 글자 크기만 갈아 끼운다.
            // 내용을 고치면 컴퓨터로 보든 휴대폰으로 보든 함께 바뀐다.
            so.FindProperty("_window").objectReferenceValue = (RectTransform)window.transform;
            so.FindProperty("_phoneShell").objectReferenceValue = phoneShell;
            so.FindProperty("_statusClockText").objectReferenceValue = statusClock;
            so.FindProperty("_statusBeliefText").objectReferenceValue = statusBelief;
            so.FindProperty("_statusCamera").objectReferenceValue = statusCamera;
            so.FindProperty("_taskbarHeight").floatValue = DesktopTaskbarHeight;

            SetObjectList(so.FindProperty("_insetGroups"),
                boardListLayout, titleBandStack, bodyAreaStack, commentBlockStack);
            SetObjectList(so.FindProperty("_insetRows"), boardHeaderRow, pageHeaderRow);
            SetObjectList(so.FindProperty("_headerElements"), boardHeaderElement, pageHeaderElement);
            SetObjectList(so.FindProperty("_scaledTexts"),
                boardSiteText, boardBoardText, siteText, boardText,
                windowTitle, btLabel, btBelief, titleText, metaText, postBelief, bodyText,
                likeText, dislikeText, commentHeader, comLabel, choiceHeader, noticeText,
                ctLabel, ctNote);

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

            // 글자 크기는 역할마다 하나로 정해 둔다. 같은 급의 글이 저마다 다른 크기면
            // 무엇이 더 중요한 글인지 눈이 알 수 없다.
            const float SectionTitle = 32f;   // 칸 제목
            const float RuleHead = 30f;       // 고르는 대상인 규칙 문장
            const float SubText = 24f;        // 곁가지 - 근거와 단서 목록
            const float GuideText = 26f;      // 안내와 결과

            var cardColor = new Color(0.16f, 0.17f, 0.23f, 1f);

            var titleText = AddText(go.transform, "Title", 54f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 430f), new Vector2(1500f, 70f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", GuideText, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 368f), new Vector2(1500f, 50f), TextAlignmentOptions.Center);

            // --- 왼쪽: 모은 단서 ---
            // 무엇을 근거로 고르는지가 늘 보여야 하므로 후보 목록 옆에 붙여 둔다.
            var clueCard = CreatePanel(go.transform, "ClueCard", cardColor);
            var clueRt = (RectTransform)clueCard.transform;
            clueRt.anchorMin = new Vector2(0.5f, 0.5f);
            clueRt.anchorMax = new Vector2(0.5f, 0.5f);
            clueRt.pivot = new Vector2(0f, 1f);
            clueRt.anchoredPosition = new Vector2(-900f, 320f);
            clueRt.sizeDelta = new Vector2(600f, 560f);

            var clueTitle = AddText(clueCard.transform, "ClueTitle", SectionTitle, UIFontWeight.SemiBold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            var clueTitleRt = clueTitle.rectTransform;
            clueTitleRt.anchorMin = new Vector2(0f, 1f);
            clueTitleRt.anchorMax = new Vector2(1f, 1f);
            clueTitleRt.pivot = new Vector2(0.5f, 1f);
            clueTitleRt.anchoredPosition = new Vector2(0f, -22f);
            clueTitleRt.sizeDelta = new Vector2(-56f, 44f);
            clueTitle.textWrappingMode = TextWrappingModes.NoWrap;

            // 제목과 목록을 가르는 가는 선. 둘이 한 덩어리로 읽히지 않게 한다.
            var clueDivider = CreatePanel(clueCard.transform, "Divider", new Color(0.32f, 0.33f, 0.40f, 1f));
            var clueDividerRt = (RectTransform)clueDivider.transform;
            clueDividerRt.anchorMin = new Vector2(0f, 1f);
            clueDividerRt.anchorMax = new Vector2(1f, 1f);
            clueDividerRt.pivot = new Vector2(0.5f, 1f);
            clueDividerRt.anchoredPosition = new Vector2(0f, -76f);
            clueDividerRt.sizeDelta = new Vector2(-56f, 2f);
            clueDivider.GetComponent<Image>().raycastTarget = false;
            AddCrisp(clueDivider, 2f);

            // 단서는 한 줄씩 눌러 고르는 칸이다. 근거로 삼을 것을 직접 골라야 규칙이 세워진다.
            var clueList = new GameObject("Clues", typeof(RectTransform));
            clueList.transform.SetParent(clueCard.transform, false);
            var clueListRt = (RectTransform)clueList.transform;
            StretchInside(clueListRt, 22f, 22f, 92f, 20f);

            var clueLayout = clueList.AddComponent<VerticalLayoutGroup>();
            clueLayout.spacing = 8f;
            clueLayout.childAlignment = TextAnchor.UpperLeft;
            clueLayout.childControlWidth = true;
            clueLayout.childControlHeight = true;
            clueLayout.childForceExpandWidth = true;
            clueLayout.childForceExpandHeight = false;

            var clueTemplate = CreatePanel(clueList.transform, "ClueTemplate", new Color(0.13f, 0.14f, 0.19f, 1f));
            var clueButton = clueTemplate.AddComponent<Button>();
            clueButton.targetGraphic = clueTemplate.GetComponent<Image>();

            var clueRowFitter = clueTemplate.AddComponent<ContentSizeFitter>();
            clueRowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            clueRowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var clueRowLayout = clueTemplate.AddComponent<VerticalLayoutGroup>();
            clueRowLayout.padding = new RectOffset(14, 14, 10, 10);
            clueRowLayout.childControlWidth = true;
            clueRowLayout.childControlHeight = true;
            clueRowLayout.childForceExpandWidth = true;
            clueRowLayout.childForceExpandHeight = false;

            var clueRowLabel = AddText(clueTemplate.transform, "ClueLabel", SubText - 2f, UIFontWeight.Regular,
                TextColor, Vector2.zero, new Vector2(520f, 40f), TextAlignmentOptions.TopLeft);
            clueRowLabel.raycastTarget = false;
            clueTemplate.SetActive(false);

            var clueEmpty = AddText(clueCard.transform, "ClueEmpty", SubText, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            StretchInside(clueEmpty.rectTransform, 28f, 28f, 96f, 24f);
            clueEmpty.gameObject.SetActive(false);

            // --- 오른쪽: 규칙 후보 ---
            var listTitle = AddText(go.transform, "ListTitle", SectionTitle, UIFontWeight.SemiBold, AccentColor,
                Vector2.zero, new Vector2(1120f, 44f), TextAlignmentOptions.Left);
            var listTitleRt = listTitle.rectTransform;
            listTitleRt.pivot = new Vector2(0f, 1f);
            listTitleRt.anchoredPosition = new Vector2(-240f, 320f);
            listTitle.textWrappingMode = TextWrappingModes.NoWrap;

            var listGo = new GameObject("List", typeof(RectTransform));
            listGo.transform.SetParent(go.transform, false);
            var listRt = (RectTransform)listGo.transform;
            listRt.anchorMin = new Vector2(0.5f, 0.5f);
            listRt.anchorMax = new Vector2(0.5f, 0.5f);
            listRt.pivot = new Vector2(0f, 1f);
            listRt.anchoredPosition = new Vector2(-240f, 258f);
            listRt.sizeDelta = new Vector2(1120f, 498f);

            var listLayout = listGo.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 16f;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            // --- 후보 한 칸 ---
            // 규칙 문장 하나와, 이미 짚어 봤다는 딱지. 그게 전부다.
            //
            // 예전에는 "근거가 된 단서" 한 줄을 곁들였다. 그것을 없앴다.
            // 어느 단서가 어느 규칙을 받치는지는 플레이어가 골라서 맞혀야 하는 것이지,
            // 후보 칸이 미리 적어 둘 것이 아니다. 적어 두면 왼쪽에서 고르는 일이 베끼기가 된다.
            //
            // 높이는 네 칸이 들어가게 잡는다. 파훼법을 묻는 대목에서 답이 넷이라
            // 칸이 크면 마지막 답이 아래 띠를 뚫고 내려간다.
            var template = CreatePanel(listGo.transform, "ItemTemplate", cardColor);
            var templateButton = template.AddComponent<Button>();
            templateButton.targetGraphic = template.GetComponent<Image>();

            const float ItemHeight = 108f;
            ((RectTransform)template.transform).sizeDelta = new Vector2(1120f, ItemHeight);

            var itemSize = template.AddComponent<LayoutElement>();
            itemSize.preferredHeight = ItemHeight;
            itemSize.minHeight = ItemHeight;

            var head = AddText(template.transform, "ItemLabel", RuleHead, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            var headRt = head.rectTransform;
            headRt.anchorMin = Vector2.zero;
            headRt.anchorMax = Vector2.one;
            headRt.pivot = new Vector2(0.5f, 0.5f);
            // 오른쪽은 딱지 자리를 비워 둔다. 딱지가 붙어도 글자가 그 아래로 들어가지 않는다.
            headRt.offsetMin = new Vector2(24f, 14f);
            headRt.offsetMax = new Vector2(-172f, -14f);

            var badge = CreatePanel(template.transform, "Badge", new Color(0.24f, 0.34f, 0.28f, 1f));
            var badgeRt = (RectTransform)badge.transform;
            badgeRt.anchorMin = new Vector2(1f, 1f);
            badgeRt.anchorMax = new Vector2(1f, 1f);
            badgeRt.pivot = new Vector2(1f, 1f);
            badgeRt.anchoredPosition = new Vector2(-20f, -18f);
            badgeRt.sizeDelta = new Vector2(120f, 40f);
            badge.GetComponent<Image>().raycastTarget = false;

            var badgeLabel = AddText(badge.transform, "Label", 22f, UIFontWeight.SemiBold, TextColor,
                Vector2.zero, new Vector2(120f, 40f), TextAlignmentOptions.Center);
            badgeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            badge.SetActive(false);

            template.SetActive(false);

            // 세울 수 있는 후보가 하나도 없을 때 그 자리에 적는 한 줄.
            var emptyText = AddText(go.transform, "Empty", GuideText, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, new Vector2(1060f, 60f), TextAlignmentOptions.Left);
            emptyText.rectTransform.pivot = new Vector2(0f, 1f);
            emptyText.rectTransform.anchoredPosition = new Vector2(-220f, 240f);
            emptyText.gameObject.SetActive(false);

            // --- 아래: 직전 결과 ---
            var resultRoot = CreatePanel(go.transform, "ResultBar", new Color(0.13f, 0.11f, 0.14f, 1f));
            var resultBarRt = (RectTransform)resultRoot.transform;
            resultBarRt.anchorMin = new Vector2(0.5f, 0.5f);
            resultBarRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultBarRt.pivot = new Vector2(0.5f, 0.5f);
            resultBarRt.anchoredPosition = new Vector2(0f, -305f);
            resultBarRt.sizeDelta = new Vector2(1760f, 62f);

            var resultText = AddText(resultRoot.transform, "Result", GuideText, UIFontWeight.Medium, WarnColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(resultText.rectTransform, 24f, 24f, 6f, 6f);
            resultText.textWrappingMode = TextWrappingModes.NoWrap;
            resultRoot.SetActive(false);

            // --- 결론 ---
            // 규칙을 맞히고 나서야 뜬다. 답해야 하는 것은 둘뿐이라 두 줄로 못 박아 둔다.
            // 후보 목록이 서던 자리를 그대로 쓴다. 결론이 나오면 목록은 물러나므로 자리가 겹치지 않는다.
            var conclusion = CreatePanel(go.transform, "ConclusionCard", new Color(0.12f, 0.16f, 0.14f, 1f));
            var conclusionRt = (RectTransform)conclusion.transform;
            conclusionRt.anchorMin = new Vector2(0.5f, 0.5f);
            conclusionRt.anchorMax = new Vector2(0.5f, 0.5f);
            conclusionRt.pivot = new Vector2(0f, 1f);
            conclusionRt.anchoredPosition = new Vector2(-240f, 320f);
            conclusionRt.sizeDelta = new Vector2(1120f, 560f);

            var conclusionEdge = CreatePanel(conclusion.transform, "Edge", new Color(0.46f, 0.72f, 0.52f, 1f));
            var conclusionEdgeRt = (RectTransform)conclusionEdge.transform;
            conclusionEdgeRt.anchorMin = new Vector2(0f, 0f);
            conclusionEdgeRt.anchorMax = new Vector2(0f, 1f);
            conclusionEdgeRt.pivot = new Vector2(0f, 0.5f);
            conclusionEdgeRt.anchoredPosition = Vector2.zero;
            conclusionEdgeRt.sizeDelta = new Vector2(6f, 0f);
            conclusionEdge.GetComponent<Image>().raycastTarget = false;

            var conclusionTitle = AddText(conclusion.transform, "ConclusionTitle", SectionTitle,
                UIFontWeight.SemiBold, new Color(0.62f, 0.86f, 0.68f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            var conclusionTitleRt = conclusionTitle.rectTransform;
            conclusionTitleRt.anchorMin = new Vector2(0f, 1f);
            conclusionTitleRt.anchorMax = new Vector2(1f, 1f);
            conclusionTitleRt.pivot = new Vector2(0.5f, 1f);
            conclusionTitleRt.anchoredPosition = new Vector2(0f, -22f);
            conclusionTitleRt.sizeDelta = new Vector2(-72f, 42f);
            conclusionTitle.textWrappingMode = TextWrappingModes.NoWrap;

            var conclusionDivider = CreatePanel(conclusion.transform, "Divider", new Color(0.32f, 0.44f, 0.36f, 1f));
            var conclusionDividerRt = (RectTransform)conclusionDivider.transform;
            conclusionDividerRt.anchorMin = new Vector2(0f, 1f);
            conclusionDividerRt.anchorMax = new Vector2(1f, 1f);
            conclusionDividerRt.pivot = new Vector2(0.5f, 1f);
            conclusionDividerRt.anchoredPosition = new Vector2(0f, -74f);
            conclusionDividerRt.sizeDelta = new Vector2(-72f, 2f);
            conclusionDivider.GetComponent<Image>().raycastTarget = false;
            AddCrisp(conclusionDivider, 2f);

            // 물음 둘을 위아래로 나란히 둔다. 각각 "물음 / 답 / 그렇게 본 까닭" 이 한 덩어리다.
            var verdictText = AddText(conclusion.transform, "Verdict", GuideText, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            var verdictRt = verdictText.rectTransform;
            verdictRt.anchorMin = new Vector2(0f, 1f);
            verdictRt.anchorMax = new Vector2(1f, 1f);
            verdictRt.pivot = new Vector2(0.5f, 1f);
            verdictRt.anchoredPosition = new Vector2(0f, -94f);
            verdictRt.sizeDelta = new Vector2(-72f, 250f);

            var counterText = AddText(conclusion.transform, "Counter", GuideText, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            var counterRt = counterText.rectTransform;
            counterRt.anchorMin = new Vector2(0f, 1f);
            counterRt.anchorMax = new Vector2(1f, 1f);
            counterRt.pivot = new Vector2(0.5f, 1f);
            counterRt.anchoredPosition = new Vector2(0f, -354f);
            counterRt.sizeDelta = new Vector2(-72f, 196f);

            conclusion.SetActive(false);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_footerText").objectReferenceValue = footerText;
            so.FindProperty("_clueCardRoot").objectReferenceValue = clueCard;
            so.FindProperty("_clueTitleText").objectReferenceValue = clueTitle;
            so.FindProperty("_clueListRoot").objectReferenceValue = clueListRt;
            so.FindProperty("_clueTemplate").objectReferenceValue = clueButton;
            so.FindProperty("_clueEmptyText").objectReferenceValue = clueEmpty;
            so.FindProperty("_listTitleText").objectReferenceValue = listTitle;
            so.FindProperty("_listRoot").objectReferenceValue = listRt;
            so.FindProperty("_itemTemplate").objectReferenceValue = templateButton;
            so.FindProperty("_emptyText").objectReferenceValue = emptyText;
            so.FindProperty("_resultRoot").objectReferenceValue = resultRoot;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.FindProperty("_conclusionRoot").objectReferenceValue = conclusion;
            so.FindProperty("_conclusionTitleText").objectReferenceValue = conclusionTitle;
            so.FindProperty("_verdictText").objectReferenceValue = verdictText;
            so.FindProperty("_counterText").objectReferenceValue = counterText;
            so.ApplyModifiedPropertiesWithoutUndo();

            ClearDisabledTint(go);

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -425f), new Vector2(900f, 110f));
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

            var titleText = AddText(go.transform, "Title", 50f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 438f), new Vector2(1500f, 90f), TextAlignmentOptions.Center);

            // 본문과 댓글은 글마다 길이가 크게 다르다. 한 화면에 욱여넣지 않고 굴러가게 둔다.
            // 글을 다 읽어야 앞뒤가 맞는지 따져 볼 수 있으므로, 잘려 보이는 쪽이 더 나쁘다.
            var viewport = new GameObject("ReadViewport", typeof(RectTransform));
            viewport.transform.SetParent(go.transform, false);
            var viewRt = (RectTransform)viewport.transform;
            viewRt.anchorMin = new Vector2(0.5f, 0.5f);
            viewRt.anchorMax = new Vector2(0.5f, 0.5f);
            viewRt.pivot = new Vector2(0.5f, 0.5f);
            viewRt.anchoredPosition = new Vector2(0f, 92f);
            viewRt.sizeDelta = new Vector2(1420f, 590f);
            viewport.AddComponent<RectMask2D>();

            // 글자가 없는 빈 곳을 잡아도 끌리도록 눌림만 받는 판을 깐다.
            var grab = viewport.AddComponent<Image>();
            grab.color = new Color(1f, 1f, 1f, 0f);
            grab.raycastTarget = true;

            var content = new GameObject("ReadContent", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            AddStack(content, 28f, new RectOffset(40, 40, 8, 24));

            var bodyText = AddText(content.transform, "Body", 32f, UIFontWeight.Regular, TextColor,
                Vector2.zero, new Vector2(1340f, 300f), TextAlignmentOptions.TopLeft);
            ConfigureScrolledText(bodyText, 32f, 16f);

            AddStackRule(content.transform, new Color(1f, 1f, 1f, 0.12f), 2f);

            var commentsText = AddText(content.transform, "Comments", 28f, UIFontWeight.Regular, TextColor,
                Vector2.zero, new Vector2(1340f, 200f), TextAlignmentOptions.TopLeft);
            ConfigureScrolledText(commentsText, 28f, 12f);

            var scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            // 세 줄이 아래로 나란히 선다.
            var statusText = AddText(go.transform, "Status", 28f, UIFontWeight.SemiBold, DimTextColor,
                new Vector2(0f, -252f), new Vector2(1420f, 44f), TextAlignmentOptions.Center);
            var statsText = AddText(go.transform, "Stats", 28f, UIFontWeight.SemiBold, AccentColor,
                new Vector2(0f, -300f), new Vector2(1420f, 44f), TextAlignmentOptions.Center);
            var feedbackText = AddText(go.transform, "Feedback", 26f, UIFontWeight.Medium, WarnColor,
                new Vector2(0f, -348f), new Vector2(1420f, 44f), TextAlignmentOptions.Center);

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_bodyText").objectReferenceValue = bodyText;
            so.FindProperty("_commentsText").objectReferenceValue = commentsText;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_statsText").objectReferenceValue = statsText;
            so.FindProperty("_feedbackText").objectReferenceValue = feedbackText;
            so.ApplyModifiedPropertiesWithoutUndo();

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -432f), new Vector2(1000f, 110f));
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
            // 열차에 탄 뒤에는 앞에 한 마디가 더 붙으므로 넉넉히 넓게 둔다.
            plateRt.sizeDelta = new Vector2(1740f, 52f);
            tickerPlate.GetComponent<Image>().raycastTarget = false;

            var tickerText = AddText(tickerPlate.transform, "Ticker", 26f, UIFontWeight.Medium, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(tickerText.rectTransform, 24f, 24f, 6f, 6f);

            // 자리 이름과 알림과 상황이 나란히 선다. 한 줄로 두고 길어지면 글자만 줄인다.
            ConfigureOneLineText(tickerText, 26f);

            // 고를 것이 떠 있는 동안 장면을 못 누르게 덮는 판.
            // 대사 띠보다 먼저 만들어야 띠와 선택지가 이 판 위에 올라온다.
            var fieldBlock = CreatePanel(safe.transform, "Blocker", new Color(0f, 0f, 0f, 0f));
            StretchFull(fieldBlock);
            fieldBlock.GetComponent<Image>().raycastTarget = true;
            fieldBlock.SetActive(false);

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

            // 초상과 이름과 대사를 한 묶음으로 둔다. 말하는 사람이 없으면 통째로 꺼진다.
            var speech = new GameObject("Speech", typeof(RectTransform));
            speech.transform.SetParent(band.transform, false);
            StretchFull(speech);

            // 초상 자리. 지금은 빈 네모다. 실제 그림이 생기면 이 Image만 갈아 끼운다.
            const float PortraitSize = 240f;
            var portrait = CreatePanel(speech.transform, "Portrait", new Color(0.16f, 0.17f, 0.22f, 1f));
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

            var speakerText = AddText(speech.transform, "Speaker", 30f, UIFontWeight.Bold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            speakerText.rectTransform.anchorMin = new Vector2(0f, 1f);
            speakerText.rectTransform.anchorMax = new Vector2(0f, 1f);
            speakerText.rectTransform.pivot = new Vector2(0f, 1f);
            speakerText.rectTransform.anchoredPosition = new Vector2(TextLeft, -48f);
            speakerText.rectTransform.sizeDelta = new Vector2(700f, 42f);
            speakerText.raycastTarget = false;

            var lineText = AddText(speech.transform, "Line", 34f, UIFontWeight.Regular, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            lineText.rectTransform.anchorMin = new Vector2(0f, 1f);
            lineText.rectTransform.anchorMax = new Vector2(0f, 1f);
            lineText.rectTransform.pivot = new Vector2(0f, 1f);
            lineText.rectTransform.anchoredPosition = new Vector2(TextLeft, -100f);

            // 오른쪽 아래 버튼 줄까지는 내려오지 않는 만큼, 폭은 화면 오른쪽 끝까지 쓴다.
            // 좁게 두면 긴 대사가 글자 몇 개만 다음 줄로 흘린다.
            lineText.rectTransform.sizeDelta = new Vector2(1460f, 140f);
            ConfigureBodyText(lineText, 34f);

            // --- 고를 것 ---
            // 대사가 서는 그 자리에 대신 늘어선다. 평소에는 꺼 둔다.
            var choices = new GameObject("Choices", typeof(RectTransform));
            choices.transform.SetParent(speech.transform, false);
            var fieldChoiceRoot = (RectTransform)choices.transform;
            fieldChoiceRoot.anchorMin = new Vector2(0f, 1f);
            fieldChoiceRoot.anchorMax = new Vector2(0f, 1f);
            fieldChoiceRoot.pivot = new Vector2(0f, 1f);
            fieldChoiceRoot.anchoredPosition = new Vector2(TextLeft, -96f);
            fieldChoiceRoot.sizeDelta = new Vector2(860f, 0f);   // 오른쪽 아래 버튼 줄과 겹치지 않는 폭

            var choiceLayout = choices.AddComponent<VerticalLayoutGroup>();
            choiceLayout.spacing = 8f;
            choiceLayout.childAlignment = TextAnchor.UpperLeft;
            choiceLayout.childControlWidth = true;
            choiceLayout.childControlHeight = false;
            choiceLayout.childForceExpandWidth = true;
            choiceLayout.childForceExpandHeight = false;

            var fieldChoiceTemplate = CreatePanel(choices.transform, "ChoiceTemplate",
                new Color(0.13f, 0.14f, 0.19f, 1f));
            var fieldChoice = fieldChoiceTemplate.AddComponent<Button>();
            fieldChoice.targetGraphic = fieldChoiceTemplate.GetComponent<Image>();

            var fieldChoiceColors = fieldChoice.colors;
            fieldChoiceColors.normalColor = Color.white;
            fieldChoiceColors.highlightedColor = new Color(1.35f, 1.35f, 1.45f, 1f);
            fieldChoiceColors.pressedColor = new Color(0.8f, 0.8f, 0.9f, 1f);
            fieldChoiceColors.selectedColor = Color.white;
            fieldChoice.colors = fieldChoiceColors;

            var fieldChoiceRt = (RectTransform)fieldChoiceTemplate.transform;
            fieldChoiceRt.sizeDelta = new Vector2(860f, 58f);

            var fieldChoiceLabel = AddText(fieldChoiceTemplate.transform, "Label", 28f, UIFontWeight.Regular,
                TextColor, Vector2.zero, new Vector2(820f, 44f), TextAlignmentOptions.Left);
            StretchInside(fieldChoiceLabel.rectTransform, 22f, 22f, 6f, 6f);

            // 고를 것은 한 칸에 한 줄로 선다. 긴 것은 접지 않고 글자를 줄여 담는다.
            ConfigureOneLineText(fieldChoiceLabel, 26f, 0.62f);

            fieldChoiceTemplate.SetActive(false);
            choices.SetActive(false);

            // 버튼은 띠 오른쪽 아래에 세운다. 장면도 대사도 가리지 않는다.
            buttonRow = CreateButtonRow(band.transform, new Vector2(600f, -110f), new Vector2(660f, 92f));

            // --- 늘 열어 볼 수 있는 휴대폰 ---
            // 대사 띠 바로 위 오른쪽 끝에 붙는다. 접었을 때도 펼쳤을 때도 같은 자리에서 자란다.
            // 주머니에서 꺼내 드는 것처럼 보이게, 접힌 모습도 작은 휴대폰 꼴로 둔다.
            const float PhoneRight = 48f;        // 오른쪽 끝에서 띄우는 만큼
            const float PhoneBottom = BandHeight + 24f;   // 대사 띠 바로 위

            var shellColor = new Color(0.05f, 0.05f, 0.07f, 1f);
            var glassColor = new Color(0.09f, 0.10f, 0.14f, 1f);
            var metalColor = new Color(0.22f, 0.23f, 0.29f, 1f);

            // 접힌 모습. 실제 휴대폰과 같은 세로 비율(9:19)로 줄인 껍데기다.
            var phoneButtonGo = CreatePanel(safe.transform, "Btn_Phone", shellColor);
            var phoneBtnRt = (RectTransform)phoneButtonGo.transform;
            phoneBtnRt.anchorMin = new Vector2(1f, 0f);
            phoneBtnRt.anchorMax = new Vector2(1f, 0f);
            phoneBtnRt.pivot = new Vector2(1f, 0f);
            phoneBtnRt.anchoredPosition = new Vector2(-PhoneRight, PhoneBottom);
            phoneBtnRt.sizeDelta = new Vector2(90f, 190f);

            var phoneButton = phoneButtonGo.AddComponent<Button>();
            phoneButton.targetGraphic = phoneButtonGo.GetComponent<Image>();

            // 껍데기 안의 화면. 여기에만 글자가 들어간다.
            var phoneBtnGlass = CreatePanel(phoneButtonGo.transform, "Glass", glassColor);
            StretchInside((RectTransform)phoneBtnGlass.transform, 7f, 7f, 12f, 16f);
            phoneBtnGlass.GetComponent<Image>().raycastTarget = false;

            var phoneBtnLabel = AddText(phoneBtnGlass.transform, "Label", 20f, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(phoneBtnLabel.rectTransform, 4f, 4f, 4f, 4f);
            phoneBtnLabel.textWrappingMode = TMPro.TextWrappingModes.Normal;
            phoneBtnLabel.raycastTarget = false;

            // 아래 손잡이 선. 접힌 것도 휴대폰으로 보이게 하는 것은 이 한 줄이다.
            var phoneBtnHome = CreatePanel(phoneButtonGo.transform, "HomeBar", new Color(0.42f, 0.43f, 0.50f, 1f));
            var phoneBtnHomeRt = (RectTransform)phoneBtnHome.transform;
            phoneBtnHomeRt.anchorMin = new Vector2(0.5f, 0f);
            phoneBtnHomeRt.anchorMax = new Vector2(0.5f, 0f);
            phoneBtnHomeRt.pivot = new Vector2(0.5f, 0f);
            phoneBtnHomeRt.anchoredPosition = new Vector2(0f, 6f);
            phoneBtnHomeRt.sizeDelta = new Vector2(36f, 4f);
            phoneBtnHome.GetComponent<Image>().raycastTarget = false;

            // --- 펼친 휴대폰 ---
            // 껍데기(테두리)가 보이고 그 안에 화면이 들어간다. 9:18.6, 실제 휴대폰 비율이다.
            const float PhoneWidth = 300f;
            const float PhoneHeight = 620f;
            const float BezelSide = 12f;
            const float BezelTop = 18f;
            const float BezelBottom = 26f;
            const float ScreenWidth = PhoneWidth - BezelSide * 2f;   // = 276

            var phone = CreatePanel(safe.transform, "Phone", shellColor);
            var phoneRt = (RectTransform)phone.transform;
            phoneRt.anchorMin = new Vector2(1f, 0f);
            phoneRt.anchorMax = new Vector2(1f, 0f);
            phoneRt.pivot = new Vector2(1f, 0f);
            phoneRt.anchoredPosition = new Vector2(-PhoneRight, PhoneBottom);
            phoneRt.sizeDelta = new Vector2(PhoneWidth, PhoneHeight);

            // 옆면 단추. 껍데기 밖으로 살짝 튀어나온다.
            AddPhoneSideKey(phone.transform, "Key_Power", 1f, -150f, 70f);
            AddPhoneSideKey(phone.transform, "Key_VolumeUp", 0f, -140f, 44f);
            AddPhoneSideKey(phone.transform, "Key_VolumeDown", 0f, -194f, 44f);

            // 화면. 이 안쪽만 휴대폰이 켜진 자리다.
            var phoneScreen = CreatePanel(phone.transform, "Screen", glassColor);
            StretchInside((RectTransform)phoneScreen.transform,
                BezelSide, BezelSide, BezelTop, BezelBottom);

            // 위쪽 가운데의 작은 동그란 카메라. 요즘 휴대폰은 노치 대신 구멍 하나다.
            BuildPhoneCamera(phoneScreen.transform, 18f, -13f);

            // 아래 손잡이 선. 껍데기 쪽에 둔다.
            var homeBar = CreatePanel(phone.transform, "HomeBar", new Color(0.42f, 0.43f, 0.50f, 1f));
            var homeBarRt = (RectTransform)homeBar.transform;
            homeBarRt.anchorMin = new Vector2(0.5f, 0f);
            homeBarRt.anchorMax = new Vector2(0.5f, 0f);
            homeBarRt.pivot = new Vector2(0.5f, 0f);
            homeBarRt.anchoredPosition = new Vector2(0f, 10f);
            homeBarRt.sizeDelta = new Vector2(110f, 5f);
            homeBar.GetComponent<Image>().raycastTarget = false;

            // 위 상태 줄. 시계와 닫기. 노치 아래에 깔린다.
            const float StatusHeight = 44f;
            var phoneBar = CreatePanel(phoneScreen.transform, "StatusBar", new Color(0.12f, 0.13f, 0.18f, 1f));
            var phoneBarRt = (RectTransform)phoneBar.transform;
            phoneBarRt.anchorMin = new Vector2(0f, 1f);
            phoneBarRt.anchorMax = new Vector2(1f, 1f);
            phoneBarRt.pivot = new Vector2(0.5f, 1f);
            phoneBarRt.anchoredPosition = Vector2.zero;
            phoneBarRt.sizeDelta = new Vector2(0f, StatusHeight);
            phoneBar.transform.SetAsFirstSibling();   // 노치가 위에 오게

            // 휴대폰 시계도 컴퓨터와 같은 시각이다. 흘러가는 것도 같다.
            // 시각과 믿음도는 나란히 왼쪽에 붙인다. 떨어뜨려 놓으면 서로 딴 것으로 보인다.
            // 가운데는 카메라 구멍 자리라 비워 둔다.
            var phoneClock = AddText(phoneBar.transform, "Clock", 14f, UIFontWeight.Medium, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            // 괴담넷 상태 줄과 같은 차림이다. 시각이 위, 믿음도가 아래로 오른쪽에 겹쳐 선다.
            // 왼쪽은 닫기 단추, 가운데는 카메라 구멍 자리다.
            var phoneClockRt = phoneClock.rectTransform;
            phoneClockRt.anchorMin = new Vector2(1f, 0.5f);
            phoneClockRt.anchorMax = new Vector2(1f, 0.5f);
            phoneClockRt.pivot = new Vector2(1f, 0.5f);
            phoneClockRt.anchoredPosition = new Vector2(-8f, 7f);
            phoneClockRt.sizeDelta = new Vector2(110f, 24f);
            phoneClock.alignment = TextAlignmentOptions.Right;
            phoneClock.raycastTarget = false;
            phoneClock.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            phoneClock.gameObject.AddComponent<ClockLabel>();

            // 시각보다 한 급 작게 둔다. 괴담넷 상태 줄과 같은 차림이다.
            var phoneBelief = AddText(phoneBar.transform, "Belief", 12f, UIFontWeight.SemiBold, PhoneBeliefColor,
                Vector2.zero, new Vector2(110f, 24f), TextAlignmentOptions.Right);
            var phoneBeliefRt = phoneBelief.rectTransform;
            phoneBeliefRt.anchorMin = new Vector2(1f, 0.5f);
            phoneBeliefRt.anchorMax = new Vector2(1f, 0.5f);
            phoneBeliefRt.pivot = new Vector2(1f, 0.5f);
            phoneBeliefRt.anchoredPosition = new Vector2(-8f, -7f);
            phoneBeliefRt.sizeDelta = new Vector2(110f, 24f);
            phoneBelief.raycastTarget = false;
            phoneBelief.textWrappingMode = TMPro.TextWrappingModes.NoWrap;

            // 글 하나가 아니라 게시판 전체의 값이다. 이름도 그대로 "전체 믿음도" 라고 적는다.
            phoneBelief.gameObject.AddComponent<BeliefLabel>();

            // 닫기는 왼쪽 끝. 괴담넷의 돌아가기와 같은 자리다.
            var phoneClose = CreatePanel(phoneBar.transform, "Btn_PhoneClose", new Color(0.30f, 0.16f, 0.18f, 1f));
            var phoneCloseRt = (RectTransform)phoneClose.transform;
            phoneCloseRt.anchorMin = new Vector2(0f, 0.5f);
            phoneCloseRt.anchorMax = new Vector2(0f, 0.5f);
            phoneCloseRt.pivot = new Vector2(0f, 0.5f);
            phoneCloseRt.anchoredPosition = new Vector2(8f, 0f);
            phoneCloseRt.sizeDelta = new Vector2(26f, 22f);
            var phoneCloseButton = phoneClose.AddComponent<Button>();
            phoneCloseButton.targetGraphic = phoneClose.GetComponent<Image>();
            var phoneCloseLabel = AddText(phoneClose.transform, "Label", 14f, UIFontWeight.Bold, TextColor,
                Vector2.zero, new Vector2(26f, 22f), TextAlignmentOptions.Center);
            phoneCloseLabel.text = "X";
            phoneCloseLabel.raycastTarget = false;

            // 앱은 컴퓨터 바탕화면과 같은 것들이다. 세로 화면이라 세 칸씩 두 줄로 늘어놓는다.
            var phoneApps = new[]
            {
                new[] { "gwedamnet", "ui.desktop.app_net" },
                new[] { "memo", "ui.desktop.app_memo" },
                new[] { "archive", "ui.desktop.app_archive" },
                new[] { "kikitalk", "ui.desktop.app_talk" },
                new[] { "gallery", "ui.desktop.app_gallery" },
            };

            var phoneIconTexts = new TMP_Text[phoneApps.Length + 2];
            var phoneIconButtons = new Button[phoneApps.Length + 2];

            const float IconSize = 60f;
            const float IconGapX = 26f;
            const float IconRowPitch = 112f;
            const float IconLeft = (ScreenWidth - (IconSize * 3f + IconGapX * 2f)) * 0.5f;   // = 22

            for (int i = 0; i < phoneApps.Length; i++)
            {
                int col = i % 3;
                int row = i / 3;
                float x = IconLeft + col * (IconSize + IconGapX);
                float y = -(StatusHeight + 20f) - row * IconRowPitch;

                phoneIconButtons[i] = BuildPhoneIcon(phoneScreen.transform, phoneApps[i][0],
                    new Vector2(x, y), IconSize, out phoneIconTexts[i]);
            }

            // 실제 휴대폰처럼 전화와 메시지는 맨 아래 줄에 따로 둔다.
            var dock = CreatePanel(phoneScreen.transform, "Dock", new Color(0.14f, 0.15f, 0.20f, 1f));
            var dockRt = (RectTransform)dock.transform;
            dockRt.anchorMin = new Vector2(0f, 0f);
            dockRt.anchorMax = new Vector2(1f, 0f);
            dockRt.pivot = new Vector2(0.5f, 0f);
            dockRt.anchoredPosition = new Vector2(0f, 10f);
            dockRt.sizeDelta = new Vector2(-20f, 104f);

            const float DockLeft = (ScreenWidth - 20f - (IconSize * 2f + 44f)) * 0.5f;   // = 36
            phoneIconButtons[phoneApps.Length] = BuildPhoneIcon(dock.transform, "call",
                new Vector2(DockLeft, -10f), IconSize, out phoneIconTexts[phoneApps.Length]);
            phoneIconButtons[phoneApps.Length + 1] = BuildPhoneIcon(dock.transform, "message",
                new Vector2(DockLeft + IconSize + 44f, -10f), IconSize, out phoneIconTexts[phoneApps.Length + 1]);

            phone.SetActive(false);

            // 튜토리얼이 현장에서 말할 때만 켜지는 진행 버튼. 화면을 덮어 조사 지점과 띠의 버튼을 막는다.
            //
            // 다만 휴대폰만은 덮지 않는다. 대사를 듣는 동안에도 휴대폰을 열어 글을 다시 볼 수 있어야 한다.
            // 그래서 대사 띠 바로 위에 끼워 넣는다. 나중에 만든 휴대폰은 이 판 위에 남는다.
            var fieldAdvance = CreatePanel(safe.transform, "Btn_Advance", new Color(0f, 0f, 0f, 0f));
            StretchFull(fieldAdvance);
            fieldAdvance.transform.SetSiblingIndex(band.transform.GetSiblingIndex() + 1);
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
            so.FindProperty("_choiceRoot").objectReferenceValue = fieldChoiceRoot;
            so.FindProperty("_choiceTemplate").objectReferenceValue = fieldChoice;
            so.FindProperty("_blockRoot").objectReferenceValue = fieldBlock;
            so.FindProperty("_tickerText").objectReferenceValue = tickerText;
            so.FindProperty("_portrait").objectReferenceValue = portrait.GetComponent<Image>();
            so.FindProperty("_speakerText").objectReferenceValue = speakerText;
            so.FindProperty("_lineText").objectReferenceValue = lineText;
            so.FindProperty("_speechRoot").objectReferenceValue = speech;
            so.FindProperty("_phoneButton").objectReferenceValue = phoneButton;
            so.FindProperty("_phonePanel").objectReferenceValue = phone;
            so.FindProperty("_phoneScreen").objectReferenceValue = (RectTransform)phoneScreen.transform;
            so.FindProperty("_phoneCloseButton").objectReferenceValue = phoneCloseButton;
            so.FindProperty("_phoneClockText").objectReferenceValue = phoneClock;
            so.FindProperty("_phoneButtonLabel").objectReferenceValue = phoneBtnLabel;

            // 앱 이름은 컴퓨터 바탕화면과 같은 문구를 쓴다. 전화와 메시지만 따로 둔다.
            // 앱 ID 는 바탕화면과 같은 것을 쓴다. 그래야 여는 쪽이 어느 화면인지 가리지 않는다.
            var appIds = new string[phoneApps.Length + 2];
            var appLabelIds = new string[phoneApps.Length + 2];
            for (int i = 0; i < phoneApps.Length; i++)
            {
                appIds[i] = phoneApps[i][0];
                appLabelIds[i] = phoneApps[i][1];
            }
            appIds[phoneApps.Length] = "call";
            appIds[phoneApps.Length + 1] = "message";
            appLabelIds[phoneApps.Length] = "ui.phone.app_call";
            appLabelIds[phoneApps.Length + 1] = "ui.phone.app_message";

            var appList = so.FindProperty("_phoneApps");
            appList.arraySize = appLabelIds.Length;
            for (int i = 0; i < appLabelIds.Length; i++)
            {
                var element = appList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("appId").stringValue = appIds[i];
                element.FindPropertyRelative("labelTextId").stringValue = appLabelIds[i];
                element.FindPropertyRelative("button").objectReferenceValue = phoneIconButtons[i];
                element.FindPropertyRelative("label").objectReferenceValue = phoneIconTexts[i];
            }

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

            // 제목 185~115, 본문 95~-85. 사이를 20 띄운다.
            var titleText = AddText(box.transform, "Title", 46f, UIFontWeight.SemiBold, TextColor,
                new Vector2(0f, 150f), new Vector2(900f, 70f), TextAlignmentOptions.Center);
            var bodyText = AddText(box.transform, "Body", 32f, UIFontWeight.Regular, TextColor,
                new Vector2(0f, 5f), new Vector2(880f, 180f), TextAlignmentOptions.Top);

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
            out TextMeshProUGUI site, out TextMeshProUGUI board, out RectTransform row)
        {
            var header = CreatePanel(parent, "Header", new Color(0.20f, 0.24f, 0.34f, 1f));

            var rowGo = new GameObject("HeaderRow", typeof(RectTransform));
            rowGo.transform.SetParent(header.transform, false);
            var rowRt = (RectTransform)rowGo.transform;
            rowRt.anchorMin = new Vector2(0f, 0f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.offsetMin = new Vector2(sideMargin, 10f);
            rowRt.offsetMax = new Vector2(-sideMargin, -10f);
            row = rowRt;

            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 20f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            site = AddText(rowGo.transform, "Site", 40f, UIFontWeight.Bold, TextColor,
                Vector2.zero, new Vector2(0f, 60f), TextAlignmentOptions.Left);
            board = AddText(rowGo.transform, "Board", 28f, UIFontWeight.Regular, new Color(0.78f, 0.82f, 0.9f),
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

        /// <summary>
        /// 대사와 선택지의 줄바꿈 규칙을 한 곳에서 정한다.
        ///
        /// 상자보다 조금 긴 대사가 글자 두세 개만 다음 줄로 흘려 보내고 있었다.
        /// 그래서 줄을 늘리기 전에 글자를 조금 줄이게 한다. 한 급 줄여 한 줄에 담기면 그렇게 담고,
        /// 그래도 넘칠 때만 줄을 바꾼다. 어느 대사든 같은 규칙을 쓰므로 모습이 고르다.
        ///
        /// minRatio 는 줄여도 되는 밑바닥이다. 여기보다 작아지지 않으므로 대사가 갑자기 잘아지지 않는다.
        /// </summary>

        private static void ConfigureBodyText(TMP_Text text, float size, float minRatio = 0.8f, float lineGap = 14f)
        {
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Round(size * minRatio);
            text.lineSpacing = lineGap;
            text.raycastTarget = false;
        }

        /// <summary>
        /// 굴러가는 칸 안에 드는 글. 글자를 줄이지 않고 칸이 글을 따라 늘어나게 둔다.
        ///
        /// 여기서 글자 크기를 저절로 줄이게 하면, 칸은 글에 맞추려 하고 글은 칸에 맞추려 해서
        /// 둘이 서로를 쫓는다. 굴러가는 자리에서는 넘치는 쪽이 아니라 길어지는 쪽이 맞다.
        /// </summary>
        private static void ConfigureScrolledText(TMP_Text text, float size, float lineGap)
        {
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = lineGap;
            text.paragraphSpacing = 22f;
            text.raycastTarget = false;
        }

        /// <summary>
        /// 화면에 든 글을 한 규칙으로 맞춘다. 화면을 다 짜고 맨 마지막에 한 번 부른다.
        ///
        /// 글자 크기는 1920 기준으로 손으로 잡아 둔 값이라, 문구가 길어지면 칸을 넘쳐 흘렀다.
        /// 넘칠 때는 칸을 넘어가지 말고 글자를 한 급 줄여 담게 한다. 어느 화면이든 같은 규칙이다.
        ///
        /// 두 곳은 건드리지 않는다.
        ///   적는 칸 - 글자가 저절로 줄어들면 커서와 글자가 어긋난다.
        ///   칸이 내용을 따라 늘어나는 곳 - 칸은 글자에, 글자는 칸에 맞추려 들면 둘이 서로를 쫓는다.
        /// 이미 제 규칙을 정해 둔 글도 그대로 둔다.
        /// </summary>
        private static void TidyTexts(GameObject root)
        {
            if (root == null) return;

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponentInParent<TMP_InputField>(true) != null) continue;

                // 제 규칙을 정해 둔 글도 줄 사이는 같이 맞춘다. 화면끼리 줄 간격이 다르면 눈에 띈다.
                if (text.enableAutoSizing)
                {
                    TidyLineGaps(text);
                    continue;
                }

                if (GrowsWithContent(text.rectTransform, root.transform)) continue;

                text.overflowMode = TextOverflowModes.Truncate;
                text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = Mathf.Max(10f, Mathf.Round(text.fontSize * 0.7f));

                TidyLineGaps(text);
            }
        }

        /// <summary>
        /// 여러 줄로 앉는 글의 줄 사이를 띄운다.
        ///
        /// 줄이 붙어 있으면 목록이 한 덩어리로 보여 어디서 한 항목이 끝나는지 읽히지 않는다.
        /// 접혀 넘어간 줄은 조금, 줄을 바꿔 새로 시작한 줄은 그보다 넉넉히 띄운다.
        /// 한 줄로만 서는 글에는 띄울 사이가 없으므로 건드리지 않는다.
        /// </summary>
        private static void TidyLineGaps(TMP_Text text)
        {
            if (text.textWrappingMode == TextWrappingModes.NoWrap) return;

            if (Mathf.Approximately(text.lineSpacing, 0f)) text.lineSpacing = 14f;
            if (Mathf.Approximately(text.paragraphSpacing, 0f)) text.paragraphSpacing = 26f;
        }

        /// <summary>이 글이 든 칸이 내용을 따라 늘어나는가.</summary>
        private static bool GrowsWithContent(Transform from, Transform stopAt)
        {
            for (var t = from; t != null && t != stopAt.parent; t = t.parent)
            {
                if (t.GetComponent<ContentSizeFitter>() != null) return true;
            }
            return false;
        }

        /// <summary>한 줄로만 서야 하는 글. 넘치면 줄을 바꾸는 대신 글자를 줄인다.</summary>
        private static void ConfigureOneLineText(TMP_Text text, float size, float minRatio = 0.72f)
        {
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.enableAutoSizing = true;
            text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Round(size * minRatio);
            text.raycastTarget = false;
        }

        /// <summary>이미 만든 버튼의 크기와 글자 크기를 바꾼다.</summary>
        private static void ResizeButton(GameObject button, Vector2 size, float fontSize)
        {
            if (button == null) return;

            ((RectTransform)button.transform).sizeDelta = size;

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.fontSize = fontSize;
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
