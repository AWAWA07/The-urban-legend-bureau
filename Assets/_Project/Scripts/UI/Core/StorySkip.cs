using System;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 이야기 건너뛰기(스킵) 단추.
    ///
    /// 처음 이야기(사무실에서 한영을 만나 컴퓨터를 보고 부서 설명을 듣기까지)에만 보인다.
    /// 누르면 대사를 하나씩 넘기지 않고 곧바로 첫 현장(승강장)으로 넘어간다. 중간의 선택지도 건너뛴다.
    /// 무엇을 건너뛸지는 이야기를 맡은 쪽(TutorialDirector)이 정해 Handler 에 걸어 둔다.
    ///
    /// 단추는 넘기기 판의 자식이라 대사가 떠 있을 때만 화면에 있다.
    /// 그 가운데서도 건너뛸 수 있을 때만 보이고 눌린다. 숙소 대화처럼 같은 화면을 다른 곳에서 쓸 때는 숨는다.
    /// </summary>
    public class StorySkip : MonoBehaviour
    {
        [SerializeField] private Button _button;

        /// <summary>지금 건너뛸 수 있는가. 이야기를 맡은 쪽이 건다. 비어 있으면 늘 숨는다.</summary>
        public static Func<bool> CanSkip;

        /// <summary>건너뛰기. 이야기를 맡은 쪽이 건다.</summary>
        public static Action Skip;

        private CanvasGroup _group;

        private void Awake()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) _button.onClick.AddListener(OnClicked);

            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (_group == null) return;
            bool on = CanSkip != null && CanSkip();
            _group.alpha = on ? 1f : 0f;
            _group.interactable = on;
            _group.blocksRaycasts = on;
        }

        private void OnClicked()
        {
            if (CanSkip == null || !CanSkip()) return;
            Skip?.Invoke();
        }
    }
}
