using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UrbanLegendBureau.Dev
{
    /// <summary>
    /// 개발용. 마우스 자리를 정해진 길을 따라 부드럽게 옮긴다(트레일러 녹화에서 휴대폰 라이트를 비추는 손짓).
    /// 실제 커서는 움직이지 않고 입력 장치의 값만 바꾼다. 입력이 갱신될 때마다(실제 커서 값으로 덮이므로) 다시 넣는다.
    /// 게임 쪽은 평소처럼 Pointer.current 를 읽는다.
    /// </summary>
    public class PointerPath : MonoBehaviour
    {
        private static bool _holding;
        private static Vector2 _screen;

        /// <summary>월드 좌표 points 를 차례로, 구간마다 seconds 초에 걸쳐 지난다. 구간 사이에 hold 초 머문다. 끝나면 linger 초 더 붙잡는다.</summary>
        public static void Play(Vector2[] points, float[] seconds, float[] hold = null, float linger = 2f)
        {
            var go = new GameObject("PointerPath");
            go.AddComponent<PointerPath>().StartCoroutine(Run(go, points, seconds, hold, linger));
        }

        private static void Apply()
        {
            var mouse = Mouse.current;
            if (_holding && mouse != null) InputState.Change(mouse.position, _screen);
        }

        private static IEnumerator Run(GameObject owner, Vector2[] points, float[] seconds, float[] hold, float linger)
        {
            var cam = Camera.main;
            if (Mouse.current == null || cam == null || points == null || points.Length == 0) { Destroy(owner); yield break; }

            Vector2 ToScreen(Vector2 world) => cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
            _screen = ToScreen(points[0]);
            _holding = true;
            InputSystem.onAfterUpdate += Apply;
            Apply();
            for (int i = 1; i < points.Length; i++)
            {
                float d = seconds != null && i - 1 < seconds.Length ? seconds[i - 1] : 1f;
                Vector2 a = points[i - 1], b = points[i];
                for (float e = 0f; e < d; e += Time.deltaTime)
                {
                    _screen = ToScreen(Vector2.Lerp(a, b, Mathf.SmoothStep(0f, 1f, e / d)));
                    Apply();
                    yield return null;
                }
                _screen = ToScreen(b);
                float h = hold != null && i - 1 < hold.Length ? hold[i - 1] : 0f;
                if (h > 0f) yield return new WaitForSeconds(h);
            }
            if (linger > 0f) yield return new WaitForSeconds(linger);
            _holding = false;
            InputSystem.onAfterUpdate -= Apply;
            Destroy(owner);
        }
    }
}
