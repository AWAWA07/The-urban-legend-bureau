using System.Collections.Generic;
using UnityEngine;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.Localization
{
    /// <summary>
    /// 문자열 ID -> 실제 문장 변환을 담당하는 유일한 창구.
    /// 게임 코드와 ScriptableObject 어디에도 실제 문장을 두지 않는다.
    ///
    /// 테이블: Assets/_Project/Resources/Localization/ 아래의 모든 .txt 테이블을 병합 로드.
    /// 형식:  ID \t ko \t en \t note
    /// 언어 추가는 열 하나 추가로 끝난다. 코드 수정은 필요 없다.
    /// </summary>
    public class LocalizationService : IService
    {
        public const string ResourceFolder = "Localization";
        public const string DefaultLanguage = "ko";
        private const string IdColumn = "ID";
        private const string NoteColumn = "note";

        // 언어코드 -> (문자열 ID -> 문장)
        private readonly Dictionary<string, Dictionary<string, string>> _tables =
            new Dictionary<string, Dictionary<string, string>>();

        private readonly HashSet<string> _reportedMissing = new HashSet<string>();

        private Dictionary<string, string> _current;

        public string CurrentLanguage { get; private set; } = DefaultLanguage;

        public IEnumerable<string> AvailableLanguages => _tables.Keys;

        public int EntryCount => _current?.Count ?? 0;

        public void Initialize()
        {
            LoadAllTables();
            SetLanguage(DefaultLanguage);
        }

        public void Shutdown()
        {
            _tables.Clear();
            _reportedMissing.Clear();
            _current = null;
        }

        // ---------------------------------------------------------------- 조회

        /// <summary>
        /// 문자열 ID로 문장을 가져온다.
        /// 없으면 화면에서 바로 눈에 띄도록 "#id" 를 돌려준다. 예외를 던지지 않는다.
        /// </summary>
        public string Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;

            if (_current != null && _current.TryGetValue(id, out var text) && !string.IsNullOrEmpty(text))
            {
                return text;
            }

            // 현재 언어에 없으면 기본 언어로 한 번 더 시도한다.
            if (CurrentLanguage != DefaultLanguage &&
                _tables.TryGetValue(DefaultLanguage, out var fallback) &&
                fallback.TryGetValue(id, out var fallbackText) &&
                !string.IsNullOrEmpty(fallbackText))
            {
                return fallbackText;
            }

            ReportMissing(id);
            return $"#{id}";
        }

        /// <summary>{0}, {1} 서식 인자를 포함한 문장용.</summary>
        public string Get(string id, params object[] args)
        {
            var format = Get(id);
            if (args == null || args.Length == 0) return format;

            try
            {
                return string.Format(format, args);
            }
            catch (System.FormatException)
            {
                Debug.LogError($"[Localization] '{id}' 서식 인자 불일치: \"{format}\"");
                return format;
            }
        }

        public bool Has(string id)
        {
            return !string.IsNullOrEmpty(id) && _current != null && _current.ContainsKey(id);
        }

        // ---------------------------------------------------------------- 언어

        public bool SetLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode)) return false;

            if (!_tables.TryGetValue(languageCode, out var table))
            {
                Debug.LogWarning($"[Localization] '{languageCode}' 열이 테이블에 없다. '{DefaultLanguage}' 로 유지한다.");
                if (!_tables.TryGetValue(DefaultLanguage, out table)) return false;
                languageCode = DefaultLanguage;
            }

            bool changed = CurrentLanguage != languageCode || _current == null;
            CurrentLanguage = languageCode;
            _current = table;
            _reportedMissing.Clear();

            if (changed)
            {
                EventBus.Publish(new LanguageChangedEvent(languageCode));
            }

            return true;
        }

        // ---------------------------------------------------------------- 로딩

        private void LoadAllTables()
        {
            _tables.Clear();

            var assets = Resources.LoadAll<TextAsset>(ResourceFolder);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[Localization] Resources/{ResourceFolder} 에서 텍스트 테이블을 찾지 못했다.");
                return;
            }

            int totalRows = 0;
            for (int i = 0; i < assets.Length; i++)
            {
                totalRows += LoadTable(assets[i]);
            }

            Debug.Log($"[Localization] 테이블 {assets.Length}개, 항목 {totalRows}개, 언어 [{string.Join(", ", AvailableLanguages)}] 로드 완료");
        }

        private int LoadTable(TextAsset asset)
        {
            if (asset == null) return 0;

            if (!TsvTable.TryParse(asset.text, out var header, out var rows))
            {
                Debug.LogError($"[Localization] '{asset.name}' 파싱 실패: 유효한 헤더 행이 없다.");
                return 0;
            }

            int idIndex = header.IndexOf(IdColumn);
            if (idIndex < 0)
            {
                Debug.LogError($"[Localization] '{asset.name}' 에 '{IdColumn}' 열이 없다.");
                return 0;
            }

            // ID 열과 note 열을 제외한 나머지를 전부 언어 열로 본다.
            var languageColumns = new List<KeyValuePair<string, int>>();
            for (int c = 0; c < header.Count; c++)
            {
                if (c == idIndex) continue;
                var name = header[c];
                if (string.IsNullOrEmpty(name)) continue;
                if (name == NoteColumn) continue;

                languageColumns.Add(new KeyValuePair<string, int>(name, c));
                if (!_tables.ContainsKey(name))
                {
                    _tables[name] = new Dictionary<string, string>();
                }
            }

            if (languageColumns.Count == 0)
            {
                Debug.LogError($"[Localization] '{asset.name}' 에 언어 열이 하나도 없다.");
                return 0;
            }

            int added = 0;
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                var id = TsvTable.Cell(row, idIndex).Trim();
                if (string.IsNullOrEmpty(id)) continue;

                for (int c = 0; c < languageColumns.Count; c++)
                {
                    var lang = languageColumns[c].Key;
                    var value = TsvTable.Cell(row, languageColumns[c].Value);
                    if (string.IsNullOrEmpty(value)) continue;

                    var table = _tables[lang];
                    if (table.ContainsKey(id))
                    {
                        Debug.LogWarning($"[Localization] 중복 ID '{id}' ({asset.name}, {lang}). 나중 값으로 덮어쓴다.");
                    }
                    table[id] = value;
                }

                added++;
            }

            return added;
        }

        private void ReportMissing(string id)
        {
            if (_reportedMissing.Add(id))
            {
                Debug.LogWarning($"[Localization] 누락된 문자열 ID: '{id}' (언어: {CurrentLanguage})");
            }
        }
    }
}
