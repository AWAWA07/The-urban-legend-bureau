using UnityEngine;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 걷는 사람의 발소리. 걸은 거리가 한 걸음(Stride)만큼 쌓일 때마다 저벅 한다.
    /// 발소리 넷(step_0~3) 가운데 하나를 골라 높낮이를 조금씩 달리해 기계적으로 들리지 않게 한다.
    /// 거리는 부모 기준 자리(localPosition)로 잰다. 달리는 열차가 객실을 흔들어도 서 있는 사람은 발소리가 나지 않는다.
    /// 한 번에 크게 옮겨진 것(장소를 바꿀 때 자리를 다시 잡는 것)은 걸은 것으로 치지 않는다.
    /// </summary>
    public class Footsteps : MonoBehaviour
    {
        public float Volume = 0.3f;
        public float Stride = 0.9f;

        private float _lastX;
        private float _travelled;

        private bool _indoor;   // 숙소 방 안. 구두 대신 실내에서 걷는 부드러운 발소리(step_soft_0~3)를 낸다. 뒤따르는 사람도 같은 크기로 들린다

        private void OnEnable()
        {
            _indoor = GetComponentInParent<RoomNight>(true) != null;

            // 걷기 그림이 한 바퀴에 두 걸음을 딛으니 발소리도 그 반 거리마다 낸다. 그림보다 소리가 잦으면 종종걸음처럼 들린다.
            // 그림은 월드 거리로, 발소리는 부모 기준 거리로 재므로 부모의 크기로 나눠 맞춘다.
            var look = GetComponent<FieldSpriteAnimator>();
            if (look != null)
            {
                float scale = transform.parent != null ? Mathf.Abs(transform.parent.lossyScale.x) : 1f;
                Stride = look.CycleDistance * 0.5f / Mathf.Max(0.01f, scale);
            }
            _lastX = transform.localPosition.x;
            _travelled = Stride * 0.6f;
        }

        private void LateUpdate()
        {
            float x = transform.localPosition.x;
            float moved = Mathf.Abs(x - _lastX);
            _lastX = x;

            if (moved > 1f) return;                 // 자리를 다시 잡았다
            if (moved < 0.0001f)                    // 섰다. 다시 걸으면 첫걸음이 곧 난다
            {
                _travelled = Stride * 0.6f;
                return;
            }

            _travelled += moved;
            if (_travelled < Stride) return;
            _travelled -= Stride;
            if (_indoor) Sfx.Play("step_soft_" + Random.Range(0, 4), Mathf.Max(Volume, 0.2f) * 1.15f, Random.Range(0.95f, 1.05f));
            else Sfx.Play("step_" + Random.Range(0, 4), Volume, Random.Range(0.93f, 1.07f));
        }
    }
}
