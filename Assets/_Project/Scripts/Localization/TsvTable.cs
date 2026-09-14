using System.Collections.Generic;

namespace UrbanLegendBureau.Localization
{
    /// <summary>
    /// 최소한의 TSV 파서.
    /// 탭은 게임 텍스트에 등장하지 않으므로 CSV와 달리 따옴표 이스케이프가 필요 없다.
    /// 셀 안의 줄바꿈만 리터럴 역슬래시+n 으로 표기하고 여기서 실제 개행으로 되돌린다.
    /// </summary>
    public static class TsvTable
    {
        // 역슬래시. \ 표기는 컴파일 전에 치환되어 문자 리터럴을 깨뜨리므로 코드값으로 쓴다.
        private const char EscapeChar = (char)92;
        private static readonly string EscapedNewline = EscapeChar + "n";
        private static readonly string EscapedTab = EscapeChar + "t";
        private static readonly char[] LineSeparators = { '\n' };

        /// <summary>
        /// 첫 유효 행을 헤더로 사용해 파싱한다.
        /// '#' 으로 시작하는 행과 빈 행은 주석으로 보고 건너뛴다.
        /// </summary>
        public static bool TryParse(string raw, out List<string> header, out List<string[]> rows)
        {
            header = null;
            rows = new List<string[]>();

            if (string.IsNullOrEmpty(raw)) return false;

            // UTF-8 BOM 제거
            if (raw[0] == '﻿') raw = raw.Substring(1);

            var lines = raw.Split(LineSeparators);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r');
                if (line.Length == 0) continue;
                if (line[0] == '#') continue;
                if (IsBlank(line)) continue;

                var cells = line.Split('\t');
                for (int c = 0; c < cells.Length; c++)
                {
                    cells[c] = Unescape(cells[c]);
                }

                if (header == null)
                {
                    header = new List<string>(cells.Length);
                    for (int c = 0; c < cells.Length; c++)
                    {
                        header.Add(cells[c].Trim());
                    }
                }
                else
                {
                    rows.Add(cells);
                }
            }

            return header != null;
        }

        private static bool IsBlank(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] != '\t' && line[i] != ' ') return false;
            }
            return true;
        }

        private static string Unescape(string cell)
        {
            if (string.IsNullOrEmpty(cell)) return cell;
            if (cell.IndexOf(EscapeChar) < 0) return cell;

            return cell
                .Replace(EscapedNewline, "\n")
                .Replace(EscapedTab, "\t");
        }

        /// <summary>행에서 안전하게 열 값을 꺼낸다. 열이 모자란 행도 허용한다.</summary>
        public static string Cell(string[] row, int index)
        {
            if (row == null || index < 0 || index >= row.Length) return string.Empty;
            return row[index];
        }
    }
}
