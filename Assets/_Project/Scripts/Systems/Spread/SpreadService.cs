using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 괴담 확산도를 다룬다.
    ///
    /// 값은 SaveData.legendStates[].spreadRate 에 그대로 들어간다.
    /// 별도의 수치를 따로 들고 있지 않는다. 저장본과 어긋날 일을 만들지 않기 위해서다.
    ///
    /// 이번 단계에서는 시간에 따른 자동 확산이나 괴담 간 전파를 하지 않는다.
    /// 플레이어의 검열 행동에만 반응한다.
    /// </summary>
    public class SpreadService : IService
    {
        /// <summary>확산도 하한.</summary>
        public const float MinSpread = 0f;

        /// <summary>확산도 상한.</summary>
        public const float MaxSpread = 100f;

        // --- 단계 경계. 여기서만 정한다. ---
        public const float SpreadingThreshold = 25f;
        public const float DangerousThreshold = 50f;
        public const float CriticalThreshold = 75f;

        /// <summary>
        /// 게시글을 처음 열어 봤을 때 확산에 더해지는 비율.
        /// 글이 퍼지는 만큼(spreadWeight)의 일부만 반영한다.
        /// 밸런스 조절점이므로 한 곳에만 둔다.
        /// </summary>
        public const float ViewSpreadRatio = 0.25f;

        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, WebPageSO> _pagesById = new Dictionary<string, WebPageSO>();

        public SpreadService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _pagesById.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[SpreadService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var pages = _catalog.WebPages;
            if (pages == null) return;

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                if (page == null || string.IsNullOrEmpty(page.PageId)) continue;
                if (_pagesById.ContainsKey(page.PageId)) continue;

                _pagesById.Add(page.PageId, page);
            }

            Debug.Log($"[SpreadService] 준비 완료 | 확산 계수 참조 게시글 {_pagesById.Count}개 | 범위 {MinSpread}~{MaxSpread}");
        }

        public void Shutdown()
        {
            _pagesById.Clear();
        }

        // ------------------------------------------------------------- 확산도

        /// <summary>괴담의 현재 확산도. 기록이 없으면 0.</summary>
        public float GetSpreadRate(SaveData save, string legendId)
        {
            var state = FindState(save, legendId);
            return state != null ? state.spreadRate : 0f;
        }

        /// <summary>확산도를 직접 지정한다. 사건 시작 시 초기값을 넣을 때 쓴다.</summary>
        public bool TrySetSpreadRate(SaveData save, string legendId, float value)
        {
            if (save == null || string.IsNullOrEmpty(legendId)) return false;

            var state = save.GetOrCreateLegendState(legendId);
            if (state == null) return false;

            state.spreadRate = Mathf.Clamp(value, MinSpread, MaxSpread);
            return true;
        }

        /// <summary>확산도를 올린다. 상한을 넘지 않는다.</summary>
        public bool TryAddSpread(SaveData save, string legendId, float amount)
        {
            if (save == null || string.IsNullOrEmpty(legendId)) return false;
            if (amount < 0f) return TryReduceSpread(save, legendId, -amount);

            var state = save.GetOrCreateLegendState(legendId);
            if (state == null) return false;

            state.spreadRate = Mathf.Clamp(state.spreadRate + amount, MinSpread, MaxSpread);
            return true;
        }

        /// <summary>확산도를 내린다. 하한 아래로 내려가지 않는다.</summary>
        public bool TryReduceSpread(SaveData save, string legendId, float amount)
        {
            if (save == null || string.IsNullOrEmpty(legendId)) return false;
            if (amount < 0f) return TryAddSpread(save, legendId, -amount);

            var state = FindState(save, legendId);
            if (state == null) return false;   // 기록이 없으면 줄일 것도 없다

            state.spreadRate = Mathf.Clamp(state.spreadRate - amount, MinSpread, MaxSpread);
            return true;
        }

        // ------------------------------------------------------------- 단계

        /// <summary>확산도 수치를 단계로 바꾼다. 경계는 이 메서드 하나가 정한다.</summary>
        public SpreadLevel GetSpreadLevel(float spreadRate)
        {
            if (spreadRate >= CriticalThreshold) return SpreadLevel.Critical;
            if (spreadRate >= DangerousThreshold) return SpreadLevel.Dangerous;
            if (spreadRate >= SpreadingThreshold) return SpreadLevel.Spreading;
            return SpreadLevel.Stable;
        }

        /// <summary>괴담의 현재 확산 단계.</summary>
        public SpreadLevel GetSpreadLevel(SaveData save, string legendId)
        {
            return GetSpreadLevel(GetSpreadRate(save, legendId));
        }

        /// <summary>단계 이름의 Localization String ID.</summary>
        public string GetSpreadLevelTextId(float spreadRate)
        {
            return GetSpreadLevel(spreadRate).ToTextId();
        }

        // ------------------------------------------------------------- 변화 추적

        /// <summary>
        /// 확산도를 더하고 전후 값을 함께 돌려준다.
        /// "42% -> 30%" 같은 안내가 필요한 곳에서 쓴다. 음수를 주면 감소한다.
        /// </summary>
        public SpreadChangeResult ApplySpreadDelta(SaveData save, string legendId, float delta)
        {
            float before = GetSpreadRate(save, legendId);
            var beforeLevel = GetSpreadLevel(before);

            bool ok = delta >= 0f
                ? TryAddSpread(save, legendId, delta)
                : TryReduceSpread(save, legendId, -delta);

            if (!ok) return SpreadChangeResult.Failed(before, beforeLevel);

            float after = GetSpreadRate(save, legendId);
            return new SpreadChangeResult(true, before, after, beforeLevel, GetSpreadLevel(after));
        }

        /// <summary>확산도를 지정한 값으로 두고 전후를 돌려준다. 봉인처럼 0으로 만들 때 쓴다.</summary>
        public SpreadChangeResult ApplySpreadRate(SaveData save, string legendId, float value)
        {
            float before = GetSpreadRate(save, legendId);
            var beforeLevel = GetSpreadLevel(before);

            if (!TrySetSpreadRate(save, legendId, value)) return SpreadChangeResult.Failed(before, beforeLevel);

            float after = GetSpreadRate(save, legendId);
            return new SpreadChangeResult(true, before, after, beforeLevel, GetSpreadLevel(after));
        }

        /// <summary>게시글을 처음 열었을 때 퍼지는 양. 검열된 글은 더 이상 퍼지지 않는다.</summary>
        public float GetViewSpreadAmount(string pageId)
        {
            return GetSpreadWeight(pageId) * ViewSpreadRatio;
        }

        // ------------------------------------------------------------- 게시글 계수

        /// <summary>게시글을 방치했을 때의 확산 기여도. 정상 검열 시 이만큼 줄인다.</summary>
        public float GetSpreadWeight(string pageId)
        {
            var page = FindPage(pageId);
            return page != null ? page.SpreadWeight : 0f;
        }

        /// <summary>잘못 검열했을 때의 페널티. 확산이 이만큼 늘어난다.</summary>
        public float GetWrongCensorPenalty(string pageId)
        {
            var page = FindPage(pageId);
            return page != null ? page.WrongCensorPenalty : 0f;
        }

        // ------------------------------------------------------------- 내부

        /// <summary>기록이 없으면 null. 없는 괴담을 조회해도 상태를 새로 만들지 않는다.</summary>
        private static LegendState FindState(SaveData save, string legendId)
        {
            if (save == null || save.legendStates == null || string.IsNullOrEmpty(legendId)) return null;

            for (int i = 0; i < save.legendStates.Count; i++)
            {
                if (save.legendStates[i] != null && save.legendStates[i].legendId == legendId)
                {
                    return save.legendStates[i];
                }
            }
            return null;
        }

        private WebPageSO FindPage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId)) return null;
            return _pagesById.TryGetValue(pageId, out var page) ? page : null;
        }
    }
}
