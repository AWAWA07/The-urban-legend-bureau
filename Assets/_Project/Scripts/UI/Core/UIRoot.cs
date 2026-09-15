using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 레이어별 Canvas를 만들어 들고 있는 UI 루트.
    /// 씬마다 Canvas를 다시 만들지 않도록 DontDestroyOnLoad로 유지한다.
    ///
    /// 코드로 만드는 이유: 씬마다 손으로 Canvas를 배치하면 해상도/Match 설정이
    /// 어긋나기 쉽다. 1920x1080 / Match 0.5 를 모든 플랫폼에서 강제하기 위해 한 곳에서 생성한다.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        private readonly Dictionary<UILayer, RectTransform> _layers = new Dictionary<UILayer, RectTransform>();

        public static UIRoot Create()
        {
            var go = new GameObject("UIRoot");
            DontDestroyOnLoad(go);

            var root = go.AddComponent<UIRoot>();
            root.BuildLayers();
            root.EnsureEventSystem();
            root.BuildPointer();
            return root;
        }

        /// <summary>
        /// 게임이 직접 그리는 마우스 화살표. 가장 위 레이어에 둔다.
        /// 어떤 화면보다 위에 있어야 화면에 가려지지 않는다.
        /// </summary>
        private void BuildPointer()
        {
            var parent = GetLayerParent(UILayer.System);
            if (parent == null) return;

            var go = new GameObject("GamePointer", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");

            var image = go.AddComponent<Image>();
            image.raycastTarget = false;

            go.AddComponent<GamePointer>();
            go.transform.SetAsLastSibling();
        }

        /// <summary>해당 레이어의 부모 RectTransform.</summary>
        public RectTransform GetLayerParent(UILayer layer)
        {
            return _layers.TryGetValue(layer, out var parent) ? parent : null;
        }

        private void BuildLayers()
        {
            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                _layers[layer] = BuildLayerCanvas(layer);
            }
        }

        private RectTransform BuildLayerCanvas(UILayer layer)
        {
            var go = new GameObject($"Canvas_{layer}", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = layer.SortingOrder();
            // 레이어별로 배치 순서를 고정해 다른 레이어 변경이 이 캔버스를 다시 만들지 않게 한다.
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenSetup.ReferenceWidth, ScreenSetup.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var raycaster = go.AddComponent<GraphicRaycaster>();
            // World 레이어는 월드 오브젝트 조사와 겹치므로 UI 레이캐스트를 받지 않는다.
            raycaster.enabled = layer != UILayer.World;

            return go.GetComponent<RectTransform>();
        }

        /// <summary>
        /// EventSystem이 없으면 만든다. 없으면 버튼이 전혀 눌리지 않는다.
        /// 씬에 이미 있으면 그대로 둔다.
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var go = new GameObject("EventSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }
}
