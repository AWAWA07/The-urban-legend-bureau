using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 첫 번째 Vertical Slice의 진행을 이어 붙이는 컴포넌트.
    ///
    /// 화면 전환은 기존 UIService, 데이터 조회는 기존 LegendService,
    /// 진행 상태는 기존 SaveData를 쓴다. 새로 만든 것은 이 연결 코드뿐이다.
    ///
    /// 사건이 여러 개가 되면 이 역할은 CaseSO + CaseService로 옮겨간다.
    /// 지금은 사건 하나를 관통시키는 것이 목적이라 최소 형태로 둔다.
    /// </summary>
    public class CaseDirector : MonoBehaviour
    {
        [Header("화면")]
        [SerializeField] private TextPanelScreen _titleScreen;
        [SerializeField] private TextPanelScreen _helpScreen;
        [SerializeField] private SettingsScreen _settingsScreen;
        [SerializeField] private CaseListScreen _caseListScreen;
        [SerializeField] private TextPanelScreen _bureauScreen;
        [SerializeField] private ActionListScreen _actionListScreen;
        [SerializeField] private InternetListScreen _internetListScreen;
        [SerializeField] private InternetPageScreen _internetPageScreen;
        [SerializeField] private FieldHudScreen _fieldHudScreen;
        [SerializeField] private TextPanelScreen _cluePopupScreen;
        [SerializeField] private TextPanelScreen _rulePopupScreen;
        [SerializeField] private RuleListScreen _ruleListScreen;
        [SerializeField] private TextPanelScreen _warningPopupScreen;
        [SerializeField] private TextPanelScreen _exorcismScreen;
        [SerializeField] private TextPanelScreen _resultScreen;

        [Tooltip("괴담넷. 컴퓨터로 여는 것과 휴대폰으로 여는 것이 같은 화면이다.")]
        [SerializeField] private CommunityPageScreen _communityScreen;

        [Header("봉인 화면 버튼")]
        [SerializeField] private GameObject _sealButton;
        [SerializeField] private GameObject _sealConfirmButton;

        [Header("현장")]
        [SerializeField] private FieldController _field;

        [Header("튜토리얼")]
        [SerializeField] private TutorialDirector _tutorial;

        private UIService _ui;
        private SaveService _save;
        private LegendService _legends;
        private RuleService _rules;
        private InternetService _internet;
        private SpreadService _spread;
        private BeliefService _belief;
        private ExorcismService _exorcism;
        private CaseService _cases;
        private InvestigationTimeService _time;
        private InvestigationActionService _actions;
        private LocalizationService _loc;

        private CaseSO _case;
        private LegendSO _legend;

        /// <summary>현재 조사 중인 사건 ID. CaseSO에서 가져온다. 하드코딩하지 않는다.</summary>
        private string _caseId => _case != null ? _case.CaseId : string.Empty;

        /// <summary>현재 사건이 다루는 괴담 ID.</summary>
        private string _legendId => _case != null ? _case.LegendId : string.Empty;
        private string _pendingClueId;

        /// <summary>지금 조사 방법을 고르고 있는 현장 지점. 사무실에서 열었으면 null.</summary>
        private InvestigationPoint _activePoint;
        private string _sealFeedbackTextId;
        private SpreadChangeResult _lastSpreadChange;

        // --- 화면 문구 String ID ---
        private const string TitleTextId = "ui.slice.title";
        private const string TitleBodyTextId = "ui.case.list_hint";
        private const string TitleFooterNewTextId = "ui.slice.title_hint";
        private const string TitleFooterDoneTextId = "ui.slice.already_completed";
        private const string BureauTitleTextId = "ui.slice.bureau_title";
        private const string InternetTitleTextId = "ui.slice.internet_title";
        private const string InternetFooterTextId = "ui.slice.internet_hint";
        private const string FieldTitleTextId = "ui.slice.field_title";
        private const string FieldHintTextId = "ui.slice.field_hint";

        /// <summary>현장에서 대사 띠에 서는 사람. 차지한 본인이다.</summary>
        private const string FieldSpeakerTextId = "tutorial.char.chajihan";
        private const string ClueAcquiredTextId = "ui.slice.clue_acquired";
        private const string ClueDuplicateTextId = "ui.slice.clue_duplicate";
        private const string NothingFoundTextId = "ui.slice.nothing_found";
        private const string ResultTitleTextId = "ui.slice.case_complete";
        private const string ResultFooterTextId = "ui.slice.result_hint";
        private const string LabelRiskTextId = "ui.slice.label_risk";
        private const string LabelClueTextId = "ui.slice.label_clue";
        private const string RuleRelatedClueTextId = "ui.slice.rule_related_clue";
        private const string RuleCandidateFoundTextId = "ui.rule.candidate_found";
        private const string RuleCandidateHintTextId = "ui.rule.candidate_hint";
        private const string RuleScreenTitleTextId = "ui.rule.screen_title";
        private const string RuleScreenHintTextId = "ui.rule.screen_hint";
        private const string RuleAlreadyMarkTextId = "ui.rule.already_mark";
        private const string RuleListTextId = "ui.slice.rule_list";
        private const string CensorAvailableTextId = "ui.net.censor_available";
        private const string CensorDoneTextId = "ui.net.censor_done";
        private const string CensorBlockedTextId = "ui.net.censor_blocked";
        private const string PageMissingTextId = "ui.net.page_missing";
        private const string CensorOkTextId = "ui.net.censor_ok_feedback";
        private const string CensorWrongTextId = "ui.net.censor_wrong_feedback";
        private const string LabelSpreadTextId = "ui.net.label_spread";
        private const string LabelBeliefTextId = "ui.net.label_belief";
        private const string ExorcismTitleTextId = "ui.seal.title";
        private const string LabelSealStateTextId = "ui.seal.label_state";
        private const string SealStateReadyTextId = "ui.seal.state_ready";
        private const string SealStateBlockedTextId = "ui.seal.state_blocked";
        private const string SealStateDoneTextId = "ui.seal.state_done";
        private const string SealNoRuleTextId = "ui.seal.no_rule";
        private const string LabelCensoredTextId = "ui.seal.label_censored";
        private const string RuleCorrectTextId = "ui.rule.correct";
        private const string RuleWrongTextId = "ui.rule.wrong";
        private const string SealStateFalseOnlyTextId = "ui.seal.state_false_only";
        private const string SealNeedTrueRuleTextId = "ui.seal.need_true_rule";
        private const string CaseListTitleTextId = "ui.case.list_title";
        private const string CaseListHintTextId = "ui.case.list_hint";
        private const string CaseCompletedMarkTextId = "ui.case.completed_mark";
        private const string ViewSpreadTextId = "spread.feedback.view";
        private const string ActionListTitleTextId = "ui.action.title";
        private const string ActionListHintTextId = "ui.action.hint";
        private const string ActionLockedMarkTextId = "ui.action.locked";
        private const string PointBlockedTextId = "ui.field.point_locked";
        private const string CostTimeTextId = "ui.action.cost_time";
        private const string CostSpreadTextId = "ui.action.cost_spread";
        private const string ElapsedTextId = "ui.time.elapsed";
        private const string ActionCountTextId = "ui.time.actions";
        private const string SpreadWarningTitleTextId = "spread.warning.title";
        private const string HelpTitleTextId = "ui.title.help";
        private const string HelpBodyTextId = "ui.help.body_placeholder";
        private const string HelpFooterTextId = "ui.common.back";

        private void Start()
        {
            ServiceRegistry.TryGet(out _ui);
            ServiceRegistry.TryGet(out _save);
            ServiceRegistry.TryGet(out _legends);
            ServiceRegistry.TryGet(out _rules);
            ServiceRegistry.TryGet(out _internet);
            ServiceRegistry.TryGet(out _spread);
            ServiceRegistry.TryGet(out _belief);
            ServiceRegistry.TryGet(out _exorcism);
            ServiceRegistry.TryGet(out _cases);
            ServiceRegistry.TryGet(out _time);
            ServiceRegistry.TryGet(out _actions);
            ServiceRegistry.TryGet(out _loc);

            if (_ui == null || _save == null || _legends == null || _loc == null)
            {
                Debug.LogError("[CaseDirector] 필요한 서비스를 찾지 못했다. 씬에 GameRoot가 있는지 확인할 것.");
                enabled = false;
                return;
            }

            // 저장된 진행이 있으면 불러온다. 없으면 SaveService가 만든 새 데이터를 그대로 쓴다.
            if (_save.HasSave() && _save.Load())
            {
                Debug.Log($"[CaseDirector] 저장 불러옴 | 완료된 사건 {_save.Current.completedCaseIds.Count}개, " +
                          $"보유 단서 {_save.Current.acquiredClueIds.Count}개");
            }

            if (_field != null)
            {
                _field.Investigated += OnPointInvestigated;
                _field.SetFieldVisible(false);
            }

            // 저장된 진행이 있어도 이번 단계에서는 타이틀부터 시작한다.
            ShowTitle();
        }

        /// <summary>사건을 현재 조사 대상으로 삼는다. 여기서만 _case 가 바뀐다.</summary>
        private bool SelectCase(CaseSO caseData)
        {
            if (caseData == null) return false;

            // 사건이 바뀌면 이전 사건의 현장 맥락은 버린다. 사건끼리 상태가 섞이지 않게 한다.
            _activePoint = null;
            _boarded = false;

            _case = caseData;
            _legend = _legends.GetLegend(caseData.LegendId);

            if (_legend == null)
            {
                Debug.LogError($"[CaseDirector] 사건 '{caseData.CaseId}' 의 괴담 '{caseData.LegendId}' 를 찾지 못했다.");
                return false;
            }

            Debug.Log($"[CaseDirector] 사건 선택 | {caseData.CaseId} -> {caseData.LegendId}");
            return true;
        }

        private void OnDestroy()
        {
            if (_field != null) _field.Investigated -= OnPointInvestigated;
        }

        // ------------------------------------------------------------- 화면 전환

        private void ShowTitle()
        {
            if (_field != null) _field.SetFieldVisible(false);

            // 타이틀 아래에는 아무 문구도 두지 않는다. 제목과 버튼만 남긴다.
            _titleScreen.Bind(TitleTextId, null, null);

            if (_ui.Count == 0) _ui.Push(_titleScreen);
            else _ui.Replace(_titleScreen);
        }

        // ------------------------------------------------------------- 타이틀 메뉴

        /// <summary>
        /// 튜토리얼이 곧바로 첫 사건의 현장으로 넘어갈 때 부른다.
        /// 사건 목록을 거치지 않고 정해진 사건 하나를 열어 현장까지 들어간다.
        /// </summary>
        public bool BeginCaseField(string caseId)
        {
            if (_cases == null || !_cases.TryGetCase(caseId, out var caseData))
            {
                Debug.LogError("[CaseDirector] 사건을 찾지 못했다: " + caseId);
                return false;
            }

            if (!SelectCase(caseData)) return false;

            OnStartCaseClicked();
            OnEnterFieldClicked();
            return true;
        }

        /// <summary>현장 화면. 튜토리얼이 대사를 걸기 위해 가져간다.</summary>
        public FieldHudScreen FieldHud => _fieldHudScreen;

        /// <summary>현장 화면의 글을 평소 내용으로 되돌린다. 튜토리얼 대사가 끝날 때 부른다.</summary>
        public void RefreshFieldHud()
        {
            if (_fieldHudScreen == null) return;
            _fieldHudScreen.Bind(BuildFieldTicker);
        }

        /// <summary>
        /// 현장 맨 위 한 줄. 열차에 탄 뒤에는 그 사실을 앞에 붙인다.
        /// </summary>
        private string BuildFieldTicker()
        {
            if (_boarded) return _loc.Get(InsideTrainTextId) + "    " + BuildStatusLine();

            // 문이 열렸는데 아직 안 탔으면, 탈 수 있다는 것부터 알린다.
            var arrival = GetArrival();
            if (arrival != null && arrival.IsOpen) return _loc.Get(DoorOpenTextId) + "    " + BuildStatusLine();

            return BuildStatusLine();
        }

        // ------------------------------------------------------------- 휴대폰 속 괴담넷

        /// <summary>휴대폰에서 괴담넷 아이콘의 ID. 컴퓨터 바탕화면과 같은 것을 쓴다.</summary>
        private const string NetAppId = "gwedamnet";


        /// <summary>
        /// 현장에서 휴대폰 앱을 눌렀을 때.
        /// 지금 열리는 것은 괴담넷뿐이다. 나머지는 자리만 잡아 둔 아이콘이라 눌러도 아무 일이 없다.
        /// </summary>
        private void OnPhoneAppClicked(string appId)
        {
            if (appId != NetAppId) return;
            OpenPhoneNet();
        }

        /// <summary>
        /// 휴대폰으로 괴담넷을 연다.
        ///
        /// 컴퓨터로 여는 그 괴담넷과 같은 화면이다. 세로로 세우고 여백과 글자만 줄인다.
        /// 게시판 목록도 같은 것을 쓴다. 한쪽을 고치면 다른 쪽도 함께 바뀐다.
        /// </summary>
        private void OpenPhoneNet()
        {
            if (_communityScreen == null) return;

            // 손에 든 휴대폰을 눈앞으로 들어 올린 것이다. 작게 떠 있던 쪽은 접는다.
            if (_fieldHudScreen != null) _fieldHudScreen.SetPhoneOpen(false);

            _communityScreen.SetShape(true);
            _communityScreen.SetControlsEnabled(true);
            _communityScreen.BindWindow(ClosePhoneNet);
            _communityScreen.BindBoard(GetNetBoard(), OnPhoneBoardEntry);
            _communityScreen.ShowNotice(null);
            _communityScreen.ShowBoard(true);

            _ui.Push(_communityScreen);
            Debug.Log("[CaseDirector] 휴대폰으로 괴담넷을 열었다");
        }

        private void ClosePhoneNet()
        {
            if (_communityScreen == null) return;

            _ui.Close(_communityScreen);

            // 다음에 컴퓨터로 열 때를 위해 창 모양으로 돌려놓는다.
            _communityScreen.SetShape(false);
        }

        /// <summary>괴담넷 게시판. 튜토리얼이 들고 있는 그 목록을 그대로 쓴다.</summary>
        private IReadOnlyList<CommunityBoardEntry> GetNetBoard()
        {
            return _tutorial != null ? _tutorial.BoardEntries : new List<CommunityBoardEntry>();
        }

        private void OnPhoneBoardEntry(CommunityBoardEntry entry)
        {
            if (entry == null || !entry.Openable || entry.Page == null) return;

            _communityScreen.BindPage(entry.Page, 1204, PostTimeTextId, 0, 0, entry.BeliefPercent);
            _communityScreen.BindComments(null);
            _communityScreen.BindChoices(null, null, null);   // 현장에서는 댓글을 달지 않는다
            _communityScreen.ShowBoard(false);
        }

        private const string PostTimeTextId = "ui.net.post_time_tutorial";

        /// <summary>지금 현장의 열차 진입 장면. 막차 사건이 아니면 null.</summary>
        private TrainArrival GetArrival()
        {
            if (_field == null || _field.ActiveRoot == null) return null;
            return _field.ActiveRoot.GetComponentInChildren<TrainArrival>(true);
        }

        /// <summary>
        /// 열차를 불러들인다. 튜토리얼이 "열차가 들어온다" 하는 마디에서 부른다.
        /// 들어와서 서고 문이 열리면 탈 자리가 켜진다. 타는 것은 플레이어가 누를 때다.
        /// </summary>
        public void StartTrainArrival()
        {
            var arrival = GetArrival();
            if (arrival == null) return;

            arrival.Opened -= OnTrainDoorsOpened;
            arrival.Opened += OnTrainDoorsOpened;
            arrival.Play();
        }

        private void OnTrainDoorsOpened()
        {
            // 튜토리얼이 대사를 걸어 둔 동안에는 그쪽 글이 우선이다. 끝나면 제 글로 돌아온다.
            if (_tutorial != null && _tutorial.IsRunning) return;
            RefreshFieldHud();
        }

        /// <summary>열차에 올라탔는가. 사건이 바뀌면 풀린다.</summary>
        private bool _boarded;

        private const string InsideTrainTextId = "ui.field.inside_train";
        private const string DoorOpenTextId = "ui.field.door_open";

        /// <summary>열린 문을 눌러 타는 자리. 현장을 짓는 쪽과 여기가 같은 이름을 써야 한다.</summary>
        public const string BoardingPointId = "point_subway_board";

        /// <summary>바깥에서 타이틀로 돌려보낼 때. 튜토리얼이 끝나면 이리로 온다.</summary>
        public void ShowTitleScreen()
        {
            ShowTitle();
        }

        /// <summary>
        /// 타이틀의 시작 버튼.
        /// 처음이면 튜토리얼부터, 이미 봤으면 사건 목록으로 간다.
        /// 봤는지 여부는 기존 storyFlags 가 들고 있다.
        /// </summary>
        public void OnStartClicked()
        {
            if (_tutorial != null && !TutorialDirector.HasSeenTutorial())
            {
                _tutorial.StartTutorial();
                return;
            }

            OnOpenCaseListClicked();
        }

        /// <summary>타이틀의 설명 버튼. 내용은 아직 비어 있다.</summary>
        public void OnOpenHelpClicked()
        {
            if (_helpScreen == null) return;

            _helpScreen.Bind(HelpTitleTextId, HelpBodyTextId, HelpFooterTextId);
            _ui.Replace(_helpScreen);
        }

        /// <summary>
        /// 설명 화면의 튜토리얼 다시 보기 버튼.
        /// 이미 본 사람도 처음부터 다시 돌릴 수 있게 한다. 저장본은 건드리지 않는다.
        /// 튜토리얼은 제 전용 저장본에서 돌기 때문이다.
        /// </summary>
        public void OnReplayTutorialClicked()
        {
            if (_tutorial == null)
            {
                Debug.LogWarning("[CaseDirector] 튜토리얼이 연결되지 않았다. 씬을 다시 빌드할 것.");
                return;
            }

            _tutorial.StartTutorial();
        }

        /// <summary>타이틀의 세팅 버튼.</summary>
        public void OnOpenSettingsClicked()
        {
            if (_settingsScreen == null) return;
            _ui.Replace(_settingsScreen);
        }

        /// <summary>설명/세팅 화면의 돌아가기 버튼.</summary>
        public void OnBackToTitleFromMenuClicked()
        {
            ShowTitle();
        }

        /// <summary>
        /// 타이틀의 종료 버튼.
        /// 플랫폼 분기는 PlatformInfo 안에서만 한다. WebGL에서는 조용히 무시된다.
        /// </summary>
        public void OnQuitClicked()
        {
            if (!PlatformInfo.QuitApplication())
            {
                Debug.Log("[CaseDirector] 종료할 수 없는 플랫폼이다. 타이틀에 그대로 머문다.");
            }
        }

        /// <summary>타이틀의 사건 목록 버튼. 선택 가능한 사건을 나열한다.</summary>
        public void OnOpenCaseListClicked()
        {
            var list = _cases != null ? _cases.GetPlayableCases() : new List<CaseSO>();

            _caseListScreen.Bind(CaseListTitleTextId, CaseListHintTextId, list, BuildCaseLabel, OnCaseSelected);
            _ui.Replace(_caseListScreen);

            Debug.Log($"[CaseDirector] 사건 목록 | 선택 가능 {list.Count}건");
        }

        /// <summary>사건 목록 항목 문구: 사건 이름 + 개요, 완료했으면 표시.</summary>
        private string BuildCaseLabel(CaseSO caseData)
        {
            if (caseData == null) return string.Empty;

            string name = _loc.Get(caseData.CaseNameTextId);
            if (CaseFlow.IsCaseCompleted(_save, caseData.CaseId))
            {
                name += "  [" + _loc.Get(CaseCompletedMarkTextId) + "]";
            }
            return name + "\n" + _loc.Get(caseData.CaseDescriptionTextId);
        }

        /// <summary>목록에서 사건을 골랐을 때. 선택한 사건으로 브리핑을 연다.</summary>
        private void OnCaseSelected(CaseSO caseData)
        {
            if (!SelectCase(caseData)) return;
            OnStartCaseClicked();
        }

        /// <summary>선택한 사건을 시작한다.</summary>
        public void OnStartCaseClicked()
        {
            if (_case == null)
            {
                Debug.LogWarning("[CaseDirector] 선택된 사건이 없다. 사건 목록에서 먼저 고를 것.");
                return;
            }

            // 처음 여는 사건이면 괴담 데이터의 초기 확산도를 심는다.
            // 이미 조사를 시작한 사건이라면 저장된 확산도를 그대로 둔다.
            bool firstTime = !_save.Current.GetOrCreateLegendState(_legendId).isDiscovered;

            CaseFlow.StartCase(_save, _caseId, _legendId);
            CaseFlow.SetStep(_save, _case.StartingStep);

            if (firstTime && _legend != null && _spread != null)
            {
                _spread.TrySetSpreadRate(_save.Current, _legendId, _legend.InitialSpreadRate);
                _save.MarkDirty();
                Debug.Log($"[CaseDirector] 초기 확산도 설정 | {_legendId} = {_legend.InitialSpreadRate}");
            }

            // 처음 여는 사건만 시간을 0부터 센다. 진행 중이던 사건은 저장된 경과를 이어간다.
            if (firstTime && _time != null)
            {
                _time.ResetCase(_save, _caseId);
            }

            _bureauScreen.BindWithBodyProvider(BureauTitleTextId, BuildLegendBriefing);
            _ui.Replace(_bureauScreen);

            Debug.Log($"[CaseDirector] 사건 시작 | caseId={_caseId} step={CaseFlow.GetStep(_save)}");
        }

        // ------------------------------------------------------------- 조사 행동

        /// <summary>사무실 화면의 조사 행동 버튼. 이 사건에서 고를 수 있는 행동을 나열한다.</summary>
        public void OnOpenActionsClicked()
        {
            if (_actions == null || _actionListScreen == null) return;

            _activePoint = null;
            BindActionScreen();

            if (_ui.Contains(_actionListScreen)) _actionListScreen.Refresh();
            else _ui.Replace(_actionListScreen);

            Debug.Log($"[CaseDirector] 조사 행동 목록 | {CurrentActions().Count}개 | step={CaseFlow.GetStep(_save)}");
        }

        /// <summary>현장 지점을 골랐을 때. 그 지점에서 가능한 조사 방법만 보여준다.</summary>
        private void OpenPointActions(InvestigationPoint point)
        {
            if (_actions == null || _actionListScreen == null) return;

            _activePoint = point;
            if (_field != null) _field.SetFieldVisible(false);

            BindActionScreen();

            if (_ui.Contains(_actionListScreen)) _actionListScreen.Refresh();
            else _ui.Replace(_actionListScreen);

            Debug.Log($"[CaseDirector] 조사 지점 | {point.PointId} | 조사 방법 {point.Actions.Count}개");
        }

        /// <summary>지금 고를 수 있는 조사 행동. 지점을 고른 상태면 그 지점의 목록이다.</summary>
        private List<InvestigationActionSO> CurrentActions()
        {
            if (_activePoint == null) return _actions.GetActionsForLegend(_legend);
            return _actions.FilterFieldActions(_activePoint.Actions);
        }

        /// <summary>행동 화면을 현재 맥락에 맞춰 다시 채운다. 제목은 지점 이름이 있으면 그것을 쓴다.</summary>
        private void BindActionScreen()
        {
            string titleId = _activePoint != null && !string.IsNullOrEmpty(_activePoint.NameTextId)
                ? _activePoint.NameTextId
                : ActionListTitleTextId;

            _actionListScreen.Bind(
                titleId, ActionListHintTextId,
                CurrentActions(), BuildActionLabel, OnActionSelected, BuildStatusBlock);
        }

        /// <summary>조건이 막은 지점. 시간도 확산도 움직이지 않는다.</summary>
        private void ShowPointBlocked(InvestigationPoint point, InvestigationFailure failure)
        {
            string reasonId = InvestigationActionService.FailureTextId(failure);
            string nameId = point.NameTextId;

            _cluePopupScreen.BindWithBodyProvider(
                PointBlockedTextId,
                () => _loc.Get(nameId) + "\n\n" + _loc.Get(reasonId));
            _ui.Push(_cluePopupScreen);
            _pendingClueId = null;

            Debug.Log($"[CaseDirector] 조사 지점 잠김 | {point.PointId} -> {failure} (시간/확산 변화 없음)");
        }

        /// <summary>
        /// 항목 문구: 행동 이름 + 설명 + 예상 비용.
        /// 비용을 미리 보여주어야 "시간을 아낄까, 정보를 얻을까"를 고를 수 있다.
        /// </summary>
        private string BuildActionLabel(InvestigationActionSO action)
        {
            if (action == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(_loc.Get(action.ActionNameTextId));

            if (_actions != null && !_actions.CanPerform(_save, action))
            {
                sb.Append("  [").Append(_loc.Get(ActionLockedMarkTextId)).Append("]");
            }

            var desc = _loc.Get(action.DescriptionTextId);
            if (!string.IsNullOrEmpty(desc)) sb.Append("\n").Append(desc);

            sb.Append("\n").Append(BuildActionCostLine(action));
            return sb.ToString();
        }

        /// <summary>"시간 +10분   확산도 +1.5%" 한 줄. 실제 실행에 쓰는 값과 같은 계산을 쓴다.</summary>
        private string BuildActionCostLine(InvestigationActionSO action)
        {
            int minutes = InvestigationActionService.GetMinutes(action);
            float spread = InvestigationActionService.GetSpreadCost(action);

            return _loc.Get(CostTimeTextId, minutes) + "   " + _loc.Get(CostSpreadTextId, spread.ToString("0.#"));
        }

        /// <summary>
        /// 행동을 골랐을 때.
        /// 조건 판정과 시간/확산 처리는 서비스가 하고, 여기서는 결과를 화면에 옮긴다.
        /// </summary>
        private void OnActionSelected(InvestigationActionSO action)
        {
            if (_actions == null || action == null) return;

            var result = _actions.Perform(_save, _caseId, _legendId, action);

            // 조사한 지점은 눈으로 구분되게 표시한다. 조건에 막혀 실행되지 않았다면 표시하지 않는다.
            if (result.Success && _activePoint != null) _activePoint.MarkInvestigated();

            // 단서를 새로 얻었다면 규칙을 추론할 수 있는지 확인한다. 기존 현장 조사와 같은 흐름이다.
            bool ruleShown = false;
            if (result.GotNewClue) ruleShown = NotifyRuleCandidates();

            BindActionScreen();

            var captured = result;
            _actionListScreen.ShowResult(() => BuildActionResultLine(captured));

            Debug.Log($"[CaseDirector] 조사 행동 결과 | {action.ActionId} -> " +
                      (result.Success ? "성공" : "차단(" + result.Failure + ")") +
                      $" | {result.ActionCount}회 / {result.ElapsedMinutes}분" +
                      (ruleShown ? " | 규칙 추론" : string.Empty));
        }

        /// <summary>
        /// 조사 결과 문구.
        /// 무엇을 얻었는지와 무엇을 썼는지를 한 번에 보여준다.
        /// 조건에 막힌 경우에는 쓴 것이 없으므로 비용 줄을 붙이지 않는다.
        /// </summary>
        private string BuildActionResultLine(InvestigationActionResult result)
        {
            var sb = new StringBuilder();
            sb.Append(_loc.Get(result.MessageTextId));

            if (result.Outcome == InvestigationOutcome.Blocked) return sb.ToString();

            if (result.GotNewClue)
            {
                sb.Append("\n- ");
                sb.Append(ResolveClueText(result.AcquiredClueId));
            }

            sb.Append("\n");
            sb.Append(_loc.Get(CostTimeTextId, result.MinutesAdded));
            sb.Append("   ");
            sb.Append(_loc.Get(CostSpreadTextId, result.SpreadAdded.ToString("0.#")));

            if (result.Spread.Changed)
            {
                sb.Append("   ");
                sb.Append(_loc.Get(LabelSpreadTextId));
                sb.Append($" {result.Spread.PreviousRate:F0}% → {result.Spread.CurrentRate:F0}%");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 조사 행동 화면의 돌아가기 버튼.
        /// 현장 지점에서 들어왔으면 현장으로, 사무실에서 들어왔으면 사무실로 돌아간다.
        /// </summary>
        public void OnActionsBackClicked()
        {
            if (_activePoint != null)
            {
                _activePoint = null;
                ShowFieldHud();
                return;
            }

            if (_ui.Contains(_actionListScreen)) _ui.Close(_actionListScreen);

            _bureauScreen.BindWithBodyProvider(BureauTitleTextId, BuildLegendBriefing);

            if (_ui.Count == 0) _ui.Push(_bureauScreen);
            else _ui.Replace(_bureauScreen);
        }

        /// <summary>현장 화면을 연다. 사건 단계는 건드리지 않는다. 지점 조사에서 돌아올 때 쓴다.</summary>
        private void ShowFieldHud()
        {
            // 조사 방법 / 규칙 추론 화면을 먼저 확실히 닫는다.
            // Replace는 최상단만 바꾸므로, 위에 팝업이 떠 있으면 아래 화면이 스택에 남는다.
            if (_ui.Contains(_actionListScreen)) _ui.Close(_actionListScreen);
            if (_ui.Contains(_ruleListScreen)) _ui.Close(_ruleListScreen);

            // 위쪽 한 줄에만 지금 상황을 건다.
            // 말하는 사람이 없으면 아래 띠에는 버튼만 남는다.
            _fieldHudScreen.Bind(BuildFieldTicker);
            _fieldHudScreen.BindPhoneApps(OnPhoneAppClicked);

            if (_ui.Count == 0) _ui.Push(_fieldHudScreen);
            else _ui.Replace(_fieldHudScreen);

            if (_field != null) _field.SetFieldVisible(true, _legendId);

            // 열차가 들어오는 장면은 튜토리얼이 한 번만 보여준다.
            // 그 밖에 현장을 열 때는 이미 열차가 서 있고 문이 열려 있다. 누르면 바로 탄다.
            if (_tutorial == null || !_tutorial.IsRunning)
            {
                var arrival = GetArrival();
                if (arrival != null && !arrival.IsOpen) arrival.SkipToOpen();
                _fieldHudScreen.Refresh();
            }
        }

        /// <summary>사무실 화면의 인터넷 조사 버튼. 이 괴담과 관련된 게시글 목록을 연다.</summary>
        public void OnInternetResearchClicked()
        {
            CaseFlow.SetStep(_save, CaseStep.InternetResearch);

            _internetListScreen.Bind(
                InternetTitleTextId, InternetFooterTextId,
                GetCasePages(), BuildPageListLabel, OnPageSelected, BuildStatusBlock);

            _ui.Replace(_internetListScreen);
            Debug.Log($"[CaseDirector] 인터넷 조사 | 게시글 {GetCasePages().Count}개 | step={CaseFlow.GetStep(_save)}");
        }

        /// <summary>
        /// 목록에서 게시글을 골랐을 때.
        /// 처음 여는 글이면 그만큼 소문이 퍼진다. 이미 열었거나 검열한 글은 퍼지지 않는다.
        /// </summary>
        private void OnPageSelected(WebPageSO page)
        {
            string feedbackId = null;

            // 글을 여는 것 자체가 조사 행동이다. 이미 본 글을 다시 열어도 시간은 흐른다.
            RegisterAction(InvestigationAction.InternetView);

            if (page != null && _internet != null && _spread != null
                && _internet.ShouldSpreadOnView(_save.Current, page.PageId))
            {
                float amount = _spread.GetViewSpreadAmount(page.PageId);
                var change = _spread.ApplySpreadDelta(_save.Current, _legendId, amount);
                _internet.TryMarkPageViewed(_save, page.PageId);
                _save.MarkDirty();

                if (change.Changed)
                {
                    _lastSpreadChange = change;
                    feedbackId = ViewSpreadTextId;
                    Debug.Log($"[CaseDirector] 열람 확산 | {page.PageId} +{amount:F1} " +
                              $"({change.PreviousRate:F0}% -> {change.CurrentRate:F0}%)");
                }
            }

            _internetPageScreen.Bind(page, BuildPageStatus, CanCensor, BuildStatusBlock);
            if (feedbackId != null)
            {
                var change = _lastSpreadChange;
                _internetPageScreen.ShowFeedbackProvider(() => BuildSpreadChangeLine(ViewSpreadTextId, change));
            }

            _ui.Push(_internetPageScreen);
            _internetListScreen.Refresh();
        }

        /// <summary>"안내 문구 / 확산도 42% → 30%" 형태의 한 줄을 만든다.</summary>
        private string BuildSpreadChangeLine(string messageTextId, SpreadChangeResult change)
        {
            var sb = new StringBuilder();
            sb.Append(_loc.Get(messageTextId));

            if (change.Changed)
            {
                sb.Append("   ");
                sb.Append(_loc.Get(LabelSpreadTextId));
                sb.Append($" {change.PreviousRate:F0}% → {change.CurrentRate:F0}%");
            }
            return sb.ToString();
        }

        /// <summary>상세 화면의 검열 버튼.</summary>
        public void OnCensorClicked()
        {
            var page = _internetPageScreen != null ? _internetPageScreen.CurrentPage : null;
            if (page == null || _internet == null) return;

            float beliefBefore = GetBelief();

            // 검열도 조사 행동이다. 확산 증감은 아래 기존 검열 규칙이 그대로 맡는다.
            RegisterAction(InvestigationAction.PageCensor);

            var result = _internet.TryCensorPage(_save, page.PageId);
            string feedbackId = ApplyCensorOutcome(page, result);

            // 검열 성공/실패와 무관하게 화면 상태를 즉시 다시 그린다.
            var change = _lastSpreadChange;
            _internetPageScreen.ShowFeedbackProvider(() => BuildSpreadChangeLine(feedbackId, change));
            _internetListScreen.Refresh();

            Debug.Log($"[CaseDirector] 검열 시도: {page.PageId} -> {result} | " +
                      $"확산 {change.PreviousRate:F0} -> {change.CurrentRate:F0} ({change.CurrentLevel}) / " +
                      $"믿음 {beliefBefore:F0} -> {GetBelief():F0}");
        }

        /// <summary>
        /// 검열 결과를 확산도와 믿음 수치에 반영한다.
        ///
        /// 정상 검열 : 확산도 -= spreadWeight          (믿음은 건드리지 않는다)
        /// 잘못된 검열: 확산도 += wrongCensorPenalty,  믿음 += 증가분 x 비율
        ///
        /// 수치를 다루는 곳을 여기 하나로 모아 둔 이유:
        /// 어느 괴담의 확산인지는 사건이 알고 있고, InternetService는 모른다.
        /// </summary>
        private string ApplyCensorOutcome(WebPageSO page, CensorResult result)
        {
            if (_spread == null || _belief == null) return null;

            switch (result)
            {
                case CensorResult.Success:
                {
                    float weight = _spread.GetSpreadWeight(page.PageId);
                    _lastSpreadChange = _spread.ApplySpreadDelta(_save.Current, _legendId, -weight);
                    _save.MarkDirty();
                    return CensorOkTextId;
                }

                case CensorResult.WrongTarget:
                {
                    float penalty = _spread.GetWrongCensorPenalty(page.PageId);
                    _lastSpreadChange = _spread.ApplySpreadDelta(_save.Current, _legendId, penalty);
                    _belief.TryAddBelief(_save.Current, BeliefService.SpreadToBelief(penalty));
                    _save.MarkDirty();
                    return CensorWrongTextId;
                }

                case CensorResult.AlreadyCensored:
                    _lastSpreadChange = SpreadChangeResult.Failed(GetSpread(), _spread.GetSpreadLevel(GetSpread()));
                    return CensorDoneTextId;

                default:
                    _lastSpreadChange = SpreadChangeResult.Failed(GetSpread(), _spread.GetSpreadLevel(GetSpread()));
                    return PageMissingTextId;
            }
        }

        // ------------------------------------------------------------- 수치 표시

        private float GetSpread()
        {
            return _spread != null ? _spread.GetSpreadRate(_save.Current, _legendId) : 0f;
        }

        private float GetBelief()
        {
            return _belief != null ? _belief.GetBeliefLevel(_save.Current) : 0f;
        }

        /// <summary>확산도 / 단계 / 믿음도 표시줄. 라벨은 Localization에서 가져온다.</summary>
        private string BuildStatsLine()
        {
            float spread = GetSpread();
            string levelText = _spread != null ? _loc.Get(_spread.GetSpreadLevelTextId(spread)) : string.Empty;

            return $"{_loc.Get(LabelSpreadTextId)}: {spread:F0}% ({levelText})" +
                   $"    {_loc.Get(LabelBeliefTextId)}: {GetBelief():F0}";
        }

        /// <summary>사건 경과 시간 / 조사 행동 횟수 표시줄.</summary>
        private string BuildTimeLine()
        {
            if (_time == null || _case == null) return string.Empty;

            int minutes = _time.GetElapsedMinutes(_save.Current, _caseId);
            int actions = _time.GetActionCount(_save.Current, _caseId);

            return _loc.Get(ElapsedTextId, minutes) + "    " + _loc.Get(ActionCountTextId, actions);
        }

        /// <summary>시간 + 수치를 두 줄로 묶는다. 조사 화면들이 공통으로 쓴다.</summary>
        private string BuildStatusBlock()
        {
            var time = BuildTimeLine();
            return string.IsNullOrEmpty(time) ? BuildStatsLine() : time + "\n" + BuildStatsLine();
        }

        /// <summary>
        /// 같은 내용을 한 줄로 잇는다. 현장 화면 맨 위에 얇게 띄우는 자리에 쓴다.
        /// 거기는 두 줄이 들어갈 높이가 아니다.
        /// </summary>
        private string BuildStatusLine()
        {
            var time = BuildTimeLine();
            return string.IsNullOrEmpty(time) ? BuildStatsLine() : time + "    " + BuildStatsLine();
        }

        /// <summary>
        /// 조사 행동 하나를 사건 시간에 반영한다.
        ///
        /// 확산 증감은 행동 종류에 따라 InvestigationTimeService가 정한다.
        /// 검열/봉인처럼 이미 자기 확산 규칙을 가진 행동은 시간만 흐르므로,
        /// 같은 행동에 확산이 두 번 붙지 않는다.
        /// </summary>
        private void RegisterAction(InvestigationAction action)
        {
            if (_time == null || _case == null) return;
            _time.RegisterAction(_save, _caseId, _legendId, action);
        }

        /// <summary>상세 화면의 목록 복귀 버튼.</summary>
        public void OnPageBackClicked()
        {
            _ui.Close(_internetPageScreen);
            _internetListScreen.Refresh();
        }

        /// <summary>
        /// 인터넷 화면의 현장 이동 버튼.
        /// 확산도가 위험 이상이면 먼저 경고를 보여준다. 진입을 막지는 않는다.
        /// </summary>
        public void OnEnterFieldClicked()
        {
            var level = _spread != null ? _spread.GetSpreadLevel(_save.Current, _legendId) : SpreadLevel.Stable;

            if (level.NeedsFieldWarning())
            {
                var warningId = level.ToWarningTextId();
                _warningPopupScreen.BindWithBodyProvider(
                    SpreadWarningTitleTextId,
                    () => _loc.Get(warningId) + "\n\n" + BuildStatusBlock());
                _ui.Push(_warningPopupScreen);

                Debug.Log($"[CaseDirector] 현장 진입 경고 | 확산 {GetSpread():F0}% ({level})");
                return;
            }

            EnterField();
        }

        /// <summary>경고 팝업의 확인 버튼. 확인하면 그대로 현장으로 들어간다.</summary>
        public void OnSpreadWarningConfirmClicked()
        {
            _ui.Close(_warningPopupScreen);
            EnterField();
        }

        private void EnterField()
        {
            CaseFlow.SetStep(_save, CaseStep.FieldInvestigation);

            _activePoint = null;
            ShowFieldHud();

            Debug.Log($"[CaseDirector] 현장 진입 | 확산 {GetSpread():F0}% step={CaseFlow.GetStep(_save)}");
        }

        /// <summary>단서 팝업의 확인 버튼.</summary>
        public void OnCluePopupConfirmClicked()
        {
            _ui.Close(_cluePopupScreen);

            if (string.IsNullOrEmpty(_pendingClueId))
            {
                // 단서가 없는 지점이었다. 현장 조사를 계속한다.
                return;
            }

            _pendingClueId = null;

            // 단서만으로는 사건이 끝나지 않는다. 규칙을 추론해야 종결할 수 있다.
            if (!NotifyRuleCandidates())
            {
                Debug.Log("[CaseDirector] 아직 추론할 수 있는 규칙이 없다. 현장 조사를 계속한다.");
            }
        }

        /// <summary>
        /// 규칙 팝업의 확인 버튼.
        ///
        /// 현장 지점에서 조사하다 규칙을 알아낸 경우에는 현장으로 돌아간다.
        /// 단서 하나를 찾았다고 조사가 끝나는 것은 아니다. 봉인으로 넘어갈지는 플레이어가 정한다.
        /// </summary>
        public void OnRulePopupConfirmClicked()
        {
            _ui.Close(_rulePopupScreen);

            if (_activePoint != null)
            {
                _activePoint = null;
                ShowFieldHud();
                return;
            }

            ShowExorcism();
        }

        /// <summary>현장 화면의 조사 종료 버튼. 봉인 단계로 넘어간다.</summary>
        public void OnFieldDoneClicked()
        {
            ShowExorcism();
        }

        // ------------------------------------------------------------- 봉인

        private void ShowExorcism()
        {
            CaseFlow.SetStep(_save, CaseStep.Exorcism);

            _activePoint = null;
            if (_ui.Contains(_ruleListScreen)) _ui.Close(_ruleListScreen);
            if (_field != null) _field.SetFieldVisible(false);

            _sealFeedbackTextId = null;
            _exorcismScreen.BindWithBodyProvider(ExorcismTitleTextId, BuildExorcismBody, null);
            _ui.Replace(_exorcismScreen);

            RefreshExorcismButtons();
            Debug.Log($"[CaseDirector] 봉인 단계 | 봉인 가능={CanSealNow()} step={CaseFlow.GetStep(_save)}");
        }

        /// <summary>봉인 화면의 [괴담 봉인] 버튼.</summary>
        public void OnSealClicked()
        {
            if (_exorcism == null) return;

            // 봉인 시도도 조사 행동이다. 성공하면 아래에서 확산이 0이 되므로
            // 여기서는 시간만 흐르게 두고 확산은 건드리지 않는다.
            RegisterAction(InvestigationAction.Seal);

            var result = _exorcism.TrySeal(_save.Current, _legendId);

            if (result.IsSuccess())
            {
                // 봉인하면 확산은 멎는다. 믿음은 이번 단계에서 건드리지 않는다.
                if (_spread != null) _lastSpreadChange = _spread.ApplySpreadRate(_save.Current, _legendId, 0f);
                _save.MarkDirty();
            }

            _sealFeedbackTextId = result.ToTextId();
            _exorcismScreen.BindWithBodyProvider(ExorcismTitleTextId, BuildExorcismBody, _sealFeedbackTextId);
            RefreshExorcismButtons();

            Debug.Log($"[CaseDirector] 봉인 시도: {_legendId} -> {result} | " +
                      $"isSealed={_exorcism.IsSealed(_save.Current, _legendId)} 확산={GetSpread():F0}");
        }

        /// <summary>봉인 화면의 확인 버튼. 봉인이 끝난 뒤에만 사건을 종료한다.</summary>
        public void OnExorcismConfirmClicked()
        {
            if (_exorcism == null || !_exorcism.IsSealed(_save.Current, _legendId)) return;
            CompleteCase();
        }

        private bool CanSealNow()
        {
            return _exorcism != null && _exorcism.CanSeal(_save.Current, _legendId);
        }

        /// <summary>
        /// 봉인 전에는 봉인 버튼만, 봉인 후에는 확인 버튼만 보인다.
        /// 올바른 규칙이 없으면 봉인 버튼을 눌러도 소용없으므로 비활성화한다.
        /// </summary>
        private void RefreshExorcismButtons()
        {
            bool sealed_ = _exorcism != null && _exorcism.IsSealed(_save.Current, _legendId);

            if (_sealButton != null)
            {
                _sealButton.SetActive(!sealed_);
                var button = _sealButton.GetComponent<UnityEngine.UI.Button>();
                if (button != null) button.interactable = CanSealNow();
            }

            if (_sealConfirmButton != null) _sealConfirmButton.SetActive(sealed_);
        }

        /// <summary>봉인 화면 본문: 괴담 / 수치 / 확인한 규칙 / 봉인 가능 여부.</summary>
        private string BuildExorcismBody()
        {
            var sb = new StringBuilder();

            if (_legend != null) sb.AppendLine(_loc.Get(_legend.NameTextId));
            sb.AppendLine(BuildStatusBlock());
            sb.AppendLine();

            sb.Append(BuildRuleListWithMarks());

            sb.AppendLine();
            sb.Append(_loc.Get(LabelSealStateTextId) + ": " + _loc.Get(GetSealStateTextId()));

            // 규칙은 있는데 전부 틀렸다면 왜 봉인이 안 되는지 알려준다.
            if (_exorcism != null
                && !_exorcism.IsSealed(_save.Current, _legendId)
                && !CanSealNow()
                && _exorcism.GetDeducedRules(_save.Current, _legendId).Count > 0)
            {
                sb.AppendLine();
                sb.Append(_loc.Get(SealNeedTrueRuleTextId));
            }

            return sb.ToString();
        }

        /// <summary>확인한 규칙을 정확/오류 표식과 함께 나열한다.</summary>
        private string BuildRuleListWithMarks()
        {
            var sb = new StringBuilder();
            sb.AppendLine(_loc.Get(RuleListTextId));

            var rules = _exorcism != null
                ? _exorcism.GetDeducedRules(_save.Current, _legendId)
                : new List<RuleSO>();

            if (rules.Count == 0)
            {
                sb.AppendLine("- " + _loc.Get(SealNoRuleTextId));
                return sb.ToString();
            }

            foreach (var rule in rules)
            {
                string mark = _loc.Get(rule.IsTrue ? RuleCorrectTextId : RuleWrongTextId);
                sb.AppendLine($"- [{mark}] {_loc.Get(rule.RuleTextId)}");
            }
            return sb.ToString();
        }

        /// <summary>봉인 상태 표시 문구의 ID. 봉인 완료 / 봉인 가능 / 올바른 규칙 없음 / 봉인 불가.</summary>
        private string GetSealStateTextId()
        {
            if (_exorcism == null) return SealStateBlockedTextId;
            if (_exorcism.IsSealed(_save.Current, _legendId)) return SealStateDoneTextId;
            if (CanSealNow()) return SealStateReadyTextId;

            return _exorcism.GetDeducedRules(_save.Current, _legendId).Count > 0
                ? SealStateFalseOnlyTextId
                : SealStateBlockedTextId;
        }

        /// <summary>결과 화면의 타이틀 복귀 버튼.</summary>
        public void OnBackToTitleClicked()
        {
            _activePoint = null;
            if (_field != null) _field.SetFieldVisible(false);
            ShowTitle();
        }

        // ------------------------------------------------------------- 현장 조사

        /// <summary>
        /// 열차에 올라탄다. 열차가 들어오는 그 순간에 부른다.
        ///
        /// 현장을 새로 만들지 않고 보이는 것만 갈아 끼운다. 사건 진행과 단서 조건은 그대로다.
        /// 조사 지점은 열차 안에 들어 있으므로, 타기 전에는 아무것도 눌리지 않는다.
        /// </summary>
        public void BoardTrain()
        {
            if (_boarded || _field == null || _field.ActiveRoot == null) return;

            var swap = _field.ActiveRoot.GetComponent<FieldSceneSwap>();
            if (swap == null) return;

            swap.SetAfter(true);
            _boarded = true;
            RefreshFieldHud();

            Debug.Log("[CaseDirector] 열차가 들어왔다 | 승강장 -> 열차 안");
        }

        private void OnPointInvestigated(InvestigationPoint point)
        {
            if (point == null) return;

            // 열린 문을 누르면 탄다. 이동일 뿐이라 시간도 확산도 쓰지 않는다.
            if (point.PointId == BoardingPointId)
            {
                BoardTrain();
                return;
            }

            // --- 해금 조건 ---
            // 막힌 지점은 아무것도 일어나지 않는다. 시간도 확산도 움직이지 않고 조사 표시도 남기지 않는다.
            if (_actions != null)
            {
                var block = _actions.CheckPointRequirements(_save, point);
                if (block != InvestigationFailure.None)
                {
                    ShowPointBlocked(point, block);
                    return;
                }
            }

            // --- 조사 방법이 있는 지점 ---
            // 지점을 고르는 것은 이동일 뿐이라 시간을 쓰지 않는다.
            // 사건 시간과 확산은 이어서 고르는 조사 행동 하나에만 붙는다. 이중 계산을 피하기 위해서다.
            if (point.HasActions)
            {
                OpenPointActions(point);
                return;
            }

            point.MarkInvestigated();

            // 현장을 한 곳 뒤지는 것이 조사 행동 한 번이다.
            // 새 단서를 찾았는지에 따라 확산량만 달라진다. 행동은 어느 쪽이든 한 번이다.
            bool newClue = point.HasClue && !CaseFlow.HasClue(_save, point.ClueId);
            RegisterAction(newClue ? InvestigationAction.ClueFound : InvestigationAction.FieldSearch);

            if (!point.HasClue)
            {
                _pendingClueId = null;
                _cluePopupScreen.BindWithBodyProvider(NothingFoundTextId, () => _loc.Get(point.ResultTextId));
                _ui.Push(_cluePopupScreen);
                Debug.Log($"[CaseDirector] 조사: {point.name} - 단서 없음");
                return;
            }

            bool acquired = CaseFlow.AcquireClue(_save, point.ClueId);

            _pendingClueId = point.ClueId;
            string clueId = point.ClueId;
            _cluePopupScreen.BindWithBodyProvider(
                acquired ? ClueAcquiredTextId : ClueDuplicateTextId,
                () => ResolveClueText(clueId));
            _ui.Push(_cluePopupScreen);

            Debug.Log($"[CaseDirector] 조사: {point.name} - 단서 '{point.ClueId}' " +
                      (acquired ? "획득" : "이미 보유(중복 추가 안 함)"));
        }

        // ------------------------------------------------------------- 규칙 추론

        /// <summary>
        /// 새 단서로 세울 수 있는 규칙이 생겼는지 알린다.
        ///
        /// 19단계부터 단서를 얻었다고 규칙이 저절로 확정되지 않는다.
        /// 여기서는 "추론해 볼 만한 것이 생겼다"만 알리고, 어느 규칙이 맞는지는
        /// 플레이어가 규칙 추론 화면에서 직접 고른다. 규칙 문장도 여기서 보여주지 않는다.
        /// </summary>
        private bool NotifyRuleCandidates()
        {
            if (_rules == null) return false;

            var candidates = _rules.GetDeduceableRules(_save, _legend);
            if (candidates.Count == 0) return false;

            CaseFlow.SetStep(_save, CaseStep.RuleDeduction);

            int count = candidates.Count;
            _rulePopupScreen.BindWithBodyProvider(
                RuleCandidateFoundTextId,
                () => _loc.Get(RuleCandidateHintTextId, count));
            _ui.Push(_rulePopupScreen);

            Debug.Log($"[CaseDirector] 규칙 후보 {count}개 생김 (자동 확정하지 않는다)");
            return true;
        }

        // ------------------------------------------------------------- 규칙 추론 화면

        /// <summary>현장 화면의 규칙 추론 버튼. 확보한 단서와 규칙 후보를 보여준다.</summary>
        public void OnOpenRulesClicked()
        {
            if (_rules == null || _ruleListScreen == null) return;

            BindRuleScreen();

            if (_ui.Contains(_ruleListScreen)) _ruleListScreen.Refresh();
            else _ui.Replace(_ruleListScreen);

            Debug.Log($"[CaseDirector] 규칙 추론 화면 | 후보 {_rules.GetRuleCandidates(_save, _legend).Count}개");
        }

        private void BindRuleScreen()
        {
            _ruleListScreen.Bind(
                RuleScreenTitleTextId, RuleScreenHintTextId,
                _rules.GetRuleCandidates(_save, _legend),
                BuildRuleCandidateLabel, OnRuleCandidateSelected, BuildAcquiredClueBlock);
        }

        /// <summary>
        /// 후보 항목 문구.
        /// 규칙 문장과 근거 단서만 보여준다. 정답 여부는 고르기 전에 절대 드러내지 않는다.
        /// </summary>
        private string BuildRuleCandidateLabel(RuleSO rule)
        {
            if (rule == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(_loc.Get(rule.RuleTextId));

            if (_rules.IsRuleDeduced(_save, rule.RuleId))
            {
                sb.Append("  [").Append(_loc.Get(RuleAlreadyMarkTextId)).Append("]");
            }

            var required = rule.RequiredClueIds;
            if (required != null && required.Count > 0)
            {
                sb.Append("\n").Append(_loc.Get(RuleRelatedClueTextId));
                for (int i = 0; i < required.Count; i++)
                {
                    if (string.IsNullOrEmpty(required[i])) continue;
                    sb.Append("\n- ").Append(ResolveClueText(required[i]));
                }
            }
            return sb.ToString();
        }

        /// <summary>확보한 단서 목록. 추론의 근거가 무엇인지 한눈에 보이게 한다.</summary>
        private string BuildAcquiredClueBlock()
        {
            var sb = new StringBuilder();
            sb.AppendLine(_loc.Get(LabelClueTextId));

            var acquired = _save.Current.acquiredClueIds;
            int shown = 0;

            if (_legend != null)
            {
                foreach (var clue in _legend.Clues)
                {
                    if (clue == null || !acquired.Contains(clue.ClueId)) continue;
                    sb.AppendLine("- " + _loc.Get(clue.ClueTextId));
                    shown++;
                }
            }

            if (shown == 0) sb.AppendLine("- " + _loc.Get(NothingFoundTextId));
            return sb.ToString();
        }

        /// <summary>후보를 골랐을 때. 판정은 RuleService가 하고 여기서는 결과를 옮긴다.</summary>
        private void OnRuleCandidateSelected(RuleSO rule)
        {
            if (_rules == null || rule == null) return;

            var result = _rules.AttemptRule(_save, rule.RuleId);

            BindRuleScreen();
            var captured = result;
            _ruleListScreen.ShowResult(() => _loc.Get(captured.MessageTextId));

            Debug.Log($"[CaseDirector] 규칙 추론 시도 | {rule.RuleId} -> " +
                      (result.Success
                          ? (result.AlreadyDeduced ? "이미 확인함" : (result.IsCorrect ? "정확" : "오류"))
                          : "불가(" + result.Failure + ")"));
        }

        /// <summary>규칙 추론 화면의 돌아가기 버튼. 현장으로 돌아가 조사를 이어간다.</summary>
        public void OnRulesBackClicked()
        {
            ShowFieldHud();
        }

        private void CompleteCase()
        {
            CaseFlow.SetStep(_save, CaseStep.ClueAcquired);
            bool newlyCompleted = CaseFlow.CompleteCase(_save, _caseId);

            if (_field != null) _field.SetFieldVisible(false);

            _resultScreen.BindWithBodyProvider(ResultTitleTextId, BuildResultBody, ResultFooterTextId);
            _ui.Replace(_resultScreen);

            Debug.Log($"[CaseDirector] 사건 종료 | caseId={_caseId} 신규완료={newlyCompleted} " +
                      $"step={CaseFlow.GetStep(_save)} 저장됨");
        }

        // ------------------------------------------------------------- 텍스트 조립

        private string BuildLegendBriefing()
        {
            if (_legend == null) return _loc.Get(NothingFoundTextId);

            var sb = new StringBuilder();
            sb.AppendLine(_loc.Get(_legend.NameTextId));
            sb.AppendLine();
            sb.AppendLine(_loc.Get(LabelRiskTextId) + ": " + _loc.Get(_legend.RiskLevelTextId));
            sb.AppendLine();
            sb.AppendLine(BuildStatusBlock());
            sb.AppendLine();
            sb.Append(_loc.Get(_legend.DescriptionTextId));
            return sb.ToString();
        }

        /// <summary>
        /// 사건 결과 요약.
        ///
        /// SaveData의 단서/게시글 목록은 게임 전체를 통틀어 누적된다.
        /// 결과 화면은 '이번 사건'의 성과를 보여주는 자리이므로,
        /// 이 사건의 괴담에 속한 것만 골라 낸다. 다른 사건의 성과가 섞이면 안 된다.
        /// </summary>
        private string BuildResultBody()
        {
            var sb = new StringBuilder();

            var time = BuildTimeLine();
            if (!string.IsNullOrEmpty(time))
            {
                sb.AppendLine(time);
                sb.AppendLine();
            }

            sb.AppendLine(_loc.Get(LabelClueTextId));

            var acquired = _save.Current.acquiredClueIds;
            if (_legend != null)
            {
                foreach (var clue in _legend.Clues)
                {
                    if (clue == null || !acquired.Contains(clue.ClueId)) continue;
                    sb.AppendLine("- " + _loc.Get(clue.ClueTextId));
                }
            }

            if (_save.Current.deducedRuleIds.Count > 0)
            {
                sb.AppendLine();
                sb.Append(BuildRuleListWithMarks());
            }

            // 이 사건에서 검열한 게시글
            var censored = _save.Current.censoredPageIds;
            if (_legend != null)
            {
                var lines = new StringBuilder();
                foreach (var page in _legend.WebPages)
                {
                    if (page == null || !censored.Contains(page.PageId)) continue;
                    lines.AppendLine("- " + _loc.Get(page.TitleTextId));
                }

                if (lines.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine(_loc.Get(LabelCensoredTextId));
                    sb.Append(lines);
                }
            }

            // 괴담 봉인 여부
            bool sealed_ = _exorcism != null && _exorcism.IsSealed(_save.Current, _legendId);
            sb.AppendLine();
            sb.Append(_loc.Get(LabelSealStateTextId) + ": " +
                      _loc.Get(sealed_ ? SealStateDoneTextId : SealStateBlockedTextId));

            return sb.ToString();
        }

        /// <summary>이번 사건에서 볼 수 있는 게시글. 괴담 데이터에 연결된 것만 보여준다.</summary>
        private List<WebPageSO> GetCasePages()
        {
            var result = new List<WebPageSO>();
            if (_legend == null) return result;

            foreach (var page in _legend.WebPages)
            {
                if (page != null) result.Add(page);
            }
            return result;
        }

        /// <summary>목록 항목 문구: 제목 + 현재 상태.</summary>
        private string BuildPageListLabel(WebPageSO page)
        {
            if (page == null) return _loc.Get(PageMissingTextId);
            return _loc.Get(page.TitleTextId) + "\n" + BuildPageStatus(page);
        }

        /// <summary>검열 가능 / 검열 완료 / 검열 불가 중 현재 상태.</summary>
        private string BuildPageStatus(WebPageSO page)
        {
            if (page == null || _internet == null) return _loc.Get(PageMissingTextId);

            if (_internet.IsCensored(_save.Current, page.PageId)) return _loc.Get(CensorDoneTextId);
            if (!_internet.IsCensorable(page)) return _loc.Get(CensorBlockedTextId);
            return _loc.Get(CensorAvailableTextId);
        }

        /// <summary>
        /// 검열 버튼을 노출할지. 이미 검열한 글에만 숨긴다.
        /// 검열하면 안 되는 글에도 버튼을 남겨 둔다 — 눌러 봐야 잘못된 대상이라는 것을 알 수 있다.
        /// </summary>
        private bool CanCensor(WebPageSO page)
        {
            if (page == null || _internet == null) return false;
            return !_internet.IsCensored(_save.Current, page.PageId);
        }

        /// <summary>단서 ID를 괴담 데이터에서 찾아 표시 문구로 바꾼다.</summary>
        private string ResolveClueText(string clueId)
        {
            if (_legend != null)
            {
                foreach (var clue in _legend.Clues)
                {
                    if (clue != null && clue.ClueId == clueId) return _loc.Get(clue.ClueTextId);
                }
            }
            return clueId;
        }
    }
}
