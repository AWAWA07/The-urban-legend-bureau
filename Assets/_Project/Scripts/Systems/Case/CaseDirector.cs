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
        [SerializeField] private CaseListScreen _caseListScreen;
        [SerializeField] private TextPanelScreen _bureauScreen;
        [SerializeField] private InternetListScreen _internetListScreen;
        [SerializeField] private InternetPageScreen _internetPageScreen;
        [SerializeField] private TextPanelScreen _fieldHudScreen;
        [SerializeField] private TextPanelScreen _cluePopupScreen;
        [SerializeField] private TextPanelScreen _rulePopupScreen;
        [SerializeField] private TextPanelScreen _warningPopupScreen;
        [SerializeField] private TextPanelScreen _exorcismScreen;
        [SerializeField] private TextPanelScreen _resultScreen;

        [Header("봉인 화면 버튼")]
        [SerializeField] private GameObject _sealButton;
        [SerializeField] private GameObject _sealConfirmButton;

        [Header("현장")]
        [SerializeField] private FieldController _field;

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
        private LocalizationService _loc;

        private CaseSO _case;
        private LegendSO _legend;

        /// <summary>현재 조사 중인 사건 ID. CaseSO에서 가져온다. 하드코딩하지 않는다.</summary>
        private string _caseId => _case != null ? _case.CaseId : string.Empty;

        /// <summary>현재 사건이 다루는 괴담 ID.</summary>
        private string _legendId => _case != null ? _case.LegendId : string.Empty;
        private string _pendingClueId;
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
        private const string ClueAcquiredTextId = "ui.slice.clue_acquired";
        private const string ClueDuplicateTextId = "ui.slice.clue_duplicate";
        private const string NothingFoundTextId = "ui.slice.nothing_found";
        private const string ResultTitleTextId = "ui.slice.case_complete";
        private const string ResultFooterTextId = "ui.slice.result_hint";
        private const string LabelRiskTextId = "ui.slice.label_risk";
        private const string LabelClueTextId = "ui.slice.label_clue";
        private const string RuleFoundTextId = "ui.slice.rule_found";
        private const string RuleRelatedClueTextId = "ui.slice.rule_related_clue";
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
        private const string ElapsedTextId = "ui.time.elapsed";
        private const string ActionCountTextId = "ui.time.actions";
        private const string SpreadWarningTitleTextId = "spread.warning.title";

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

            _titleScreen.Bind(TitleTextId, TitleBodyTextId, TitleFooterNewTextId);

            if (_ui.Count == 0) _ui.Push(_titleScreen);
            else _ui.Replace(_titleScreen);
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

            _fieldHudScreen.BindWithBodyProvider(
                FieldTitleTextId,
                () => _loc.Get(FieldHintTextId) + "\n\n" + BuildStatusBlock());
            _ui.Replace(_fieldHudScreen);

            if (_field != null) _field.SetFieldVisible(true, _legendId);
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
            if (!TryDeduceRules())
            {
                Debug.Log("[CaseDirector] 아직 추론할 수 있는 규칙이 없다. 현장 조사를 계속한다.");
            }
        }

        /// <summary>규칙 팝업의 확인 버튼. 규칙을 알았으니 봉인 단계로 넘어간다.</summary>
        public void OnRulePopupConfirmClicked()
        {
            _ui.Close(_rulePopupScreen);
            ShowExorcism();
        }

        // ------------------------------------------------------------- 봉인

        private void ShowExorcism()
        {
            CaseFlow.SetStep(_save, CaseStep.Exorcism);

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
            if (_field != null) _field.SetFieldVisible(false);
            ShowTitle();
        }

        // ------------------------------------------------------------- 현장 조사

        private void OnPointInvestigated(InvestigationPoint point)
        {
            if (point == null) return;

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
        /// 지금 보유한 단서로 추론할 수 있는 규칙을 해금한다.
        /// 새로 해금한 규칙이 있으면 규칙 팝업을 띄우고 true를 돌려준다.
        /// </summary>
        private bool TryDeduceRules()
        {
            if (_rules == null) return false;

            var candidates = _rules.GetDeduceableRules(_save, _legend);
            if (candidates.Count == 0) return false;

            var deduced = new List<RuleSO>();
            foreach (var rule in candidates)
            {
                if (_rules.TryDeduceRule(_save, rule.RuleId))
                {
                    deduced.Add(rule);
                    Debug.Log($"[CaseDirector] 규칙 해금: {rule.RuleId}");
                }
            }

            if (deduced.Count == 0) return false;

            CaseFlow.SetStep(_save, CaseStep.RuleDeduction);

            _rulePopupScreen.BindWithBodyProvider(RuleFoundTextId, () => BuildRuleBody(deduced));
            _ui.Push(_rulePopupScreen);
            return true;
        }

        /// <summary>규칙 문구 + 근거가 된 단서를 함께 보여준다.</summary>
        private string BuildRuleBody(List<RuleSO> rules)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (i > 0) sb.AppendLine();
                sb.AppendLine(_loc.Get(rule.RuleTextId));
                sb.AppendLine();
                sb.AppendLine(_loc.Get(RuleRelatedClueTextId));

                var required = rule.RequiredClueIds;
                for (int c = 0; c < required.Count; c++)
                {
                    sb.AppendLine("- " + ResolveClueText(required[c]));
                }
            }
            return sb.ToString();
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
