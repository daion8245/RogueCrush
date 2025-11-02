using UnityEngine;

/*구조 설명: PotionBoard > Node > Potion
 Potion은 구조적으로 Node 안에 있으며 포션의 여러 데이터들을 담고 있습니다.

Node는 Potion 데이터를 가지고 있므며 게임의 "칸" 역할을 합니다.
Node는 포션들의 이동, 삭제, 추가 등을 관리합니다.

PotionBoard는 게임 오브젝트로서 여러 Node들을 포함하고 있습니다.
여러 Node들을 관리하며 게임의 전체적인 포션 보드 역할을 합니다.
 */

public class Potion : MonoBehaviour
{
    public PotionType potionType;

    public int xIndex; // 포션의 x 좌표
    public int yIndex; // 포션의 y 좌표

    public bool isMatched; // 포션이 매치되었는지(매치: 같은 색의 포션들이 4개 이상 연결되어 있는가) 여부
    public Vector2 currentPos; // 현재 위치
    public Vector2 targetPos; // 목표 위치

    public bool isMoving; // 포션이 이동 중인지 여부

    // 생성자
    public Potion(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void SetIndices(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }
}

/// <summary>
/// 포션의 타입(색깔)
/// </summary>
public enum PotionType
{
    Red,
    Blue,
    Purple,
    Green,
    White,
}
