namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 프로젝트 전체에서 쓰는 텍스트 굵기 기준.
    /// Pretendard의 실제 굵기 파일과 1:1로 대응한다.
    ///
    /// 굵기를 코드로 고르는 일은 드물다. 대부분은 에디터에서 TMP 컴포넌트에
    /// 해당 Font Asset을 직접 지정한다. 이 enum은 씬 생성 도구와 문서용 기준이다.
    /// </summary>
    public enum UIFontWeight
    {
        /// <summary>본문 / 긴 설명 / 디버그 텍스트</summary>
        Regular = 400,

        /// <summary>일반 UI / 버튼 / 보조 정보</summary>
        Medium = 500,

        /// <summary>섹션 제목 / 강조</summary>
        SemiBold = 600,

        /// <summary>메인 제목 / 강한 강조</summary>
        Bold = 700
    }
}
