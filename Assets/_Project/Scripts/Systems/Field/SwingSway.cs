using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>그네처럼 위쪽 꼭짓점을 축으로 천천히 흔들린다. 바람에 밀리는 쪽으로 조금 더 기운다.</summary>
    public class SwingSway : MonoBehaviour
    {
        public float Angle = 14f;
        public float Period = 2.6f;
        public float Lean = 3f;
        [Tooltip("0 이 아니면 흔들지 않고 1초에 이만큼(도) 돈다. 환풍기 날개.")]
        public float Spin;

        private void Update()
        {
            if (Spin != 0f) { transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * Spin); return; }
            float a = Mathf.Sin(Time.time * Mathf.PI * 2f / Period) * Angle + Lean;
            transform.localRotation = Quaternion.Euler(0f, 0f, a);
        }
    }
}
