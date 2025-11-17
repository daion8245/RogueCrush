using System.Collections;
using UnityEngine;

/* 구조 설명: 보드(Board) > 노드(Node) > 피스(Piece)
   - Piece는 구조적으로 Node 안에 있으며 피스의 여러 데이터들을 담고 있습니다.
   - Node는 Piece 데이터를 가지고 있으며 게임의 "칸" 역할을 합니다. Node는 피스의 이동, 삭제, 추가 등을 관리합니다.
   - Board는 여러 Node들을 포함하는 게임 오브젝트로, 전체 피스 보드를 관리합니다.
*/

public class Piece : MonoBehaviour
{
    public PieceType pieceType;

    public int xIndex; // 피스의 x 좌표
    public int yIndex; // 피스의 y 좌표

    public bool isMatched; // 피스가 매치되었는지 여부
    public Vector2 currentPos; // 현재 위치
    public Vector2 targetPos; // 목표 위치

    public bool isMoving; // 피스가 이동 중인지 여부

    // 생성자
    public Piece(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void SetIndices(int x, int y)
    {
        xIndex = x;
        yIndex = y;
    }

    public void MoveToTarget(Vector2 targetPositon)
    {
        StartCoroutine(MoveCoroutine(targetPositon));
    }

    private IEnumerator MoveCoroutine(Vector2 target)
    {
        isMoving =  true;
        float duration = 0.2f;
        
        Vector2 startPos = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float time = elapsedTime / duration;
            
            transform.position = Vector2.Lerp(startPos, target, time);
            
            elapsedTime += Time.deltaTime;
            
            yield return null;
        }
    }
}

/// <summary>
/// 피스의 타입(색깔)
/// </summary>
public enum PieceType
{
    Red,
    Blue,
    Purple,
    Green,
    White,
}
