using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardSystem : MonoBehaviour
{
    #region Serialized Fields
    
    //보드의 크기 설정
    [SerializeField]private int width = 6; //보드의 가로 크기
    [SerializeField]private int height = 8;//보드의 세로 크기

    //노드간의 간격 설정
    [SerializeField]private float spacingX; //노드간의 가로 간격
    [SerializeField]private float spacingY; //노드간의 세로 간격

    [SerializeField] private GameObject[] piecePrefabs; //피스 프리팹 배열

    private GameObject _boardRoot; // 보드 부모 오브젝트
    
    #endregion
    
    
    private Node[,] _boardPieces; // 보드의 2차원 배열 이곳에 노드들이 들어간다.
    
    public static BoardSystem Instance; // 싱글톤 인스턴스 

    [Header("Optional Parents")] public Transform piecesRoot; // 피스 부모 오브젝트
   
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        InitializeBoard();
    }

    void InitializeBoard()
    {
        _boardPieces = new Node[width, height]; // 보드 노드 배열 초기화

        spacingX = (float)(width - 1) / 2; // 가로 간격 설정
        spacingY = (float)(height - 1) / 2; // 세로 간격 설정

        // 피스 랜덤 생성 후 보드에 채우기
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY); //피스의 위치 계산

                int randomIndex = Random.Range(0, piecePrefabs.Length); //랜덤한 피스 프리팹 인덱스 선택

                // 피스 인스턴스 생성
                Transform parentForPiece = piecesRoot != null ? piecesRoot :
                                            (_boardRoot != null ? _boardRoot.transform : transform);
                GameObject pieceGo = Instantiate(
                    piecePrefabs[randomIndex],
                    position,
                    Quaternion.identity,
                    parentForPiece
                );// 피스 인스턴스 생성

                // 좌표 설정
                var pComp = pieceGo.GetComponent<Piece>();
                if (pComp != null) pComp.SetIndices(x, y); //피스의 좌표 설정

                // 노드 생성 및 보드에 할당
                GameObject nodeGo = new GameObject($"Node_{x}_{y}");
                nodeGo.transform.SetParent(_boardRoot != null ? _boardRoot.transform : transform, false);
                nodeGo.transform.localPosition = position;

                Node node = nodeGo.AddComponent<Node>();
                node.Initialize(x, y);
                node.SetPiece(pieceGo);

                _boardPieces[x, y] = node; //보드에 노드 할당
            }
        }

        CheckBoardToMatches();
    }
    
    /// <summary>
    /// 전반적으로 일치하는 피스가 있는지 확인하는 함수
    /// 일치하는 피스가 있으면 true 반환
    /// 그리고 일치하는 피스들은 isMatched 플래그를 true로 설정
    /// Debug.Log로 매치 발생 상황 출력
    /// </summary>
    /// <returns></returns>
    public bool CheckBoardToMatches()
    {
        Debug.Log($"Checking board to match pos");
        bool hasMatches = false;
        
        List<Piece> piecesToRemove = new List<Piece>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_boardPieces[x, y].isUsable)
                {
                    Piece piece = _boardPieces[x,y].piece.GetComponent<Piece>();

                    if (!piece.isMatched)
                    {
                        MatchResult matchPiece = IsConnected(piece);

                        if (matchPiece.connectedPieces.Count >= 3)
                        {
                            piecesToRemove.AddRange(matchPiece.connectedPieces);
                            foreach (Piece pieceToRemove in matchPiece.connectedPieces)
                            {
                                pieceToRemove.isMatched = true;
                            }
                            
                            hasMatches = true;
                        }
                    }
                }
            }
        }

        return hasMatches;
    }

    private MatchResult IsConnected(Piece piece)
    {
        List<Piece> connectedPieces = new();
        
        connectedPieces.Add(piece);
        
        //오른쪽 방향 확인
        CheckDirection(piece, new Vector2Int(1,0), connectedPieces);
        //왼쪽 방향 확인
        CheckDirection(piece, new Vector2Int(-1,0), connectedPieces);
        // 3매치가 되었는지 확인
        if (connectedPieces.Count == 3)
        {
            Debug.Log($"가로 3매치 발생, 색깔 : {connectedPieces[0].pieceType}");

            return new MatchResult()
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.Horizontal
            };
        }
        //3매치 이상인지 확인
        else if (connectedPieces.Count > 3)
        {
            Debug.Log($"가로 3매치 이상 매치 발생, 색깔 : {connectedPieces[0].pieceType}");

            return new MatchResult()
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongHorizontal
            };
        }
        //세로 매치 확인을 위해 리스트 초기화
        connectedPieces.Clear();
        //현재 피스 추가
        connectedPieces.Add(piece);
        
        //위 방향 확인
        CheckDirection(piece, new Vector2Int(0,1), connectedPieces);
        //아래 방향 확인
        CheckDirection(piece, new Vector2Int(0,-1), connectedPieces);
        // 3매치가 되었는지 확인
        if (connectedPieces.Count == 3)
        {
            Debug.Log($"세로 3매치 발생, 색깔 : {connectedPieces[0].pieceType}");

            return new MatchResult()
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.Vertical
            };
        }
        //3매치 이상인지 확인
        else if (connectedPieces.Count > 3)
        {
            Debug.Log($"세로 3매치 이상 매치 발생, 색깔 : {connectedPieces[0].pieceType}");

            return new MatchResult()
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongVertical
            };
        }
        else
        {
            return new MatchResult()
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.None
            };
        }
    }

    /// <summary>
    /// 현재 보드에 일정 갯수 이상 겹치는 피스가 있는지 확인하는 함수
    /// </summary>
    private void CheckDirection(Piece originPiece, Vector2Int direction, List<Piece> connectedPieces)
    {
        PieceType pieceType = originPiece.pieceType;
        int x = originPiece.xIndex + direction.x;
        int y = originPiece.yIndex + direction.y;
        
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if(!_boardPieces[x, y].isUsable) break;
            
            Piece neighborPiece = _boardPieces[x, y].piece.GetComponent<Piece>();

            if (!neighborPiece.isMatched && neighborPiece.pieceType == pieceType)
            {
                connectedPieces.Add(neighborPiece);
                
                x += direction.x;
                y += direction.y;
            }
            else break;
        }
    }
}
