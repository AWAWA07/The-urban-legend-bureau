using UnityEngine;
using UnityEngine.InputSystem;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.InputSystemLayer
{
    /// <summary>
    /// Input System 액션 자산을 감싸는 유일한 창구.
    /// 장치 분기는 액션 바인딩이 처리하므로 여기에도 플랫폼 #if 는 없다.
    ///
    /// 사용 액션 (기존 InputSystem_Actions 자산):
    ///   UI/Point   - Mouse/position, Pen/position, Touchscreen/touch*/position
    ///   UI/Click   - Mouse/leftButton, Pen/tip, Touchscreen/touch*/press
    ///   UI/Cancel  - Escape, Gamepad buttonEast  (Android 뒤로가기는 Escape로 매핑된다)
    ///   Player/Move, Player/Interact - 이후 단계에서 사용
    /// </summary>
    public class InputService : IService, ITickable
    {
        private const string UiMapName = "UI";
        private const string PlayerMapName = "Player";

        private readonly InputActionAsset _asset;

        private InputActionMap _uiMap;
        private InputActionMap _playerMap;

        private InputAction _pointAction;
        private InputAction _clickAction;
        private InputAction _cancelAction;
        private InputAction _moveAction;
        private InputAction _interactAction;

        public InputService(InputActionAsset asset)
        {
            _asset = asset;
        }

        /// <summary>마우스/터치 통합 포인터. 게임 코드는 이것만 본다.</summary>
        public PointerInput Pointer { get; private set; }

        /// <summary>마지막으로 사용된 입력 장치 종류.</summary>
        public InputMode CurrentMode { get; private set; } = InputMode.Pointer;

        public bool IsReady { get; private set; }

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            if (_asset == null)
            {
                Debug.LogError("[InputService] InputActionAsset이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                Pointer = new PointerInput(null, null);
                return;
            }

            _uiMap = _asset.FindActionMap(UiMapName, false);
            _playerMap = _asset.FindActionMap(PlayerMapName, false);

            if (_uiMap == null)
            {
                Debug.LogError($"[InputService] '{UiMapName}' 액션 맵을 찾지 못했다.");
                Pointer = new PointerInput(null, null);
                return;
            }

            _pointAction = _uiMap.FindAction("Point", false);
            _clickAction = _uiMap.FindAction("Click", false);
            _cancelAction = _uiMap.FindAction("Cancel", false);

            if (_playerMap != null)
            {
                _moveAction = _playerMap.FindAction("Move", false);
                _interactAction = _playerMap.FindAction("Interact", false);
            }

            _uiMap.Enable();
            _playerMap?.Enable();

            Pointer = new PointerInput(_pointAction, _clickAction);

            // 초기 모드는 현재 붙어 있는 장치로 추정한다.
            CurrentMode = DetectInitialMode();
            InputSystem.onActionChange += OnActionChange;

            IsReady = true;
            Debug.Log($"[InputService] 준비 완료 | 모드: {CurrentMode} | Point={_pointAction != null} Click={_clickAction != null} Cancel={_cancelAction != null}");
        }

        public void Shutdown()
        {
            InputSystem.onActionChange -= OnActionChange;

            _uiMap?.Disable();
            _playerMap?.Disable();

            Pointer?.Reset();
            IsReady = false;
        }

        public void Tick(float deltaTime)
        {
            Pointer?.Tick(deltaTime);
        }

        // ------------------------------------------------------------- 조회 API

        /// <summary>이동 입력(WASD / 방향키 / 게임패드 스틱). 이후 단계에서 사용한다.</summary>
        public Vector2 MoveValue => _moveAction != null && _moveAction.enabled
            ? _moveAction.ReadValue<Vector2>()
            : Vector2.zero;

        /// <summary>상호작용 키가 이번 프레임에 눌렸는가.</summary>
        public bool InteractPressed => _interactAction != null && _interactAction.WasPressedThisFrame();

        /// <summary>취소 / 뒤로가기. PC의 ESC와 Android 뒤로가기 버튼이 같은 액션으로 들어온다.</summary>
        public bool CancelPressed => _cancelAction != null && _cancelAction.WasPressedThisFrame();

        /// <summary>씬 전환 후 메인 카메라가 바뀌었을 때 호출한다.</summary>
        public void OnSceneChanged()
        {
            Pointer?.InvalidateCamera();
            Pointer?.Reset();
        }

        // ------------------------------------------------------------- 장치 감지

        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed) return;
            if (obj is not InputAction action) return;

            var device = action.activeControl?.device;
            if (device == null) return;

            var mode = ClassifyDevice(device);
            if (mode == CurrentMode) return;

            CurrentMode = mode;
            ApplyCursorPolicy();
        }

        private InputMode DetectInitialMode()
        {
            if (Touchscreen.current != null && Mouse.current == null) return InputMode.Touch;
            if (Gamepad.current != null && Mouse.current == null && Touchscreen.current == null) return InputMode.Gamepad;
            return InputMode.Pointer;
        }

        private static InputMode ClassifyDevice(InputDevice device)
        {
            if (device is Touchscreen) return InputMode.Touch;
            if (device is Gamepad) return InputMode.Gamepad;
            return InputMode.Pointer;
        }

        private void ApplyCursorPolicy()
        {
            // 터치 기기에서는 커서가 의미 없다. 플랫폼 분기가 아니라 '현재 입력 모드' 기준으로 판단한다.
            Cursor.visible = CurrentMode != InputMode.Touch;
        }
    }
}
