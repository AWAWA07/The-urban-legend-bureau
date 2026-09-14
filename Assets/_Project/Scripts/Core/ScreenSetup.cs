using UnityEngine;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 가로 화면 고정 및 런타임 화면 설정.
    /// 기준 해상도 1920x1080(16:9), 지원 범위 4:3 ~ 21:9.
    /// </summary>
    public static class ScreenSetup
    {
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;
        public const float ReferenceAspect = 16f / 9f;
        public const float MinSupportedAspect = 4f / 3f;
        public const float MaxSupportedAspect = 21f / 9f;

        public static float CurrentAspect => Screen.height <= 0 ? ReferenceAspect : (float)Screen.width / Screen.height;

        public static void Apply()
        {
            if (PlatformInfo.IsMobile)
            {
                // 가로 2방향만 허용. 세로는 막는다.
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.orientation = ScreenOrientation.AutoRotation;

                // 모바일에서 조사 중 화면이 꺼지지 않도록.
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }

            if (CurrentAspect < MinSupportedAspect - 0.01f)
            {
                Debug.LogWarning($"[ScreenSetup] 화면 비율 {CurrentAspect:F2} 가 최소 지원 비율 {MinSupportedAspect:F2} 보다 좁다. UI가 잘릴 수 있다.");
            }
        }
    }
}
