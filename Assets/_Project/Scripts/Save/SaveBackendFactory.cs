using UrbanLegendBureau.Core;

namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// 플랫폼에 맞는 백엔드를 고르는 유일한 지점.
    /// 판단 근거는 PlatformInfo 하나뿐이고, 여기에도 #if 는 없다.
    /// </summary>
    public static class SaveBackendFactory
    {
        public static ISaveBackend Create()
        {
            // WebGL: 파일 시스템 없음 -> PlayerPrefs(IndexedDB)
            // Windows / Android: persistentDataPath 파일
            return PlatformInfo.RequiresStorageFlush
                ? new PlayerPrefsSaveBackend()
                : (ISaveBackend)new FileSaveBackend();
        }
    }
}
