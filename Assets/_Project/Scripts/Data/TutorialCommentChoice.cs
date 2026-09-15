namespace UrbanLegendBureau.Data
{
    /// <summary>댓글이 괴담에 무슨 짓을 하는가.</summary>
    public enum TutorialCommentKind
    {
        /// <summary>괴담을 더 믿게 만든다. 검열관이 할 일이 아니다.</summary>
        Amplify = 0,

        /// <summary>괴담이 사실이 아님을 짚는다. 사람들이 덜 믿게 된다.</summary>
        Debunk = 1,

        /// <summary>괴담에 걸렸을 때의 대처법. 현장에서 규칙을 확인해야 쓸 수 있다.</summary>
        Remedy = 2
    }

    /// <summary>
    /// 튜토리얼에서 고르는 댓글 하나.
    ///
    /// ScriptableObject로 만들지 않은 이유: 튜토리얼 한 곳에서만 쓰는 세 줄짜리 선택지다.
    /// 에셋으로 올릴 만한 재사용성이 없다. 필요해지면 그때 SO로 옮기면 된다.
    /// </summary>
    public class TutorialCommentChoice
    {
        public string ChoiceId;

        /// <summary>선택지 설명(이 댓글이 어떤 성격인지).</summary>
        public string SummaryTextId;

        /// <summary>실제로 달리는 댓글 문장.</summary>
        public string BodyTextId;

        public TutorialCommentKind Kind;

        /// <summary>
        /// 이 댓글을 쓰려면 필요한 단서 ID. 비어 있으면 조건이 없다.
        ///
        /// 잠금 여부를 bool로 들고 있지 않는 이유: "현장 조사를 했는가"는
        /// 이미 SaveData.acquiredClueIds 가 답할 수 있는 질문이다.
        /// 현장에서만 얻을 수 있는 단서를 요구하면 그것으로 충분하다.
        /// </summary>
        public string RequiredClueId;
    }
}
