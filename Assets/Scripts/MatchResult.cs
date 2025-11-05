using System.Collections.Generic;

/// <summary>
/// 피스들의 매치 상태를 나타내는 열거형
/// </summary>
public enum MatchDirection
{
    Horizontal, //가로 3
    Vertical, //세로 3
    LongVertical, //세로 4
    LongHorizontal, //가로 4
    Super, //5개 이상
    None, //없음
}

/// <summary>
/// 피스 매치 결과를 나타내는 클래스
/// </summary>
public class MatchResult
{
    public List<Piece> connectedPieces;
    public MatchDirection direction;
}
