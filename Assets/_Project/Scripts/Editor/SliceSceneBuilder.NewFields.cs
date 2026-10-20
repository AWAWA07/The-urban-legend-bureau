using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Systems;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 새 본편 사건 셋의 현장.
    ///
    ///   #003 7호실의 노크   동명고시원 복도 → 7호실. 관리인 수첩으로 결계 순서를 알아내고, 7호실에서 한영이 결계를 친다.
    ///   #004 4시 44분의 호출 새벽마트 구도시점. 계산대·택배 보관함·출입문 공지.
    ///   #005 혼자 흔들리는 그네  달빛 어린이공원. 그네·상가 환풍기·입구 게시판.
    /// </summary>
    public static partial class SliceSceneBuilder
    {
        // =========================================================== #003 고시원

        /// <summary>돌려주는 지점: 관리인 책상, 7호실 문, 문지방, 머리맡, 창틀, 7호실 나가기.</summary>
        private static GameObject[] BuildGosiField(Transform parent)
        {
            var hall = BuildGosiHall(parent, out var desk, out var door7, out var npc);
            var room = BuildGosiRoom(parent, out var sill, out var head, out var window, out var exit);
            var swap = parent.gameObject.AddComponent<FieldSceneSwap>();
            var so = new SerializedObject(swap);
            so.Update();
            SetObjectArray(so.FindProperty("_beforeRoots"), hall);
            SetObjectArray(so.FindProperty("_afterRoots"), room);
            so.ApplyModifiedPropertiesWithoutUndo();
            return new[] { desk, door7, sill, head, window, exit, npc };
        }

        private static GameObject BuildGosiHall(Transform parent, out GameObject deskPoint, out GameObject doorPoint, out GameObject npcPoint)
        {
            var root = new GameObject("GosiHall");
            root.transform.SetParent(parent, false);
            var wall = new Color(0.36f, 0.34f, 0.30f);
            var doorCol = new Color(0.42f, 0.33f, 0.26f);
            var shadow = new Color(0f, 0f, 0f, 0.4f);

            // 좁고 긴 고시원 복도. 누런 벽지, 낮은 천장, 문이 다닥다닥 붙어 있다.
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), wall, -9);
            for (int i = 0; i <= 60; i++)
                AddFieldRect(root.transform, "Paper_" + i, new Vector2(-22f + i * 0.74f, 0.6f), new Vector2(0.03f, 6.4f), Color.Lerp(wall, Color.black, 0.12f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.4f), new Vector2(44f, 1.2f), new Color(0.18f, 0.17f, 0.16f), -8);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), new Color(0.30f, 0.24f, 0.18f), -8);
            for (int i = 0; i <= 44; i++)
                AddFieldRect(root.transform, "Board_" + i, new Vector2(-22f + i * 1.0f, -4.5f), new Vector2(0.03f, 2.2f), new Color(0.24f, 0.19f, 0.14f), -7);
            AddFieldRect(root.transform, "Baseboard", new Vector2(0f, -3.3f), new Vector2(44f, 0.18f), new Color(0.2f, 0.17f, 0.14f), -7);
            AddFieldGradient(root.transform, "CeilingShade", new Vector2(0f, 2.4f), new Vector2(44f, 1.4f), new Color(0f, 0f, 0f, 0.45f), -6);

            // 형광등 셋. 가운데 하나는 연출로 깜빡인다.
            var tubes = new GameObject("Tubes");
            tubes.transform.SetParent(root.transform, false);
            foreach (var x in new[] { -7f, 0.5f, 8f })
            {
                AddFieldRect(tubes.transform, "Tube_" + x, new Vector2(x, 2.72f), new Vector2(1.8f, 0.12f), new Color(0.55f, 0.58f, 0.52f), -6);
                AddFieldSoft(tubes.transform, "Pool_" + x, new Vector2(x, 0.4f), new Vector2(6f, 6f), new Color(0.9f, 1f, 0.9f, 0.05f), -6);
            }

            // 맨 왼쪽 총무실: 밀창이 달린 작은 창구와 책상, 그 위 수첩.
            AddFieldRect(root.transform, "OfficeWall", new Vector2(-11.2f, -0.6f), new Vector2(3.8f, 5.4f), new Color(0.28f, 0.27f, 0.25f), -7);
            AddFieldRect(root.transform, "OfficeWindow", new Vector2(-11.2f, 0.4f), new Vector2(2.8f, 1.6f), new Color(0.12f, 0.14f, 0.16f), -6);
            AddFieldRect(root.transform, "OfficeSign", new Vector2(-11.2f, 1.6f), new Vector2(1.6f, 0.4f), new Color(0.85f, 0.83f, 0.75f), -5);
            var sign = AddFieldWorldText(root.transform, "OfficeSignText", new Vector2(-11.2f, 1.6f), new Vector2(1.5f, 0.36f), "field.gosi.office", new Color(0.2f, 0.18f, 0.16f), -4);
            sign.fontSizeMax = 2.2f;
            AddFieldRect(root.transform, "Desk", new Vector2(-11.2f, -1.1f), new Vector2(3.4f, 0.2f), new Color(0.45f, 0.35f, 0.25f), -5);
            AddFieldRect(root.transform, "Notebook", new Vector2(-10.7f, -0.93f), new Vector2(0.7f, 0.14f), new Color(0.18f, 0.22f, 0.34f), -4);
            AddFieldRect(root.transform, "Bell", new Vector2(-11.9f, -0.9f), new Vector2(0.3f, 0.2f), new Color(0.75f, 0.7f, 0.4f), -4);

            // 방문 일곱 개(3호~9호). 7호실만 문틈 아래로 빛이 없다.
            for (int n = 3; n <= 9; n++)
            {
                float x = -7.6f + (n - 3) * 2.75f;
                AddFieldSoft(root.transform, "Shadow_" + n, new Vector2(x, -3.42f), new Vector2(1.9f, 0.3f), shadow, -6);
                AddFieldRect(root.transform, "DoorFrame_" + n, new Vector2(x, -0.85f), new Vector2(1.6f, 5.0f), new Color(0.25f, 0.2f, 0.16f), -6);
                AddFieldRect(root.transform, "Door_" + n, new Vector2(x, -0.9f), new Vector2(1.4f, 4.8f), doorCol, -5);
                AddFieldCircle(root.transform, "Knob_" + n, new Vector2(x + 0.5f, -1.0f), 0.18f, new Color(0.7f, 0.65f, 0.5f), -4);
                AddFieldRect(root.transform, "Plate_" + n, new Vector2(x, 1.0f), new Vector2(0.5f, 0.32f), new Color(0.85f, 0.83f, 0.75f), -4);
                var num = AddFieldWorldText(root.transform, "Num_" + n, new Vector2(x, 1.0f), new Vector2(0.5f, 0.32f), "field.gosi.room_" + n, new Color(0.2f, 0.18f, 0.16f), -3);
                num.fontSizeMax = 2.2f;
                if (n != 7) AddFieldRect(root.transform, "Gap_" + n, new Vector2(x, -3.28f), new Vector2(1.2f, 0.05f), new Color(1f, 0.85f, 0.55f, 0.6f), -4);
                if (n % 2 == 0) AddFieldRect(root.transform, "Slippers_" + n, new Vector2(x - 0.3f, -3.35f), new Vector2(0.5f, 0.12f), new Color(0.3f, 0.4f, 0.55f), -4);
            }
            // 7호실 문 앞의 빛바랜 출입 금지 띠와 소금 자국.
            var tape = AddFieldRect(root.transform, "Tape7", new Vector2(3.4f, -0.2f), new Vector2(1.9f, 0.16f), new Color(0.85f, 0.7f, 0.2f), -3);
            tape.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            AddFieldRect(root.transform, "OldSalt7", new Vector2(3.4f, -3.32f), new Vector2(1.3f, 0.06f), new Color(0.85f, 0.85f, 0.82f, 0.5f), -4);
            var door7 = root.transform.Find("Door_7").gameObject;

            AddFieldRect(root.transform, "Rack", new Vector2(11.4f, -2.2f), new Vector2(1.6f, 2.2f), new Color(0.5f, 0.52f, 0.55f, 0.5f), -5);
            AddFieldRect(root.transform, "Towel", new Vector2(11.2f, -1.6f), new Vector2(0.6f, 0.8f), new Color(0.6f, 0.3f, 0.3f), -4);
            AddFieldRect(root.transform, "Night", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.03f, 0.03f, 0.06f, 0.5f), -2);

            BuildFieldActors(root.transform, -3.86f, -8.8f, 2.4f, -10.6f, 11.0f);

            deskPoint = BuildPoint(root.transform, "InvestigationPoint_GosiDesk", new Vector2(-11.2f, -0.6f), new Vector2(3.6f, 3.2f),
                new Color(0.6f, 0.5f, 0.4f), "field.gosi.desk", "field.gosi.desk", null);
            doorPoint = BuildPoint(root.transform, "InvestigationPoint_GosiDoor7", new Vector2(3.4f, -0.9f), new Vector2(1.8f, 5.0f),
                new Color(0.6f, 0.3f, 0.3f), "field.gosi.door7", "field.gosi.door7", null);
            SetHighlightTargets(deskPoint, root.transform, "Desk", "Notebook", "OfficeWindow");
            SetHighlightTargets(doorPoint, root.transform, "Door_7");

            // 8호실 고시생. 트레이닝복에 안경, 컵라면을 든 채 반쯤 열린 8호실 문 옆에 서 있다. 말을 걸 수 있다.
            var student = new GameObject("GosiStudent"); student.transform.SetParent(root.transform, false);
            const float sx = 7.5f;
            AddFieldSoft(student.transform, "Shadow", new Vector2(sx, -3.86f), new Vector2(1.4f, 0.3f), shadow, -4);
            AddFieldRect(student.transform, "LegL", new Vector2(sx - 0.18f, -3.5f), new Vector2(0.26f, 0.7f), new Color(0.16f, 0.17f, 0.22f), -3);
            AddFieldRect(student.transform, "LegR", new Vector2(sx + 0.18f, -3.5f), new Vector2(0.26f, 0.7f), new Color(0.16f, 0.17f, 0.22f), -3);
            AddFieldRect(student.transform, "Slippers", new Vector2(sx, -3.82f), new Vector2(0.9f, 0.1f), new Color(0.3f, 0.4f, 0.55f), -2);
            AddFieldRect(student.transform, "Body", new Vector2(sx, -2.65f), new Vector2(0.95f, 1.1f), new Color(0.32f, 0.38f, 0.34f), -3);
            AddFieldRect(student.transform, "Zip", new Vector2(sx, -2.65f), new Vector2(0.04f, 1.0f), new Color(0.75f, 0.75f, 0.7f), -2);
            AddFieldRect(student.transform, "Cup", new Vector2(sx - 0.42f, -2.55f), new Vector2(0.34f, 0.4f), new Color(0.92f, 0.9f, 0.85f), -1);
            AddFieldRect(student.transform, "CupBand", new Vector2(sx - 0.42f, -2.45f), new Vector2(0.34f, 0.08f), new Color(0.8f, 0.2f, 0.15f), 0);
            AddFieldCircle(student.transform, "Head", new Vector2(sx, -1.55f), 1.05f, new Color(0.93f, 0.82f, 0.72f), -2);
            AddFieldRect(student.transform, "Hair", new Vector2(sx, -1.12f), new Vector2(1.05f, 0.36f), new Color(0.12f, 0.11f, 0.12f), -1);
            AddFieldRect(student.transform, "GlassL", new Vector2(sx - 0.2f, -1.55f), new Vector2(0.26f, 0.18f), new Color(0.15f, 0.15f, 0.18f, 0.9f), -1);
            AddFieldRect(student.transform, "GlassR", new Vector2(sx + 0.2f, -1.55f), new Vector2(0.26f, 0.18f), new Color(0.15f, 0.15f, 0.18f, 0.9f), -1);
            AddFieldRect(student.transform, "Mouth", new Vector2(sx, -1.88f), new Vector2(0.2f, 0.04f), new Color(0.45f, 0.25f, 0.22f), -1);
            // 복도의 밤 그늘(-2) 아래에 둔다. 다른 물건처럼 어둑하게 보인다.
            foreach (var sr in student.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder -= 3;
            npcPoint = BuildPoint(root.transform, "InvestigationPoint_GosiStudent", new Vector2(sx, -2.4f), new Vector2(1.3f, 2.9f),
                new Color(0.5f, 0.6f, 0.55f), "field.gosi.npc.point", "field.gosi.npc.point", null);
            SetHighlightTargets(npcPoint, root.transform, "GosiStudent");

            // --- 무서운 연출
            var midTube = root.transform.Find("Tubes").gameObject;
            var gap5 = root.transform.Find("Gap_5").gameObject;
            AddScare(root.transform, "gosi_gap", FieldScare.TriggerKind.ReachX, -3.0f, null, 1.5f,
                SSound("step_soft_1", 0.3f), SHide(gap5), SWait(0.5f), SSound("step_soft_2", 0.3f), SWait(0.5f), SShow(gap5), SWait(0.4f),
                SLines("field.gosi.scare.gap.001", "!field.gosi.scare.gap.002"));
            AddScare(root.transform, "gosi_knock", FieldScare.TriggerKind.ReachX, 0.6f, null, 0f,
                SSound("door_bang_0", 0.55f), SShake(door7, 0.15f, 0.04f), SWait(0.45f),
                SSound("door_bang_0", 0.6f), SShake(door7, 0.15f, 0.04f), SWait(0.45f),
                SSound("door_bang_1", 0.7f), SShake(door7, 0.2f, 0.06f), SWait(0.9f),
                SLines("field.gosi.scare.knock.001", "!field.gosi.scare.knock.002", "!field.gosi.scare.knock.003"));
            var slippers = root.transform.Find("Slippers_6").gameObject;
            AddScare(root.transform, "gosi_slippers", FieldScare.TriggerKind.AfterPoint, 0f, "point_gosi_desk", 0f,
                SWait(0.5f), SSound("step_soft_2", 0.35f), SHide(slippers), SWait(1.4f),
                SLines("field.gosi.scare.slipper.001", "!field.gosi.scare.slipper.002"), SShow(slippers));
            return root;
        }

        private static GameObject BuildGosiRoom(Transform parent, out GameObject sillPoint, out GameObject headPoint, out GameObject windowPoint, out GameObject exitPoint)
        {
            var root = new GameObject("GosiRoom");
            root.transform.SetParent(parent, false);
            const float half = 5.2f;   // 한 평 남짓. 엘리베이터만큼 좁다
            AddFieldRect(root.transform, "Outside", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.01f, 0.01f, 0.015f), -10);
            AddFieldRect(root.transform, "Wall", new Vector2(0f, -0.2f), new Vector2(2f * half, 7.0f), new Color(0.32f, 0.31f, 0.28f), -9);
            for (int i = 0; i < 14; i++)
                AddFieldRect(root.transform, "Paper_" + i, new Vector2(-half + 0.4f + i * 0.74f, -0.2f), new Vector2(0.03f, 7f), new Color(0.27f, 0.26f, 0.24f), -8);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.3f), new Vector2(2f * half, 1.8f), new Color(0.28f, 0.22f, 0.17f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.5f), new Vector2(2f * half + 1f, 0.8f), new Color(0.1f, 0.1f, 0.1f), -8);
            AddFieldRect(root.transform, "Edge_L", new Vector2(-half - 0.25f, -0.3f), new Vector2(0.5f, 7.2f), new Color(0.12f, 0.11f, 0.1f), -8);
            AddFieldRect(root.transform, "Edge_R", new Vector2(half + 0.25f, -0.3f), new Vector2(0.5f, 7.2f), new Color(0.12f, 0.11f, 0.1f), -8);

            // 왼쪽 방문과 문지방.
            AddFieldRect(root.transform, "Door", new Vector2(-4.1f, -0.9f), new Vector2(1.6f, 4.8f), new Color(0.38f, 0.3f, 0.24f), -7);
            AddFieldRect(root.transform, "Sill", new Vector2(-4.1f, -3.36f), new Vector2(1.8f, 0.14f), new Color(0.5f, 0.42f, 0.32f), -6);
            // 가운데 창. 밖은 옆 건물 벽이 바짝 붙어 깜깜하다.
            AddFieldRect(root.transform, "WindowFrame", new Vector2(0f, 1.0f), new Vector2(2.4f, 1.9f), new Color(0.5f, 0.48f, 0.44f), -7);
            AddFieldRect(root.transform, "WindowGlass", new Vector2(0f, 1.0f), new Vector2(2.1f, 1.6f), new Color(0.04f, 0.05f, 0.07f), -6);
            AddFieldRect(root.transform, "WindowSill", new Vector2(0f, -0.02f), new Vector2(2.7f, 0.16f), new Color(0.55f, 0.52f, 0.46f), -5);
            AddFieldRect(root.transform, "OldWax", new Vector2(0.6f, 0.08f), new Vector2(0.3f, 0.06f), new Color(0.85f, 0.8f, 0.65f), -4);
            // 창에 비치는 얼굴. 결계가 틀리면, 그리고 마지막에 나온다.
            var face = new GameObject("WindowFace");
            face.transform.SetParent(root.transform, false);
            AddFieldCircle(face.transform, "Head", new Vector2(0.1f, 1.15f), 0.9f, new Color(0.75f, 0.75f, 0.72f, 0.55f), -5);
            AddFieldRect(face.transform, "EyeL", new Vector2(-0.12f, 1.25f), new Vector2(0.12f, 0.16f), new Color(0f, 0f, 0f, 0.9f), -4);
            AddFieldRect(face.transform, "EyeR", new Vector2(0.32f, 1.25f), new Vector2(0.12f, 0.16f), new Color(0f, 0f, 0f, 0.9f), -4);
            AddFieldRect(face.transform, "Mouth", new Vector2(0.1f, 0.92f), new Vector2(0.36f, 0.08f), new Color(0f, 0f, 0f, 0.8f), -4);
            face.SetActive(false);
            // 오른쪽 좁은 침대와 머리맡.
            AddFieldRect(root.transform, "Bed", new Vector2(3.2f, -2.75f), new Vector2(3.4f, 0.9f), new Color(0.3f, 0.32f, 0.38f), -6);
            AddFieldRect(root.transform, "Pillow", new Vector2(4.4f, -2.2f), new Vector2(0.9f, 0.3f), new Color(0.75f, 0.75f, 0.72f), -5);
            AddFieldRect(root.transform, "Headboard", new Vector2(4.9f, -1.9f), new Vector2(0.2f, 1.8f), new Color(0.35f, 0.27f, 0.2f), -5);
            AddFieldRect(root.transform, "Desk", new Vector2(-1.6f, -2.1f), new Vector2(1.6f, 0.14f), new Color(0.45f, 0.35f, 0.25f), -6);
            AddFieldRect(root.transform, "DeskLeg", new Vector2(-1.6f, -2.8f), new Vector2(0.12f, 1.3f), new Color(0.35f, 0.27f, 0.2f), -6);

            // 결계 세 가지. 처음엔 숨겨 두고 놓을 때 드러난다.
            var salt = AddFieldRect(root.transform, "Rite_Salt", new Vector2(-4.1f, -3.28f), new Vector2(1.6f, 0.08f), new Color(0.95f, 0.95f, 0.92f), -3);
            salt.SetActive(false);
            var charm = new GameObject("Rite_Charm");
            charm.transform.SetParent(root.transform, false);
            AddFieldRect(charm.transform, "Paper", new Vector2(4.65f, -1.5f), new Vector2(0.28f, 0.7f), new Color(0.95f, 0.85f, 0.35f), -3);
            AddFieldRect(charm.transform, "Ink", new Vector2(4.65f, -1.5f), new Vector2(0.06f, 0.5f), new Color(0.75f, 0.12f, 0.1f), -2);
            charm.SetActive(false);
            var candle = new GameObject("Rite_Candle");
            candle.transform.SetParent(root.transform, false);
            AddFieldRect(candle.transform, "Wax", new Vector2(-0.6f, 0.22f), new Vector2(0.16f, 0.36f), new Color(0.92f, 0.9f, 0.82f), -3);
            AddFieldCircle(candle.transform, "Flame", new Vector2(-0.6f, 0.5f), 0.14f, new Color(1f, 0.8f, 0.35f), -2);
            AddFieldSoft(candle.transform, "Glow", new Vector2(-0.6f, 0.4f), new Vector2(3.2f, 3.2f), new Color(1f, 0.7f, 0.3f, 0.22f), -2);
            candle.SetActive(false);

            AddFieldRect(root.transform, "Dim", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.02f, 0.02f, 0.05f, 0.42f), -2);
            AddFieldGradient(root.transform, "Close_L", new Vector2(-half + 0.9f, -0.3f), new Vector2(1.8f, 7f), new Color(0f, 0f, 0f, 0.5f), 15, 90f);
            AddFieldGradient(root.transform, "Close_R", new Vector2(half - 0.9f, -0.3f), new Vector2(1.8f, 7f), new Color(0f, 0f, 0f, 0.5f), 15, -90f);
            var dark = AddBlackout(root.transform, "Blackout", new Color(0f, 0f, 0.02f, 0.95f));
            dark.name = "Blackout";

            BuildFieldActors(root.transform, -3.86f, -2.6f, 1.9f, -3.6f, 3.0f);

            sillPoint = BuildPoint(root.transform, "InvestigationPoint_RiteSill", new Vector2(-4.1f, -3.0f), new Vector2(1.8f, 0.8f),
                new Color(0.8f, 0.8f, 0.8f), "field.gosi.rite_sill", "field.gosi.rite_sill", null);
            headPoint = BuildPoint(root.transform, "InvestigationPoint_RiteHead", new Vector2(4.4f, -1.8f), new Vector2(1.4f, 1.6f),
                new Color(0.8f, 0.7f, 0.4f), "field.gosi.rite_head", "field.gosi.rite_head", null);
            windowPoint = BuildPoint(root.transform, "InvestigationPoint_RiteWindow", new Vector2(0f, 0.8f), new Vector2(2.6f, 2.2f),
                new Color(0.6f, 0.6f, 0.7f), "field.gosi.rite_window", "field.gosi.rite_window", null);
            exitPoint = BuildPoint(root.transform, "InvestigationPoint_GosiExit", new Vector2(-4.1f, 0.4f), new Vector2(1.6f, 2.4f),
                new Color(0.5f, 0.5f, 0.5f), "field.gosi.exit", "field.gosi.exit", null);
            SetHighlightTargets(sillPoint, root.transform, "Sill");
            SetHighlightTargets(headPoint, root.transform, "Pillow", "Headboard");
            SetHighlightTargets(windowPoint, root.transform, "WindowFrame");
            SetHighlightTargets(exitPoint, root.transform, "Door");
            return root;
        }

        /// <summary>총무실 책상의 관리인 수첩. 펼친 두 쪽에 7호실 결계 순서가 적혀 있다.</summary>
        private static CloseupScreen BuildGosiDeskCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.06f, 0.05f, 0.04f, 1f), out var go);
            CloseupRect(go.transform, "Desk", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.32f, 0.24f, 0.17f));
            for (int i = 0; i < 9; i++)
                CloseupRect(go.transform, "Grain", new Vector2(0f, -280f + i * 95f), new Vector2(1920f, 3f), new Color(0.27f, 0.2f, 0.14f));
            var book = CloseupGroup(go.transform, "Notebook", new Vector2(-40f, 150f));
            CloseupRect(book.transform, "Cover", Vector2.zero, new Vector2(1180f, 640f), new Color(0.16f, 0.2f, 0.3f));
            var pageL = CloseupRect(book.transform, "PageL", new Vector2(-290f, 0f), new Vector2(560f, 600f), new Color(0.94f, 0.92f, 0.85f));
            var pageR = CloseupRect(book.transform, "PageR", new Vector2(290f, 0f), new Vector2(560f, 600f), new Color(0.94f, 0.92f, 0.85f));
            CloseupLabel(pageL.transform, "Text", "field.gosi.note_left", 26f, UIFontWeight.Medium, new Color(0.18f, 0.2f, 0.32f), new Vector2(0f, 20f), new Vector2(480f, 520f), TextAlignmentOptions.TopLeft);
            CloseupLabel(pageR.transform, "Text", "field.gosi.note_right", 26f, UIFontWeight.Medium, new Color(0.18f, 0.2f, 0.32f), new Vector2(0f, 20f), new Vector2(480f, 520f), TextAlignmentOptions.TopLeft);
            var leftHit = CloseupHotspot(book.transform, "Hotspot_PageL", new Vector2(-290f, 0f), new Vector2(560f, 600f));
            var rightHit = CloseupHotspot(book.transform, "Hotspot_PageR", new Vector2(290f, 0f), new Vector2(560f, 600f));
            AttachGlow(leftHit, pageL.gameObject);
            AttachGlow(rightHit, pageR.gameObject);
            FinishCloseup(go, screen, "field.gosi.desk_hint",
                new[] { leftHit, rightHit },
                new[] { "field.gosi.note_left_read", "field.gosi.note_right_read" },
                actionIds: new[] { "action_test_013", "action_test_014" });
            return screen;
        }

        // =========================================================== #004 편의점

        private static GameObject[] BuildStoreField(Transform parent)
        {
            var root = new GameObject("Store");
            root.transform.SetParent(parent, false);
            var wall = new Color(0.46f, 0.48f, 0.5f);   // 새벽 4시, 반쯤 불을 끈 가게

            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), wall, -9);
            AddFieldRect(root.transform, "Band", new Vector2(0f, 2.55f), new Vector2(44f, 0.5f), new Color(0.16f, 0.45f, 0.38f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.5f), new Vector2(44f, 1f), new Color(0.3f, 0.31f, 0.32f), -8);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), new Color(0.4f, 0.4f, 0.39f), -8);
            for (int i = 0; i <= 29; i++)
                AddFieldRect(root.transform, "Tile_" + i, new Vector2(-21.75f + i * 1.5f, -4.5f), new Vector2(0.03f, 2.2f), new Color(0.62f, 0.62f, 0.6f), -7);
            AddFieldGradient(root.transform, "FloorSheen", new Vector2(0f, -3.7f), new Vector2(44f, 0.8f), new Color(1f, 1f, 1f, 0.15f), -7);
            var lights = new GameObject("Lights");
            lights.transform.SetParent(root.transform, false);
            foreach (var x in new[] { -8f, -2.5f, 3f, 8.5f })
                AddFieldRect(lights.transform, "Light_" + x, new Vector2(x, 3.05f), new Vector2(2.6f, 0.14f), new Color(1f, 1f, 1f), -6);

            // 왼쪽 자동문. 밖은 깜깜한 밤거리. 문에 공지가 붙어 있다.
            AddFieldRect(root.transform, "DoorFrame", new Vector2(-11.6f, -0.6f), new Vector2(3.4f, 5.6f), new Color(0.4f, 0.42f, 0.44f), -7);
            var doorL = new GameObject("AutoDoorL"); doorL.transform.SetParent(root.transform, false); doorL.transform.localPosition = new Vector3(-12.4f, -0.6f, 0f);
            AddFieldRect(doorL.transform, "Glass", Vector2.zero, new Vector2(1.55f, 5.3f), new Color(0.05f, 0.07f, 0.1f, 0.95f), -6);
            var doorR = new GameObject("AutoDoorR"); doorR.transform.SetParent(root.transform, false); doorR.transform.localPosition = new Vector3(-10.8f, -0.6f, 0f);
            AddFieldRect(doorR.transform, "Glass", Vector2.zero, new Vector2(1.55f, 5.3f), new Color(0.05f, 0.07f, 0.1f, 0.95f), -6);
            AddFieldRect(doorR.transform, "Notice", new Vector2(0.1f, 0.4f), new Vector2(0.8f, 1.0f), new Color(0.96f, 0.95f, 0.88f), -5);
            AddFieldRect(doorR.transform, "NoticeHead", new Vector2(0.1f, 0.82f), new Vector2(0.8f, 0.16f), new Color(0.16f, 0.45f, 0.38f), -4);

            // 진열대 두 줄.
            Color[] goods = { new Color(0.85f, 0.3f, 0.25f), new Color(0.95f, 0.75f, 0.2f), new Color(0.3f, 0.55f, 0.85f), new Color(0.4f, 0.75f, 0.4f) };
            foreach (var sx in new[] { -6.5f, -2.0f })
            {
                AddFieldRect(root.transform, "Shelf_" + sx, new Vector2(sx, -1.6f), new Vector2(3.6f, 3.6f), new Color(0.9f, 0.9f, 0.9f), -6);
                for (int r = 0; r < 4; r++)
                {
                    AddFieldRect(root.transform, "Board", new Vector2(sx, -3.2f + r * 0.9f), new Vector2(3.6f, 0.08f), new Color(0.7f, 0.7f, 0.7f), -5);
                    for (int k = 0; k < 7; k++)
                        AddFieldRect(root.transform, "Item", new Vector2(sx - 1.5f + k * 0.5f, -2.95f + r * 0.9f), new Vector2(0.36f, 0.45f), goods[(k + r) % 4], -4);
                }
            }
            // 냉장고. 한 칸의 불이 연출로 깜빡인다.
            AddFieldRect(root.transform, "Fridge", new Vector2(2.6f, -0.9f), new Vector2(3.0f, 5.0f), new Color(0.25f, 0.27f, 0.3f), -6);
            var fridgeLight = AddFieldRect(root.transform, "FridgeGlow", new Vector2(2.6f, -0.9f), new Vector2(2.7f, 4.7f), new Color(0.7f, 0.9f, 1f, 0.5f), -5);
            for (int r = 0; r < 4; r++)
                for (int k = 0; k < 5; k++)
                    AddFieldRect(root.transform, "Drink", new Vector2(1.6f + k * 0.5f, -2.9f + r * 1.1f), new Vector2(0.28f, 0.6f), goods[(k * 3 + r) % 4], -4);

            // 오른쪽 계산대. 위에 방송 스피커, 옆에 택배 보관함.
            AddFieldRect(root.transform, "Counter", new Vector2(7.6f, -2.5f), new Vector2(3.6f, 1.8f), new Color(0.2f, 0.4f, 0.34f), -5);
            AddFieldRect(root.transform, "CounterTop", new Vector2(7.6f, -1.55f), new Vector2(3.8f, 0.14f), new Color(0.9f, 0.9f, 0.88f), -4);
            AddFieldRect(root.transform, "POS", new Vector2(7.0f, -1.1f), new Vector2(0.8f, 0.7f), new Color(0.12f, 0.12f, 0.14f), -3);
            AddFieldRect(root.transform, "Announcer", new Vector2(8.4f, -1.3f), new Vector2(0.6f, 0.36f), new Color(0.2f, 0.2f, 0.22f), -3);
            AddFieldRect(root.transform, "AnnouncerLed", new Vector2(8.25f, -1.25f), new Vector2(0.12f, 0.08f), new Color(1f, 0.3f, 0.2f), -2);
            var speaker = AddFieldCircle(root.transform, "Speaker", new Vector2(7.6f, 2.95f), 0.7f, new Color(0.95f, 0.95f, 0.95f), -5);
            AddFieldCircle(root.transform, "SpeakerGrill", new Vector2(7.6f, 2.95f), 0.45f, new Color(0.6f, 0.6f, 0.62f), -4);
            AddFieldRect(root.transform, "Locker", new Vector2(11.4f, -1.2f), new Vector2(2.4f, 4.4f), new Color(0.55f, 0.6f, 0.62f), -6);
            for (int r = 0; r < 4; r++)
                for (int k = 0; k < 2; k++)
                {
                    AddFieldRect(root.transform, "Box", new Vector2(10.85f + k * 1.1f, -2.8f + r * 1.05f), new Vector2(1.0f, 0.95f), new Color(0.48f, 0.53f, 0.55f), -5);
                    AddFieldCircle(root.transform, "BoxLed", new Vector2(11.2f + k * 1.1f, -2.5f + r * 1.05f), 0.1f, (r + k) % 3 == 0 ? new Color(0.9f, 0.3f, 0.2f) : new Color(0.3f, 0.8f, 0.4f), -4);
                }
            AddFieldRect(root.transform, "Clock", new Vector2(4.8f, 2.0f), new Vector2(1.3f, 0.5f), new Color(0.08f, 0.08f, 0.1f), -5);
            var clock = AddFieldWorldText(root.transform, "ClockText", new Vector2(4.8f, 2.0f), new Vector2(1.2f, 0.45f), "field.store.clock", new Color(1f, 0.35f, 0.25f), -4);
            clock.fontSizeMax = 3f;
            AddFieldRect(root.transform, "Night", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.03f, 0.05f, 0.09f, 0.45f), -2);

            BuildFieldActors(root.transform, -3.86f, -9.0f, 2.4f, -10.4f, 10.0f);

            var counterPoint = BuildPoint(root.transform, "InvestigationPoint_StoreCounter", new Vector2(7.6f, -1.6f), new Vector2(3.8f, 2.6f),
                new Color(0.4f, 0.6f, 0.5f), "field.store.counter", "field.store.counter", null);
            var lockerPoint = BuildPoint(root.transform, "InvestigationPoint_StoreLocker", new Vector2(11.4f, -1.2f), new Vector2(2.4f, 4.4f),
                new Color(0.6f, 0.6f, 0.6f), "field.store.locker", "field.store.locker", null);
            var noticePoint = BuildPoint(root.transform, "InvestigationPoint_StoreNotice", new Vector2(-10.7f, -0.2f), new Vector2(1.4f, 1.6f),
                new Color(0.8f, 0.8f, 0.7f), "field.store.notice", "field.store.notice", null);
            SetHighlightTargets(counterPoint, root.transform, "Counter", "CounterTop", "Announcer");
            SetHighlightTargets(lockerPoint, root.transform, "Locker");
            SetHighlightTargets(noticePoint, root.transform, "AutoDoorR/Notice");

            // --- 무서운 연출
            AddScare(root.transform, "store_call", FieldScare.TriggerKind.ReachX, 5.0f, null, 0f,
                SSound("intercom_static", 0.45f), SShake(speaker, 0.6f, 0.03f), SWait(0.4f), SSound("ghost_whisper_0", 0.3f), SWait(0.6f),
                SLines("field.store.scare.call.001", "field.store.scare.call.002", "!field.store.scare.call.003", "field.store.scare.call.004"));
            AddScare(root.transform, "store_door", FieldScare.TriggerKind.AfterPoint, 0f, "point_store_locker", 0f,
                SWait(0.6f), SSound("elevator_ding", 0.4f, 1.25f), SSound("door_slide", 0.4f),
                SMoveAsync(doorL, new Vector2(-1.4f, 0f), 0.6f), SMove(doorR, new Vector2(1.4f, 0f), 0.6f), SWait(1.4f),
                SLines("field.store.scare.door.001", "!field.store.scare.door.002"),
                SSound("door_slide", 0.35f), SMoveAsync(doorL, new Vector2(1.4f, 0f), 0.6f), SMove(doorR, new Vector2(-1.4f, 0f), 0.6f));
            return new[] { counterPoint, lockerPoint, noticePoint };
        }

        private static CloseupScreen BuildStoreCounterCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.08f, 0.1f, 0.1f, 1f), out var go);
            CloseupRect(go.transform, "Top", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.82f, 0.82f, 0.8f));
            var unit = CloseupGroup(go.transform, "Announcer", new Vector2(-360f, 160f));
            CloseupRect(unit.transform, "Body", Vector2.zero, new Vector2(760f, 470f), new Color(0.18f, 0.18f, 0.2f));
            CloseupRect(unit.transform, "Screen", new Vector2(0f, 40f), new Vector2(660f, 300f), new Color(0.08f, 0.2f, 0.16f));
            CloseupLabel(unit.transform, "Text", "field.store.schedule", 26f, UIFontWeight.SemiBold, new Color(0.55f, 1f, 0.7f), new Vector2(0f, 40f), new Vector2(620f, 280f), TextAlignmentOptions.TopLeft);
            for (int i = 0; i < 4; i++) CloseupRect(unit.transform, "Key", new Vector2(-240f + i * 160f, -170f), new Vector2(120f, 50f), new Color(0.35f, 0.35f, 0.38f));
            var unitHit = CloseupHotspot(go.transform, "Hotspot_Schedule", new Vector2(-360f, 160f), new Vector2(780f, 490f));
            AttachGlow(unitHit, unit);
            var log = CloseupGroup(go.transform, "Log", new Vector2(500f, 60f));
            var lp = CloseupRect(log.transform, "Paper", Vector2.zero, new Vector2(520f, 640f), new Color(0.97f, 0.96f, 0.9f));
            lp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -4f);
            CloseupLabel(lp.transform, "Text", "field.store.log_text", 30f, UIFontWeight.Medium, new Color(0.15f, 0.15f, 0.2f), new Vector2(0f, 0f), new Vector2(460f, 580f), TextAlignmentOptions.TopLeft);
            var logHit = CloseupHotspot(go.transform, "Hotspot_Log", new Vector2(500f, 60f), new Vector2(540f, 660f));
            AttachGlow(logHit, log);
            FinishCloseup(go, screen, "field.store.counter_hint",
                new[] { unitHit, logHit },
                new[] { "field.store.counter_schedule", "field.store.counter_log" },
                actionIds: new[] { "action_test_016", "action_test_019" });
            return screen;
        }

        private static CloseupScreen BuildStoreLockerCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.08f, 0.09f, 0.1f, 1f), out var go);
            CloseupRect(go.transform, "Locker", new Vector2(0f, 120f), new Vector2(1400f, 860f), new Color(0.55f, 0.6f, 0.62f));
            var boxes = CloseupGroup(go.transform, "Boxes", new Vector2(0f, 140f));
            string[] ids = { "field.store.label_1", "field.store.label_2", "field.store.label_3" };
            for (int i = 0; i < 3; i++)
            {
                var box = CloseupRect(boxes.transform, "Box_" + i, new Vector2(-420f + i * 420f, 0f), new Vector2(360f, 300f), new Color(0.72f, 0.56f, 0.38f));
                var lab = CloseupRect(box.transform, "Label", new Vector2(0f, 20f), new Vector2(280f, 170f), new Color(0.98f, 0.98f, 0.96f));
                CloseupLabel(lab.transform, "Text", ids[i], 22f, UIFontWeight.SemiBold, new Color(0.15f, 0.15f, 0.18f), Vector2.zero, new Vector2(260f, 150f), TextAlignmentOptions.Center);
            }
            var hit = CloseupHotspot(go.transform, "Hotspot_Labels", new Vector2(0f, 140f), new Vector2(1240f, 320f));
            AttachGlow(hit, boxes);
            FinishCloseup(go, screen, "field.store.locker_hint",
                new[] { hit }, new[] { "field.store.locker_labels" }, actionIds: new[] { "action_test_017" });
            return screen;
        }

        private static CloseupScreen BuildStoreNoticeCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.03f, 0.04f, 0.06f, 1f), out var go);
            CloseupRect(go.transform, "Glass", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.06f, 0.08f, 0.11f));
            var paper = CloseupGroup(go.transform, "Notice", new Vector2(0f, 150f));
            var p = CloseupRect(paper.transform, "Paper", Vector2.zero, new Vector2(760f, 640f), new Color(0.97f, 0.96f, 0.9f));
            CloseupRect(p.transform, "Head", new Vector2(0f, 280f), new Vector2(760f, 80f), new Color(0.16f, 0.45f, 0.38f));
            CloseupLabel(p.transform, "Title", "field.store.notice_title", 30f, UIFontWeight.Bold, Color.white, new Vector2(0f, 280f), new Vector2(720f, 70f), TextAlignmentOptions.Center);
            CloseupLabel(p.transform, "Body", "field.store.notice_body", 26f, UIFontWeight.Medium, new Color(0.15f, 0.15f, 0.18f), new Vector2(0f, -40f), new Vector2(680f, 480f), TextAlignmentOptions.Top);
            var hit = CloseupHotspot(go.transform, "Hotspot_Notice", new Vector2(0f, 150f), new Vector2(780f, 660f));
            AttachGlow(hit, paper);
            FinishCloseup(go, screen, "field.store.notice_hint",
                new[] { hit }, new[] { "field.store.notice_read" }, actionIds: new[] { "action_test_018" });
            return screen;
        }

        // =========================================================== #005 공원

        private static GameObject[] BuildParkField(Transform parent)
        {
            var root = new GameObject("Park");
            root.transform.SetParent(parent, false);
            AddFieldRect(root.transform, "Sky", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.05f, 0.07f, 0.12f), -10);
            AddFieldCircle(root.transform, "Moon", new Vector2(-6f, 2.6f), 1.1f, new Color(0.9f, 0.9f, 0.82f), -9);
            AddFieldSoft(root.transform, "MoonGlow", new Vector2(-6f, 2.6f), new Vector2(4f, 4f), new Color(0.9f, 0.9f, 0.8f, 0.15f), -9);
            // 뒤쪽 아파트와 오른쪽 상가 건물.
            for (int i = 0; i < 6; i++)
                AddFieldRect(root.transform, "Apt_" + i, new Vector2(-18f + i * 4.2f, 1.2f + (i % 2) * 0.8f), new Vector2(3.6f, 5f), new Color(0.08f, 0.09f, 0.13f), -9);
            AddFieldRect(root.transform, "Shop", new Vector2(11.6f, 0.2f), new Vector2(6f, 7f), new Color(0.22f, 0.2f, 0.2f), -8);
            var vent = new GameObject("Vent"); vent.transform.SetParent(root.transform, false); vent.transform.localPosition = new Vector3(9.4f, -0.4f, 0f);
            AddFieldRect(vent.transform, "Box", Vector2.zero, new Vector2(1.5f, 1.5f), new Color(0.5f, 0.52f, 0.55f), -6);
            AddFieldCircle(vent.transform, "Fan", Vector2.zero, 1.2f, new Color(0.18f, 0.18f, 0.2f), -5);
            var blades = new GameObject("Blades"); blades.transform.SetParent(vent.transform, false);
            AddFieldRect(blades.transform, "BladeA", Vector2.zero, new Vector2(1.0f, 0.2f), new Color(0.45f, 0.46f, 0.48f), -4);
            AddFieldRect(blades.transform, "BladeB", Vector2.zero, new Vector2(0.2f, 1.0f), new Color(0.45f, 0.46f, 0.48f), -4);
            blades.AddComponent<SwingSway>().Spin = -540f;
            AddFieldSoft(root.transform, "Wind", new Vector2(6.2f, -0.8f), new Vector2(4.5f, 1.2f), new Color(0.7f, 0.8f, 0.9f, 0.06f), -5);

            AddFieldRect(root.transform, "Ground", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), new Color(0.32f, 0.28f, 0.22f), -8);
            AddFieldRect(root.transform, "Sand", new Vector2(1.5f, -3.6f), new Vector2(8f, 0.8f), new Color(0.55f, 0.48f, 0.36f), -7);
            AddFieldRect(root.transform, "SandGouge", new Vector2(2.0f, -3.4f), new Vector2(1.2f, 0.12f), new Color(0.4f, 0.34f, 0.25f), -6);
            AddFieldRect(root.transform, "Fence", new Vector2(0f, -2.6f), new Vector2(44f, 0.08f), new Color(0.4f, 0.4f, 0.42f), -7);
            for (int i = 0; i < 30; i++)
                AddFieldRect(root.transform, "Post", new Vector2(-21f + i * 1.45f, -2.95f), new Vector2(0.06f, 0.7f), new Color(0.4f, 0.4f, 0.42f), -7);

            // 그네 틀과 세 자리. 가운데만 흔들린다.
            AddFieldRect(root.transform, "SwingBar", new Vector2(1.5f, 1.6f), new Vector2(6.4f, 0.2f), new Color(0.65f, 0.25f, 0.2f), -6);
            var legL = AddFieldRect(root.transform, "LegL", new Vector2(-1.4f, -1.0f), new Vector2(0.2f, 5.4f), new Color(0.65f, 0.25f, 0.2f), -6);
            legL.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
            var legR = AddFieldRect(root.transform, "LegR", new Vector2(4.4f, -1.0f), new Vector2(0.2f, 5.4f), new Color(0.65f, 0.25f, 0.2f), -6);
            legR.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            GameObject Seat(string n, float x)
            {
                var pivot = new GameObject(n); pivot.transform.SetParent(root.transform, false); pivot.transform.localPosition = new Vector3(x, 1.5f, 0f);
                AddFieldRect(pivot.transform, "ChainL", new Vector2(-0.38f, -1.9f), new Vector2(0.04f, 3.8f), new Color(0.6f, 0.6f, 0.62f), -5);
                AddFieldRect(pivot.transform, "ChainR", new Vector2(0.38f, -1.9f), new Vector2(0.04f, 3.8f), new Color(0.6f, 0.6f, 0.62f), -5);
                AddFieldRect(pivot.transform, "Seat", new Vector2(0f, -3.8f), new Vector2(0.95f, 0.14f), new Color(0.2f, 0.25f, 0.35f), -4);
                return pivot;
            }
            Seat("SwingLeft", -0.4f);
            var moving = Seat("SwingMiddle", 1.5f);
            var sway = moving.AddComponent<SwingSway>(); sway.Angle = 13f; sway.Period = 2.8f; sway.Lean = -4f;
            Seat("SwingRight", 3.4f);
            var still = Seat("SwingMiddleStill", 1.5f);
            still.SetActive(false);

            // 미끄럼틀, 벤치, 가로등, 입구 게시판.
            AddFieldRect(root.transform, "Slide", new Vector2(-6.6f, -2.0f), new Vector2(2.6f, 0.2f), new Color(0.85f, 0.7f, 0.2f), -6);
            AddFieldRect(root.transform, "Bench", new Vector2(6.4f, -2.9f), new Vector2(2.2f, 0.2f), new Color(0.45f, 0.32f, 0.22f), -6);
            var lampPool = new GameObject("Lamp");
            lampPool.transform.SetParent(root.transform, false);
            AddFieldRect(root.transform, "LampPost", new Vector2(-3.8f, -0.4f), new Vector2(0.14f, 6f), new Color(0.3f, 0.3f, 0.32f), -6);
            AddFieldRect(lampPool.transform, "LampHead", new Vector2(-3.6f, 2.6f), new Vector2(0.7f, 0.2f), new Color(1f, 0.9f, 0.6f), -5);
            AddFieldSoft(lampPool.transform, "LampLight", new Vector2(-3.4f, -1.2f), new Vector2(5f, 5.5f), new Color(1f, 0.85f, 0.5f, 0.06f), -5);
            AddFieldRect(root.transform, "BoardPost", new Vector2(-10.4f, -2.2f), new Vector2(0.14f, 2.4f), new Color(0.3f, 0.3f, 0.32f), -6);
            AddFieldRect(root.transform, "Board", new Vector2(-10.4f, -0.3f), new Vector2(2.4f, 1.8f), new Color(0.2f, 0.32f, 0.26f), -6);
            AddFieldRect(root.transform, "BoardPaper", new Vector2(-10.3f, -0.3f), new Vector2(1.1f, 1.3f), new Color(0.95f, 0.94f, 0.88f), -5);
            AddFieldRect(root.transform, "Night", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.02f, 0.03f, 0.08f, 0.35f), -2);

            // 배웅에 놓는 것 셋. 처음엔 숨겨 두고 한영이 놓을 때 드러난다.
            var shoe = new GameObject("Rite_Shoe"); shoe.transform.SetParent(root.transform, false);
            AddFieldRect(shoe.transform, "Sole", new Vector2(1.5f, -3.35f), new Vector2(0.42f, 0.08f), new Color(0.9f, 0.9f, 0.88f), -3);
            AddFieldRect(shoe.transform, "Body", new Vector2(1.45f, -3.24f), new Vector2(0.34f, 0.16f), new Color(0.9f, 0.45f, 0.55f), -3);
            shoe.SetActive(false);
            var tag = new GameObject("Rite_Tag"); tag.transform.SetParent(root.transform, false);
            AddFieldRect(tag.transform, "Card", new Vector2(1.88f, -1.3f), new Vector2(0.22f, 0.28f), new Color(0.98f, 0.95f, 0.75f), -3);
            AddFieldRect(tag.transform, "String", new Vector2(1.88f, -1.08f), new Vector2(0.02f, 0.18f), new Color(0.8f, 0.3f, 0.3f), -3);
            tag.SetActive(false);
            var candleP = new GameObject("Rite_Candle"); candleP.transform.SetParent(root.transform, false);
            AddFieldRect(candleP.transform, "Wax", new Vector2(-3.55f, -3.2f), new Vector2(0.16f, 0.34f), new Color(0.95f, 0.93f, 0.85f), -3);
            AddFieldCircle(candleP.transform, "Flame", new Vector2(-3.55f, -2.95f), 0.14f, new Color(1f, 0.8f, 0.35f), -2);
            AddFieldSoft(candleP.transform, "Glow", new Vector2(-3.55f, -2.9f), new Vector2(3.4f, 3.4f), new Color(1f, 0.75f, 0.35f, 0.22f), -2);
            candleP.SetActive(false);
            // 그네에 앉은 아이. 틀리면 잠깐, 마칠 때는 천천히 드러난다.
            var child = new GameObject("ChildFigure"); child.transform.SetParent(root.transform, false);
            AddFieldCircle(child.transform, "Head", new Vector2(1.5f, -1.45f), 0.62f, new Color(0.82f, 0.86f, 0.92f, 0.55f), -3);
            AddFieldRect(child.transform, "Hair", new Vector2(1.5f, -1.22f), new Vector2(0.62f, 0.2f), new Color(0.25f, 0.25f, 0.3f, 0.55f), -2);
            AddFieldRect(child.transform, "Body", new Vector2(1.5f, -2.0f), new Vector2(0.55f, 0.75f), new Color(0.95f, 0.75f, 0.8f, 0.5f), -3);
            AddFieldRect(child.transform, "Legs", new Vector2(1.5f, -2.55f), new Vector2(0.36f, 0.4f), new Color(0.82f, 0.86f, 0.92f, 0.45f), -3);
            child.SetActive(false);

            BuildFieldActors(root.transform, -3.86f, -9.4f, 2.4f, -11f, 10.6f);

            var riteSwing = BuildPoint(root.transform, "InvestigationPoint_PRiteSwing", new Vector2(1.0f, -3.35f), new Vector2(1.0f, 0.6f),
                new Color(0.9f, 0.6f, 0.7f), "field.park.rite_swing", "field.park.rite_swing", null);
            var riteChain = BuildPoint(root.transform, "InvestigationPoint_PRiteChain", new Vector2(2.3f, -0.4f), new Vector2(0.7f, 2.4f),
                new Color(0.9f, 0.9f, 0.6f), "field.park.rite_chain", "field.park.rite_chain", null);
            var riteLamp = BuildPoint(root.transform, "InvestigationPoint_PRiteLamp", new Vector2(-3.8f, -1.2f), new Vector2(1.0f, 4.4f),
                new Color(1f, 0.85f, 0.5f), "field.park.rite_lamp", "field.park.rite_lamp", null);
            SetHighlightTargets(riteLamp, root.transform, "LampPost");

            var swingPoint = BuildPoint(root.transform, "InvestigationPoint_ParkSwing", new Vector2(1.5f, -1.0f), new Vector2(2.2f, 5.0f),
                new Color(0.6f, 0.5f, 0.6f), "field.park.swing", "field.park.swing", null);
            var ventPoint = BuildPoint(root.transform, "InvestigationPoint_ParkVent", new Vector2(9.4f, -0.4f), new Vector2(1.8f, 1.8f),
                new Color(0.6f, 0.6f, 0.6f), "field.park.vent", "field.park.vent", null);
            var boardPoint = BuildPoint(root.transform, "InvestigationPoint_ParkBoard", new Vector2(-10.4f, -0.3f), new Vector2(2.6f, 2.0f),
                new Color(0.5f, 0.7f, 0.6f), "field.park.board", "field.park.board", null);
            SetHighlightTargets(swingPoint, root.transform, "SwingMiddle");
            SetHighlightTargets(ventPoint, root.transform, "Vent");
            SetHighlightTargets(boardPoint, root.transform, "Board", "BoardPaper");

            // --- 무서운 연출
            AddScare(root.transform, "park_stop", FieldScare.TriggerKind.ReachX, -1.2f, null, 0f,
                SHide(moving), SShow(still), SSound("heartbeat", 0.45f), SWait(1.6f),
                SLines("field.park.scare.stop.001", "!field.park.scare.stop.002"), SSound("air_rush_pass", 0.35f), SWait(0.4f), SHide(still), SShow(moving));
            AddScare(root.transform, "park_laugh", FieldScare.TriggerKind.AfterPoint, 0f, "point_park_vent", 0f,
                SWait(0.5f), SSound("ghost_whisper_0", 0.45f), SWait(1.0f),
                SLines("field.park.scare.laugh.001", "!field.park.scare.laugh.002"));
            return new[] { swingPoint, ventPoint, boardPoint, riteSwing, riteChain, riteLamp };
        }

        private static CloseupScreen BuildParkSwingCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.03f, 0.04f, 0.07f, 1f), out var go);
            CloseupRect(go.transform, "Sky", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.07f, 0.09f, 0.14f));
            CloseupRect(go.transform, "Bar", new Vector2(-300f, 470f), new Vector2(900f, 40f), new Color(0.65f, 0.25f, 0.2f));
            var hook = CloseupGroup(go.transform, "Hook", new Vector2(-300f, 400f));
            CloseupRect(hook.transform, "Ring", Vector2.zero, new Vector2(110f, 70f), new Color(0.55f, 0.52f, 0.5f));
            CloseupRect(hook.transform, "Wear", new Vector2(30f, -10f), new Vector2(40f, 30f), new Color(0.6f, 0.35f, 0.2f));
            CloseupRect(hook.transform, "Chain", new Vector2(0f, -200f), new Vector2(10f, 340f), new Color(0.6f, 0.6f, 0.62f));
            CloseupRect(go.transform, "Hook2", new Vector2(200f, 400f), new Vector2(110f, 70f), new Color(0.55f, 0.52f, 0.5f));
            CloseupRect(go.transform, "Chain2", new Vector2(200f, 200f), new Vector2(10f, 340f), new Color(0.6f, 0.6f, 0.62f));
            CloseupRect(go.transform, "SwingSeat", new Vector2(-50f, 20f), new Vector2(560f, 40f), new Color(0.2f, 0.25f, 0.35f));
            var hookHit = CloseupHotspot(go.transform, "Hotspot_Hook", new Vector2(-300f, 330f), new Vector2(240f, 300f));
            AttachGlow(hookHit, hook);
            CloseupRect(go.transform, "Sand", new Vector2(0f, -190f), new Vector2(1920f, 240f), new Color(0.55f, 0.48f, 0.36f));
            var gouge = CloseupGroup(go.transform, "Gouge", new Vector2(320f, -190f));
            CloseupRect(gouge.transform, "Mark", Vector2.zero, new Vector2(520f, 60f), new Color(0.4f, 0.34f, 0.25f));
            CloseupRect(gouge.transform, "Mark2", new Vector2(-60f, 40f), new Vector2(360f, 20f), new Color(0.44f, 0.38f, 0.28f));
            var sandHit = CloseupHotspot(go.transform, "Hotspot_Sand", new Vector2(320f, -190f), new Vector2(580f, 160f));
            AttachGlow(sandHit, gouge);
            FinishCloseup(go, screen, "field.park.swing_hint",
                new[] { hookHit, sandHit },
                new[] { "field.park.swing_hook", "field.park.swing_sand" },
                actionIds: new[] { "action_test_020", "action_test_023" });
            return screen;
        }

        private static CloseupScreen BuildParkVentCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.05f, 0.05f, 0.06f, 1f), out var go);
            CloseupRect(go.transform, "Wall", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.24f, 0.22f, 0.22f));
            var vent = CloseupGroup(go.transform, "Vent", new Vector2(-200f, 140f));
            CloseupRect(vent.transform, "Box", Vector2.zero, new Vector2(620f, 620f), new Color(0.5f, 0.52f, 0.55f));
            var fan = CloseupRect(vent.transform, "Fan", Vector2.zero, new Vector2(500f, 500f), new Color(0.15f, 0.15f, 0.17f));
            fan.sprite = RoundSprite(); fan.type = Image.Type.Simple;
            var sticker = CloseupRect(vent.transform, "Sticker", new Vector2(240f, -230f), new Vector2(300f, 140f), new Color(0.95f, 0.85f, 0.25f));
            CloseupLabel(sticker.transform, "Text", "field.park.vent_sticker", 24f, UIFontWeight.Bold, new Color(0.2f, 0.15f, 0.05f), Vector2.zero, new Vector2(280f, 120f), TextAlignmentOptions.Center);
            var hit = CloseupHotspot(go.transform, "Hotspot_Sticker", new Vector2(40f, -90f), new Vector2(340f, 180f));
            AttachGlow(hit, sticker.gameObject);
            FinishCloseup(go, screen, "field.park.vent_hint",
                new[] { hit }, new[] { "field.park.vent_read" }, actionIds: new[] { "action_test_021" });
            return screen;
        }

        private static CloseupScreen BuildParkBoardCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.04f, 0.06f, 0.05f, 1f), out var go);
            CloseupRect(go.transform, "Board", new Vector2(0f, 140f), new Vector2(1500f, 800f), new Color(0.2f, 0.32f, 0.26f));
            var paper = CloseupGroup(go.transform, "Notice", new Vector2(0f, 140f));
            var p = CloseupRect(paper.transform, "Paper", Vector2.zero, new Vector2(760f, 640f), new Color(0.97f, 0.96f, 0.9f));
            CloseupRect(p.transform, "Head", new Vector2(0f, 280f), new Vector2(760f, 80f), new Color(0.2f, 0.32f, 0.5f));
            CloseupLabel(p.transform, "Title", "field.park.board_title", 30f, UIFontWeight.Bold, Color.white, new Vector2(0f, 280f), new Vector2(720f, 70f), TextAlignmentOptions.Center);
            CloseupLabel(p.transform, "Body", "field.park.board_body", 26f, UIFontWeight.Medium, new Color(0.15f, 0.15f, 0.18f), new Vector2(0f, -40f), new Vector2(680f, 480f), TextAlignmentOptions.Top);
            var hit = CloseupHotspot(go.transform, "Hotspot_Notice", new Vector2(0f, 140f), new Vector2(780f, 660f));
            AttachGlow(hit, paper);
            FinishCloseup(go, screen, "field.park.board_hint",
                new[] { hit }, new[] { "field.park.board_read" }, actionIds: new[] { "action_test_022" });
            return screen;
        }
    }
}
