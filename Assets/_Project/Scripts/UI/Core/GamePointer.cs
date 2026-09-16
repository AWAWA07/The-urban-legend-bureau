using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.InputSystemLayer;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 게임 안에서 그리는 마우스 화살표.
    ///
    /// 운영체제가 그려 주는 화살표를 숨기고 같은 자리에 직접 그린다.
    /// 차지한의 컴퓨터를 들여다보는 화면이라, 화살표까지 게임 안의 것이어야 그림이 맞는다.
    ///
    /// 그림 파일은 쓰지 않는다. 모양을 글자판으로 적어 두고 실행할 때 만든다.
    /// 자리는 기존 InputService 가 알려 주는 것을 그대로 쓴다. 입력 경로를 새로 만들지 않는다.
    /// </summary>
    public class GamePointer : MonoBehaviour
    {
        [Tooltip("화살표를 그릴 칸. 비워 두면 스스로 찾는다.")]
        [SerializeField] private Image _image;

        [Tooltip("화면 기준(1920x1080)에서 화살표의 높이. 창 크기가 달라져도 이만큼으로 보인다.")]
        [SerializeField] private float _designHeight = 30f;

        private InputService _input;
        private RectTransform _rect;
        private RectTransform _canvasRect;
        private Canvas _canvas;

        /// <summary>지금 화면에 떠 있는 화살표. 켜고 끄는 것은 밖에서 정한다.</summary>
        private static GamePointer _instance;

        /// <summary>
        /// 화살표를 보일지 정한다.
        /// 차지한의 컴퓨터를 들여다보는 동안에만 켠다. 그 밖에서는 운영체제 화살표를 그대로 쓴다.
        /// </summary>
        public static void SetVisible(bool visible)
        {
            if (_instance == null) return;
            _instance.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 화살표 모양. X는 테두리, O는 속, 점은 빈 곳이다.
        /// 위에서 아래로 적는다. 왼쪽 위 끝이 실제로 가리키는 지점이다.
        /// </summary>
        private static readonly string[] Shape =
        {
            "X...........",
            "XX..........",
            "XOX.........",
            "XOOX........",
            "XOOOX.......",
            "XOOOOX......",
            "XOOOOOX.....",
            "XOOOOOOX....",
            "XOOOOOOOX...",
            "XOOOOOOOOX..",
            "XOOOOOXXXXX.",
            "XOOXOOX.....",
            "XOX.XOOX....",
            "XX..XOOX....",
            "X....XOOX...",
            "......XOOX..",
            ".......XOX..",
            "........XX..",
        };

        private void Awake()
        {
            _instance = this;

            if (_image == null) _image = GetComponent<Image>();
            _rect = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvasRect = (RectTransform)_canvas.transform;

            if (_image != null)
            {
                _image.sprite = BuildSprite();
                _image.raycastTarget = false;

                // 가리키는 지점이 왼쪽 위 끝이다. 그래야 실제 화살표처럼 움직인다.
                _rect.pivot = new Vector2(0f, 1f);
                _rect.anchorMin = new Vector2(0f, 0f);
                _rect.anchorMax = new Vector2(0f, 0f);
            }

            // 켜고 끄는 것은 밖에서 정한다. 처음에는 꺼 둔다.
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            ServiceRegistry.TryGet(out _input);
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.visible = true;
        }

        private void LateUpdate()
        {
            if (_image == null || _canvasRect == null) return;

            if (_input == null) ServiceRegistry.TryGet(out _input);

            Vector2 screen = _input != null && _input.Pointer != null
                ? _input.Pointer.Position
                : Vector2.zero;

            // 화면 밖으로 나가면 그리지 않는다. 창 밖에 화살표가 남아 있으면 이상하다.
            bool inside = screen.x >= 0f && screen.y >= 0f
                          && screen.x <= Screen.width && screen.y <= Screen.height;
            if (_image.enabled != inside) _image.enabled = inside;
            if (!inside) return;

            // 화면 좌표를 캔버스 좌표로 옮긴다. 캔버스는 화면 크기에 맞춰 늘고 줄기 때문이다.
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

            // 그림 한 칸이 실제 화면에서 정확히 정수 픽셀이 되게 크기를 거꾸로 계산한다.
            // 캔버스 배율에 그냥 맡기면 칸이 소수 픽셀로 늘어나 모양이 일그러진다.
            // 배수는 원하는 크기에서 뽑는다. 창이 커지면 배수도 따라 커져 크기가 일정해 보인다.
            int step = Mathf.Max(1, Mathf.RoundToInt(_designHeight * scale / Shape.Length));
            _rect.sizeDelta = new Vector2(Shape[0].Length * step, Shape.Length * step) / scale;

            // 자리도 픽셀에 맞춰 떨어뜨린다. 반 픽셀에 걸치면 가장자리가 흐려진다.
            var snapped = new Vector2(Mathf.Round(screen.x), Mathf.Round(screen.y));
            _rect.anchoredPosition = snapped / scale;
        }

        /// <summary>글자판을 읽어 화살표 그림을 만든다. 한 번만 만들고 그대로 쓴다.</summary>
        private static Sprite BuildSprite()
        {
            int w = Shape[0].Length;
            int h = Shape.Length;

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var outline = new Color32(20, 20, 24, 255);
            var fill = new Color32(255, 255, 255, 255);
            var empty = new Color32(0, 0, 0, 0);

            for (int y = 0; y < h; y++)
            {
                // 글자판은 위에서 아래로 적었고 텍스처는 아래에서 위로 쌓인다.
                string row = Shape[h - 1 - y];
                for (int x = 0; x < w; x++)
                {
                    char c = x < row.Length ? row[x] : '.';
                    texture.SetPixel(x, y, c == 'X' ? outline : c == 'O' ? fill : empty);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0f, 1f), 100f);
        }
    }
}
