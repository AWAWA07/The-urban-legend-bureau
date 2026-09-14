using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Dev;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// Dev_UI_Test 씬을 코드로 구성한다.
    /// 손으로 배치하면 해상도/앵커 설정이 어긋나기 쉬워 재현 가능한 형태로 만들어 둔다.
    /// 개발 전용이며 빌드에 포함되지 않는다.
    /// </summary>
    public static class DevUITestSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Dev_UI_Test.unity";
        private const string ActionAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string FontFolder = "Assets/_Project/UI/Fonts/Pretendard/";
        private const string CatalogPath = "Assets/_Project/Data/Config/GameDataCatalog.asset";

        private static readonly Color BackColor = new Color(0.07f, 0.07f, 0.10f);
        private static readonly Color PanelA = new Color(0.12f, 0.14f, 0.20f, 0.95f);
        private static readonly Color PanelB = new Color(0.20f, 0.12f, 0.14f, 0.95f);
        private static readonly Color PopupColor = new Color(0.10f, 0.10f, 0.12f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.22f, 0.26f, 0.34f, 1f);
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.95f);

        [MenuItem("UrbanLegendBureau/Dev/Build Dev_UI_Test Scene")]
        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BackColor;
            }

            // --- GameRoot ---
            var rootGo = new GameObject("GameRoot");
            var gameRoot = rootGo.AddComponent<GameRoot>();
            var actions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(ActionAssetPath);
            var catalog = AssetDatabase.LoadAssetAtPath<UrbanLegendBureau.Data.GameDataCatalogSO>(CatalogPath);
            if (catalog == null) Debug.LogError("[DevUITestSceneBuilder] GameDataCatalog을 찾지 못했다: " + CatalogPath);

            var rootSo = new SerializedObject(gameRoot);
            rootSo.FindProperty("_inputActions").objectReferenceValue = actions;
            rootSo.FindProperty("_gameDataCatalog").objectReferenceValue = catalog;
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 화면 A (Screen 레이어) ---
            // 굵기 기준: 메인 제목 Bold / 섹션 제목 SemiBold / 버튼 Medium / 본문·힌트 Regular
            var screenA = CreateScreen("Screen_A", UILayer.Screen, PanelA, true, true);
            AddTitle(screenA.transform, "ui.test.title", 64f, new Vector2(0f, 260f), UIFontWeight.Bold);
            AddTitle(screenA.transform, "ui.test.screen_a", 44f, new Vector2(0f, 175f), UIFontWeight.SemiBold);
            AddTitle(screenA.transform, "ui.test.sample_long", 28f, new Vector2(0f, 95f), UIFontWeight.Regular);
            AddTitle(screenA.transform, "ui.test.sample_glyphs", 26f, new Vector2(0f, 30f), UIFontWeight.Regular);
            AddTitle(screenA.transform, "ui.test.hint", 26f, new Vector2(0f, -210f), UIFontWeight.Regular);

            var buttonRow = new GameObject("Buttons", typeof(RectTransform));
            buttonRow.transform.SetParent(screenA.transform, false);
            var rowRt = (RectTransform)buttonRow.transform;
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, -90f);
            rowRt.sizeDelta = new Vector2(1700f, 110f);
            var layout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var pushBtn = CreateButton(buttonRow.transform, "Btn_Push", "ui.test.btn_push");
            var popBtn = CreateButton(buttonRow.transform, "Btn_Pop", "ui.test.btn_pop");
            var replaceBtn = CreateButton(buttonRow.transform, "Btn_Replace", "ui.test.btn_replace");
            var popupBtn = CreateButton(buttonRow.transform, "Btn_Popup", "ui.test.btn_popup");
            var langBtn = CreateButton(buttonRow.transform, "Btn_Language", "ui.test.btn_language");

            // --- 화면 B (Screen 레이어) ---
            var screenB = CreateScreen("Screen_B", UILayer.Screen, PanelB, true, true);
            AddTitle(screenB.transform, "ui.test.screen_b", 64f, new Vector2(0f, 0f), UIFontWeight.SemiBold);
            AddTitle(screenB.transform, "ui.test.hint", 26f, new Vector2(0f, -90f), UIFontWeight.Regular);

            // --- 팝업 (Popup 레이어, 아래 화면을 가리지 않음) ---
            var popup = CreateScreen("Popup_Test", UILayer.Popup, new Color(0f, 0f, 0f, 0.55f), false, true);
            var box = CreatePanel(popup.transform, "Box", PopupColor);
            var boxRt = (RectTransform)box.transform;
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(800f, 400f);
            AddTitle(box.transform, "ui.test.popup", 48f, new Vector2(0f, 90f), UIFontWeight.SemiBold);

            var okBtn = CreateButton(box.transform, "Btn_OK", "ui.common.ok");
            var okRt = (RectTransform)okBtn.transform;
            okRt.anchorMin = new Vector2(0.5f, 0.5f);
            okRt.anchorMax = new Vector2(0.5f, 0.5f);
            okRt.pivot = new Vector2(0.5f, 0.5f);
            okRt.anchoredPosition = new Vector2(0f, -80f);
            okRt.sizeDelta = new Vector2(320f, 100f);

            // --- HUD: 상태 표시 (Safe Area 적용 대상) ---
            var hud = CreateScreen("HUD_Debug", UILayer.HUD, new Color(0f, 0f, 0f, 0f), false, false);
            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(hud.transform, false);
            var safeRt = (RectTransform)safe.transform;
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safe.AddComponent<SafeAreaFitter>();

            var infoGo = new GameObject("InfoText", typeof(RectTransform));
            infoGo.transform.SetParent(safe.transform, false);
            var info = infoGo.AddComponent<TextMeshProUGUI>();
            var infoFont = LoadFont(UIFontWeight.Regular);
            if (infoFont != null) info.font = infoFont;
            info.fontSize = 28f;
            info.color = TextColor;
            info.alignment = TextAlignmentOptions.TopLeft;
            info.raycastTarget = false;
            var infoRt = (RectTransform)infoGo.transform;
            infoRt.anchorMin = new Vector2(0f, 1f);
            infoRt.anchorMax = new Vector2(0f, 1f);
            infoRt.pivot = new Vector2(0f, 1f);
            infoRt.anchoredPosition = new Vector2(40f, -30f);
            infoRt.sizeDelta = new Vector2(1100f, 240f);

            var debugPanel = infoGo.AddComponent<UIDebugPanel>();
            var debugSo = new SerializedObject(debugPanel);
            debugSo.FindProperty("_target").objectReferenceValue = info;
            debugSo.ApplyModifiedPropertiesWithoutUndo();

            // --- 컨트롤러 배선 ---
            // 빌드된 앱에서 자동 검증을 수행하는 개발용 컴포넌트
            var smokeGo = new GameObject("PlatformSmokeTest");
            smokeGo.AddComponent<PlatformSmokeTest>();

            var controllerGo = new GameObject("UITestController");
            var controller = controllerGo.AddComponent<UITestController>();
            var ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("_screenA").objectReferenceValue = screenA.GetComponent<UIScreen>();
            ctrlSo.FindProperty("_screenB").objectReferenceValue = screenB.GetComponent<UIScreen>();
            ctrlSo.FindProperty("_popup").objectReferenceValue = popup.GetComponent<UIScreen>();
            ctrlSo.FindProperty("_hud").objectReferenceValue = hud.GetComponent<UIScreen>();
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(pushBtn.GetComponent<Button>().onClick, controller.OnPushClicked);
            UnityEventTools.AddPersistentListener(popBtn.GetComponent<Button>().onClick, controller.OnPopClicked);
            UnityEventTools.AddPersistentListener(replaceBtn.GetComponent<Button>().onClick, controller.OnReplaceClicked);
            UnityEventTools.AddPersistentListener(popupBtn.GetComponent<Button>().onClick, controller.OnPopupClicked);
            UnityEventTools.AddPersistentListener(langBtn.GetComponent<Button>().onClick, controller.OnToggleLanguageClicked);
            UnityEventTools.AddPersistentListener(okBtn.GetComponent<Button>().onClick, popup.GetComponent<UIScreen>().RequestClose);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            return "Dev_UI_Test 씬 생성 완료: " + ScenePath;
        }

        // ------------------------------------------------------------- 헬퍼

        private static GameObject CreateScreen(string name, UILayer layer, Color background, bool hidesUnderlying, bool closableByBack)
        {
            var go = CreatePanel(null, name, background);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var screen = go.AddComponent<UIScreen>();
            var so = new SerializedObject(screen);
            so.FindProperty("_screenId").stringValue = name;
            so.FindProperty("_layer").enumValueIndex = (int)layer;
            so.FindProperty("_hidesUnderlying").boolValue = hidesUnderlying;
            so.FindProperty("_closableByBack").boolValue = closableByBack;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
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

        /// <summary>굵기에 해당하는 Pretendard TMP 폰트 에셋을 가져온다.</summary>
        private static TMP_FontAsset LoadFont(UIFontWeight weight)
        {
            var path = FontFolder + "Pretendard-" + weight + " SDF.asset";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) Debug.LogError("[DevUITestSceneBuilder] 폰트를 찾지 못했다: " + path);
            return font;
        }

        private static TextMeshProUGUI AddTitle(Transform parent, string textId, float fontSize, Vector2 anchoredPosition,
            UIFontWeight weight = UIFontWeight.Regular)
        {
            var go = new GameObject("Text_" + textId, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            var font = LoadFont(weight);
            if (font != null) text.font = font;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(1600f, fontSize * 1.8f);

            var localized = go.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.FindProperty("_textId").stringValue = textId;
            so.ApplyModifiedPropertiesWithoutUndo();

            return text;
        }

        private static GameObject CreateButton(Transform parent, string name, string textId,
            UIFontWeight weight = UIFontWeight.Medium)
        {
            var go = CreatePanel(parent, name, ButtonColor);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(300f, 100f);

            var label = AddTitle(go.transform, textId, 26f, Vector2.zero, weight);
            var labelRt = (RectTransform)label.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(10f, 6f);
            labelRt.offsetMax = new Vector2(-10f, -6f);
            label.textWrappingMode = TextWrappingModes.Normal;

            return go;
        }
    }
}
