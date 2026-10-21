# 괴담 검열국 (The Urban Legend Bureau) — Claude 작업 안내

Unity 6000.3.19f1, URP 2D. 사용자가 방향을 정하고 Claude 가 구현한다. 고친 것은 실제 플레이 모드에서 확인한다.
이 파일은 2026-10-21 에 이전 PC 의 Claude 메모리와 작업 규칙을 옮겨 적은 것이다. 자세한 원본은 이전 패키지의 `claude_memory/` 에 있다.

## 사용자와 일하는 규칙

- **답변·보고는 항상 한국어.** 코드 식별자와 경로는 그대로 둔다.
- 고칠 때마다 로컬 커밋. **푸시는 사용자가 말할 때만.** 커밋 끝에 `Co-Authored-By` 한 줄.
- 커밋하지 않는 것: `Assets/_Project/UI/Fonts/Pretendard/*SDF.asset`, `Assets/VARCO_URP/`, `Assets/VARCO_URP.meta`, `Assets/Resources.meta`, `Packages/manifest.json`, `Packages/packages-lock.json`.
- 씬을 다시 빌드한 뒤에는 `git checkout -- Assets/_Project/UI/Sprites/Closeup/` 로 Closeup 메타 변경을 되돌린다.
- 정해진 규칙(확정된 설정)을 마음대로 바꾸지 않는다.
- 사용자는 대화 방식을 번호로 부른다.
  - 타입 1: DialogueScreen 원래 모양(전신 + 가운데 큰 상자). 처음 브리핑, 컴퓨터 화면 위의 한영.
  - 타입 2: FieldHudScreen 아래 띠(작은 초상 + 이름 + 대사). 현장·숙소 상호작용의 기본.
  - 타입 3: 타입 1 을 띠 자리·색에 맞춘 것(`DialogueScreen.SetBandLayout(true)`). 숙소 장면 대화에만.

## 이야기·연출 규칙

- **한영은 귀신을 무서워하지 않는다.** 겁먹고 움찔하는 역은 차지한. 한영은 차분하게 짚는다.
- **불 꺼짐·형광등 깜빡임 연출은 아낀다.** 남은 것은 지하철 정전, 엘리베이터 3층 정전 두 곳뿐. 새 현장은 처음부터 어둡게 짓고 소리·문틈·움직임으로 무섭게.
- **일수가 지날수록 어려워진다.** `WorkdayDirector.DayPressure`(근무일마다 확산 +12%, 최대 2배), 동시 사건 수는 2일마다 +1(최대 5).
- **결말(보류):** 세계 자체가 「현실」이라는 거대한 괴담. 사건 20개 이상 해결 후 결말, 전체 또는 괴담 하나가 100% 면 게임 오버. 복선(지워지지 않는 괴담넷, "가짜가 진짜가 되는 경우는 없었다" 등)에 어긋나지 않게.
- 현장 화면 배치: 위 2/3 장면, 아래 1/3 초상 + 대사 띠. 참고 화면의 그림을 따라 만들지 않는다.
- 판정 후보 문구 `ui.rule.verdict_opt_*` 는 지금 전 괴담 공용. 괴담별로 나누는 일은 사용자가 요청할 때만.

## 본편 사건 (2026-10-20 기준 #001~#006, "6까지만" — #007 부터는 지시를 받고 만든다)

| 번호 | 이름 | 진위 | 형식 |
|---|---|---|---|
| #001 | 삭제되지 않는 게시글 (자취방) | — | 문 앞에서 사람 없는지 확인 후 진입 |
| #002 | 구도시 구오 빌라 엘리베이터 | — | 일반 조사 |
| #003 | 7호실의 노크 (동명고시원) | 진짜 | 퇴마 먼저(결계 순서: 소금→부적→초, 한영이 직접 걸어가 친다) → 믿음도 35% → 수사 노트·정정글. 8호실 고시생과 대화(현장 인물) |
| #004 | 4시 44분의 호출 (새벽마트) | 가짜 | 일반 조사 |
| #005 | 빈 그네의 아이 (달빛 어린이공원) | 진짜(슬픈 이야기) | 엄마를 기다리던 아이를 배웅(신발→이름표→초) |
| #006 | 해월장 304호 안전 수칙 (나폴리탄) | 진짜, 초반 최고 난도 | 304호에 갇힘. 검은 수칙만 순서대로(거울 가리기→노크에 같은 횟수→전화 받고 말없이 끊기→커튼 걷기). 틀리면 처음부터 +3%p |

