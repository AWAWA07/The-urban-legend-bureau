using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 괴담 정적 데이터를 ID로 조회하는 창구.
    ///
    /// SaveData는 legendId 문자열만 들고 있으므로, 저장된 진행 상태를
    /// 실제 LegendSO와 다시 이어 붙이려면 이 서비스가 필요하다.
    ///
    /// 조회만 한다. 확산도 변경이나 사건 진행 같은 게임 로직은 여기에 넣지 않는다.
    /// </summary>
    public class LegendService : IService
    {
        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, LegendSO> _byId = new Dictionary<string, LegendSO>();
        private readonly List<LegendSO> _all = new List<LegendSO>();

        public LegendService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        /// <summary>등록된 괴담 수.</summary>
        public int Count => _all.Count;

        /// <summary>등록된 괴담 전체. 등록 순서를 유지한다.</summary>
        public IReadOnlyList<LegendSO> GetAllLegends() => _all;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _byId.Clear();
            _all.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[LegendService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var legends = _catalog.Legends;
            int skipped = 0;

            for (int i = 0; i < legends.Count; i++)
            {
                var legend = legends[i];

                if (legend == null)
                {
                    Debug.LogError($"[LegendService] 카탈로그 {i}번 항목이 비어 있다.");
                    skipped++;
                    continue;
                }

                if (string.IsNullOrEmpty(legend.LegendId))
                {
                    Debug.LogError($"[LegendService] '{legend.name}' 의 legendId가 비어 있다. 등록하지 않는다.");
                    skipped++;
                    continue;
                }

                if (_byId.TryGetValue(legend.LegendId, out var existing))
                {
                    Debug.LogError(
                        $"[LegendService] legendId '{legend.LegendId}' 가 중복된다. " +
                        $"'{existing.name}' 를 유지하고 '{legend.name}' 를 건너뛴다.");
                    skipped++;
                    continue;
                }

                _byId.Add(legend.LegendId, legend);
                _all.Add(legend);
            }

            Debug.Log($"[LegendService] 괴담 {_all.Count}개 등록" + (skipped > 0 ? $" (건너뜀 {skipped}개)" : ""));
        }

        public void Shutdown()
        {
            _byId.Clear();
            _all.Clear();
        }

        // ------------------------------------------------------------- 조회

        /// <summary>
        /// ID로 괴담을 찾는다. 없으면 null을 돌려주고 경고를 남긴다.
        /// 없을 수도 있는 상황이라면 TryGetLegend를 쓴다.
        /// </summary>
        public LegendSO GetLegend(string legendId)
        {
            if (string.IsNullOrEmpty(legendId))
            {
                Debug.LogWarning("[LegendService] 빈 legendId로 조회했다.");
                return null;
            }

            if (_byId.TryGetValue(legendId, out var legend)) return legend;

            Debug.LogWarning($"[LegendService] '{legendId}' 에 해당하는 괴담이 없다. 카탈로그를 확인할 것.");
            return null;
        }

        /// <summary>로그 없이 조회한다. 없을 수 있는 경우에 사용한다.</summary>
        public bool TryGetLegend(string legendId, out LegendSO legend)
        {
            legend = null;
            if (string.IsNullOrEmpty(legendId)) return false;
            return _byId.TryGetValue(legendId, out legend);
        }

        public bool HasLegend(string legendId)
        {
            return !string.IsNullOrEmpty(legendId) && _byId.ContainsKey(legendId);
        }
    }
}
