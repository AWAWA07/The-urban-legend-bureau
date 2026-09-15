using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 스크롤 막대 양 끝의 화살표가 부르는 물건.
    ///
    /// 실제 창의 막대는 화살표를 누르면 한 칸씩 움직인다. 그 한 칸을 여기서 준다.
    /// 한 칸은 보이는 높이의 일부로 잡는다. 내용이 길든 짧든 손에 익은 만큼 움직인다.
    /// </summary>
    public class ScrollNudge : MonoBehaviour
    {
        [SerializeField] private ScrollRect _target;

        [Tooltip("한 번에 움직이는 양. 보이는 높이에 대한 비율이다.")]
        [SerializeField] private float _stepRatio = 0.15f;

        public void StepUp() => Step(1f);
        public void StepDown() => Step(-1f);

        private void Step(float direction)
        {
            if (_target == null || _target.content == null || _target.viewport == null) return;

            float hidden = _target.content.rect.height - _target.viewport.rect.height;
            if (hidden <= 0f) return;    // 굴러갈 것이 없다

            float step = _target.viewport.rect.height * _stepRatio / hidden;
            _target.verticalNormalizedPosition =
                Mathf.Clamp01(_target.verticalNormalizedPosition + step * direction);
        }
    }
}
