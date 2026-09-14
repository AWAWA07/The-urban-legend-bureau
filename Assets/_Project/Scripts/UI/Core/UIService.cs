using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 화면 스택 관리자. 화면을 열고 닫는 유일한 창구다.
    ///
    /// 뒤로가기: InputService의 Cancel 액션 하나에 PC의 ESC와 Android 뒤로가기가
    /// 함께 바인딩되어 있으므로, 여기서는 플랫폼을 구분하지 않고 Pop만 한다.
    /// </summary>
    public class UIService : IService, ITickable
    {
        private readonly List<UIScreen> _stack = new List<UIScreen>();

        private UIRoot _root;
        private InputService _input;

        /// <summary>현재 최상단 화면. 없으면 null.</summary>
        public UIScreen Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

        public int Count => _stack.Count;

        public bool IsReady => _root != null;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _root = UIRoot.Create();
            ServiceRegistry.TryGet(out _input);

            Debug.Log($"[UIService] 준비 완료 | 레이어 Canvas {System.Enum.GetValues(typeof(UILayer)).Length}개 | 기준 {ScreenSetup.ReferenceWidth}x{ScreenSetup.ReferenceHeight} Match 0.5");
        }

        public void Shutdown()
        {
            CloseAll();
            _stack.Clear();
            _input = null;

            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
            _root = null;
        }

        public void Tick(float deltaTime)
        {
            if (_input == null || !_input.IsReady) return;

            // ESC(PC) / 뒤로가기(Android) / Cancel(게임패드) 가 모두 같은 액션으로 들어온다.
            if (_input.CancelPressed)
            {
                Back();
            }
        }

        // ------------------------------------------------------------- 스택 조작

        /// <summary>화면을 스택 위에 올린다.</summary>
        public bool Push(UIScreen screen)
        {
            if (!ValidateReady() || screen == null) return false;

            if (_stack.Contains(screen))
            {
                Debug.LogWarning($"[UIService] '{screen.ScreenId}' 는 이미 스택에 있다. 무시한다.");
                return false;
            }

            AttachToLayer(screen);

            // 새 화면이 아래를 가리는 종류라면 기존 최상단을 덮는다.
            var previous = Current;
            if (previous != null)
            {
                previous.SetCovered(true, screen.HidesUnderlying);
            }

            _stack.Add(screen);
            screen.Open();

            LogStack($"Push({screen.ScreenId})");
            return true;
        }

        /// <summary>최상단 화면을 닫는다.</summary>
        public bool Pop()
        {
            if (!ValidateReady()) return false;

            if (_stack.Count == 0)
            {
                Debug.Log("[UIService] 스택이 비어 있어 Pop을 수행하지 않는다.");
                return false;
            }

            var top = _stack[_stack.Count - 1];
            _stack.RemoveAt(_stack.Count - 1);
            top.Close();

            // 아래 화면을 되살린다.
            var next = Current;
            if (next != null)
            {
                next.SetCovered(false, top.HidesUnderlying);
            }

            LogStack($"Pop({top.ScreenId})");
            return true;
        }

        /// <summary>최상단 화면을 닫고 새 화면으로 바꾼다. 스택 깊이는 그대로다.</summary>
        public bool Replace(UIScreen screen)
        {
            if (!ValidateReady() || screen == null) return false;

            if (_stack.Count > 0)
            {
                var top = _stack[_stack.Count - 1];
                _stack.RemoveAt(_stack.Count - 1);
                top.Close();
            }

            AttachToLayer(screen);

            var previous = Current;
            if (previous != null)
            {
                previous.SetCovered(true, screen.HidesUnderlying);
            }

            _stack.Add(screen);
            screen.Open();

            LogStack($"Replace({screen.ScreenId})");
            return true;
        }

        /// <summary>ESC / 뒤로가기 처리. 최상단이 닫을 수 없는 화면이면 무시한다.</summary>
        public bool Back()
        {
            var top = Current;
            if (top == null) return false;

            if (!top.ClosableByBack)
            {
                Debug.Log($"[UIService] '{top.ScreenId}' 는 뒤로가기로 닫을 수 없다.");
                return false;
            }

            return Pop();
        }

        /// <summary>특정 화면을 닫는다. 최상단이 아니면 스택에서 제거만 한다.</summary>
        public bool Close(UIScreen screen)
        {
            if (screen == null) return false;

            int index = _stack.IndexOf(screen);
            if (index < 0) return false;

            if (index == _stack.Count - 1)
            {
                return Pop();
            }

            _stack.RemoveAt(index);
            screen.Close();
            LogStack($"Close({screen.ScreenId})");
            return true;
        }

        /// <summary>모든 화면을 닫는다.</summary>
        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                // 씬 언로드나 플레이 종료 중에는 화면이 이미 파괴되어 있을 수 있다.
                // Unity의 == null 은 파괴된 오브젝트도 참으로 판정한다.
                var screen = _stack[i];
                if (screen == null) continue;

                screen.Close();
            }
            _stack.Clear();
        }

        public bool Contains(UIScreen screen) => screen != null && _stack.Contains(screen);

        // ------------------------------------------------------------- 스택 밖 화면

        /// <summary>
        /// 스택에 넣지 않고 연다. 상시 표시되는 HUD처럼 다른 화면에 덮이면 안 되는 UI에 쓴다.
        /// 스택 밖이므로 Pop이나 뒤로가기의 영향을 받지 않는다.
        /// </summary>
        public bool ShowDetached(UIScreen screen)
        {
            if (!ValidateReady() || screen == null) return false;

            if (_stack.Contains(screen))
            {
                Debug.LogWarning($"[UIService] '{screen.ScreenId}' 는 스택에 있다. ShowDetached 대상이 아니다.");
                return false;
            }

            AttachToLayer(screen);
            screen.Open();
            return true;
        }

        /// <summary>ShowDetached로 연 화면을 닫는다.</summary>
        public bool HideDetached(UIScreen screen)
        {
            if (screen == null || _stack.Contains(screen)) return false;

            screen.Close();
            return true;
        }

        /// <summary>스택 상태 문자열. 디버그 표시용.</summary>
        public string DescribeStack()
        {
            if (_stack.Count == 0) return "(비어 있음)";

            var sb = new StringBuilder();
            for (int i = 0; i < _stack.Count; i++)
            {
                if (i > 0) sb.Append(" > ");
                sb.Append(_stack[i].ScreenId);
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------- 내부

        private void AttachToLayer(UIScreen screen)
        {
            var parent = _root.GetLayerParent(screen.Layer);
            if (parent == null)
            {
                Debug.LogError($"[UIService] 레이어 {screen.Layer} 의 부모를 찾지 못했다.");
                return;
            }

            if (screen.transform.parent == parent) return;

            screen.transform.SetParent(parent, false);

            // 레이어 전체를 채우도록 맞춘다.
            var rt = (RectTransform)screen.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private bool ValidateReady()
        {
            if (IsReady) return true;
            Debug.LogError("[UIService] 초기화되지 않았다.");
            return false;
        }

        private void LogStack(string action)
        {
            Debug.Log($"[UIService] {action} -> 스택({_stack.Count}): {DescribeStack()}");
        }
    }
}
