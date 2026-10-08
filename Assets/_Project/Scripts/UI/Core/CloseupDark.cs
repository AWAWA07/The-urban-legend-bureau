using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 정전 뒤의 가까이 보기 화면. 현장처럼 거의 새까맣고, 휴대폰 라이트가 비추는 둥근 자리만 조금 보인다.
    /// 라이트는 마우스를 따라간다. 비춰 가며 살필 곳을 찾는다.
    ///
    /// 화면을 다 덮는 검은 판에 구멍 하나가 뚫린 그림이다. 판은 화면보다 훨씬 커서 구멍이 어디로 가도 가장자리가 드러나지 않는다.
    /// 누르는 것은 막지 않는다(raycastTarget 끔). 대사 띠와 돌아가기 단추는 이 판보다 앞에 있다.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class CloseupDark : MonoBehaviour
    {
        [Tooltip("빛 둘레의 반지름(화면 픽셀, 1920 기준).")]
        [SerializeField] private float _radius = 260f;

        [Tooltip("빛 한가운데의 어둠. 현장의 휴대폰 라이트와 같다.")]
        [SerializeField] private float _innerAlpha = 0.72f;

        [Tooltip("빛 밖의 어둠. 현장과 같다.")]
        [SerializeField] private float _outerAlpha = 0.995f;

        private RawImage _image;
        private RectTransform _rect;

        private void Awake()
        {
            _image = GetComponent<RawImage>();
            _rect = (RectTransform)transform;
            _image.raycastTarget = false;
            _image.texture = MakeTexture();
            _image.color = Color.white;
        }

        /// <summary>판 크기는 빛 반지름의 몇 배인가. 구멍이 화면 끝으로 가도 판이 화면을 다 덮을 만큼 크다.</summary>
        private const float Span = 16f;

        private Texture2D MakeTexture()
        {
            const int W = 2048, H = 2048;   // 구멍 가장자리가 계단지지 않게 넉넉히
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            float r = W / Span;   // 구멍 반지름(픽셀)
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x + 0.5f - W * 0.5f) / r, dy = (y + 0.5f - H * 0.5f) / r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Lerp(_innerAlpha, _outerAlpha, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, d)));
                px[y * W + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private void OnEnable()
        {
            _rect = (RectTransform)transform;
            _rect.sizeDelta = Vector2.one * _radius * Span;
            Follow();
        }

        private void LateUpdate()
        {
            Follow();
        }

        private void Follow()
        {
            var pointer = Pointer.current;
            var parent = _rect.parent as RectTransform;
            if (pointer == null || parent == null) return;
            var canvas = GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, pointer.position.ReadValue(), cam, out var local))
                _rect.localPosition = local;
        }
    }
}
