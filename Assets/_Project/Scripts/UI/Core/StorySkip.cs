using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 대사 넘기기(스킵) 단추.
    ///
    /// 누르면 지금 이어지는 대화를 끝까지 빠르게 넘긴다. 대화마다 따로 짜지 않는다.
    /// 화면에 떠 있는 "눌러서 넘기기" 판(Btn_Advance)을 한 프레임에 한 번씩 대신 눌러 줄 뿐이다.
    /// 그래서 대사 사이에 걸린 연출(인물 그림, 표정, 화면 전환)은 그대로 지나가고, 이야기 흐름도 평소와 같다.
    ///
    /// 고를 것이 뜨면 넘기기 판이 꺼지거나 눌리지 않게 되므로 거기서 저절로 멈춘다. 고르는 것은 플레이어 몫이다.
    /// 넘길 대사가 잠깐 없어도(인물이 걷는 연출 같은 것) 곧바로 멈추지 않고 조금 기다렸다가 멈춘다.
    ///
    /// 이 단추는 넘기기 판의 자식으로 둔다. 판이 켜져 있을 때만 보이고, 단추를 눌러도 판까지 눌리지 않는다.
    /// </summary>
    public class StorySkip : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(Begin);
        }

        /// <summary>넘기기를 시작한다. 이미 넘기는 중이면 그대로 둔다.</summary>
        public static void Begin()
        {
            Runner.Get().Active = true;
        }

        /// <summary>지금 넘기는 중인가.</summary>
        public static bool IsSkipping => _runner != null && _runner.Active;

        private static Runner _runner;

        /// <summary>장면이 바뀌어도 남는 보이지 않는 일꾼. 넘기는 동안 매 프레임 판을 눌러 준다.</summary>
        private class Runner : MonoBehaviour
        {
            /// <summary>눌러 줄 판이 이만큼(초) 없으면 대화가 끝난 것으로 보고 멈춘다.</summary>
            private const float IdleStop = 0.6f;

            private bool _active;
            private float _idle;

            public bool Active
            {
                get => _active;
                set { _active = value; _idle = 0f; }
            }

            public static Runner Get()
            {
                if (_runner != null) return _runner;
                var go = new GameObject("[StorySkip]");
                go.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
                return _runner;
            }

            private void Update()
            {
                if (!_active) return;

                var target = FindAdvance();
                if (target == null)
                {
                    _idle += Time.unscaledDeltaTime;
                    if (_idle >= IdleStop) _active = false;
                    return;
                }

                _idle = 0f;
                target.onClick.Invoke();
            }

            /// <summary>
            /// 지금 눌러 줄 넘기기 판. 켜져 있고 눌릴 수 있는 것 가운데 가장 위에 그려지는 것을 고른다.
            /// 전신 대화가 현장 위에 떠 있으면 전신 대화 쪽을 누른다.
            /// </summary>
            private static Button FindAdvance()
            {
                Button best = null;
                int bestOrder = int.MinValue;
                foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
                {
                    if (b == null || b.name != "Btn_Advance") continue;
                    if (!b.isActiveAndEnabled || !b.interactable) continue;

                    var group = b.GetComponentInParent<CanvasGroup>();
                    if (group != null && (!group.interactable || !group.blocksRaycasts || group.alpha <= 0.01f)) continue;

                    var parentCanvas = b.GetComponentInParent<Canvas>();
                    var canvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
                    int order = canvas != null ? canvas.sortingOrder * 10000 : 0;
                    order += b.transform.GetSiblingIndex();
                    var screen = b.GetComponentInParent<UIScreen>();
                    if (screen != null) order += screen.transform.GetSiblingIndex() * 100;

                    if (order > bestOrder)
                    {
                        bestOrder = order;
                        best = b;
                    }
                }
                return best;
            }
        }
    }
}