- 수사 노트에서 틀리면 그 괴담 믿음도 +1%p. 퇴마·배웅·탈출을 마치면 믿음도 35%(`RiteDoneBelief`).
- 장소마다 무서운 연출 2~3개(`FieldScare`).

## 코드 지도

- 사건 흐름: `Assets/_Project/Scripts/Systems/Case/CaseDirector*.cs`
  - `CaseDirector.Exorcism.cs` — 의식 틀 `RiteDef` (#003 결계, #005 배웅, #006 갇힘 `StepOptions`)
  - `CaseDirector.Napolitan.cs` — #006
  - `CaseDirector.Npc.cs` — 현장 인물 대화 틀 `NpcDef` (인사 → 물을 것 고르기 → 단서 → 퇴마 뒤 인사). 말하는 사람 접두사 `@` 인물, `!` 한영, 없음 차지한
  - `CaseDirector.MainField.cs` — 본편 현장 지점 처리
- 근무·난이도: `Systems/Workday/WorkdayDirector.cs`
- 현장: `Systems/Field/*` (`FieldScare`, `FieldSceneSwap`, `FieldSpriteAnimator`, `InvestigationPoint`)
- 씬은 코드로 만든다: `Scripts/Editor/SliceSceneBuilder*.cs` (메뉴 "Build 01_Title Slice Scene"). 현장 루트는 `FieldRoot_Legend3~6`, `FieldController._fieldGroups` 는 8칸.
- 사건 데이터: `Scripts/Editor/MainCaseData*.cs` (메뉴 "UrbanLegendBureau/Data/Apply Main Case Data"). #004~#006 은 #002 에셋을 복사해 ID 만 바꾼다(`ApplyClonedCase`).
- 문구: `Assets/_Project/Resources/Localization/Strings.txt` (탭 구분: id, ko, en, 메모)
- 인물 그림: `Resources/Characters/{Hanyoung,Chajihan,Staff,Npc/<이름>}/Portrait/*.png` (512×512, 얼굴 크기는 눈 높이 약 42%, 눈~턱 약 150px), 현장 그림 `Art/Characters/<이름>/Field/{walk,idle,...}_N.png` (약 260×492, 투명). 현장 인물 이름 ID 는 `npc.` 로 시작.

## 작업 방법

- 코드를 고친 뒤 **컴파일이 끝난 다음** 씬을 빌드한다. 컴파일 중에 빌드하면 씬이 조용히 저장되지 않는다(씬 파일 수정 시각으로 확인).
- 테스트 중 현장에서 `ui.CloseAll()` 을 부르지 않는다(FieldHud 가 닫혀 대사가 깨진다). `BeginCaseField(caseId, true)` 는 단서를 지우므로 이어서 볼 땐 `false`.
- 테스트로 저장본(`%USERPROFILE%\AppData\LocalLow\DefaultCompany\The Urban Legend Bureau\Saves\slot_0.json`)이 바뀌면 테스트 전으로 되돌린다.
- 유니티 연결: `.mcp.json` 의 MCP for Unity (포트 6400). 이전 PC 에서 쓰던 보조 스크립트는 이전 패키지의 `claude_tools/` 에 있다(경로는 새 PC 에 맞게 고쳐 써야 한다).
- 윈도우 빌드 출력: `Builds/Windows/UrbanLegendBureau.exe`
