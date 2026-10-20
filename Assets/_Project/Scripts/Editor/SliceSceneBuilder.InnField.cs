using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UrbanLegendBureau.Systems;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 사건 #006 해월장 304호 — 나폴리탄. 구도시의 낡은 여관. 3층 복도 → 304호(갇힘).
    /// 돌려주는 지점: 카운터, 304호 문, 수칙서, 침대, 거울, 방문, 전화, 창문.
    /// </summary>
    public static partial class SliceSceneBuilder
    {
        private static GameObject[] BuildInnField(Transform parent)
        {
            var hall = BuildInnHall(parent, out var desk, out var door304);
            var room = BuildInnRoom(parent, out var rules, out var bed, out var mirror, out var door, out var phone, out var window);
            var swap = parent.gameObject.AddComponent<FieldSceneSwap>();
            var so = new SerializedObject(swap);
            so.Update();
            SetObjectArray(so.FindProperty("_beforeRoots"), hall);
            SetObjectArray(so.FindProperty("_afterRoots"), room);
            so.ApplyModifiedPropertiesWithoutUndo();
            return new[] { desk, door304, rules, bed, mirror, door, phone, window };
        }

        private static GameObject BuildInnHall(Transform parent, out GameObject deskPoint, out GameObject doorPoint)
        {
            var root = new GameObject("InnHall");
            root.transform.SetParent(parent, false);
            var wall = new Color(0.30f, 0.22f, 0.20f);
            var shadow = new Color(0f, 0f, 0f, 0.45f);
            // 낡은 여관 복도. 꽃무늬가 바랜 붉은 벽지, 붉은 카펫, 노란 벽등 몇 개.
            AddFieldRect(root.transform, "Wall", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), wall, -9);
            for (int i = 0; i < 60; i++)
                AddFieldCircle(root.transform, "Flower_" + i, new Vector2(-21f + (i % 30) * 1.45f, 2.2f - (i / 30) * 2.2f), 0.3f, new Color(0.38f, 0.26f, 0.22f), -8);
            AddFieldRect(root.transform, "Wainscot", new Vector2(0f, -2.3f), new Vector2(44f, 2.2f), new Color(0.22f, 0.15f, 0.12f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.5f), new Vector2(44f, 1f), new Color(0.12f, 0.1f, 0.1f), -8);
            AddFieldRect(root.transform, "Carpet", new Vector2(0f, -4.5f), new Vector2(44f, 2.2f), new Color(0.38f, 0.1f, 0.1f), -8);
            AddFieldRect(root.transform, "CarpetEdge", new Vector2(0f, -3.45f), new Vector2(44f, 0.1f), new Color(0.6f, 0.45f, 0.2f), -7);
            foreach (var x in new[] { -9f, -1f, 7f })
            {
                AddFieldRect(root.transform, "Sconce_" + x, new Vector2(x, 1.6f), new Vector2(0.4f, 0.5f), new Color(0.95f, 0.8f, 0.45f), -6);
                AddFieldSoft(root.transform, "SconceGlow_" + x, new Vector2(x, 1.2f), new Vector2(3.2f, 3.6f), new Color(1f, 0.75f, 0.35f, 0.12f), -6);
            }
            // 왼쪽 계단참의 작은 카운터(야간 무인). 숙박부와 메모 카드.
            AddFieldRect(root.transform, "Counter", new Vector2(-11.2f, -2.4f), new Vector2(3.4f, 1.8f), new Color(0.32f, 0.2f, 0.14f), -6);
            AddFieldRect(root.transform, "CounterTop", new Vector2(-11.2f, -1.45f), new Vector2(3.6f, 0.14f), new Color(0.5f, 0.35f, 0.22f), -5);
            AddFieldRect(root.transform, "Ledger", new Vector2(-11.6f, -1.32f), new Vector2(1.0f, 0.1f), new Color(0.75f, 0.7f, 0.6f), -4);
            AddFieldRect(root.transform, "Bell", new Vector2(-10.4f, -1.3f), new Vector2(0.3f, 0.2f), new Color(0.8f, 0.7f, 0.35f), -4);
            AddFieldRect(root.transform, "KeyBoard", new Vector2(-11.2f, 0.8f), new Vector2(2.4f, 1.6f), new Color(0.25f, 0.17f, 0.12f), -6);
            for (int k = 0; k < 8; k++)
                AddFieldRect(root.transform, "Key_" + k, new Vector2(-12.1f + (k % 4) * 0.6f, 1.2f - (k / 4) * 0.7f), new Vector2(0.08f, 0.3f), k == 5 ? new Color(0.2f, 0.17f, 0.12f) : new Color(0.75f, 0.65f, 0.35f), -5);
            // 객실 문 넷(301~304). 304 호는 맨 오른쪽, 문틈으로 빛이 없다.
            for (int n = 1; n <= 4; n++)
            {
                float x = -6f + (n - 1) * 5.0f;
                AddFieldSoft(root.transform, "Shadow_" + n, new Vector2(x, -3.42f), new Vector2(2.2f, 0.3f), shadow, -6);
                AddFieldRect(root.transform, "Frame_" + n, new Vector2(x, -0.85f), new Vector2(1.9f, 5.0f), new Color(0.2f, 0.13f, 0.1f), -6);
                AddFieldRect(root.transform, "Door_" + n, new Vector2(x, -0.9f), new Vector2(1.7f, 4.8f), new Color(0.45f, 0.28f, 0.2f), -5);
                AddFieldCircle(root.transform, "Knob_" + n, new Vector2(x + 0.6f, -1.0f), 0.18f, new Color(0.8f, 0.7f, 0.4f), -4);
                AddFieldRect(root.transform, "Plate_" + n, new Vector2(x, 1.0f), new Vector2(0.7f, 0.36f), new Color(0.8f, 0.7f, 0.4f), -4);
                var num = AddFieldWorldText(root.transform, "Num_" + n, new Vector2(x, 1.0f), new Vector2(0.66f, 0.34f), "field.inn.room_" + n, new Color(0.2f, 0.12f, 0.08f), -3);
                num.fontSizeMax = 2.2f;
                if (n != 4) AddFieldRect(root.transform, "Gap_" + n, new Vector2(x, -3.28f), new Vector2(1.4f, 0.05f), new Color(1f, 0.8f, 0.5f, 0.5f), -4);
            }
            var tray = AddFieldRect(root.transform, "Tray", new Vector2(8.3f, -3.3f), new Vector2(0.9f, 0.12f), new Color(0.7f, 0.7f, 0.68f), -4);
            AddFieldRect(root.transform, "Night", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.04f, 0.02f, 0.04f, 0.45f), -2);

            BuildFieldActors(root.transform, -3.86f, -8.4f, 2.4f, -10.4f, 10.4f);

            deskPoint = BuildPoint(root.transform, "InvestigationPoint_InnDesk", new Vector2(-11.2f, -1.0f), new Vector2(3.6f, 3.4f),
                new Color(0.6f, 0.45f, 0.3f), "field.inn.desk", "field.inn.desk", null);
            doorPoint = BuildPoint(root.transform, "InvestigationPoint_InnDoor304", new Vector2(9.0f, -0.9f), new Vector2(1.9f, 5.0f),
                new Color(0.6f, 0.3f, 0.3f), "field.inn.door304", "field.inn.door304", null);
            SetHighlightTargets(deskPoint, root.transform, "Counter", "CounterTop", "Ledger");
            SetHighlightTargets(doorPoint, root.transform, "Door_4");

            // --- 무서운 연출: 301호 앞 쟁반 위 그릇이 저절로 미끄러진다. 맞은편 열쇠 하나가 흔들린다.
            var key5 = root.transform.Find("Key_5").gameObject;
            AddScare(root.transform, "inn_tray", FieldScare.TriggerKind.ReachX, 4.0f, null, 0f,
                SSound("door_bang_2", 0.25f, 1.6f), SMove(tray, new Vector2(0.4f, 0f), 0.6f), SWait(0.6f),
                SLines("field.inn.scare.tray.001", "!field.inn.scare.tray.002"));
            AddScare(root.transform, "inn_key", FieldScare.TriggerKind.AfterPoint, 0f, "point_inn_desk", 0f,
                SWait(0.4f), SSound("key_3", 0.4f), SShake(key5, 0.8f, 0.03f), SWait(0.5f),
                SLines("field.inn.scare.key.001", "!field.inn.scare.key.002"));
            return root;
        }

        private static GameObject BuildInnRoom(Transform parent, out GameObject rulesPoint, out GameObject bedPoint, out GameObject mirrorPoint,
            out GameObject doorPoint, out GameObject phonePoint, out GameObject windowPoint)
        {
            var root = new GameObject("InnRoom");
            root.transform.SetParent(parent, false);
            const float half = 6.4f;
            AddFieldRect(root.transform, "Outside", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.01f, 0.005f, 0.01f), -10);
            AddFieldRect(root.transform, "Wall", new Vector2(0f, -0.2f), new Vector2(2f * half, 7.2f), new Color(0.34f, 0.27f, 0.24f), -9);
            for (int i = 0; i < 16; i++)
                AddFieldCircle(root.transform, "Flower_" + i, new Vector2(-half + 0.6f + (i % 8) * 1.6f, 2.0f - (i / 8) * 2.2f), 0.26f, new Color(0.4f, 0.31f, 0.27f), -8);
            AddFieldRect(root.transform, "Floor", new Vector2(0f, -4.3f), new Vector2(2f * half, 1.8f), new Color(0.35f, 0.12f, 0.12f), -8);
            AddFieldRect(root.transform, "Ceiling", new Vector2(0f, 3.5f), new Vector2(2f * half + 1f, 0.8f), new Color(0.1f, 0.08f, 0.08f), -8);
            AddFieldRect(root.transform, "Edge_L", new Vector2(-half - 0.25f, -0.3f), new Vector2(0.5f, 7.4f), new Color(0.12f, 0.08f, 0.07f), -8);
            AddFieldRect(root.transform, "Edge_R", new Vector2(half + 0.25f, -0.3f), new Vector2(0.5f, 7.4f), new Color(0.12f, 0.08f, 0.07f), -8);

            // 왼쪽 방문(잠김). 문 위 벽시계는 03:33 에 멈춰 있다.
            AddFieldRect(root.transform, "Door", new Vector2(-5.3f, -0.9f), new Vector2(1.7f, 4.8f), new Color(0.45f, 0.28f, 0.2f), -7);
            AddFieldCircle(root.transform, "Knob", new Vector2(-4.7f, -1.0f), 0.18f, new Color(0.8f, 0.7f, 0.4f), -6);
            AddFieldRect(root.transform, "ClockBox", new Vector2(-5.3f, 2.2f), new Vector2(1.3f, 0.5f), new Color(0.08f, 0.06f, 0.06f), -6);
            var clock = AddFieldWorldText(root.transform, "Clock", new Vector2(-5.3f, 2.2f), new Vector2(1.2f, 0.45f), "field.inn.clock", new Color(1f, 0.3f, 0.22f), -5);
            clock.fontSizeMax = 3f;
            Object.DestroyImmediate(clock.GetComponent<LocalizedText>());
            clock.text = "03:33";
            var knock = new GameObject("Trap_Knock"); knock.transform.SetParent(root.transform, false); knock.SetActive(false);
            // 화장대 거울.
            AddFieldRect(root.transform, "Vanity", new Vector2(-2.4f, -2.6f), new Vector2(1.8f, 1.4f), new Color(0.38f, 0.24f, 0.17f), -6);
            AddFieldRect(root.transform, "MirrorFrame", new Vector2(-2.4f, 0.0f), new Vector2(1.6f, 2.4f), new Color(0.5f, 0.38f, 0.22f), -6);
            AddFieldRect(root.transform, "Mirror", new Vector2(-2.4f, 0.0f), new Vector2(1.4f, 2.2f), new Color(0.38f, 0.42f, 0.46f), -5);
            var face = new GameObject("MirrorFace"); face.transform.SetParent(root.transform, false);
            AddFieldCircle(face.transform, "Head", new Vector2(-2.4f, 0.4f), 0.9f, new Color(0.85f, 0.85f, 0.8f, 0.6f), -1);
            AddFieldRect(face.transform, "EyeL", new Vector2(-2.6f, 0.5f), new Vector2(0.12f, 0.16f), new Color(0f, 0f, 0f, 0.9f), 0);
            AddFieldRect(face.transform, "EyeR", new Vector2(-2.2f, 0.5f), new Vector2(0.12f, 0.16f), new Color(0f, 0f, 0f, 0.9f), 0);
            AddFieldRect(face.transform, "Mouth", new Vector2(-2.4f, 0.12f), new Vector2(0.5f, 0.08f), new Color(0f, 0f, 0f, 0.85f), 0);
            face.SetActive(false);
            var towel = AddFieldRect(root.transform, "Trap_Towel", new Vector2(-2.4f, 0.0f), new Vector2(1.5f, 2.3f), new Color(0.88f, 0.88f, 0.84f), -2);
            towel.SetActive(false);
            // 액자처럼 걸린 안전 수칙서.
            AddFieldRect(root.transform, "RulesFrame", new Vector2(0.2f, 1.0f), new Vector2(1.4f, 1.8f), new Color(0.3f, 0.22f, 0.15f), -6);
            AddFieldRect(root.transform, "RulesPaper", new Vector2(0.2f, 1.0f), new Vector2(1.2f, 1.6f), new Color(0.92f, 0.9f, 0.82f), -5);
            for (int l = 0; l < 6; l++)
                AddFieldRect(root.transform, "RulesLine", new Vector2(0.2f, 1.5f - l * 0.22f), new Vector2(0.9f, 0.04f), l % 3 == 2 ? new Color(0.75f, 0.15f, 0.12f) : new Color(0.2f, 0.2f, 0.2f), -4);
            // 침대와 협탁의 전화.
            AddFieldRect(root.transform, "Bed", new Vector2(2.6f, -2.75f), new Vector2(3.2f, 0.9f), new Color(0.55f, 0.5f, 0.45f), -6);
            AddFieldRect(root.transform, "Blanket", new Vector2(2.9f, -2.45f), new Vector2(2.4f, 0.35f), new Color(0.45f, 0.15f, 0.15f), -5);
            AddFieldRect(root.transform, "Headboard", new Vector2(1.0f, -1.9f), new Vector2(0.2f, 1.8f), new Color(0.35f, 0.22f, 0.15f), -5);
            AddFieldRect(root.transform, "Nightstand", new Vector2(4.6f, -2.8f), new Vector2(0.9f, 1.0f), new Color(0.35f, 0.22f, 0.15f), -6);
            AddFieldRect(root.transform, "Phone", new Vector2(4.6f, -2.1f), new Vector2(0.7f, 0.3f), new Color(0.65f, 0.6f, 0.5f), -5);
            var phoneOff = AddFieldRect(root.transform, "Trap_PhoneOff", new Vector2(4.6f, -1.9f), new Vector2(0.75f, 0.12f), new Color(0.25f, 0.22f, 0.2f), -4);
            phoneOff.SetActive(false);
            // 오른쪽 창, 두꺼운 커튼. 걷으면 새벽빛.
            AddFieldRect(root.transform, "WindowFrame", new Vector2(5.4f, 1.0f), new Vector2(1.6f, 2.4f), new Color(0.3f, 0.22f, 0.15f), -7);
            AddFieldRect(root.transform, "Glass", new Vector2(5.4f, 1.0f), new Vector2(1.4f, 2.2f), new Color(0.05f, 0.05f, 0.08f), -6);
            var curtainClosed = AddFieldRect(root.transform, "Curtain", new Vector2(5.4f, 1.0f), new Vector2(1.7f, 2.6f), new Color(0.45f, 0.12f, 0.12f), -4);
            var dawn = new GameObject("Trap_CurtainOpen"); dawn.transform.SetParent(root.transform, false);
            AddFieldRect(dawn.transform, "Dawn", new Vector2(5.4f, 1.0f), new Vector2(1.4f, 2.2f), new Color(0.55f, 0.62f, 0.75f), -3);
            AddFieldRect(dawn.transform, "CurtainL", new Vector2(4.55f, 1.0f), new Vector2(0.35f, 2.6f), new Color(0.45f, 0.12f, 0.12f), -3);
            AddFieldSoft(dawn.transform, "Light", new Vector2(4.4f, 0f), new Vector2(5f, 5f), new Color(0.7f, 0.8f, 1f, 0.15f), -3);
            dawn.SetActive(false);

            AddFieldRect(root.transform, "Dim", new Vector2(0f, 0.2f), new Vector2(44f, 10.8f), new Color(0.03f, 0.01f, 0.03f, 0.4f), -2);
            AddFieldGradient(root.transform, "Close_L", new Vector2(-half + 0.9f, -0.3f), new Vector2(1.8f, 7f), new Color(0f, 0f, 0f, 0.5f), 15, 90f);
            AddFieldGradient(root.transform, "Close_R", new Vector2(half - 0.9f, -0.3f), new Vector2(1.8f, 7f), new Color(0f, 0f, 0f, 0.5f), 15, -90f);

            BuildFieldActors(root.transform, -3.86f, -3.6f, 1.9f, -4.6f, 4.2f);

            rulesPoint = BuildPoint(root.transform, "InvestigationPoint_InnRules", new Vector2(0.2f, 1.0f), new Vector2(1.5f, 1.9f),
                new Color(0.8f, 0.75f, 0.6f), "field.inn.rules", "field.inn.rules", null);
            bedPoint = BuildPoint(root.transform, "InvestigationPoint_InnBed", new Vector2(2.6f, -2.6f), new Vector2(3.2f, 1.2f),
                new Color(0.6f, 0.5f, 0.45f), "field.inn.bed", "field.inn.bed", null);
            mirrorPoint = BuildPoint(root.transform, "InvestigationPoint_TrapMirror", new Vector2(-2.4f, 0.0f), new Vector2(1.7f, 2.5f),
                new Color(0.6f, 0.65f, 0.7f), "field.inn.mirror", "field.inn.mirror", null);
            doorPoint = BuildPoint(root.transform, "InvestigationPoint_TrapDoor", new Vector2(-5.3f, -0.9f), new Vector2(1.8f, 4.8f),
                new Color(0.6f, 0.4f, 0.3f), "field.inn.door", "field.inn.door", null);
            phonePoint = BuildPoint(root.transform, "InvestigationPoint_TrapPhone", new Vector2(4.6f, -2.2f), new Vector2(1.0f, 0.8f),
                new Color(0.7f, 0.65f, 0.5f), "field.inn.phone", "field.inn.phone", null);
            windowPoint = BuildPoint(root.transform, "InvestigationPoint_TrapWindow", new Vector2(5.4f, 1.0f), new Vector2(1.8f, 2.7f),
                new Color(0.6f, 0.3f, 0.3f), "field.inn.window", "field.inn.window", null);
            SetHighlightTargets(rulesPoint, root.transform, "RulesFrame", "RulesPaper");
            SetHighlightTargets(bedPoint, root.transform, "Bed", "Blanket");
            SetHighlightTargets(mirrorPoint, root.transform, "MirrorFrame", "Mirror");
            SetHighlightTargets(doorPoint, root.transform, "Door");
            SetHighlightTargets(phonePoint, root.transform, "Phone");
            SetHighlightTargets(windowPoint, root.transform, "Curtain");
            return root;
        }

        /// <summary>304호 안전 수칙서. 검은 인쇄 규칙 사이에 빨간 손글씨 규칙이 끼어 있다.</summary>
        private static CloseupScreen BuildInnRulesCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.08f, 0.05f, 0.05f, 1f), out var go);
            CloseupRect(go.transform, "Wall", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.3f, 0.22f, 0.2f));
            var paper = CloseupGroup(go.transform, "Rules", new Vector2(0f, 140f));
            CloseupRect(paper.transform, "Frame", Vector2.zero, new Vector2(1000f, 760f), new Color(0.3f, 0.2f, 0.14f));
            var p = CloseupRect(paper.transform, "Paper", Vector2.zero, new Vector2(940f, 700f), new Color(0.93f, 0.91f, 0.84f));
            CloseupLabel(p.transform, "Title", "field.inn.rules_title", 32f, UIFontWeight.Bold, new Color(0.12f, 0.1f, 0.1f), new Vector2(0f, 300f), new Vector2(880f, 60f), TextAlignmentOptions.Center);
            CloseupLabel(p.transform, "Body", "field.inn.rules_body", 24f, UIFontWeight.Medium, new Color(0.12f, 0.1f, 0.1f), new Vector2(0f, -30f), new Vector2(860f, 580f), TextAlignmentOptions.TopLeft);
            var hit = CloseupHotspot(go.transform, "Hotspot_Rules", new Vector2(0f, 140f), new Vector2(1000f, 760f));
            AttachGlow(hit, paper);
            FinishCloseup(go, screen, "field.inn.rules_hint", new[] { hit }, new[] { "field.inn.rules_read" }, actionIds: new[] { "action_test_026" });
            return screen;
        }

        /// <summary>침대 매트리스 밑의 쪽지. 이전 투숙객이 남겼다.</summary>
        private static CloseupScreen BuildInnBedCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.06f, 0.04f, 0.04f, 1f), out var go);
            CloseupRect(go.transform, "Mattress", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.55f, 0.5f, 0.45f));
            for (int i = 0; i < 10; i++) CloseupRect(go.transform, "Quilt", new Vector2(-900f + i * 200f, 120f), new Vector2(3f, 860f), new Color(0.48f, 0.43f, 0.38f));
            var note = CloseupGroup(go.transform, "Note", new Vector2(80f, 100f));
            var n = CloseupRect(note.transform, "Paper", Vector2.zero, new Vector2(640f, 480f), new Color(0.96f, 0.95f, 0.88f));
            n.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -5f);
            CloseupLabel(n.transform, "Text", "field.inn.note_text", 28f, UIFontWeight.Medium, new Color(0.15f, 0.15f, 0.25f), Vector2.zero, new Vector2(580f, 420f), TextAlignmentOptions.TopLeft);
            var hit = CloseupHotspot(go.transform, "Hotspot_Note", new Vector2(80f, 100f), new Vector2(660f, 500f));
            AttachGlow(hit, note);
            FinishCloseup(go, screen, "field.inn.bed_hint", new[] { hit }, new[] { "field.inn.note_read" }, actionIds: new[] { "action_test_027" });
            return screen;
        }

        /// <summary>1층 카운터. 숙박부와 야간 안내 카드.</summary>
        private static CloseupScreen BuildInnDeskCloseup(string name)
        {
            var screen = NewCloseup(name, new Color(0.06f, 0.04f, 0.03f, 1f), out var go);
            CloseupRect(go.transform, "Top", new Vector2(0f, 120f), new Vector2(1920f, 860f), new Color(0.42f, 0.28f, 0.18f));
            var ledger = CloseupGroup(go.transform, "Ledger", new Vector2(-330f, 140f));
            CloseupRect(ledger.transform, "Paper", Vector2.zero, new Vector2(820f, 620f), new Color(0.92f, 0.9f, 0.82f));
            CloseupLabel(ledger.transform, "Text", "field.inn.ledger_text", 24f, UIFontWeight.Medium, new Color(0.15f, 0.12f, 0.1f), Vector2.zero, new Vector2(760f, 560f), TextAlignmentOptions.TopLeft);
            var ledgerHit = CloseupHotspot(go.transform, "Hotspot_Ledger", new Vector2(-330f, 140f), new Vector2(840f, 640f));
            AttachGlow(ledgerHit, ledger);
            var card = CloseupGroup(go.transform, "Card", new Vector2(520f, 160f));
            var c = CloseupRect(card.transform, "Paper", Vector2.zero, new Vector2(420f, 300f), new Color(0.97f, 0.95f, 0.85f));
            c.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 4f);
            CloseupLabel(c.transform, "Text", "field.inn.card_text", 26f, UIFontWeight.SemiBold, new Color(0.3f, 0.12f, 0.1f), Vector2.zero, new Vector2(380f, 260f), TextAlignmentOptions.Center);
            var cardHit = CloseupHotspot(go.transform, "Hotspot_Card", new Vector2(520f, 160f), new Vector2(440f, 320f));
            AttachGlow(cardHit, card);
            FinishCloseup(go, screen, "field.inn.desk_hint", new[] { ledgerHit, cardHit },
                new[] { "field.inn.ledger_read", "field.inn.card_read" }, actionIds: new[] { "action_test_024", "action_test_025" });
            return screen;
        }
    }
}
