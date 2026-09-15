using UnityEngine;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 모든 전체 화면 UI의 기반.
    /// 실제 화면(타이틀, 브라우저, 단서 보드 등)은 이 클래스를 상속해 만든다.
    /// 상속하지 않고 그대로 붙여 쓸 수도 있다.
    ///
    /// 열고 닫는 주체는 UIService다. 외부에서 Open/Close를 직접 부르지 않는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIScreen : MonoBehaviour
    {
        [Header("식별")]
        [Tooltip("스택 로그와 조회에 쓰는 ID. 비워 두면 게임오브젝트 이름을 쓴다.")]
        [SerializeField] private string _screenId;

        [Header("동작")]
        [SerializeField] private UILayer _layer = UILayer.Screen;

        [Tooltip("켜면 이 화면이 열릴 때 아래 화면을 숨긴다. 팝업은 꺼 둔다.")]
        [SerializeField] private bool _hidesUnderlying = true;

        [Tooltip("끄면 ESC / 뒤로가기로 닫히지 않는다. 진행 중 차단 화면에 사용한다.")]
        [SerializeField] private bool _closableByBack = true;

        [Tooltip("켜면 이 화면이 열려 있어도 아래 화면을 그대로 쓸 수 있다. " +
                 "아래 화면 위에 대사만 얹는 경우에 쓴다. 기본은 꺼 둔다.")]
        [SerializeField] private bool _keepsUnderlyingUsable;

        private CanvasGroup _canvasGroup;

        public string ScreenId => string.IsNullOrEmpty(_screenId) ? name : _screenId;
        public UILayer Layer => _layer;
        public bool HidesUnderlying => _hidesUnderlying;
        public bool ClosableByBack => _closableByBack;
        public bool KeepsUnderlyingUsable => _keepsUnderlyingUsable;
        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 씬에 배치된 상태 그대로 열려 있지 않도록 닫힌 상태에서 시작한다.
            gameObject.SetActive(false);
            IsOpen = false;
        }

        // ------------------------------------------------------------- UIService 전용

        internal void Open()
        {
            if (IsOpen) return;

            gameObject.SetActive(true);
            SetInteractable(true);
            IsOpen = true;

            OnOpen();
        }

        internal void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            OnClose();

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 다른 화면에 덮였을 때. 숨기거나 입력만 막는다.
        /// keepUsable 이면 덮여 있어도 입력을 막지 않는다. 위 화면이 대사만 얹는 경우다.
        /// </summary>
        internal void SetCovered(bool covered, bool hide, bool keepUsable = false)
        {
            if (!IsOpen) return;

            if (hide)
            {
                gameObject.SetActive(!covered);
            }
            else
            {
                gameObject.SetActive(true);
                SetInteractable(!covered || keepUsable);
            }

            OnCoveredChanged(covered);
        }

        private void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
        }

        // ------------------------------------------------------------- 상속 지점

        /// <summary>화면이 열린 직후. 여기서 텍스트와 데이터를 채운다.</summary>
        protected virtual void OnOpen() { }

        /// <summary>화면이 닫히기 직전. 여기서 구독을 해제한다.</summary>
        protected virtual void OnClose() { }

        /// <summary>다른 화면에 덮이거나 다시 드러났을 때.</summary>
        protected virtual void OnCoveredChanged(bool covered) { }

        // ------------------------------------------------------------- 편의

        /// <summary>스스로 닫는다. 화면 안의 닫기 버튼에서 사용한다.</summary>
        public void RequestClose()
        {
            if (ServiceRegistry.TryGet<UIService>(out var ui))
            {
                ui.Close(this);
            }
        }
    }
}
