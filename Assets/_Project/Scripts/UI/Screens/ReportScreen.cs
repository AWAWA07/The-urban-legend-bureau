using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Localization;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 보고서 작성 화면.
    ///
    /// 조사가 끝나면 검열국에 낼 보고서를 쓴다. 글은 이미 적혀 있고 몇 군데가 비어 있다.
    /// 그 빈칸을 플레이어가 채운다. 답을 다시 묻는 것이 아니라, 알아낸 것을 제 손으로
    /// 한 장에 옮겨 적게 하는 자리다. 옮겨 적고 나면 그 장이 메모장에 남는다.
    ///
    /// 맞고 틀림은 여기서 가리지 않는다. 화면은 칸과 후보를 보여주고 고른 것을 넘겨줄 뿐이다.
    /// 어느 칸이 맞았는지는 CaseDirector 가 정해서 다시 알려 준다.
    /// </summary>
    public class ReportScreen : UIScreen
    {
        /// <summary>보고서의 빈칸 하나.</summary>
        public class Blank
        {
            /// <summary>칸 이름. 판정, 규칙, 파훼법 같은 것.</summary>
            public string Label;

            /// <summary>이 칸에 넣어 볼 수 있는 말들.</summary>
            public IReadOnlyList<string> Options;

            /// <summary>지금 넣어 둔 것. 아직 비었으면 -1.</summary>
            public int Picked = -1;

            /// <summary>검사한 결과. 아직 검사하지 않았으면 null.</summary>
            public bool? Correct;
        }

        [Header("머리말")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _footerText;

        [Header("왼쪽: 보고서")]
        [SerializeField] private TMP_Text _paperTitleText;
        [SerializeField] private RectTransform _rowRoot;
        [SerializeField] private Button _rowTemplate;

        [Header("오른쪽: 넣어 볼 말")]
        [SerializeField] private TMP_Text _choiceTitleText;
        [SerializeField] private RectTransform _choiceRoot;
        [SerializeField] private Button _choiceTemplate;

        [Header("아래")]
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private TMP_Text _resultText;
        [SerializeField] private GameObject _submitButton;

        private const string LabelName = "Text_RowLabel";
        private const string SlotName = "Text_RowSlot";
        private const string EmptySlot = "____________________";

        private const string PaperTitleTextId = "ui.report.paper_title";
        private const string ChoiceTitleTextId = "ui.report.choice_title";

        private IReadOnlyList<Blank> _blanks;
        private Action _onSubmit;
        private Func<string> _resultProvider;
        private string _titleId;
        private string _footerId;
        private int _selected;
        private Coroutine _resultFade;

        private readonly List<Button> _rows = new List<Button>();
        private readonly List<Button> _choices = new List<Button>();

        /// <summary>채울 칸들을 건다. 고른 것과 검사 결과는 Blank 가 들고 있다.</summary>
        public void Bind(string titleTextId, string footerTextId, IReadOnlyList<Blank> blanks, Action onSubmit)
        {
            _titleId = titleTextId;
            _footerId = footerTextId;
            _blanks = blanks;
            _onSubmit = onSubmit;
            _selected = 0;
            _resultProvider = null;
            Refresh();
        }

        /// <summary>아래 한 줄을 알린다. 잠깐 떴다가 옅어진다.</summary>
        public void ShowResult(Func<string> provider)
        {
            _resultProvider = provider;
            Refresh();

            if (_resultFade != null) { StopCoroutine(_resultFade); _resultFade = null; }
            if (provider == null || _resultRoot == null || !isActiveAndEnabled) return;

            UiFade.Show(_resultRoot);
            _resultFade = StartCoroutine(UiFade.Play(_resultRoot, () =>
            {
                _resultFade = null;
                _resultProvider = null;
                if (_resultRoot != null) _resultRoot.SetActive(false);
            }, 2f));
        }

        /// <summary>다 맞아서 더 고칠 것이 없을 때. 제출 단추를 거둔다.</summary>
        public void SetSubmitVisible(bool on)
        {
            if (_submitButton != null) _submitButton.SetActive(on);
        }

        protected override void OnOpen()
        {
            EventBus.Subscribe<LanguageChangedEvent>(OnLanguageChanged);
            Refresh();
        }

        protected override void OnClose()
        {
            EventBus.Unsubscribe<LanguageChangedEvent>(OnLanguageChanged);
        }

        private void OnLanguageChanged(LanguageChangedEvent evt)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<LocalizationService>(out var loc)) return;

            if (_titleText != null) _titleText.text = string.IsNullOrEmpty(_titleId) ? string.Empty : loc.Get(_titleId);
            if (_footerText != null) _footerText.text = string.IsNullOrEmpty(_footerId) ? string.Empty : loc.Get(_footerId);
            if (_paperTitleText != null) _paperTitleText.text = loc.Get(PaperTitleTextId);
            if (_choiceTitleText != null) _choiceTitleText.text = loc.Get(ChoiceTitleTextId);

            string result = _resultProvider != null ? _resultProvider() : string.Empty;
            if (_resultText != null) _resultText.text = result;
            if (_resultRoot != null) _resultRoot.SetActive(!string.IsNullOrEmpty(result));

            RebuildRows();
            RebuildChoices();
        }

        /// <summary>보고서의 칸들. 한 줄이 이름과 빈칸으로 이루어진다.</summary>
        private void RebuildRows()
        {
            if (_rowRoot == null || _rowTemplate == null) return;

            Clear(_rows);
            if (_blanks == null) return;

            for (int i = 0; i < _blanks.Count; i++)
            {
                var blank = _blanks[i];
                if (blank == null) continue;

                var item = Instantiate(_rowTemplate, _rowRoot);
                item.gameObject.name = "Row_" + i;
                item.gameObject.SetActive(true);

                int captured = i;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => SelectRow(captured));

                _rows.Add(item);
                PaintRow(item, blank, i == _selected);
            }
        }

        private void SelectRow(int index)
        {
            _selected = index;
            Refresh();
        }

        /// <summary>
        /// 한 줄을 칠한다.
        ///
        /// 아직 검사하지 않았으면 고르고 있는 줄만 밝다. 검사하고 나면 맞은 칸은 풀빛,
        /// 틀린 칸은 붉은빛으로 둔다. 어디를 고쳐야 하는지가 한눈에 들어와야 한다.
        /// </summary>
        private void PaintRow(Button item, Blank blank, bool selected)
        {
            if (item == null || blank == null) return;

            var image = item.targetGraphic as Image;
            if (image != null)
            {
                image.color = blank.Correct == true ? RightColor
                    : blank.Correct == false ? WrongColor
                    : selected ? SelectedColor : IdleColor;
            }

            var labelTr = item.transform.Find(LabelName);
            var label = labelTr != null ? labelTr.GetComponent<TMP_Text>() : null;
            if (label != null) label.text = blank.Label;

            var slotTr = item.transform.Find(SlotName);
            var slot = slotTr != null ? slotTr.GetComponent<TMP_Text>() : null;
            if (slot == null) return;

            bool filled = blank.Picked >= 0 && blank.Options != null && blank.Picked < blank.Options.Count;
            slot.text = filled ? blank.Options[blank.Picked] : EmptySlot;
            slot.color = filled ? TextColor : DimTextColor;
        }

        /// <summary>고르고 있는 줄에 넣어 볼 수 있는 말들.</summary>
        private void RebuildChoices()
        {
            if (_choiceRoot == null || _choiceTemplate == null) return;

            Clear(_choices);

            var blank = _blanks != null && _selected >= 0 && _selected < _blanks.Count ? _blanks[_selected] : null;
            var options = blank != null ? blank.Options : null;
            if (options == null) return;

            for (int i = 0; i < options.Count; i++)
            {
                var item = Instantiate(_choiceTemplate, _choiceRoot);
                item.gameObject.name = "Choice_" + i;
                item.gameObject.SetActive(true);

                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = options[i];
                    label.color = i == blank.Picked ? TextColor : DimTextColor;
                }

                var image = item.targetGraphic as Image;
                if (image != null) image.color = i == blank.Picked ? SelectedColor : IdleColor;

                int captured = i;
                item.onClick.RemoveAllListeners();
                item.onClick.AddListener(() => Fill(captured));

                _choices.Add(item);
            }
        }

        /// <summary>고른 말을 지금 줄에 넣는다. 넣으면 그 줄의 검사 표시는 지운다.</summary>
        private void Fill(int optionIndex)
        {
            var blank = _blanks != null && _selected >= 0 && _selected < _blanks.Count ? _blanks[_selected] : null;
            if (blank == null) return;

            blank.Picked = optionIndex;
            blank.Correct = null;

            // 채웠으면 아직 빈 다음 줄로 옮겨 간다. 한 줄 채울 때마다 다시 누르지 않아도 된다.
            for (int i = 1; i <= _blanks.Count; i++)
            {
                int next = (_selected + i) % _blanks.Count;
                if (_blanks[next] != null && _blanks[next].Picked < 0) { _selected = next; break; }
            }

            Refresh();
        }

        /// <summary>아래 제출 단추. 다 채우지 않았어도 누를 수 있다. 판정은 밖에서 한다.</summary>
        public void OnSubmitClicked()
        {
            if (_onSubmit != null) _onSubmit.Invoke();
        }

        private void Clear(List<Button> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;

                var old = list[i].gameObject;
                old.transform.SetParent(null, false);
                old.SetActive(false);
                Destroy(old);
            }
            list.Clear();
        }

        private static readonly Color IdleColor = new Color(0.13f, 0.14f, 0.19f, 1f);
        private static readonly Color SelectedColor = new Color(0.22f, 0.24f, 0.32f, 1f);
        private static readonly Color RightColor = new Color(0.18f, 0.34f, 0.24f, 1f);
        private static readonly Color WrongColor = new Color(0.38f, 0.18f, 0.20f, 1f);
        private static readonly Color TextColor = new Color(0.93f, 0.93f, 0.96f);
        private static readonly Color DimTextColor = new Color(0.62f, 0.64f, 0.70f);
    }
}
