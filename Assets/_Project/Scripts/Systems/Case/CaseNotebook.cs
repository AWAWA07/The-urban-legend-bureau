using System;
using System.Collections.Generic;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Save;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 수사 노트의 진행을 SaveData 위에서 바로 읽고 쓰는 도우미. CaseFlow 와 같은 방식이다.
    ///
    ///   얻은 단서   - acquiredClueIds (예전 그대로)
    ///   분석 기록   - analysisRecords  "사건ID|분석ID"
    ///   맞힌 추론   - solvedDeductions "사건ID|물음ID"
    ///   확정 보고서 - confirmedReports "사건ID"
    ///
    /// 사건 ID 를 앞에 붙여 두므로 다른 사건의 기록과 섞이지 않는다.
    /// 시간, 확산도, 믿음도는 여기서 건드리지 않는다. 분석과 추론은 아무 값도 바꾸지 않는다.
    /// </summary>
    public static class CaseNotebook
    {
        private static string Key(string caseId, string id) => caseId + "|" + id;

        // ------------------------------------------------------------- 단서

        /// <summary>이 사건 현장 단서 가운데 얻은 것. 자료에 적힌 순서대로.</summary>
        public static List<CaseDeductionSO.FieldClue> AcquiredClues(SaveData save, CaseDeductionSO data)
        {
            var list = new List<CaseDeductionSO.FieldClue>();
            if (save == null || data == null) return list;

            foreach (var clue in data.FieldClues)
            {
                if (clue != null && save.acquiredClueIds.Contains(clue.clueId)) list.Add(clue);
            }
            return list;
        }

        public static bool HasClue(SaveData save, string clueId) =>
            save != null && !string.IsNullOrEmpty(clueId) && save.acquiredClueIds.Contains(clueId);

        // ------------------------------------------------------------- 분석

        public static bool HasAnalysis(SaveData save, CaseDeductionSO data, string analysisId) =>
            save != null && data != null && save.analysisRecords.Contains(Key(data.CaseId, analysisId));

        public static int AnalysisCount(SaveData save, CaseDeductionSO data)
        {
            int n = 0;
            if (data == null) return n;
            foreach (var a in data.Analyses)
            {
                if (a != null && HasAnalysis(save, data, a.analysisId)) n++;
            }
            return n;
        }

        /// <summary>최종 추론을 여는 데 필요한 분석(추가 분석이 아닌 것)을 모두 찾았는가.</summary>
        public static bool AllAnalysesFound(SaveData save, CaseDeductionSO data)
        {
            if (data == null) return false;
            int required = 0;
            foreach (var a in data.Analyses)
            {
                if (a == null || a.optional) continue;
                required++;
                if (!HasAnalysis(save, data, a.analysisId)) return false;
            }
            return required > 0;
        }

        /// <summary>
        /// 두 단서를 맞대어 본다. 둘 다 얻은 단서여야 한다.
        /// 맞는 짝이면 분석을 돌려주고, 처음 찾은 것이면 기록한다(isNew). 같은 기록은 두 번 남기지 않는다.
        /// </summary>
        public static CaseDeductionSO.Analysis Compare(SaveData save, CaseDeductionSO data, string a, string b, out bool isNew)
        {
            isNew = false;
            if (save == null || data == null) return null;
            if (!HasClue(save, a) || !HasClue(save, b)) return null;

            var found = data.FindAnalysis(a, b);
            if (found == null) return null;

            string key = Key(data.CaseId, found.analysisId);
            if (!save.analysisRecords.Contains(key))
            {
                save.analysisRecords.Add(key);
                isNew = true;
            }
            return found;
        }

        // ------------------------------------------------------------- 추론

        public static bool IsSolved(SaveData save, CaseDeductionSO data, string questionId) =>
            save != null && data != null && save.solvedDeductions.Contains(Key(data.CaseId, questionId));

        public static int SolvedCount(SaveData save, CaseDeductionSO data)
        {
            int n = 0;
            if (data == null) return n;
            foreach (var q in data.Questions)
            {
                if (q != null && IsSolved(save, data, q.questionId)) n++;
            }
            return n;
        }

        public static bool AllSolved(SaveData save, CaseDeductionSO data) =>
            data != null && data.Questions.Count > 0 && SolvedCount(save, data) >= data.Questions.Count;

        /// <summary>아직 풀지 않은 첫 물음의 번호. 다 풀었으면 물음 수.</summary>
        public static int CurrentQuestionIndex(SaveData save, CaseDeductionSO data)
        {
            if (data == null) return 0;
            for (int i = 0; i < data.Questions.Count; i++)
            {
                var q = data.Questions[i];
                if (q != null && !IsSolved(save, data, q.questionId)) return i;
            }
            return data.Questions.Count;
        }

        public static void MarkSolved(SaveData save, CaseDeductionSO data, string questionId)
        {
            if (save == null || data == null) return;
            string key = Key(data.CaseId, questionId);
            if (!save.solvedDeductions.Contains(key)) save.solvedDeductions.Add(key);
        }

        // ------------------------------------------------------------- 보고서

        public static bool IsReportConfirmed(SaveData save, string caseId) =>
            save != null && !string.IsNullOrEmpty(caseId) && save.confirmedReports.Contains(caseId);

        public static void ConfirmReport(SaveData save, string caseId)
        {
            if (save == null || string.IsNullOrEmpty(caseId)) return;
            if (!save.confirmedReports.Contains(caseId)) save.confirmedReports.Add(caseId);
        }

        // ------------------------------------------------------------- 보고서 빈칸

        /// <summary>빈칸에 적어 둔 글. 없으면 빈 문자열.</summary>
        public static string GetInput(SaveData save, string caseId, string blankId)
        {
            if (save == null || save.reportInputs == null) return string.Empty;
            string head = caseId + "|" + blankId + "|";
            foreach (var entry in save.reportInputs)
            {
                if (entry != null && entry.StartsWith(head)) return entry.Substring(head.Length);
            }
            return string.Empty;
        }

        /// <summary>빈칸에 적은 글을 남긴다. 비우면 지운다.</summary>
        public static void SetInput(SaveData save, string caseId, string blankId, string text)
        {
            if (save == null) return;
            save.reportInputs ??= new List<string>();
            string head = caseId + "|" + blankId + "|";
            save.reportInputs.RemoveAll(e => e != null && e.StartsWith(head));
            if (!string.IsNullOrEmpty(text)) save.reportInputs.Add(head + text);
        }

        /// <summary>
        /// 견주기 전에 글을 정리한다. 앞뒤와 사이의 공백, 문장부호, 괄호, 따옴표를 모두 빼고 영문은 소문자로 맞춘다.
        /// "[정리] 막차 괴담 규칙 총정리" 와 "정리 막차괴담 규칙총정리." 가 같은 글이 된다.
        /// 띄어쓰기가 틀려도 맞게 본다. 한글 띄어쓰기는 사람마다 다르게 쓰기 때문이다.
        /// </summary>
        public static string Normalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 빈칸에 적은 글이 맞는가.
        ///
        /// 정리한 글이 허용 답안 하나와 처음부터 끝까지 같을 때만 맞다. 일부가 들어 있다는 것으로는 맞히지 않는다.
        /// "고개를 돌려 확인해도 아무 일 없다" 처럼 핵심어는 다 들어 있지만 뜻이 반대인 글이 맞게 나오지 않게 하려는 것이다.
        ///
        /// 허용 답안이 ~ 로 시작하면 틀이다. 낱말을 띄어 쓰고, [가|나] 는 그중 하나, 뒤에 ? 가 붙은 조각은 없어도 된다.
        ///   ~[반쯤|일부] [진짜|사실] [다|이다]?   →  반쯤진짜, 일부사실이다 ...
        /// 틀도 글 전체와 맞춰 본다. 조각마다 들어갈 수 있는 말은 자료에 적어 둔 것뿐이다.
        /// 판정하기 어려운 글은 무리해서 맞히지 않는다. 틀렸다고 알리면 플레이어가 다시 적는다.
        /// </summary>
        public static bool CheckAnswer(CaseDeductionSO.ReportBlank blank, string input)
        {
            if (blank == null || blank.acceptedAnswers == null) return false;
            string text = Normalize(input);
            if (text.Length == 0) return false;

            foreach (var answer in blank.acceptedAnswers)
            {
                if (string.IsNullOrEmpty(answer)) continue;
                if (answer[0] == '~')
                {
                    var regex = Pattern(answer);
                    if (regex != null && regex.IsMatch(text)) return true;
                }
                else if (Normalize(answer) == text) return true;
            }
            return false;
        }

        /// <summary>틀을 조각으로 나눈다. 공백에서 자르되 [ ] 안의 공백은 자르지 않는다("[do not|dont]" 는 한 조각).</summary>
        private static List<string> Tokens(string pattern)
        {
            var tokens = new List<string>();
            var sb = new System.Text.StringBuilder();
            int depth = 0;
            foreach (char c in pattern)
            {
                if (c == '[') depth++;
                else if (c == ']') depth = Math.Max(0, depth - 1);
                if (c == ' ' && depth == 0)
                {
                    if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                    continue;
                }
                sb.Append(c);
            }
            if (sb.Length > 0) tokens.Add(sb.ToString());
            return tokens;
        }

        private static readonly Dictionary<string, System.Text.RegularExpressions.Regex> _patterns =
            new Dictionary<string, System.Text.RegularExpressions.Regex>();

        /// <summary>틀 하나를 정리한 글과 견줄 정규식으로 바꾼다. 한 번 만든 것은 다시 쓴다.</summary>
        private static System.Text.RegularExpressions.Regex Pattern(string pattern)
        {
            if (_patterns.TryGetValue(pattern, out var cached)) return cached;

            var sb = new System.Text.StringBuilder("^");
            foreach (var raw in Tokens(pattern.Substring(1)))
            {
                string token = raw;
                bool optional = token.EndsWith("?");
                if (optional) token = token.Substring(0, token.Length - 1);
                if (token.StartsWith("[") && token.EndsWith("]")) token = token.Substring(1, token.Length - 2);

                var alts = new List<string>();
                foreach (var alt in token.Split('|'))
                {
                    string w = Normalize(alt);
                    if (w.Length > 0) alts.Add(System.Text.RegularExpressions.Regex.Escape(w));
                }
                if (alts.Count == 0) continue;
                sb.Append("(?:").Append(string.Join("|", alts)).Append(')');
                if (optional) sb.Append('?');
            }
            sb.Append('$');

            System.Text.RegularExpressions.Regex regex = null;
            try { regex = new System.Text.RegularExpressions.Regex(sb.ToString()); }
            catch (ArgumentException e) { UnityEngine.Debug.LogWarning("[CaseNotebook] 보고서 답안 틀을 읽지 못했다: " + pattern + " | " + e.Message); }
            _patterns[pattern] = regex;
            return regex;
        }

        // ------------------------------------------------------------- 처음부터

        /// <summary>이 사건의 분석, 추론, 보고서 기록을 지운다. 얻은 단서는 사건 쪽이 따로 지운다.</summary>
        public static void ResetCase(SaveData save, string caseId)
        {
            if (save == null || string.IsNullOrEmpty(caseId)) return;
            string prefix = caseId + "|";
            save.analysisRecords.RemoveAll(k => k != null && k.StartsWith(prefix));
            save.solvedDeductions.RemoveAll(k => k != null && k.StartsWith(prefix));
            save.confirmedReports.Remove(caseId);
            save.reportInputs?.RemoveAll(k => k != null && k.StartsWith(prefix));
        }
    }
}
