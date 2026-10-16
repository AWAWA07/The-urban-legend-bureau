using UnityEngine;

namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// 구버전 저장 파일을 현재 스키마로 올린다.
    /// 저장 구조를 바꿀 때마다 CurrentVersion을 올리고 여기에 변환 단계를 추가한다.
    /// 기존 플레이어의 저장을 깨뜨리지 않기 위한 장치다.
    /// </summary>
    public static class SaveMigration
    {
        /// <summary>현재 스키마 버전.</summary>
        public const int CurrentVersion = 11;

        // 사건 종료를 뜻하던 단계 값. 중간 단계가 끼어들 때마다 뒤로 밀렸다.
        private const int V1CompletedStep = 5;   // v1: 규칙 추론 단계가 없었다
        private const int V2CompletedStep = 6;   // v2: 봉인 단계가 없었다
        private const int V3CompletedStep = 7;

        public enum Result
        {
            /// <summary>이미 최신. 변환하지 않았다.</summary>
            UpToDate,

            /// <summary>구버전을 최신으로 올렸다.</summary>
            Migrated,

            /// <summary>이 빌드보다 최신 저장이다. 읽을 수 없다.</summary>
            TooNew,

            /// <summary>손상되어 복구할 수 없다.</summary>
            Failed
        }

        public static Result Migrate(SaveData data)
        {
            if (data == null) return Result.Failed;

            if (data.saveVersion > CurrentVersion)
            {
                Debug.LogError(
                    $"[SaveMigration] 저장 버전 {data.saveVersion} 은 이 빌드(v{CurrentVersion})보다 최신이다. 불러올 수 없다.");
                return Result.TooNew;
            }

            if (data.saveVersion == CurrentVersion)
            {
                return Result.UpToDate;
            }

            int from = data.saveVersion;

            // 버전을 하나씩 올린다.
            if (data.saveVersion == 1)
            {
                // v2에서 CaseStep에 RuleDeduction(5)이 추가되면서 Completed가 5 -> 6으로 밀렸다.
                // v1 저장본의 '종료' 상태가 '규칙 추론 중'으로 잘못 읽히지 않게 옮겨준다.
                if (data.currentStepIndex == V1CompletedStep)
                {
                    data.currentStepIndex = V2CompletedStep;
                }
                data.saveVersion = 2;
            }

            if (data.saveVersion == 2)
            {
                // v3에서 CaseStep에 Exorcism(6)이 추가되면서 Completed가 6 -> 7로 밀렸다.
                if (data.currentStepIndex == V2CompletedStep)
                {
                    data.currentStepIndex = V3CompletedStep;
                }
                data.saveVersion = 3;
            }

            if (data.saveVersion == 3)
            {
                // v4에서 사건별 조사 경과(caseTimes)가 추가됐다.
                // 구버전 저장본에는 이 기록이 없다. 빈 목록으로 두면
                // 각 사건은 0회 / 0분에서 시작한다. 잃는 진행은 없다.
                data.caseTimes ??= new System.Collections.Generic.List<CaseTimeState>();
                data.saveVersion = 4;
            }

            if (data.saveVersion == 4)
            {
                // v5에서 메모장(memo)이 추가됐다. 구버전 저장본에는 적어 둔 것이 없다.
                // 빈 글로 두면 메모장을 처음 여는 것과 같다. 잃는 것은 없다.
                data.memos ??= new System.Collections.Generic.List<string>();
                data.saveVersion = 5;
            }

            if (data.saveVersion == 5)
            {
                // v6에서 이미 해 본 조사 방법(doneActions)이 추가됐다.
                // 구버전 저장본에는 그 기록이 없다. 빈 목록이면 아직 아무것도 안 해 본 것으로 읽힌다.
                // 방법이 주는 단서를 이미 들고 있으면 해 본 것으로 치므로 크게 어긋나지 않는다.
                data.doneActions ??= new System.Collections.Generic.List<string>();
                data.saveVersion = 6;
            }

            if (data.saveVersion == 6)
            {
                // v7에서 벽시계와 게시판이 어디까지 흘렀는지(clockElapsedMinutes, boardActivityMinute)가 추가됐다.
                // 구버전 저장본에는 적어 둔 적이 없다. -1 로 두면 시계와 게시판은 처음부터 센다.
                if (data.global != null)
                {
                    data.global.clockElapsedMinutes = -1;
                    data.global.boardActivityMinute = -1;
                }
                data.saveVersion = 7;
            }

            if (data.saveVersion == 7)
            {
                // v8에서 수사 노트(분석 기록, 맞힌 추론, 확정한 보고서)가 추가됐다.
                // 구버전 저장본에는 그 기록이 없다. 빈 목록이면 아직 분석도 추론도 하지 않은 것으로 읽힌다.
                // 얻은 단서는 예전 그대로 남으니 수사 노트의 단서 칸은 그대로 채워진다.
                data.analysisRecords ??= new System.Collections.Generic.List<string>();
                data.solvedDeductions ??= new System.Collections.Generic.List<string>();
                data.confirmedReports ??= new System.Collections.Generic.List<string>();
                data.saveVersion = 8;
            }

            if (data.saveVersion == 8)
            {
                // v9에서 보고서 빈칸에 적던 글(reportInputs)이 추가됐다. 구버전 저장본은 빈칸이 비어 있는 채로 열린다.
                // 이미 확정한 보고서는 confirmedReports 에 남아 있으니 다시 쓰라고 하지 않는다.
                data.reportInputs ??= new System.Collections.Generic.List<string>();
                data.saveVersion = 9;
            }

            if (data.saveVersion == 9)
            {
                // v10에서 플레이어가 괴담넷에 올린 글(boardPosts)이 추가됐다. 구버전 저장본에는 올린 글의 기록이 없다.
                // 빈 목록이면 아직 글을 올리지 않은 것으로 읽힌다. 게시 성공 표시만 남아 있으면 글쓰기 단추가 다시 서지 않으므로
                // 그 사건의 글은 게시판에 없는 채로 이어진다(예전과 같다).
                data.boardPosts ??= new System.Collections.Generic.List<SavedBoardPost>();
                data.saveVersion = 10;
            }

            if (data.saveVersion == 10)
            {
                // v11에서 본편의 하루 진행(work)과 게시판 글 상태(boardStates)가 추가됐다.
                // 구버전 저장본은 아직 본편을 시작하지 않은 것으로 읽힌다. 튜토리얼 진행은 그대로 남는다.
                data.work ??= new WorkState();
                data.boardStates ??= new System.Collections.Generic.List<BoardEntryState>();
                data.saveVersion = 11;
            }

            if (data.saveVersion < CurrentVersion)
            {
                Debug.LogWarning(
                    $"[SaveMigration] v{from} -> v{CurrentVersion} 변환 규칙이 없다. 버전만 올리고 기본값으로 채운다.");
                data.saveVersion = CurrentVersion;
            }

            Debug.Log($"[SaveMigration] 저장 파일을 v{from} 에서 v{data.saveVersion} 로 변환했다.");
            return Result.Migrated;
        }
    }
}
