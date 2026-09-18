using System;
using System.Collections.Generic;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// 괴담 하나의 진행 상태. 괴담 자체의 정의(SO)는 저장하지 않고 ID로만 가리킨다.
    /// </summary>
    [Serializable]
    public class LegendState
    {
        public string legendId;
        public float spreadRate;
        public bool isDiscovered;
        public bool isSealed;

        public LegendState() { }

        public LegendState(string legendId)
        {
            this.legendId = legendId;
        }
    }

    /// <summary>
    /// 사건 하나의 조사 경과. 실제 플레이 시간이 아니라 조사 행동으로 흐르는 사건 내 시간이다.
    /// 사건마다 따로 들고 있어야 다른 사건의 경과가 섞이지 않는다.
    /// </summary>
    [Serializable]
    public class CaseTimeState
    {
        public string caseId;

        /// <summary>이 사건에서 수행한 조사 행동 횟수.</summary>
        public int actionCount;

        /// <summary>사건 경과 시간(분).</summary>
        public int elapsedMinutes;

        public CaseTimeState() { }

        public CaseTimeState(string caseId)
        {
            this.caseId = caseId;
        }
    }

    /// <summary>사건과 무관하게 이어지는 전역 수치.</summary>
    [Serializable]
    public class GlobalState
    {
        /// <summary>믿음 수치. 높을수록 괴담이 실체에 가까워진다.</summary>
        public int beliefLevel;

        /// <summary>현재 스토리 챕터.</summary>
        public int chapterIndex;

        /// <summary>누적 플레이 시간(초).</summary>
        public int totalPlaySeconds;
    }

    /// <summary>플레이어 설정. 진행과 별개지만 같은 파일에 함께 둔다.</summary>
    [Serializable]
    public class SettingsData
    {
        public string languageCode = LocalizationService.DefaultLanguage;
        public float bgmVolume = 0.8f;
        public float sfxVolume = 1.0f;
        public float textSpeed = 1.0f;
    }

    /// <summary>
    /// 저장 파일의 루트. JsonUtility로 직렬화된다.
    ///
    /// 규칙:
    ///  - string ID와 숫자/bool만 담는다.
    ///  - UnityEngine.Object, ScriptableObject, Scene 참조는 절대 넣지 않는다.
    ///  - 리스트는 null이 되지 않도록 항상 초기화한다. (JsonUtility는 null 리스트를 빈 리스트로 읽는다)
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>스키마 버전. 구조를 바꾸면 올리고 SaveMigration에 변환을 추가한다.</summary>
        public int saveVersion = SaveMigration.CurrentVersion;

        // --- 사건 진행 ---
        public string currentCaseId = string.Empty;
        public int currentStepIndex;
        public List<string> completedCaseIds = new List<string>();

        // --- 조사 결과 ---
        public List<string> acquiredClueIds = new List<string>();
        public List<string> deducedRuleIds = new List<string>();
        public List<string> censoredPageIds = new List<string>();

        // --- 사건 경과 시간 (v4) ---
        public List<CaseTimeState> caseTimes = new List<CaseTimeState>();

        // --- 괴담 ---
        public List<LegendState> legendStates = new List<LegendState>();

        // --- 스토리 ---
        public List<string> storyFlags = new List<string>();

        /// <summary>
        /// 메모장에 적어 둔 것 (v5). 플레이어가 직접 쓴 글이라 그대로 들고 있는다.
        /// 여러 장을 둘 수 있다. 몇 장까지인지는 메모장 화면이 정한다.
        /// </summary>
        public List<string> memos = new List<string>();

        // --- 전역 ---
        public GlobalState global = new GlobalState();
        public SettingsData settings = new SettingsData();

        /// <summary>역직렬화 후 null이 된 필드를 되살린다. JsonUtility는 누락 필드를 채워주지 않는다.</summary>
        public void EnsureIntegrity()
        {
            completedCaseIds ??= new List<string>();
            acquiredClueIds ??= new List<string>();
            deducedRuleIds ??= new List<string>();
            censoredPageIds ??= new List<string>();
            legendStates ??= new List<LegendState>();
            caseTimes ??= new List<CaseTimeState>();
            storyFlags ??= new List<string>();
            global ??= new GlobalState();
            settings ??= new SettingsData();
            currentCaseId ??= string.Empty;
            memos ??= new List<string>();
        }

        /// <summary>괴담 상태를 찾거나 없으면 만들어 돌려준다.</summary>
        public LegendState GetOrCreateLegendState(string legendId)
        {
            for (int i = 0; i < legendStates.Count; i++)
            {
                if (legendStates[i].legendId == legendId) return legendStates[i];
            }

            var state = new LegendState(legendId);
            legendStates.Add(state);
            return state;
        }

        /// <summary>사건의 경과 기록을 찾거나 없으면 만들어 돌려준다.</summary>
        public CaseTimeState GetOrCreateCaseTime(string caseId)
        {
            if (string.IsNullOrEmpty(caseId)) return null;

            caseTimes ??= new List<CaseTimeState>();

            for (int i = 0; i < caseTimes.Count; i++)
            {
                if (caseTimes[i] != null && caseTimes[i].caseId == caseId) return caseTimes[i];
            }

            var state = new CaseTimeState(caseId);
            caseTimes.Add(state);
            return state;
        }
    }
}
