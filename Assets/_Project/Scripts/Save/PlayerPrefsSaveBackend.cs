using System;
using UnityEngine;

namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// WebGL 용. 브라우저에서는 파일 시스템을 쓸 수 없으므로 PlayerPrefs에 저장한다.
    /// Unity WebGL의 PlayerPrefs는 IndexedDB에 얹혀 있고, PlayerPrefs.Save()가
    /// IndexedDB 동기화를 유발한다. 이 Save() 호출이 없으면 탭을 닫는 순간 저장이 사라진다.
    ///
    /// SaveData 전체를 키 하나에 통째로 넣는다. 키를 잘게 쪼개면 브라우저 쿼터에 빨리 닿는다.
    /// </summary>
    public class PlayerPrefsSaveBackend : ISaveBackend
    {
        private const string KeyPrefix = "ulb.save.";

        public string Name => "PlayerPrefs";

        public string DescribeLocation(string key) => $"PlayerPrefs[{PrefKey(key)}]";

        public bool Exists(string key)
        {
            return PlayerPrefs.HasKey(PrefKey(key));
        }

        public string Read(string key)
        {
            var prefKey = PrefKey(key);
            if (!PlayerPrefs.HasKey(prefKey)) return null;

            var value = PlayerPrefs.GetString(prefKey, null);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public bool Write(string key, string json)
        {
            try
            {
                PlayerPrefs.SetString(PrefKey(key), json);
                return true;
            }
            catch (Exception ex)
            {
                // 시크릿 모드나 저장 공간 차단 시 여기로 온다. 게임을 죽이지 않고 알린다.
                Debug.LogError($"[PlayerPrefsSaveBackend] 쓰기 실패: {ex.Message}");
                return false;
            }
        }

        public void Flush()
        {
            try
            {
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerPrefsSaveBackend] Flush 실패: {ex.Message}");
            }
        }

        public bool Delete(string key)
        {
            var prefKey = PrefKey(key);
            if (!PlayerPrefs.HasKey(prefKey)) return false;

            PlayerPrefs.DeleteKey(prefKey);
            return true;
        }

        private static string PrefKey(string key) => KeyPrefix + key;
    }
}
