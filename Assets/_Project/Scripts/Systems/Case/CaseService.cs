using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 사건 정적 데이터를 ID로 조회하는 창구.
    /// LegendService / RuleService 와 같은 패턴이다.
    ///
    /// SaveData.currentCaseId 문자열을 다시 CaseSO 로 잇는 것이 주 역할이다.
    /// </summary>
    public class CaseService : IService
    {
        private readonly GameDataCatalogSO _catalog;
        private readonly Dictionary<string, CaseSO> _byId = new Dictionary<string, CaseSO>();
        private readonly List<CaseSO> _all = new List<CaseSO>();

        public CaseService(GameDataCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public int Count => _all.Count;

        /// <summary>등록된 사건 전체. 카탈로그 순서(ID 정렬)를 유지한다.</summary>
        public IReadOnlyList<CaseSO> GetAllCases() => _all;

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _byId.Clear();
            _all.Clear();

            if (_catalog == null)
            {
                Debug.LogError("[CaseService] GameDataCatalog이 지정되지 않았다. GameRoot 인스펙터를 확인할 것.");
                return;
            }

            var cases = _catalog.Cases;
            if (cases == null)
            {
                Debug.LogError("[CaseService] 카탈로그의 사건 목록이 비어 있다.");
                return;
            }

            int skipped = 0;
            for (int i = 0; i < cases.Count; i++)
            {
                var caseData = cases[i];

                if (caseData == null)
                {
                    Debug.LogError($"[CaseService] 카탈로그 {i}번 사건 항목이 비어 있다.");
                    skipped++;
                    continue;
                }

                if (string.IsNullOrEmpty(caseData.CaseId))
                {
                    Debug.LogError($"[CaseService] '{caseData.name}' 의 caseId가 비어 있다. 등록하지 않는다.");
                    skipped++;
                    continue;
                }

                if (_byId.TryGetValue(caseData.CaseId, out var existing))
                {
                    Debug.LogError(
                        $"[CaseService] caseId '{caseData.CaseId}' 가 중복된다. " +
                        $"'{existing.name}' 를 유지하고 '{caseData.name}' 를 건너뛴다.");
                    skipped++;
                    continue;
                }

                _byId.Add(caseData.CaseId, caseData);
                _all.Add(caseData);
            }

            Debug.Log($"[CaseService] 사건 {_all.Count}개 등록" + (skipped > 0 ? $" (건너뜀 {skipped}개)" : ""));
        }

        public void Shutdown()
        {
            _byId.Clear();
            _all.Clear();
        }

        // ------------------------------------------------------------- 조회

        /// <summary>ID로 사건을 찾는다. 없으면 null을 돌려주고 경고를 남긴다.</summary>
        public CaseSO GetCase(string caseId)
        {
            if (string.IsNullOrEmpty(caseId))
            {
                Debug.LogWarning("[CaseService] 빈 caseId로 조회했다.");
                return null;
            }

            if (_byId.TryGetValue(caseId, out var caseData)) return caseData;

            Debug.LogWarning($"[CaseService] '{caseId}' 에 해당하는 사건이 없다. 카탈로그를 확인할 것.");
            return null;
        }

        /// <summary>로그 없이 조회한다. 없을 수 있는 경우에 사용한다.</summary>
        public bool TryGetCase(string caseId, out CaseSO caseData)
        {
            caseData = null;
            if (string.IsNullOrEmpty(caseId)) return false;
            return _byId.TryGetValue(caseId, out caseData);
        }

        public bool HasCase(string caseId)
        {
            return !string.IsNullOrEmpty(caseId) && _byId.ContainsKey(caseId);
        }

        /// <summary>지금 플레이할 수 있는 사건인가. 없는 사건이면 false.</summary>
        public bool IsPlayable(string caseId)
        {
            return TryGetCase(caseId, out var caseData) && caseData.IsPlayable;
        }

        /// <summary>선택 가능한 사건만 모은다. 사건 목록 화면에 쓴다.</summary>
        public List<CaseSO> GetPlayableCases()
        {
            var result = new List<CaseSO>();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i] != null && _all[i].IsPlayable) result.Add(_all[i]);
            }
            return result;
        }
    }
}
