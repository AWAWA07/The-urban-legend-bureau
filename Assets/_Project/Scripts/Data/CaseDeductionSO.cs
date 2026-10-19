using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Data
{
    /// <summary>
    /// 한 사건의 추리 자료. 수사 노트, 단서 분석, 최종 추론, 조사 보고서가 이것을 읽는다.
    ///
    ///   현장 단서 - 이 사건 현장에서 얻을 수 있는 단서와 얻은 자리. 수사 노트의 "수집한 단서" 칸이 된다.
    ///   분석      - 단서 두 개를 맞대어 새로 알아내는 것(분석 기록). 순서는 상관없다.
    ///   추론      - 진위, 발동 조건, 대처법처럼 차례로 답하는 물음. 보기 셋 중 하나가 답이다.
    ///
    /// 글은 전부 문구 ID 로만 든다. 사건마다 한 장씩 두므로 다른 사건의 보기나 기록이 섞이지 않는다.
    /// 사건(CaseSO)이 이것을 들고 있지 않으면 예전 흐름(규칙 추론 화면, 빈칸 보고서)을 그대로 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "deduction_", menuName = "UrbanLegendBureau/Case Deduction", order = 32)]
    public class CaseDeductionSO : ScriptableObject
    {
        [Serializable]
        public class FieldClue
        {
            [Tooltip("단서 ID. ClueSO 의 clueId 와 같다.")]
            public string clueId;

            [Tooltip("얻은 자리 이름의 문구 ID. 예: 빈자리, 창문.")]
            public string placeTextId;

            [Tooltip("본편: 이 인터넷 글(WebPageSO ID)을 괴담넷에서 읽으면 이 단서를 얻는다. 비우면 현장에서만 얻는다.")]
            public string fromPageId;
        }

        [Serializable]
        public class Analysis
        {
            public string analysisId;

            [Tooltip("맞대어 볼 단서 둘. 어느 쪽을 먼저 골라도 같다.")]
            public string clueA;
            public string clueB;

            [Tooltip("켜면 최종 추론을 여는 데 필요하지 않은 추가 분석이다. 정정글·댓글의 새 근거로 쓴다.")]
            public bool optional;
            public string titleTextId;
            public string bodyTextId;
        }

        [Serializable]
        public class Question
        {
            public string questionId;

            [Tooltip("이 물음이 무엇을 정하는지. 예: 괴담의 진위. 추론 기록과 보고서의 칸 이름이 된다.")]
            public string labelTextId;

            public string questionTextId;

            [Tooltip("보기 셋의 문구 ID.")]
            public List<string> optionTextIds = new List<string>();

            [Tooltip("맞는 보기의 번호(0부터).")]
            public int correctIndex;

            [Tooltip("맞혔을 때 보여 주는 짧은 해설.")]
            public string explanationTextId;

            [Tooltip("보고서에 적히는 결론 한 줄.")]
            public string reportTextId;

            [Tooltip("이 물음의 근거. 근거 확인 칸에 함께 세운다. 정답을 알려 주는 표시는 하지 않는다.")]
            public List<string> evidenceClueIds = new List<string>();
            public List<string> evidenceAnalysisIds = new List<string>();
        }

        /// <summary>보고서 빈칸이 무엇을 묻는가.</summary>
        public enum ReportAnswerType { None, Truth, Rule, Countermeasure, Target }

        /// <summary>
        /// 보고서의 한 섹션(문단). 이미 적혀 있는 사건 보고서 문단이고, 그 안에 핵심 낱말 하나를 비워 둘 수 있다.
        ///
        /// 문단 글에 {_} 를 적은 자리가 빈칸이다. 빈칸은 문장 사이의 밑줄로 보이고 플레이어가 짧은 낱말을 적는다.
        /// 빈칸 앞뒤 말은 어떤 허용 답이 들어가도 문법이 맞게 짓는다(조사가 답에 따라 바뀌지 않게).
        /// blankId 가 비어 있으면 빈칸 없는 설명 문단이다.
        ///
        /// 판정은 정리한 글(공백, 문장부호, 괄호, 따옴표, 대소문자를 뺀다)이 허용 답 하나와 통째로 같을 때만 맞다.
        /// 허용 답이 "~" 로 시작하면 짧은 틀이다. 낱말 사이를 띄어 쓰고 [가|나] 는 그중 하나, 뒤에 ? 가 붙으면 없어도 된다.
        /// 정답 낱말이 들어 있다는 것만으로는 맞히지 않는다("돌려" 가 답이면 "돌리지" 는 틀리다).
        /// </summary>
        [Serializable]
        public class ReportBlank
        {
            [Tooltip("빈칸 ID. 비우면 빈칸 없는 설명 문단이다.")]
            public string blankId;
            public ReportAnswerType answerType;

            [Tooltip("섹션 제목. 예: 사건 개요.")]
            public string titleTextId;

            [Tooltip("문단 글. {_} 자리가 빈칸이다.")]
            public string bodyTextId;

            [Tooltip("빈칸 밑줄의 폭(1920 기준). 가장 긴 허용 답이 들어갈 만큼.")]
            public float blankWidth = 180f;

            [Tooltip("대표 정답. 검수와 메모장 확인용이다. 메모장에는 플레이어가 적은 글이 들어간다.")]
            public string answerTextId;

            [Tooltip("검열 대상 빈칸이 가리키는 게시물 ID. 빈칸에는 제목의 핵심 낱말만 적지만 실제 대상은 이 글이다.")]
            public string targetPageId;

            [Tooltip("받아 주는 답(한국어, 영어). 정리한 뒤 통째로 견준다. ~ 로 시작하면 틀이다(위 설명).")]
            public List<string> acceptedAnswers = new List<string>();

            [Tooltip("보기로 함께 내놓을 오답(문장 ID). 대표 정답(answerTextId)과 섞어 보기 3개를 만든다. 비우면 예전처럼 직접 적는다.")]
            public List<string> choiceTextIds = new List<string>();

            public bool HasBlank => !string.IsNullOrEmpty(blankId);

            /// <summary>보기 중에서 고르는 빈칸인가. 대표 정답과 오답이 있어야 한다.</summary>
            public bool HasChoices => HasBlank && !string.IsNullOrEmpty(answerTextId) && choiceTextIds != null && choiceTextIds.Count > 0;

            /// <summary>
            /// 보기 문장 ID 를 늘어놓는다. 정답이 늘 같은 자리에 있지 않게 빈칸 ID 로 정한 순서로 섞는다.
            /// 열 때마다 바뀌지 않으므로 다시 열어도 같은 자리에 같은 보기가 있다.
            /// </summary>
            public List<string> ChoiceOrder()
            {
                var list = new List<string> { answerTextId };
                foreach (var id in choiceTextIds) if (!string.IsNullOrEmpty(id)) list.Add(id);
                uint seed = 2166136261;   // FNV-1a
                foreach (char ch in blankId + answerTextId) seed = unchecked((seed ^ ch) * 16777619);
                int shift = (int)((seed >> 7) % (uint)list.Count);
                var ordered = new List<string>(list.Count);
                for (int i = 0; i < list.Count; i++) ordered.Add(list[(i + shift) % list.Count]);
                return ordered;
            }
        }

        [SerializeField] private string _caseId;

        [SerializeField] private List<FieldClue> _fieldClues = new List<FieldClue>();
        [SerializeField] private List<Analysis> _analyses = new List<Analysis>();
        [SerializeField] private List<Question> _questions = new List<Question>();

        [Tooltip("조사 보고서 문장 속 빈칸들. 플레이어가 직접 적어 넣는다. 비워 두면 결론을 늘어놓는 문서만 보인다.")]
        [SerializeField] private List<ReportBlank> _reportBlanks = new List<ReportBlank>();

        [Tooltip("보고서 머리의 문서 번호. 예: ULB-2026-001.")]
        [SerializeField] private string _documentNumber;

        [Tooltip("보고서의 종합 판단 한 문단. 결론을 되풀이하지 않고 무엇을 근거로 그렇게 봤는지 적는다.")]
        [SerializeField] private string _summaryTextId;

        public string CaseId => _caseId;
        public IReadOnlyList<FieldClue> FieldClues => _fieldClues;
        public IReadOnlyList<Analysis> Analyses => _analyses;
        public IReadOnlyList<Question> Questions => _questions;
        public IReadOnlyList<ReportBlank> ReportBlanks => _reportBlanks;

        /// <summary>보고서 섹션 가운데 빈칸이 있는 것의 수.</summary>
        public int BlankCount
        {
            get
            {
                int n = 0;
                foreach (var b in _reportBlanks) if (b != null && b.HasBlank) n++;
                return n;
            }
        }
        public string DocumentNumber => _documentNumber;
        public string SummaryTextId => _summaryTextId;

        /// <summary>두 단서로 찾을 수 있는 분석. 순서는 가리지 않는다. 없으면 null.</summary>
        public Analysis FindAnalysis(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b) return null;

            foreach (var analysis in _analyses)
            {
                if (analysis == null) continue;
                if ((analysis.clueA == a && analysis.clueB == b) || (analysis.clueA == b && analysis.clueB == a)) return analysis;
            }
            return null;
        }

        public Analysis GetAnalysis(string analysisId)
        {
            foreach (var analysis in _analyses)
            {
                if (analysis != null && analysis.analysisId == analysisId) return analysis;
            }
            return null;
        }

        public FieldClue GetFieldClue(string clueId)
        {
            foreach (var clue in _fieldClues)
            {
                if (clue != null && clue.clueId == clueId) return clue;
            }
            return null;
        }

        /// <summary>이 사건 현장에서 얻는 단서인가.</summary>
        public bool IsFieldClue(string clueId) => GetFieldClue(clueId) != null;

        private void OnValidate()
        {
            _caseId = _caseId?.Trim();
            foreach (var q in _questions)
            {
                if (q == null || q.optionTextIds == null) continue;
                q.correctIndex = Mathf.Clamp(q.correctIndex, 0, Mathf.Max(0, q.optionTextIds.Count - 1));
            }
        }
    }
}
