namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// UI 레이어. Canvas 하나당 레이어 하나로 분리한다.
    /// Canvas를 나누면 한 레이어가 바뀌어도 다른 레이어의 메시가 다시 만들어지지 않는다.
    /// 5개는 분리 이득이 비용보다 큰 최소 단위이며, 이보다 더 쪼개지 않는다.
    /// </summary>
    public enum UILayer
    {
        /// <summary>월드에 붙는 UI. 오브젝트 하이라이트, 말풍선.</summary>
        World = 0,

        /// <summary>상시 표시. 믿음 게이지, 확산도, 사건 정보.</summary>
        HUD = 1,

        /// <summary>전체 화면. 브라우저, 단서 보드, 추론, 검열.</summary>
        Screen = 2,

        /// <summary>모달. 확인창, 단서 상세.</summary>
        Popup = 3,

        /// <summary>로딩, 페이드, 토스트. 항상 최상단.</summary>
        System = 4
    }

    public static class UILayerExtensions
    {
        /// <summary>레이어별 Canvas sortingOrder. 사이를 비워 두어 나중에 끼워 넣을 수 있게 한다.</summary>
        public static int SortingOrder(this UILayer layer)
        {
            return (int)layer * 100;
        }
    }
}
