using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 괴담넷 글쓰기 화면. 괴담넷 창 안, 제목 표시줄 아래를 통째로 덮는다.
    ///
    /// 글을 쓰는 동안에는 좋아요 / 댓글 같은 게시글 화면을 보이지 않는다. 아직 올리지 않은 글이기 때문이다.
    /// 세 가지 모습이 있다.
    ///   작성   - "게시물 작성 n/4", 쓰고 있는 글 미리보기, 후보 카드 셋, 아래 고정된 이전 / 다음 단추
    ///   미리보기 - 게시판에 걸릴 모습 그대로의 글, 아래 고정된 다시 쓰기 / 올리기 단추
    ///   결과   - 올린 뒤의 평가. 믿음도 변화와 칸마다 고른 문장이 어떤 영향을 줬는지
    ///
    /// 문구는 부르는 쪽(PostWritingDirector)이 이미 번역해서 넘긴다. 여기서는 그리기만 한다.
    /// 처음 쓸 때 스스로 짓는다. 씬 빌더를 건드리지 않는다.
    /// </summary>
    public class PostComposeView : MonoBehaviour
    {
        // ------------------------------------------------------------- 넘겨받는 내용

        public class Step
        {
            public string Header;           // 게시물 작성 2/4 · 도입
            public string Notice;           // 글을 내린 직후 한 번 띄우는 알림. 비어 있으면 없다
            public string Account;          // 실화게시판 · 괴담 분석 덕후 (차지한의 부계정)
            public string Goal;             // 목표 — ...
            public string[] StepNames;      // 제목 / 도입 / 사실 전달 / 행동 안내
            public int StepIndex;
            public string DraftLabel;       // 쓰고 있는 글
            public string DraftTitle;       // 고른 제목, 아니면 자리 표시
            public bool DraftTitleIsCurrent;
            public List<string> DraftBody;  // 문단마다 한 줄. 지금 칸이면 CurrentBodyIndex 로 가리킨다
            public int CurrentBodyIndex = -1;
            public string Prompt;           // 도입으로 쓸 문장을 하나 고르세요
            public List<string> Candidates;
            public int Selected = -1;
            public string PrevLabel;
            public string NextLabel;
            public bool CanPrev;
            public Action<int> OnSelect;
            public Action OnPrev;
            public Action OnNext;
        }

        public class Preview
        {
            public string Header;
            public string Sub;
            public string Board;            // 괴담넷 › 실화게시판
            public string Title;
            public string Meta;             // 괴담 분석 덕후    조회 0    방금
            public List<string> Body;
            public string Note;             // 차지한의 부계정으로 올라간다는 안내(우리만 보는 것)
            public string RewriteLabel;
            public string PublishLabel;
            public Action OnRewrite;
            public Action OnPublish;
        }

        public class ResultRow
        {
            public string Section;
            public string Sentence;
            public string Effect;
            public int Direction;           // -1 내림, 0 그대로, 1 올림
            public string Review;
            public string Warning;          // 믿음을 가장 크게 올린 칸에만 붙는 작은 경고
        }

        public class Result
        {
            public string Header;
            public string Grade;
            public int GradeLevel;          // 2 우수, 1 성공, 0 부족 / 효과 없음, -1 역효과
            public string Legend;           // 이번 괴담 믿음도 31% → 20%
            public string Goal;             // 목표 24% 이하 · 20% 이하면 우수
            public string Overall;          // 전체 괴담 믿음도 ...
            public List<ResultRow> Rows;
            public string ConfirmLabel;
            public Action OnConfirm;
        }

        // ------------------------------------------------------------- 색

        private static readonly Color PageColor = new Color(0.94f, 0.94f, 0.95f, 1f);
        private static readonly Color Paper = Color.white;
        private static readonly Color Ink = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color Dim = new Color(0.33f, 0.35f, 0.39f, 1f);
        private static readonly Color Faint = new Color(0.47f, 0.49f, 0.53f, 1f);
        private static readonly Color Rule = new Color(0.86f, 0.87f, 0.90f, 1f);
        private static readonly Color Blue = new Color(0.18f, 0.38f, 0.78f, 1f);
        private static readonly Color BlueSoft = new Color(0.92f, 0.95f, 1f, 1f);
        private static readonly Color Gold = new Color(0.86f, 0.74f, 0.48f, 1f);
        private static readonly Color Pill = new Color(0.88f, 0.89f, 0.92f, 1f);
        private static readonly Color Down = new Color(0.16f, 0.52f, 0.32f, 1f);
        private static readonly Color Up = new Color(0.75f, 0.22f, 0.20f, 1f);
        private static readonly Color NoticeColor = new Color(1f, 0.97f, 0.88f, 1f);
        private static readonly Color NoticeEdge = new Color(0.90f, 0.78f, 0.45f, 1f);
        private static readonly Color NoticeInk = new Color(0.45f, 0.32f, 0.06f, 1f);

        private const float TitleBarHeight = 56f;
        private const float BarHeight = 104f;
        private const float StepWidth = 1240f;
        private const float ResultWidth = 1160f;

        /// <summary>결과 기둥을 옆으로 비켜 세울 거리. 지금은 가운데다.</summary>
        private const float ResultShift = 0f;

        // ------------------------------------------------------------- 짓기

        private TMP_FontAsset _regular;
        private TMP_FontAsset _bold;

        private RectTransform _root;
        private RectTransform _column;
        private RectTransform _bar;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>올리기를 한 번 눌렀는가. 두 번 올라가지 않게 막는다.</summary>
        private bool _locked;

        public bool IsShowing => _root != null && _root.gameObject.activeSelf;

        /// <summary>괴담넷 창 안에 글쓰기 화면을 짓는다. 이미 지었으면 그것을 돌려준다.</summary>
        public static PostComposeView Ensure(RectTransform window, TMP_FontAsset regular, TMP_FontAsset bold)
        {
            if (window == null) return null;
            var found = window.GetComponentInChildren<PostComposeView>(true);
            if (found != null) return found;

            var go = new GameObject("PostCompose", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(window, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, -TitleBarHeight);
            var bg = go.GetComponent<Image>();
            bg.color = PageColor;
            bg.raycastTarget = true;   // 아래 게시글 화면을 누르지 못하게 막는다

            var view = go.AddComponent<PostComposeView>();
            view._regular = regular;
            view._bold = bold != null ? bold : regular;
            view._root = rt;
            go.SetActive(false);
            return view;
        }

        /// <summary>한영이 말하는 동안처럼 누르면 안 될 때 잠근다. 괴담넷의 다른 단추와 함께 잠긴다.</summary>
        public void SetInteractable(bool on)
        {
            if (_root == null) return;
            var group = _root.GetComponent<CanvasGroup>();
            if (group == null) group = _root.gameObject.AddComponent<CanvasGroup>();
            group.interactable = on;
        }

        public void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void Begin(float width, float shift)
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            foreach (var go in _spawned) if (go != null) { go.SetActive(false); Destroy(go); }   // 지운 것은 이 프레임 끝까지 남는다. 곧바로 숨겨 눌리지 않게 한다
            _spawned.Clear();

            // 가운데 기둥. 위에서부터 차례로 쌓는다.
            _column = NewRect("Column", _root);
            _column.anchorMin = new Vector2(0.5f, 1f);
            _column.anchorMax = new Vector2(0.5f, 1f);
            _column.pivot = new Vector2(0.5f, 1f);
            _column.anchoredPosition = new Vector2(shift, -34f);
            _column.sizeDelta = new Vector2(width, 0f);
            var layout = _column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14f;
            var fit = _column.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _spawned.Add(_column.gameObject);

            // 아래 고정 단추 줄. 내용이 얼마나 길든 늘 같은 자리에 있다.
            _bar = NewRect("Bar", _root);
            _bar.anchorMin = new Vector2(0f, 0f);
            _bar.anchorMax = new Vector2(1f, 0f);
            _bar.pivot = new Vector2(0.5f, 0f);
            _bar.sizeDelta = new Vector2(0f, BarHeight);
            _bar.anchoredPosition = Vector2.zero;
            var barBg = _bar.gameObject.AddComponent<Image>();
            barBg.color = Paper;
            var top = Box("Rule", _bar, Rule);
            Place(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), Vector2.zero);
            _spawned.Add(_bar.gameObject);
        }

        // ------------------------------------------------------------- 작성

        public void ShowStep(Step s)
        {
            _locked = false;
            Begin(StepWidth, 0f);

            // 머리말: 왼쪽에 단계, 오른쪽에 어느 게시판에 누구 이름으로 올라가는지.
            var head = Row("Head", 52f);
            var h = Label("Header", head, _bold, 36f, Ink, TextAlignmentOptions.Left);
            Stretch(h.rectTransform);
            h.text = s.Header;
            var acc = Label("Account", head, _regular, 21f, Dim, TextAlignmentOptions.Right);
            Stretch(acc.rectTransform);
            acc.text = s.Account;

            // 글을 내린 직후의 알림. 실제로 내린 뒤에만 들어온다.
            if (!string.IsNullOrEmpty(s.Notice))
            {
                var notice = Card("Notice", NoticeColor, 22f, 10f, 0f);
                Border(notice, NoticeEdge);
                Para(s.Notice, _bold, 21f, NoticeInk, notice);
            }

            // 네 단계. 지난 것은 ✓, 지금 것은 파랗게.
            var pills = Row("Steps", 42f);
            int n = s.StepNames != null ? s.StepNames.Length : 0;
            float gap = 10f, w = (StepWidth - gap * (n - 1)) / Mathf.Max(1, n);
            for (int i = 0; i < n; i++)
            {
                bool done = i < s.StepIndex, now = i == s.StepIndex;
                var pill = Box("Step" + i, pills, now ? Blue : done ? BlueSoft : Pill);
                Place(pill.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(i * (w + gap), 0f), new Vector2(i * (w + gap) + w, 0f));
                var t = Label("Text", pill.transform, now ? _bold : _regular, 21f, now ? Color.white : done ? Blue : Dim, TextAlignmentOptions.Center);
                Stretch(t.rectTransform);
                t.text = (done ? "✓ " : (i + 1) + "  ") + s.StepNames[i];
            }

            if (!string.IsNullOrEmpty(s.Goal)) Para(s.Goal, _regular, 21f, Dim);

            // 쓰고 있는 글. 고른 문장이 차례로 쌓이고, 지금 칸은 파란 자리 표시로 선다.
            var draft = Card("Draft", Paper, 22f, 16f, 6f);
            Border(draft, Rule);
            Para(s.DraftLabel, _regular, 20f, Faint, draft);
            Para(s.DraftTitle, _bold, 27f, s.DraftTitleIsCurrent ? Blue : Ink, draft);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; s.DraftBody != null && i < s.DraftBody.Count; i++)
            {
                string line = s.DraftBody[i];
                if (sb.Length > 0) sb.Append('\n');
                if (i == s.CurrentBodyIndex) sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Blue)).Append(">").Append(line).Append("</color>");
                else sb.Append(line);
            }
            if (sb.Length > 0)
            {
                var body = Para(sb.ToString(), _regular, 23f, Ink, draft);
                body.lineSpacing = 6f;
                body.paragraphSpacing = 6f;
            }

            Spacer(4f);
            Para(s.Prompt, _bold, 26f, Ink);

            // 후보 셋. 고른 것은 파란 테두리와 ✓.
            for (int i = 0; s.Candidates != null && i < s.Candidates.Count; i++)
            {
                int index = i;
                bool on = i == s.Selected;
                var card = Card("Candidate" + i, on ? BlueSoft : Paper, 84f, 22f, 0f);
                card.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(84, 28, 18, 18);   // 왼쪽은 ✓ 칸 자리
                card.GetComponent<LayoutElement>().minHeight = 92f;
                if (on) Border(card, Blue, 3f); else Border(card, Rule);

                var mark = Box("Mark", card, on ? Blue : Paper);
                mark.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var mrt = mark.rectTransform;
                mrt.anchorMin = mrt.anchorMax = new Vector2(0f, 0.5f);
                mrt.pivot = new Vector2(0f, 0.5f);
                mrt.anchoredPosition = new Vector2(26f, 0f);
                mrt.sizeDelta = new Vector2(34f, 34f);
                if (!on) Border(mark.rectTransform, Faint, 2f);
                var check = Label("Check", mark.transform, _bold, 24f, Color.white, TextAlignmentOptions.Center);
                Stretch(check.rectTransform);
                check.text = on ? "✓" : string.Empty;

                var text = Para(s.Candidates[i], on ? _bold : _regular, 26f, Ink, card);
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.lineSpacing = 4f;   // 영어처럼 두 줄로 넘어갈 때 줄이 붙지 않게

                var button = card.gameObject.AddComponent<Button>();
                var bg = card.GetComponent<Image>();
                bg.raycastTarget = true;
                button.targetGraphic = bg;
                var colors = button.colors;
                colors.highlightedColor = new Color(0.96f, 0.97f, 1f, 1f);
                colors.pressedColor = new Color(0.90f, 0.93f, 0.99f, 1f);
                colors.selectedColor = Color.white;
                button.colors = colors;
                button.onClick.AddListener(() => s.OnSelect?.Invoke(index));
            }

            var prev = BarButton("Prev", s.PrevLabel, false, -StepWidth * 0.5f, 0f, 260f);
            prev.interactable = s.CanPrev;
            prev.onClick.AddListener(() => s.OnPrev?.Invoke());

            var next = BarButton("Next", s.NextLabel, true, StepWidth * 0.5f, 1f, 300f);
            next.interactable = s.Selected >= 0;
            next.onClick.AddListener(() => s.OnNext?.Invoke());
        }

        // ------------------------------------------------------------- 미리보기

        public void ShowPreview(Preview p)
        {
            _locked = false;
            Begin(StepWidth, 0f);

            Para(p.Header, _bold, 36f, Ink);
            Para(p.Sub, _regular, 21f, Dim);

            // 게시판에 걸릴 모습 그대로.
            var paper = Card("Paper", Paper, 34f, 28f, 10f);
            Border(paper, Rule);
            Para(p.Board, _regular, 20f, Blue, paper);
            Para(p.Title, _bold, 33f, Ink, paper);
            Para(p.Meta, _regular, 20f, Dim, paper);
            var rule = Box("Rule", paper, Rule);
            rule.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
            var body = Para(string.Join("\n", p.Body ?? new List<string>()), _regular, 26f, Ink, paper);
            body.lineSpacing = 10f;
            body.paragraphSpacing = 14f;

            if (!string.IsNullOrEmpty(p.Note)) Para(p.Note, _regular, 20f, Dim);

            var rewrite = BarButton("Rewrite", p.RewriteLabel, false, -StepWidth * 0.5f, 0f, 260f);
            var publish = BarButton("Publish", p.PublishLabel, true, StepWidth * 0.5f, 1f, 300f);

            rewrite.onClick.AddListener(() =>
            {
                if (_locked) return;
                p.OnRewrite?.Invoke();
            });
            publish.onClick.AddListener(() =>
            {
                // 두 번 눌러도 한 번만 올라간다.
                if (_locked) return;
                _locked = true;
                publish.interactable = false;
                rewrite.interactable = false;
                p.OnPublish?.Invoke();
            });
        }

        // ------------------------------------------------------------- 결과

        public void ShowResult(Result r)
        {
            _locked = false;
            Begin(ResultWidth, ResultShift);

            var head = Row("Head", 52f);
            var h = Label("Header", head, _bold, 34f, Ink, TextAlignmentOptions.Left);
            Stretch(h.rectTransform);
            h.text = r.Header;

            Color gradeColor = r.GradeLevel >= 2 ? Blue : r.GradeLevel == 1 ? Down : r.GradeLevel == 0 ? new Color(0.70f, 0.50f, 0.12f, 1f) : Up;
            var chip = Box("Grade", head, gradeColor);
            var crt = chip.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot = new Vector2(1f, 0.5f);
            crt.sizeDelta = new Vector2(220f, 46f);
            var grade = Label("Text", chip.transform, _bold, 24f, Color.white, TextAlignmentOptions.Center);
            Stretch(grade.rectTransform);
            grade.text = r.Grade;

            var belief = Card("Belief", Paper, 26f, 16f, 4f);
            Border(belief, Rule);
            Para(r.Legend, _bold, 32f, Ink, belief);
            Para(r.Goal, _regular, 21f, Dim, belief);
            Para(r.Overall, _regular, 21f, Dim, belief);

            for (int i = 0; r.Rows != null && i < r.Rows.Count; i++)
            {
                var row = r.Rows[i];
                var card = Card("Row" + i, Paper, 26f, 12f, 3f);
                bool warn = !string.IsNullOrEmpty(row.Warning);
                if (warn) Border(card, Up, 2f); else Border(card, Rule);

                var line = Row("Top", 30f, card);
                var sec = Label("Section", line, _bold, 21f, Dim, TextAlignmentOptions.Left);
                Stretch(sec.rectTransform);
                // 믿음을 가장 크게 올린 칸에는 작은 경고를 붙인다. 이것만 빨간 테두리다.
                sec.text = warn
                    ? row.Section + "   <color=#" + ColorUtility.ToHtmlStringRGB(Up) + ">" + row.Warning + "</color>"
                    : row.Section;
                var eff = Label("Effect", line, _bold, 21f, row.Direction < 0 ? Down : row.Direction > 0 ? Up : Dim, TextAlignmentOptions.Right);
                Stretch(eff.rectTransform);
                eff.text = row.Effect;

                Para(row.Sentence, _regular, 23f, Ink, card);
                Para(row.Review, _regular, 21f, Dim, card);
            }

            var ok = BarButton("Confirm", r.ConfirmLabel, true, ResultShift + ResultWidth * 0.5f, 1f, 300f);
            ok.onClick.AddListener(() =>
            {
                if (_locked) return;
                _locked = true;
                ok.interactable = false;
                r.OnConfirm?.Invoke();
            });
        }

        // ------------------------------------------------------------- 조각

        private TMP_Text Para(string text, TMP_FontAsset font, float size, Color color, RectTransform parent = null)
        {
            var t = Label("Text", parent != null ? parent : _column, font, size, color, TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.text = text ?? string.Empty;
            return t;
        }

        private void Spacer(float height)
        {
            var rt = NewRect("Spacer", _column);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        /// <summary>층에 세우는 높이가 정해진 한 줄. 안에 든 것은 손으로 자리를 잡는다.</summary>
        private RectTransform Row(string name, float height, RectTransform parent = null)
        {
            var rt = NewRect(name, parent != null ? parent : _column);
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            return rt;
        }

        /// <summary>안에 글을 쌓는 판. 높이는 글 길이만큼 자란다.</summary>
        private RectTransform Card(string name, Color color, float padX, float padY, float spacing)
        {
            var image = Box(name, _column, color);
            var rt = image.rectTransform;
            var layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(Mathf.RoundToInt(padX), Mathf.RoundToInt(padX), Mathf.RoundToInt(padY), Mathf.RoundToInt(padY));
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            rt.gameObject.AddComponent<LayoutElement>();
            return rt;
        }

        /// <summary>아래 단추 줄의 단추. 기둥의 왼쪽 끝 또는 오른쪽 끝에 맞춘다.</summary>
        private Button BarButton(string name, string label, bool primary, float x, float pivotX, float width)
        {
            var image = Box(name, _bar, primary ? Gold : Paper);
            image.raycastTarget = true;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(pivotX, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, 64f);
            if (!primary) Border(rt, new Color(0.72f, 0.74f, 0.78f, 1f));

            var t = Label("Label", rt, _bold, 25f, Ink, TextAlignmentOptions.Center);
            Stretch(t.rectTransform);
            t.text = label;

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.80f, 0.55f);
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            button.colors = colors;
            return button;
        }

        private static void Border(RectTransform parent, Color color, float width = 1f)
        {
            Line(parent, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -width), Vector2.zero, color);
            Line(parent, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, width), color);
            Line(parent, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(width, 0f), color);
            Line(parent, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-width, 0f), Vector2.zero, color);
        }

        private static void Line(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            var line = Box("Border" + name, parent, color);
            line.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Place(line.rectTransform, min, max, offMin, offMax);
        }

        private TMP_Text Label(string name, Transform parent, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font != null ? font : TMP_Settings.defaultFontAsset;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.richText = true;
            return t;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Box(string name, Transform parent, Color color)
        {
            var rt = NewRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
        }
    }
}
