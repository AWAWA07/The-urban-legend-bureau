using System;
using UnityEngine;
using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// 저장/로드의 유일한 창구.
    /// 게임 코드는 File, PlayerPrefs, JsonUtility 를 직접 만지지 않는다.
    /// 어떤 매체에 어떤 형식으로 들어가는지는 전적으로 이 클래스와 백엔드의 내부 사정이다.
    /// </summary>
    public class SaveService : IService
    {
        public const string DefaultSlotKey = "slot_0";

        private ISaveBackend _backend;

        /// <summary>현재 메모리에 올라와 있는 저장 데이터. 게임 시스템이 읽고 쓴다.</summary>
        public SaveData Current { get; private set; }

        /// <summary>현재 사용 중인 슬롯 키.</summary>
        public string SlotKey { get; private set; } = DefaultSlotKey;

        /// <summary>마지막 저장 이후 변경이 있었는가. 자동 저장 판단에 쓴다.</summary>
        public bool IsDirty { get; private set; }

        public string BackendName => _backend?.Name ?? "(none)";

        public string SaveLocation => _backend?.DescribeLocation(SlotKey) ?? "(none)";

        // ------------------------------------------------------------- 수명 주기

        public void Initialize()
        {
            _backend = SaveBackendFactory.Create();
            Current = new SaveData();

            Debug.Log($"[SaveService] 백엔드={_backend.Name} | 위치={_backend.DescribeLocation(SlotKey)}");
        }

        public void Shutdown()
        {
            // 종료 시점에 미저장 변경이 있으면 흘려보내지 않고 기록한다.
            if (IsDirty)
            {
                Save();
            }

            _backend = null;
            Current = null;
        }

        // ------------------------------------------------------------- 공개 API

        /// <summary>저장 파일이 존재하는가.</summary>
        public bool HasSave()
        {
            return _backend != null && _backend.Exists(SlotKey);
        }

        /// <summary>현재 상태를 저장한다. 성공하면 Flush까지 마친 뒤 true.</summary>
        public bool Save()
        {
            if (_backend == null || Current == null)
            {
                Debug.LogError("[SaveService] 초기화되지 않았다.");
                return false;
            }

            string json;
            try
            {
                Current.saveVersion = SaveMigration.CurrentVersion;
                json = JsonUtility.ToJson(Current, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] 직렬화 실패: {ex.Message}");
                return false;
            }

            if (!_backend.Write(SlotKey, json))
            {
                return false;
            }

            // 자동 저장 직후 반드시 Flush. WebGL은 이게 없으면 탭을 닫는 순간 사라진다.
            _backend.Flush();

            IsDirty = false;
            return true;
        }

        /// <summary>저장 파일을 불러온다. 없거나 손상되면 false를 돌려주고 현재 상태를 유지한다.</summary>
        public bool Load()
        {
            if (_backend == null)
            {
                Debug.LogError("[SaveService] 초기화되지 않았다.");
                return false;
            }

            var json = _backend.Read(SlotKey);
            if (string.IsNullOrEmpty(json))
            {
                Debug.Log("[SaveService] 저장 파일이 없다.");
                return false;
            }

            SaveData loaded;
            try
            {
                loaded = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] 역직렬화 실패: {ex.Message}");
                return false;
            }

            if (loaded == null)
            {
                Debug.LogError("[SaveService] 저장 파일이 비어 있거나 손상되었다.");
                return false;
            }

            loaded.EnsureIntegrity();

            var result = SaveMigration.Migrate(loaded);
            if (result == SaveMigration.Result.TooNew || result == SaveMigration.Result.Failed)
            {
                return false;
            }

            Current = loaded;
            IsDirty = false;
            return true;
        }

        /// <summary>저장 파일을 지운다.</summary>
        public bool DeleteSave()
        {
            if (_backend == null) return false;

            bool deleted = _backend.Delete(SlotKey);
            if (deleted)
            {
                _backend.Flush();
            }
            return deleted;
        }

        /// <summary>메모리 상태를 새 게임 상태로 초기화한다. 저장 파일은 건드리지 않는다.</summary>
        public void NewGame()
        {
            Current = new SaveData();
            IsDirty = true;
        }

        /// <summary>슬롯을 바꾼다. 멀티 슬롯이 필요해질 때를 위한 자리.</summary>
        public void SetSlot(string slotKey)
        {
            if (string.IsNullOrEmpty(slotKey)) return;
            SlotKey = slotKey;
        }

        /// <summary>게임 시스템이 Current를 변경했을 때 호출한다.</summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>변경이 있을 때만 저장한다. 사건 단계 전환/씬 전환 시점에 부르는 용도.</summary>
        public bool AutoSave()
        {
            if (!IsDirty) return false;
            return Save();
        }
    }
}
