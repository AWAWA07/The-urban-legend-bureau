using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 창밖에 내리는 비. 창 유리(자식 "Glass") 안에서만 가는 빗줄기가 비스듬히 떨어지고,
    /// 유리에 맺힌 물방울 몇 개가 천천히 흘러내린다. 창살과 커튼보다 뒤, 유리보다 앞에 그린다.
    /// 그림 파일 없이 유리와 같은 흰 네모 그림을 늘려 쓴다.
    /// </summary>
    public class WindowRain : MonoBehaviour
    {
        public int StreakCount = 46;
        public int DropCount = 9;
        public float FallSpeed = 9f;
        public float Slant = 0.18f;                                  // 떨어지며 옆으로 흐르는 만큼(아래로 1 갈 때)
        public Color StreakColor = new Color(0.70f, 0.78f, 0.95f, 0.22f);
        public Color DropColor = new Color(0.80f, 0.86f, 1f, 0.35f);

        private Vector2 _half;
        private Transform[] _streaks;
        private float[] _speeds;
        private Transform[] _drops;
        private float[] _dropSpeeds;
        private float[] _dropWait;

        private void Awake()
        {
            var glass = transform.Find("Glass");
            var glassRenderer = glass != null ? glass.GetComponent<SpriteRenderer>() : null;
            if (glassRenderer == null) { enabled = false; return; }

            _half = glassRenderer.size * 0.5f;
            var sprite = glassRenderer.sprite;
            int layer = glassRenderer.sortingLayerID;
            int order = glassRenderer.sortingOrder + 1;

            var root = new GameObject("Rain").transform;
            root.SetParent(transform, false);
            root.localPosition = glass.localPosition;

            // 빗줄기. 길이와 빠르기를 조금씩 달리해 멀고 가까운 비가 섞여 보이게 한다.
            _streaks = new Transform[StreakCount];
            _speeds = new float[StreakCount];
            for (int i = 0; i < StreakCount; i++)
            {
                float near = Random.value;
                var sr = MakePiece(root, "Streak_" + i, sprite, layer, order,
                    new Vector2(Mathf.Lerp(0.012f, 0.026f, near), Mathf.Lerp(0.22f, 0.5f, near)),
                    new Color(StreakColor.r, StreakColor.g, StreakColor.b, StreakColor.a * Mathf.Lerp(0.5f, 1f, near)));
                sr.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan(Slant) * Mathf.Rad2Deg);
                _streaks[i] = sr.transform;
                _speeds[i] = FallSpeed * Mathf.Lerp(0.7f, 1.15f, near);
                _streaks[i].localPosition = new Vector3(Random.Range(-_half.x, _half.x), Random.Range(-_half.y, _half.y), 0f);
            }

            // 유리에 맺혀 흐르는 물방울. 잠깐 멈췄다가 주르륵 내려간다.
            _drops = new Transform[DropCount];
            _dropSpeeds = new float[DropCount];
            _dropWait = new float[DropCount];
            for (int i = 0; i < DropCount; i++)
            {
                var sr = MakePiece(root, "Drop_" + i, sprite, layer, order, new Vector2(0.045f, 0.07f), DropColor);
                _drops[i] = sr.transform;
                ResetDrop(i, true);
            }
        }

        private static SpriteRenderer MakePiece(Transform parent, string name, Sprite sprite, int layer, int order, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.color = color;
            sr.sortingLayerID = layer;
            sr.sortingOrder = order;
            return sr;
        }

        private void ResetDrop(int i, bool anywhere)
        {
            float y = anywhere ? Random.Range(-_half.y, _half.y) : Random.Range(0f, _half.y);
            _drops[i].localPosition = new Vector3(Random.Range(-_half.x * 0.95f, _half.x * 0.95f), y, 0f);
            _dropSpeeds[i] = 0f;
            _dropWait[i] = Random.Range(0.5f, 3.5f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _streaks.Length; i++)
            {
                var p = _streaks[i].localPosition;
                p.y -= _speeds[i] * dt;
                p.x -= _speeds[i] * Slant * dt;
                if (p.y < -_half.y || p.x < -_half.x)
                {
                    // 위로 되돌려 다시 떨어뜨린다. 기울어진 만큼 오른쪽으로 조금 더 넓게 뿌린다.
                    p.y = _half.y + Random.Range(0f, 0.4f);
                    p.x = Random.Range(-_half.x, _half.x + _half.y * 2f * Slant);
                }
                _streaks[i].localPosition = p;
                // 유리 가장자리에 걸쳐 창틀 위로 삐져나오는 줄기는 숨긴다.
                _streaks[i].gameObject.SetActive(p.y <= _half.y - 0.26f && p.y >= -_half.y + 0.26f && p.x <= _half.x - 0.06f && p.x >= -_half.x + 0.06f);
            }

            for (int i = 0; i < _drops.Length; i++)
            {
                if (_dropWait[i] > 0f) { _dropWait[i] -= dt; continue; }
                _dropSpeeds[i] = Mathf.Min(_dropSpeeds[i] + dt * 1.2f, 1.1f);
                var p = _drops[i].localPosition;
                p.y -= _dropSpeeds[i] * dt;
                if (Random.value < dt * 0.8f) _dropWait[i] = Random.Range(0.1f, 0.6f);   // 걸렸다가 다시 흐른다
                _drops[i].localPosition = p;
                if (p.y < -_half.y + 0.04f) ResetDrop(i, false);
            }
        }
    }
}
