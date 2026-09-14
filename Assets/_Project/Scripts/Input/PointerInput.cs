using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UrbanLegendBureau.InputSystemLayer
{
    /// <summary>
    /// 마우스 / 터치 / 펜을 하나의 포인터로 통합해 노출한다.
    /// 게임 코드는 이 클래스만 보고, 어떤 장치인지 알 필요가 없다.
    ///
    /// 갱신은 InputService가 매 프레임 Tick으로 수행한다. 외부에서 Tick을 호출하지 않는다.
    /// </summary>
    public class PointerInput
    {
        // UI 레이캐스트 결과 버퍼. 매 프레임 재사용해 할당을 만들지 않는다.
        private static readonly List<RaycastResult> UiRaycastResults = new List<RaycastResult>(8);

        private readonly InputAction _pointAction;
        private readonly InputAction _pressAction;

        private PointerEventData _uiEventData;
        private Camera _camera;
        private bool _wasPressed;

        public PointerInput(InputAction pointAction, InputAction pressAction)
        {
            _pointAction = pointAction;
            _pressAction = pressAction;
        }

        // ------------------------------------------------------------- 노출 값

        /// <summary>현재 포인터의 스크린 좌표. 장치가 없으면 직전 값을 유지한다.</summary>
        public Vector2 Position { get; private set; }

        /// <summary>현재 포인터의 2D 월드 좌표.</summary>
        public Vector2 WorldPosition { get; private set; }

        /// <summary>이번 프레임에 눌렸는가.</summary>
        public bool Pressed { get; private set; }

        /// <summary>이번 프레임에 떼어졌는가.</summary>
        public bool Released { get; private set; }

        /// <summary>현재 눌린 상태인가.</summary>
        public bool IsPressed { get; private set; }

        /// <summary>누르고 있는 시간(초). 떼면 0으로 돌아간다.</summary>
        public float HeldDuration { get; private set; }

        /// <summary>포인터가 UI 요소 위에 있는가. 월드 오브젝트 조사 차단에 사용한다.</summary>
        public bool IsOverUI { get; private set; }

        /// <summary>이번 프레임 이동량(스크린 좌표).</summary>
        public Vector2 Delta { get; private set; }

        /// <summary>월드 좌표 변환에 사용할 카메라. 지정하지 않으면 Camera.main을 쓴다.</summary>
        public Camera OverrideCamera { get; set; }

        // ------------------------------------------------------------- 갱신

        internal void Tick(float deltaTime)
        {
            var previousPosition = Position;

            if (_pointAction != null && _pointAction.enabled)
            {
                Position = _pointAction.ReadValue<Vector2>();
            }
            Delta = Position - previousPosition;

            bool isPressedNow = _pressAction != null && _pressAction.enabled && _pressAction.IsPressed();

            Pressed = isPressedNow && !_wasPressed;
            Released = !isPressedNow && _wasPressed;
            IsPressed = isPressedNow;
            _wasPressed = isPressedNow;

            if (isPressedNow)
            {
                HeldDuration += deltaTime;
            }
            else if (!Released)
            {
                // 뗀 프레임에는 최종 누른 시간을 그대로 유지해 수신 측이 읽을 수 있게 한다.
                HeldDuration = 0f;
            }

            IsOverUI = EvaluateIsOverUI();
            WorldPosition = ScreenToWorld(Position);
        }

        internal void Reset()
        {
            Pressed = false;
            Released = false;
            IsPressed = false;
            _wasPressed = false;
            HeldDuration = 0f;
            Delta = Vector2.zero;
        }

        // ------------------------------------------------------------- 헬퍼

        /// <summary>현재 상태의 스냅샷. IPointerInteractable 전달용.</summary>
        public PointerContext CreateContext()
        {
            return new PointerContext(Position, WorldPosition, HeldDuration, IsOverUI);
        }

        /// <summary>지정 시간 이상 누르고 있는가. 모바일 롱프레스 판정용.</summary>
        public bool IsHeldLongerThan(float seconds)
        {
            return IsPressed && HeldDuration >= seconds;
        }

        /// <summary>
        /// EventSystem.IsPointerOverGameObject() 는 UI 입력 모듈이 '직전 프레임에' 수행한
        /// 레이캐스트 결과를 돌려준다. 포인터 이동과 누름이 같은 프레임에 들어오는 터치에서는
        /// 누른 순간 판정이 한 프레임 어긋난다. 그래서 현재 좌표로 직접 레이캐스트한다.
        /// </summary>
        private bool EvaluateIsOverUI()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            _uiEventData ??= new PointerEventData(eventSystem);
            _uiEventData.Reset();
            _uiEventData.position = Position;

            UiRaycastResults.Clear();
            eventSystem.RaycastAll(_uiEventData, UiRaycastResults);

            // Physics2DRaycaster 등 월드 레이캐스터 결과가 섞일 수 있으므로 UI 그래픽만 센다.
            for (int i = 0; i < UiRaycastResults.Count; i++)
            {
                if (UiRaycastResults[i].module is GraphicRaycaster) return true;
            }

            return false;
        }

        private Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            var cam = ResolveCamera();
            if (cam == null) return Vector2.zero;

            // 2D 직교 카메라 기준. 원근 카메라여도 카메라 평면 거리만큼 밀어 변환한다.
            float depth = cam.orthographic ? Mathf.Abs(cam.transform.position.z) : cam.nearClipPlane;
            var world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
            return new Vector2(world.x, world.y);
        }

        private Camera ResolveCamera()
        {
            if (OverrideCamera != null) return OverrideCamera;
            if (_camera == null) _camera = Camera.main;
            return _camera;
        }

        /// <summary>씬 전환 등으로 카메라가 바뀌었을 때 캐시를 버린다.</summary>
        internal void InvalidateCamera()
        {
            _camera = null;
        }
    }
}
