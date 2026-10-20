using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 본편 사건 현장. 막차처럼 걸어 다니며 지점을 누르면 그 물건을 가까이 들여다보고, 살필 곳에서 단서를 얻는다.
    ///
    ///   가까이 보기   지점 ID 마다 가까이 보기 화면이 하나씩 있다(_mainCloseups). 살필 곳에 조사 행동이 걸려 있으면
    ///                 처음 살필 때 그 행동을 한다(시간·확산·단서). 이미 얻은 단서의 자리는 다시 눌러도 시간이 들지 않는다.
    ///                 행동의 조건(먼저 얻을 단서, 인터넷 조사)이 모자라면 차지한이 한마디 하고 살피지 않는다.
    ///   연출          사건 #001: 모니터의 그 글이 새로고침되며 다른 작성자로 다시 올라온다 / 휴대폰에 공유 알림이 하나 더 온다.
    ///                 사건 #002: 엘리베이터에 타면 아무도 누르지 않은 3층에 서서 문이 열린다. 닫힘 버튼이 듣지 않다가 저절로 닫힌다.
    ///                 연출은 사건마다 한 번. 본 것은 storyFlags 에 남긴다(다시 들어가도 되풀이하지 않는다).
    /// </summary>
    public partial class CaseDirector
    {
        [Serializable]
        public class MainCloseup
        {
            public string pointId;
            public CloseupScreen screen;
        }

        [Header("본편 현장")]
        [Tooltip("본편 사건 현장의 가까이 보기 화면. 지점 ID 와 짝을 짓는다.")]
        [SerializeField] private List<MainCloseup> _mainCloseups = new List<MainCloseup>();

        [Tooltip("사건 #002 엘리베이터 안의 움직이는 것들.")]
        [SerializeField] private ElevatorCar _elevator;

        private const string ElevBoardPointId = "point_elev_board";
        private const string MainEventFlagPrefix = "field_event:";
        private const string SpotNotYetTextId = "field.main.spot_not_yet";

        private static readonly string[] DeskRefreshLines = { "field.post.refresh.001", "field.post.refresh.002", "field.post.refresh.003" };
        private static readonly bool[] DeskRefreshHanyoung = { false, true, false };
        private static readonly string[] PhoneBuzzLines = { "field.post.buzz.001", "field.post.buzz.002" };
        private static readonly bool[] PhoneBuzzHanyoung = { false, true };
        private static readonly string[] ElevRideLines = { "field.elev.ride.001", "field.elev.ride.002" };
        private static readonly bool[] ElevRideHanyoung = { true, false };
        private static readonly string[] ElevOpenLines = { "field.elev.open.001", "field.elev.open.002", "field.elev.open.003", "field.elev.open.004" };
        private static readonly bool[] ElevOpenHanyoung = { false, true, false, true };

        private CloseupScreen FindMainCloseup(string pointId)
        {
            foreach (var m in _mainCloseups) if (m != null && m.screen != null && m.pointId == pointId) return m.screen;
            return null;
        }

        private bool MainEventDone(string id) => HasFlag(MainEventFlagPrefix + id);

        private void MarkMainEvent(string id)
        {
            if (_save == null || _save.Current == null || MainEventDone(id)) return;
            _save.Current.storyFlags.Add(MainEventFlagPrefix + id);
            _save.MarkDirty();
        }

        /// <summary>사건을 새로 고를 때 본편 현장의 보이는 것을 처음으로 돌린다. 얻은 단서와 본 연출은 저장본에 있다.</summary>
        private void ResetMainField()
        {
            ResetRiteVisuals();
            WireFieldScares();
            foreach (var m in _mainCloseups) if (m != null && m.screen != null) m.screen.ResetChecked();
            if (_elevator != null) _elevator.ResetCar();
            // 갈아 끼우는 본편 현장(복도→원룸, 홀→엘리베이터)은 처음 장소로 돌린다. 막차 현장은 건드리지 않는다.
            foreach (var swap in FindObjectsByType<FieldSceneSwap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (swap.name.StartsWith("FieldRoot_Legend")) swap.SetAfter(false);
        }

        /// <summary>현장 연출(FieldScare)이 저장본과 대사 띠를 쓰도록 건다. 여러 번 불러도 같다.</summary>
        private void WireFieldScares()
        {
            FieldScare.Done = id => MainEventDone("scare_" + id);
            FieldScare.Mark = id => MarkMainEvent("scare_" + id);
            FieldScare.HasFlag = HasFlag;
            FieldScare.CanFire = () => InMain && !FieldHudScreen.IsSpeaking && FieldHudScreen.IsFront && !FieldHudScreen.IsCutscene && !TravelScreen.IsPlaying;
            FieldScare.Say = (lines, hanyoung, done) => CctvLines(lines, hanyoung, 0, () => { _fieldHudScreen.ClearSpeech(); done?.Invoke(); });
        }

        /// <summary>본편 현장 지점을 눌렀다. 맡은 일이 있으면 처리하고 true.</summary>
        private bool TryHandleMainFieldPoint(InvestigationPoint point)
        {
            if (point == null) return false;
            switch (point.PointId)
            {
                case ElevBoardPointId: BoardElevator(); return true;
                case PostDoorPointId: KnockPostDoor(); return true;
                case PostExitPointId: LeavePostRoom(); return true;
            }
            if (TryHandleGosiPoint(point)) return true;
            if (TryHandleNpcPoint(point)) return true;
            if (MainRemarks.TryGetValue(point.PointId, out var remark))
            {
                CctvLines(remark.lines, remark.hanyoung, 0, () => _fieldHudScreen.ClearSpeech());
                return true;
            }

            var screen = FindMainCloseup(point.PointId);
            if (screen == null) return false;
            OpenMainCloseup(point.PointId, screen);
            return true;
        }

        // ------------------------------------------------------------- 한마디만 하는 지점

        /// <summary>가까이 볼 것 없이 한마디 나누는 지점. 시간은 흐르지 않는다. '!' 로 시작하면 한영이 한다.</summary>
        private static readonly Dictionary<string, (string[] lines, bool[] hanyoung)> MainRemarks = new Dictionary<string, (string[], bool[])>
        {
            { "point_post_meter", Remark("field.post.meter.001", "!field.post.meter.002") },
            { "point_post_window", Remark("field.post.window.001", "!field.post.window.002") },
        };

        private static (string[] lines, bool[] hanyoung) Remark(params string[] ids)
        {
            var lines = new string[ids.Length];
            var hanyoung = new bool[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                hanyoung[i] = ids[i].StartsWith("!");
                lines[i] = hanyoung[i] ? ids[i].Substring(1) : ids[i];
            }
            return (lines, hanyoung);
        }

        // ------------------------------------------------------------- 사건 #001 302호 문

        private const string PostDoorPointId = "point_post_door";
        private const string PostExitPointId = "point_post_exit";

        /// <summary>
        /// 302호 문. 처음에는 두드리고 초인종을 눌러 사람이 없는지 확인한다. 대답이 없고 안에서 알림 소리만 난다.
        /// 확인한 뒤로는 관리인에게 받은 열쇠로 곧장 들어간다.
        /// </summary>
        private void KnockPostDoor()
        {
            if (MainEventDone("post_knock")) { EnterPostRoom(); return; }
            MarkMainEvent("post_knock");
            StartCoroutine(KnockRoutine());
        }

        private IEnumerator KnockRoutine()
        {
            FieldHudScreen.IsCutscene = true;
            for (int i = 0; i < 3; i++) { Sfx.Play("door_bang_0", 0.45f, 1.15f); yield return new WaitForSeconds(0.32f); }
            yield return new WaitForSeconds(0.6f);
            bool said = false;
            CctvLines(KnockLines, KnockHanyoung, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();
            Sfx.Play("intercom_ring", 0.4f);   // 초인종
            yield return new WaitForSeconds(2.2f);
            // 아무 대답도 없는데, 문 너머에서 휴대폰 알림이 두 번 울린다.
            Sfx.Play("phone_click", 0.22f, 0.7f);
            yield return new WaitForSeconds(0.5f);
            Sfx.Play("phone_click", 0.22f, 0.7f);
            yield return new WaitForSeconds(1.0f);
            said = false;
            CctvLines(KnockAfterLines, KnockAfterHanyoung, 0, () => said = true);
            while (!said) yield return null;
            _fieldHudScreen.ClearSpeech();
            FieldHudScreen.IsCutscene = false;
            EnterPostRoom();
        }

        private static readonly string[] KnockLines = { "field.post.knock.001", "field.post.knock.002" };
        private static readonly bool[] KnockHanyoung = { false, true };
        private static readonly string[] KnockAfterLines = { "field.post.knock.003", "field.post.knock.004", "field.post.knock.005" };
        private static readonly bool[] KnockAfterHanyoung = { false, true, true };

        /// <summary>열쇠로 302호에 들어간다. 원룸으로 바뀌고 현관 앞에 선다.</summary>
        private void EnterPostRoom()
        {
            var root = _field != null ? _field.ActiveRoot : null;
            var swap = root != null ? root.GetComponent<FieldSceneSwap>() : null;
            if (swap == null) return;
            Sfx.Play("key_1", 0.5f);
            Sfx.Play("room_door_open", 0.45f);
            swap.SetAfter(true);
            foreach (var walker in root.GetComponentsInChildren<FieldWalker>(true)) if (walker.isActiveAndEnabled) walker.ResetToStart();
            Debug.Log("[CaseDirector] 본편 현장 | 302호에 들어간다");
        }

        /// <summary>현관으로 나가 복도에 선다.</summary>
        private void LeavePostRoom()
        {
            var root = _field != null ? _field.ActiveRoot : null;
            var swap = root != null ? root.GetComponent<FieldSceneSwap>() : null;
            if (swap == null) return;
            Sfx.Play("room_door", 0.45f);
            swap.SetAfter(false);
            foreach (var walker in root.GetComponentsInChildren<FieldWalker>(true)) if (walker.isActiveAndEnabled) walker.ResetToStart();
        }

        private void OpenMainCloseup(string pointId, CloseupScreen screen)
        {
            screen.Closed = () =>
            {
                if (_ui.Contains(screen)) _ui.Close(screen);
                FieldScare.NotifyPoint(pointId);   // 살피고 돌아오면 터지는 연출
            };
            screen.SpotIntercept = index => InterceptMainSpot(screen, index);
            screen.SpotIndexChecked = index =>
            {
                PerformMainSpot(screen, pointId, index);
                AfterMainSpot(pointId, screen, index);
            };
            screen.SpotChecked = null;
            screen.SetDark(false);
            ApplyMainEventState(screen);
            _ui.Push(screen);
            Sfx.Play("closeup_open", 0.4f);
            Debug.Log($"[CaseDirector] 본편 현장 | {pointId} 가까이 보기");
        }

        /// <summary>
        /// 살필 곳을 누르기 전에 본다. 걸린 조사 행동의 조건이 모자라고 그 단서도 아직 없으면, 차지한이 한마디 하고 살피지 않는다.
        /// 살핀 것으로 치지 않으므로 조건을 채운 뒤 다시 누르면 그때 살핀다.
        /// </summary>
        private bool InterceptMainSpot(CloseupScreen screen, int index)
        {
            var action = MainSpotAction(screen, index);
            if (action == null || _actions == null) return false;
            if (action.HasRewardClue && CaseFlow.HasClue(_save, action.RewardClueId)) return false;
            if (_actions.CanPerform(_save, action)) return false;
            screen.Talk(new[] { FieldSpeakerTextId }, new[] { SpotNotYetTextId }, null);
            return true;
        }

        private InvestigationActionSO MainSpotAction(CloseupScreen screen, int index)
        {
            string id = screen.SpotActionId(index);
            return string.IsNullOrEmpty(id) || _actions == null ? null : _actions.GetAction(id);
        }

        /// <summary>
        /// 처음 살핀 곳. 조사 행동이 걸린 자리면 그 행동을 한다(PerformCloseupSpot: 시간·확산·단서·알림).
        /// 단서를 이미 얻은 자리면 시간을 쓰지 않는다. 행동이 없는 자리는 둘러본 것으로 몇 분이 흐른다.
        /// </summary>
        private void PerformMainSpot(CloseupScreen screen, string pointId, int index)
        {
            var action = MainSpotAction(screen, index);
            if (action != null && action.HasRewardClue && CaseFlow.HasClue(_save, action.RewardClueId)) return;
            PerformCloseupSpot(screen, pointId, index);
        }

        /// <summary>살핀 뒤의 연출. 사건마다 한 번.</summary>
        private void AfterMainSpot(string pointId, CloseupScreen screen, int index)
        {
            // 사건 #001 책상: 두 군데를 살피면 모니터의 글이 새로고침되며 다시 올라온다.
            if (pointId == "point_desk" && !MainEventDone("desk_refresh") && CountChecked(screen) >= 2)
            {
                MarkMainEvent("desk_refresh");
                StartCoroutine(DeskRefresh(screen));
                return;
            }
            // 사건 #001 휴대폰: 알림을 살핀 뒤 하나가 더 온다.
            if (pointId == "point_phone" && index == 0 && !MainEventDone("phone_buzz"))
            {
                MarkMainEvent("phone_buzz");
                StartCoroutine(PhoneBuzz(screen));
                return;
            }
            // 사건 #001 우유 투입구: 안쪽의 파란 빛이 한 번 꺼졌다 켜진다. 누가 앞을 지나간 것처럼.
            if (pointId == "point_post_pile" && index == 2 && !MainEventDone("post_slot"))
            {
                MarkMainEvent("post_slot");
                StartCoroutine(SlotBlink(screen));
                return;
            }
            // 사건 #002 비상 호출: 잡음뿐이다.
            if (pointId == "point_panel" && index == 2) Sfx.Play("intercom_static", 0.4f);
        }

        /// <summary>이미 본 연출의 결과를 다시 연 화면에도 남긴다. 새로고침된 글, 네 번째 알림.</summary>
        private void ApplyMainEventState(CloseupScreen screen)
        {
            var monitor = screen.transform.Find("Monitor");
            if (monitor != null && MainEventDone("desk_refresh"))
            {
                var postA = monitor.Find("PostA");
                var postB = monitor.Find("PostB");
                if (postA != null) postA.gameObject.SetActive(false);
                if (postB != null) postB.gameObject.SetActive(true);
            }
            var phone = screen.transform.Find("Phone");
            var fourth = phone != null ? phone.Find("Notice_3") : null;
            if (fourth != null && MainEventDone("phone_buzz")) fourth.gameObject.SetActive(true);
        }

        /// <summary>우유 투입구 틈의 파란 빛이 뚝 끊겼다가 돌아온다. 한영은 모니터 화면보호기라고 본다.</summary>
        private IEnumerator SlotBlink(CloseupScreen screen)
        {
            screen.SetInteractable(false);
            var leak = screen.transform.Find("MilkSlot/Leak");
            yield return new WaitForSeconds(0.9f);
            if (leak != null) leak.gameObject.SetActive(false);
            Sfx.Play("heartbeat", 0.35f);
            yield return new WaitForSeconds(1.1f);
            if (leak != null) leak.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.4f);
            screen.Talk(Speakers(SlotHanyoung), SlotLines, () => screen.SetInteractable(true));
        }

        private static readonly string[] SlotLines = { "field.post.slot.001", "field.post.slot.002" };
        private static readonly bool[] SlotHanyoung = { false, true };

        private static int CountChecked(CloseupScreen screen)
        {
            int n = 0;
            for (int i = 0; i < 8; i++) if (screen.IsChecked(i)) n++;
            return n;
        }

        private IEnumerator DeskRefresh(CloseupScreen screen)
        {
            screen.SetInteractable(false);
            var monitor = screen.transform.Find("Monitor");
            var refresh = monitor != null ? monitor.Find("Refresh") : null;
            var postA = monitor != null ? monitor.Find("PostA") : null;
            var postB = monitor != null ? monitor.Find("PostB") : null;
            yield return new WaitForSeconds(0.8f);
            // 화면이 한 번 깜빡이고 저절로 새로고침된다.
            Sfx.Play("lamp_flicker", 0.25f);
            if (refresh != null) refresh.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.25f);
            if (refresh != null) refresh.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.12f);
            if (refresh != null) refresh.gameObject.SetActive(true);
            Sfx.Play("ui_click", 0.4f);
            yield return new WaitForSeconds(1.1f);
            if (postA != null) postA.gameObject.SetActive(false);
            if (postB != null) postB.gameObject.SetActive(true);
            if (refresh != null) refresh.gameObject.SetActive(false);
            Sfx.Play("ghost_whisper_0", 0.18f);
            yield return new WaitForSeconds(0.9f);
            screen.Talk(Speakers(DeskRefreshHanyoung), DeskRefreshLines, () => screen.SetInteractable(true));
        }

        private IEnumerator PhoneBuzz(CloseupScreen screen)
        {
            screen.SetInteractable(false);
            yield return new WaitForSeconds(1.0f);
            var phone = screen.transform.Find("Phone");
            var fourth = phone != null ? phone.Find("Notice_3") : null;
            // 짧게 두 번 떨린다.
            for (int i = 0; i < 2; i++)
            {
                Sfx.Play("phone_click", 0.5f, 0.7f);
                if (phone != null) yield return Shake(phone, 0.22f, 8f);
                yield return new WaitForSeconds(0.12f);
            }
            if (fourth != null) fourth.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.6f);
            screen.Talk(Speakers(PhoneBuzzHanyoung), PhoneBuzzLines, () => screen.SetInteractable(true));
        }

        private static IEnumerator Shake(Transform t, float seconds, float pixels)
        {
            var rt = t as RectTransform;
            if (rt == null) yield break;
            var start = rt.anchoredPosition;
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                rt.anchoredPosition = start + UnityEngine.Random.insideUnitCircle * pixels;
                yield return null;
            }
            rt.anchoredPosition = start;
        }

        private string[] Speakers(bool[] hanyoung)
        {
            var list = new string[hanyoung.Length];
            for (int i = 0; i < list.Length; i++) list[i] = hanyoung[i] ? HanyoungNameTextId : FieldSpeakerTextId;
            return list;
        }

        // ------------------------------------------------------------- 사건 #002 엘리베이터

        /// <summary>
        /// 홀의 엘리베이터에 탄다. 안쪽으로 바뀌고, 처음 탈 때만 3층 연출이 나온다.
        /// 연출 동안은 걷지 못한다. 연출을 본 뒤에는 탄 채로 조작판과 거울을 살핀다.
        /// </summary>
        private void BoardElevator()
        {
            var root = _field != null ? _field.ActiveRoot : null;
            var swap = root != null ? root.GetComponent<FieldSceneSwap>() : null;
            if (swap == null) return;
            Sfx.Play("door_slide", 0.5f);
            swap.SetAfter(true);
            if (root != null)
                foreach (var walker in root.GetComponentsInChildren<FieldWalker>(true)) if (walker.isActiveAndEnabled) walker.ResetToStart();
            if (_elevator == null || MainEventDone("elev_3f")) return;

            MarkMainEvent("elev_3f");
            FieldHudScreen.IsCutscene = true;
            _elevator.ResetCar();
            // 한영이 묻고 차지한이 3층 불이 켜진 것을 본다. 그사이 엘리베이터가 오른다.
            CctvLines(ElevRideLines, ElevRideHanyoung, 0, () =>
            {
                _fieldHudScreen.ClearSpeech();
                _elevator.Ride(3, 1.5f, () =>
                {
                    _elevator.ShakeCabinLight();
                    _elevator.OpenDoors(() => StartCoroutine(ElevatorWait()));
                });
            });
        }

        /// <summary>3층에서 문이 열려 있다. 아무도 없다. 닫힘 버튼이 듣지 않다가 몇 초 뒤 저절로 닫힌다.</summary>
        private IEnumerator ElevatorWait()
        {
            yield return new WaitForSeconds(1.6f);
            bool talked = false;
            CctvLines(ElevOpenLines, ElevOpenHanyoung, 0, () => talked = true);
            while (!talked) yield return null;
            _fieldHudScreen.ClearSpeech();
            yield return new WaitForSeconds(2.2f);
            _elevator.CloseDoors(() =>
            {
                FieldHudScreen.IsCutscene = false;
                Debug.Log("[CaseDirector] 본편 | 엘리베이터 3층 연출 끝");
            });
        }
    }
}
