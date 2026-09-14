using TMPro;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Dev
{
    /// <summary>
    /// UI 시스템 상태를 화면에 표시하는 개발용 패널.
    /// 라벨은 Localization에서 가져오고, 값만 런타임 정보를 붙인다.
    /// </summary>
    public class UIDebugPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _target;
        [SerializeField] private float _refreshInterval = 0.25f;

        private UIService _ui;
        private LocalizationService _localization;
        private float _timer;

        private void Start()
        {
            if (_target == null) _target = GetComponent<TMP_Text>();
            ServiceRegistry.TryGet(out _ui);
            ServiceRegistry.TryGet(out _localization);
            Refresh();
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;

            _timer = _refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            if (_target == null || _localization == null) return;

            var safeArea = Screen.safeArea;
            float aspect = ScreenSetup.CurrentAspect;

            _target.text =
                $"{_localization.Get("ui.test.stack")}: {(_ui != null ? _ui.DescribeStack() : "-")}\n" +
                $"{_localization.Get("ui.test.resolution")}: {Screen.width}x{Screen.height}  (aspect {aspect:F2})\n" +
                $"{_localization.Get("ui.test.platform")}: {PlatformInfo.Kind}\n" +
                $"{_localization.Get("ui.test.language")}: {_localization.CurrentLanguage}\n" +
                $"{_localization.Get("ui.test.safearea")}: x{safeArea.x:F0} y{safeArea.y:F0} w{safeArea.width:F0} h{safeArea.height:F0}";
        }
    }
}
