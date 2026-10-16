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

        /// <summary>
        /// 그날 밤 벽시계(GameClock)가 시작한 뒤로 흐른 분 (v7). -1 이면 적어 둔 적이 없다.
        /// 저장본을 불러오면 이 값으로 시계를 되돌린다. 새벽 2시 12분에 저장했으면 불러와도 2시 12분이다.
        /// </summary>
        public int clockElapsedMinutes = -1;

        /// <summary>
        /// 괴담넷 게시판이 어느 시각까지 살아 움직였는가 (v7). 벽시계의 흐른 분으로 적는다.
        /// 불러온 뒤 같은 시간만큼 조회수와 댓글이 또 늘지 않게, 이 뒤로 흐른 몫만 새로 반영한다.
        /// </summary>
        public int boardActivityMinute = -1;
    }

    /// <summary>
    /// 본편의 하루 진행과 괴담 상태 (v11). 튜토리얼에서는 비어 있다.
    /// 시각은 벽시계(GameClock)가 1일차 22:30 부터 흐른 분으로 적는다. 달력 날짜와 근무 일차는 이 값에서 계산한다(WorkdayDirector).
    /// </summary>
    [Serializable]
    public class WorkState
    {
        /// <summary>본편이 시작됐는가. 튜토리얼 첫날 밤 취침 뒤 켜진다.</summary>
        public bool mainStarted;

        /// <summary>벽시계가 흐른 분. -1 이면 적어 둔 적이 없다.</summary>
        public int elapsedMinutes = -1;

        /// <summary>자연 확산을 어느 시각(벽시계 흐른 분)까지 반영했는가. 불러온 뒤 같은 시간이 두 번 반영되지 않게 한다.</summary>
        public int spreadAppliedMinute = -1;

        /// <summary>오늘 근무가 03:00 으로 끝났고 잠들기를 기다리는가.</summary>
        public bool shiftEnded;

        /// <summary>지금 맡고 있는(활성) 사건.</summary>
        public List<string> activeCaseIds = new List<string>();

        /// <summary>괴담마다 1%에 못 미친 자연 확산 몫. "괴담ID|몫" 으로 적는다.</summary>
        public List<string> legendCarry = new List<string>();

        /// <summary>이번 근무를 시작할 때의 괴담 믿음도. "괴담ID|%". 전체는 "*" 로 적는다.</summary>
        public List<string> dayStartBeliefs = new List<string>();

        /// <summary>지난 근무를 시작할 때의 괴담 믿음도. 현황판의 "전날 대비"에 쓴다.</summary>
        public List<string> prevDayBeliefs = new List<string>();

        /// <summary>이미 알린 경고 단계. "괴담ID|단계%". 처음 넘을 때 한 번만 알린다.</summary>
        public List<string> warned = new List<string>();

        /// <summary>게임 오버가 났는가와 그 원인 괴담("*" 이면 전체 믿음도).</summary>
        public bool gameOver;
        public string gameOverLegendId;

        /// <summary>지금 맡아 조사하는 사건 (v12). 현황판에서 고른다. 비어 있으면 아직 고르지 않았다.</summary>
        public string currentCaseId;

        /// <summary>
        /// 괴담 글을 게시판에 건 사건 (v12). 활성 사건과 따로 센다. 해결해 활성에서 빠져도 그 글은 게시판에 남는다.
        /// </summary>
        public List<string> postedCaseIds = new List<string>();
    }

    /// <summary>
    /// 게시판 글 하나의 바뀌는 숫자 (v11). 게시판은 매번 새로 짓고, 본편에서는 이 값으로 덮어 저장 시점의 모습을 되살린다.
    /// 열쇠는 글의 제목 ID 다(플레이어 글은 PlayerPostStore 가 따로 다룬다).
    /// </summary>
    [Serializable]
    public class BoardEntryState
    {
        public string key;
        public int belief;
        public int views;
        public int likes;
        public int dislikes;

        /// <summary>
        /// 이 글의 댓글 전부 (v12). "작성자ID|본문ID|플레이어면1". 시간이 흘러 붙은 댓글도 여기에 남는다.
        /// 비어 있으면(v11 저장본) 게시판이 지은 기본 댓글을 그대로 쓴다.
        /// </summary>
        public List<string> comments = new List<string>();

        /// <summary>시간이 흘러 붙은 반응의 남은 몫과 붙은 댓글 수, 눌러 둔 좋아요·싫어요 (v12). 불러와도 그 자리에서 이어 간다.</summary>
        public float[] growthCarry = new float[4];
        public int growthAddedComments;
        public bool likePressed;
        public bool dislikePressed;
    }

    /// <summary>
    /// 플레이어가 괴담넷에 올린 글 하나 (v10).
    /// 게시판 목록은 저장하지 않고 매번 새로 짓는다. 플레이어가 쓴 글만 여기에 적어 두었다가, 게시판을 지을 때 되살린다.
    /// 화면에 걸린 글(CommunityBoardEntry)의 숫자가 바뀌면 PlayerPostStore 가 이쪽으로 옮겨 적는다.
    /// </summary>
    [Serializable]
    public class SavedBoardPost
    {
        public string postId;
        public string caseId;
        public string postWritingId;
        public string legendId;
        public string authorTextId;
        public string boardTextId;

        /// <summary>고른 문장. 제목 하나와 본문(도입 / 사실 / 행동 안내)의 String ID.</summary>
        public string titleTextId;
        public List<string> bodyTextIds = new List<string>();

        /// <summary>올린 시각. 벽시계가 흐른 분으로 적는다.</summary>
        public int postedAtMinute;

        public int views;
        public int likes;
        public int dislikes;
        public bool likePressed;
        public bool dislikePressed;

        /// <summary>달린 댓글. "작성자ID|본문ID|플레이어면 1" 로 적는다.</summary>
        public List<string> comments = new List<string>();

        /// <summary>시간이 흘러 붙는 반응 가운데 1에 못 미친 몫(조회 / 좋아요 / 싫어요 / 댓글)과 그렇게 붙은 댓글 수.</summary>
        public float[] growthCarry = new float[4];
        public int growthAddedComments;

        /// <summary>이 글이 이 괴담의 믿음도를 옮긴 몫(%p). 게시판을 새로 지을 때 한 번만 다시 반영한다.</summary>
        public int beliefShift;

        /// <summary>내린 글인가. 내린 글은 게시판에 되살리지 않는다.</summary>
        public bool removed;
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
        /// 이미 해 본 조사 방법 (v6). "사건ID|지점ID|행동ID" 로 적는다.
        /// 같은 방법이 여러 지점에 걸려 있으므로 지점까지 함께 적어야 한 곳에서 한 일이 다른 곳에 번지지 않는다.
        /// </summary>
        public List<string> doneActions = new List<string>();

        /// <summary>
        /// 메모장에 적어 둔 것 (v5). 플레이어가 직접 쓴 글이라 그대로 들고 있는다.
        /// 여러 장을 둘 수 있다. 몇 장까지인지는 메모장 화면이 정한다.
        /// </summary>
        public List<string> memos = new List<string>();

        /// <summary>
        /// 수사 노트 (v8). 사건이 섞이지 않게 모두 "사건ID|ID" 로 적는다.
        ///   analysisRecords  - 단서 둘을 맞대어 찾아낸 분석 기록
        ///   solvedDeductions - 맞힌 최종 추론 물음
        ///   confirmedReports - 조사 결과를 확정한 사건(ID 만)
        /// 얻은 단서는 예전처럼 acquiredClueIds 가 들고 있다. 따로 두지 않는다.
        /// </summary>
        public List<string> analysisRecords = new List<string>();
        public List<string> solvedDeductions = new List<string>();
        public List<string> confirmedReports = new List<string>();

        /// <summary>
        /// 보고서 빈칸에 적고 있던 글 (v9). "사건ID|빈칸ID|글" 로 적는다. 글 안에 | 가 있어도 앞의 두 칸만 자른다.
        /// 다 맞혀 확정했는지는 confirmedReports 가 들고 있다.
        /// </summary>
        public List<string> reportInputs = new List<string>();

        /// <summary>플레이어가 괴담넷에 올린 글 (v10). 내린 글도 removed 로 남는다.</summary>
        public List<SavedBoardPost> boardPosts = new List<SavedBoardPost>();

        /// <summary>본편의 하루 진행과 괴담 상태 (v11).</summary>
        public WorkState work = new WorkState();

        /// <summary>본편 게시판 글의 믿음도와 반응 (v11).</summary>
        public List<BoardEntryState> boardStates = new List<BoardEntryState>();

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
            doneActions ??= new List<string>();
            analysisRecords ??= new List<string>();
            solvedDeductions ??= new List<string>();
            confirmedReports ??= new List<string>();
            reportInputs ??= new List<string>();
            boardPosts ??= new List<SavedBoardPost>();
            work ??= new WorkState();
            work.activeCaseIds ??= new List<string>();
            work.legendCarry ??= new List<string>();
            work.dayStartBeliefs ??= new List<string>();
            work.prevDayBeliefs ??= new List<string>();
            work.warned ??= new List<string>();
            work.postedCaseIds ??= new List<string>();
            boardStates ??= new List<BoardEntryState>();
            foreach (var b in boardStates)
            {
                if (b == null) continue;
                b.comments ??= new List<string>();
                if (b.growthCarry == null || b.growthCarry.Length != 4) b.growthCarry = new float[4];
            }
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
