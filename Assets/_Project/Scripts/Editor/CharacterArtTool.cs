using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 인물 그림을 게임에 쓸 모양으로 정리한다. 메뉴: UrbanLegendBureau / Art / 인물 그림 정리
    ///
    /// 원본은 Assets/_Project/Art/Characters/{인물}/Source/{포즈}.png 에 넣는다.
    /// 누르면 원본마다 다음을 해서 Assets/_Project/Resources/Characters/{인물}/ 로 보낸다.
    ///
    ///   1. 배경이 흰색이면(투명이 아니면) 가장자리에서 이어진 흰 부분을 지운다.
    ///   2. 모든 포즈를 같은 캔버스(1240x1640)에 같은 키로 앉힌다.
    ///      머리 꼭대기부터 발끝까지 1624px, 머리 가운데를 캔버스 가운데에 둔다.
    ///      원본마다 인물 크기와 위치가 달라도 게임에서는 똑같은 자리, 똑같은 크기로 보인다.
    ///   3. 얼굴 초상을 잘라 Face/{포즈}.png(512x512)로 둔다. 현장 대사 띠의 초상 칸에 쓴다.
    ///
    /// 원본 파일 이름이 곧 포즈 이름이다. 어느 대사에 어느 포즈를 쓸지는 Resources/Characters/{인물}/poses.txt 가 정한다.
    /// WebP 는 읽지 못한다. PNG 로 바꿔서 넣는다.
    /// </summary>
    public static class CharacterArtTool
    {
        private const string SourceRoot = "Assets/_Project/Art/Characters";
        private const string OutputRoot = "Assets/_Project/Resources/Characters";

        private const int CanvasW = 1240;
        private const int CanvasH = 1640;
        private const int CharH = 1624;        // 머리 꼭대기부터 발끝까지
        private const int TopMargin = 8;
        private const float FaceRatio = 0.21f; // 얼굴 칸 한 변 = 키의 이만큼
        private const int FaceSize = 512;

        [MenuItem("UrbanLegendBureau/Art/인물 그림 정리")]
        public static void RunMenu()
        {
            string report = Run();
            Debug.Log(report);
            EditorUtility.DisplayDialog("인물 그림 정리", report, "확인");
        }

        /// <summary>원본 폴더를 모두 정리한다. 무엇을 했는지 적어 돌려준다.</summary>
        public static string Run()
        {
            if (!Directory.Exists(SourceRoot)) return "원본 폴더가 없다: " + SourceRoot;

            var lines = new List<string>();
            int done = 0;

            foreach (var characterDir in Directory.GetDirectories(SourceRoot))
            {
                string character = Path.GetFileName(characterDir);
                string sourceDir = Path.Combine(characterDir, "Source");
                if (!Directory.Exists(sourceDir)) continue;

                string outDir = OutputRoot + "/" + character;
                Directory.CreateDirectory(outDir + "/Face");

                var adjust = LoadAdjust(Path.Combine(sourceDir, AdjustFile));
                var shift = LoadAdjust(Path.Combine(sourceDir, ShiftFile));

                foreach (var file in Directory.GetFiles(sourceDir, "*.png"))
                {
                    string pose = Path.GetFileNameWithoutExtension(file);
                    float factor = adjust.TryGetValue(pose, out var f) ? f : 1f;
                    float dxShift = shift.TryGetValue(pose, out var sh) ? sh : 0f;
                    string result = Process(file, outDir + "/" + pose + ".png", outDir + "/Face/" + pose + ".png", factor, dxShift);
                    lines.Add(character + "/" + pose + " : " + result);
                    done++;
                }
            }

            AssetDatabase.Refresh();
            return done == 0
                ? "정리할 원본이 없다. " + SourceRoot + "/{인물}/Source/{포즈}.png 에 PNG 를 넣을 것."
                : "인물 그림 " + done + "장을 정리했다.\n" + string.Join("\n", lines);
        }

        /// <summary>
        /// 포즈별 크기 보정. Source/adjust.txt 에 "포즈<탭>배율" 로 적는다. 예: explain	0.93
        /// 아래에서 올려다본 그림처럼 다리가 짧게 그려진 포즈는 키를 맞추면 머리가 커 보인다. 그럴 때 줄인다.
        /// 머리 꼭대기 자리는 그대로 두고 크기만 바뀐다.
        /// </summary>
        private const string AdjustFile = "adjust.txt";

        /// <summary>
        /// 포즈별 좌우 자리 보정. Source/shift.txt 에 "포즈<탭>픽셀" 로 적는다. 양수면 오른쪽으로 옮긴다.
        /// 손을 머리 위로 올린 포즈는 머리 꼭대기가 손이라 가운데가 어긋난다. 그럴 때 얼굴을 가운데로 되돌린다.
        /// </summary>
        private const string ShiftFile = "shift.txt";

        private static Dictionary<string, float> LoadAdjust(string path)
        {
            var map = new Dictionary<string, float>();
            if (!File.Exists(path)) return map;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                var cells = line.Split('\t', ' ');
                if (cells.Length < 2) continue;
                if (float.TryParse(cells[cells.Length - 1].Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var factor) && (factor > 0f || path.EndsWith(ShiftFile)))
                {
                    map[cells[0].Trim()] = factor;
                }
            }
            return map;
        }

        private static string Process(string sourcePath, string bodyPath, string facePath, float factor = 1f, float dxShift = 0f)
        {
            var src = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!src.LoadImage(File.ReadAllBytes(sourcePath))) return "읽지 못함";

            int w = src.width, h = src.height;
            var px = src.GetPixels32();   // 아래 줄부터 위로

            bool whiteBackground = px[0].a > 200 && px[w - 1].a > 200 && px[(h - 1) * w].a > 200;
            if (whiteBackground)
            {
                RemoveWhite(px, w, h);
                src.SetPixels32(px);
                src.Apply();
            }

            // 인물이 있는 범위. 줄 번호는 위에서부터 센다.
            int top = -1, bottom = -1;
            for (int row = 0; row < h && top < 0; row++)
                if (RowHasInk(px, w, h - 1 - row)) top = row;
            for (int row = h - 1; row >= 0 && bottom < 0; row--)
                if (RowHasInk(px, w, h - 1 - row)) bottom = row;
            if (top < 0) return "그림이 비어 있음";

            // 머리 가운데: 머리 꼭대기 몇 줄의 가운데.
            int band = Mathf.Max(4, h / 30);
            double sum = 0; int n = 0;
            for (int row = top; row < top + band && row < h; row++)
            {
                int y = h - 1 - row, l = -1, r = -1;
                for (int x = 0; x < w; x++) if (px[y * w + x].a > 128) { if (l < 0) l = x; r = x; }
                if (l >= 0) { sum += (l + r) / 2.0; n++; }
            }
            float headX = n > 0 ? (float)(sum / n) : w / 2f;

            float s = CharH / (float)(bottom - top + 1) * factor;
            float dx = CanvasW / 2f - headX * s + dxShift;
            float dy = TopMargin - top * s;

            // 캔버스에 옮긴다. 캔버스의 한 칸마다 원본의 어디를 읽을지 거꾸로 따진다.
            var body = new Texture2D(CanvasW, CanvasH, TextureFormat.RGBA32, false);
            var outPx = new Color32[CanvasW * CanvasH];
            for (int oy = 0; oy < CanvasH; oy++)
            {
                int outRow = CanvasH - 1 - oy;                  // 위에서부터 센 줄
                float srcRow = (outRow + 0.5f - dy) / s;         // 원본의 위에서부터 센 줄
                if (srcRow < 0f || srcRow >= h) continue;
                float v = 1f - srcRow / h;

                for (int ox = 0; ox < CanvasW; ox++)
                {
                    float srcX = (ox + 0.5f - dx) / s;
                    if (srcX < 0f || srcX >= w) continue;
                    outPx[oy * CanvasW + ox] = src.GetPixelBilinear(srcX / w, v);
                }
            }
            body.SetPixels32(outPx);
            body.Apply();
            File.WriteAllBytes(bodyPath, body.EncodeToPNG());

            // 얼굴: 머리 꼭대기 조금 위에서부터 키의 21% 정사각형.
            int side = Mathf.RoundToInt(CharH * FaceRatio);
            int fx = CanvasW / 2 - side / 2;
            int fyTop = Mathf.Max(0, TopMargin - side / 14);
            var face = new Texture2D(FaceSize, FaceSize, TextureFormat.RGBA32, false);
            var facePx = new Color32[FaceSize * FaceSize];
            for (int y = 0; y < FaceSize; y++)
            {
                float rowFromTop = fyTop + (FaceSize - 1 - y + 0.5f) * side / FaceSize;
                float v = 1f - rowFromTop / CanvasH;
                for (int x = 0; x < FaceSize; x++)
                {
                    float colX = fx + (x + 0.5f) * side / FaceSize;
                    facePx[y * FaceSize + x] = body.GetPixelBilinear(colX / CanvasW, v);
                }
            }
            face.SetPixels32(facePx);
            face.Apply();
            File.WriteAllBytes(facePath, face.EncodeToPNG());

            Object.DestroyImmediate(src);
            Object.DestroyImmediate(body);
            Object.DestroyImmediate(face);

            return string.Format("원본 {0}x{1}{2}, {3:0.000}배{4}", w, h, whiteBackground ? " (흰 배경 지움)" : "", s,
                Mathf.Approximately(factor, 1f) ? "" : string.Format(" (보정 {0:0.00})", factor));
        }

        private static bool RowHasInk(Color32[] px, int w, int y)
        {
            for (int x = 0; x < w; x++) if (px[y * w + x].a > 128) return true;
            return false;
        }

        /// <summary>가장자리와 이어진 흰 부분을 투명하게 한다. 셔츠처럼 안쪽에 갇힌 흰색은 남는다.</summary>
        private static void RemoveWhite(Color32[] px, int w, int h)
        {
            var bg = new bool[w * h];
            var queue = new Queue<int>();
            for (int x = 0; x < w; x++) { queue.Enqueue(x); queue.Enqueue((h - 1) * w + x); }
            for (int y = 0; y < h; y++) { queue.Enqueue(y * w); queue.Enqueue(y * w + w - 1); }

            while (queue.Count > 0)
            {
                int p = queue.Dequeue();
                if (bg[p] || !IsWhite(px[p])) continue;
                bg[p] = true;

                int x = p % w, y = p / w;
                if (x > 0) queue.Enqueue(p - 1);
                if (x < w - 1) queue.Enqueue(p + 1);
                if (y > 0) queue.Enqueue(p - w);
                if (y < h - 1) queue.Enqueue(p + w);
            }

            // 다리 사이나 팔과 몸 사이처럼 가장자리와 이어지지 않은 배경도 있다.
            // 그런 곳은 순백에 가깝고 넓게 뭉쳐 있다. 셔츠는 음영이 있어 이만큼 희지 않다.
            var seen = new bool[w * h];
            var region = new List<int>();
            for (int start = 0; start < w * h; start++)
            {
                if (bg[start] || seen[start] || !IsPureWhite(px[start])) continue;

                region.Clear();
                queue.Enqueue(start);
                seen[start] = true;
                while (queue.Count > 0)
                {
                    int p = queue.Dequeue();
                    region.Add(p);
                    int x = p % w, y = p / w;
                    TryVisit(p - 1, x > 0);
                    TryVisit(p + 1, x < w - 1);
                    TryVisit(p - w, y > 0);
                    TryVisit(p + w, y < h - 1);
                }

                if (region.Count >= EnclosedMinArea) foreach (int p in region) bg[p] = true;
            }

            void TryVisit(int q, bool inside)
            {
                if (!inside || seen[q] || bg[q] || !IsPureWhite(px[q])) return;
                seen[q] = true;
                queue.Enqueue(q);
            }

            for (int p = 0; p < w * h; p++) if (bg[p]) px[p] = new Color32(0, 0, 0, 0);
        }

        /// <summary>이만큼 넓게 뭉친 순백만 갇힌 배경으로 본다. 작은 반짝임은 그림으로 남긴다.</summary>
        private const int EnclosedMinArea = 1500;

        private static bool IsPureWhite(Color32 c)
        {
            return c.r >= 250 && c.g >= 250 && c.b >= 250;
        }

        private static bool IsWhite(Color32 c)
        {
            int mn = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            int mx = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return mn > 232 && mx - mn < 18;
        }
    }
}
