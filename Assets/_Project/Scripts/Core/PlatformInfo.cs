using UnityEngine;

namespace UrbanLegendBureau.Core
{
    public enum RuntimePlatformKind
    {
        Unknown,
        Desktop,
        Mobile,
        Web
    }

    /// <summary>
    /// 플랫폼 분기를 이 한 곳에 가둔다.
    /// 게임 로직에는 #if UNITY_WEBGL 같은 전처리기를 절대 쓰지 않는다.
    /// </summary>
    public static class PlatformInfo
    {
        public static RuntimePlatformKind Kind
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return RuntimePlatformKind.Web;
#elif UNITY_ANDROID || UNITY_IOS
                return RuntimePlatformKind.Mobile;
#else
                return RuntimePlatformKind.Desktop;
#endif
            }
        }

        public static bool IsWeb => Kind == RuntimePlatformKind.Web;
        public static bool IsMobile => Kind == RuntimePlatformKind.Mobile;
        public static bool IsDesktop => Kind == RuntimePlatformKind.Desktop;

        /// <summary>WebGL은 프로세스를 종료할 수 없다. 종료 버튼을 숨기는 데 쓴다.</summary>
        public static bool CanQuit => !IsWeb;

        /// <summary>터치 기기에서는 마우스 호버로만 전달되는 정보를 만들면 안 된다.</summary>
        public static bool SupportsHover => !IsMobile;

        /// <summary>WebGL은 싱글 스레드. 스레드 기반 코드를 막는 가드.</summary>
        public static bool SupportsThreads => !IsWeb;

        /// <summary>WebGL 저장은 IndexedDB 비동기 flush가 필요하다.</summary>
        public static bool RequiresStorageFlush => IsWeb;

        public static string Describe()
        {
            return $"{Kind} / {Application.platform} / {Screen.width}x{Screen.height}";
        }
    }
}
