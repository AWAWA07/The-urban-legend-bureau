using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 열차 안 CCTV 를 가까이서 들여다보는 화면. 화면 전체에 CCTV 가 크게 보인다.
    ///
    /// 눌러 볼 수 있는 곳이 둘이다. 몸통의 흠집과 아래쪽 배터리 칸.
    /// 누르면 아래 띠에 그곳을 본 생각이 한 줄 뜬다. 돌아가기를 누르면 현장으로 돌아간다.
    /// 무엇을 보았는지에 따라 진행이 바뀌지는 않는다. 살펴보는 것뿐이라 시간도 확산도 쓰지 않는다.
    /// </summary>
    public class CctvCloseupScreen : UIScreen
    {
        [SerializeField] private Button _scratchButton;
        [SerializeField] private Button _batteryButton;
        [SerializeField] private Button _closeButton;

        [Tooltip("아래 띠. 무언가를 누르기 전에는 안내 한 줄이 떠 있다.")]
        [SerializeField] private TMP_Text _lineText;

        private const string HintTextId = "field.subway.cctv_close.hint";
        private const string ScratchTextId = "field.subway.cctv_close.scratch";
        private const string BatteryTextId = "field.subway.cctv_close.battery";

        /// <summary>돌아가기를 눌렀다. 화면 스택을 아는 쪽이 닫는다.</summary>
        public Action Closed;

        protected override void Awake()
        {
            base.Awake();
            if (_scratchButton != null) _scratchButton.onClick.AddListener(() => ShowLine(ScratchTextId));
            if (_batteryButton != null) _batteryButton.onClick.AddListener(() => ShowLine(BatteryTextId));
            if (_closeButton != null) _closeButton.onClick.AddListener(() => Closed?.Invoke());
        }

        protected override void OnOpen()
        {
            ShowLine(HintTextId);
        }

        private void ShowLine(string id)
        {
            if (_lineText == null) return;
            _lineText.text = ServiceRegistry.TryGet<LocalizationService>(out var loc) ? loc.Get(id) : id;
        }
    }
}
