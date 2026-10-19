using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 괴담넷에 글을 한 편 써서 올리는 흐름.
    ///
    /// 글을 쓰는 동안에는 괴담넷 창 안에 글쓰기 화면(PostComposeView)을 덮는다.
    /// 제목 → 도입 → 사실 전달 → 행동 안내를 한 칸씩 고르고, 이전 / 다음 단계로 오갈 수 있다.
    /// 고르는 동안에는 믿음이 어느 쪽으로 움직이는지 알려 주지 않는다. 올린 뒤에 한꺼번에 평가한다.
    /// 다 고르면 게시판에 걸릴 모습 그대로 미리 보고, 올리면 실제 게시판에 걸린다.
    ///
    /// 믿음도는 기존 게시판 몫(TutorialDirector)과 기존 BeliefService 로 움직인다. 새 믿음 계산은 없다.
    /// 문장과 수치와 반응은 전부 PostWritingSO 에 있다. 사건마다 이 흐름을 복제하지 않는다.
    /// </summary>
    public class PostWritingDirector : MonoBehaviour
    {
        [SerializeField] private CommunityPageScreen _communityScreen;

        [Tooltip("게시판 목록과 믿음 몫, 한영의 대화창을 들고 있는 쪽.")]
        [SerializeField] private TutorialDirector _tutorial;

        private LocalizationService _loc;
        private SaveService _save;
        private BeliefService _belief;

        private PostWritingSO _data;
        private PostComposeView _compose;

        /// <summary>지금 고르고 있는 칸. 0 제목, 1 도입, 2 사실, 3 행동 안내, 4 미리보기.</summary>
        private int _section;

        /// <summary>칸마다 고른 후보. 다시 쓰거나 이전 단계로 가도 지우지 않는다.</summary>
        private readonly int[] _picks = { -1, -1, -1, -1 };

        /// <summary>이번에 올린 횟수. 사건을 새로 열면 처음부터 센다.</summary>
        private int _attempts;

        /// <summary>글쓰기 전 한영의 말을 들었는가. 창을 닫았다 다시 열어도 되풀이하지 않는다.</summary>
        private bool _introTalked;

        /// <summary>마지막으로 올린 글과 그 글이 옮긴 믿음 몫. 다시 쓸 때 되돌리는 데 쓴다.</summary>
        private CommunityBoardEntry _lastPost;
        private int _lastShift;

        /// <summary>올리는 중이거나 반응이 달리는 중인가. 그 사이에는 무엇도 새로 시작하지 않는다.</summary>
        private bool _busy;

        /// <summary>지금 글쓰기 화면이 무엇을 보여주는가. 언어가 바뀌면 같은 것을 다시 그린다.</summary>
        private enum View { None, Step, Preview, Result }
        private View _view;
        private Func<PostComposeView.Result> _resultBuilder;

        private const string PublishedFlagPrefix = "post_published:";

        /// <summary>이 글쓰기를 마쳤다는 이야기 표시. 사건을 처음부터 다시 할 때 지우는 쪽도 이것을 쓴다.</summary>
        public static string PublishedFlag(PostWritingSO data) => PublishedFlagPrefix + (data != null ? data.PostWritingId : string.Empty);

        private const string TitlePlaceholderTextId = "ui.post.title_placeholder";
        private static readonly string[] BodyPlaceholderTextIds =
        {
            null,
            "ui.post.placeholder_intro",
            "ui.post.placeholder_fact",
            "ui.post.placeholder_action",
        };

        private static readonly string[] SectionNameTextIds =
        {
            "ui.post.section_title",
            "ui.post.section_intro",
            "ui.post.section_fact",
            "ui.post.section_action",
        };

        private static readonly string[] PromptTextIds =
        {
            "ui.post.prompt_title",
            "ui.post.prompt_intro",
            "ui.post.prompt_fact",
            "ui.post.prompt_action",
        };

        private const string ComposeHeaderTextId = "ui.post.compose_header";
        private const string ComposeAccountTextId = "ui.post.compose_account";
        private const string GoalTextId = "ui.post.goal";
        private const string DraftLabelTextId = "ui.post.draft_label";
        private const string PrevTextId = "ui.post.btn_prev";
        private const string NextTextId = "ui.post.btn_next";
        private const string ToPreviewTextId = "ui.post.btn_to_preview";

        private const string PreviewHeaderTextId = "ui.post.header_preview";
        private const string PreviewSubTextId = "ui.post.preview_sub";
        private const string PreviewBoardTextId = "ui.post.preview_board";
        private const string PreviewMetaTextId = "ui.post.preview_meta";
        private const string PreviewNoteTextId = "ui.post.preview_note";
        private const string PublishTextId = "ui.post.btn_publish";
        private const string RewriteTextId = "ui.post.btn_rewrite";

        private const string ResultHeaderTextId = "ui.post.result_header";
        private const string ResultLegendTextId = "ui.post.result_legend";
        private const string ResultGoalTextId = "ui.post.result_goal";
        private const string ResultOverallTextId = "ui.post.result_overall";
        private const string EffectDownTextId = "ui.post.effect_down";
        private const string EffectUpTextId = "ui.post.effect_up";
        private const string EffectFlatTextId = "ui.post.effect_flat";
        private const string ConfirmTextId = "ui.post.btn_result_ok";

        private const string ResultDownTextId = "ui.post.result_down";
        private const string ResultUpTextId = "ui.post.result_up";
        private const string ResultFlatTextId = "ui.post.result_flat";

        /// <summary>댓글이 하나씩 달리는 간격(초). 댓글 연습 때와 같은 빠르기다.</summary>
        private const float ReactionInterval = 0.8f;

        /// <summary>글쓰기를 성공으로 마쳤을 때. 사건 쪽이 이것을 받아 다음 흐름(첫날 밤)으로 넘어간다.</summary>
        public event Action<PostWritingSO> Succeeded;

        /// <summary>올리기를 확정한 순간 한 번. 글이 걸리고 믿음도가 움직이기 바로 앞이다. 본편은 여기서 글 쓰는 시간이 흐른다.</summary>
        public event Action<PostWritingSO> Published;

        /// <summary>평가를 읽은 뒤(성공이든 아니든). 본편은 여기서 사건을 정식으로 해결할 수 있는지 따진다.</summary>
        public event Action<PostWritingSO> ResultConfirmed;

        /// <summary>
        /// 본편 괴담 믿음도(소수 첫째 자리, 사건 해결 판정과 같은 값). 비워 두면 게시판 글 평균(정수)을 쓴다.
        /// 튜토리얼 글에는 쓰지 않는다. 튜토리얼 평가 기준(성공 24% / 우수 20%)이 그대로여야 하기 때문이다.
        /// </summary>
        public Func<string, float> BeliefOf;
        public Func<float> OverallOf;

        /// <summary>성공한 글을 올린 뒤에도 더 쓸 수 있는가. 본편은 사건을 정식으로 해결하기 전까지 열어 둔다.</summary>
        public Func<PostWritingSO, bool> KeepOpen;

        /// <summary>평가 화면에 덧붙이는 사건 해결 목표 한 줄. 게시물 평가와 사건 해결은 다른 기준이라 따로 적는다.</summary>
        public Func<PostWritingSO, string> CaseGoalLine;

        /// <summary>한 칸에 한 번에 보이는 후보 수. 본편은 이미 올린 문장을 빼고 남은 것 가운데 앞에서부터 보인다.</summary>
        private const int ShownPerSection = 3;

        private void Start()
        {
            ServiceRegistry.TryGet(out _loc);
            ServiceRegistry.TryGet(out _save);
            ServiceRegistry.TryGet(out _belief);

            // 올린 글은 저장본에 따로 적힌다. 게시판을 새로 지으면 되살리고, 저장하기 직전에 숫자를 옮겨 적는다.
            if (_tutorial != null) _tutorial.BoardRebuilt += RestoreSavedPosts;
            if (_save != null) _save.BeforeSave += SyncSavedPosts;
        }

        private void OnDestroy()
        {
            if (_tutorial != null) _tutorial.BoardRebuilt -= RestoreSavedPosts;
            if (_save != null) _save.BeforeSave -= SyncSavedPosts;
        }

        private void RestoreSavedPosts()
        {
            if (_save == null || _save.Current == null || _tutorial == null) return;
            PlayerPostStore.Restore(_save.Current, _tutorial);
        }

        private void SyncSavedPosts()
        {
            if (_save == null || _save.Current == null || _tutorial == null) return;
            PlayerPostStore.Sync(_save.Current, _tutorial.BoardEntries);
        }

        private void OnEnable() => EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
        private void OnDisable() => EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            if (_compose == null || !_compose.IsShowing || _data == null) return;
            switch (_view)
            {
                case View.Step: ShowStep(); break;
                case View.Preview: ShowPreview(); break;
                case View.Result: if (_resultBuilder != null) _compose.ShowResult(_resultBuilder()); break;
            }
        }

        // ------------------------------------------------------------- 밖에서 묻는 것

        /// <summary>이 글쓰기를 이미 성공으로 마쳤는가. 저장본의 이야기 표시로 안다.</summary>
        public bool IsFinished(PostWritingSO data)
        {
            if (data == null || _save == null || _save.Current == null) return false;
            return _save.Current.storyFlags.Contains(PublishedFlagPrefix + data.PostWritingId);
        }

        /// <summary>지금 글쓰기 단추를 세워도 되는가.</summary>
        public bool CanWrite(PostWritingSO data)
        {
            if (data == null) return false;
            if (IsFinished(data) && !(KeepOpen != null && KeepOpen(data))) return false;
            if (_attempts > 0 && !data.AllowRewrite) return false;
            if (data.MaxAttempts > 0 && _attempts >= data.MaxAttempts) return false;
            // 본편은 칸마다 아직 올리지 않은 문장이 남아 있어야 쓸 수 있다. 튜토리얼은 예전 그대로다.
            for (int s = 0; !data.IsTutorial && s < PostWritingSO.SectionCount; s++)
                if (Visible(data, s).Count == 0) return false;
            return true;
        }

        /// <summary>
        /// 이 칸에 보일 후보(전체 목록의 번호).
        ///
        /// 본편은 같은 사건에서 이미 올린 문장을 다시 쓸 수 없다. 맞는 문장이든 틀린 문장이든 똑같이 빠지므로
        /// 빠진 것을 보고 무엇이 정답이었는지 알 수 없다. 같은 글을 되풀이해 믿음도를 끝없이 깎는 것을 막는다.
        /// 남은 문장 가운데 앞에서부터 ShownPerSection 개가 보이고, 뒤에 둔 문장(다른 근거로 다시 말하는 정정)이 차례로 올라온다.
        /// 근거(단서나 분석 기록)가 있어야 쓰는 문장은 그 근거를 얻은 뒤에 보인다.
        /// 튜토리얼은 예전 그대로다(틀린 글은 한영이 내려 주고, 맞는 글을 다시 쓸 수 있다). 처음 성공하면 끝나 남용할 길이 없다.
        /// </summary>
        private List<int> Visible(PostWritingSO data, int section)
        {
            var shown = new List<int>();
            var list = data != null ? data.GetCandidates(section) : null;
            if (list == null) return shown;

            var used = data.IsTutorial ? null : UsedTextIds(data);
            for (int i = 0; i < list.Count && shown.Count < ShownPerSection; i++)
            {
                var c = list[i];
                if (c == null) continue;
                if (used != null && used.Contains(c.textId)) continue;
                if (!data.IsTutorial && !HasEvidence(data, c)) continue;
                shown.Add(i);
            }
            return shown;
        }

        /// <summary>이 글쓰기로 이미 올린 문장들. 저장본의 올린 글 기록(사건별 대응 기록)에서 읽는다.</summary>
        private HashSet<string> UsedTextIds(PostWritingSO data)
        {
            var used = new HashSet<string>();
            if (_save == null || _save.Current == null || _save.Current.boardPosts == null) return used;
            foreach (var r in _save.Current.boardPosts)
            {
                if (r == null || r.postWritingId != data.PostWritingId) continue;
                if (!string.IsNullOrEmpty(r.titleTextId)) used.Add(r.titleTextId);
                if (r.bodyTextIds != null) foreach (var id in r.bodyTextIds) used.Add(id);
            }
            return used;
        }

        private bool HasEvidence(PostWritingSO data, PostWritingCandidate c)
        {
            if (c.requiredEvidenceIds == null || c.requiredEvidenceIds.Count == 0) return true;
            if (_save == null || _save.Current == null) return false;
            foreach (var id in c.requiredEvidenceIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                bool has = _save.Current.acquiredClueIds.Contains(id) || _save.Current.analysisRecords.Contains(data.CaseId + "|" + id);
                if (!has) return false;
            }
            return true;
        }

        /// <summary>고른 문장이 이제 보이지 않으면(이미 올렸다) 그 칸을 비운다.</summary>
        private void ValidatePicks()
        {
            if (_data == null) return;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
                if (_picks[s] >= 0 && !Visible(_data, s).Contains(_picks[s])) _picks[s] = -1;
            if (_section >= PostWritingSO.SectionCount)
                for (int s = 0; s < PostWritingSO.SectionCount; s++) if (_picks[s] < 0) { _section = s; break; }
        }

        /// <summary>쓰던 글을 닫는다. 본편에서 사건이 해결되어 더 쓸 것이 없을 때 부른다.</summary>
        public void Close()
        {
            _busy = false;
            HideCompose();
            if (_communityScreen != null) _communityScreen.BindWrite(null);
        }

        /// <summary>지금 이 괴담 믿음도. 본편 글은 해결 판정과 같은 값, 튜토리얼 글은 게시판 글 평균(정수)이다.</summary>
        private float CurrentBelief()
        {
            if (_data == null || _tutorial == null) return 0f;
            if (!_data.IsTutorial && BeliefOf != null) return BeliefOf(_data.LegendId);
            return _tutorial.GetLegendBelief(_data.LegendId);
        }

        private float CurrentOverall()
        {
            if (_data != null && !_data.IsTutorial && OverallOf != null) return OverallOf();
            return _tutorial != null ? _tutorial.BoardBelief : 0f;
        }

        private static string P(float v) => (Mathf.Round(v * 10f) / 10f).ToString("0.#");

        /// <summary>사건을 새로 열 때 부른다. 지난번 사건의 횟수와 올린 글 기록과 고른 문장을 버린다.</summary>
        public void ResetProgress()
        {
            StopAllCoroutines();
            _data = null;
            _attempts = 0;
            _lastPost = null;
            _lastShift = 0;
            _busy = false;
            _introTalked = false;
            _takenDownNotice = null;
            _resultBuilder = null;
            ClearPicks();
            HideCompose();
        }

        private void ClearPicks()
        {
            _section = 0;
            for (int i = 0; i < _picks.Length; i++) _picks[i] = -1;
        }

        private void HideCompose()
        {
            _view = View.None;
            if (_compose != null) _compose.Hide();
        }

        // ------------------------------------------------------------- 쓰기

        /// <summary>
        /// 글쓰기 단추를 눌렀을 때. 글쓰기 화면을 먼저 펴고, 튜토리얼이면 한영이 그 위에서 한마디 한다.
        /// 보고서를 그대로 올리면 안 된다는 것, 읽는 사람이 안심하게 써야 한다는 것을 이른다.
        /// 창을 닫았다 다시 열면 고르던 칸에서 이어 쓴다.
        /// </summary>
        public void Begin(PostWritingSO data)
        {
            if (data == null || _communityScreen == null || _busy || !CanWrite(data)) return;

            if (_data != data) ClearPicks();
            _data = data;
            ValidatePicks();

            _compose = PostComposeView.Ensure(_communityScreen.Window, _communityScreen.BodyFont, _communityScreen.TitleFont);
            if (_compose == null) return;

            Debug.Log($"[PostWriting] 글쓰기 시작 | {data.PostWritingId} | {_attempts + 1}번째 | {_section + 1}칸부터");

            if (_section >= PostWritingSO.SectionCount) ShowPreview();
            else ShowStep();

            if (data.IsTutorial && !_introTalked && _tutorial != null)
            {
                _introTalked = _tutorial.ShowComputerTalk(data.IntroLineTextId, null);
            }
        }

        private string L(string id) => _loc != null ? _loc.Get(id) : id;
        private string L(string id, params object[] args) => _loc != null ? _loc.Get(id, args) : id;

        /// <summary>이 괴담의 지금 믿음도와 성공 / 우수 기준(%). 올리기 전 화면과 올린 뒤 평가가 같은 값을 쓴다.</summary>
        private void Lines(float before, out float success, out float excellent)
        {
            success = Mathf.Max(before - _data.RequiredDrop, _data.TutorialTargetBelief);
            excellent = before - _data.ExcellentDrop;
            if (excellent > success) excellent = success;
        }

        /// <summary>
        /// 한 칸을 고르는 화면. 위에는 쓰고 있는 글, 가운데에 후보 셋, 아래에 이전 / 다음.
        /// 고른 문장은 곧바로 위의 글에 들어가 보인다. 믿음이 어느 쪽으로 가는지는 보이지 않는다.
        /// </summary>
        private void ShowStep()
        {
            _view = View.Step;
            int section = Mathf.Clamp(_section, 0, PostWritingSO.SectionCount - 1);

            ValidatePicks();
            float before = CurrentBelief();
            Lines(before, out float success, out float excellent);

            var names = new string[PostWritingSO.SectionCount];
            for (int i = 0; i < names.Length; i++) names[i] = L(SectionNameTextIds[i]);

            var body = new List<string>();
            int current = -1;
            for (int s = 1; s < PostWritingSO.SectionCount; s++)
            {
                if (_picks[s] >= 0)
                {
                    if (s == section) current = body.Count;
                    body.Add(L(Picked(s).textId));
                }
                else if (s == section)
                {
                    current = body.Count;
                    body.Add(L(BodyPlaceholderTextIds[s]));
                }
            }

            var candidates = new List<string>();
            var list = _data.GetCandidates(section);
            var visible = Visible(_data, section);
            foreach (int i in visible) candidates.Add(list[i] != null ? L(list[i].textId) : string.Empty);

            _compose.ShowStep(new PostComposeView.Step
            {
                Header = L(ComposeHeaderTextId, section + 1, PostWritingSO.SectionCount, names[section]),
                Notice = _takenDownNotice != null ? _takenDownNotice() : null,
                Account = L(ComposeAccountTextId, L(_data.BoardTextId), L(_data.AuthorTextId)),
                Goal = before > 0 ? L(GoalTextId, P(before), P(success), P(excellent)) : null,
                StepNames = names,
                StepIndex = section,
                // 정정글이면 무엇을 바로잡는 글인지 쓰고 있는 글 맨 위에 밝힌다.
                DraftLabel = string.IsNullOrEmpty(_data.ReplyLineTextId) ? L(DraftLabelTextId) : L(DraftLabelTextId) + "   ·   " + L(_data.ReplyLineTextId),
                DraftTitle = _picks[0] >= 0 ? L(Picked(0).textId) : L(TitlePlaceholderTextId),
                DraftTitleIsCurrent = section == 0,
                DraftBody = body,
                CurrentBodyIndex = current,
                Prompt = L(PromptTextIds[section]),
                Candidates = candidates,
                Selected = visible.IndexOf(_picks[section]),
                PrevLabel = L(PrevTextId),
                NextLabel = L(section == PostWritingSO.SectionCount - 1 ? ToPreviewTextId : NextTextId),
                CanPrev = section > 0,
                OnSelect = OnCandidatePicked,
                OnPrev = OnPrev,
                OnNext = OnNext,
            });
        }

        private PostWritingCandidate Picked(int section)
        {
            var list = _data.GetCandidates(section);
            int i = _picks[section];
            return list != null && i >= 0 && i < list.Count ? list[i] : null;
        }

        /// <summary>후보 하나를 골랐다. 위의 글에만 들어간다. 다음 단계로 가는 것은 플레이어가 정한다.</summary>
        private void OnCandidatePicked(int index)
        {
            if (_busy || _data == null || _section >= PostWritingSO.SectionCount) return;
            var list = _data.GetCandidates(_section);
            var visible = Visible(_data, _section);
            if (list == null || index < 0 || index >= visible.Count) return;

            _picks[_section] = visible[index];
            Debug.Log($"[PostWriting] {_section}칸 | {list[visible[index]].candidateId}");
            ShowStep();
        }

        private void OnPrev()
        {
            if (_busy || _section <= 0) return;
            _section--;
            ShowStep();
        }

        private void OnNext()
        {
            if (_busy || _section >= PostWritingSO.SectionCount || _picks[_section] < 0) return;
            _takenDownNotice = null;
            _section++;
            if (_section >= PostWritingSO.SectionCount) ShowPreview();
            else ShowStep();
        }

        /// <summary>게시판에 걸릴 모습 그대로. 다시 쓰기는 고른 것을 그대로 둔 채 첫 칸으로 돌아간다.</summary>
        private void ShowPreview()
        {
            _view = View.Preview;
            _section = PostWritingSO.SectionCount;

            var body = new List<string>();
            if (!string.IsNullOrEmpty(_data.ReplyLineTextId)) body.Add(L(_data.ReplyLineTextId));
            for (int s = 1; s < PostWritingSO.SectionCount; s++) body.Add(L(Picked(s).textId));

            _compose.ShowPreview(new PostComposeView.Preview
            {
                Header = L(PreviewHeaderTextId),
                Sub = L(PreviewSubTextId),
                Board = L(PreviewBoardTextId, L(_data.BoardTextId)),
                Title = L(Picked(0).textId),
                Meta = L(PreviewMetaTextId, L(_data.AuthorTextId)),
                Body = body,
                Note = L(PreviewNoteTextId, L(_data.AuthorTextId)),
                RewriteLabel = L(RewriteTextId),
                PublishLabel = L(PublishTextId),
                OnRewrite = () =>
                {
                    if (_busy) return;
                    _section = 0;
                    ShowStep();
                },
                OnPublish = AskPublish,
            });
        }

        private const string ConfirmPostTextId = "ui.post.confirm_post";
        private const string ConfirmYesTextId = "ui.post.confirm_post_yes";
        private const string ConfirmBackTextId = "ui.post.confirm_back";

        /// <summary>
        /// 올리기 직전의 마지막 확인. 정답이나 믿음도가 얼마나 바뀔지는 보여 주지 않는다.
        /// "다시 확인하기"를 고르면 미리보기로 돌아가고 아무것도 바뀌지 않는다. 올리기를 고른 그 순간에만 글이 걸린다.
        /// </summary>
        private void AskPublish()
        {
            if (_busy || _data == null || _compose == null) return;
            ConfirmDialog.Show(_communityScreen.Window, _communityScreen.BodyFont, _communityScreen.TitleFont,
                L(ConfirmPostTextId), L(ConfirmYesTextId), L(ConfirmBackTextId),
                Publish,
                () => { if (!_busy) ShowPreview(); });
        }

        // ------------------------------------------------------------- 올리기

        /// <summary>고른 문장들과 글 하나의 몫을 더한 값. 이 괴담을 실어 나르는 글들이 이만큼 움직인다.</summary>
        private int SumDelta()
        {
            float total = _data.PostModifier;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c != null) total += c.beliefDelta;
            }
            return Mathf.RoundToInt(total);
        }

        private enum Outcome { Excellent, Success, Weak, Flat, Fail }

        /// <summary>
        /// 판정. 올린 뒤 믿음도가 얼마나 내려갔는가로 가른다.
        /// 우수와 성공은 덧붙은 거짓을 걷어낸 문장(사실 칸의 반박)을 골랐을 때만 나온다.
        /// 사실이라도 "진짜 있다"만 강조하면 사람들은 더 믿는다. 거짓을 걷어내야 믿음이 내려간다.
        /// </summary>
        private Outcome Judge(int shift, float before, float after)
        {
            if (shift > 0 || after > before) return Outcome.Fail;
            if (Mathf.Approximately(after, before)) return Outcome.Flat;

            int refuteSection = _data.RefuteSection;
            bool refuted = refuteSection < 0 || (Picked(refuteSection) != null && Picked(refuteSection).tag == PostTone.Refutation);
            if (!refuted) return Outcome.Weak;

            Lines(before, out float success, out float excellent);
            if (after <= excellent) return Outcome.Excellent;
            if (after <= success) return Outcome.Success;
            return Outcome.Weak;
        }

        /// <summary>
        /// 확인창에서 올리기를 골랐다. 글을 실제로 게시판에 건다.
        ///
        /// 글이 걸리는 것, 저장본에 적히는 것, 믿음도가 움직이는 것이 모두 이 자리에서 한 번에 일어난다.
        /// 반응 댓글은 그 뒤에 하나씩 보일 뿐이다. 그래서 반응을 보는 사이에 저장하고 불러와도 글이 빠지거나 몫이 두 번 들지 않는다.
        /// 한 번 올린 글은 고치거나 지우거나 되돌리지 않는다. 틀린 글로 오른 믿음도는 새 정정글로만 낮출 수 있다.
        /// </summary>
        private void Publish()
        {
            if (_tutorial == null || _busy || _data == null) return;
            for (int s = 0; s < PostWritingSO.SectionCount; s++) if (Picked(s) == null) return;
            _busy = true;

            // 올리기를 확정했다. 본편이면 글을 쓰고 올리는 데 드는 시간이 여기서 한 번 흐른다(그동안의 확산 포함).
            // 그 뒤의 믿음도에서 이 글의 몫이 움직인다. 평가 화면의 숫자와 해결 목표 줄의 숫자가 같은 시점을 가리키게 한다.
            Published?.Invoke(_data);

            int shift = SumDelta();
            float before = CurrentBelief();
            float overallBefore = CurrentOverall();

            var body = new List<string>();
            // 정정글이면 본문 맨 위에 원글을 밝히는 한 줄이 붙는다. 게시판에 걸린 글에도, 저장본에도 그대로 남는다.
            if (!string.IsNullOrEmpty(_data.ReplyLineTextId)) body.Add(_data.ReplyLineTextId);
            for (int s = 1; s < PostWritingSO.SectionCount; s++) body.Add(Picked(s).textId);

            var entry = new CommunityBoardEntry
            {
                TitleTextId = Picked(0).textId,
                BodyTextIds = body,
                AuthorTextId = _data.AuthorTextId,
                BoardTextId = _data.BoardTextId,
                Views = _data.Views,
                PostedMinutesAgo = -GameClock.ElapsedMinutes,
                Comments = new List<CommunityComment>(),
                IsPlayerPost = true,
                PostId = _data.PostWritingId + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
            };

            _tutorial.AddBoardPost(entry);
            _attempts++;
            _lastPost = entry;

            // 믿음도가 움직인다. 게시판 몫이 바뀌면 작업 표시줄과 휴대폰 숫자가 따라 바뀐다.
            _tutorial.ShiftLegendBelief(_data.LegendId, shift);
            _lastShift = shift;
            if (_belief != null && _save != null) _belief.TryAddBelief(_save.Current, shift);

            float after = CurrentBelief();
            float overallAfter = CurrentOverall();
            var outcome = Judge(shift, before, after);
            bool good = outcome == Outcome.Excellent || outcome == Outcome.Success;
            entry.Likes = good ? _data.SuccessLikes : outcome == Outcome.Fail ? _data.FailLikes : _data.WeakLikes;
            entry.Dislikes = good ? _data.SuccessDislikes : outcome == Outcome.Fail ? _data.FailDislikes : _data.WeakDislikes;

            // 저장본에도 적는다. 게시판 목록은 저장하지 않으므로 다시 불러오면 이 기록으로 되살린다.
            // 이 글이 옮긴 몫도 함께 적는다. 게시판을 새로 지어 되살릴 때 이만큼만 한 번 다시 반영한다.
            if (_save != null && _save.Current != null)
            {
                var record = PlayerPostStore.Record(_save.Current, entry, _data.CaseId, _data.PostWritingId, _data.LegendId);
                if (record != null) record.beliefShift = shift;
                _save.MarkDirty();
            }

            // 글쓰기 화면을 걷고 올린 글을 연다. 게시판에 걸린 그 글이다. 반응은 이제부터 보인다.
            HideCompose();
            _tutorial.OpenFillerPost(entry);
            _communityScreen.SetControlsEnabled(false);

            Debug.Log($"[PostWriting] 게시 | {_data.PostWritingId} {_attempts}번째 | 몫 {shift:+0;-0;0}% | 이번 괴담 {P(before)}% -> {P(after)}% | 결과 {outcome}");
            StartCoroutine(PlayReactions(entry, before, after, overallBefore, overallAfter, outcome));
        }

        private IEnumerator PlayReactions(CommunityBoardEntry entry, float before, float after, float overallBefore, float overallAfter, Outcome outcome)
        {
            yield return new WaitForSecondsRealtime(ReactionInterval);

            string resultId = after < before ? ResultDownTextId : after > before ? ResultUpTextId : ResultFlatTextId;
            _communityScreen.ShowNotice(() => L(resultId, P(before), P(after), P(overallBefore), P(overallAfter)));

            bool good = outcome == Outcome.Excellent || outcome == Outcome.Success;
            var reactions = good ? _data.SuccessReactions
                : outcome == Outcome.Fail ? _data.FailReactions
                : _data.WeakReactions;

            for (int i = 0; reactions != null && i < reactions.Count; i++)
            {
                entry.Comments.Add(new CommunityComment
                {
                    AuthorTextId = reactions[i].authorTextId,
                    BodyTextId = reactions[i].bodyTextId,
                });
                _communityScreen.BindComments(entry.Comments);

                yield return new WaitForSecondsRealtime(ReactionInterval);
            }

            yield return new WaitForSecondsRealtime(ResultButtonDelay);

            Debug.Log($"[PostWriting] 반응 끝 | 이번 괴담 {P(before)}% -> {P(after)}% | 전체 {P(overallBefore)}% -> {P(overallAfter)}% | {outcome}");
            SyncSavedPosts();

            // 결과로 저절로 넘어가지 않는다. 댓글을 다 읽고 플레이어가 누르면 넘어간다.
            // 댓글 쓰기 칸이 있던 자리에 단추 하나를 세운다. 기존 게시글 화면의 선택지 자리다.
            _resultBuilder = () => BuildResult(before, after, overallBefore, overallAfter, outcome);
            _resultOpened = false;
            _communityScreen.SetChoiceHeader(ResultReadyTextId);
            _communityScreen.BindChoices(new List<TutorialCommentChoice>
            {
                new TutorialCommentChoice { ChoiceId = ResultChoiceId, BodyTextId = SeeResultTextId },
            }, c => c != null ? L(c.BodyTextId) : string.Empty, _ => OpenResult());
            _communityScreen.SetControlsEnabled(true);
            _communityScreen.ScrollPostToBottom();
        }

        /// <summary>"게시 결과 확인"을 눌렀다. 두 번 눌러도 한 번만 연다.</summary>
        private void OpenResult()
        {
            if (_resultOpened || _resultBuilder == null) return;
            _resultOpened = true;

            _communityScreen.SetChoiceHeader(null);
            _communityScreen.BindChoices(null, null, null);
            _view = View.Result;
            _compose.ShowResult(_resultBuilder());
        }

        private bool _resultOpened;
        private const string ResultChoiceId = "post_see_result";
        private const string SeeResultTextId = "ui.post.btn_see_result";
        private const string ResultReadyTextId = "ui.post.result_ready";

        /// <summary>마지막 댓글이 달리고 결과 확인 단추가 서기까지의 뜸(초). 짧게 둔다.</summary>
        private const float ResultButtonDelay = 0.6f;

        private PostComposeView.Result BuildResult(float before, float after, float overallBefore, float overallAfter, Outcome outcome)
        {
            Lines(before, out float success, out float excellent);

            // 믿음도를 가장 크게 올린 칸에 작은 경고를 붙인다. 올린 칸이 없으면 붙이지 않는다.
            int worst = WorstSection();

            var rows = new List<PostComposeView.ResultRow>();
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c == null) continue;
                int d = Mathf.RoundToInt(c.beliefDelta);
                rows.Add(new PostComposeView.ResultRow
                {
                    Section = L(SectionNameTextIds[s]),
                    Sentence = L(c.textId),
                    Direction = Math.Sign(d),
                    Effect = d < 0 ? L(EffectDownTextId, -d) : d > 0 ? L(EffectUpTextId, d) : L(EffectFlatTextId),
                    Review = string.IsNullOrEmpty(c.reviewTextId) ? string.Empty : L(c.reviewTextId),
                    Warning = s == worst ? L(WorstWarningTextId) : null,
                });
            }

            int level = outcome == Outcome.Excellent ? 2 : outcome == Outcome.Success ? 1 : outcome == Outcome.Fail ? -1 : 0;
            return new PostComposeView.Result
            {
                Header = L(ResultHeaderTextId),
                Grade = L("ui.post.grade_" + outcome.ToString().ToLowerInvariant()),
                GradeLevel = level,
                Legend = L(ResultLegendTextId, P(before), P(after), Mathf.Round((after - before) * 10f) / 10f),
                // 게시물 평가 기준. 사건 해결 목표는 다른 기준이라 본편이면 아래에 따로 적는다.
                Goal = L(ResultGoalTextId, P(success), P(excellent), Mathf.RoundToInt(_data.PostModifier))
                       + (CaseGoalLine != null && !_data.IsTutorial ? "\n" + CaseGoalLine(_data) : string.Empty),
                Overall = L(ResultOverallTextId, P(overallBefore), P(overallAfter)),
                Rows = rows,
                ConfirmLabel = L(ConfirmTextId),
                OnConfirm = () => OnResultConfirmed(outcome),
            };
        }

        /// <summary>평가를 읽었다. 한영이 한마디 하고, 잘 썼으면 끝내고 아니면 다시 쓴다.</summary>
        private void OnResultConfirmed(Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Excellent: Talk(_data.ExcellentLineTextId, OnSucceeded); break;
                case Outcome.Success: Talk(_data.SuccessLineTextId, OnSucceeded); break;
                case Outcome.Fail: Talk(_data.FailLineTextId, TalkFixHint); break;
                case Outcome.Flat: Talk(_data.FlatLineTextId, TalkFixHint); break;
                default: Talk(_data.WeakLineTextId, TalkFixHint); break;
            }
            // 본편 글은 한영의 말이 없어 위에서 곧바로 끝까지 간다. 그 뒤에 사건 해결을 따진다.
            ResultConfirmed?.Invoke(_data);
        }

        private const string WorstWarningTextId = "ui.post.worst_warning";
        private static readonly string[] FixHintTextIds =
        {
            "post.fix.title",
            "post.fix.intro",
            "post.fix.fact",
            "post.fix.action",
        };

        /// <summary>믿음도를 가장 크게 올린 칸. 올린 칸이 없으면 -1.</summary>
        private int WorstSection()
        {
            int worst = -1;
            float max = 0f;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c != null && c.beliefDelta > max) { max = c.beliefDelta; worst = s; }
            }
            return worst;
        }

        /// <summary>
        /// 먼저 고쳐야 할 칸. 덧붙은 거짓을 걷어내지 않았으면 그 칸이 먼저다. 아니면 믿음을 가장 크게 올린 칸,
        /// 그것도 없으면 가장 적게 내린 칸이다. 어느 문장으로 바꾸라고는 말하지 않는다. 칸만 짚는다.
        /// </summary>
        private int SectionToFix()
        {
            int rs = _data.RefuteSection;
            if (rs >= 0 && rs < PostWritingSO.SectionCount && (Picked(rs) == null || Picked(rs).tag != PostTone.Refutation)) return rs;

            int worst = WorstSection();
            if (worst >= 0) return worst;

            float max = float.MinValue;
            for (int s = 0; s < PostWritingSO.SectionCount; s++)
            {
                var c = Picked(s);
                if (c != null && c.beliefDelta > max) { max = c.beliefDelta; worst = s; }
            }
            return worst;
        }

        /// <summary>못 미쳤을 때 한영이 한 칸을 짚어 준다. 정답을 골라 주지는 않는다.</summary>
        private void TalkFixHint()
        {
            // 한영이 곁에 있는 튜토리얼에서만 고칠 칸을 짚어 준다. 본편에서는 평가 화면의 설명을 읽고 고친다.
            if (_data == null || !_data.IsTutorial) { AfterMiss(); return; }
            int s = SectionToFix();
            if (s < 0) { AfterMiss(); return; }
            Talk(FixHintTextIds[s], AfterMiss);
        }

        /// <summary>한영이 말하게 한다. 말할 수 없으면 곧바로 다음으로 간다.</summary>
        private void Talk(string lineTextId, Action onDone)
        {
            if (string.IsNullOrEmpty(lineTextId)) { onDone?.Invoke(); return; }
            bool talked = _tutorial != null && _tutorial.ShowComputerTalk(lineTextId, onDone);
            if (!talked) onDone?.Invoke();
        }

        /// <summary>잘 썼다. 글쓰기는 여기서 끝나고 저장본에 남는다.</summary>
        private void OnSucceeded()
        {
            _busy = false;
            HideCompose();

            if (_save != null && _save.Current != null)
            {
                string flag = PublishedFlagPrefix + _data.PostWritingId;
                if (!_save.Current.storyFlags.Contains(flag)) _save.Current.storyFlags.Add(flag);
                _save.MarkDirty();

                // 성공한 글과 그 반응을 곧바로 저장한다. 성공 표시만 남고 글이 사라지는 일이 없게 한다.
                _save.AutoSave();
            }

            _communityScreen.BindWrite(null);
            Debug.Log($"[PostWriting] 성공 | {_data.PostWritingId}");

            Succeeded?.Invoke(_data);
        }

        /// <summary>
        /// 목표에 못 미쳤다. 다시 쓸 수 있으면 새 정정글을 쓰게 한다. 고른 문장은 그대로 두고 첫 칸부터 다시 본다.
        ///
        /// 본편: 올린 글은 게시판에 그대로 남고, 그 글로 오른 믿음도도 되돌리지 않는다. 새 정정글로만 바로잡는다.
        /// 튜토리얼: 예전 그대로 한영이 실패한 글을 내려 주고 그 글이 올린 믿음도를 되돌린다. 처음 배우는 자리라 한 번 봐주는 것이다.
        ///           내려 주면서, 본편에서는 한 번 올린 글을 내릴 수 없다는 것을 한영이 일러 둔다(데이터의 RewriteLineTextId).
        /// 순서: 한영이 내리겠다고(그리고 앞으로는 못 내린다고) 말한다 → 넘기면 실제로 내린다 → 목록이 갱신된 것을 보여 준다 → 다시 쓰기.
        /// </summary>
        private void AfterMiss()
        {
            if (!CanWrite(_data))
            {
                _busy = false;
                HideCompose();
                _communityScreen.BindWrite(null);
                Debug.Log($"[PostWriting] 더 쓸 수 없다 | {_data.PostWritingId} | {_attempts}번 올림");
                return;
            }

            if (_data.IsTutorial && _lastPost != null)
            {
                // 결과 화면을 띄운 채로 말한다. 뒤에 내릴 글이 보이지 않는다.
                Talk(_data.RewriteLineTextId, () => StartCoroutine(TakeDownThenRewrite()));
                return;
            }
            Rewrite();
        }

        private void Rewrite()
        {
            _busy = false;
            _section = 0;
            ValidatePicks();
            ShowStep();
        }

        /// <summary>목록이 갱신된 것을 보여 주는 시간(초).</summary>
        private const float TakenDownBoardSeconds = 1.2f;

        /// <summary>튜토리얼에서만. 실패한 글을 내리고 그 글이 옮긴 믿음도를 되돌린 뒤 다시 쓰기로 간다.</summary>
        private IEnumerator TakeDownThenRewrite()
        {
            _communityScreen.SetControlsEnabled(false);

            int before = _tutorial.GetLegendBelief(_data.LegendId);
            int overallBefore = _tutorial.BoardBelief;

            _tutorial.RemoveBoardPost(_lastPost);
            if (_save != null) PlayerPostStore.MarkRemoved(_save.Current, _lastPost.PostId);
            _tutorial.ShiftLegendBelief(_data.LegendId, -_lastShift);
            if (_belief != null && _save != null) _belief.TryAddBelief(_save.Current, -_lastShift);
            if (_save != null) _save.MarkDirty();

            int after = _tutorial.GetLegendBelief(_data.LegendId);
            int overallAfter = _tutorial.BoardBelief;
            _lastPost = null;
            _lastShift = 0;

            Debug.Log($"[PostWriting] 튜토리얼 | 실패한 글을 내림 | 이번 괴담 {before}% -> {after}% | 전체 {overallBefore}% -> {overallAfter}%");

            // 내린 뒤의 게시판 목록. 그 글이 빠진 것을 잠깐 보여 준다.
            HideCompose();
            _communityScreen.ShowNotice(null);
            _communityScreen.ShowBoard(true);
            yield return new WaitForSecondsRealtime(TakenDownBoardSeconds);

            _communityScreen.SetControlsEnabled(true);
            _takenDownNotice = () => L(TakenDownTextId, before, after, overallBefore, overallAfter);
            Rewrite();
        }

        private const string TakenDownTextId = "ui.post.taken_down";

        /// <summary>글을 내린 직후 다시 쓰기 화면 맨 위에 한 번 띄우는 알림. 다음 단계로 가면 사라진다.</summary>
        private Func<string> _takenDownNotice;
    }
}
