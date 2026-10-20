using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Systems;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 본편 사건 두 곳의 현장. 막차(지하철)처럼 걸어 다니며 조사 지점을 누르면 그 물건을 화면 가득 들여다본다.
    ///
    ///   사건 #001 삭제되지 않는 게시글   다세대 3층 복도(302호 문 앞) → 자취방. 문 앞 우편물, 책상과 모니터, 휴대폰, 벽 낙서.
    ///   사건 #002 3층에서 멈추는 엘리베이터   구도시 구오 빌라 1층 홀 → 엘리베이터 안. 관리실 게시판, 조작판과 거울.
    ///
    /// 그림 파일 없이 도형(네모, 흐린 동그라미, 띠)과 글자로만 짓는다. 다른 현장과 같은 도구를 쓴다.
    /// 가까이 보기에서 살필 곳마다 조사 행동이 걸려 있어, 살피면 시간이 흐르고 단서가 생긴다(CaseDirector.PerformCloseupSpot).
    /// </summary>
    public static partial class SliceSceneBuilder
    {
        // =========================================================== 사건 #001 원룸

        /// <summary>
        /// 사건 #001 현장. 검열국이 글을 지우던 사람의 주소를 따서 찾아왔다.
        /// 다세대 3층 복도(302호 문 앞)에서 사람이 없는지 먼저 확인하고, 관리인 열쇠로 들어가면 원룸으로 바뀐다.
        /// 돌려주는 지점: 복도[문, 문 앞 우편물, 계량기], 방[책상, 휴대폰, 벽 낙서, 현관, 창문].
        /// </summary>
        private static GameObject[] BuildPostField(Transform parent)
        {
            var hall = BuildPostHall(parent, out var doorPoint, out var pilePoint, out var meterPoint);
            var room = BuildPostRoomField(parent, out var roomPoints);

            var swap = parent.gameObject.AddComponent<FieldSceneSwap>();
            var so = new SerializedObject(swap);
            so.Update();
            SetObjectArray(so.FindProperty("_beforeRoots"), hall);
            SetObjectArray(so.FindProperty("_afterRoots"), room);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new[] { doorPoint, pilePoint, meterPoint, roomPoints[0], roomPoints[1], roomPoints[2], roomPoints[3], roomPoints[4] };
        }

        /// <summary>
        /// 낡은 다세대의 3층 복도. 아래는 녹색 페인트, 위는 누런 벽. 왼쪽 끝 계단참에 창, 천장에 센서등 하나.
        /// 301·302·303 철문이 늘어서 있다. 302호 문 앞에 전단과 고지서가 쌓여 있다.
        /// 무서운 연출: 센서등이 꺼진다 / 노크 뒤 안에서 알림 소리(진행 담당) / 계단에서 내려오던 발소리가 멈춘다.
        /// </summary>
        private static GameObject BuildPostHall(Transform parent, out GameObject doorPoint, out GameObject pilePoint, out GameObject meterPoint)
        {
            var root = new GameObject("PostHall");
            root.transform.SetParent(parent, false);

            var upper = new Color(0.40f, 0.38f, 0.32f);
            var lower = new Color(0.16f, 0.24f, 0.22f);
            var stripe = new Color(0.10f, 0.15f, 0.14f);
            var floorCol = new Color(0.27f, 0.27f, 0.26f);
            var steel = new Color(0.30f, 0.25f, 0.22f);
            var steelDark = new Color(0.20f, 0.17f, 0.15f);
            var frameCol = new Color(0.22f, 0.22f, 0.24f);
            var shadow = new Color(0f, 0f, 0f, 0.4f);

            // 벽. 허리 아래는 짙은 녹색 페인트, 위는 누렇게 바랜 벽. 얼룩이 군데군데.
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), upper, -9);
            AddFieldRect(root.transform, "WallLow", new Vector2(0f, -2.15f), new Vector2(44f, 2.5f), lower, -8);
            AddFieldRect(root.transform, "WallStripe", new Vector2(0f, -0.88f), new Vector2(44f, 0.08f), stripe, -8);
            foreach (var p in new[] { new Vector2(-3.2f, 1.9f), new Vector2(7.8f, 2.4f), new Vector2(12.6f, 0.6f) })
                AddFieldSoft(root.transform, "Stain", p, new Vector2(1.8f, 1.3f), new Color(0.22f, 0.20f, 0.14f, 0.35f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.75f), new Vector2(44f, 0.5f), new Color(0.24f, 0.23f, 0.21f), -8);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), floorCol, -8);
            // 테라조 바닥의 잔 돌가루.
            var rng = new System.Random(302);
            for (int i = 0; i < 70; i++)
            {
                float x = -21f + (float)rng.NextDouble() * 42f, y = -5.4f + (float)rng.NextDouble() * 1.9f;
                AddFieldRect(root.transform, "Grit_" + i, new Vector2(x, y), new Vector2(0.06f, 0.04f), new Color(0.36f, 0.36f, 0.34f), -7);
            }
            AddFieldRect(root.transform, "Baseboard", new Vector2(0f, -3.32f), new Vector2(44f, 0.16f), stripe, -7);
            AddFieldGradient(root.transform, "FloorContact", new Vector2(0f, -3.0f), new Vector2(44f, 0.6f), new Color(0f, 0f, 0f, 0.3f), -7, 180f);

            // 왼쪽 끝 계단. 위로 올라가는 계단참이 어둡게 뚫려 있고, 참에 비 오는 창.
            AddFieldRect(root.transform, "Stairwell", new Vector2(-11.6f, 0.0f), new Vector2(5.0f, 7.0f), new Color(0.07f, 0.07f, 0.08f), -7);
            // 계단. 칸마다 바닥까지 채운 덩어리로 쌓아 올라가는 옆모습이 보이게 한다. 디딤판 끝에 밝은 줄.
            for (int i = 0; i < 7; i++)
            {
                float h = 0.5f * (i + 1);
                float sx = -9.5f - i * 0.6f;
                AddFieldRect(root.transform, "Step_" + i, new Vector2(sx, -3.35f + h * 0.5f), new Vector2(0.62f, h), new Color(0.19f, 0.19f, 0.2f), -6);
                AddFieldRect(root.transform, "StepNose_" + i, new Vector2(sx, -3.35f + h - 0.03f), new Vector2(0.62f, 0.06f), new Color(0.34f, 0.34f, 0.33f), -5);
            }
            var rail = AddFieldRect(root.transform, "Handrail", new Vector2(-11.3f, -0.45f), new Vector2(5.0f, 0.09f), new Color(0.38f, 0.32f, 0.25f), -4);
            rail.transform.localRotation = Quaternion.Euler(0f, 0f, 39.8f);
            for (int i = 0; i < 4; i++)
                AddFieldRect(root.transform, "Baluster_" + i, new Vector2(-9.8f - i * 1.2f, -2.3f + i * 1.0f), new Vector2(0.05f, 0.9f), new Color(0.3f, 0.26f, 0.22f), -4);
            var landing = new GameObject("Window");
            landing.transform.SetParent(root.transform, false);
            landing.transform.localPosition = new Vector3(-12.4f, 1.9f, 0f);
            AddFieldRect(landing.transform, "Frame", Vector2.zero, new Vector2(1.8f, 1.6f), frameCol, -6);
            AddFieldRect(landing.transform, "Glass", Vector2.zero, new Vector2(1.5f, 1.3f), new Color(0.08f, 0.10f, 0.16f), -5);
            AddFieldRect(landing.transform, "Mullion", Vector2.zero, new Vector2(0.06f, 1.3f), frameCol, -3);
            landing.AddComponent<WindowRain>();
            // 계단 벽을 스치는 그림자. 발소리 연출에서만 스르르 지나간다.
            var stairShadow = AddFieldSoft(root.transform, "StairShadow", new Vector2(-11.0f, 1.2f), new Vector2(1.4f, 3.2f), new Color(0f, 0f, 0f, 0.55f), -4);
            stairShadow.SetActive(false);

            // 천장 센서등. 가운데 하나뿐이다.
            AddFieldCircle(root.transform, "SensorLamp", new Vector2(-0.6f, 3.38f), 0.8f, new Color(0.32f, 0.31f, 0.28f), -6);   // 센서등은 고장 나 꺼져 있다
            AddFieldSoft(root.transform, "ExitGlowHall", new Vector2(13.5f, 1.5f), new Vector2(5f, 5f), new Color(0.2f, 0.9f, 0.45f, 0.07f), -6);

            // 철문 셋. 302호가 이번 주소다.
            float[] doorX = { -5.6f, 3.0f, 10.6f };
            string[] numbers = { "field.post.door_301", "field.post.door_302", "field.post.door_303" };
            for (int i = 0; i < doorX.Length; i++)
            {
                float x = doorX[i];
                AddFieldSoft(root.transform, "Shadow_Door" + i, new Vector2(x, -3.42f), new Vector2(2.8f, 0.3f), shadow, -6);
                AddFieldRect(root.transform, "DoorFrame_" + i, new Vector2(x, -0.55f), new Vector2(2.4f, 5.6f), frameCol, -6);
                AddFieldRect(root.transform, "Door_" + i, new Vector2(x, -0.6f), new Vector2(2.1f, 5.3f), steel, -5);
                AddFieldRect(root.transform, "DoorPanel_" + i, new Vector2(x, 0.4f), new Vector2(1.6f, 2.2f), steelDark, -4);
                AddFieldRect(root.transform, "DoorPanelLow_" + i, new Vector2(x, -2.2f), new Vector2(1.6f, 1.4f), steelDark, -4);
                AddFieldRect(root.transform, "Lever_" + i, new Vector2(x - 0.72f, -0.75f), new Vector2(0.42f, 0.1f), new Color(0.62f, 0.60f, 0.55f), -3);
                AddFieldRect(root.transform, "Lock_" + i, new Vector2(x - 0.72f, -0.35f), new Vector2(0.24f, 0.36f), new Color(0.12f, 0.12f, 0.13f), -3);
                AddFieldRect(root.transform, "Plate_" + i, new Vector2(x, 2.55f), new Vector2(0.9f, 0.4f), new Color(0.82f, 0.80f, 0.72f), -4);
                var num = AddFieldWorldText(root.transform, "Number_" + i, new Vector2(x, 2.55f), new Vector2(0.86f, 0.38f), numbers[i], new Color(0.18f, 0.16f, 0.14f), -3);
                num.fontSizeMax = 2.6f;
                AddFieldRect(root.transform, "MilkSlot_" + i, new Vector2(x + 0.2f, -1.55f), new Vector2(0.7f, 0.16f), new Color(0.10f, 0.10f, 0.11f), -3);
            }
            // 302호. 도시가스 점검 스티커, 문틈에 꽂힌 전단, 문 앞에 쌓인 우편물.
            AddFieldRect(root.transform, "GasSticker", new Vector2(3.35f, 1.35f), new Vector2(0.5f, 0.32f), new Color(0.86f, 0.74f, 0.28f), -3);
            foreach (var (p, r) in new[] { (new Vector2(2.2f, 0.1f), 12f), (new Vector2(2.25f, -0.4f), -8f), (new Vector2(2.18f, -1.0f), 20f) })
            {
                var flyer = AddFieldRect(root.transform, "GapFlyer", p, new Vector2(0.55f, 0.32f), new Color(0.88f, 0.86f, 0.8f), -3);
                flyer.transform.localRotation = Quaternion.Euler(0f, 0f, r);
            }
            var pile = new GameObject("Pile");
            pile.transform.SetParent(root.transform, false);
            pile.transform.localPosition = new Vector3(1.3f, -3.25f, 0f);
            Color[] paper = { new Color(0.9f, 0.88f, 0.82f), new Color(0.84f, 0.42f, 0.36f), new Color(0.80f, 0.84f, 0.92f), new Color(0.92f, 0.86f, 0.6f) };
            for (int i = 0; i < 6; i++)
            {
                var sheet = AddFieldRect(pile.transform, "Sheet_" + i, new Vector2((i % 3 - 1) * 0.16f, i * 0.05f), new Vector2(0.8f, 0.12f), paper[i % paper.Length], -3 + i % 2);
                sheet.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 4f : -6f) + i);
            }
            // 초인종(인터폰). 302호 오른쪽.
            AddFieldRect(root.transform, "Bell", new Vector2(4.75f, -0.2f), new Vector2(0.42f, 0.62f), new Color(0.78f, 0.78f, 0.74f), -4);
            AddFieldCircle(root.transform, "BellButton", new Vector2(4.75f, -0.35f), 0.18f, new Color(0.35f, 0.35f, 0.36f), -3);
            // 전기 계량기 함. 둥근 유리 안에서 원판이 천천히 돈다(안에 켜 둔 것이 있다).
            AddFieldRect(root.transform, "MeterBox", new Vector2(6.8f, 1.0f), new Vector2(1.1f, 1.5f), new Color(0.48f, 0.48f, 0.46f), -5);
            AddFieldCircle(root.transform, "MeterGlass", new Vector2(6.8f, 1.1f), 0.7f, new Color(0.62f, 0.68f, 0.70f), -4);
            AddFieldRect(root.transform, "MeterDisc", new Vector2(6.8f, 0.95f), new Vector2(0.5f, 0.06f), new Color(0.2f, 0.2f, 0.22f), -3);
            // 301호 앞 신발, 303호 앞 젖은 우산. 사람이 사는 층이다.
            AddFieldRect(root.transform, "Shoes", new Vector2(-4.4f, -3.3f), new Vector2(0.7f, 0.18f), new Color(0.12f, 0.12f, 0.13f), -4);
            var umbrella = AddFieldRect(root.transform, "Umbrella", new Vector2(12.1f, -2.6f), new Vector2(0.12f, 1.6f), new Color(0.15f, 0.2f, 0.32f), -4);
            umbrella.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            AddFieldSoft(root.transform, "Puddle", new Vector2(12.4f, -3.4f), new Vector2(1.0f, 0.18f), new Color(0.5f, 0.6f, 0.8f, 0.25f), -6);
            AddFieldRect(root.transform, "Hydrant", new Vector2(14.2f, -0.4f), new Vector2(1.0f, 1.6f), new Color(0.55f, 0.12f, 0.12f), -5);

            AddFieldGradient(root.transform, "CeilingShade", new Vector2(0f, 3.0f), new Vector2(44f, 1.2f), new Color(0f, 0f, 0f, 0.4f), -6);
            AddFieldRect(root.transform, "Night", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.02f, 0.03f, 0.07f, 0.55f), -2);

            // 계단에서 올라와 302호 쪽을 보고 선다.
            BuildFieldActors(root.transform, -3.86f, -8.4f, 2.4f, -9.2f, 13.6f);

            doorPoint = BuildPoint(root.transform, "InvestigationPoint_PostDoor", new Vector2(3.0f, 0.2f), new Vector2(2.4f, 4.4f),
                new Color(0.6f, 0.5f, 0.4f), "field.post.door", "field.post.door", null);
            pilePoint = BuildPoint(root.transform, "InvestigationPoint_PostPile", new Vector2(1.2f, -3.0f), new Vector2(1.4f, 0.9f),
                new Color(0.85f, 0.8f, 0.7f), "field.post.pile", "field.post.pile", null);
            meterPoint = BuildPoint(root.transform, "InvestigationPoint_PostMeter", new Vector2(6.8f, 1.0f), new Vector2(1.4f, 1.8f),
                new Color(0.6f, 0.62f, 0.6f), "field.post.meter", "field.post.meter", null);
            SetHighlightTargets(doorPoint, root.transform, "Door_1", "DoorPanel_1", "DoorPanelLow_1");
            SetHighlightTargets(pilePoint, root.transform, "Pile");
            SetHighlightTargets(meterPoint, root.transform, "MeterBox", "MeterGlass");

            // --- 무서운 연출
            var dark = AddBlackout(root.transform, "Blackout", new Color(0f, 0f, 0.02f, 0.95f));
            // 가운데쯤 오면 센서등이 꺼진다. 잠깐 깜깜하다가 손을 흔들면 다시 켜진다.
            var lever303 = root.transform.Find("Lever_2").gameObject;
            AddScare(root.transform, "post_lever", FieldScare.TriggerKind.ReachX, 9.6f, null, 0f,
                SSound("door_bang_2", 0.3f, 1.5f), SShake(lever303, 0.5f, 0.05f), SWait(0.6f), SSound("door_bang_2", 0.25f, 1.6f), SShake(lever303, 0.3f, 0.05f), SWait(0.5f),
                SLines("field.post.scare.lever.001", "!field.post.scare.lever.002"));
            // 계단 위에서 누가 내려오는 발소리. 참 벽에 그림자가 비치다가 멈춘다. 아무도 내려오지 않는다.
            AddScare(root.transform, "post_stairs", FieldScare.TriggerKind.ReachX, 6.0f, null, 0f,
                SSound("step_soft_0", 0.25f), SWait(0.55f), SSound("step_soft_1", 0.3f), SWait(0.5f), SSound("step_soft_2", 0.38f),
                SFadeIn(stairShadow, 0.6f), SSound("step_soft_3", 0.45f), SWait(1.2f), SFadeOut(stairShadow, 0.4f), SWait(0.6f),
                SLines("field.post.scare.stairs.001", "field.post.scare.stairs.002", "!field.post.scare.stairs.003"));
            return root;
        }

        /// <summary>원룸 현장을 짓는다. 조사 지점(책상, 휴대폰, 벽 낙서, 현관, 창문)을 돌려준다.</summary>
        private static GameObject BuildPostRoomField(Transform parent, out GameObject[] points)
        {
            var root = new GameObject("PostRoom");
            root.transform.SetParent(parent, false);

            var wall = new Color(0.17f, 0.19f, 0.24f);
            var wallLow = new Color(0.14f, 0.15f, 0.20f);
            var trim = new Color(0.26f, 0.28f, 0.33f);
            var floor = new Color(0.19f, 0.18f, 0.19f);
            var floorLine = new Color(0.14f, 0.13f, 0.14f);
            var wood = new Color(0.30f, 0.25f, 0.21f);
            var woodDark = new Color(0.20f, 0.17f, 0.15f);
            var shadow = new Color(0f, 0f, 0f, 0.4f);

            // 방 껍데기. 숙소와 같은 높이 틀이되, 불 꺼진 원룸이라 차고 푸르다.
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), wall, -9);
            AddFieldRect(root.transform, "WallLow", new Vector2(0f, -2.4f), new Vector2(44f, 2.0f), wallLow, -8);
            AddFieldRect(root.transform, "CeilingTrim", new Vector2(0f, 3.62f), new Vector2(44f, 0.14f), trim, -7);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), floor, -8);
            AddFieldRect(root.transform, "Baseboard", new Vector2(0f, -3.3f), new Vector2(44f, 0.2f), woodDark, -7);
            for (int i = 0; i <= 30; i++)
                AddFieldRect(root.transform, "FloorSeam_" + i, new Vector2(-21.6f + i * 1.45f, -4.5f), new Vector2(0.03f, 2.2f), floorLine, -7);
            AddFieldRect(root.transform, "FloorRow", new Vector2(0f, -4.25f), new Vector2(44f, 0.03f), floorLine, -7);

            // 벽지의 아주 옅은 격자. 싸구려 원룸 벽지다.
            var grid = Color.Lerp(wall, Color.white, 0.03f);
            for (int i = 0; i <= 40; i++)
                AddFieldRect(root.transform, "Paper_" + i, new Vector2(-21.6f + i * 1.1f, 1.0f), new Vector2(0.04f, 5.4f), grid, -8);
            AddFieldGradient(root.transform, "CeilingShade", new Vector2(0f, 3.0f), new Vector2(44f, 1.4f), new Color(0f, 0f, 0f, 0.4f), -7);
            AddFieldGradient(root.transform, "FloorContact", new Vector2(0f, -3.0f), new Vector2(44f, 0.6f), new Color(0f, 0f, 0f, 0.3f), -7, 180f);

            // --- 왼쪽 창. 블라인드 틈으로 비 내리는 밤거리 불빛이 비친다.
            var window = new GameObject("Window");
            window.transform.SetParent(root.transform, false);
            window.transform.localPosition = new Vector3(-10.4f, 1.0f, 0f);
            AddFieldRect(window.transform, "Frame", Vector2.zero, new Vector2(4.0f, 3.2f), trim, -7);
            AddFieldRect(window.transform, "Glass", Vector2.zero, new Vector2(3.6f, 2.8f), new Color(0.07f, 0.09f, 0.15f), -6);
            var neon = new Color(0.85f, 0.30f, 0.38f);
            AddFieldSoft(window.transform, "Neon", new Vector2(0.9f, -0.7f), new Vector2(2.0f, 1.2f), new Color(neon.r, neon.g, neon.b, 0.35f), -5);
            AddFieldRect(window.transform, "Building_0", new Vector2(-1.1f, -0.9f), new Vector2(0.9f, 1.0f), new Color(0.05f, 0.06f, 0.10f), -5);
            AddFieldRect(window.transform, "Building_1", new Vector2(0.4f, -0.6f), new Vector2(1.2f, 1.6f), new Color(0.06f, 0.07f, 0.11f), -5);
            var lit = new Color(0.70f, 0.62f, 0.36f);
            foreach (var p in new[] { new Vector2(-1.3f, -0.7f), new Vector2(0.2f, -0.2f), new Vector2(0.6f, -0.6f), new Vector2(0.6f, 0.0f) })
                AddFieldRect(window.transform, "Lit", p, new Vector2(0.14f, 0.16f), lit, -4);
            // 블라인드 날개. 반쯤 내려 틈마다 빛이 갈라진다.
            for (int i = 0; i < 9; i++)
                AddFieldRect(window.transform, "Slat_" + i, new Vector2(0f, 1.25f - i * 0.2f), new Vector2(3.6f, 0.09f), new Color(0.55f, 0.57f, 0.62f), -3);
            AddFieldRect(window.transform, "BlindRail", new Vector2(0f, 1.42f), new Vector2(3.8f, 0.14f), trim, -3);
            AddFieldRect(window.transform, "Sill", new Vector2(0f, -1.66f), new Vector2(4.4f, 0.14f), trim, -3);
            // 창에서 방바닥으로 번지는 흐린 거리 불빛. 줄무늬가 따로 놀지 않게 옅은 빛 한 덩이로 둔다.
            AddFieldSoft(root.transform, "BlindLight", new Vector2(-8.2f, -3.7f), new Vector2(5.0f, 0.7f), new Color(0.65f, 0.72f, 0.95f, 0.08f), -6);
            window.AddComponent<WindowRain>();   // 숙소 창과 같은 빗줄기. Glass 크기에 저절로 맞춘다
            AddFieldRect(root.transform, "Radiator", new Vector2(-10.4f, -2.6f), new Vector2(2.4f, 1.1f), new Color(0.32f, 0.33f, 0.36f), -6);
            for (int i = 0; i < 9; i++)
                AddFieldRect(root.transform, "RadiatorFin_" + i, new Vector2(-11.4f + i * 0.25f, -2.6f), new Vector2(0.06f, 1.0f), new Color(0.24f, 0.25f, 0.28f), -5);

            // --- 침대. 이불이 엉망으로 걷혀 있다. 그 위에 화면이 켜진 휴대폰.
            AddFieldSoft(root.transform, "Shadow_Bed", new Vector2(-5.6f, -3.42f), new Vector2(5.4f, 0.4f), shadow, -7);
            AddFieldRect(root.transform, "BedFrame", new Vector2(-5.6f, -3.0f), new Vector2(5.0f, 0.6f), woodDark, -6);
            AddFieldRect(root.transform, "Mattress", new Vector2(-5.6f, -2.55f), new Vector2(4.9f, 0.45f), new Color(0.62f, 0.62f, 0.64f), -5);
            AddFieldRect(root.transform, "Blanket", new Vector2(-4.9f, -2.35f), new Vector2(3.6f, 0.5f), new Color(0.24f, 0.27f, 0.34f), -4);
            AddFieldRect(root.transform, "BlanketFold", new Vector2(-3.4f, -2.1f), new Vector2(1.1f, 0.28f), new Color(0.29f, 0.33f, 0.41f), -3);
            AddFieldRect(root.transform, "Pillow", new Vector2(-7.4f, -2.12f), new Vector2(1.0f, 0.36f), new Color(0.76f, 0.76f, 0.78f), -4);
            AddFieldRect(root.transform, "Headboard", new Vector2(-8.15f, -2.2f), new Vector2(0.3f, 2.0f), wood, -6);
            var phone = new GameObject("Phone");
            phone.transform.SetParent(root.transform, false);
            phone.transform.localPosition = new Vector3(-5.2f, -2.05f, 0f);
            AddFieldRect(phone.transform, "Body", Vector2.zero, new Vector2(0.62f, 0.12f), new Color(0.06f, 0.06f, 0.08f), -2);
            AddFieldRect(phone.transform, "Screen", new Vector2(0f, 0.02f), new Vector2(0.54f, 0.06f), new Color(0.55f, 0.75f, 1f), -1);
            AddFieldSoft(phone.transform, "Glow", new Vector2(0f, 0.25f), new Vector2(1.6f, 0.9f), new Color(0.5f, 0.7f, 1f, 0.25f), -1);

            // --- 책상과 모니터. 방에서 유일하게 켜진 것. 화면에는 괴담넷이 떠 있다.
            AddFieldSoft(root.transform, "Shadow_Desk", new Vector2(1.6f, -3.42f), new Vector2(4.8f, 0.42f), shadow, -7);
            AddFieldRect(root.transform, "DeskTop", new Vector2(1.6f, -1.6f), new Vector2(4.4f, 0.16f), wood, -5);
            AddFieldRect(root.transform, "DeskLeg_L", new Vector2(-0.45f, -2.5f), new Vector2(0.14f, 1.75f), woodDark, -6);
            AddFieldRect(root.transform, "DeskLeg_R", new Vector2(3.65f, -2.5f), new Vector2(0.14f, 1.75f), woodDark, -6);
            AddFieldRect(root.transform, "Drawer", new Vector2(3.0f, -1.98f), new Vector2(1.1f, 0.55f), woodDark, -5);
            AddFieldRect(root.transform, "Chair", new Vector2(1.4f, -2.75f), new Vector2(1.1f, 0.18f), new Color(0.12f, 0.12f, 0.14f), -4);
            AddFieldRect(root.transform, "ChairBack", new Vector2(0.95f, -2.1f), new Vector2(0.16f, 1.4f), new Color(0.12f, 0.12f, 0.14f), -4);
            AddFieldRect(root.transform, "ChairPost", new Vector2(1.4f, -3.1f), new Vector2(0.1f, 0.6f), new Color(0.1f, 0.1f, 0.12f), -4);
            var monitor = new GameObject("Monitor");
            monitor.transform.SetParent(root.transform, false);
            monitor.transform.localPosition = new Vector3(1.6f, -0.55f, 0f);
            AddFieldSoft(monitor.transform, "WallGlow", new Vector2(0f, 0.3f), new Vector2(6.0f, 4.0f), new Color(0.35f, 0.55f, 1f, 0.16f), -6);
            AddFieldRect(monitor.transform, "Bezel", Vector2.zero, new Vector2(2.3f, 1.45f), new Color(0.05f, 0.05f, 0.07f), -4);
            AddFieldRect(monitor.transform, "Screen", new Vector2(0f, 0.04f), new Vector2(2.1f, 1.22f), new Color(0.82f, 0.86f, 0.92f), -3);
            AddFieldRect(monitor.transform, "PageHead", new Vector2(0f, 0.53f), new Vector2(2.1f, 0.16f), new Color(0.22f, 0.27f, 0.38f), -2);
            for (int i = 0; i < 5; i++)
                AddFieldRect(monitor.transform, "PageLine_" + i, new Vector2(-0.2f + (i % 2) * 0.12f, 0.28f - i * 0.17f), new Vector2(1.5f - (i % 3) * 0.3f, 0.05f),
                    new Color(0.45f, 0.48f, 0.55f), -2);
            AddFieldRect(monitor.transform, "Stand", new Vector2(0f, -0.82f), new Vector2(0.18f, 0.3f), new Color(0.08f, 0.08f, 0.1f), -4);
            AddFieldRect(monitor.transform, "Foot", new Vector2(0f, -0.96f), new Vector2(0.8f, 0.08f), new Color(0.08f, 0.08f, 0.1f), -4);
            AddFieldRect(root.transform, "Keyboard", new Vector2(1.4f, -1.47f), new Vector2(1.5f, 0.1f), new Color(0.1f, 0.1f, 0.12f), -3);
            AddFieldRect(root.transform, "Mug", new Vector2(3.25f, -1.32f), new Vector2(0.28f, 0.36f), new Color(0.72f, 0.70f, 0.66f), -3);
            AddFieldRect(root.transform, "Can_0", new Vector2(-0.1f, -1.36f), new Vector2(0.18f, 0.32f), new Color(0.25f, 0.55f, 0.48f), -3);
            AddFieldRect(root.transform, "Can_1", new Vector2(4.6f, -3.25f), new Vector2(0.18f, 0.32f), new Color(0.25f, 0.55f, 0.48f), -3);
            // 책상 밑에 흘러내린 출력물. 가까이 보기에서 줍는다.
            var prints = AddFieldRect(root.transform, "Printouts", new Vector2(2.3f, -3.28f), new Vector2(1.0f, 0.08f), new Color(0.86f, 0.86f, 0.82f), -3);
            prints.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);

            // --- 오른쪽 벽. 매직으로 갈겨쓴 낙서. 붉은 글씨가 비뚤게 세 줄.
            var graffiti = new GameObject("Graffiti");
            graffiti.transform.SetParent(root.transform, false);
            graffiti.transform.localPosition = new Vector3(7.4f, 0.7f, 0f);
            var ink = new Color(0.55f, 0.13f, 0.15f);
            for (int i = 0; i < 3; i++)
            {
                var line = AddFieldWorldText(graffiti.transform, "Line_" + i, new Vector2(-0.1f + i * 0.15f, 0.55f - i * 0.6f), new Vector2(3.4f, 0.6f),
                    "field.post.graffiti_line", new Color(ink.r, ink.g, ink.b, 0.9f - i * 0.15f), -5);
                line.transform.localRotation = Quaternion.Euler(0f, 0f, 4f - i * 3f);
                line.fontSizeMax = 4.4f;
            }
            AddFieldSoft(graffiti.transform, "Smudge", new Vector2(0.2f, -0.6f), new Vector2(1.6f, 0.6f), new Color(ink.r, ink.g, ink.b, 0.2f), -5);

            // 맨 오른쪽 현관문과 신발장. 들어온 자리다.
            AddFieldRect(root.transform, "DoorFrame", new Vector2(11.6f, -0.5f), new Vector2(2.3f, 5.6f), trim, -7);
            AddFieldRect(root.transform, "Door", new Vector2(11.6f, -0.55f), new Vector2(2.0f, 5.4f), new Color(0.27f, 0.26f, 0.27f), -6);
            AddFieldCircle(root.transform, "DoorKnob", new Vector2(10.9f, -0.9f), 0.2f, new Color(0.6f, 0.58f, 0.52f), -5);
            AddFieldRect(root.transform, "ShoeRack", new Vector2(9.4f, -2.85f), new Vector2(1.4f, 0.9f), woodDark, -6);
            AddFieldRect(root.transform, "TrashBag", new Vector2(5.2f, -3.0f), new Vector2(0.9f, 0.75f), new Color(0.12f, 0.12f, 0.13f), -4);

            // 천장 형광등은 꺼져 있다. 모니터와 휴대폰 불빛이 전부다. 방 전체를 어둡게 덮는다.
            AddFieldRect(root.transform, "Dim", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.02f, 0.03f, 0.08f, 0.32f), -2);
            AddFieldRect(root.transform, "Fluorescent", new Vector2(-1.0f, 3.45f), new Vector2(2.6f, 0.14f), new Color(0.40f, 0.42f, 0.46f), -1);
            // 형광등이 혼자 켜지는 연출에서만 쓰는 빛. 처음에는 꺼져 있다.
            var tubeOn = new GameObject("FluorescentOn");
            tubeOn.transform.SetParent(root.transform, false);
            AddFieldRect(tubeOn.transform, "Tube", new Vector2(-1.0f, 3.45f), new Vector2(2.6f, 0.14f), new Color(0.92f, 0.96f, 1f), 0);
            AddFieldSoft(tubeOn.transform, "Pool", new Vector2(-1.0f, 0.4f), new Vector2(16f, 8f), new Color(0.85f, 0.92f, 1f, 0.16f), 0);
            tubeOn.SetActive(false);

            // --- 조사 지점
            var deskPoint = BuildPoint(root.transform, "InvestigationPoint_Desk", new Vector2(1.6f, -1.4f), new Vector2(4.6f, 3.6f),
                new Color(0.55f, 0.66f, 0.85f), "field.test.desk", "field.test.desk.result", null);
            var phonePoint = BuildPoint(root.transform, "InvestigationPoint_Phone", new Vector2(-5.2f, -2.05f), new Vector2(1.6f, 1.2f),
                new Color(0.35f, 0.60f, 0.72f), "field.test.phone", "field.test.phone.result", null);
            var wallPoint = BuildPoint(root.transform, "InvestigationPoint_Wall", new Vector2(7.5f, 0.6f), new Vector2(3.8f, 2.4f),
                new Color(0.62f, 0.30f, 0.34f), "field.test.wall", "field.test.wall.result", null);
            var exitPoint = BuildPoint(root.transform, "InvestigationPoint_PostExit", new Vector2(11.6f, -0.5f), new Vector2(2.2f, 5.0f),
                new Color(0.5f, 0.5f, 0.5f), "field.post.exit", "field.post.exit", null);
            var windowPoint = BuildPoint(root.transform, "InvestigationPoint_PostWindow", new Vector2(-10.4f, 1.0f), new Vector2(4.0f, 3.2f),
                new Color(0.4f, 0.5f, 0.7f), "field.post.window", "field.post.window", null);
            SetHighlightTargets(deskPoint, root.transform, "Monitor/Bezel", "Monitor/Screen", "DeskTop", "Keyboard");
            SetHighlightTargets(phonePoint, root.transform, "Phone/Body", "Phone/Screen");
            SetHighlightTargets(exitPoint, root.transform, "Door");
            SetHighlightTargets(windowPoint, root.transform, "Window/Frame");

            // 현관으로 들어와 왼쪽을 보고 선다.
            BuildFieldActors(root.transform, -3.86f, 9.4f, 2.4f, -11f, 10.4f, faceLeft: true);

            // --- 무서운 연출
            // 창 쪽으로 가면 블라인드가 덜컹이고 등 뒤에서 현관문이 쾅 닫힌다. 창이 조금 열려 있었다.
            var slats = window.transform;
            AddScare(root.transform, "post_window", FieldScare.TriggerKind.ReachX, -7.6f, null, 1f,
                SSound("air_rush_pass", 0.35f), SShake(slats.gameObject, 0.6f, 0.06f), SWait(0.3f), SSound("door_slam_1", 0.7f), SWait(0.5f),
                SLines("field.post.scare.slam.001", "!field.post.scare.slam.002", "field.post.scare.slam.003"));
            // 벽 낙서를 보고 오면 꺼져 있던 형광등이 혼자 깜빡이며 켜졌다 꺼진다.
            var knob = root.transform.Find("DoorKnob").gameObject;
            AddScare(root.transform, "post_knob", FieldScare.TriggerKind.AfterPoint, 0f, "point_wall", 0f,
                SWait(0.6f), SSound("door_bang_2", 0.3f, 1.4f), SShake(knob, 0.6f, 0.04f), SWait(0.3f), SSound("door_bang_2", 0.3f, 1.5f), SShake(knob, 0.4f, 0.04f), SWait(0.6f),
                SLines("field.post.scare.knob.001", "!field.post.scare.knob.002"));

            points = new[] { deskPoint, phonePoint, wallPoint, exitPoint, windowPoint };
            return root;
        }

        // =========================================================== 사건 #002 엘리베이터

        /// <summary>
        /// 엘리베이터 현장을 짓는다. 홀(타기 전)과 엘리베이터 안(탄 뒤) 두 묶음을 FieldSceneSwap 으로 갈아 끼운다.
        /// 돌려주는 것: 타기 지점, 조작판 지점, 거울 지점, 엘리베이터(ElevatorCar).
        /// </summary>
        private static void BuildElevatorField(Transform parent, out GameObject boardPoint, out GameObject panelPoint,
            out GameObject mirrorPoint, out GameObject bulletinPoint, out ElevatorCar car)
        {
            var hall = BuildElevatorHall(parent, out boardPoint, out bulletinPoint);
            var inside = BuildElevatorInside(parent, out panelPoint, out mirrorPoint, out car);

            var swap = parent.gameObject.AddComponent<FieldSceneSwap>();
            var so = new SerializedObject(swap);
            so.Update();
            SetObjectArray(so.FindProperty("_beforeRoots"), hall);
            SetObjectArray(so.FindProperty("_afterRoots"), inside);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static readonly Color Steel = new Color(0.52f, 0.55f, 0.59f);
        private static readonly Color SteelDark = new Color(0.34f, 0.36f, 0.40f);
        private static readonly Color SteelLight = new Color(0.68f, 0.71f, 0.75f);

        private static GameObject BuildElevatorHall(Transform parent, out GameObject boardPoint, out GameObject bulletinPoint)
        {
            var root = new GameObject("ElevatorHall");
            root.transform.SetParent(parent, false);

            var wall = new Color(0.30f, 0.31f, 0.33f);
            var shadow = new Color(0f, 0f, 0f, 0.4f);

            // 구도시 구오 빌라 1층 현관 홀. 누렇게 바랜 잔 타일 벽, 검은 줄눈의 바닥 타일, 천장 등 두 개.
            // 왼쪽 끝은 비 오는 공동 현관 유리문과 우편함, 엘리베이터 위에 건물 이름판, 오른쪽에 관리실 게시판.
            var tile = new Color(0.47f, 0.44f, 0.37f);
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), tile, -9);
            for (int i = 0; i <= 88; i++)
                AddFieldRect(root.transform, "TileV_" + i, new Vector2(-22f + i * 0.5f, 0.4f), new Vector2(0.025f, 7.2f), Color.Lerp(tile, Color.black, 0.18f), -8);
            for (int i = 0; i <= 14; i++)
                AddFieldRect(root.transform, "TileH_" + i, new Vector2(0f, -3.2f + i * 0.5f), new Vector2(44f, 0.025f), Color.Lerp(tile, Color.black, 0.18f), -8);
            AddFieldRect(root.transform, "WainBand", new Vector2(0f, -1.6f), new Vector2(44f, 0.18f), new Color(0.32f, 0.26f, 0.20f), -8);
            foreach (var p in new[] { new Vector2(-4.6f, 2.2f), new Vector2(9.8f, 1.4f), new Vector2(-8.4f, -1.9f) })
                AddFieldSoft(root.transform, "Stain", p, new Vector2(2.0f, 1.4f), new Color(0.2f, 0.18f, 0.12f, 0.3f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.75f), new Vector2(44f, 0.5f), new Color(0.2f, 0.2f, 0.2f), -8);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), new Color(0.24f, 0.22f, 0.21f), -8);
            for (int i = 0; i <= 29; i++)
                AddFieldRect(root.transform, "Tile_" + i, new Vector2(-21.75f + i * 1.5f, -4.5f), new Vector2(0.04f, 2.2f), new Color(0.12f, 0.12f, 0.12f), -7);
            AddFieldRect(root.transform, "TileRow", new Vector2(0f, -4.3f), new Vector2(44f, 0.04f), new Color(0.12f, 0.12f, 0.12f), -7);
            AddFieldGradient(root.transform, "FloorSheen", new Vector2(0f, -3.7f), new Vector2(44f, 0.8f), new Color(1f, 1f, 1f, 0.05f), -7);
            AddFieldRect(root.transform, "Baseboard", new Vector2(0f, -3.3f), new Vector2(44f, 0.18f), new Color(0.16f, 0.15f, 0.14f), -7);

            // 천장 등. 왼쪽은 밝고 오른쪽은 누렇게 약하다.
            var lamps = new GameObject("Lamps");
            lamps.transform.SetParent(root.transform, false);
            foreach (var x in new[] { -6f, 4.5f })
            {
                AddFieldRect(lamps.transform, "Light_" + x, new Vector2(x, 3.42f), new Vector2(1.8f, 0.16f), new Color(0.95f, 0.93f, 0.85f), -6);
                AddFieldSoft(lamps.transform, "LightPool_" + x, new Vector2(x, 0.8f), new Vector2(7.5f, 6.4f), new Color(1f, 0.92f, 0.75f, x < 0f ? 0.13f : 0.08f), -6);
            }

            // 엘리베이터 두 대. 왼쪽(타는 쪽)은 1층에 서 있다. 오른쪽은 '점검 중' 안내가 붙어 있다.
            float[] carX = { -1.6f, 3.4f };
            for (int c = 0; c < carX.Length; c++)
            {
                float x = carX[c];
                AddFieldRect(root.transform, "Frame_" + c, new Vector2(x, -0.55f), new Vector2(3.4f, 5.6f), SteelDark, -7);
                // 문 뒤의 텅 빈 칸. 오른쪽 문이 저절로 열리는 연출에서만 보인다.
                if (c == 1)
                {
                    var empty = new GameObject("EmptyCar_1");
                    empty.transform.SetParent(root.transform, false);
                    AddFieldRect(empty.transform, "Dark", new Vector2(x, -0.6f), new Vector2(3.1f, 5.3f), new Color(0.05f, 0.05f, 0.06f), -6);
                    AddFieldSoft(empty.transform, "Dim", new Vector2(x, 1.6f), new Vector2(2.8f, 1.8f), new Color(0.7f, 0.75f, 0.8f, 0.12f), -6);
                    AddFieldRect(empty.transform, "Rail", new Vector2(x, -1.6f), new Vector2(3.0f, 0.08f), new Color(0.35f, 0.36f, 0.38f), -6);
                }
                var leafL = new GameObject("DoorL_" + c);
                leafL.transform.SetParent(root.transform, false);
                leafL.transform.localPosition = new Vector3(x - 0.78f, -0.6f, 0f);
                AddFieldRect(leafL.transform, "Leaf", Vector2.zero, new Vector2(1.52f, 5.3f), Steel, -5);
                var leafR = new GameObject("DoorR_" + c);
                leafR.transform.SetParent(root.transform, false);
                leafR.transform.localPosition = new Vector3(x + 0.78f, -0.6f, 0f);
                AddFieldRect(leafR.transform, "Leaf", Vector2.zero, new Vector2(1.52f, 5.3f), Steel, -5);
                AddFieldRect(leafR.transform, "Seam", new Vector2(-0.76f, 0f), new Vector2(0.04f, 5.3f), SteelDark, -4);
                AddFieldGradient(root.transform, "DoorSheen_" + c, new Vector2(x, 0.8f), new Vector2(3.1f, 2.4f), new Color(1f, 1f, 1f, 0.06f), -4);
                // 문짝이 열리며 숨는 양옆 벽.
                AddFieldRect(root.transform, "JambL_" + c, new Vector2(x - 2.5f, -0.55f), new Vector2(1.6f, 5.6f), tile, -3);
                AddFieldRect(root.transform, "JambR_" + c, new Vector2(x + 2.5f, -0.55f), new Vector2(1.6f, 5.6f), tile, -3);
                AddFieldRect(root.transform, "Indicator_" + c, new Vector2(x, 2.75f), new Vector2(1.0f, 0.42f), new Color(0.05f, 0.05f, 0.06f), -6);
                var digit = AddFieldWorldText(root.transform, "Floor_" + c, new Vector2(x, 2.75f), new Vector2(0.9f, 0.4f),
                    c == 0 ? "field.elev.floor_1" : "field.elev.floor_3", new Color(0.95f, 0.30f, 0.22f), -5);
                digit.fontSizeMax = 3.4f;
            }
            // 호출 단추. 두 대 사이 벽에 하나.
            AddFieldRect(root.transform, "CallPanel", new Vector2(0.9f, -0.9f), new Vector2(0.36f, 0.8f), SteelDark, -2);
            AddFieldCircle(root.transform, "CallUp", new Vector2(0.9f, -0.72f), 0.18f, new Color(1f, 0.8f, 0.4f), -1);
            AddFieldCircle(root.transform, "CallDown", new Vector2(0.9f, -1.08f), 0.18f, SteelLight, -1);
            // 오른쪽 엘리베이터 문에 붙은 점검 안내. 홀에 서면 바로 보인다.
            var noticeGroup = new GameObject("Notice");
            noticeGroup.transform.SetParent(root.transform, false);
            AddFieldRect(noticeGroup.transform, "NoticeSheet", new Vector2(3.4f, -0.1f), new Vector2(1.5f, 1.1f), new Color(0.92f, 0.9f, 0.82f), -3);
            var notice = AddFieldWorldText(noticeGroup.transform, "NoticeText", new Vector2(3.4f, -0.1f), new Vector2(1.4f, 1.0f), "field.elev.notice", new Color(0.2f, 0.18f, 0.16f), -2);
            notice.textWrappingMode = TextWrappingModes.Normal;
            notice.fontSizeMax = 1.6f;
            // 엘리베이터 위의 건물 이름판.
            AddFieldRect(root.transform, "NamePlate", new Vector2(0.9f, 3.25f), new Vector2(4.4f, 0.5f), new Color(0.28f, 0.22f, 0.16f), -5);
            var plate = AddFieldWorldText(root.transform, "NamePlateText", new Vector2(0.9f, 3.25f), new Vector2(4.2f, 0.46f), "field.elev.building", new Color(0.86f, 0.78f, 0.55f), -4);
            plate.fontSizeMax = 2.6f;

            // 왼쪽 끝 공동 현관 유리문. 밖에 비가 온다.
            var entrance = new GameObject("Window");
            entrance.transform.SetParent(root.transform, false);
            entrance.transform.localPosition = new Vector3(-12.6f, -0.4f, 0f);
            AddFieldRect(entrance.transform, "Frame", Vector2.zero, new Vector2(3.6f, 5.8f), new Color(0.22f, 0.22f, 0.24f), -7);
            AddFieldRect(entrance.transform, "Glass", Vector2.zero, new Vector2(3.2f, 5.4f), new Color(0.06f, 0.08f, 0.13f), -6);
            AddFieldRect(entrance.transform, "Mullion", Vector2.zero, new Vector2(0.08f, 5.4f), new Color(0.22f, 0.22f, 0.24f), -4);
            AddFieldRect(entrance.transform, "Handle", new Vector2(0.3f, -0.4f), new Vector2(0.08f, 0.8f), new Color(0.6f, 0.6f, 0.58f), -4);
            AddFieldSoft(entrance.transform, "StreetLamp", new Vector2(-0.6f, 1.6f), new Vector2(1.6f, 1.6f), new Color(1f, 0.8f, 0.45f, 0.3f), -5);
            entrance.AddComponent<WindowRain>();
            // 우편함. 칸 열여섯, 몇 칸은 전단이 삐져나와 있다.
            var mail = new GameObject("Mailboxes");
            mail.transform.SetParent(root.transform, false);
            mail.transform.localPosition = new Vector3(-8.6f, 0.2f, 0f);
            AddFieldRect(mail.transform, "Back", Vector2.zero, new Vector2(3.0f, 2.2f), new Color(0.36f, 0.37f, 0.38f), -6);
            for (int r = 0; r < 4; r++)
                for (int k = 0; k < 4; k++)
                {
                    var box = new Vector2(-1.1f + k * 0.74f, 0.78f - r * 0.52f);
                    AddFieldRect(mail.transform, "Box", box, new Vector2(0.66f, 0.44f), new Color(0.50f, 0.51f, 0.52f), -5);
                    AddFieldRect(mail.transform, "Slot", box + new Vector2(0f, 0.08f), new Vector2(0.4f, 0.05f), new Color(0.15f, 0.15f, 0.16f), -4);
                    if ((r * 4 + k) % 5 == 2) AddFieldRect(mail.transform, "Flyer", box + new Vector2(0.05f, 0.16f), new Vector2(0.36f, 0.12f), new Color(0.88f, 0.84f, 0.7f), -3);
                }
            var flap = AddFieldRect(mail.transform, "LooseFlap", new Vector2(1.12f, -0.78f), new Vector2(0.66f, 0.44f), new Color(0.56f, 0.57f, 0.58f), -3);
            // 관리실 게시판. 코르크판에 안내문 몇 장. 가운데 한 장이 3층 정지 안내다.
            var board = new GameObject("Bulletin");
            board.transform.SetParent(root.transform, false);
            board.transform.localPosition = new Vector3(8.2f, 0.5f, 0f);
            AddFieldRect(board.transform, "Frame", Vector2.zero, new Vector2(2.8f, 2.0f), new Color(0.30f, 0.22f, 0.15f), -6);
            AddFieldRect(board.transform, "Cork", Vector2.zero, new Vector2(2.6f, 1.8f), new Color(0.55f, 0.42f, 0.28f), -5);
            AddFieldRect(board.transform, "Sheet_0", new Vector2(-0.7f, 0.25f), new Vector2(0.8f, 1.0f), new Color(0.92f, 0.91f, 0.86f), -4);
            AddFieldRect(board.transform, "Sheet_1", new Vector2(0.25f, 0.1f), new Vector2(0.9f, 1.2f), new Color(0.96f, 0.95f, 0.9f), -4);
            AddFieldRect(board.transform, "Sheet_2", new Vector2(0.95f, -0.35f), new Vector2(0.55f, 0.6f), new Color(0.95f, 0.85f, 0.5f), -4);
            AddFieldRect(board.transform, "Pin", new Vector2(0.25f, 0.62f), new Vector2(0.1f, 0.1f), new Color(0.8f, 0.2f, 0.2f), -3);

            // 오른쪽 끝 계단실 문과 비상구 표시, 소화기.
            AddFieldRect(root.transform, "StairDoor", new Vector2(11.6f, -0.6f), new Vector2(2.0f, 5.3f), new Color(0.24f, 0.26f, 0.25f), -6);
            AddFieldRect(root.transform, "StairDoorWin", new Vector2(11.6f, 0.9f), new Vector2(0.6f, 0.9f), new Color(0.05f, 0.06f, 0.08f), -5);
            AddFieldRect(root.transform, "ExitSign", new Vector2(11.6f, 2.6f), new Vector2(1.1f, 0.42f), new Color(0.15f, 0.65f, 0.35f), -6);
            AddFieldSoft(root.transform, "ExitGlow", new Vector2(11.6f, 2.6f), new Vector2(2.4f, 1.4f), new Color(0.2f, 0.9f, 0.45f, 0.18f), 13);
            AddFieldRect(root.transform, "Extinguisher", new Vector2(9.8f, -2.8f), new Vector2(0.4f, 0.9f), new Color(0.65f, 0.12f, 0.12f), -5);
            AddFieldGradient(root.transform, "CeilingShade", new Vector2(0f, 3.0f), new Vector2(44f, 1.2f), new Color(0f, 0f, 0f, 0.35f), -6);
            AddFieldRect(root.transform, "Night", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.02f, 0.03f, 0.07f, 0.25f), -2);

            BuildFieldActors(root.transform, -3.86f, -9.0f, 2.4f, -12f, 10.6f);

            boardPoint = BuildPoint(root.transform, "InvestigationPoint_ElevBoard", new Vector2(-1.6f, -0.6f), new Vector2(3.2f, 5.4f),
                new Color(0.6f, 0.66f, 0.74f), "field.elev.board", "field.elev.board", null);
            SetHighlightTargets(boardPoint, root.transform, "DoorL_0", "DoorR_0");
            bulletinPoint = BuildPoint(root.transform, "InvestigationPoint_ElevBulletin", new Vector2(8.2f, 0.5f), new Vector2(3.0f, 2.2f),
                new Color(0.7f, 0.6f, 0.45f), "field.elev.bulletin", "field.elev.bulletin", null);
            SetHighlightTargets(bulletinPoint, root.transform, "Bulletin");

            // --- 무서운 연출
            var dark = AddBlackout(root.transform, "Blackout", new Color(0f, 0f, 0.02f, 0.96f));
            var emptyCar = root.transform.Find("EmptyCar_1").gameObject;
            emptyCar.SetActive(false);
            var leafL1 = root.transform.Find("DoorL_1").gameObject;
            var leafR1 = root.transform.Find("DoorR_1").gameObject;
            root.transform.Find("Notice").SetParent(leafR1.transform, true);   // 안내문은 오른쪽 문짝에 붙어 함께 움직인다
            // 점검 중이라던 오른쪽 엘리베이터가 혼자 내려와 텅 빈 채 열렸다 닫힌다.
            AddScare(root.transform, "elev_empty", FieldScare.TriggerKind.ReachX, 1.4f, null, 1.5f,
                SSound("elevator_ding", 0.45f, 0.92f), SWait(0.5f), SShow(emptyCar), SSound("door_slide", 0.45f),
                SMoveAsync(leafL1, new Vector2(-1.5f, 0f), 1.0f), SMove(leafR1, new Vector2(1.5f, 0f), 1.0f), SWait(1.6f),
                SLines("field.elev.scare.empty.001", "!field.elev.scare.empty.002"),
                SSound("door_slide", 0.4f, 0.9f), SMoveAsync(leafL1, new Vector2(1.5f, 0f), 0.9f), SMove(leafR1, new Vector2(-1.5f, 0f), 0.9f), SHide(emptyCar));
            // 돌아 나오는 길, 우편함 뚜껑 하나가 혼자 덜컹인다.
            var mailScare = AddScare(root.transform, "elev_mail", FieldScare.TriggerKind.ReachX, -6.6f, null, 0f,
                SSound("door_bang_2", 0.3f, 1.4f), SShake(flap, 0.35f, 0.05f), SWait(0.4f), SSound("door_bang_2", 0.35f, 1.3f), SShake(flap, 0.3f, 0.05f), SSound("heartbeat", 0.35f), SWait(0.6f),
                SLines("field.elev.scare.mail.001", "!field.elev.scare.mail.002"));
            mailScare.RequireFlag = "field_event:scare_elev_empty";   // 빈 엘리베이터를 본 뒤, 돌아 나올 때
            return root;
        }

        private static GameObject BuildElevatorInside(Transform parent, out GameObject panelPoint, out GameObject mirrorPoint, out ElevatorCar car)
        {
            var root = new GameObject("ElevatorInside");
            root.transform.SetParent(parent, false);
            var shadow = new Color(0f, 0f, 0f, 0.45f);

            // 문 너머 3층 복도. 문이 열렸을 때만 보인다. 문짝보다 뒤에 그린다.
            var corridor = new GameObject("Corridor3F");
            corridor.transform.SetParent(root.transform, false);
            AddFieldRect(corridor.transform, "Dark", new Vector2(0f, -0.6f), new Vector2(3.1f, 5.3f), new Color(0.03f, 0.03f, 0.05f), -9);
            AddFieldRect(corridor.transform, "FarWall", new Vector2(0f, -0.2f), new Vector2(3.1f, 3.4f), new Color(0.10f, 0.10f, 0.12f), -9);
            AddFieldRect(corridor.transform, "FloorFar", new Vector2(0f, -2.75f), new Vector2(3.1f, 1.0f), new Color(0.08f, 0.08f, 0.09f), -9);
            AddFieldRect(corridor.transform, "Sign3F", new Vector2(-0.7f, 0.6f), new Vector2(0.8f, 0.5f), new Color(0.2f, 0.21f, 0.24f), -8);
            var sign = AddFieldWorldText(corridor.transform, "Sign3FText", new Vector2(-0.7f, 0.6f), new Vector2(0.8f, 0.5f), "field.elev.floor_3f", new Color(0.75f, 0.76f, 0.8f), -7);
            sign.fontSizeMax = 2.2f;
            var corridorLight = AddFieldSoft(corridor.transform, "Light", new Vector2(0.4f, 1.3f), new Vector2(2.6f, 1.6f), new Color(0.75f, 0.85f, 1f, 0.35f), -8);
            AddFieldRect(corridor.transform, "Tube", new Vector2(0.4f, 1.6f), new Vector2(1.0f, 0.06f), new Color(0.8f, 0.85f, 0.9f), -8);
            corridor.SetActive(false);

            // 엘리베이터 안. 문을 마주 보는 쪽에서 본다. 두 사람이 겨우 서는 좁은 칸이다(안쪽 폭 10).
            // 칸 바깥은 아무것도 없는 어둠이고, 천장은 낮고, 모서리마다 그늘이 져 갇힌 느낌이 든다.
            const float half = 5.0f;   // 칸 안쪽 반폭
            AddFieldRect(root.transform, "BackWall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.20f, 0.21f, 0.24f), -10);
            // 문 양옆 벽판 한 장씩. 헤어라인 스테인리스.
            foreach (float x in new[] { -3.95f, 3.95f })
            {
                string side = x < 0f ? "L" : "R";
                AddFieldRect(root.transform, "WallPanel_" + side, new Vector2(x, -0.3f), new Vector2(1.95f, 6.2f), Steel, -9);
                AddFieldGradient(root.transform, "WallSheen_" + side, new Vector2(x, 1.6f), new Vector2(1.95f, 2.6f), new Color(1f, 1f, 1f, 0.06f), -8);
                for (int s = 0; s < 5; s++)
                    AddFieldRect(root.transform, "Hairline_" + side + "_" + s, new Vector2(x - 0.8f + s * 0.4f, -0.3f), new Vector2(0.02f, 6.0f), SteelLight, -8);
            }
            AddFieldRect(root.transform, "CabinEdge_L", new Vector2(-half - 0.3f, -0.3f), new Vector2(0.6f, 6.6f), SteelDark, -9);
            AddFieldRect(root.transform, "CabinEdge_R", new Vector2(half + 0.3f, -0.3f), new Vector2(0.6f, 6.6f), SteelDark, -9);
            AddFieldRect(root.transform, "Outside_L", new Vector2(-half - 0.6f - 6.5f, 0.2f), new Vector2(13f, 10.8f), new Color(0.015f, 0.015f, 0.02f), -9);
            AddFieldRect(root.transform, "Outside_R", new Vector2(half + 0.6f + 6.5f, 0.2f), new Vector2(13f, 10.8f), new Color(0.015f, 0.015f, 0.02f), -9);
            // 낮은 천장. 등은 가운데 한 줄뿐이다.
            AddFieldRect(root.transform, "CeilingBlock", new Vector2(0f, 3.75f), new Vector2(2f * half + 1.2f, 1.3f), new Color(0.10f, 0.10f, 0.12f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.05f), new Vector2(2f * half, 0.35f), new Color(0.62f, 0.64f, 0.66f), -8);
            AddFieldRect(root.transform, "CeilingLamp", new Vector2(0f, 3.02f), new Vector2(3.6f, 0.16f), new Color(0.95f, 0.97f, 1f), -7);
            var cabinLight = AddFieldSoft(root.transform, "CabinLight", new Vector2(0f, 1.9f), new Vector2(8f, 3.2f), new Color(0.95f, 0.97f, 1f, 0.20f), -7);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.3f), new Vector2(2f * half, 1.8f), new Color(0.18f, 0.18f, 0.19f), -8);
            for (int i = 0; i < 6; i++)
                AddFieldRect(root.transform, "FloorRib_" + i, new Vector2(-4.5f + i * 1.8f, -4.3f), new Vector2(0.05f, 1.8f), new Color(0.13f, 0.13f, 0.14f), -7);
            // 손잡이는 문 앞에서 끊긴다.
            AddFieldRect(root.transform, "Handrail_L", new Vector2(-3.95f, -1.55f), new Vector2(1.9f, 0.12f), SteelLight, -6);
            AddFieldRect(root.transform, "Handrail_R", new Vector2(3.95f, -1.55f), new Vector2(1.9f, 0.12f), SteelLight, -6);
            AddFieldGradient(root.transform, "FloorContact", new Vector2(0f, -3.2f), new Vector2(2f * half, 0.6f), new Color(0f, 0f, 0f, 0.4f), -7, 180f);

            // 가운데 문. 두 짝이 양옆으로 열린다. 위에 층 표시.
            AddFieldRect(root.transform, "DoorFrame", new Vector2(0f, -0.55f), new Vector2(3.5f, 5.6f), SteelDark, -9);
            var leftDoor = new GameObject("DoorL");
            leftDoor.transform.SetParent(root.transform, false);
            leftDoor.transform.localPosition = new Vector3(-0.78f, -0.6f, 0f);
            AddFieldRect(leftDoor.transform, "Leaf", Vector2.zero, new Vector2(1.55f, 5.3f), Steel, -7);
            AddFieldGradient(leftDoor.transform, "Sheen", new Vector2(0f, 1.4f), new Vector2(1.55f, 2.4f), new Color(1f, 1f, 1f, 0.08f), -6);
            var rightDoor = new GameObject("DoorR");
            rightDoor.transform.SetParent(root.transform, false);
            rightDoor.transform.localPosition = new Vector3(0.78f, -0.6f, 0f);
            AddFieldRect(rightDoor.transform, "Leaf", Vector2.zero, new Vector2(1.55f, 5.3f), Steel, -7);
            AddFieldGradient(rightDoor.transform, "Sheen", new Vector2(0f, 1.4f), new Vector2(1.55f, 2.4f), new Color(1f, 1f, 1f, 0.08f), -6);
            AddFieldRect(rightDoor.transform, "Edge", new Vector2(-0.77f, 0f), new Vector2(0.04f, 5.3f), SteelDark, -6);
            // 문 양옆 벽 기둥(문짝이 열리며 그 뒤로 들어간다).
            AddFieldRect(root.transform, "Jamb_L", new Vector2(-2.35f, -0.55f), new Vector2(1.2f, 5.6f), Steel, -5);
            AddFieldRect(root.transform, "Jamb_R", new Vector2(2.35f, -0.55f), new Vector2(1.2f, 5.6f), Steel, -5);
            AddFieldRect(root.transform, "Lintel", new Vector2(0f, 2.45f), new Vector2(5.9f, 0.7f), SteelDark, -5);
            AddFieldRect(root.transform, "IndicatorBox", new Vector2(0f, 2.45f), new Vector2(1.5f, 0.55f), new Color(0.04f, 0.04f, 0.05f), -4);
            var floorText = AddFieldWorldText(root.transform, "IndicatorFloor", new Vector2(0.2f, 2.45f), new Vector2(0.8f, 0.5f), "field.elev.floor_1", new Color(0.98f, 0.32f, 0.22f), -3);
            floorText.fontSizeMax = 4.2f;
            var up = AddFieldWorldText(root.transform, "IndicatorUp", new Vector2(-0.45f, 2.45f), new Vector2(0.4f, 0.5f), "field.elev.up", new Color(0.98f, 0.32f, 0.22f), -3);
            up.fontSizeMax = 3f;

            // 오른쪽 기둥의 조작판. 층 버튼 두 줄, 문 열림·닫힘, 비상 호출.
            AddFieldRect(root.transform, "PanelPlate", new Vector2(2.35f, -0.6f), new Vector2(0.9f, 2.4f), SteelLight, -4);
            for (int i = 0; i < 10; i++)
            {
                int col = i % 2, row = i / 2;
                bool three = i == 2;
                AddFieldCircle(root.transform, "Btn_" + (i + 1), new Vector2(2.17f + col * 0.36f, 0.25f - row * 0.32f), 0.2f,
                    three ? new Color(1f, 0.82f, 0.45f) : new Color(0.3f, 0.32f, 0.36f), -3);
            }
            AddFieldRect(root.transform, "PanelCover", new Vector2(2.35f, -1.5f), new Vector2(0.7f, 0.4f), Steel, -3);

            // 왼쪽 벽판의 거울. 그 아래 귀퉁이에 종이 한 장.
            AddFieldRect(root.transform, "MirrorFrame", new Vector2(-3.95f, 0.4f), new Vector2(1.7f, 3.2f), SteelDark, -4);
            AddFieldRect(root.transform, "Mirror", new Vector2(-3.95f, 0.4f), new Vector2(1.55f, 3.05f), new Color(0.40f, 0.46f, 0.52f), -3);
            AddFieldGradient(root.transform, "MirrorSheen", new Vector2(-3.95f, 1.2f), new Vector2(1.55f, 1.6f), new Color(1f, 1f, 1f, 0.14f), -2);
            var memo = AddFieldRect(root.transform, "Memo", new Vector2(-3.45f, -1.3f), new Vector2(0.5f, 0.36f), new Color(0.92f, 0.88f, 0.62f), -2);
            memo.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            // 천장 모서리 감시 카메라.
            AddFieldCircle(root.transform, "Dome", new Vector2(4.4f, 2.6f), 0.42f, new Color(0.15f, 0.15f, 0.17f), -4);
            AddFieldCircle(root.transform, "DomeLed", new Vector2(4.45f, 2.55f), 0.07f, new Color(0.9f, 0.2f, 0.15f), -3);
            AddFieldSoft(root.transform, "Shadow_Corner", new Vector2(0f, -3.3f), new Vector2(9.5f, 0.5f), shadow, -6);
            // 양옆 벽과 천장 쪽으로 짙어지는 그늘. 사람보다 앞에 그려 칸이 조여 오게 한다.
            AddFieldGradient(root.transform, "Close_L", new Vector2(-half + 0.9f, -0.3f), new Vector2(1.8f, 6.6f), new Color(0f, 0f, 0f, 0.55f), 15, 90f);
            AddFieldGradient(root.transform, "Close_R", new Vector2(half - 0.9f, -0.3f), new Vector2(1.8f, 6.6f), new Color(0f, 0f, 0f, 0.55f), 15, -90f);
            AddFieldGradient(root.transform, "Close_Top", new Vector2(0f, 2.3f), new Vector2(2f * half, 1.6f), new Color(0f, 0f, 0f, 0.35f), 15);

            BuildFieldActors(root.transform, -3.86f, -0.4f, 1.9f, -3.6f, 3.6f);

            car = root.AddComponent<ElevatorCar>();
            var cso = new SerializedObject(car);
            cso.Update();
            cso.FindProperty("_leftDoor").objectReferenceValue = leftDoor.transform;
            cso.FindProperty("_rightDoor").objectReferenceValue = rightDoor.transform;
            cso.FindProperty("_floorText").objectReferenceValue = floorText;
            cso.FindProperty("_upArrow").objectReferenceValue = up.gameObject;
            cso.FindProperty("_corridor").objectReferenceValue = corridor;
            cso.FindProperty("_corridorLight").objectReferenceValue = corridorLight.GetComponent<SpriteRenderer>();
            cso.FindProperty("_cabinLight").objectReferenceValue = cabinLight.GetComponent<SpriteRenderer>();
            cso.ApplyModifiedPropertiesWithoutUndo();
            // 층 표시는 진행 담당이 숫자로 바꾼다. 번역 글이 덮어쓰지 않게 뗀다.
            Object.DestroyImmediate(floorText.GetComponent<LocalizedText>());
            floorText.text = "1";

            panelPoint = BuildPoint(root.transform, "InvestigationPoint_Panel", new Vector2(2.35f, -0.6f), new Vector2(1.2f, 2.8f),
                new Color(0.75f, 0.78f, 0.82f), "field.test.panel", "field.test.panel.result", null);
            mirrorPoint = BuildPoint(root.transform, "InvestigationPoint_Mirror", new Vector2(-3.95f, 0.2f), new Vector2(1.9f, 3.6f),
                new Color(0.55f, 0.65f, 0.75f), "field.test.mirror", "field.test.mirror.result", null);
            SetHighlightTargets(panelPoint, root.transform, "PanelPlate", "PanelCover");
            SetHighlightTargets(mirrorPoint, root.transform, "MirrorFrame", "Mirror");

            // --- 무서운 연출
            // 3층 연출 뒤: 칸의 불이 나가고 붉은 비상등만 켜진다. 표시등은 3 에서 깜빡인다. 몇 초 뒤 불이 돌아온다.
            var dark = AddBlackout(root.transform, "Blackout", new Color(0f, 0f, 0.02f, 0.97f));
            var red = AddFieldSoft(root.transform, "EmergencyRed", new Vector2(0f, 2.6f), new Vector2(12f, 7f), new Color(0.9f, 0.08f, 0.06f, 0.3f), 13);
            red.SetActive(false);
            var redLamp = AddFieldRect(root.transform, "EmergencyLamp", new Vector2(-3.2f, 2.95f), new Vector2(0.5f, 0.2f), new Color(1f, 0.2f, 0.15f), 14);
            redLamp.SetActive(false);
            var blackoutScare = AddScare(root.transform, "elev_blackout", FieldScare.TriggerKind.OnShow, 0f, null, 0f,
                SWait(2.5f), SSound("power_down", 0.5f), SFadeIn(dark, 0.12f), SWait(1.0f), SSound("lamp_flicker", 0.3f, 0.7f), SShow(red), SShow(redLamp), SWait(0.8f),
                SLines("field.elev.scare.blackout.001", "!field.elev.scare.blackout.002", "field.elev.scare.blackout.003"),
                SSound("light_switch", 0.4f), SHide(red), SHide(redLamp), SFadeOut(dark, 0.2f));
            blackoutScare.RequireFlag = "field_event:elev_3f";
            // 거울을 보고 오면, 거울 속 두 사람 뒤에 하나가 더 비쳤다가 사라진다.
            var third = new GameObject("MirrorThird");
            third.transform.SetParent(root.transform, false);
            AddFieldCircle(third.transform, "Head", new Vector2(-3.55f, 1.25f), 0.42f, new Color(0.08f, 0.09f, 0.11f, 0.85f), -2);
            AddFieldRect(third.transform, "Body", new Vector2(-3.55f, 0.15f), new Vector2(0.62f, 1.7f), new Color(0.08f, 0.09f, 0.11f, 0.85f), -2);
            third.SetActive(false);
            AddScare(root.transform, "elev_mirror", FieldScare.TriggerKind.AfterPoint, 0f, "point_mirror", 0f,
                SWait(0.5f), SFadeIn(third, 0.08f), SSound("ghost_sting", 0.35f), SWait(0.35f), SFadeOut(third, 0.12f), SSound("heartbeat", 0.4f), SWait(0.5f),
                SLines("field.elev.scare.mirror.001", "!field.elev.scare.mirror.002", "field.elev.scare.mirror.003"));
            return root;
        }

        // =========================================================== 가까이 보기 화면

        /// <summary>가까이 보기 화면의 바탕을 만든다. 위 2/3 에 물건, 아래에 띠(FinishCloseup 이 붙인다).</summary>
        private static CloseupScreen NewCloseup(string name, Color backColor, out GameObject go)
        {
            go = CreatePanel(null, name, backColor);
            StretchFull(go);
            var screen = go.AddComponent<CloseupScreen>();
            ConfigureScreen(screen, name, UILayer.Screen, true, true);
            return screen;
        }

        private static GameObject CloseupGroup(Transform parent, string name, Vector2 position)
        {
            var g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            var rt = (RectTransform)g.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = position;
            return g;
        }

        /// <summary>가까이 보기 화면 안의 글. 번역 표를 따른다.</summary>
        private static TextMeshProUGUI CloseupLabel(Transform parent, string name, string textId, float size, UIFontWeight weight,
            Color color, Vector2 position, Vector2 box, TextAlignmentOptions align)
        {
            var text = AddText(parent, name, size, weight, color, position, box, align);
            var localized = text.gameObject.AddComponent<LocalizedText>();
            var so = new SerializedObject(localized);
            so.Update();
            so.FindProperty("_textId").stringValue = textId;
            so.ApplyModifiedPropertiesWithoutUndo();
            return text;
        }

        /// <summary>
        /// 원룸 책상. 모니터에 괴담넷의 그 글이 떠 있다. 글(작성자가 다른 두 판)은 진행 담당이 갈아 끼운다(PostA → PostB).
        /// 살필 곳: 모니터의 글 / 키보드 밑 출력물(단서) / 모니터 테두리의 메모.
        /// </summary>
        private static CloseupScreen BuildPostDeskCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.05f, 0.06f, 0.09f, 1f), out var go);
            CloseupRect(go.transform, "Wall", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.11f, 0.13f, 0.18f));
            var glow = CloseupRect(go.transform, "Glow", new Vector2(0f, 230f), new Vector2(1500f, 900f), new Color(0.35f, 0.55f, 1f, 0.18f));
            glow.sprite = FieldSoftSprite(); glow.type = Image.Type.Simple;
            CloseupRect(go.transform, "Desk", new Vector2(0f, -190f), new Vector2(1920f, 220f), new Color(0.27f, 0.22f, 0.18f));
            CloseupRect(go.transform, "DeskEdge", new Vector2(0f, -86f), new Vector2(1920f, 14f), new Color(0.34f, 0.28f, 0.23f));

            var monitor = CloseupGroup(go.transform, "Monitor", new Vector2(0f, 222f));
            CloseupRect(monitor.transform, "Bezel", Vector2.zero, new Vector2(1060f, 600f), new Color(0.04f, 0.04f, 0.05f));
            CloseupRect(monitor.transform, "Screen", new Vector2(0f, 10f), new Vector2(1010f, 540f), new Color(0.94f, 0.95f, 0.97f));
            CloseupRect(monitor.transform, "SiteBar", new Vector2(0f, 248f), new Vector2(1010f, 44f), new Color(0.20f, 0.25f, 0.36f));
            CloseupLabel(monitor.transform, "Site", "field.post.monitor_site", 24f, UIFontWeight.Bold, Color.white, new Vector2(-330f, 248f), new Vector2(320f, 40f), TextAlignmentOptions.Left);
            CloseupRect(monitor.transform, "Stand", new Vector2(0f, -340f), new Vector2(80f, 80f), new Color(0.06f, 0.06f, 0.08f));

            // 글 두 판. 처음엔 A, 새로고침되면 B. 제목과 본문은 같고 작성자와 시각만 다르다.
            foreach (var key in new[] { "A", "B" })
            {
                var post = CloseupGroup(monitor.transform, "Post" + key, Vector2.zero);
                CloseupLabel(post.transform, "Title", "web.test.001.title", 40f, UIFontWeight.Bold, new Color(0.1f, 0.1f, 0.12f), new Vector2(-40f, 170f), new Vector2(880f, 56f), TextAlignmentOptions.Left);
                CloseupLabel(post.transform, "Meta", "field.post.monitor_meta_" + key.ToLowerInvariant(), 22f, UIFontWeight.Medium,
                    key == "B" ? new Color(0.75f, 0.2f, 0.2f) : new Color(0.4f, 0.42f, 0.46f), new Vector2(-40f, 122f), new Vector2(880f, 34f), TextAlignmentOptions.Left);
                CloseupRect(post.transform, "Rule", new Vector2(0f, 96f), new Vector2(900f, 2f), new Color(0.8f, 0.81f, 0.84f));
                CloseupLabel(post.transform, "Body", "web.test.001.body", 28f, UIFontWeight.Regular, new Color(0.15f, 0.15f, 0.18f), new Vector2(-40f, 20f), new Vector2(880f, 120f), TextAlignmentOptions.TopLeft);
                CloseupLabel(post.transform, "Stats", "field.post.monitor_stats_" + key.ToLowerInvariant(), 20f, UIFontWeight.Medium, new Color(0.45f, 0.47f, 0.52f),
                    new Vector2(-40f, -170f), new Vector2(880f, 30f), TextAlignmentOptions.Left);
                if (key == "B") post.SetActive(false);
            }
            // 새로고침 순간에 잠깐 덮는 하얀 판과 도는 표시.
            var refresh = CloseupGroup(monitor.transform, "Refresh", Vector2.zero);
            CloseupRect(refresh.transform, "Blank", new Vector2(0f, -12f), new Vector2(1010f, 496f), new Color(0.97f, 0.97f, 0.98f));
            CloseupLabel(refresh.transform, "Loading", "field.post.monitor_loading", 26f, UIFontWeight.Medium, new Color(0.5f, 0.52f, 0.56f), Vector2.zero, new Vector2(600f, 40f), TextAlignmentOptions.Center);
            refresh.SetActive(false);
            var postHit = CloseupHotspot(monitor.transform, "Hotspot_Post", new Vector2(0f, 60f), new Vector2(980f, 360f));
            AttachGlow(postHit, monitor.transform.Find("Bezel").gameObject);

            // 모니터 테두리 메모.
            var note = CloseupRect(monitor.transform, "StickyNote", new Vector2(470f, -230f), new Vector2(150f, 130f), new Color(0.98f, 0.88f, 0.42f));
            note.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 6f);
            CloseupLabel(note.transform, "Text", "field.post.sticky", 19f, UIFontWeight.SemiBold, new Color(0.25f, 0.2f, 0.1f), Vector2.zero, new Vector2(130f, 110f), TextAlignmentOptions.Center);
            var noteHit = CloseupHotspot(monitor.transform, "Hotspot_Note", new Vector2(470f, -230f), new Vector2(170f, 150f));
            AttachGlow(noteHit, note.gameObject);

            // 키보드와, 키보드 받침 밑으로 삐져나온 출력물 세 장.
            CloseupRect(go.transform, "Keyboard", new Vector2(-60f, -150f), new Vector2(760f, 80f), new Color(0.10f, 0.10f, 0.12f));
            for (int r = 0; r < 3; r++)
                for (int k = 0; k < 14; k++)
                    CloseupRect(go.transform, "Key", new Vector2(-385f + k * 50f + r * 10f, -128f - r * 22f), new Vector2(40f, 16f), new Color(0.18f, 0.18f, 0.21f));
            var prints = CloseupGroup(go.transform, "Printouts", new Vector2(520f, -210f));
            for (int i = 0; i < 3; i++)
            {
                var sheet = CloseupRect(prints.transform, "Sheet_" + i, new Vector2(i * 26f, i * -6f), new Vector2(300f, 120f), new Color(0.93f, 0.93f, 0.9f));
                sheet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f + i * 7f);
                for (int l = 0; l < 4; l++)
                    CloseupRect(sheet.transform, "Line", new Vector2(-20f, 30f - l * 18f), new Vector2(220f - l * 30f, 5f), new Color(0.55f, 0.56f, 0.6f));
            }
            var printHit = CloseupHotspot(go.transform, "Hotspot_Prints", new Vector2(545f, -215f), new Vector2(380f, 170f));
            AttachGlow(printHit, prints);

            // 키보드 왼쪽의 손바닥만 한 수첩. 펼친 쪽에 날짜와 시각, 계정 이름이 빽빽하다(지운 기록).
            var book = CloseupGroup(go.transform, "Notebook", new Vector2(-700f, -175f));
            var cover = CloseupRect(book.transform, "Cover", Vector2.zero, new Vector2(300f, 150f), new Color(0.16f, 0.2f, 0.3f));
            cover.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
            var pageL = CloseupRect(book.transform, "PageL", new Vector2(-70f, 4f), new Vector2(136f, 136f), new Color(0.94f, 0.93f, 0.88f));
            var pageR = CloseupRect(book.transform, "PageR", new Vector2(70f, 4f), new Vector2(136f, 136f), new Color(0.94f, 0.93f, 0.88f));
            foreach (var page in new[] { pageL, pageR })
                for (int l = 0; l < 6; l++)
                    CloseupRect(page.transform, "Row", new Vector2(-6f, 50f - l * 19f), new Vector2(100f - (l % 3) * 14f, 4f), l % 2 == 0 ? new Color(0.25f, 0.3f, 0.5f) : new Color(0.6f, 0.2f, 0.2f));
            var bookHit = CloseupHotspot(go.transform, "Hotspot_Notebook", new Vector2(-700f, -175f), new Vector2(320f, 170f));
            AttachGlow(bookHit, book);

            FinishCloseup(go, screen, "field.post.desk_hint",
                new[] { postHit, printHit, noteHit, bookHit },
                new[] { "field.post.desk_post", "field.post.desk_prints", "field.post.desk_note", "field.post.desk_book" },
                actionIds: new[] { null, "action_test_002", null, "action_test_011" });
            return screen;
        }

        /// <summary>침대 위 휴대폰. 잠금 화면에 공유 알림이 세 개. 넷째 알림은 진행 담당이 띄운다.</summary>
        /// <summary>
        /// 302호 문 앞에 쌓인 우편물. 철문 아랫단과 복도 바닥. 고지서, 전단 묶음, 우유 투입구.
        /// 고지서 날짜로 며칠째 아무도 드나들지 않았는지 안다. 투입구 안으로는 파란 모니터 불빛이 새어 나온다.
        /// </summary>
        private static CloseupScreen BuildPostPileCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.06f, 0.06f, 0.07f, 1f), out var go);
            CloseupRect(go.transform, "Door", new Vector2(-40f, 300f), new Vector2(1100f, 560f), new Color(0.30f, 0.25f, 0.22f));
            CloseupRect(go.transform, "DoorPanel", new Vector2(-40f, 320f), new Vector2(900f, 420f), new Color(0.22f, 0.18f, 0.16f));
            CloseupRect(go.transform, "Sill", new Vector2(-40f, 12f), new Vector2(1180f, 24f), new Color(0.18f, 0.18f, 0.19f));
            CloseupRect(go.transform, "Floor", new Vector2(0f, -170f), new Vector2(1920f, 340f), new Color(0.28f, 0.28f, 0.27f));
            for (int i = 0; i < 40; i++)
                CloseupRect(go.transform, "Grit", new Vector2(-940f + (i * 137) % 1880, -40f - (i * 53) % 290), new Vector2(8f, 5f), new Color(0.38f, 0.38f, 0.36f));
            var shade = CloseupRect(go.transform, "Shade", new Vector2(0f, 300f), new Vector2(1920f, 600f), new Color(0f, 0f, 0f, 0.25f));
            shade.sprite = FieldSoftSprite(); shade.type = Image.Type.Simple;

            // 우유 투입구. 덮개 틈으로 안쪽의 파란 빛이 새어 나온다(진행 담당이 살핀 뒤 깜빡이게 한다).
            var slot = CloseupGroup(go.transform, "MilkSlot", new Vector2(120f, 330f));
            CloseupRect(slot.transform, "Frame", Vector2.zero, new Vector2(360f, 90f), new Color(0.55f, 0.55f, 0.52f));
            CloseupRect(slot.transform, "Gap", new Vector2(0f, -8f), new Vector2(320f, 34f), new Color(0.03f, 0.03f, 0.05f));
            var leak = CloseupRect(slot.transform, "Leak", new Vector2(0f, -8f), new Vector2(300f, 18f), new Color(0.35f, 0.55f, 1f, 0.55f));
            leak.gameObject.name = "Leak";
            var slotHit = CloseupHotspot(go.transform, "Hotspot_Slot", new Vector2(120f, 330f), new Vector2(400f, 130f));
            AttachGlow(slotHit, slot);

            // 바닥의 전단 묶음. 같은 치킨집 전단이 날짜만 다르게 세 장.
            var flyers = CloseupGroup(go.transform, "Flyers", new Vector2(-470f, -120f));
            Color[] flyerCol = { new Color(0.92f, 0.86f, 0.55f), new Color(0.90f, 0.88f, 0.84f), new Color(0.86f, 0.5f, 0.42f) };
            for (int i = 0; i < 3; i++)
            {
                var f = CloseupRect(flyers.transform, "Flyer_" + i, new Vector2(i * 40f, i * 18f), new Vector2(330f, 200f), flyerCol[i]);
                f.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f + i * 9f);
                CloseupRect(f.transform, "Head", new Vector2(0f, 60f), new Vector2(260f, 36f), new Color(0.75f, 0.2f, 0.15f, 0.8f));
                for (int l = 0; l < 3; l++) CloseupRect(f.transform, "Line", new Vector2(0f, 6f - l * 30f), new Vector2(240f - l * 40f, 8f), new Color(0.4f, 0.4f, 0.4f));
            }
            var flyerHit = CloseupHotspot(go.transform, "Hotspot_Flyers", new Vector2(-430f, -100f), new Vector2(460f, 280f));
            AttachGlow(flyerHit, flyers);

            // 붉은 줄의 고지서 봉투. '미납 안내'. 소인 날짜가 사흘 전이다.
            var bill = CloseupGroup(go.transform, "Bill", new Vector2(360f, -150f));
            var env = CloseupRect(bill.transform, "Envelope", Vector2.zero, new Vector2(400f, 220f), new Color(0.93f, 0.92f, 0.88f));
            env.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 7f);
            CloseupRect(env.transform, "RedBand", new Vector2(0f, 70f), new Vector2(400f, 30f), new Color(0.75f, 0.18f, 0.16f));
            CloseupLabel(env.transform, "Text", "field.post.pile_bill_label", 26f, UIFontWeight.Bold, new Color(0.3f, 0.12f, 0.1f), new Vector2(0f, 0f), new Vector2(360f, 110f), TextAlignmentOptions.Center);
            var billHit = CloseupHotspot(go.transform, "Hotspot_Bill", new Vector2(360f, -150f), new Vector2(440f, 260f));
            AttachGlow(billHit, bill);

            FinishCloseup(go, screen, "field.post.pile_hint",
                new[] { billHit, flyerHit, slotHit },
                new[] { "field.post.pile_bill", "field.post.pile_flyers", "field.post.pile_slot" },
                actionIds: new[] { "action_test_012", null, null });
            return screen;
        }

        private static CloseupScreen BuildPostPhoneCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.06f, 0.07f, 0.10f, 1f), out var go);
            CloseupRect(go.transform, "Sheet", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.20f, 0.22f, 0.28f));
            for (int i = 0; i < 7; i++)
            {
                var fold = CloseupRect(go.transform, "Fold_" + i, new Vector2(-800f + i * 270f, 140f + (i % 2) * 60f), new Vector2(380f, 18f), new Color(0.16f, 0.18f, 0.23f));
                fold.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f + i * 5f);
            }
            var phone = CloseupGroup(go.transform, "Phone", new Vector2(0f, 150f));
            phone.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, -4f);
            var glow = CloseupRect(phone.transform, "Glow", Vector2.zero, new Vector2(900f, 1000f), new Color(0.5f, 0.7f, 1f, 0.18f));
            glow.sprite = FieldSoftSprite(); glow.type = Image.Type.Simple;
            CloseupRect(phone.transform, "Body", Vector2.zero, new Vector2(400f, 720f), new Color(0.04f, 0.04f, 0.05f));
            CloseupRect(phone.transform, "Screen", Vector2.zero, new Vector2(370f, 690f), new Color(0.14f, 0.16f, 0.26f));
            CloseupLabel(phone.transform, "Clock", "field.post.phone_clock", 72f, UIFontWeight.Medium, new Color(0.92f, 0.94f, 0.98f), new Vector2(0f, 230f), new Vector2(340f, 90f), TextAlignmentOptions.Center);
            CloseupLabel(phone.transform, "Date", "field.post.phone_date", 20f, UIFontWeight.Regular, new Color(0.75f, 0.78f, 0.86f), new Vector2(0f, 175f), new Vector2(340f, 30f), TextAlignmentOptions.Center);
            var cards = new List<GameObject>();
            for (int i = 0; i < 4; i++)
            {
                var card = CloseupGroup(phone.transform, "Notice_" + i, new Vector2(0f, 90f - i * 96f));
                CloseupRect(card.transform, "Card", Vector2.zero, new Vector2(340f, 84f), new Color(0.88f, 0.9f, 0.95f, 0.92f));
                CloseupRect(card.transform, "Icon", new Vector2(-140f, 10f), new Vector2(34f, 34f), new Color(0.75f, 0.2f, 0.22f));
                CloseupLabel(card.transform, "App", "field.post.phone_app", 16f, UIFontWeight.SemiBold, new Color(0.3f, 0.32f, 0.38f), new Vector2(10f, 22f), new Vector2(260f, 24f), TextAlignmentOptions.Left);
                CloseupLabel(card.transform, "Text", "field.post.phone_notice", 18f, UIFontWeight.Medium, new Color(0.1f, 0.1f, 0.14f), new Vector2(10f, -12f), new Vector2(260f, 44f), TextAlignmentOptions.Left);
                cards.Add(card);
                if (i == 3) card.SetActive(false);   // 넷째 알림은 살피는 사이에 들어온다
            }
            var noticeHit = CloseupHotspot(phone.transform, "Hotspot_Notices", new Vector2(0f, -50f), new Vector2(360f, 340f));
            AttachGlow(noticeHit, phone.transform.Find("Screen").gameObject);
            // 액정 귀퉁이의 금. 떨어뜨린 자국.
            CloseupMarks(phone.transform, "Crack", new Vector2(150f, 320f), new[] { new[] { -30f, 0f, 30f, 10f }, new[] { -10f, -10f, 20f, -30f }, new[] { 0f, 5f, 40f, 30f } }, 2f, new Color(0.8f, 0.85f, 0.95f, 0.6f));
            var crackHit = CloseupHotspot(phone.transform, "Hotspot_Crack", new Vector2(150f, 315f), new Vector2(130f, 90f));
            AttachGlow(crackHit, phone.transform.Find("Body").gameObject);

            FinishCloseup(go, screen, "field.post.phone_hint",
                new[] { noticeHit, crackHit },
                new[] { "field.post.phone_notices", "field.post.phone_crack" },
                actionIds: new[] { "action_test_008", null });
            return screen;
        }

        /// <summary>벽의 낙서. 같은 말이 세 번, 아래로 갈수록 번져 있다. 벽지가 들뜬 귀퉁이도 살필 수 있다.</summary>
        private static CloseupScreen BuildPostWallCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.10f, 0.11f, 0.14f, 1f), out var go);
            CloseupRect(go.transform, "Wallpaper", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.24f, 0.26f, 0.31f));
            for (int i = 0; i < 24; i++)
                CloseupRect(go.transform, "Grid_" + i, new Vector2(-920f + i * 80f, 120f), new Vector2(2f, 860f), new Color(0.27f, 0.29f, 0.34f));
            var light = CloseupRect(go.transform, "MonitorLight", new Vector2(-700f, 100f), new Vector2(1400f, 900f), new Color(0.35f, 0.55f, 1f, 0.12f));
            light.sprite = FieldSoftSprite(); light.type = Image.Type.Simple;

            var writing = CloseupGroup(go.transform, "Writing", new Vector2(-60f, 230f));
            var ink = new Color(0.62f, 0.14f, 0.16f);
            for (int i = 0; i < 3; i++)
            {
                var line = CloseupLabel(writing.transform, "Line_" + i, "field.post.graffiti_line", 76f - i * 4f, UIFontWeight.Bold,
                    new Color(ink.r, ink.g, ink.b, 0.95f - i * 0.2f), new Vector2(i * 40f, 150f - i * 150f), new Vector2(1100f, 110f), TextAlignmentOptions.Center);
                line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3f - i * 2.5f);
                line.characterSpacing = 6f;
            }
            CloseupMarks(writing.transform, "Drips", new Vector2(120f, -230f), new[] { new[] { 0f, 0f, 0f, -70f }, new[] { 60f, 10f, 60f, -40f }, new[] { -90f, 0f, -90f, -110f } }, 6f, new Color(ink.r, ink.g, ink.b, 0.5f));
            var writeHit = CloseupHotspot(writing.transform, "Hotspot_Writing", new Vector2(40f, 0f), new Vector2(1200f, 520f));
            AttachGlow(writeHit, writing);

            // 오른쪽 아래 들뜬 벽지. 그 밑으로 덮어 놓은 예전 낙서가 조금 보인다.
            var peel = CloseupGroup(go.transform, "Peel", new Vector2(720f, -150f));
            CloseupRect(peel.transform, "Under", new Vector2(0f, 0f), new Vector2(300f, 200f), new Color(0.32f, 0.30f, 0.28f));
            CloseupLabel(peel.transform, "Old", "field.post.graffiti_old", 26f, UIFontWeight.Bold, new Color(0.45f, 0.15f, 0.16f, 0.6f), new Vector2(0f, -10f), new Vector2(280f, 80f), TextAlignmentOptions.Center);
            var flap = CloseupRect(peel.transform, "Flap", new Vector2(-40f, 50f), new Vector2(260f, 150f), new Color(0.28f, 0.30f, 0.35f));
            flap.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var peelHit = CloseupHotspot(peel.transform, "Hotspot_Peel", Vector2.zero, new Vector2(330f, 230f));
            AttachGlow(peelHit, peel);

            FinishCloseup(go, screen, "field.post.wall_hint",
                new[] { writeHit, peelHit },
                new[] { "field.post.wall_writing", "field.post.wall_peel" },
                actionIds: new[] { "action_test_003", null });
            return screen;
        }

        /// <summary>엘리베이터 조작판. 3층 버튼만 닳아 반들거린다. 아래 덮개를 열면 점검 표시가 붙어 있다.</summary>
        /// <summary>
        /// 구오 빌라 1층 관리실 게시판. 코르크판에 안내문 셋과 손글씨 쪽지 하나.
        /// 가운데 관리실 안내가 3층 자동 정지(점검 설정)를 알린다. 쪽지는 주민이 붙인 항의다.
        /// </summary>
        private static CloseupScreen BuildElevBulletinCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.07f, 0.06f, 0.05f, 1f), out var go);
            CloseupRect(go.transform, "Wall", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.36f, 0.33f, 0.27f));
            for (int i = 0; i < 48; i++)
                CloseupRect(go.transform, "Grout", new Vector2(-940f + i * 40f, 120f), new Vector2(2f, 860f), new Color(0.3f, 0.27f, 0.22f));
            CloseupRect(go.transform, "Frame", new Vector2(0f, 150f), new Vector2(1500f, 760f), new Color(0.30f, 0.22f, 0.15f));
            CloseupRect(go.transform, "Cork", new Vector2(0f, 150f), new Vector2(1440f, 700f), new Color(0.58f, 0.44f, 0.29f));
            for (int i = 0; i < 60; i++)
                CloseupRect(go.transform, "Speck", new Vector2(-700f + (i * 233) % 1400, -180f + (i * 97) % 650), new Vector2(6f, 6f), new Color(0.48f, 0.36f, 0.23f));

            // 왼쪽: 정기 소독 안내. 오래돼 누렇다.
            var left = CloseupGroup(go.transform, "Disinfect", new Vector2(-470f, 200f));
            var lp = CloseupRect(left.transform, "Paper", Vector2.zero, new Vector2(340f, 440f), new Color(0.88f, 0.85f, 0.74f));
            lp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            CloseupLabel(lp.transform, "Text", "field.elev.bulletin_disinfect", 26f, UIFontWeight.SemiBold, new Color(0.25f, 0.22f, 0.18f), Vector2.zero, new Vector2(300f, 400f), TextAlignmentOptions.Center);
            CloseupRect(left.transform, "Pin", new Vector2(0f, 205f), new Vector2(18f, 18f), new Color(0.2f, 0.4f, 0.8f));
            var leftHit = CloseupHotspot(go.transform, "Hotspot_Disinfect", new Vector2(-470f, 200f), new Vector2(370f, 470f));
            AttachGlow(leftHit, left);

            // 가운데: 관리실 안내. 새 종이, 굵은 글씨.
            var main = CloseupGroup(go.transform, "Notice", new Vector2(20f, 170f));
            var mp = CloseupRect(main.transform, "Paper", Vector2.zero, new Vector2(460f, 560f), new Color(0.97f, 0.96f, 0.92f));
            CloseupRect(mp.transform, "Head", new Vector2(0f, 230f), new Vector2(460f, 60f), new Color(0.2f, 0.28f, 0.42f));
            CloseupLabel(mp.transform, "Title", "field.elev.bulletin_title", 28f, UIFontWeight.Bold, Color.white, new Vector2(0f, 230f), new Vector2(440f, 56f), TextAlignmentOptions.Center);
            CloseupLabel(mp.transform, "Text", "field.elev.bulletin_body", 25f, UIFontWeight.Medium, new Color(0.15f, 0.15f, 0.18f), new Vector2(0f, -30f), new Vector2(410f, 420f), TextAlignmentOptions.Top);
            CloseupRect(main.transform, "Pin", new Vector2(0f, 270f), new Vector2(20f, 20f), new Color(0.85f, 0.2f, 0.2f));
            var mainHit = CloseupHotspot(go.transform, "Hotspot_Notice", new Vector2(20f, 170f), new Vector2(490f, 590f));
            AttachGlow(mainHit, main);

            // 오른쪽: 주민이 붙인 손글씨 쪽지. 비뚤게 붙어 있다.
            var memo = CloseupGroup(go.transform, "Complaint", new Vector2(500f, 90f));
            var cp = CloseupRect(memo.transform, "Paper", Vector2.zero, new Vector2(320f, 260f), new Color(0.98f, 0.9f, 0.55f));
            cp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            CloseupLabel(cp.transform, "Text", "field.elev.bulletin_complaint", 26f, UIFontWeight.SemiBold, new Color(0.3f, 0.15f, 0.1f), Vector2.zero, new Vector2(290f, 230f), TextAlignmentOptions.Center);
            var memoHit = CloseupHotspot(go.transform, "Hotspot_Complaint", new Vector2(500f, 90f), new Vector2(350f, 290f));
            AttachGlow(memoHit, memo);

            FinishCloseup(go, screen, "field.elev.bulletin_hint",
                new[] { mainHit, memoHit, leftHit },
                new[] { "field.elev.bulletin_notice", "field.elev.bulletin_memo", "field.elev.bulletin_old" },
                actionIds: new[] { "action_test_010", null, null });
            return screen;
        }

        private static CloseupScreen BuildElevPanelCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.08f, 0.08f, 0.09f, 1f), out var go);
            CloseupRect(go.transform, "Wall", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.40f, 0.42f, 0.46f));
            for (int i = 0; i < 40; i++)
                CloseupRect(go.transform, "Hair_" + i, new Vector2(-950f + i * 48f, 120f), new Vector2(2f, 860f), new Color(0.46f, 0.48f, 0.52f));
            var plate = CloseupGroup(go.transform, "Plate", new Vector2(-120f, 140f));
            CloseupRect(plate.transform, "Plate", Vector2.zero, new Vector2(560f, 720f), new Color(0.70f, 0.72f, 0.76f));
            CloseupRect(plate.transform, "PlateShade", new Vector2(0f, -340f), new Vector2(560f, 40f), new Color(0.55f, 0.57f, 0.61f));
            GameObject threeButton = null;
            for (int i = 0; i < 10; i++)
            {
                int col = i % 2, row = 4 - i / 2;
                var pos = new Vector2(-110f + col * 220f, -120f + row * 100f);
                bool three = i == 2;
                var ring = CloseupRect(plate.transform, "Ring_" + (i + 1), pos, new Vector2(88f, 88f), new Color(0.45f, 0.47f, 0.51f));
                ring.sprite = RoundSprite(); ring.type = Image.Type.Simple;
                var btn = CloseupRect(plate.transform, "Button_" + (i + 1), pos, new Vector2(74f, 74f), three ? new Color(0.86f, 0.84f, 0.76f) : new Color(0.62f, 0.64f, 0.68f));
                btn.sprite = RoundSprite(); btn.type = Image.Type.Simple;
                var label = AddText(btn.transform, "N", 40f, UIFontWeight.Bold, three ? new Color(0.45f, 0.42f, 0.36f, 0.6f) : new Color(0.2f, 0.21f, 0.24f), Vector2.zero, new Vector2(74f, 74f), TextAlignmentOptions.Center);
                label.text = (i + 1).ToString();
                if (three)
                {
                    // 닳아 숫자가 흐려지고 둘레에 손때가 번졌다.
                    var smudge = CloseupRect(plate.transform, "Smudge", pos, new Vector2(150f, 150f), new Color(0.35f, 0.3f, 0.25f, 0.25f));
                    smudge.sprite = FieldSoftSprite(); smudge.type = Image.Type.Simple;
                    threeButton = btn.gameObject;
                }
            }
            CloseupLabel(plate.transform, "OpenClose", "field.elev.panel_openclose", 30f, UIFontWeight.Bold, new Color(0.25f, 0.26f, 0.3f), new Vector2(0f, -185f), new Vector2(440f, 50f), TextAlignmentOptions.Center);
            var threeHit = CloseupHotspot(plate.transform, "Hotspot_Three", new Vector2(-110f, 180f), new Vector2(150f, 110f));
            AttachGlow(threeHit, threeButton);

            // 아래 덮개. 나사 넷. 열면 안쪽 점검 표시.
            var coverOpen = CloseupGroup(plate.transform, "CoverOpen", new Vector2(0f, -290f));
            CloseupRect(coverOpen.transform, "Bay", Vector2.zero, new Vector2(440f, 130f), new Color(0.18f, 0.19f, 0.21f));
            var tag = CloseupRect(coverOpen.transform, "Tag", new Vector2(0f, 0f), new Vector2(380f, 90f), new Color(0.95f, 0.85f, 0.25f));
            CloseupLabel(tag.transform, "Text", "field.elev.panel_tag", 24f, UIFontWeight.Bold, new Color(0.15f, 0.12f, 0.05f), Vector2.zero, new Vector2(360f, 80f), TextAlignmentOptions.Center);
            coverOpen.SetActive(false);
            var cover = CloseupRect(plate.transform, "Cover", new Vector2(0f, -290f), new Vector2(460f, 140f), new Color(0.62f, 0.64f, 0.68f));
            foreach (var p in new[] { new Vector2(-200f, 50f), new Vector2(200f, 50f), new Vector2(-200f, -50f), new Vector2(200f, -50f) })
            {
                var screw = CloseupRect(cover.transform, "Screw", p, new Vector2(18f, 18f), new Color(0.38f, 0.39f, 0.42f));
                screw.sprite = RoundSprite(); screw.type = Image.Type.Simple;
            }
            var coverHit = CloseupHotspot(plate.transform, "Hotspot_Cover", new Vector2(0f, -290f), new Vector2(480f, 160f));
            AttachGlow(coverHit, cover.gameObject, coverOpen);

            // 오른쪽 비상 호출. 누르면 잡음만 난다.
            var call = CloseupGroup(go.transform, "Call", new Vector2(420f, 220f));
            CloseupRect(call.transform, "Box", Vector2.zero, new Vector2(240f, 300f), new Color(0.62f, 0.64f, 0.68f));
            for (int i = 0; i < 6; i++) CloseupRect(call.transform, "Grille", new Vector2(0f, 90f - i * 22f), new Vector2(160f, 8f), new Color(0.3f, 0.31f, 0.34f));
            var bell = CloseupRect(call.transform, "Bell", new Vector2(0f, -70f), new Vector2(90f, 90f), new Color(0.85f, 0.65f, 0.15f));
            bell.sprite = RoundSprite(); bell.type = Image.Type.Simple;
            var callHit = CloseupHotspot(call.transform, "Hotspot_Call", Vector2.zero, new Vector2(260f, 320f));
            AttachGlow(callHit, call);

            FinishCloseup(go, screen, "field.elev.panel_hint",
                new[] { threeHit, coverHit, callHit },
                new[] { "field.elev.panel_three", "field.elev.panel_cover", "field.elev.panel_call" },
                new[] { null, cover.gameObject, null }, new[] { null, coverOpen, null },
                actionIds: new[] { "action_test_005", "action_test_009", null });
            return screen;
        }

        /// <summary>엘리베이터 거울. 두 사람과 뒤쪽 문이 비친다. 아래 귀퉁이에 관리실 메모.</summary>
        private static CloseupScreen BuildElevMirrorCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.07f, 0.07f, 0.08f, 1f), out var go);
            CloseupRect(go.transform, "Wall", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.36f, 0.38f, 0.42f));
            var mirror = CloseupGroup(go.transform, "Mirror", new Vector2(-60f, 160f));
            CloseupRect(mirror.transform, "Frame", Vector2.zero, new Vector2(1180f, 760f), new Color(0.22f, 0.23f, 0.26f));
            CloseupRect(mirror.transform, "Glass", Vector2.zero, new Vector2(1140f, 720f), new Color(0.30f, 0.34f, 0.39f));
            // 비친 엘리베이터 안: 맞은편 문, 손잡이, 천장 불빛.
            CloseupRect(mirror.transform, "RefDoor", new Vector2(120f, -40f), new Vector2(300f, 560f), new Color(0.40f, 0.44f, 0.49f));
            CloseupRect(mirror.transform, "RefSeam", new Vector2(120f, -40f), new Vector2(4f, 560f), new Color(0.3f, 0.33f, 0.37f));
            CloseupRect(mirror.transform, "RefRail", new Vector2(0f, -160f), new Vector2(1140f, 14f), new Color(0.55f, 0.58f, 0.63f));
            var refLight = CloseupRect(mirror.transform, "RefLight", new Vector2(0f, 330f), new Vector2(1000f, 200f), new Color(1f, 1f, 1f, 0.2f));
            refLight.sprite = FieldSoftSprite(); refLight.type = Image.Type.Simple;
            // 거울에 비친 두 사람의 어두운 윤곽(가까이 본 거울이라 흐릿하다).
            foreach (var p in new[] { new Vector2(-300f, -120f), new Vector2(-120f, -110f) })
            {
                var head = CloseupRect(mirror.transform, "RefHead", p + new Vector2(0f, 170f), new Vector2(110f, 120f), new Color(0.12f, 0.12f, 0.15f, 0.8f));
                head.sprite = RoundSprite(); head.type = Image.Type.Simple;
                CloseupRect(mirror.transform, "RefBody", p, new Vector2(170f, 260f), new Color(0.10f, 0.10f, 0.13f, 0.8f));
            }
            var sheen = CloseupRect(mirror.transform, "Sheen", new Vector2(-300f, 120f), new Vector2(240f, 900f), new Color(1f, 1f, 1f, 0.06f));
            sheen.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -25f);
            var glassHit = CloseupHotspot(mirror.transform, "Hotspot_Glass", new Vector2(0f, 60f), new Vector2(1100f, 520f));
            AttachGlow(glassHit, mirror.transform.Find("Frame").gameObject);

            var memo = CloseupGroup(go.transform, "Memo", new Vector2(360f, -170f));
            var paper = CloseupRect(memo.transform, "Paper", Vector2.zero, new Vector2(320f, 190f), new Color(0.96f, 0.93f, 0.72f));
            paper.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            CloseupRect(memo.transform, "Tape", new Vector2(0f, 95f), new Vector2(110f, 30f), new Color(0.9f, 0.9f, 0.85f, 0.7f));
            CloseupLabel(paper.transform, "Text", "field.elev.mirror_memo_text", 22f, UIFontWeight.SemiBold, new Color(0.2f, 0.18f, 0.12f), Vector2.zero, new Vector2(290f, 160f), TextAlignmentOptions.Center);
            var memoHit = CloseupHotspot(memo.transform, "Hotspot_Memo", Vector2.zero, new Vector2(340f, 220f));
            AttachGlow(memoHit, memo);

            FinishCloseup(go, screen, "field.elev.mirror_hint",
                new[] { glassHit, memoHit },
                new[] { "field.elev.mirror_glass", "field.elev.mirror_memo" },
                actionIds: new[] { null, "action_test_007" });
            return screen;
        }
    }
}
