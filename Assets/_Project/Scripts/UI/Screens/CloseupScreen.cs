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
    /// 현장의 물건 하나를 화면 가득 크게 들여다보는 화면. 열차 안 CCTV, 창문 같은 것.
    ///
    /// 그 물건에서 눌러 볼 수 있는 곳(살필 곳)이 몇 군데 있다. 누르면 아래 띠에 본 것을 한 줄 적는다.
    /// 무엇을 보았는지는 화면이 기억한다. 몇 군데를 살폈는지 SpotChecked 로 알린다. 그것으로 연출을 거는 것은 부르는 쪽 일이다.
    /// 살펴보는 것뿐이라 시간도 확산도 쓰지 않는다. 돌아가기를 누르면 현장으로 돌아간다.
    ///
    /// 말을 주고받는 연출도 띄울 수 있다(Talk). 그동안 띠 위 이름표에 말하는 사람이 적히고, 화면 어디를 눌러도 다음 말로 넘어간다.
    /// </summary>
    public class CloseupScreen : UIScreen
    {
        [Serializable]
        public class Spot
        {
            public Button button;
            [Tooltip("눌렀을 때 아래 띠에 적는 글.")]
            public string textId;
        }

        [SerializeField] private Spot[] _spots = new Spot[0];
        [SerializeField] private Button _closeButton;

        [Tooltip("아래 띠. 처음에는 안내 한 줄이 떠 있다.")]
        [SerializeField] private TMP_Text _lineText;

        [Tooltip("띠 위 이름표. 평소에는 '조사', 말을 주고받는 동안에는 말하는 사람.")]
        [SerializeField] private TMP_Text _tabText;

        [Tooltip("말을 주고받는 동안 화면 전체를 덮어 누르면 넘어가게 하는 판.")]
        [SerializeField] private Button _advanceButton;

        [SerializeField] private string _hintTextId = "field.subway.cctv_close.hint";
        [SerializeField] private string _tabTextId = "ui.field.closeup_tab";

        /// <summary>돌아가기를 눌렀다. 화면 스택을 아는 쪽이 닫는다.</summary>
        public Action Closed;

        /// <summary>새 곳을 살폈다. 지금까지 살핀 곳의 수를 넘긴다(같은 곳을 다시 누르면 세지 않는다).</summary>
        public Action<int> SpotChecked;

        private readonly HashSet<int> _checked = new HashSet<int>();
        private string[] _talkSpeakers;
        private string[] _talkLines;
        private int _talkIndex;
        private Action _talkDone;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < _spots.Length; i++)
            {
                int index = i;
                if (_spots[i] != null && _spots[i].button != null) _spots[i].button.onClick.AddListener(() => OnSpot(index));
            }
            if (_closeButton != null) _closeButton.onClick.AddListener(() => Closed?.Invoke());
            if (_advanceButton != null)
            {
                _advanceButton.onClick.AddListener(NextTalk);
                _advanceButton.gameObject.SetActive(false);
            }
        }

        protected override void OnOpen()
        {
            SetTab(_tabTextId);
            ShowLine(_hintTextId);
            SetInteractable(true);
        }

        /// <summary>살핀 기록을 지운다. 사건이 새로 시작될 때 부른다.</summary>
        public void ResetChecked()
        {
            _checked.Clear();
        }

        /// <summary>살필 곳과 돌아가기를 누를 수 있는지 정한다. 연출 동안 막는다.</summary>
        public void SetInteractable(bool on)
        {
            foreach (var s in _spots) if (s != null && s.button != null) s.button.interactable = on;
            if (_closeButton != null) _closeButton.interactable = on;
        }

        /// <summary>
        /// 말을 차례로 띄운다. speakers 는 말하는 사람 이름 글 ID, lines 는 대사 글 ID 다.
        /// 화면 어디를 눌러도 다음 말로 넘어가고, 다 끝나면 이름표를 '조사' 로 되돌리고 done 을 부른다.
        /// </summary>
        public void Talk(string[] speakers, string[] lines, Action done)
        {
            _talkSpeakers = speakers;
            _talkLines = lines;
            _talkIndex = -1;
            _talkDone = done;
            SetInteractable(false);
            if (_advanceButton != null)
            {
                _advanceButton.gameObject.SetActive(true);
                _advanceButton.transform.SetAsLastSibling();
            }
            NextTalk();
        }

        private void NextTalk()
        {
            if (_talkLines == null) return;
            _talkIndex++;
            if (_talkIndex >= _talkLines.Length)
            {
                _talkLines = null;
                if (_advanceButton != null) _advanceButton.gameObject.SetActive(false);
                SetTab(_tabTextId);
                SetInteractable(true);
                var done = _talkDone;
                _talkDone = null;
                done?.Invoke();
                return;
            }
            SetTab(_talkSpeakers[_talkIndex]);
            ShowLine(_talkLines[_talkIndex]);
        }

        private void OnSpot(int index)
        {
            ShowLine(_spots[index].textId);
            if (_checked.Add(index)) SpotChecked?.Invoke(_checked.Count);
        }

        private void ShowLine(string id)
        {
            if (_lineText != null) _lineText.text = Get(id);
        }

        private void SetTab(string id)
        {
            if (_tabText != null) _tabText.text = Get(id);
        }

        private static string Get(string id)
        {
            return ServiceRegistry.TryGet<LocalizationService>(out var loc) ? loc.Get(id) : id;
        }
    }
}
