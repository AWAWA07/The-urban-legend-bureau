using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Dev
{
    /// <summary>
    /// 4단계 UI 기반 검증용. 버튼이 UIService를 호출하기만 한다.
    /// 실제 게임 화면이 아니며, 확인이 끝나면 제거해도 된다.
    /// </summary>
    public class UITestController : MonoBehaviour
    {
        [SerializeField] private UIScreen _screenA;
        [SerializeField] private UIScreen _screenB;
        [SerializeField] private UIScreen _popup;
        [SerializeField] private UIScreen _hud;

        private UIService _ui;

        private void Start()
        {
            if (!ServiceRegistry.TryGet(out _ui))
            {
                Debug.LogError("[UITestController] UIService를 찾지 못했다.");
                enabled = false;
                return;
            }

            // HUD는 스택 밖에서 상시 표시한다. 다른 화면에 덮이지 않는다.
            if (_hud != null) _ui.ShowDetached(_hud);

            // 시작 화면을 올린다.
            if (_screenA != null) _ui.Push(_screenA);
        }

        public void OnPushClicked()
        {
            if (_screenB != null) _ui.Push(_screenB);
        }

        public void OnPopClicked()
        {
            _ui.Pop();
        }

        public void OnReplaceClicked()
        {
            if (_screenB != null) _ui.Replace(_screenB);
        }

        public void OnPopupClicked()
        {
            if (_popup != null) _ui.Push(_popup);
        }

        public void OnToggleLanguageClicked()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var localization)) return;

            var next = localization.CurrentLanguage == "ko" ? "en" : "ko";
            localization.SetLanguage(next);
            Debug.Log($"[UITestController] 언어 전환 -> {localization.CurrentLanguage}");
        }
    }
}
