using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장 인물의 모양 그대로 노란 외곽선을 가린다.
    ///
    /// 정전으로 깜깜할 때 조사할 곳의 노란 외곽선은 어둠 위에 그려진다(InvestigationPoint.OutlinesOnTop).
    /// 그대로 두면 앞에 선 사람 위로 외곽선이 지나간다. 사람 그림과 같은 모양의 가림판(SpriteMask)을 두어
    /// 외곽선이 사람 뒤로 가는 것처럼 보이게 한다. 가림판은 외곽선의 그리기 순서에만 작용한다.
    /// 걷고 서는 그림이 바뀌면 가림판도 매 프레임 같은 그림으로 바꾼다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class OutlineOccluder : MonoBehaviour
    {
        private SpriteRenderer _source;
        private SpriteMask _mask;

        private void Awake()
        {
            _source = GetComponent<SpriteRenderer>();
            var go = new GameObject("OutlineOccluder");
            go.transform.SetParent(transform, false);
            _mask = go.AddComponent<SpriteMask>();
            _mask.isCustomRangeActive = true;
            _mask.frontSortingLayerID = _source.sortingLayerID;
            _mask.backSortingLayerID = _source.sortingLayerID;
            _mask.frontSortingOrder = InvestigationPoint.OutlineOnTopOrder;
            _mask.backSortingOrder = InvestigationPoint.OutlineOnTopOrder - 1;
            _mask.enabled = false;
        }

        private void LateUpdate()
        {
            bool on = InvestigationPoint.OutlinesOnTop && _source.enabled && _source.sprite != null;
            _mask.enabled = on;
            if (!on) return;
            _mask.sprite = _source.sprite;
            // 왼쪽을 볼 때 그림을 뒤집는 것은 부모의 크기 부호라 그대로 따라간다. 그림 자체를 뒤집는 경우만 맞춘다.
            var s = _mask.transform.localScale;
            s.x = Mathf.Abs(s.x) * (_source.flipX ? -1f : 1f);
            _mask.transform.localScale = s;
        }
    }
}
