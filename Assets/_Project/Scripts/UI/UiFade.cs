using System;
using System.Collections;
using UnityEngine;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 잠깐 떴다가 옅어지며 사라지는 안내.
    ///
    /// 맞았다 틀렸다 같은 말은 읽고 나면 그만이다. 화면에 계속 붙어 있으면
    /// 다음에 한 일의 결과인지 아까 것인지 알 수 없게 되고, 자리만 차지한다.
    /// 그렇다고 곧바로 사라지면 눈에 남지 않으므로, 잠시 두었다가 옅어지게 한다.
    ///
    /// 알림, 추론 결과, 검열 결과가 모두 이 규칙을 쓴다. 한 곳에서 정해 두어야 모습이 고르다.
    /// </summary>
    public static class UiFade
    {
        /// <summary>떠 있는 시간(초). 한 줄을 읽기에 넉넉한 만큼.</summary>
        public const float Hold = 2.4f;

        /// <summary>옅어지는 데 걸리는 시간(초).</summary>
        public const float Fade = 0.55f;

        /// <summary>
        /// 대상을 잠시 보여 주었다가 옅어지며 감춘다.
        ///
        /// 이미 돌고 있는 것이 있으면 넘겨 준 쪽에서 멈추고 다시 부르면 된다.
        /// 시간은 실시간으로 센다. 멈춰 있는 화면 위에서도 사라져야 한다.
        /// </summary>
        public static IEnumerator Play(GameObject target, Action onDone = null,
            float hold = Hold, float fade = Fade)
        {
            var group = Group(target);
            if (group == null) { onDone?.Invoke(); yield break; }

            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);

            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - (t / fade);
                yield return null;
            }

            group.alpha = 0f;
            onDone?.Invoke();
        }

        /// <summary>옅어짐을 다루려면 CanvasGroup 이 있어야 한다. 없으면 붙인다.</summary>
        public static CanvasGroup Group(GameObject target)
        {
            if (target == null) return null;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            return group;
        }

        /// <summary>다시 보이게 되돌린다. 새 안내를 띄우기 전에 부른다.</summary>
        public static void Show(GameObject target)
        {
            var group = Group(target);
            if (group != null) group.alpha = 1f;
        }
    }
}
