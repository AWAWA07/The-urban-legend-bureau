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

        public static bool AllAnalysesFound(SaveData save, CaseDeductionSO data) =>
            data != null && data.Analyses.Count > 0 && AnalysisCount(save, data) >= data.Analyses.Count;

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

        // ------------------------------------------------------------- 처음부터

        /// <summary>이 사건의 분석, 추론, 보고서 기록을 지운다. 얻은 단서는 사건 쪽이 따로 지운다.</summary>
        public static void ResetCase(SaveData save, string caseId)
        {
            if (save == null || string.IsNullOrEmpty(caseId)) return;
            string prefix = caseId + "|";
            save.analysisRecords.RemoveAll(k => k != null && k.StartsWith(prefix));
            save.solvedDeductions.RemoveAll(k => k != null && k.StartsWith(prefix));
            save.confirmedReports.Remove(caseId);
        }
    }
}
