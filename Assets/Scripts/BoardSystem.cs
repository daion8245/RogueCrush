using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class BoardSystem : MonoBehaviour
{
    #region SerializeField Variables
    
    [Header("보드 크기 설정")]
    [SerializeField, Tooltip("보드의 가로 크기")] private int width = 6;
    [SerializeField, Tooltip("보드의 세로 크기")] private int height = 8;
    
    [Header("피스 간 간격 설정")]
    [SerializeField, Tooltip("피스의 가로 간격")] private float spacingX;
    [SerializeField, Tooltip("피스의 세로 간격")] private float spacingY;
    
    [Header("피스 프리팹 설정")]
    [SerializeField, Tooltip("보드에 생성할 피스 프리팹(Prefab)")] private GameObject[] piecePrefabs;
    
    [Header("피스 부모 오브젝트 설정")]
    [SerializeField, Tooltip("부모 오브젝트 설정(보통 스크립트 넣은 보드 게임 오브젝트 넣음)"
         )] private Transform piecesRoot;
    
    [Header("보드 레이아웃 설정")]
    [SerializeField, Tooltip("어떤 칸이 막혀있어야 하는지 설정할수 있음.")] private ArrayLayout arrayLayout;

    [Header("내부 디버깅 표시 변수")]
    [SerializeField,Tooltip("현제 선택된 피스")] private Piece selectedPiece;
    [SerializeField,Tooltip("현제 피스가 이동하고 있는지 상태")] private bool isProcessingMoving;
    [SerializeField, Tooltip("삭제되게 선택된 피스들")] private List<Piece> piecesToRemove = new();
    
    #endregion

    #region PrivateVariables
    
    /// <summary>
    /// 보드의 피스들을 담는 2차원 배열
    /// </summary>
    private Node[,] _boardPieces;
    
    /// <summary>
    ///  삭제할 피스들을 담는 리스트
    /// </summary>
    [Tooltip("")]private readonly List<GameObject> _piecesToDestroy = new();

    /// <summary>
    /// 메인 카메라 참조
    /// </summary>
    private Camera _mainCam;
    
    #endregion

    private static BoardSystem _instance; //싱글톤 인스턴스

    private void Awake()
    {
        //싱글톤 패턴 구현
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            //이미 인스턴스가 존재하면 자신을 파괴
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _mainCam = Camera.main;//메인 카메라 참조 초기화
        InitializeBoard();//보드 초기화
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = _mainCam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
        Collider2D overlapPoint = Physics2D.OverlapPoint(worldPos);

        if (overlapPoint == null)
            return;

        Piece piece = overlapPoint.gameObject.GetComponent<Piece>();
        if (piece == null || isProcessingMoving)
            return;

        SelectPiece(piece);
    }
    
    /// <summary>
    /// 보드 초기화(초기 생성)
    /// </summary>
    private void InitializeBoard()
    {
        DestroyPieces();//기존 피스들 삭제
        _boardPieces = new Node[width, height]; //보드 피스 배열 초기화(설정된 가로,세로 크기로)

        spacingX = (float)(width - 1) / 2f; //피스 간 가로 간격 설정
        spacingY = (float)(height - 1) / 2f; //피스 간 세로 간격 설정
        
        //모든 보드 칸 순회 하면서 피스 생성 
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new(x - spacingX, y - spacingY); //피스 생성 위치 계산

                if (arrayLayout != null && arrayLayout.IsBlocked(x, y)) //해당 칸이 막혀있는지 확인`
                {
                    _boardPieces[x, y] = new Node(false, null); //막혀있으면 사용 불가 노드로 설정
                    continue; //다음 칸으로 이동
                }
                
                //피스 생성
                int randomIndex = Random.Range(0, piecePrefabs.Length); //피스 프리팹 중 랜덤 선택
                Transform parentForPiece =
                    piecesRoot != null ? piecesRoot : transform; //피스의 부모 오브젝트 설정
                GameObject pieceGo = Instantiate(piecePrefabs[randomIndex],
                    position, Quaternion.identity, parentForPiece);//피스 생성
                Piece pieceComp = pieceGo.GetComponent<Piece>();//생성된 피스의 Piece 컴포넌트 참조
                
                //피스의 인덱스 설정
                if (pieceComp != null)
                {
                    pieceComp.SetIndices(x, y); //피스의 x,y 인덱스 설정
                }

                _boardPieces[x, y] = new Node(true, pieceGo); //보드 칸에 피스 할당
                _piecesToDestroy.Add(pieceGo); //삭제할 피스 리스트에 추가
            }
        }
        
        if (CheckBoardToMatches(false))
        {
            InitializeBoard();
        }
    }
    
    /// <summary>
    /// 모든 피스들을 삭제하는 함수
    /// </summary>
    private void DestroyPieces()
    {
        //삭제할 피스가 없으면 종료
        if (_piecesToDestroy.Count == 0)
            return;

        //모든 피스 삭제
        foreach (GameObject piece in _piecesToDestroy)
        {
            if (piece != null)
            {
                Destroy(piece);
            }
        }

        //삭제할 피스 리스트 초기화
        _piecesToDestroy.Clear();
    }
    
    /// <summary>
    /// 보드에 매치된 피스들이 있는지 확인하는 함수
    /// </summary>
    /// <param name="takeAction">
    /// 보드에 매치가 있는지 검사 / False,
    /// 보드에 매치가 발견되면 실제 제거 및 리필처리 / True</param>
    /// <returns></returns>
    public bool CheckBoardToMatches(bool takeAction)
    {
        //보드 피스 배열이 null이면 종료
        if (_boardPieces == null)
            return false;
        
        bool hasMatched = false; //매치된 피스가 있는지 여부

        piecesToRemove.Clear(); //제거할 피스 리스트 초기화
        ResetMatchedFlags(); //모든 피스의 매치 플래그 초기화

        //모든 피스 순회하면서 매치 검사
        //매치된 플래그 초기화
        foreach (Node nodePiece in _boardPieces)
        {
            //노드가 null이 아니고 피스가 존재하면 매치 플래그 초기화
            //이전에 매치된 피스가 있을수도 있으니 초기화
            if (nodePiece != null && nodePiece.piece != null)
            {
                nodePiece.piece.GetComponent<Piece>().isMatched = false;
            }
        }
        //2중 for문으로 보드 전체 순회
        //보드의 모든 피스를 검사해서 매치된 피스들을 찾는 for문
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //노드가 사용 불가이거나 피스가 없으면 다음 칸으로
                if (!_boardPieces[x, y].isUsable || _boardPieces[x, y].piece == null)
                    continue;

                //피스 컴포넌트 참조
                Piece piece = _boardPieces[x, y].piece.GetComponent<Piece>();
                
                //이미 매치된 피스면 다음 칸으로
                if (piece.isMatched)
                    continue;

                MatchResult matchedPieces = IsConnected(piece); //피스가 매치되었는지 검사
                //매치된 피스가 3개 이상이면 매칭 처리
                if (matchedPieces.connectedPieces.Count >= 3)
                {
                    MatchResult superMatchedPieces = SuperMatch(matchedPieces); //슈퍼 매치 검사

                    piecesToRemove.AddRange(superMatchedPieces.connectedPieces); //제거할 피스 리스트에 추가
                    
                    // 매치 플래그 설정
                    foreach (Piece pie in superMatchedPieces.connectedPieces) 
                    {
                        pie.isMatched = true;
                    }

                    //매치된 피스가 있으므로 플래그 설정
                    hasMatched = true;
                }
            }
        }
        
        //매치된 피스가 있고 실제 제거 및 리필 처리를 원하면 코루틴 시작
        if (hasMatched && takeAction)
        {
            StartCoroutine(ProcessMatchedBoard());
        }

        return hasMatched;
    }

    /// <summary>
    /// 매치된 보드,피스를 처리하는 코루틴
    /// </summary>
    /// <returns></returns>
    private IEnumerator ProcessMatchedBoard()
    {
        //매치된 피스들의 매치 플래그 초기화
        foreach (Piece piece in piecesToRemove)
        {
            piece.isMatched = false;
        }

        //매치된 피스 제거 및 리필 처리
        RemoveAndRefill(piecesToRemove);
        yield return new WaitForSeconds(0.4f);

        if (CheckBoardToMatches(false))
        {
            yield return StartCoroutine(ProcessMatchedBoard());
        }
    }
    
    private void RemoveAndRefill(List<Piece> removeTargets)
    {
        foreach (Piece piece in removeTargets)
        {
            int xIndex = piece.xIndex;
            int yIndex = piece.yIndex;

            Destroy(piece.gameObject);

            _boardPieces[xIndex, yIndex] = new Node(true, null);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (_boardPieces[x, y].piece == null && _boardPieces[x, y].isUsable)
                {
                    RefillPiece(x, y);
                }
            }
        }
    }

    private void RefillPiece(int x, int y)
    {
        int yOffset = 1;
        
        while (y + yOffset < height && _boardPieces[x, y + yOffset].piece == null)
        {
            yOffset++;
        }
        
        if (y + yOffset < height && _boardPieces[x, y + yOffset].piece != null)
        {
            Piece pieceAbove = _boardPieces[x, y + yOffset].piece.GetComponent<Piece>();

            Vector3 targetPos = GetPiecePosition(x, y, pieceAbove.transform.position.z);
            pieceAbove.MoveToTarget(targetPos);
            pieceAbove.SetIndices(x, y);

            _boardPieces[x, y] = _boardPieces[x, y + yOffset];
            _boardPieces[x, y + yOffset] = new Node(true, null);
        }
        
        if (y + yOffset == height)
        {
            SpawnPieceAtTop(x);
        }
    }
    
    private void SpawnPieceAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x);
        if (index < 0)
            return;

        int locationToMoveTo = height - index;

        int randomIndex = Random.Range(0, piecePrefabs.Length);
        Vector2 spawnPos = new(x - spacingX, height - spacingY);

        Transform parentForPiece = piecesRoot != null ? piecesRoot : transform;
        GameObject newPiece = Instantiate(piecePrefabs[randomIndex], spawnPos, Quaternion.identity, parentForPiece);
        Piece pieceComp = newPiece.GetComponent<Piece>();
        pieceComp.SetIndices(x, index);

        _boardPieces[x, index] = new Node(true, newPiece);
        _piecesToDestroy.Add(newPiece);

        Vector3 targetPosition = new Vector3(newPiece.transform.position.x, newPiece.transform.position.y - locationToMoveTo, newPiece.transform.position.z);
        pieceComp.MoveToTarget(targetPosition);
    }

    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = -1;
        for (int y = height - 1; y >= 0; y--)
        {
            if (_boardPieces[x, y].piece == null && _boardPieces[x, y].isUsable)
            {
                lowestNull = y;
            }
        }

        return lowestNull;
    }
    
    private MatchResult SuperMatch(MatchResult matchedResults)
    {
        if (matchedResults.direction == MatchDirection.Horizontal || matchedResults.direction == MatchDirection.LongHorizontal)
        {
            foreach (Piece pie in matchedResults.connectedPieces)
            {
                List<Piece> extraConnectedPieces = new();
                CheckDirection(pie, new Vector2Int(0, 1), extraConnectedPieces);
                CheckDirection(pie, new Vector2Int(0, -1), extraConnectedPieces);

                if (extraConnectedPieces.Count >= 2)
                {
                    extraConnectedPieces.AddRange(matchedResults.connectedPieces);

                    return new MatchResult
                    {
                        connectedPieces = extraConnectedPieces,
                        direction = MatchDirection.Super,
                    };
                }
            }

            return new MatchResult
            {
                connectedPieces = matchedResults.connectedPieces,
                direction = matchedResults.direction
            };
        }
        else if (matchedResults.direction == MatchDirection.Vertical || matchedResults.direction == MatchDirection.LongVertical)
        {
            foreach (Piece pie in matchedResults.connectedPieces)
            {
                List<Piece> extraConnectedPieces = new();
                CheckDirection(pie, new Vector2Int(1, 0), extraConnectedPieces);
                CheckDirection(pie, new Vector2Int(-1, 0), extraConnectedPieces);

                if (extraConnectedPieces.Count >= 2)
                {
                    extraConnectedPieces.AddRange(matchedResults.connectedPieces);

                    return new MatchResult
                    {
                        connectedPieces = extraConnectedPieces,
                        direction = MatchDirection.Super,
                    };
                }
            }

            return new MatchResult
            {
                connectedPieces = matchedResults.connectedPieces,
                direction = matchedResults.direction
            };
        }

        return matchedResults;
    }

    private void ResetMatchedFlags()
    {
        if (_boardPieces == null)
            return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = _boardPieces[x, y];
                if (node == null || !node.isUsable || node.piece == null)
                    continue;

                Piece piece = node.piece.GetComponent<Piece>();
                if (piece != null)
                {
                    piece.isMatched = false;
                }
            }
        }
    }

    private MatchResult IsConnected(Piece piece)
    {
        List<Piece> connectedPieces = new() { piece };

        CheckDirection(piece, new Vector2Int(1, 0), connectedPieces);
        CheckDirection(piece, new Vector2Int(-1, 0), connectedPieces);
        if (connectedPieces.Count == 3)
        {
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.Horizontal
            };
        }
        else if (connectedPieces.Count > 3)
        {
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongHorizontal
            };
        }

        connectedPieces.Clear();
        connectedPieces.Add(piece);

        CheckDirection(piece, new Vector2Int(0, 1), connectedPieces);
        CheckDirection(piece, new Vector2Int(0, -1), connectedPieces);
        if (connectedPieces.Count == 3)
        {
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.Vertical
            };
        }
        else if (connectedPieces.Count > 3)
        {
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongVertical
            };
        }

        return new MatchResult
        {
            connectedPieces = connectedPieces,
            direction = MatchDirection.None
        };
    }

    private void CheckDirection(Piece originPiece, Vector2Int direction, List<Piece> connectedPieces)
    {
        PieceType pieceType = originPiece.pieceType;
        int x = originPiece.xIndex + direction.x;
        int y = originPiece.yIndex + direction.y;

        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            if (!_boardPieces[x, y].isUsable || _boardPieces[x, y].piece == null)
                break;

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

    public void SelectPiece(Piece piece)
    {
        if (selectedPiece == null)
        {
            selectedPiece = piece;
        }
        else if (selectedPiece == piece)
        {
            selectedPiece = null;
        }
        else if (selectedPiece != piece)
        {
            SwapPieces(selectedPiece, piece);
            selectedPiece = null;
        }
    }

    private void SwapPieces(Piece currentPiece, Piece targetPiece)
    {
        if (!IsAdjacent(currentPiece, targetPiece))
            return;

        DoSwap(currentPiece, targetPiece);

        isProcessingMoving = true;

        StartCoroutine(ProcessMatches(currentPiece, targetPiece));
    }

    private void DoSwap(Piece currentPiece, Piece targetPiece)
    {
        GameObject temp = _boardPieces[currentPiece.xIndex, currentPiece.yIndex].piece;

        _boardPieces[currentPiece.xIndex, currentPiece.yIndex].piece = _boardPieces[targetPiece.xIndex, targetPiece.yIndex].piece;
        _boardPieces[targetPiece.xIndex, targetPiece.yIndex].piece = temp;

        int tempXIndex = currentPiece.xIndex;
        int tempYIndex = currentPiece.yIndex;
        currentPiece.xIndex = targetPiece.xIndex;
        currentPiece.yIndex = targetPiece.yIndex;
        targetPiece.xIndex = tempXIndex;
        targetPiece.yIndex = tempYIndex;

        currentPiece.MoveToTarget(GetPiecePosition(currentPiece.xIndex, currentPiece.yIndex, currentPiece.transform.position.z));
        targetPiece.MoveToTarget(GetPiecePosition(targetPiece.xIndex, targetPiece.yIndex, targetPiece.transform.position.z));
    }

    private IEnumerator ProcessMatches(Piece currentPiece, Piece targetPiece)
    {
        yield return new WaitForSeconds(0.2f);

        if (CheckBoardToMatches(true))
        {
            yield return null;
        }
        else
        {
            DoSwap(currentPiece, targetPiece);
        }

        isProcessingMoving = false;
    }

    private bool IsAdjacent(Piece currentPiece, Piece targetPiece)
    {
        return Mathf.Abs(currentPiece.xIndex - targetPiece.xIndex) + Mathf.Abs(currentPiece.yIndex - targetPiece.yIndex) == 1;
    }

    private Vector3 GetPiecePosition(int x, int y, float z = 0f)
    {
        return new Vector3(x - spacingX, y - spacingY, z);
    }
}
