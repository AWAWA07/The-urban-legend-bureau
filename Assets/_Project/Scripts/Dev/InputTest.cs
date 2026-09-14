using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;

namespace UrbanLegendBureau.Dev
{
    /// <summary>
    /// 2단계 입력 시스템 검증용 컴포넌트. 개발 확인이 끝나면 제거해도 된다.
    /// Dev_InputTest 씬에서만 사용하며 게임 로직에는 관여하지 않는다.
    /// </summary>
    public class InputTest : MonoBehaviour, IPointerInteractable
    {
        [SerializeField] private float _holdLogInterval = 0.5f;
        [SerializeField] private float _longPressThreshold = 0.6f;

        private InputService _input;
        private float _nextHoldLogTime;
        private bool _longPressReported;

        private void Start()
        {
            if (!ServiceRegistry.TryGet(out _input))
            {
                Debug.LogError("[InputTest] InputService를 찾지 못했다. 씬에 GameRoot가 있는지 확인할 것.");
                enabled = false;
                return;
            }

            Debug.Log($"[InputTest] 시작 | 입력 모드: {_input.CurrentMode} | 마우스를 클릭하거나 화면을 터치할 것.");
        }

        private void Update()
        {
            if (_input == null || !_input.IsReady) return;

            var pointer = _input.Pointer;
            var context = pointer.CreateContext();

            if (pointer.Pressed)
            {
                _longPressReported = false;
                _nextHoldLogTime = _holdLogInterval;
                PointerPressed(in context);
            }

            if (pointer.IsPressed)
            {
                if (pointer.HeldDuration >= _nextHoldLogTime)
                {
                    _nextHoldLogTime += _holdLogInterval;
                    PointerHeld(in context);
                }

                if (!_longPressReported && pointer.IsHeldLongerThan(_longPressThreshold))
                {
                    _longPressReported = true;
                    Debug.Log($"[InputTest] 롱프레스 감지 ({_longPressThreshold}초 이상)");
                }
            }

            if (pointer.Released)
            {
                PointerReleased(in context);
            }

            if (_input.CancelPressed)
            {
                Debug.Log("[InputTest] Cancel(ESC / Android 뒤로가기) 입력");
            }
        }

        // ------------------------------------------------------- IPointerInteractable

        public void PointerPressed(in PointerContext context)
        {
            Debug.Log(
                $"[InputTest] PRESSED   모드={_input.CurrentMode}  " +
                $"Screen={Format(context.ScreenPosition)}  World={Format(context.WorldPosition)}  " +
                $"UI위={context.IsOverUI}");
        }

        public void PointerHeld(in PointerContext context)
        {
            Debug.Log($"[InputTest] HELD      {context.HeldDuration:F2}초  Screen={Format(context.ScreenPosition)}");
        }

        public void PointerReleased(in PointerContext context)
        {
            Debug.Log(
                $"[InputTest] RELEASED  누른시간={context.HeldDuration:F2}초  " +
                $"Screen={Format(context.ScreenPosition)}  UI위={context.IsOverUI}");
        }

        private static string Format(Vector2 v) => $"({v.x:F0}, {v.y:F0})";
    }
}
