using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 인물 그림을 찾아 준다. 지금은 한영 한 사람이다.
    ///
    /// 그림은 Resources/Characters/Hanyoung 아래에 포즈 이름으로 있다.
    ///   전신   - Characters/Hanyoung/{포즈}       대화 화면에 세운다
    ///   초상   - Characters/Hanyoung/Portrait/{표정}  현장 대사 띠(타입 2)의 초상 칸에 넣는다
    ///
    /// 어느 대사에 어느 포즈를 쓰는지는 같은 폴더의 poses.txt 에 "대사ID[탭]포즈" 로 적는다.
    /// 초상은 따로 그린 얼굴 그림이라 표정 이름으로 고른다. portraits.txt 에 "대사ID[탭]표정" 으로 적는다.
    /// 적혀 있지 않은 대사는 default 포즈다. 포즈를 바꾸려면 그 파일만 고치면 된다. 코드는 그대로다.
    /// </summary>
    public static class CharacterArt
    {
        /// <summary>한영의 이름 ID. 현장 대사 띠가 말하는 사람을 이것으로 알아본다.</summary>
        public const string HanyoungNameTextId = "tutorial.char.hanyoung";

        public const string DefaultPose = "default";

        private const string Root = "Characters/Hanyoung/";
        private const string PoseTable = Root + "poses";

        private const string PortraitTable = Root + "portraits";

        // 차지한. 대사 화면 오른쪽에 선다. 그림은 Characters/Chajihan/{포즈}, 표는 같은 폴더의 poses.txt.
        // 한영과 달리 표에 적힌 대사에서만 모습을 바꾼다. 적지 않은 대사에서는 하던 모습 그대로다.
        // 한영이 말하는 대사도 적을 수 있다. 듣는 동안의 표정이다.
        private const string ChajihanRoot = "Characters/Chajihan/";
        public const string ChajihanDefaultPose = "stand_b";
        private static Dictionary<string, string> _chajihanPoses;
        public const string DefaultPortrait = "neutral";

        private static Dictionary<string, string> _poses;
        private static Dictionary<string, string> _portraits;
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>그 대사에 쓸 한영의 포즈 이름. 적혀 있지 않으면 default.</summary>
        public static string PoseFor(string lineTextId)
        {
            if (_poses == null) LoadPoses();
            if (!string.IsNullOrEmpty(lineTextId) && _poses.TryGetValue(lineTextId, out var pose)) return pose;
            return DefaultPose;
        }

        /// <summary>그 대사에 맞는 한영의 전신 그림.</summary>
        public static Sprite HanyoungBody(string lineTextId)
        {
            return Load(Root + PoseFor(lineTextId)) ?? Load(Root + DefaultPose);
        }

        /// <summary>그 대사에서 차지한이 바꿀 모습. 표에 없으면 null 이다. 그때는 하던 모습을 그대로 둔다.</summary>
        public static Sprite ChajihanBody(string lineTextId)
        {
            if (_chajihanPoses == null) _chajihanPoses = LoadTable(ChajihanRoot + "poses");
            if (string.IsNullOrEmpty(lineTextId) || !_chajihanPoses.TryGetValue(lineTextId, out var pose)) return null;
            return Load(ChajihanRoot + pose);
        }

        /// <summary>차지한의 처음 모습.</summary>
        public static Sprite ChajihanDefault()
        {
            return Load(ChajihanRoot + ChajihanDefaultPose);
        }

        /// <summary>그 대사에 쓸 한영의 표정 이름. 적혀 있지 않으면 neutral.</summary>
        public static string PortraitFor(string lineTextId)
        {
            if (_portraits == null) _portraits = LoadTable(PortraitTable);
            if (!string.IsNullOrEmpty(lineTextId) && _portraits.TryGetValue(lineTextId, out var face)) return face;
            return DefaultPortrait;
        }

        /// <summary>그 대사에 맞는 한영의 초상. 현장 대사 띠의 초상 칸에 쓴다.</summary>
        public static Sprite HanyoungFace(string lineTextId)
        {
            return Load(Root + "Portrait/" + PortraitFor(lineTextId)) ?? Load(Root + "Portrait/" + DefaultPortrait);
        }

        private static void LoadPoses()
        {
            _poses = LoadTable(PoseTable);
        }

        /// <summary>"대사ID[탭]이름" 꼴의 표를 읽는다. # 으로 시작하는 줄과 빈 줄은 건너뛴다.</summary>
        private static Dictionary<string, string> LoadTable(string path)
        {
            var result = new Dictionary<string, string>();

            var table = Resources.Load<TextAsset>(path);
            if (table == null) return result;

            foreach (var raw in table.text.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var cells = line.Split('\t');
                if (cells.Length < 2) continue;

                string id = cells[0].Trim(), pose = cells[1].Trim();
                if (id.Length > 0 && pose.Length > 0) result[id] = pose;
            }
            return result;
        }

        /// <summary>
        /// 그림 한 장. Sprite 로 들여와 있으면 그대로 쓰고, 그림(Texture2D)으로 들여와 있으면 Sprite 로 만들어 쓴다.
        /// 들여오기 설정이 어느 쪽이든 보이게 한다.
        /// </summary>
        private static Sprite Load(string path)
        {
            if (_cache.TryGetValue(path, out var cached)) return cached;

            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = tex.name;
                }
            }

            _cache[path] = sprite;
            return sprite;
        }
    }
}
