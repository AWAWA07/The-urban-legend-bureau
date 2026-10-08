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

            // 곁에 서면 네모 칸이 아니라 그 자리의 그림이 밝아진다.
            // 빈자리는 긴 의자의 한 칸이라 따로 떨어진 그림이 없다. 그 칸 크기의 네모를 그대로 쓴다.
            FitHighlightToArt(window, trainInside.transform);
            SetHighlightTargets(cctv, trainInside.transform, "Cctv/Mount", "Cctv/Arm", "Cctv/Joint", "Cctv/Head/Housing",
                "Cctv/Head/HousingShade", "Cctv/Head/Visor", "Cctv/Head/LensRing");   // 빨간 불은 따로 켜고 끄므로 뺀다
            FitHighlightToArt(platform, trainInside.transform);

            // 좌석에서 얻은 진술이 있어야 영상과 대조할 마음이 든다.
            ConfigurePoint(seat, "point_subway_seat",
                new[] { "action_subway_seat_search", "action_subway_photo" }, null, false, CaseStep.Started);
            ConfigurePoint(window, "point_subway_window",
                new[] { "action_subway_window_trace", "action_subway_glass_check", "action_subway_photo" }, null, false, CaseStep.Started);
            ConfigurePoint(cctv, "point_subway_cctv",
                new[] { "action_subway_cctv_inspect", "action_subway_photo" },
                new[] { "clue_subway_001" }, false, CaseStep.Started);
            // 글끼리 견줘 보는 조사는 여기에 둔다. 승강장 기록을 뒤지는 자리라 견줄 거리가 있다.
            // 이 방법이 주는 단서(clue_subway_004)가 없으면 맞는 규칙을 세울 수 없다. 어디에도 걸려 있지 않았다.
            // 목격담 대조도 여기에 둔다. 겪고도 멀쩡한 사람이 하나 있다는 것(clue_subway_006)이
            // 파훼법의 다른 한쪽이다. 돌아본 쪽과 안 돌아본 쪽을 나란히 놓아야 무엇이 갈랐는지가 보인다.
            ConfigurePoint(platform, "point_subway_platform",
                new[] { "action_subway_platform_search", "action_subway_platform_trace", "action_subway_compare", "action_subway_witness" },
                null, false, CaseStep.Started);

            // 열차가 들어오면 승강장 대신 열차 안이 보인다.
            // 장소를 새로 만들지 않고 보이는 것만 갈아 끼운다.
            var swap = fieldRootSubway.AddComponent<FieldSceneSwap>();
            var swapSo = new SerializedObject(swap);
            swapSo.Update();
            SetObjectArray(swapSo.FindProperty("_beforeRoots"), platformOutside);
            SetObjectArray(swapSo.FindProperty("_afterRoots"), trainInside);
            swapSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 숙소 ---
            // 사건 현장이 아니라 검열국 사람들이 지내는 방이다. 걷기와 말풍선은 현장과 같은 것을 쓴다.
            var fieldRootRoom = new GameObject("FieldRoot_Room");
            BuildFieldBackground(fieldRootRoom.transform);
            BuildRoom(fieldRootRoom.transform);

            var fieldGo = new GameObject("FieldController");
            var field = fieldGo.AddComponent<FieldController>();
            var fieldSo = new SerializedObject(field);
            fieldSo.Update();
            var groups = fieldSo.FindProperty("_fieldGroups");
            groups.arraySize = 4;
            var g3 = groups.GetArrayElementAtIndex(3);
            g3.FindPropertyRelative("legendId").stringValue = CaseDirector.RoomFieldId;
            g3.FindPropertyRelative("root").objectReferenceValue = fieldRootRoom;
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
            var report = BuildReportScreen("Screen_Report", out var reportButtons);
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
            var travel = BuildTravelScreen("Screen_Travel");
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
            // 타이틀은 최종 시안대로 꾸민다. 예전 가로 버튼 줄은 숨기고 왼쪽 세로 메뉴 여섯 줄을 쓴다.
            var titleMenu = StyleTitleScreen(title, titleButtons);
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
            var btnReportSubmit = CreateButton(reportButtons, "Btn_ReportSubmit", "ui.report.btn_submit");
            var btnReportBack = CreateButton(reportButtons, "Btn_ReportBack", "ui.report.btn_back");
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
            dso.FindProperty("_reportScreen").objectReferenceValue = report;
            dso.FindProperty("_toastScreen").objectReferenceValue = toast;
            dso.FindProperty("_resultScreen").objectReferenceValue = result;
            dso.FindProperty("_field").objectReferenceValue = field;
            // 괴담넷은 하나뿐이다. 컴퓨터도 휴대폰도 이 화면을 연다. 메모장도 마찬가지다.
            dso.FindProperty("_communityScreen").objectReferenceValue = community;
            dso.FindProperty("_memoScreen").objectReferenceValue = memo;
            // 바탕화면도 하나뿐이다. 튜토리얼이 처음 여는 것과 숙소의 컴퓨터가 여는 것이 같다.
            dso.FindProperty("_desktopScreen").objectReferenceValue = desktop;

            // 괴담넷에 글을 써 올리는 흐름. 괴담넷 화면과 게시판을 그대로 빌려 쓴다.
            var postWriting = directorGo.AddComponent<PostWritingDirector>();
            dso.FindProperty("_postWriting").objectReferenceValue = postWriting;
            dso.FindProperty("_travelScreen").objectReferenceValue = travel;
            dso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(btnStart.GetComponent<Button>().onClick, director.OnStartClicked);
            UnityEventTools.AddPersistentListener(btnHelp.GetComponent<Button>().onClick, director.OnOpenHelpClicked);
            UnityEventTools.AddPersistentListener(btnSettings.GetComponent<Button>().onClick, director.OnOpenSettingsClicked);
            UnityEventTools.AddPersistentListener(btnQuit.GetComponent<Button>().onClick, director.OnQuitClicked);

            // 타이틀 세로 메뉴. 이어하기는 예전 시작과 같고(처음이면 이야기부터), 새 게임은 이야기를 처음부터 돌린다.
            // 불러오기는 아직 저장 칸이 하나라 사건 목록을 연다. 크레딧은 아직 따로 없어 설명 화면을 연다.
            UnityEventTools.AddPersistentListener(titleMenu[0].onClick, director.OnStartClicked);
            UnityEventTools.AddPersistentListener(titleMenu[1].onClick, director.OnReplayTutorialClicked);
            UnityEventTools.AddPersistentListener(titleMenu[2].onClick, director.OnOpenCaseListClicked);
            UnityEventTools.AddPersistentListener(titleMenu[3].onClick, director.OnOpenSettingsClicked);
            UnityEventTools.AddPersistentListener(titleMenu[4].onClick, director.OnOpenHelpClicked);
            UnityEventTools.AddPersistentListener(titleMenu[5].onClick, director.OnQuitClicked);
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

            // 시간이 흐르면 괴담넷 글에 조회수와 반응이 붙고 새 글이 올라온다. 게시판은 튜토리얼이 들고 있는 그것이다.
            var boardActivity = tutorialGo.AddComponent<BoardActivityDirector>();
            var baso = new SerializedObject(boardActivity);
            baso.Update();
            baso.FindProperty("_data").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<UrbanLegendBureau.Data.BoardActivitySO>("Assets/_Project/Data/Board/board_activity.asset");
            baso.FindProperty("_tutorial").objectReferenceValue = tutorial;
            baso.FindProperty("_communityScreen").objectReferenceValue = community;
            baso.ApplyModifiedPropertiesWithoutUndo();
            dso.FindProperty("_boardActivity").objectReferenceValue = boardActivity;
            dso.ApplyModifiedPropertiesWithoutUndo();

            var pwso = new SerializedObject(postWriting);
            pwso.Update();
            pwso.FindProperty("_communityScreen").objectReferenceValue = community;
            pwso.FindProperty("_tutorial").objectReferenceValue = tutorial;
            pwso.ApplyModifiedPropertiesWithoutUndo();
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
            // 제출 단추는 Build 에서 만들므로 여기서 걸어 준다. 다 맞히면 화면이 스스로 거둔다.
            var rso = new SerializedObject(report);
            rso.Update();
            rso.FindProperty("_submitButton").objectReferenceValue = btnReportSubmit;
            rso.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(btnReportSubmit.GetComponent<Button>().onClick, report.OnSubmitClicked);
            UnityEventTools.AddPersistentListener(btnReportBack.GetComponent<Button>().onClick, director.OnRulesBackClicked);
            UnityEventTools.AddPersistentListener(btnBack.GetComponent<Button>().onClick, director.OnBackToTitleClicked);
            UnityEventTools.AddPersistentListener(btnClueOk.GetComponent<Button>().onClick, director.OnCluePopupConfirmClicked);
            UnityEventTools.AddPersistentListener(btnRuleOk.GetComponent<Button>().onClick, director.OnRulePopupConfirmClicked);
            UnityEventTools.AddPersistentListener(btnWarningOk.GetComponent<Button>().onClick, director.OnSpreadWarningConfirmClicked);

            // 화면마다 손으로 잡아 둔 글자 크기를 한 규칙으로 맞춘다.
            // 문구가 길어져 칸을 넘치던 곳들이 여기서 한꺼번에 정리된다.
            var screens = new UIScreen[]
            {
                title, bureau, caseList, actionList, ruleList, internetList, internetPage, fieldHud,
                exorcism, result, help, settings, dialogue, talk, desktop, community, memo, toast, report, travel,
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

                // 유리에 비친 승강장 조명. 위쪽이 옅게 밝다.
                AddFieldGradient(train.transform, "CarWindowSheen_" + i, new Vector2(windowX[i], 1.5f),
                    new Vector2(2.6f, 1.0f), new Color(1f, 1f, 1f, 0.07f), -5);
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

            // --- 승강장의 결 ---
            // 역 이름. 이름표가 비어 있으면 판자처럼 보인다.
            AddFieldWorldText(root.transform, "SignText", new Vector2(-5f, 2.2f), new Vector2(4.8f, 0.9f),
                "ui.field.platform", new Color(0.86f, 0.88f, 0.94f), -2);

            // 천장 조명. 처마 아래에 형광등이 줄지어 있고, 그 아래로 빛이 번진다.
            for (int i = -3; i <= 3; i++)
            {
                AddFieldRect(root.transform, "PlatformLamp_" + (i + 3), new Vector2(i * 6.2f, 3.2f),
                    new Vector2(2.4f, 0.12f), new Color(0.86f, 0.86f, 0.78f), -2);
                AddFieldSoft(root.transform, "PlatformLampGlow_" + (i + 3), new Vector2(i * 6.2f, 2.7f),
                    new Vector2(4.4f, 1.6f), new Color(1f, 0.97f, 0.85f, 0.12f), -2);
            }

            // 처마 그늘. 천장 바로 아래가 조금 어둡다.
            AddFieldGradient(root.transform, "SoffitShade", new Vector2(0f, 2.9f), new Vector2(44f, 0.9f),
                new Color(0f, 0f, 0f, 0.28f), -3);

            // 바닥 타일. 세로 이음새와 가로 이음새 하나. 안전선 쪽이 조명을 받아 조금 밝다.
            AddFieldGradient(root.transform, "FloorLight", new Vector2(0f, -3.4f), new Vector2(44f, 1.2f),
                new Color(1f, 1f, 1f, 0.05f), 0);
            for (int i = 0; i <= 27; i++)
            {
                AddFieldRect(root.transform, "FloorJoint_" + i, new Vector2(-21.6f + i * 1.6f, -4.15f),
                    new Vector2(0.04f, 2.6f), new Color(0.18f, 0.18f, 0.22f), 0);
            }
            AddFieldRect(root.transform, "FloorJointH", new Vector2(0f, -4.3f), new Vector2(44f, 0.04f),
                new Color(0.18f, 0.18f, 0.22f), 0);

            // 안전선 위의 점자 블록 홈. 노란 선이 한 장의 띠가 아니라 블록처럼 보인다.
            AddFieldRect(root.transform, "SafetyGroove_A", new Vector2(0f, -2.66f), new Vector2(44f, 0.03f),
                new Color(0.58f, 0.49f, 0.20f), 1);
            AddFieldRect(root.transform, "SafetyGroove_B", new Vector2(0f, -2.78f), new Vector2(44f, 0.03f),
                new Color(0.58f, 0.49f, 0.20f), 1);

            // 벽과 바닥이 만나는 곳. 스크린도어 발치가 조금 어둡다.
            AddFieldGradient(root.transform, "FloorContact", new Vector2(0f, -2.34f), new Vector2(44f, 0.5f),
                new Color(0f, 0f, 0f, 0.25f), 0, 180f);

            // --- 탈 자리 ---
            // 가운데 문 앞. 문이 다 열린 뒤에만 켜진다.
            var boarding = BuildPoint(root.transform, "InvestigationPoint_SubwayBoard", new Vector2(0f, 0.4f),
                new Vector2(DoorGap - 0.4f, 4.6f), new Color(0.40f, 0.44f, 0.36f),
                "field.subway.board", "field.subway.board", null);
            ConfigurePoint(boarding, CaseDirector.BoardingPointId, null, null, false, CaseStep.Started);
            FitHighlightToArt(boarding, root.transform);
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

            // 왼쪽 위 모서리의 CCTV. 천장에 붙은 받침에서 팔이 내려오고, 팔 끝 관절에 몸통이 매달린다.
            // 몸통은 관절을 축으로 돈다(CctvGlance). 평소에는 부자연스럽게 천장 쪽을 올려다본다.
            // 이름표가 맨 위 한 줄에 가리지 않도록 조사 지점을 y 2.6 에 둔다.
            // 걸어가서 닿는 자리여야 한다. 걸을 수 있는 왼쪽 끝(-12)보다 안쪽에 둔다.
            var camBody = new Color(0.78f, 0.79f, 0.80f);
            var camShade = new Color(0.55f, 0.56f, 0.59f);
            var camDark = new Color(0.10f, 0.10f, 0.12f);
            var cctvRoot = new GameObject("Cctv");
            cctvRoot.transform.SetParent(root.transform, false);
            cctvRoot.transform.localPosition = new Vector2(-10.6f, 0f);
            AddFieldRect(cctvRoot.transform, "Mount", new Vector2(0f, 3.5f), new Vector2(0.62f, 0.14f), camShade, -5);
            AddFieldRect(cctvRoot.transform, "Arm", new Vector2(0f, 3.18f), new Vector2(0.14f, 0.62f), camShade, -5);
            AddFieldRect(cctvRoot.transform, "Joint", new Vector2(0f, 2.88f), new Vector2(0.26f, 0.26f), camDark, -4);

            // 몸통. 관절이 원점이고 렌즈는 오른쪽을 본다. 관절 아래쪽에 매달린 꼴이라 몸통 가운데가 관절보다 조금 아래다.
            var cctvHead = new GameObject("Head");
            cctvHead.transform.SetParent(cctvRoot.transform, false);
            cctvHead.transform.localPosition = new Vector2(0f, 2.88f);
            cctvHead.transform.localScale = Vector3.one * 1.35f;   // 멀리서도 렌즈가 어디를 보는지 알아보게 조금 키운다
            AddFieldRect(cctvHead.transform, "Housing", new Vector2(0.42f, -0.16f), new Vector2(1.05f, 0.5f), camBody, -4);
            AddFieldRect(cctvHead.transform, "HousingShade", new Vector2(0.42f, -0.36f), new Vector2(1.05f, 0.12f), camShade, -3);
            AddFieldRect(cctvHead.transform, "Visor", new Vector2(0.5f, 0.12f), new Vector2(1.25f, 0.1f), camShade, -3);
            AddFieldRect(cctvHead.transform, "LensRing", new Vector2(0.98f, -0.16f), new Vector2(0.16f, 0.42f), camDark, -3);
            AddFieldRect(cctvHead.transform, "Lens", new Vector2(1.06f, -0.16f), new Vector2(0.08f, 0.26f),
                new Color(0.18f, 0.24f, 0.34f), -2);
            var cctvLight = AddFieldRect(cctvHead.transform, "RecLight", new Vector2(0.16f, -0.06f), new Vector2(0.13f, 0.13f),
                new Color(1f, 0.12f, 0.1f, 1f), -2);

            var glance = cctvRoot.AddComponent<CctvGlance>();
            var gso = new SerializedObject(glance);
            gso.Update();
            gso.FindProperty("_head").objectReferenceValue = cctvHead.transform;
            gso.FindProperty("_light").objectReferenceValue = cctvLight.GetComponent<SpriteRenderer>();
            gso.ApplyModifiedPropertiesWithoutUndo();

            // --- 객실의 빛과 그늘 ---
            // 형광등 아래로 번지는 빛. 등 하나하나가 제 몫의 빛을 떨군다.
            for (int i = -3; i <= 3; i++)
            {
                AddFieldSoft(root.transform, "CeilingLampGlow_" + (i + 3), new Vector2(i * 4.4f, 3.45f),
                    new Vector2(4.6f, 1.8f), new Color(1f, 0.98f, 0.86f, 0.13f), -6);
            }

            // 천장 모서리 아래의 그늘과, 벽과 바닥이 만나는 곳의 어둠.
            AddFieldGradient(root.transform, "CeilingShade", new Vector2(0f, 3.1f), new Vector2(44f, 0.9f),
                new Color(0f, 0f, 0f, 0.25f), -7);
            AddFieldGradient(root.transform, "FloorContact", new Vector2(0f, -3.05f), new Vector2(44f, 0.8f),
                new Color(0f, 0f, 0f, 0.3f), -7, 180f);

            // 바닥. 형광등이 비쳐 벽 쪽이 옅게 번들거리고, 가운데로 미끄럼 방지 줄이 지나간다.
            AddFieldGradient(root.transform, "FloorSheen", new Vector2(0f, -3.85f), new Vector2(44f, 0.8f),
                new Color(1f, 1f, 1f, 0.05f), -7);
            AddFieldRect(root.transform, "FloorStrip", new Vector2(0f, -4.3f), new Vector2(44f, 0.06f),
                new Color(0.19f, 0.19f, 0.24f), -7);

            // 벽판 이음새. 차체가 판 여러 장을 이어 붙인 것으로 보인다.
            foreach (float seamX in new[] { -12.6f, -7.6f, -2.4f, 2.4f, 7.6f, 12.6f })
            {
                AddFieldRect(root.transform, "WallSeam_" + seamX, new Vector2(seamX, 0.3f), new Vector2(0.05f, 6.4f),
                    new Color(0.16f, 0.17f, 0.21f), -8);
            }

            // 창 위의 광고 액자. 막차 안이 사람 사는 곳으로 보이게 한다.
            foreach (float adX in new[] { -5f, 5f })
            {
                AddFieldRect(root.transform, "AdFrame_" + adX, new Vector2(adX, 2.72f), new Vector2(3.4f, 0.62f),
                    new Color(0.30f, 0.31f, 0.37f), -7);
                AddFieldRect(root.transform, "Ad_" + adX, new Vector2(adX, 2.72f), new Vector2(3.2f, 0.46f),
                    adX < 0 ? new Color(0.46f, 0.40f, 0.30f) : new Color(0.30f, 0.38f, 0.46f), -6);
            }

            // 객실 안에 선 두 사람. 바닥 위에 놓는다. 의자와 봉 사이를 오간다.
            BuildFieldActors(root.transform, -3.86f, -2.0f, 2.4f, -12f, 12f, scale: TrainActorScale);

            root.SetActive(false);
            return root;
        }

        /// <summary>
        /// 검열국 숙소의 방 하나. 열차 안과 같은 짜임으로 옆에서 본 한 폭이다.
        ///
        /// 왼쪽은 잠자리(창, 침대, 협탁), 가운데는 일하는 자리(책상, 컴퓨터, 의자, 게시판),
        /// 오른쪽은 살림(책장, 벽시계, 화분, 방문)이다. 바닥 높이와 걷는 폭은 열차 안과 같다.
        ///
        /// 살펴볼 수 있는 것은 컴퓨터 / 침대 / 책장 / 시계 / 방문이다. 제 구실을 하는 것은 컴퓨터뿐이고
        /// 나머지는 차지한이 한마디 하고 끝난다. 무엇을 하는지는 CaseDirector 가 정한다.
        /// </summary>
        private static void BuildRoom(Transform parent)
        {
            var root = new GameObject("Room");
            root.transform.SetParent(parent, false);

            var wall = new Color(0.24f, 0.21f, 0.21f);
            var wainscot = new Color(0.19f, 0.16f, 0.16f);
            var trim = new Color(0.31f, 0.26f, 0.24f);
            var wood = new Color(0.33f, 0.24f, 0.18f);
            var woodDark = new Color(0.24f, 0.17f, 0.13f);

            // 방 껍데기. 벽 / 아래 벽판 / 천장 몰딩 / 바닥. 높이는 열차 안과 맞춘다.
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), wall, -9);
            AddFieldRect(root.transform, "CeilingTrim", new Vector2(0f, 3.62f), new Vector2(44f, 0.16f), trim, -7);
            AddFieldRect(root.transform, "Wainscot", new Vector2(0f, -2.3f), new Vector2(44f, 2.2f), wainscot, -8);
            AddFieldRect(root.transform, "ChairRail", new Vector2(0f, -1.2f), new Vector2(44f, 0.12f), trim, -7);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f),
                new Color(0.25f, 0.19f, 0.15f), -8);
            AddFieldRect(root.transform, "FloorEdge", new Vector2(0f, -3.42f), new Vector2(44f, 0.16f), woodDark, -7);
            AddFieldRect(root.transform, "Rug", new Vector2(0.5f, -4.05f), new Vector2(7.4f, 0.5f),
                new Color(0.36f, 0.22f, 0.22f), -6);   // 마룻바닥 이음새보다 위에 깐다
            AddFieldRect(root.transform, "RugBorder", new Vector2(0.5f, -4.05f), new Vector2(6.9f, 0.3f),
                new Color(0.44f, 0.30f, 0.26f), -5);
            AddFieldRect(root.transform, "RugInner", new Vector2(0.5f, -4.05f), new Vector2(6.6f, 0.2f),
                new Color(0.36f, 0.22f, 0.22f), -4);

            // --- 방의 결 ---
            // 벽지의 옅은 세로 줄무늬. 민짜 벽이 아니라 도배한 벽으로 보인다.
            var stripe = Color.Lerp(wall, Color.white, 0.035f);
            for (int i = 0; i <= 48; i++)
            {
                AddFieldRect(root.transform, "Wallpaper_" + i, new Vector2(-21.6f + i * 0.9f, 1.25f),
                    new Vector2(0.28f, 4.9f), stripe, -8);
            }

            // 아래 벽판의 이음새와 걸레받이.
            for (int i = 0; i <= 24; i++)
            {
                AddFieldRect(root.transform, "WainscotSeam_" + i, new Vector2(-21.6f + i * 1.8f, -2.3f),
                    new Vector2(0.05f, 2.0f), Color.Lerp(wainscot, Color.black, 0.25f), -7);
            }
            AddFieldRect(root.transform, "Baseboard", new Vector2(0f, -3.28f), new Vector2(44f, 0.22f), woodDark, -7);

            // 마룻바닥. 가로 이음새 둘과 엇갈린 세로 이음새.
            var plankLine = new Color(0.20f, 0.15f, 0.12f);
            float[] plankY = { -3.95f, -4.5f, -5.05f };
            for (int row = 0; row < plankY.Length; row++)
            {
                AddFieldRect(root.transform, "PlankRow_" + row, new Vector2(0f, plankY[row]), new Vector2(44f, 0.03f), plankLine, -7);
                float offset = row % 2 == 0 ? 0f : 1.2f;
                for (int i = 0; i <= 18; i++)
                {
                    AddFieldRect(root.transform, "PlankJoint_" + row + "_" + i,
                        new Vector2(-21.6f + offset + i * 2.4f, plankY[row] + 0.27f), new Vector2(0.03f, 0.52f), plankLine, -7);
                }
            }
            AddFieldGradient(root.transform, "FloorSheen", new Vector2(0f, -3.8f), new Vector2(44f, 0.7f),
                new Color(1f, 0.95f, 0.85f, 0.05f), -7);

            // 천장 아래 그늘과 벽이 바닥에 닿는 곳의 어둠.
            AddFieldGradient(root.transform, "CeilingShade", new Vector2(0f, 3.1f), new Vector2(44f, 1.0f),
                new Color(0f, 0f, 0f, 0.28f), -7);
            AddFieldGradient(root.transform, "FloorContact", new Vector2(0f, -3.0f), new Vector2(44f, 0.6f),
                new Color(0f, 0f, 0f, 0.28f), -7, 180f);

            // 가구 밑의 그림자. 물건이 바닥에 놓여 있는 것으로 보인다.
            var floorShadow = new Color(0f, 0f, 0f, 0.38f);
            AddFieldSoft(root.transform, "Shadow_Bed", new Vector2(-9.2f, -3.42f), new Vector2(7.2f, 0.5f), floorShadow, -7);
            AddFieldSoft(root.transform, "Shadow_Nightstand", new Vector2(-5.1f, -3.42f), new Vector2(1.8f, 0.34f), floorShadow, -7);
            AddFieldSoft(root.transform, "Shadow_Desk", new Vector2(0.6f, -3.42f), new Vector2(5.4f, 0.42f), floorShadow, -7);
            AddFieldSoft(root.transform, "Shadow_Shelf", new Vector2(6.8f, -3.42f), new Vector2(3.8f, 0.42f), floorShadow, -7);
            AddFieldSoft(root.transform, "Shadow_Plant", new Vector2(9.9f, -3.42f), new Vector2(1.3f, 0.28f), floorShadow, -7);

            // --- 왼쪽: 잠자리 ---
            // 창 너머는 한밤의 도시다. 멀리 켜진 창 몇 개만 보인다.
            var window = new GameObject("Window");
            window.transform.SetParent(root.transform, false);
            window.transform.localPosition = new Vector3(-9.2f, 1.5f, 0f);
            AddFieldRect(window.transform, "Frame", Vector2.zero, new Vector2(4.4f, 2.9f), trim, -7);
            AddFieldRect(window.transform, "Glass", Vector2.zero, new Vector2(4.0f, 2.5f),
                new Color(0.09f, 0.11f, 0.19f), -6);
            var cityLight = new Color(0.62f, 0.54f, 0.32f);
            AddFieldRect(window.transform, "City_0", new Vector2(-1.3f, -0.8f), new Vector2(0.18f, 0.22f), cityLight, -5);
            AddFieldRect(window.transform, "City_1", new Vector2(-0.6f, -0.5f), new Vector2(0.18f, 0.22f), cityLight, -5);
            AddFieldRect(window.transform, "City_2", new Vector2(0.9f, -0.9f), new Vector2(0.18f, 0.22f), cityLight, -5);
            AddFieldRect(window.transform, "City_3", new Vector2(1.5f, -0.4f), new Vector2(0.18f, 0.22f), cityLight, -5);
            AddFieldRect(window.transform, "Mullion_V", Vector2.zero, new Vector2(0.14f, 2.5f), trim, -4);
            AddFieldRect(window.transform, "Mullion_H", Vector2.zero, new Vector2(4.0f, 0.14f), trim, -4);
            AddFieldGradient(window.transform, "Sheen", new Vector2(0f, 0.6f), new Vector2(4.0f, 1.3f),
                new Color(0.75f, 0.82f, 1f, 0.08f), -5);
            var curtain = new Color(0.38f, 0.25f, 0.27f);
            AddFieldRect(window.transform, "Curtain_L", new Vector2(-2.35f, -0.15f), new Vector2(0.8f, 3.3f), curtain, -3);
            AddFieldRect(window.transform, "Curtain_R", new Vector2(2.35f, -0.15f), new Vector2(0.8f, 3.3f), curtain, -3);
            AddFieldRect(window.transform, "CurtainRod", new Vector2(0f, 1.65f), new Vector2(5.6f, 0.12f), woodDark, -3);

            // 침대. 머리판이 왼쪽 벽에 붙고 발치는 가운데를 향한다.
            var bed = new GameObject("Bed");
            bed.transform.SetParent(root.transform, false);
            bed.transform.localPosition = new Vector3(-9.2f, 0f, 0f);
            AddFieldRect(bed.transform, "Headboard", new Vector2(-3.05f, -1.95f), new Vector2(0.45f, 2.9f), wood, -6);
            AddFieldRect(bed.transform, "Footboard", new Vector2(3.05f, -2.45f), new Vector2(0.4f, 1.9f), wood, -4);
            AddFieldRect(bed.transform, "Frame", new Vector2(0f, -2.95f), new Vector2(6.0f, 0.8f), woodDark, -6);
            AddFieldRect(bed.transform, "Mattress", new Vector2(0f, -2.3f), new Vector2(5.8f, 0.55f),
                new Color(0.80f, 0.78f, 0.74f), -5);
            AddFieldRect(bed.transform, "Blanket", new Vector2(0.6f, -2.2f), new Vector2(4.6f, 0.7f),
                new Color(0.29f, 0.36f, 0.52f), -4);
            AddFieldRect(bed.transform, "BlanketFold", new Vector2(-1.6f, -1.92f), new Vector2(0.5f, 0.18f),
                new Color(0.36f, 0.43f, 0.60f), -3);
            AddFieldRect(bed.transform, "Pillow", new Vector2(-2.25f, -1.86f), new Vector2(1.2f, 0.42f),
                new Color(0.90f, 0.88f, 0.84f), -4);

            // 협탁과 스탠드. 방에 켜진 불은 이것과 모니터뿐이다.
            AddFieldRect(root.transform, "Nightstand", new Vector2(-5.1f, -2.75f), new Vector2(1.2f, 1.3f), wood, -6);
            AddFieldRect(root.transform, "NightstandDrawer", new Vector2(-5.1f, -2.6f), new Vector2(0.9f, 0.08f), woodDark, -5);
            AddFieldRect(root.transform, "LampStem", new Vector2(-5.1f, -1.85f), new Vector2(0.1f, 0.5f), woodDark, -5);
            AddFieldRect(root.transform, "LampShade", new Vector2(-5.1f, -1.42f), new Vector2(0.85f, 0.5f),
                new Color(0.86f, 0.74f, 0.46f), -5);
            // 스탠드 불빛이 벽과 협탁에 번진다.
            AddFieldSoft(root.transform, "LampGlow", new Vector2(-5.1f, -1.6f), new Vector2(3.0f, 2.4f),
                new Color(1f, 0.85f, 0.5f, 0.2f), -3);

            // 옷걸이. 자기 전에 코트를 벗어 거는 곳이다. 지금은 도형으로 세운 임시 모습이다.
            // 침대 머리맡 왼쪽, 침대보다 앞에 선다. 그래서 침대 그림보다 위에 그린다.
            // 걸린 코트(CoatHung)는 꺼 두었다가 코트를 거는 그림이 들어오면 그 순간에 켠다.
            var coatStand = new GameObject("CoatStand");
            coatStand.transform.SetParent(root.transform, false);
            coatStand.transform.localPosition = new Vector2(-12.7f, -0.35f);   // 벽보다 바닥 앞쪽에 선다
            AddFieldSoft(coatStand.transform, "Shadow", new Vector2(0f, -3.42f), new Vector2(1.2f, 0.26f), floorShadow, -3);
            AddFieldRect(coatStand.transform, "Base", new Vector2(0f, -3.3f), new Vector2(0.9f, 0.12f), woodDark, -2);
            AddFieldRect(coatStand.transform, "Foot_L", new Vector2(-0.32f, -3.36f), new Vector2(0.2f, 0.1f), woodDark, -2);
            AddFieldRect(coatStand.transform, "Foot_R", new Vector2(0.32f, -3.36f), new Vector2(0.2f, 0.1f), woodDark, -2);
            AddFieldRect(coatStand.transform, "Pole", new Vector2(0f, -1.2f), new Vector2(0.12f, 4.1f), wood, -2);
            AddFieldRect(coatStand.transform, "Knob", new Vector2(0f, 0.92f), new Vector2(0.22f, 0.18f), woodDark, -2);
            AddFieldRect(coatStand.transform, "Hook_L", new Vector2(-0.22f, 0.62f), new Vector2(0.34f, 0.07f), woodDark, -2);
            AddFieldRect(coatStand.transform, "Hook_R", new Vector2(0.22f, 0.62f), new Vector2(0.34f, 0.07f), woodDark, -2);
            AddFieldRect(coatStand.transform, "HookTip_L", new Vector2(-0.37f, 0.69f), new Vector2(0.06f, 0.16f), woodDark, -2);
            AddFieldRect(coatStand.transform, "HookTip_R", new Vector2(0.37f, 0.69f), new Vector2(0.06f, 0.16f), woodDark, -2);

            var coatHung = new GameObject("CoatHung");
            coatHung.transform.SetParent(coatStand.transform, false);
            AddFieldRect(coatHung.transform, "Coat", new Vector2(0.05f, -0.55f), new Vector2(0.95f, 2.3f), new Color(0.07f, 0.07f, 0.09f), -1);
            AddFieldRect(coatHung.transform, "Lining", new Vector2(0.05f, -0.9f), new Vector2(0.18f, 1.5f), new Color(0.13f, 0.24f, 0.62f), 0);
            AddFieldRect(coatHung.transform, "Collar", new Vector2(0.05f, 0.5f), new Vector2(0.7f, 0.22f), new Color(0.07f, 0.07f, 0.09f), 0);
            coatHung.SetActive(false);

            // --- 가운데: 일하는 자리 ---
            // 책상 위 벽의 게시판. 사건 메모가 꽂혀 있다. 검열국 사람의 방이라는 것이 여기서 보인다.
            AddFieldRect(root.transform, "Corkboard", new Vector2(0.5f, 2.05f), new Vector2(3.6f, 1.5f),
                new Color(0.46f, 0.34f, 0.24f), -7);
            AddFieldRect(root.transform, "Note_0", new Vector2(-0.6f, 2.2f), new Vector2(0.7f, 0.6f),
                new Color(0.84f, 0.80f, 0.60f), -6);
            AddFieldRect(root.transform, "Note_1", new Vector2(0.4f, 1.9f), new Vector2(0.8f, 0.55f),
                new Color(0.78f, 0.80f, 0.84f), -6);
            AddFieldRect(root.transform, "Note_2", new Vector2(1.5f, 2.25f), new Vector2(0.6f, 0.65f),
                new Color(0.80f, 0.62f, 0.60f), -6);

            // 책상. 오른쪽 아래에 서랍장이 붙는다.
            AddFieldRect(root.transform, "DeskTop", new Vector2(0.5f, -1.5f), new Vector2(4.6f, 0.22f), wood, -5);
            AddFieldRect(root.transform, "DeskLeg_L", new Vector2(-1.6f, -2.5f), new Vector2(0.2f, 1.8f), woodDark, -6);
            AddFieldRect(root.transform, "DeskDrawers", new Vector2(2.1f, -2.45f), new Vector2(1.4f, 1.7f), woodDark, -6);
            AddFieldRect(root.transform, "DeskDrawerLine", new Vector2(2.1f, -2.1f), new Vector2(1.2f, 0.06f), wood, -5);

            // 컴퓨터. 화면이 켜져 있다. 이 방에서 할 일이 여기 있다는 것을 멀리서도 알게 한다.
            AddFieldRect(root.transform, "MonitorStand", new Vector2(0.5f, -1.18f), new Vector2(0.2f, 0.45f),
                new Color(0.16f, 0.16f, 0.19f), -5);
            AddFieldRect(root.transform, "MonitorFrame", new Vector2(0.5f, -0.25f), new Vector2(2.3f, 1.5f),
                new Color(0.12f, 0.12f, 0.15f), -5);
            AddFieldRect(root.transform, "MonitorScreen", new Vector2(0.5f, -0.25f), new Vector2(2.08f, 1.28f),
                new Color(0.26f, 0.40f, 0.58f), -4);
            // 켜진 화면의 푸른 빛이 책상 위로 번진다.
            AddFieldSoft(root.transform, "MonitorGlow", new Vector2(0.5f, -0.4f), new Vector2(4.2f, 2.8f),
                new Color(0.45f, 0.65f, 1f, 0.16f), -6);
            AddFieldRect(root.transform, "MonitorTaskbar", new Vector2(0.5f, -0.82f), new Vector2(2.08f, 0.14f),
                new Color(0.14f, 0.18f, 0.26f), -3);
            AddFieldRect(root.transform, "Keyboard", new Vector2(0.3f, -1.34f), new Vector2(1.6f, 0.1f),
                new Color(0.20f, 0.20f, 0.24f), -4);
            AddFieldRect(root.transform, "Mug", new Vector2(2.3f, -1.2f), new Vector2(0.3f, 0.38f),
                new Color(0.78f, 0.76f, 0.72f), -4);

            // 의자. 옆에서 보면 등받이와 앉는 자리와 다리 하나다. 책상 앞으로 빼 둔 채다.
            var chairColor = new Color(0.22f, 0.24f, 0.30f);
            AddFieldRect(root.transform, "ChairBack", new Vector2(-1.25f, -1.55f), new Vector2(0.24f, 1.6f), chairColor, -3);
            AddFieldRect(root.transform, "ChairSeat", new Vector2(-0.7f, -2.35f), new Vector2(1.3f, 0.22f), chairColor, -3);
            AddFieldRect(root.transform, "ChairStem", new Vector2(-0.7f, -2.85f), new Vector2(0.14f, 0.8f),
                new Color(0.16f, 0.16f, 0.19f), -3);
            AddFieldRect(root.transform, "ChairBase", new Vector2(-0.7f, -3.28f), new Vector2(1.1f, 0.12f),
                new Color(0.16f, 0.16f, 0.19f), -3);

            // --- 오른쪽: 살림 ---
            // 책장. 선반 넷에 크기가 제각각인 책과 파일철이 꽂혀 있다.
            var shelf = new GameObject("Bookshelf");
            shelf.transform.SetParent(root.transform, false);
            shelf.transform.localPosition = new Vector3(6.8f, 0f, 0f);
            AddFieldRect(shelf.transform, "Frame", new Vector2(0f, -0.7f), new Vector2(3.2f, 5.4f), woodDark, -7);
            AddFieldRect(shelf.transform, "Back", new Vector2(0f, -0.7f), new Vector2(2.9f, 5.1f),
                new Color(0.17f, 0.13f, 0.11f), -6);

            var bookColors = new[]
            {
                new Color(0.52f, 0.26f, 0.24f), new Color(0.28f, 0.36f, 0.48f), new Color(0.62f, 0.56f, 0.40f),
                new Color(0.30f, 0.42f, 0.32f), new Color(0.46f, 0.40f, 0.52f), new Color(0.70f, 0.66f, 0.60f),
            };
            for (int row = 0; row < 4; row++)
            {
                float bottom = -3.2f + row * 1.25f;
                AddFieldRect(shelf.transform, "Board_" + row, new Vector2(0f, bottom - 0.05f), new Vector2(2.9f, 0.1f), wood, -5);

                // 한 칸에 책을 여러 권. 줄마다 빈 곳을 달리 둔다. 가지런하기만 하면 그림처럼 보인다.
                float x = -1.3f;
                for (int i = 0; x < 1.2f; i++)
                {
                    float w = 0.2f + ((row * 3 + i * 5) % 4) * 0.06f;
                    float h = 0.7f + ((row * 7 + i * 3) % 4) * 0.1f;
                    if ((row + i) % 5 == 4) { x += 0.35f; continue; }

                    AddFieldRect(shelf.transform, "Book_" + row + "_" + i, new Vector2(x + w * 0.5f, bottom + h * 0.5f),
                        new Vector2(w, h), bookColors[(row * 2 + i) % bookColors.Length], -4);
                    x += w + 0.04f;
                }
            }
            AddFieldRect(shelf.transform, "Top", new Vector2(0f, 2.02f), new Vector2(3.3f, 0.12f), wood, -5);

            // 벽시계. 둥근 판에 바늘 둘.
            var clock = new GameObject("WallClock");
            clock.transform.SetParent(root.transform, false);
            clock.transform.localPosition = new Vector3(9.9f, 1.8f, 0f);
            AddFieldCircle(clock.transform, "Rim", Vector2.zero, 1.3f, woodDark, -6);
            AddFieldCircle(clock.transform, "Face", Vector2.zero, 1.1f, new Color(0.88f, 0.86f, 0.80f), -5);
            var hand = new Color(0.14f, 0.13f, 0.14f);
            AddFieldRect(clock.transform, "HourHand", new Vector2(0f, 0.17f), new Vector2(0.08f, 0.34f), hand, -4);
            var minuteHand = AddFieldRect(clock.transform, "MinuteHand", new Vector2(0.14f, 0.1f), new Vector2(0.06f, 0.46f), hand, -4);
            minuteHand.transform.localRotation = Quaternion.Euler(0f, 0f, -60f);

            // 화분 하나. 방이 사람 사는 곳으로 보이는 데는 이런 것이 한몫한다.
            AddFieldRect(root.transform, "PlantPot", new Vector2(9.9f, -3.02f), new Vector2(0.75f, 0.75f),
                new Color(0.46f, 0.30f, 0.24f), -5);
            AddFieldCircle(root.transform, "PlantLeaves", new Vector2(9.9f, -2.2f), 1.2f,
                new Color(0.26f, 0.40f, 0.28f), -6);

            // 방문. 오른쪽 끝. 걸어서 닿는 자리(12)보다 조금 바깥이지만 거리 안에 든다.
            var door = new GameObject("Door");
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = new Vector3(12.8f, 0f, 0f);
            AddFieldRect(door.transform, "Frame", new Vector2(0f, -0.65f), new Vector2(2.7f, 5.5f), trim, -7);

            // 닫힌 문. 문짝과 문고리.
            var doorClosed = new GameObject("Closed");
            doorClosed.transform.SetParent(door.transform, false);
            AddFieldRect(doorClosed.transform, "Panel", new Vector2(0f, -0.78f), new Vector2(2.3f, 5.2f), wood, -6);
            AddFieldRect(doorClosed.transform, "PanelInset_T", new Vector2(0f, 0.6f), new Vector2(1.6f, 1.6f), woodDark, -5);
            AddFieldRect(doorClosed.transform, "PanelInset_B", new Vector2(0f, -1.9f), new Vector2(1.6f, 2.2f), woodDark, -5);
            AddFieldCircle(doorClosed.transform, "Knob", new Vector2(-0.85f, -1.0f), 0.22f, new Color(0.78f, 0.66f, 0.40f), -4);

            // 열린 문. 문간 너머는 불 꺼진 복도다. 문짝은 방 안쪽으로 젖혀져 모서리만 보인다.
            var doorOpen = new GameObject("Open");
            doorOpen.transform.SetParent(door.transform, false);
            AddFieldRect(doorOpen.transform, "Hallway", new Vector2(0f, -0.78f), new Vector2(2.3f, 5.2f),
                new Color(0.05f, 0.05f, 0.07f), -6);
            AddFieldRect(doorOpen.transform, "HallwayFloor", new Vector2(0f, -3.2f), new Vector2(2.3f, 0.36f),
                new Color(0.10f, 0.09f, 0.10f), -5);
            AddFieldRect(doorOpen.transform, "Leaf", new Vector2(-1.3f, -0.78f), new Vector2(0.3f, 5.2f), wood, -2);
            doorOpen.SetActive(false);

            // 방 조명 스위치. 방문 옆 벽, 손 닿는 높이에 붙어 있다.
            AddFieldRect(root.transform, "SwitchPlate", new Vector2(11.1f, -0.35f), new Vector2(0.36f, 0.56f),
                new Color(0.86f, 0.84f, 0.80f), -5);
            // 토글이 오르내리는 홈. 토글은 켜면 위, 끄면 아래에 선다(RoomNight).
            AddFieldRect(root.transform, "SwitchSlot", new Vector2(11.1f, -0.35f), new Vector2(0.16f, 0.36f),
                new Color(0.52f, 0.50f, 0.48f), -4);
            var switchToggle = AddFieldRect(root.transform, "SwitchToggle", new Vector2(11.1f, -0.27f), new Vector2(0.12f, 0.18f),
                new Color(0.97f, 0.96f, 0.93f), -3);

            // 누운 몸 위로 덮던 이불은 뺐다. 누운 그림이 몸을 다 보여 주는 편이 낫다.

            // 조명. 방 전체를 비추는 전역 조명 하나와, 불을 끄면 창으로 들어오는 달빛.
            // 방 뿌리 아래에 두므로 숙소가 보일 때만 켜진다. 다른 현장의 밝기는 그대로다.
            var roomLightGo = new GameObject("RoomLight");
            roomLightGo.transform.SetParent(root.transform, false);
            var roomLight = roomLightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            roomLight.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            roomLight.intensity = 1f;
            roomLight.color = Color.white;

            var moonGo = new GameObject("MoonLight");
            moonGo.transform.SetParent(root.transform, false);
            moonGo.transform.localPosition = new Vector3(-9.2f, 1.2f, 0f);
            var moon = moonGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            moon.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            moon.color = new Color(0.62f, 0.72f, 1f);
            moon.intensity = 0.9f;
            moon.pointLightInnerRadius = 1.2f;
            moon.pointLightOuterRadius = 7.5f;
            moonGo.SetActive(false);

            // --- 살펴볼 수 있는 것 ---
            var computer = BuildPoint(root.transform, "InvestigationPoint_RoomComputer", new Vector2(0.5f, -0.4f),
                new Vector2(2.6f, 2.2f), new Color(0.40f, 0.56f, 0.72f), "field.room.computer", "field.room.computer.result", null);
            var bedPoint = BuildPoint(root.transform, "InvestigationPoint_RoomBed", new Vector2(-9.2f, -2.4f),
                new Vector2(6.4f, 1.8f), new Color(0.46f, 0.50f, 0.62f), "field.room.bed", "field.room.bed.result", null);
            var shelfPoint = BuildPoint(root.transform, "InvestigationPoint_RoomBookshelf", new Vector2(6.8f, -0.7f),
                new Vector2(3.2f, 5.4f), new Color(0.52f, 0.42f, 0.32f), "field.room.bookshelf", "field.room.bookshelf.result", null);
            var clockPoint = BuildPoint(root.transform, "InvestigationPoint_RoomClock", new Vector2(9.9f, 1.8f),
                new Vector2(1.5f, 1.5f), new Color(0.80f, 0.76f, 0.62f), "field.room.clock", "field.room.clock.result", null);
            var doorPoint = BuildPoint(root.transform, "InvestigationPoint_RoomDoor", new Vector2(12.8f, -0.7f),
                new Vector2(2.7f, 5.5f), new Color(0.52f, 0.40f, 0.30f), "field.room.door", "field.room.door.result", null);

            ConfigureRoomPoint(computer, "point_room_computer", "ui.field.prompt_use");
            ConfigureRoomPoint(bedPoint, "point_room_bed", "ui.field.prompt_look");
            ConfigureRoomPoint(shelfPoint, "point_room_bookshelf", "ui.field.prompt_look");
            ConfigureRoomPoint(clockPoint, "point_room_clock", "ui.field.prompt_look");
            ConfigureRoomPoint(doorPoint, "point_room_door", "ui.field.prompt_look");

            // 방에 들어선 두 사람. 방문 바로 안쪽에서 방을 바라보고 선다. 밖에서 막 들어온 것이다.
            // 한영이 한 걸음 앞서 방 안쪽에 있다. 차지한은 문 앞이라 곁에 선 것이 방문이다.
            BuildFieldActors(root.transform, -3.86f, 11.6f, 2.4f, -12f, 12f, faceLeft: true);

            // 조명 스위치. 방문 옆 벽의 그것이다. 누르면 불이 켜지고 꺼진다.
            var switchPoint = BuildPoint(root.transform, "InvestigationPoint_RoomSwitch", new Vector2(11.1f, -0.35f),
                new Vector2(0.9f, 1.1f), new Color(0.86f, 0.84f, 0.80f), "field.room.switch", string.Empty, null);
            ConfigureRoomPoint(switchPoint, "point_room_switch", "ui.field.prompt_press");

            // 곁에 서면 물건 그림 그대로 밝아진다. 네모 칸이 물건 옆으로 삐져나오지 않는다.
            SetHighlightTargets(computer, root.transform, "MonitorStand", "MonitorFrame", "MonitorScreen", "MonitorTaskbar", "Keyboard");
            SetHighlightTargets(bedPoint, root.transform, "Bed");
            SetHighlightTargets(shelfPoint, root.transform, "Bookshelf");
            SetHighlightTargets(clockPoint, root.transform, "WallClock");
            SetHighlightTargets(doorPoint, root.transform, "Door");
            SetHighlightTargets(switchPoint, root.transform, "SwitchPlate", "SwitchSlot", "SwitchToggle");

            // 방의 밤. 조명과 문과 두 사람의 움직임을 한데 쥔다.
            var lead = root.transform.Find("Actor_Chajihan");
            var mate = root.transform.Find("Actor_Hanyoung");
            var night = root.AddComponent<RoomNight>();
            var nso = new SerializedObject(night);
            nso.Update();
            nso.FindProperty("_chajihan").objectReferenceValue = lead != null ? lead.GetComponent<FieldWalker>() : null;
            nso.FindProperty("_hanyoung").objectReferenceValue = mate != null ? mate.GetComponent<FieldFollower>() : null;
            nso.FindProperty("_doorClosed").objectReferenceValue = doorClosed;
            nso.FindProperty("_doorOpen").objectReferenceValue = doorOpen;
            nso.FindProperty("_roomLight").objectReferenceValue = roomLight;
            nso.FindProperty("_moonLight").objectReferenceValue = moon;
            nso.FindProperty("_coatHung").objectReferenceValue = root.transform.Find("CoatStand/CoatHung")?.gameObject;
            nso.FindProperty("_switchToggle").objectReferenceValue = switchToggle.transform;

            // 잠들 때 화면 위아래에서 닫혀 오는 눈꺼풀. 화면 맨 위에 덮이도록 따로 캔버스를 둔다.
            var lids = new GameObject("SleepLids", typeof(RectTransform));
            lids.transform.SetParent(root.transform, false);
            var lidCanvas = lids.AddComponent<Canvas>();
            lidCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lidCanvas.sortingOrder = 900;   // 대사 띠와 휴대폰까지 덮는다

            // 눈 모양으로 뚫린 판. 그림은 RoomNight 가 처음에 그린다. 위아래로 납작해지며 감긴다.
            var eyeMask = CreatePanel(lids.transform, "EyeMask", Color.white);
            var eyeMaskRt = (RectTransform)eyeMask.transform;
            eyeMaskRt.anchorMin = new Vector2(0.5f, 0.5f);
            eyeMaskRt.anchorMax = new Vector2(0.5f, 0.5f);
            eyeMaskRt.pivot = new Vector2(0.5f, 0.5f);
            eyeMaskRt.anchoredPosition = Vector2.zero;
            eyeMask.GetComponent<Image>().raycastTarget = false;

            // 판이 납작해지면 그 위아래가 비므로 검은 판을 붙여 덮는다.
            foreach (bool top in new[] { true, false })
            {
                var fill = CreatePanel(eyeMask.transform, top ? "FillTop" : "FillBottom", Color.black);
                var fillRt = (RectTransform)fill.transform;
                fillRt.anchorMin = new Vector2(0f, top ? 1f : 0f);
                fillRt.anchorMax = new Vector2(1f, top ? 1f : 0f);
                fillRt.pivot = new Vector2(0.5f, top ? 0f : 1f);
                fillRt.anchoredPosition = Vector2.zero;
                fillRt.sizeDelta = new Vector2(0f, 4000f);
                fill.GetComponent<Image>().raycastTarget = false;
            }
            eyeMask.SetActive(false);

            var lidFade = CreatePanel(lids.transform, "LidFade", Color.black);
            StretchFull(lidFade);
            lidFade.GetComponent<Image>().raycastTarget = false;
            var lidFadeGroup = lidFade.AddComponent<CanvasGroup>();
            lidFadeGroup.alpha = 0f;
            lidFadeGroup.blocksRaycasts = false;

            nso.FindProperty("_eyeMask").objectReferenceValue = eyeMaskRt;
            nso.FindProperty("_lidFade").objectReferenceValue = lidFadeGroup;
            nso.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 곁에 섰을 때 네모 칸 대신 이 물건 그림들을 밝힌다. 밝아지는 범위가 물건 모양에 꼭 맞는다.
        /// names 는 root 아래 자식 이름이다. 묶음이면 그 안의 그림을 모두 넣는다(꺼진 것까지. 문은 열리고 닫힌다).
        /// </summary>
        private static void SetHighlightTargets(GameObject pointGo, Transform root, params string[] names)
        {
            var list = new List<SpriteRenderer>();
            foreach (var n in names)
            {
                var t = root.Find(n);
                if (t == null) { Debug.LogWarning("[SliceSceneBuilder] 밝힐 그림이 없다: " + n); continue; }
                list.AddRange(t.GetComponentsInChildren<SpriteRenderer>(true));
            }
            AssignHighlightTargets(pointGo, list);
        }

        /// <summary>
        /// 이름을 하나하나 대지 않고, 지점의 네모 칸 안에 들어앉은 그림을 밝힐 그림으로 삼는다.
        /// 칸보다 조금 삐져나온 것까지는 넣고, 벽이나 바닥처럼 칸보다 훨씬 큰 것은 뺀다.
        /// 걷는 두 사람과 다른 조사 지점의 칸은 넣지 않는다. 하나도 못 찾으면 네모 칸을 그대로 쓴다.
        /// </summary>
        private static void FitHighlightToArt(GameObject pointGo, Transform searchRoot)
        {
            var box = pointGo.GetComponent<SpriteRenderer>();
            if (box == null) return;

            var area = box.bounds;
            var loose = new Bounds(area.center, area.size * 1.3f);

            var list = new List<SpriteRenderer>();
            foreach (var sr in searchRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == box || sr.sprite == null) continue;
                if (sr.GetComponent<InvestigationPoint>() != null) continue;
                if (sr.GetComponentInParent<FieldWalker>(true) != null || sr.GetComponentInParent<FieldFollower>(true) != null) continue;
                if (sr.color.a < 0.5f) continue;   // 번지는 빛과 그늘은 밝히지 않는다

                var b = sr.bounds;
                if (!area.Contains(new Vector3(b.center.x, b.center.y, area.center.z))) continue;
                if (!loose.Contains(new Vector3(b.min.x, b.min.y, area.center.z)) || !loose.Contains(new Vector3(b.max.x, b.max.y, area.center.z))) continue;
                list.Add(sr);
            }
            AssignHighlightTargets(pointGo, list);
        }

        private static void AssignHighlightTargets(GameObject pointGo, List<SpriteRenderer> list)
        {
            var point = pointGo.GetComponent<InvestigationPoint>();
            if (point == null || list.Count == 0) return;

            var so = new SerializedObject(point);
            so.Update();
            var prop = so.FindProperty("_highlightTargets");
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>숙소의 물건 하나. 조사 방법도 해금 조건도 없고, 말풍선 글만 제 것을 쓴다.</summary>
        private static void ConfigureRoomPoint(GameObject pointGo, string pointId, string promptTextId)
        {
            var point = pointGo.GetComponent<InvestigationPoint>();
            if (point == null) return;

            var so = new SerializedObject(point);
            so.Update();
            so.FindProperty("_pointId").stringValue = pointId;
            so.FindProperty("_promptTextId").stringValue = promptTextId;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private const string FieldCirclePath = "Assets/_Project/Art/Objects/field_circle.png";

        /// <summary>현장 동그라미 그림의 한 변(픽셀). 방 하나를 가득 채울 만큼 키워도 가장자리가 뭉개지지 않는 크기다.</summary>
        private const int FieldCircleSize = 512;

        /// <summary>
        /// 현장에 쓰는 동그라미 그림. 없으면 여기서 그려서 저장한다.
        ///
        /// 유니티에 딸린 동그라미(Knob)는 16픽셀짜리라 시계판만 하게 늘리면 가장자리가 흐려진다.
        /// 그래서 넉넉한 크기로 한 장 그려 두고 그것을 늘려 쓴다. 가장자리는 1픽셀 폭으로만 부드럽게 한다.
        /// </summary>
        private static Sprite FieldCircleSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FieldCirclePath);
            if (sprite != null) return sprite;

            const int n = FieldCircleSize;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color32[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float edge = r - Mathf.Sqrt(dx * dx + dy * dy);   // 안쪽이면 양수
                    byte a = (byte)(Mathf.Clamp01(edge + 0.5f) * 255f);
                    pixels[y * n + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            System.IO.File.WriteAllBytes(FieldCirclePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(FieldCirclePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(FieldCirclePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = n;          // 한 장이 월드 1단위다
            importer.mipmapEnabled = true;             // 작게 줄였을 때도 계단이 지지 않게
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(FieldCirclePath);
        }

        // ------------------------------------------------------------- 현장의 빛과 그림자

        private const string FieldSoftPath = "Assets/_Project/Art/Objects/field_soft.png";
        private const string FieldGradientPath = "Assets/_Project/Art/Objects/field_gradient.png";

        /// <summary>
        /// 가운데가 진하고 가장자리로 갈수록 옅어지는 흐린 동그라미. 없으면 그려서 저장한다.
        /// 발밑 그림자, 가구 밑 그림자, 전등과 모니터의 빛번짐에 쓴다. 색은 쓰는 쪽이 입힌다.
        /// </summary>
        private static Sprite FieldSoftSprite()
        {
            return LoadOrPaint(FieldSoftPath, 256, 256, (u, v) =>
            {
                float dx = u - 0.5f, dy = v - 0.5f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) * 2f);
                float a = 1f - d;
                return a * a * (3f - 2f * a);   // 부드럽게 줄어든다
            });
        }

        /// <summary>
        /// 위가 진하고 아래로 갈수록 사라지는 띠. 없으면 그려서 저장한다.
        /// 천장 아래의 그늘, 벽과 바닥이 만나는 곳의 어둠, 유리의 반사, 화면 가장자리의 어둠에 쓴다.
        /// </summary>
        private static Sprite FieldGradientSprite()
        {
            return LoadOrPaint(FieldGradientPath, 8, 256, (u, v) =>
            {
                float t = v;               // 아래(0)에서 위(1)로
                return t * t;
            });
        }

        /// <summary>
        /// 흰 그림 한 장을 알파만 달리해 그려 저장하고 Sprite 로 들여온다. 이미 있으면 그것을 쓴다.
        /// 한 장이 월드 1단위가 되도록 들여온다. 크기는 쓰는 쪽이 늘려서 맞춘다.
        /// </summary>
        private static Sprite LoadOrPaint(string path, int width, int height, System.Func<float, float, float> alpha)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float a = Mathf.Clamp01(alpha((x + 0.5f) / width, (y + 0.5f) / height));
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = Mathf.Max(width, height);
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>흐린 동그라미 하나를 그 크기로 깐다. 그림자면 검은색, 빛번짐이면 빛의 색을 준다.</summary>
        private static GameObject AddFieldSoft(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, int order)
        {
            return AddStretched(parent, name, FieldSoftSprite(), position, size, color, order, 0f);
        }

        /// <summary>
        /// 한쪽이 진하고 반대쪽으로 사라지는 띠를 깐다. angle 0 이면 위가 진하다.
        /// 180 이면 아래가, 90 이면 왼쪽이, -90 이면 오른쪽이 진하다.
        /// </summary>
        private static GameObject AddFieldGradient(Transform parent, string name, Vector2 position, Vector2 size,
            Color color, int order, float angle = 0f)
        {
            return AddStretched(parent, name, FieldGradientSprite(), position, size, color, order, angle);
        }

        private static GameObject AddStretched(Transform parent, string name, Sprite sprite, Vector2 position,
            Vector2 size, Color color, int order, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;

            // 옆으로 눕힌 띠는 가로와 세로가 바뀐다. 돌린 뒤에 size 가 되도록 맞춘다.
            bool sideways = Mathf.Abs(Mathf.Abs(angle) - 90f) < 1f;
            var want = sideways ? new Vector2(size.y, size.x) : size;
            var bounds = sprite != null ? sprite.bounds.size : Vector3.one;
            go.transform.localScale = new Vector3(want.x / Mathf.Max(0.0001f, bounds.x), want.y / Mathf.Max(0.0001f, bounds.y), 1f);
            return go;
        }

        /// <summary>
        /// 현장마다 까는 공통의 분위기. 화면 위와 양옆 가장자리를 살짝 어둡게 한다.
        /// 장면이 판때기처럼 끝나지 않고 가운데로 눈이 모인다. 사람과 물건보다 앞, 말풍선보다 뒤에 그린다.
        /// </summary>
        private static void AddFieldVignette(Transform parent)
        {
            var dark = new Color(0f, 0f, 0f, 0.42f);
            AddFieldGradient(parent, "Vignette_Top", new Vector2(0f, 2.9f), new Vector2(44f, 2.4f), dark, 20);
            AddFieldGradient(parent, "Vignette_Bottom", new Vector2(0f, -4.8f), new Vector2(44f, 1.6f),
                new Color(0f, 0f, 0f, 0.30f), 20, 180f);
            AddFieldGradient(parent, "Vignette_Left", new Vector2(-13.2f, 0f), new Vector2(3.2f, 12f), dark, 20, 90f);
            AddFieldGradient(parent, "Vignette_Right", new Vector2(13.2f, 0f), new Vector2(3.2f, 12f), dark, 20, -90f);
        }

        /// <summary>
        /// 현장 안에 적힌 글자. 역 이름표처럼 장면의 일부인 글에 쓴다.
        /// 말풍선과 같은 3D TextMeshPro 이고, 글은 번역 표에서 가져온다(LocalizedText).
        /// </summary>
        private static TextMeshPro AddFieldWorldText(Transform parent, string name, Vector2 position, Vector2 size,
            string textId, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var text = go.AddComponent<TextMeshPro>();
            text.font = LoadFont(UIFontWeight.SemiBold);
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = 2f;
            text.fontSizeMax = 6f;
            text.rectTransform.sizeDelta = size;

            var mesh = go.GetComponent<MeshRenderer>();
            if (mesh != null) mesh.sortingOrder = order;

            var localized = go.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.Update();
            so.FindProperty("_textId").stringValue = textId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return text;
        }

        /// <summary>현장 배경에 까는 동그라미 하나. 시계판이나 문고리처럼 둥근 것에 쓴다.</summary>
        private static GameObject AddFieldCircle(Transform parent, string name, Vector2 position, float diameter,
            Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FieldCircleSprite();
            sr.color = color;
            sr.sortingOrder = order;

            // 동그라미 그림의 원래 크기에 맞춰 늘린다. 그림 크기가 바뀌어도 지름은 그대로다.
            var bounds = sr.sprite != null ? sr.sprite.bounds.size : Vector3.one;
            go.transform.localScale = new Vector3(diameter / Mathf.Max(0.0001f, bounds.x),
                diameter / Mathf.Max(0.0001f, bounds.y), 1f);
            return go;
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

            // 앉는 자리 앞 모서리가 조명을 받는다. 판이 아니라 쿠션으로 보인다.
            AddFieldRect(go.transform, "SeatEdge", new Vector2(0f, -2.18f), new Vector2(width, 0.06f),
                Color.Lerp(fabric, Color.white, 0.3f), -4);
            AddFieldGradient(go.transform, "BackShade", new Vector2(0f, -1.95f), new Vector2(width, 0.6f),
                new Color(0f, 0f, 0f, 0.18f), -5, 180f);
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

            // 캄캄한 유리에 비친 객실 조명. 위쪽이 옅게 밝다.
            AddFieldGradient(go.transform, "Sheen", new Vector2(0f, 0.55f), new Vector2(width, 1.2f),
                new Color(1f, 1f, 1f, 0.07f), -5);
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

            var box = CreatePanel(go.transform, "Box", Color.white);
            var boxSprite = LoadBackdropSprite(UIIconPath + "app_" + id + ".png");
            if (boxSprite != null)
            {
                box.GetComponent<Image>().sprite = boxSprite;
                box.GetComponent<Image>().preserveAspect = true;
            }
            else box.GetComponent<Image>().color = new Color(0.24f, 0.26f, 0.34f, 1f);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 1f);
            boxRt.anchorMax = new Vector2(0.5f, 1f);
            boxRt.pivot = new Vector2(0.5f, 1f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(size, size);
            box.GetComponent<Image>().raycastTarget = true;   // 누르는 자리는 아이콘 그림이다
            if (boxSprite != null) AddIconHighlight(box.transform, button);
            else button.targetGraphic = box.GetComponent<Image>();

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

            // 가장자리를 살짝 어둡게. 어느 현장이든 같은 틀로 보이게 한다.
            AddFieldVignette(parent);
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

        /// <summary>지하철 안에서만 쓰는 인물 크기. 객실 배경에 비해 사람이 작아 보이므로 더 키운다.</summary>
        private const float TrainActorScale = 1.9f;

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
            float minX, float maxX, bool faceLeft = false, float scale = ActorScale)
        {
            var lead = BuildFieldActor(parent, "Actor_Chajihan", new Vector2(leadX, groundY), "Chajihan");
            // 왼쪽을 보고 서야 하면 처음부터 뒤집어 세운다. 걷는 쪽이 이 방향을 처음 방향으로 기억한다.
            float side = faceLeft ? -1f : 1f;
            lead.transform.localScale = new Vector3(scale * side, scale, scale);

            var walker = lead.AddComponent<FieldWalker>();
            var wso = new SerializedObject(walker);
            wso.Update();
            wso.FindProperty("_minX").floatValue = minX;
            wso.FindProperty("_maxX").floatValue = maxX;
            wso.ApplyModifiedPropertiesWithoutUndo();

            var mate = BuildFieldActor(parent, "Actor_Hanyoung", new Vector2(leadX - gap, groundY), "Hanyoung");
            mate.transform.localScale = new Vector3(scale * side, scale, scale);

            // 인물을 키운 만큼 한 걸음도 길다. 걷기 한 바퀴에 걷는 거리를 크기에 맞춰 늘려 발이 미끄러지지 않게 한다.
            foreach (var actor in new[] { lead, mate })
            {
                var aso = new SerializedObject(actor.GetComponent<FieldSpriteAnimator>());
                aso.Update();
                aso.FindProperty("_cycleDistance").floatValue = 3.6f * scale / 1.5f;
                aso.ApplyModifiedPropertiesWithoutUndo();
            }

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
        /// 현장 인물 하나. 발이 닿는 자리를 기준으로 선다.
        ///
        /// 그림은 Art/Characters/{art}/Field 의 걷기 칸(walk_0 ...)이다. 오른쪽을 보고 그려져 있고, 왼쪽은 뒤집어 보인다.
        /// 키는 그림을 들여올 때 정한다(CharacterArtImporter). 칸 넘기기는 FieldSpriteAnimator 가 한다.
        /// </summary>
        private static GameObject BuildFieldActor(Transform parent, string name, Vector2 footPosition, string art)
        {
            const int Order = 3;   // 승강장 문짝과 안전선보다 앞이다

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = footPosition;

            // 발밑의 흐린 그림자. 바닥에 서 있는 것처럼 보이게 한다. 누우면 끈다(RoomNight).
            AddFieldSoft(go.transform, "Shadow", new Vector2(0f, 0.02f), new Vector2(1.0f, 0.24f),
                new Color(0f, 0f, 0f, 0.45f), Order - 1);

            // 걷는 그림. 발밑이 기준점이라 자식을 원점에 두면 바닥에 선다.
            var frames = LoadFieldFrames(art, "walk");
            if (frames.Count == 0) Debug.LogWarning("[SliceSceneBuilder] " + art + " 의 현장 걷기 그림이 없다.");

            var sprite = new GameObject("Sprite");
            sprite.transform.SetParent(go.transform, false);
            var sr = sprite.AddComponent<SpriteRenderer>();
            sr.sprite = frames.Count > 0 ? frames[0] : null;
            sr.sortingOrder = Order;

            var animator = go.AddComponent<FieldSpriteAnimator>();
            var aso = new SerializedObject(animator);
            aso.Update();
            aso.FindProperty("_renderer").objectReferenceValue = sr;
            // 걸을 때 몸이 오르내리는 폭. 두 사람 걷기 그림에 오르내림이 들어 있어 코드로는 더하지 않는다.
            aso.FindProperty("_stepBob").floatValue = 0f;
            var walk = aso.FindProperty("_walk");
            walk.arraySize = frames.Count;
            for (int i = 0; i < frames.Count; i++) walk.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            // 연출용 그림 묶음. 잠자리에 들며 코트를 벗고 거는 동작 같은 것이다. 있는 것만 넣는다.
            var clipNames = new[] { "coatoff", "coathang", "tie", "sit", "lie", "sleep" };
            var clips = aso.FindProperty("_clips");
            clips.arraySize = 0;
            foreach (var clipName in clipNames)
            {
                var list = LoadFieldFrames(art, clipName);
                if (list.Count == 0) continue;
                clips.arraySize++;
                var clip = clips.GetArrayElementAtIndex(clips.arraySize - 1);
                clip.FindPropertyRelative("name").stringValue = clipName;
                var clipFrames = clip.FindPropertyRelative("frames");
                clipFrames.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++) clipFrames.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }

            // 서 있는 그림과 뒷모습은 있는 인물만 넣는다. 없으면 걷기 칸으로 대신한다.
            foreach (var kind in new[] { "idle", "back" })
            {
                var list = LoadFieldFrames(art, kind);
                var prop = aso.FindProperty("_" + kind);
                prop.arraySize = list.Count;
                for (int i = 0; i < list.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            }
            aso.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        /// <summary>현장 그림 한 벌(kind_0, kind_1 ...)을 차례대로 읽는다. 번호가 끊기면 멈춘다.</summary>
        private static List<Sprite> LoadFieldFrames(string art, string kind)
        {
            var frames = new List<Sprite>();
            for (int i = 0; ; i++)
            {
                var frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/" + art + "/Field/" + kind + "_" + i + ".png");
                if (frame == null) break;
                frames.Add(frame);
            }
            return frames;
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
        /// <summary>타이틀 그림이 있는 곳. 최종 시안에서 뽑은 배경, 로고, 메뉴 글자, 붉은 띠다.</summary>
        private const string TitleArtPath = "Assets/_Project/UI/Sprites/Title/";

        /// <summary>
        /// 타이틀 화면을 최종 시안대로 꾸민다.
        ///
        /// 시안 그림(1672x941)의 자리를 1920x1080 기준으로 옮겨 놓는다. 왼쪽 위 모서리에서 잰 자리다.
        /// 로고 아래에 영문 한 줄, 그 아래에 메뉴 여섯 줄이 서고 줄 사이에 가는 선이 있다.
        /// 메뉴 줄에 마우스를 올리면 그 줄 뒤에 붉은 띠가 깔린다(TitleMenuItem).
        ///
        /// 예전의 글자 제목과 가로 버튼 줄은 숨긴다. 진행 담당이 그 글자에 제목을 넣어도 보이지 않는다.
        /// 돌려주는 것은 메뉴 단추 여섯 개다. 순서는 이어하기, 새 게임, 불러오기, 설정, 크레딧, 종료.
        /// </summary>
        private static Button[] StyleTitleScreen(TextPanelScreen title, Transform buttonRow)
        {
            const float K = 1920f / 1672f;   // 시안 그림 한 점이 화면에서 차지하는 크기

            // --- 배경 ---
            var image = title.GetComponent<Image>();
            if (image != null)
            {
                var bg = LoadBackdropSprite(TitleArtPath + "title_bg.png");
                image.sprite = bg;
                image.color = bg != null ? Color.white : new Color(0.03f, 0.035f, 0.06f, 1f);
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }

            // 예전 제목, 본문, 안내 글과 가로 버튼 줄은 쓰지 않는다.
            foreach (var oldName in new[] { "Text_Title", "Text_Body", "Text_Footer" })
            {
                var old = title.transform.Find(oldName);
                if (old != null) old.gameObject.SetActive(false);
            }
            buttonRow.gameObject.SetActive(false);

            // 왼쪽을 어둡게 눌러 로고와 메뉴가 잘 읽히게 한다. 시안에서도 왼쪽이 더 어둡다.
            var shade = CreatePanel(title.transform, "Shade", new Color(1f, 1f, 1f, 0.78f));
            var shadeImage = shade.GetComponent<Image>();
            shadeImage.sprite = LoadBackdropSprite(TitleArtPath + "title_shade.png");
            shadeImage.raycastTarget = false;
            var shadeRt = (RectTransform)shade.transform;
            shadeRt.anchorMin = new Vector2(0f, 0f);
            shadeRt.anchorMax = new Vector2(0.62f, 1f);
            shadeRt.offsetMin = Vector2.zero;
            shadeRt.offsetMax = Vector2.zero;

            // --- 로고 ---
            var logo = AddTitleImage(title.transform, "Logo", "title_logo.png",
                new Vector2(34f, 82f) * K, new Vector2(1642f, 855f) * (0.44f * K));
            logo.color = new Color(0.92f, 0.9f, 0.9f, 1f);   // 시안처럼 배경에 조금 묻히게

            // 로고 아래 영문 한 줄. Censorship 만 붉다.
            var sub = AddText(title.transform, "Subtitle", 19f, UIFontWeight.Regular, new Color(0.9f, 0.9f, 0.92f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            PlaceTopLeft(sub.rectTransform, new Vector2(175f, 366f) * K, new Vector2(530f, 28f) * K);
            sub.characterSpacing = 42f;
            sub.richText = true;
            sub.raycastTarget = false;
            sub.text = "Urban Legend <color=#D8312C>Censorship</color> Bureau";

            // --- 메뉴 ---
            // 줄 가운데 높이(시안 기준). 줄 사이는 59.
            float[] rowY = { 471f, 530f, 588f, 647f, 706f, 765f };
            var buttons = new Button[rowY.Length];
            for (int i = 0; i < rowY.Length; i++)
            {
                var row = new GameObject("Menu_" + i, typeof(RectTransform));
                row.transform.SetParent(title.transform, false);
                var rowRt = (RectTransform)row.transform;
                PlaceTopLeft(rowRt, new Vector2(220f, rowY[i] - 29f) * K, new Vector2(290f, 58f) * K);

                // 누르는 자리. 투명하지만 줄 전체가 눌린다.
                var hit = row.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
                hit.raycastTarget = true;
                var button = row.AddComponent<Button>();
                button.targetGraphic = hit;
                button.transition = Selectable.Transition.None;
                buttons[i] = button;

                // 마우스를 올리면 깔리는 붉은 띠. 시안보다 조금 어둡게 둔다.
                var bar = AddTitleImage(row.transform, "Highlight", "title_bar.png", Vector2.zero, Vector2.zero);
                var barRt = bar.rectTransform;
                barRt.anchorMin = new Vector2(0f, 0f);
                barRt.anchorMax = new Vector2(1f, 1f);
                barRt.offsetMin = new Vector2(-6f, -4f);
                barRt.offsetMax = new Vector2(10f, 4f);
                bar.color = new Color(0.62f, 0.5f, 0.5f, 0.9f);

                // 메뉴 글자. 시안에서 뽑은 그림이다.
                var label = AddTitleImage(row.transform, "Label", "title_menu_" + i + ".png", Vector2.zero, Vector2.zero);
                var labelRt = label.rectTransform;
                labelRt.anchorMin = new Vector2(0f, 0.5f);
                labelRt.anchorMax = new Vector2(0f, 0.5f);
                labelRt.pivot = new Vector2(0f, 0.5f);
                labelRt.anchoredPosition = new Vector2(16f * K, 0f);
                labelRt.sizeDelta = new Vector2(150f, 56f) * K;

                var item = row.AddComponent<TitleMenuItem>();
                var iso = new SerializedObject(item);
                iso.Update();
                iso.FindProperty("_highlight").objectReferenceValue = bar;
                iso.ApplyModifiedPropertiesWithoutUndo();

                // 줄 사이의 가는 선. 마지막 줄 아래에는 없다.
                if (i < rowY.Length - 1)
                {
                    var line = CreatePanel(title.transform, "MenuLine_" + i, new Color(1f, 1f, 1f, 0.16f));
                    line.GetComponent<Image>().raycastTarget = false;
                    PlaceTopLeft((RectTransform)line.transform, new Vector2(222f, rowY[i] + 29.5f) * K, new Vector2(280f * K, 1.5f));
                }
            }

            return buttons;
        }

        /// <summary>타이틀 그림 한 장을 놓는다. 자리와 크기는 화면 왼쪽 위에서 잰다.</summary>
        private static Image AddTitleImage(Transform parent, string name, string file, Vector2 topLeft, Vector2 size)
        {
            var go = CreatePanel(parent, name, Color.white);
            var image = go.GetComponent<Image>();
            image.sprite = LoadBackdropSprite(TitleArtPath + file);
            image.preserveAspect = false;
            image.raycastTarget = false;
            PlaceTopLeft((RectTransform)go.transform, topLeft, size);
            return image;
        }

        /// <summary>왼쪽 위 모서리를 기준으로 자리를 잡는다. 시안 그림의 좌표를 그대로 옮기기 좋다.</summary>
        private static void PlaceTopLeft(RectTransform rt, Vector2 topLeft, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rt.sizeDelta = size;
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

        /// <summary>뒤에 아무것도 없는 대화 화면에 까는 그림. 밤의 사무실.</summary>
        private const string DialogueBackdropPath = "Assets/_Project/Art/Environments/office_night.png";

        /// <summary>배경 그림을 한 장짜리 스프라이트로 들여온다. 화면을 다 덮으니 크기를 줄이지 않는다.</summary>
        private static Sprite LoadBackdropSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning("[SliceSceneBuilder] 배경 그림이 없다: " + path); return null; }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled || importer.maxTextureSize < 2048)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>현장 대사 띠(타입 2)의 높이. 화면 아래 1/3.</summary>
        private const float BandHeight = 1080f / 3f;
        private static readonly Color BandEdgeColor = new Color(0.30f, 0.30f, 0.36f, 1f);

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

            // 단독 대화 화면은 뒤가 비어 있다. 밤의 사무실 그림을 깐다. 인물보다 먼저 만들어 맨 뒤에 둔다.
            // 화면 비율이 달라도 빈틈이 생기지 않게 화면을 덮는 쪽으로 맞춘다(넘치는 가장자리는 잘린다).
            Image backdropForScreen = null;
            if (fullScreen)
            {
                var backdropSprite = LoadBackdropSprite(DialogueBackdropPath);
                if (backdropSprite != null)
                {
                    var backdrop = CreatePanel(go.transform, "Backdrop", Color.black);   // 처음에는 까맣다. 대화가 밝힌다(DialogueScreen.SetBackdropLevel)
                    var backdropImage = backdrop.GetComponent<Image>();
                    backdropImage.sprite = backdropSprite;
                    backdropImage.raycastTarget = false;
                    var fitter = backdrop.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fitter.aspectRatio = backdropSprite.rect.width / backdropSprite.rect.height;
                    backdropForScreen = backdropImage;
                }
            }

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

            // 이야기 건너뛰기(스킵) 단추. 처음 이야기 동안만 보인다(StorySkip). 대사 상자 안에는 두지 않는다.
            // 전신 대화는 화면 오른쪽 위, 상자형(넘기기 판이 상자 크기)은 상자 바깥 바로 위 오른쪽에 붙는다.
            var skipRt = AddSkipButton(advanceGo.transform, fullScreen ? new Vector2(-64.1f, -51.9f) : new Vector2(-40f, 14f));
            if (!fullScreen) skipRt.pivot = new Vector2(1f, 0f);

            // 인물 배치는 겹침 대화에서도 처음 튜토리얼과 똑같이 둔다.
            // 배경만 투명할 뿐 대화 자체는 같은 모습이어야 한다.
            var left = CreateCharacterImage(go.transform, "Char_Left", -520f, "placeholder_hanyoung");

            // 한영은 실제 그림이다. 허리 위가 크게 보이도록 키우고 아래로 내려 세운다. 에디터에서 직접 맞춘 값이다.
            // 혼자 설 때는 가운데로, 차지한이 나오면 왼쪽으로 가는데 높이는 이 값을 그대로 쓴다.
            var leftRt = left.rectTransform;
            leftRt.anchoredPosition = new Vector2(-520f, HanyoungArtY);
            leftRt.sizeDelta = HanyoungArtSize;
            var hanyoungArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Characters/Hanyoung/default.png");
            if (hanyoungArt != null) left.sprite = hanyoungArt;
            var right = CreateCharacterImage(go.transform, "Char_Right", 520f, "placeholder_chajihan");

            // 차지한도 실제 그림이다. 한영과 같은 캔버스로 정리했지만 코트 끝까지 한 키라 머리가 조금 크게 보인다.
            // 조금 줄이고, 대사 상자 위로 보이는 몸이 상자 오른쪽 끝(x 800)을 넘지 않게 안쪽으로 들인다.
            // 팔을 가장 멀리 뻗는 포즈(docs)를 기준으로 잰 자리다. 머리는 한영보다 살짝 높게 선다.
            var rightRt = right.rectTransform;
            rightRt.anchoredPosition = new Vector2(330f, HanyoungArtY + 70f);
            rightRt.sizeDelta = HanyoungArtSize * 0.95f;
            var chajihanArt = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/Characters/Chajihan/" + CharacterArt.ChajihanDefaultPose + ".png");
            if (chajihanArt != null) right.sprite = chajihanArt;

            var box = CreatePanel(go.transform, "Box", new Color(0.09f, 0.09f, 0.12f, 0.96f));
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = new Vector2(0f, -340f);
            boxRt.sizeDelta = new Vector2(1600f, 300f);

            // 타입 3(숙소에서 띠에 맞춘 상자)일 때만 켜는 위쪽 가는 선. 현장 대사 띠의 그것과 같다.
            var boxEdge = CreatePanel(box.transform, "Edge", BandEdgeColor);
            var boxEdgeRt = (RectTransform)boxEdge.transform;
            boxEdgeRt.anchorMin = new Vector2(0f, 1f);
            boxEdgeRt.anchorMax = new Vector2(1f, 1f);
            boxEdgeRt.pivot = new Vector2(0.5f, 1f);
            boxEdgeRt.anchoredPosition = Vector2.zero;
            boxEdgeRt.sizeDelta = new Vector2(0f, 2f);
            boxEdge.GetComponent<Image>().raycastTarget = false;
            AddCrisp(boxEdge, 2f);
            boxEdge.SetActive(false);

            // 상자 안쪽 여백을 기준으로 붙인다. 좌표를 손으로 계산하면 상자 밖으로 나간다.
            // 상자 높이가 달라져도 세 줄이 겹치지 않도록 높이에서 되짚어 계산한다.
            float boxH = boxRt.sizeDelta.y;
            const float nameH = 50f, hintH = 28f, pad = 20f;

            const float textLeft = 48f;
            const float textRight = 48f;

            var nameText = AddText(box.transform, "Name", 36f, UIFontWeight.Bold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(nameText.rectTransform, textLeft, textRight, pad, boxH - pad - nameH);

            var lineText = AddText(box.transform, "Line", 34f, UIFontWeight.Regular, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            StretchInside(lineText.rectTransform, textLeft, textRight, pad + nameH + 10f, pad + hintH + 8f);

            // 현장의 대사 띠와 같은 규칙을 쓴다. 두 곳의 대사가 같은 모습으로 보여야 한다.
            ConfigureBodyText(lineText, 34f);

            var hintText = AddText(box.transform, "Hint", 24f, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.BottomRight);
            StretchInside(hintText.rectTransform, textLeft, textRight, boxH - pad - hintH, pad - 6f);

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
            so.FindProperty("_box").objectReferenceValue = boxRt;
            so.FindProperty("_backdrop").objectReferenceValue = backdropForScreen;
            so.FindProperty("_boxEdge").objectReferenceValue = boxEdge;
            so.FindProperty("_advanceRect").objectReferenceValue = (RectTransform)advanceGo.transform;
            so.FindProperty("_advanceCoversScreen").boolValue = fullScreen;
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
        /// <summary>대화 화면의 한영 그림 크기. 에디터에서 보며 맞춘 값이다.</summary>
        private static readonly Vector2 HanyoungArtSize = new Vector2(1646.5f, 2052.78f);

        /// <summary>대화 화면의 한영 그림 높이(가운데 기준). 아래로 내려 허리 위만 보이게 한다.</summary>
        private const float HanyoungArtY = -602f;

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

        /// <summary>앱 아이콘, 배경화면, 작업 표시줄 그림이 있는 곳.</summary>
        private const string UIIconPath = "Assets/_Project/Art/UI/Icons/";

        /// <summary>부모를 빈틈없이 덮는 그림. 비율이 달라 넘치는 가장자리는 잘린다. 그림이 없으면 null.</summary>
        private static GameObject AddCoverImage(Transform parent, string name, string path)
        {
            var sprite = LoadBackdropSprite(path);
            if (sprite == null) return null;

            var go = CreatePanel(parent, name, Color.white);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var fitter = go.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
            go.transform.SetAsFirstSibling();
            return go;
        }

        /// <summary>
        /// 작업 표시줄 위의 그림 하나. x 가 0 이상이면 왼쪽 끝에서, 음수면 오른쪽 끝에서 잰 가운데 자리다.
        /// 눌리지 않는 장식이다.
        /// </summary>
        private static void AddTaskbarImage(Transform parent, string name, string file, float x, float size, float alpha = 1f)
        {
            var sprite = LoadBackdropSprite(UIIconPath + file);
            if (sprite == null) return;

            var go = CreatePanel(parent, name, new Color(1f, 1f, 1f, alpha));
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            float side = x < 0f ? 1f : 0f;
            rt.anchorMin = new Vector2(side, 0.5f);
            rt.anchorMax = new Vector2(side, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// 아이콘 위에 겹치는 흰 판. 아이콘과 같은 둥근 네모 모양이라 밝아지는 범위가 아이콘에 꼭 맞는다.
        /// 평소에는 투명하고, 마우스를 올리면 옅게, 누르면 조금 더 밝아진다.
        /// </summary>
        private static void AddIconHighlight(Transform icon, Button button)
        {
            var mask = LoadBackdropSprite(UIIconPath + "app_mask.png");
            var go = CreatePanel(icon, "Highlight", Color.white);
            StretchFull(go);
            var image = go.GetComponent<Image>();
            image.sprite = mask;
            image.preserveAspect = true;
            image.raycastTarget = false;

            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.22f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.36f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        /// <summary>휴대폰 상태 줄의 작은 그림. 왼쪽 끝에서 x 만큼 떨어진 곳에 왼쪽을 맞춘다.</summary>
        private static void AddStatusIcon(Transform parent, string name, string file, float x, float size)
        {
            var sprite = LoadBackdropSprite(UIIconPath + file);
            if (sprite == null) return;

            var go = CreatePanel(parent, name, new Color(1f, 1f, 1f, 0.85f));
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(size, size);
        }

        private static DesktopScreen BuildDesktopScreen(string name)
        {
            var go = CreatePanel(null, name, new Color(0.10f, 0.13f, 0.20f, 1f));
            StretchFull(go);

            // 배경화면. 검열국 문장이 옅게 깔린 밤빛 그림이다. 화면 비율이 달라도 빈틈이 없게 덮는다.
            AddCoverImage(go.transform, "Wallpaper", UIIconPath + "wallpaper_desktop.png");

            var screen = go.AddComponent<DesktopScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            // 작업 표시줄. 괴담넷 창은 이 자리를 비워 두므로 창을 열어도 계속 보인다.
            var taskbar = CreatePanel(go.transform, "Taskbar", new Color(0.05f, 0.06f, 0.09f, 0.90f));
            var tbRt = (RectTransform)taskbar.transform;
            tbRt.anchorMin = new Vector2(0f, 0f);
            tbRt.anchorMax = new Vector2(1f, 0f);
            tbRt.pivot = new Vector2(0.5f, 0f);
            tbRt.anchoredPosition = Vector2.zero;
            tbRt.sizeDelta = new Vector2(0f, DesktopTaskbarHeight);

            // 작업 표시줄 위 가는 선. 배경화면과 갈라 보이게 한다.
            var tbEdge = CreatePanel(taskbar.transform, "Edge", new Color(1f, 1f, 1f, 0.08f));
            var tbEdgeRt = (RectTransform)tbEdge.transform;
            tbEdgeRt.anchorMin = new Vector2(0f, 1f);
            tbEdgeRt.anchorMax = new Vector2(1f, 1f);
            tbEdgeRt.pivot = new Vector2(0.5f, 1f);
            tbEdgeRt.sizeDelta = new Vector2(0f, 1f);
            tbEdge.GetComponent<Image>().raycastTarget = false;

            // 왼쪽부터 시작 단추, 검색 칸, 고정해 둔 앱. 모두 그림일 뿐 눌리지 않는다.
            AddTaskbarImage(taskbar.transform, "Start", "start.png", 30f, 38f);

            var search = CreatePanel(taskbar.transform, "Search", new Color(1f, 1f, 1f, 0.07f));
            var searchRt = (RectTransform)search.transform;
            searchRt.anchorMin = new Vector2(0f, 0.5f);
            searchRt.anchorMax = new Vector2(0f, 0.5f);
            searchRt.pivot = new Vector2(0f, 0.5f);
            searchRt.anchoredPosition = new Vector2(64f, 0f);
            searchRt.sizeDelta = new Vector2(320f, 38f);
            search.GetComponent<Image>().raycastTarget = false;
            AddTaskbarImage(search.transform, "Icon", "search.png", 20f, 20f, 0.6f);
            // 실제로 적을 수 있는 검색 칸. 적으면 위에 맞는 앱이 뜬다(DesktopScreen).
            search.GetComponent<Image>().raycastTarget = true;
            var searchInput = search.AddComponent<TMP_InputField>();
            searchInput.targetGraphic = search.GetComponent<Image>();
            var searchArea = new GameObject("TextArea", typeof(RectTransform));
            searchArea.transform.SetParent(search.transform, false);
            var searchAreaRt = (RectTransform)searchArea.transform;
            StretchInside(searchAreaRt, 42f, 10f, 4f, 4f);
            searchArea.AddComponent<RectMask2D>();

            var searchText = AddText(searchArea.transform, "Placeholder", 18f, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(searchText.rectTransform, 0f, 0f, 0f, 0f);
            searchText.raycastTarget = false;
            var searchTyped = AddText(searchArea.transform, "Text", 18f, UIFontWeight.Regular, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(searchTyped.rectTransform, 0f, 0f, 0f, 0f);
            searchTyped.raycastTarget = false;

            searchInput.textViewport = searchAreaRt;
            searchInput.textComponent = searchTyped;
            searchInput.placeholder = searchText;
            searchInput.lineType = TMP_InputField.LineType.SingleLine;
            searchInput.richText = false;
            searchInput.restoreOriginalTextOnEscape = false;
            searchInput.customCaretColor = true;
            searchInput.caretColor = TextColor;
            searchInput.caretWidth = 2;
            searchInput.selectionColor = new Color(0.36f, 0.52f, 0.78f, 0.45f);
            searchInput.text = string.Empty;

            var searchLoc = searchText.gameObject.AddComponent<LocalizedText>();
            var searchSo = new SerializedObject(searchLoc);
            searchSo.Update();
            searchSo.FindProperty("_textId").stringValue = "ui.desktop.search";
            searchSo.ApplyModifiedPropertiesWithoutUndo();

            // 오른쪽 알림 칸. 와이파이, 소리, 배터리 그림이 시계 왼쪽에 선다.
            AddTaskbarImage(taskbar.transform, "Tray_Battery", "tray_battery.png", -190f, 24f, 0.85f);
            AddTaskbarImage(taskbar.transform, "Tray_Volume", "tray_volume.png", -224f, 22f, 0.85f);
            AddTaskbarImage(taskbar.transform, "Tray_Wifi", "tray_wifi.png", -258f, 22f, 0.85f);

            var clock = AddText(taskbar.transform, "Clock", 24f, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Right);
            StretchInside(clock.rectTransform, 40f, 40f, 8f, 8f);

            // 왼쪽에는 지금 이 괴담을 얼마나 믿고 있는지를 띄운다. 게임의 핵심 숫자다.
            var belief = AddText(taskbar.transform, "Belief", 24f, UIFontWeight.Medium, new Color(0.92f, 0.44f, 0.42f),
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(belief.rectTransform, 404f, 40f, 8f, 8f);

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
                var icon = BuildDesktopIcon(go.transform, apps[i][0], new Vector2(-840f, 400f - i * 150f),
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

            // --- 검색 결과 ---
            // 검색 칸 바로 위에 뜬다. 맞는 앱을 한 줄씩 늘어놓는다. 줄은 DesktopScreen 이 채운다.
            var results = CreatePanel(go.transform, "SearchResults", new Color(0.09f, 0.10f, 0.14f, 0.97f));
            var resultsRt = (RectTransform)results.transform;
            resultsRt.anchorMin = new Vector2(0f, 0f);
            resultsRt.anchorMax = new Vector2(0f, 0f);
            resultsRt.pivot = new Vector2(0f, 0f);
            resultsRt.anchoredPosition = new Vector2(64f, DesktopTaskbarHeight + 8f);
            resultsRt.sizeDelta = new Vector2(420f, 80f);
            results.GetComponent<Image>().raycastTarget = true;
            var resultsLayout = results.AddComponent<VerticalLayoutGroup>();
            resultsLayout.padding = new RectOffset(10, 10, 10, 10);
            resultsLayout.spacing = 4f;
            resultsLayout.childControlWidth = true;
            resultsLayout.childControlHeight = true;
            resultsLayout.childForceExpandWidth = true;
            resultsLayout.childForceExpandHeight = false;
            var resultsFit = results.AddComponent<ContentSizeFitter>();
            resultsFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var resultItem = CreatePanel(results.transform, "ItemTemplate", Color.white);
            resultItem.GetComponent<Image>().raycastTarget = true;
            var resultItemSize = resultItem.AddComponent<LayoutElement>();
            resultItemSize.preferredHeight = 56f;
            var resultButton = resultItem.AddComponent<Button>();
            resultButton.targetGraphic = resultItem.GetComponent<Image>();
            var resultColors = resultButton.colors;
            resultColors.normalColor = new Color(1f, 1f, 1f, 0f);
            resultColors.highlightedColor = new Color(1f, 1f, 1f, 0.10f);
            resultColors.pressedColor = new Color(1f, 1f, 1f, 0.18f);
            resultColors.selectedColor = new Color(1f, 1f, 1f, 0.10f);
            resultColors.disabledColor = new Color(1f, 1f, 1f, 0f);
            resultButton.colors = resultColors;

            var resultIcon = CreatePanel(resultItem.transform, "Icon", Color.white);
            var resultIconRt = (RectTransform)resultIcon.transform;
            resultIconRt.anchorMin = new Vector2(0f, 0.5f);
            resultIconRt.anchorMax = new Vector2(0f, 0.5f);
            resultIconRt.pivot = new Vector2(0f, 0.5f);
            resultIconRt.anchoredPosition = new Vector2(10f, 0f);
            resultIconRt.sizeDelta = new Vector2(38f, 38f);
            resultIcon.GetComponent<Image>().preserveAspect = true;
            resultIcon.GetComponent<Image>().raycastTarget = false;

            var resultLabel = AddText(resultItem.transform, "Label", 22f, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            StretchInside(resultLabel.rectTransform, 62f, 10f, 4f, 4f);
            resultLabel.raycastTarget = false;
            resultItem.SetActive(false);

            var resultEmpty = AddText(results.transform, "Empty", 20f, UIFontWeight.Regular, DimTextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            resultEmpty.raycastTarget = false;
            var resultEmptySize = resultEmpty.gameObject.AddComponent<LayoutElement>();
            resultEmptySize.preferredHeight = 44f;
            results.SetActive(false);

            so.FindProperty("_searchInput").objectReferenceValue = searchInput;
            so.FindProperty("_searchResults").objectReferenceValue = resultsRt;
            so.FindProperty("_searchItemTemplate").objectReferenceValue = resultButton;
            so.FindProperty("_searchEmpty").objectReferenceValue = resultEmpty;

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

            // 누르는 자리. 그림과 이름표를 함께 덮는 투명한 판이다.
            var hit = CreatePanel(go.transform, "HitArea", new Color(1f, 1f, 1f, 0f));
            StretchInside((RectTransform)hit.transform, 36f, 36f, -6f, -6f);
            hit.GetComponent<Image>().raycastTarget = true;

            var box = CreatePanel(go.transform, "Box", Color.white);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchoredPosition = new Vector2(0f, 24f);
            boxRt.sizeDelta = new Vector2(80f, 80f);
            var boxImage = box.GetComponent<Image>();
            boxImage.sprite = LoadBackdropSprite(UIIconPath + "app_" + id + ".png");
            boxImage.preserveAspect = true;
            boxImage.raycastTarget = false;

            // 마우스를 올리면 아이콘 모양 그대로 옅게 밝아진다. 현장 물건을 밝히는 것과 같은 느낌이다.
            AddIconHighlight(box.transform, button);

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
        /// <summary>
        /// 장소를 옮겨 가는 동안 덮는 화면.
        ///
        /// 한가운데에 어디로 가는지와 지금 시각을 적고, 그 아래 길 위로 작은 택시가 달린다.
        /// 맨 아래 막대가 함께 차오른다. 그림 파일 없이 네모로만 그린다.
        /// </summary>
        private static TravelScreen BuildTravelScreen(string name)
        {
            var go = CreatePanel(null, name, new Color(0.05f, 0.06f, 0.09f, 1f));
            StretchFull(go);

            var screen = go.AddComponent<TravelScreen>();
            // 화면 쌓기 밖에 맨 위 층으로 띄운다(ShowDetached). 덮고 있는 동안 아래 화면이 바뀌어도 밀려나지 않는다.
            ConfigureScreen(screen, name, UILayer.System, false, false);
            var group = go.AddComponent<CanvasGroup>();

            var title = AddText(go.transform, "Title", 50f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 130f), new Vector2(1200f, 70f), TextAlignmentOptions.Center);

            var dots = AddText(go.transform, "Dots", 50f, UIFontWeight.Bold, AccentColor,
                new Vector2(0f, 72f), new Vector2(300f, 50f), TextAlignmentOptions.Center);
            dots.text = "·";

            var duration = AddText(go.transform, "Duration", 26f, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 20f), new Vector2(1200f, 40f), TextAlignmentOptions.Center);

            // 지금 시각. 이동하는 동안 시계가 흘러가는 것이 여기에 그대로 보인다.
            var clock = AddText(go.transform, "Clock", 36f, UIFontWeight.SemiBold, AccentColor,
                new Vector2(0f, -36f), new Vector2(600f, 50f), TextAlignmentOptions.Center);
            clock.gameObject.AddComponent<ClockLabel>();

            // 도로. 택시는 이 길의 왼쪽 끝에서 오른쪽 끝까지 간다.
            // 길 위쪽 가장자리가 바퀴가 닿는 선이다. 가운데에 흰 점선을 긋는다.
            const float TrackWidth = 900f;
            var track = CreatePanel(go.transform, "Road", new Color(0.36f, 0.37f, 0.44f, 1f));
            var trackRt = (RectTransform)track.transform;
            trackRt.anchoredPosition = new Vector2(0f, -150f);
            trackRt.sizeDelta = new Vector2(TrackWidth, 3f);
            track.GetComponent<Image>().raycastTarget = false;

            var asphalt = CreatePanel(track.transform, "Asphalt", new Color(0.13f, 0.14f, 0.18f, 1f));
            var asphaltRt = (RectTransform)asphalt.transform;
            asphaltRt.anchorMin = new Vector2(0f, 0.5f);
            asphaltRt.anchorMax = new Vector2(1f, 0.5f);
            asphaltRt.pivot = new Vector2(0.5f, 1f);
            asphaltRt.anchoredPosition = new Vector2(0f, -1.5f);
            asphaltRt.sizeDelta = new Vector2(0f, 26f);
            asphalt.GetComponent<Image>().raycastTarget = false;

            for (int i = 0; i < 15; i++)
            {
                var dash = CreatePanel(asphalt.transform, "Lane_" + i, new Color(0.78f, 0.78f, 0.72f, 1f));
                var dashRt = (RectTransform)dash.transform;
                dashRt.anchorMin = new Vector2((i + 0.5f) / 15f, 0.5f);
                dashRt.anchorMax = new Vector2((i + 0.5f) / 15f, 0.5f);
                dashRt.anchoredPosition = Vector2.zero;
                dashRt.sizeDelta = new Vector2(28f, 3f);
                dash.GetComponent<Image>().raycastTarget = false;
            }

            // 택시. 노란 몸통 위에 창 둘 달린 지붕, 지붕 위에 TAXI 등, 옆구리에 체크 띠, 바퀴 둘.
            // 오른쪽으로 달리므로 앞머리 불빛은 오른쪽, 붉은 꼬리등은 왼쪽이다.
            var taxiYellow = new Color(0.97f, 0.78f, 0.20f, 1f);
            var runner = CreatePanel(track.transform, "Taxi", new Color(1f, 1f, 1f, 0f));
            var runnerRt = (RectTransform)runner.transform;
            runnerRt.anchorMin = new Vector2(0f, 0.5f);
            runnerRt.anchorMax = new Vector2(0f, 0.5f);
            runnerRt.pivot = new Vector2(0.5f, 0f);
            runnerRt.anchoredPosition = Vector2.zero;
            runnerRt.sizeDelta = new Vector2(150f, 72f);
            runner.GetComponent<Image>().raycastTarget = false;

            TaxiPart(runner.transform, "Body", taxiYellow, new Vector2(0f, 10f), new Vector2(150f, 30f));
            TaxiPart(runner.transform, "Cabin", taxiYellow, new Vector2(34f, 38f), new Vector2(82f, 22f));
            TaxiPart(runner.transform, "Glass_Back", new Color(0.18f, 0.24f, 0.34f, 1f), new Vector2(40f, 42f), new Vector2(32f, 14f));
            TaxiPart(runner.transform, "Glass_Front", new Color(0.18f, 0.24f, 0.34f, 1f), new Vector2(78f, 42f), new Vector2(32f, 14f));
            TaxiPart(runner.transform, "Pillar", taxiYellow, new Vector2(73f, 42f), new Vector2(4f, 14f));

            var sign = TaxiPart(runner.transform, "RoofSign", new Color(0.98f, 0.95f, 0.82f, 1f), new Vector2(57f, 60f), new Vector2(36f, 12f));
            var signText = AddText(sign.transform, "Label", 10f, UIFontWeight.Bold, new Color(0.14f, 0.12f, 0.10f),
                Vector2.zero, new Vector2(36f, 12f), TextAlignmentOptions.Center);
            signText.text = "TAXI";
            signText.raycastTarget = false;
            signText.enableAutoSizing = false;

            // 체크 띠. 흑백 네모를 번갈아 한 줄로 늘어놓는다.
            for (int i = 0; i < 12; i++)
            {
                TaxiPart(runner.transform, "Check_" + i,
                    i % 2 == 0 ? new Color(0.12f, 0.12f, 0.14f, 1f) : new Color(0.96f, 0.96f, 0.94f, 1f),
                    new Vector2(15f + i * 10f, 22f), new Vector2(10f, 6f));
            }

            TaxiPart(runner.transform, "HeadLight", new Color(1f, 0.97f, 0.78f, 1f), new Vector2(142f, 28f), new Vector2(8f, 7f));
            TaxiPart(runner.transform, "TailLight", new Color(0.86f, 0.22f, 0.20f, 1f), new Vector2(0f, 28f), new Vector2(6f, 7f));

            var wheelSprite = FieldCircleSprite();
            foreach (var wheelX in new[] { 34f, 116f })
            {
                var wheel = TaxiPart(runner.transform, "Wheel_" + wheelX, new Color(0.10f, 0.10f, 0.12f, 1f),
                    new Vector2(wheelX - 13f, 0f), new Vector2(26f, 26f));
                wheel.GetComponent<Image>().sprite = wheelSprite;
                var hub = TaxiPart(wheel.transform, "Hub", new Color(0.62f, 0.64f, 0.70f, 1f), new Vector2(8f, 8f), new Vector2(10f, 10f));
                hub.GetComponent<Image>().sprite = wheelSprite;
            }

            // 진행 막대.
            var bar = CreatePanel(go.transform, "Bar", new Color(0.16f, 0.17f, 0.23f, 1f));
            var barRt = (RectTransform)bar.transform;
            barRt.anchoredPosition = new Vector2(0f, -225f);
            barRt.sizeDelta = new Vector2(TrackWidth, 10f);
            bar.GetComponent<Image>().raycastTarget = false;

            var fill = CreatePanel(bar.transform, "Fill", AccentColor);
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().raycastTarget = false;

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = title;
            so.FindProperty("_durationText").objectReferenceValue = duration;
            so.FindProperty("_dotsText").objectReferenceValue = dots;
            so.FindProperty("_runner").objectReferenceValue = runnerRt;
            so.FindProperty("_track").objectReferenceValue = trackRt;
            so.FindProperty("_barFill").objectReferenceValue = fillRt;
            so.FindProperty("_group").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();

            return screen;
        }

        /// <summary>
        /// 택시 그림의 한 조각. 부모의 왼쪽 아래 모서리를 기준으로 자리와 크기를 준다.
        /// 몸통과 창과 바퀴를 모두 이렇게 쌓는다. 그림 파일이 생기면 조각들을 한 장으로 바꾸면 된다.
        /// </summary>
        private static GameObject TaxiPart(Transform parent, string name, Color color, Vector2 bottomLeft, Vector2 size)
        {
            var part = CreatePanel(parent, name, color);
            var rt = (RectTransform)part.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = bottomLeft;
            rt.sizeDelta = size;
            part.GetComponent<Image>().raycastTarget = false;
            return part;
        }

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

            // 머리말 오른쪽 끝의 글쓰기 단추. 쓸 글이 있을 때만 켜진다. 켜고 끄는 것은 괴담넷 화면이 한다.
            var writeGo = CreatePanel(boardHeader.transform, "Btn_Write", new Color(0.86f, 0.74f, 0.48f, 1f));
            var writeRt = (RectTransform)writeGo.transform;
            writeRt.anchorMin = new Vector2(1f, 0.5f);
            writeRt.anchorMax = new Vector2(1f, 0.5f);
            writeRt.pivot = new Vector2(1f, 0.5f);
            writeRt.anchoredPosition = new Vector2(-BoardInset, 0f);
            writeRt.sizeDelta = new Vector2(190f, 58f);
            var writeButton = writeGo.AddComponent<Button>();
            writeButton.targetGraphic = writeGo.GetComponent<Image>();
            var writeLabel = AddText(writeGo.transform, "Label", 26f, UIFontWeight.Bold, new Color(0.14f, 0.12f, 0.10f),
                Vector2.zero, new Vector2(190f, 58f), TextAlignmentOptions.Center);
            writeLabel.raycastTarget = false;
            var writeLabelText = writeLabel.gameObject.AddComponent<LocalizedText>();
            var wlso = new SerializedObject(writeLabelText);
            wlso.Update();
            wlso.FindProperty("_textId").stringValue = "ui.post.btn_write";
            wlso.ApplyModifiedPropertiesWithoutUndo();

            // 단추 왼쪽에서 단추를 가리키는 세모. 튜토리얼에서만 켠다.
            var writeHint = AddText(boardHeader.transform, "WriteHint", 30f, UIFontWeight.Bold, AccentColor,
                Vector2.zero, new Vector2(44f, 44f), TextAlignmentOptions.Center);
            writeHint.text = "▶";
            writeHint.raycastTarget = false;
            var writeHintRt = writeHint.rectTransform;
            writeHintRt.anchorMin = new Vector2(1f, 0.5f);
            writeHintRt.anchorMax = new Vector2(1f, 0.5f);
            writeHintRt.pivot = new Vector2(1f, 0.5f);
            writeHintRt.anchoredPosition = new Vector2(-BoardInset - 200f, 0f);
            writeHint.gameObject.AddComponent<HintNudge>();
            writeHint.gameObject.SetActive(false);
            writeGo.SetActive(false);

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
            so.FindProperty("_writeButton").objectReferenceValue = writeButton;
            so.FindProperty("_writeHint").objectReferenceValue = writeHint.gameObject;

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
            // 단서가 여섯 줄이고 줄마다 여백을 두므로 칸을 아래로 늘린다.
            // 아래 결과 띠(-274 즈음)에 닿지 않는 선까지다.
            clueRt.sizeDelta = new Vector2(600f, 590f);

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
            //
            // 사건마다 단서 수가 다르고, 한 줄이 두 줄로 접히기도 한다. 칸 높이를 손으로 잡아 두면
            // 단서가 하나 늘 때마다 아래로 넘쳐 흐른다. 그래서 끌어서 볼 수 있게 둔다.
            // 막대는 붙이지 않는다. 다섯 줄이든 여덟 줄이든 잡아서 올리면 된다.
            var clueViewport = new GameObject("ClueViewport", typeof(RectTransform));
            clueViewport.transform.SetParent(clueCard.transform, false);
            var clueViewRt = (RectTransform)clueViewport.transform;
            StretchInside(clueViewRt, 22f, 22f, 92f, 20f);
            clueViewport.AddComponent<RectMask2D>();

            // 글자가 없는 빈 곳을 잡아도 끌리게 한다. 보이지 않지만 눌림은 받는 판이다.
            var clueGrab = clueViewport.AddComponent<Image>();
            clueGrab.color = new Color(1f, 1f, 1f, 0f);
            clueGrab.raycastTarget = true;

            var clueList = new GameObject("Clues", typeof(RectTransform));
            clueList.transform.SetParent(clueViewport.transform, false);
            var clueListRt = (RectTransform)clueList.transform;
            clueListRt.anchorMin = new Vector2(0f, 1f);
            clueListRt.anchorMax = new Vector2(1f, 1f);
            clueListRt.pivot = new Vector2(0.5f, 1f);
            clueListRt.anchoredPosition = Vector2.zero;
            clueListRt.sizeDelta = Vector2.zero;

            var clueFitter = clueList.AddComponent<ContentSizeFitter>();
            clueFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            clueFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var clueScroll = clueViewport.AddComponent<ScrollRect>();
            clueScroll.viewport = clueViewRt;
            clueScroll.content = clueListRt;
            clueScroll.horizontal = false;
            clueScroll.vertical = true;
            clueScroll.movementType = ScrollRect.MovementType.Clamped;
            clueScroll.scrollSensitivity = 32f;

            var clueLayout = clueList.AddComponent<VerticalLayoutGroup>();
            // 줄과 줄 사이를 넉넉히 띄운다. 붙여 두면 다섯 줄이 한 덩어리로 읽혀서
            // 어디까지가 한 단서인지 눈으로 끊지 못한다.
            clueLayout.spacing = 16f;
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
            clueRowLayout.padding = new RectOffset(18, 18, 14, 14);
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

        /// <summary>
        /// 보고서 작성 화면.
        ///
        /// 왼쪽이 종이고 오른쪽이 넣을 말이다. 종이의 한 줄을 고르면 오른쪽이 그 줄의 후보로 바뀐다.
        /// 규칙 추론 화면과 같은 두 칸 짜임을 쓴다. 조사 끝에 이어지는 화면이라 모습이 이어져야 한다.
        /// </summary>
        private static ReportScreen BuildReportScreen(string name, out Transform buttonRow)
        {
            var go = CreatePanel(null, name, PanelColor);
            StretchFull(go);

            var screen = go.AddComponent<ReportScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);

            const float SectionTitle = 32f;
            const float RowLabel = 26f;
            const float RowSlot = 28f;
            const float GuideText = 26f;

            var cardColor = new Color(0.16f, 0.17f, 0.23f, 1f);

            var titleText = AddText(go.transform, "Title", 54f, UIFontWeight.Bold, TextColor,
                new Vector2(0f, 430f), new Vector2(1500f, 70f), TextAlignmentOptions.Center);
            var footerText = AddText(go.transform, "Footer", GuideText, UIFontWeight.Regular, DimTextColor,
                new Vector2(0f, 368f), new Vector2(1500f, 50f), TextAlignmentOptions.Center);

            // --- 왼쪽: 종이 ---
            var paper = CreatePanel(go.transform, "Paper", cardColor);
            var paperRt = (RectTransform)paper.transform;
            paperRt.anchorMin = new Vector2(0.5f, 0.5f);
            paperRt.anchorMax = new Vector2(0.5f, 0.5f);
            paperRt.pivot = new Vector2(0f, 1f);
            paperRt.anchoredPosition = new Vector2(-900f, 320f);
            paperRt.sizeDelta = new Vector2(900f, 590f);

            var paperTitle = AddText(paper.transform, "PaperTitle", SectionTitle, UIFontWeight.SemiBold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            var paperTitleRt = paperTitle.rectTransform;
            paperTitleRt.anchorMin = new Vector2(0f, 1f);
            paperTitleRt.anchorMax = new Vector2(1f, 1f);
            paperTitleRt.pivot = new Vector2(0.5f, 1f);
            paperTitleRt.anchoredPosition = new Vector2(0f, -22f);
            paperTitleRt.sizeDelta = new Vector2(-56f, 44f);
            paperTitle.textWrappingMode = TextWrappingModes.NoWrap;

            var paperDivider = CreatePanel(paper.transform, "Divider", new Color(0.32f, 0.33f, 0.40f, 1f));
            var paperDividerRt = (RectTransform)paperDivider.transform;
            paperDividerRt.anchorMin = new Vector2(0f, 1f);
            paperDividerRt.anchorMax = new Vector2(1f, 1f);
            paperDividerRt.pivot = new Vector2(0.5f, 1f);
            paperDividerRt.anchoredPosition = new Vector2(0f, -76f);
            paperDividerRt.sizeDelta = new Vector2(-56f, 2f);
            paperDivider.GetComponent<Image>().raycastTarget = false;
            AddCrisp(paperDivider, 2f);

            var rows = new GameObject("Rows", typeof(RectTransform));
            rows.transform.SetParent(paper.transform, false);
            var rowsRt = (RectTransform)rows.transform;
            StretchInside(rowsRt, 22f, 22f, 92f, 20f);

            var rowsLayout = rows.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 16f;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = false;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;

            // 한 줄. 위에 칸 이름, 아래에 채우는 자리.
            var rowTemplate = CreatePanel(rows.transform, "RowTemplate", new Color(0.13f, 0.14f, 0.19f, 1f));
            var rowButton = rowTemplate.AddComponent<Button>();
            rowButton.targetGraphic = rowTemplate.GetComponent<Image>();
            ((RectTransform)rowTemplate.transform).sizeDelta = new Vector2(856f, 96f);

            var rowSize = rowTemplate.AddComponent<LayoutElement>();
            rowSize.preferredHeight = 96f;
            rowSize.minHeight = 96f;

            var rowLabel = AddText(rowTemplate.transform, "RowLabel", RowLabel, UIFontWeight.SemiBold, AccentColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            var rowLabelRt = rowLabel.rectTransform;
            rowLabelRt.anchorMin = new Vector2(0f, 1f);
            rowLabelRt.anchorMax = new Vector2(1f, 1f);
            rowLabelRt.pivot = new Vector2(0.5f, 1f);
            rowLabelRt.anchoredPosition = new Vector2(0f, -12f);
            rowLabelRt.sizeDelta = new Vector2(-40f, 32f);
            rowLabel.textWrappingMode = TextWrappingModes.NoWrap;
            rowLabel.raycastTarget = false;

            var rowSlot = AddText(rowTemplate.transform, "RowSlot", RowSlot, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
            var rowSlotRt = rowSlot.rectTransform;
            rowSlotRt.anchorMin = new Vector2(0f, 1f);
            rowSlotRt.anchorMax = new Vector2(1f, 1f);
            rowSlotRt.pivot = new Vector2(0.5f, 1f);
            rowSlotRt.anchoredPosition = new Vector2(0f, -48f);
            rowSlotRt.sizeDelta = new Vector2(-40f, 40f);
            rowSlot.raycastTarget = false;
            rowTemplate.SetActive(false);

            // --- 오른쪽: 넣을 말 ---
            var choiceTitle = AddText(go.transform, "ChoiceTitle", SectionTitle, UIFontWeight.SemiBold, AccentColor,
                Vector2.zero, new Vector2(820f, 44f), TextAlignmentOptions.Left);
            var choiceTitleRt = choiceTitle.rectTransform;
            choiceTitleRt.pivot = new Vector2(0f, 1f);
            choiceTitleRt.anchoredPosition = new Vector2(60f, 320f);
            choiceTitle.textWrappingMode = TextWrappingModes.NoWrap;

            var choiceViewport = new GameObject("ChoiceViewport", typeof(RectTransform));
            choiceViewport.transform.SetParent(go.transform, false);
            var choiceViewRt = (RectTransform)choiceViewport.transform;
            choiceViewRt.anchorMin = new Vector2(0.5f, 0.5f);
            choiceViewRt.anchorMax = new Vector2(0.5f, 0.5f);
            choiceViewRt.pivot = new Vector2(0f, 1f);
            choiceViewRt.anchoredPosition = new Vector2(60f, 258f);
            choiceViewRt.sizeDelta = new Vector2(820f, 528f);
            choiceViewport.AddComponent<RectMask2D>();

            var choiceGrab = choiceViewport.AddComponent<Image>();
            choiceGrab.color = new Color(1f, 1f, 1f, 0f);
            choiceGrab.raycastTarget = true;

            var choices = new GameObject("Choices", typeof(RectTransform));
            choices.transform.SetParent(choiceViewport.transform, false);
            var choicesRt = (RectTransform)choices.transform;
            choicesRt.anchorMin = new Vector2(0f, 1f);
            choicesRt.anchorMax = new Vector2(1f, 1f);
            choicesRt.pivot = new Vector2(0.5f, 1f);
            choicesRt.anchoredPosition = Vector2.zero;
            choicesRt.sizeDelta = Vector2.zero;

            var choicesFitter = choices.AddComponent<ContentSizeFitter>();
            choicesFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            choicesFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var choiceScroll = choiceViewport.AddComponent<ScrollRect>();
            choiceScroll.viewport = choiceViewRt;
            choiceScroll.content = choicesRt;
            choiceScroll.horizontal = false;
            choiceScroll.vertical = true;
            choiceScroll.movementType = ScrollRect.MovementType.Clamped;
            choiceScroll.scrollSensitivity = 32f;

            var choicesLayout = choices.AddComponent<VerticalLayoutGroup>();
            choicesLayout.spacing = 12f;
            choicesLayout.childAlignment = TextAnchor.UpperLeft;
            choicesLayout.childControlWidth = true;
            choicesLayout.childControlHeight = true;
            choicesLayout.childForceExpandWidth = true;
            choicesLayout.childForceExpandHeight = false;

            var choiceTemplate = CreatePanel(choices.transform, "ChoiceTemplate", new Color(0.13f, 0.14f, 0.19f, 1f));
            var choiceButton = choiceTemplate.AddComponent<Button>();
            choiceButton.targetGraphic = choiceTemplate.GetComponent<Image>();

            var choiceFit = choiceTemplate.AddComponent<ContentSizeFitter>();
            choiceFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            choiceFit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var choiceRowLayout = choiceTemplate.AddComponent<VerticalLayoutGroup>();
            choiceRowLayout.padding = new RectOffset(18, 18, 14, 14);
            choiceRowLayout.childControlWidth = true;
            choiceRowLayout.childControlHeight = true;
            choiceRowLayout.childForceExpandWidth = true;
            choiceRowLayout.childForceExpandHeight = false;

            var choiceLabel = AddText(choiceTemplate.transform, "ChoiceLabel", 24f, UIFontWeight.Regular,
                TextColor, Vector2.zero, new Vector2(760f, 40f), TextAlignmentOptions.TopLeft);
            choiceLabel.raycastTarget = false;
            choiceTemplate.SetActive(false);

            // --- 아래 ---
            var resultBar = CreatePanel(go.transform, "ResultBar", new Color(0.12f, 0.13f, 0.18f, 1f));
            var resultRt = (RectTransform)resultBar.transform;
            resultRt.anchorMin = new Vector2(0.5f, 0.5f);
            resultRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultRt.pivot = new Vector2(0.5f, 0.5f);
            resultRt.anchoredPosition = new Vector2(0f, -305f);
            resultRt.sizeDelta = new Vector2(1760f, 62f);

            var resultText = AddText(resultBar.transform, "Text_Result", GuideText, UIFontWeight.Medium,
                WarnColor, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(resultText.rectTransform, 24f, 24f, 6f, 6f);
            resultText.raycastTarget = false;
            resultBar.SetActive(false);

            buttonRow = CreateButtonRow(go.transform, new Vector2(0f, -425f), new Vector2(900f, 110f));

            var so = new SerializedObject(screen);
            so.Update();
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_footerText").objectReferenceValue = footerText;
            so.FindProperty("_paperTitleText").objectReferenceValue = paperTitle;
            so.FindProperty("_rowRoot").objectReferenceValue = rowsRt;
            so.FindProperty("_rowTemplate").objectReferenceValue = rowButton;
            so.FindProperty("_choiceTitleText").objectReferenceValue = choiceTitle;
            so.FindProperty("_choiceRoot").objectReferenceValue = choicesRt;
            so.FindProperty("_choiceTemplate").objectReferenceValue = choiceButton;
            so.FindProperty("_resultRoot").objectReferenceValue = resultBar;
            so.FindProperty("_resultText").objectReferenceValue = resultText;
            so.ApplyModifiedPropertiesWithoutUndo();

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
            // 띠 높이(BandHeight)는 겹침 대화 상자와 같이 쓴다. 위에 따로 적어 두었다.

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

            // 테두리는 초상 뒤에 따로 깐다. 초상의 자식으로 두면 자식이 위에 그려져 그림을 통째로 덮는다.
            var portraitEdge = CreatePanel(speech.transform, "PortraitEdge", new Color(0.42f, 0.44f, 0.52f, 1f));
            var edgeRt2 = (RectTransform)portraitEdge.transform;
            edgeRt2.anchorMin = new Vector2(0f, 0.5f);
            edgeRt2.anchorMax = new Vector2(0f, 0.5f);
            edgeRt2.pivot = new Vector2(0f, 0.5f);
            edgeRt2.anchoredPosition = new Vector2(58f, 0f);
            edgeRt2.sizeDelta = new Vector2(PortraitSize + 4f, PortraitSize + 4f);
            portraitEdge.GetComponent<Image>().raycastTarget = false;

            var portrait = CreatePanel(speech.transform, "Portrait", new Color(0.16f, 0.17f, 0.22f, 1f));
            var portraitRt = (RectTransform)portrait.transform;
            portraitRt.anchorMin = new Vector2(0f, 0.5f);
            portraitRt.anchorMax = new Vector2(0f, 0.5f);
            portraitRt.pivot = new Vector2(0f, 0.5f);
            portraitRt.anchoredPosition = new Vector2(60f, 0f);
            portraitRt.sizeDelta = new Vector2(PortraitSize, PortraitSize);

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
            var phoneBar = CreatePanel(phoneScreen.transform, "StatusBar", new Color(0f, 0f, 0f, 0.35f));
            var phoneBarRt = (RectTransform)phoneBar.transform;
            phoneBarRt.anchorMin = new Vector2(0f, 1f);
            phoneBarRt.anchorMax = new Vector2(1f, 1f);
            phoneBarRt.pivot = new Vector2(0.5f, 1f);
            phoneBarRt.anchoredPosition = Vector2.zero;
            phoneBarRt.sizeDelta = new Vector2(0f, StatusHeight);
            phoneBar.transform.SetAsFirstSibling();   // 노치가 위에 오게

            // 휴대폰 배경화면. 상태 줄보다 뒤, 화면 맨 뒤에 깐다.
            var phoneWall = AddCoverImage(phoneScreen.transform, "Wallpaper", UIIconPath + "wallpaper_phone.png");
            if (phoneWall != null) phoneWall.transform.SetAsFirstSibling();

            // 닫기 단추 오른쪽에 신호, 와이파이, 배터리.
            AddStatusIcon(phoneBar.transform, "Signal", "tray_signal.png", 42f, 14f);
            AddStatusIcon(phoneBar.transform, "Wifi", "tray_wifi.png", 60f, 14f);
            AddStatusIcon(phoneBar.transform, "Battery", "tray_battery.png", 78f, 18f);

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
            var dock = CreatePanel(phoneScreen.transform, "Dock", new Color(1f, 1f, 1f, 0f));   // 판은 보이지 않는다. 자리만 잡는다
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
            so.FindProperty("_buttonRow").objectReferenceValue = buttonRow.gameObject;
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

        /// <summary>
        /// 대사 넘기기(스킵) 단추. 넘기기 판의 자식이라 판이 켜졌을 때만 보인다.
        /// offset 은 판의 오른쪽 위 모서리에서 잰 자리다.
        /// </summary>
        private static RectTransform AddSkipButton(Transform advance, Vector2 offset)
        {
            var go = CreatePanel(advance, "Btn_Skip", new Color(0.05f, 0.05f, 0.07f, 0.72f));
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(150f, 52f);
            var image = go.GetComponent<Image>();
            image.raycastTarget = true;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;

            var edge = CreatePanel(go.transform, "Edge", new Color(1f, 1f, 1f, 0.18f));
            StretchFull(edge);
            edge.GetComponent<Image>().raycastTarget = false;
            edge.transform.SetAsFirstSibling();
            var inner = CreatePanel(go.transform, "Fill", new Color(0.05f, 0.05f, 0.07f, 0.9f));
            StretchInside((RectTransform)inner.transform, 1f, 1f, 1f, 1f);
            inner.GetComponent<Image>().raycastTarget = false;

            var label = AddText(go.transform, "Label", 24f, UIFontWeight.Medium, TextColor,
                Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            StretchInside(label.rectTransform, 6f, 6f, 4f, 4f);
            label.raycastTarget = false;
            var localized = label.gameObject.AddComponent<LocalizedText>();
            var lso = new SerializedObject(localized);
            lso.Update();
            lso.FindProperty("_textId").stringValue = "ui.dialogue.skip";
            lso.ApplyModifiedPropertiesWithoutUndo();

            var skip = go.AddComponent<StorySkip>();
            var sso = new SerializedObject(skip);
            sso.Update();
            sso.FindProperty("_button").objectReferenceValue = button;
            sso.ApplyModifiedPropertiesWithoutUndo();
            return rt;
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
