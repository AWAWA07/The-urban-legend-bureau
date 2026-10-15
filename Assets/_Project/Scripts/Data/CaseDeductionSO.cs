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
        }

        [Serializable]
        public class Analysis
        {
            public string analysisId;

            [Tooltip("맞대어 볼 단서 둘. 어느 쪽을 먼저 골라도 같다.")]
            public string clueA;
            public string clueB;

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
        public enum ReportAnswerType { Truth, Rule, Countermeasure, Target }

        /// <summary>
        /// 보고서의 작성란 하나. 문서 안에서 "01 진위 판정 / 설명 문장 / 작성란" 순서로 한 섹션을 이룬다.
        /// 작성란 뒤에 말을 잇지 않는다. 답이 명사든 문장이든 문법이 깨지지 않게 하기 위해서다.
        /// 판정은 정리한 글(공백, 문장부호, 괄호, 따옴표, 대소문자를 뺀다)이 허용 답안 하나와 통째로 같을 때만 맞다.
        /// 허용 답안이 "~" 로 시작하면 짧은 틀이다. 낱말 사이를 띄어 쓰고 [가|나] 는 그중 하나, 뒤에 ? 가 붙으면 없어도 된다.
        /// 틀도 글 전체가 처음부터 끝까지 맞아야 한다. 핵심어가 들어 있다는 것만으로는 맞히지 않는다(반대 뜻이 섞여 들어오지 않게).
        /// </summary>
        [Serializable]
        public class ReportBlank
        {
            public string blankId;
            public ReportAnswerType answerType;

            [Tooltip("섹션 제목. 예: 진위 판정.")]
            public string titleTextId;

            [Tooltip("작성란 위의 설명 문장. 작성란은 그 아래에 따로 놓이므로 이 문장이 답과 이어 붙지 않는다.")]
            public string prefixTextId;

            [Tooltip("여러 줄로 적는 작성란인가. 규칙과 대처 방법처럼 문장으로 답하는 칸.")]
            public bool multiLine;

            [Tooltip("빈칸이 비었을 때 흐리게 보이는 안내. 답을 알려 주지 않는다.")]
            public string hintTextId;

            [Tooltip("대표 정답. 다 맞힌 뒤 보고서와 메모장에 이 말을 쓰지 않고 플레이어가 적은 글을 쓴다. 검수용이다.")]
            public string answerTextId;

            [Tooltip("받아 주는 답(한국어, 영어). 정리한 뒤 통째로 견준다. ~ 로 시작하면 틀이다(위 설명).")]
            public List<string> acceptedAnswers = new List<string>();
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
