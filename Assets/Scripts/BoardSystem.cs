using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;

public class BoardSystem : MonoBehaviour
{
    #region Serialized Fields

    //보드의 크기 설정
    [SerializeField] private int width = 6; //보드의 가로 크기
    [SerializeField] private int height = 8;//보드의 세로 크기

    //노드간의 간격 설정
    [SerializeField] private float spacingX; //노드간의 가로 간격
    [SerializeField] private float spacingY; //노드간의 세로 간격

    //선택된 피스
    [SerializeField] private Piece selectedPiece; //선택된 피스

    //시스템에서 피스를 움직이는 중인가?
    [SerializeField] private bool isProcessingMoving; //피스가 움직이는 중인지 여부

    [SerializeField] private GameObject[] piecePrefabs; //피스 프리팹 배열

    private GameObject _boardRoot; // 보드 부모 오브젝트

    #endregion

    private Node[,] _boardPieces; // 보드의 2차원 배열 이곳에 노드들이 들어간다.

    public List<GameObject> piecesToDestroy = new(); // 제거할 피스들 리스트

    public static BoardSystem Instance; // 싱글톤 인스턴스

    [Header("Optional Parents")] public Transform piecesRoot; // 피스 부모 오브젝트

    private Camera _mainCam;

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
        _mainCam = Camera.main;
        InitializeBoard();
    }

    private void Update()
    {
        #region oldInput
        /*
        if (Input.GetMouseButtonDown(0))
        {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider is not null && hit.collider.gameObject.GetComponent<Piece>())
            {
                if(isProcessingMoving)
                    return;
                
                Piece piece = hit.collider.gameObject.GetComponent<Piece>();
                Debug.Log($"{piece.gameObject} <= 해당 피스를 클릭했습니다.");
            }
            
        }
        */

        #endregion
        // 마우스가 없는 환경일 수도 있으니 한 번 체크
        if (Mouse.current == null)
            return;

        // 왼쪽 마우스 버튼이 "이번 프레임에 눌렸을 때"만 처리
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 1. 마우스 스크린 좌표 읽기
            Vector2 mousePos = Mouse.current.position.ReadValue();

            // 2. 스크린 좌표 → 월드 좌표 (2D 전용)
            Vector3 worldPos = _mainCam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));

            // 3. 해당 지점을 덮는 2D 콜라이더 찾기
            Collider2D overlapPoint = Physics2D.OverlapPoint(worldPos);

            // 4. 맞은 콜라이더가 있고, 거기에 Piece가 붙어 있는지 확인
            if (overlapPoint is not null)
            {
                Piece piece = overlapPoint.gameObject.GetComponent<Piece>();
                if (piece is not null)
                {
                    if (isProcessingMoving)
                        return;

                    SelectPiece(piece);
                    Debug.Log($"{piece.gameObject} <= 해당 피스를 클릭했습니다.");
                }
            }
        }
    }

    /// <summary>
    /// 보드를 초기 생성하는 함수
    /// </summary>
    void InitializeBoard()
    {
        DestroyPieces();
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

                piecesToDestroy.Add(nodeGo); //제거할 피스 리스트에 추가
            }
        }

        CheckBoardToMatches();
    }

    private void DestroyPieces()
    {
        if (piecesToDestroy != null)
        {
            foreach (GameObject piece in piecesToDestroy)
            {
                Destroy(piece);
            }
            piecesToDestroy.Clear();
        }
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

        ResetMatchedFlags();

        //제거할 피스들(매치된 피스들)을 저장할 리스트
        List<Piece> piecesToRemove = new List<Piece>();

        //보드의 모든 피스를 순회하며 매치 확인
        //중복 제거를 피하기 위해 isMatched가 false인 피스만 검사
        //매치된 피스들은 piecesToRemove 리스트에 추가하고 isMatched 플래그를 true로 설정
        //매치가 발견되면 hasMatches를 true로 설정
        //모든 보드를 돌기떄문에 최적화가 필요해보임
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //사용 가능한 노드인지 확인
                if (_boardPieces[x, y].isUsable)
                {
                    //노드의 피스 가져오기
                    Piece piece = _boardPieces[x, y].piece.GetComponent<Piece>();

                    //피스가 아직 매치되지 않았는지 확인
                    if (!piece.isMatched)
                    {
                        //피스가 다른 동일한 색상의 피스들과 연결되어 있는지 확인
                        MatchResult matchPiece = IsConnected(piece);

                        //3개 이상 매치되었는지 확인
                        if (matchPiece.connectedPieces.Count >= 3)
                        {
                            MatchResult superMatchedPieces = SuperMatch(matchPiece);

                            //매치된 피스들을 제거할 리스트에 추가하고 isMatched 플래그 설정
                            piecesToRemove.AddRange(superMatchedPieces.connectedPieces);
                            //매치된 피스들을 전부 isMatched로 플래그 설정
                            foreach (Piece pieceToRemove in superMatchedPieces.connectedPieces)
                            {
                                pieceToRemove.isMatched = true;
                            }

                            //매치가 발견되었음을 표시
                            hasMatches = true;
                        }
                    }
                }
            }
        }

        //매치된 피스들 출력
        return hasMatches;
    }

    private MatchResult SuperMatch(MatchResult _matchedResults)
    {
        // 가로 매칭 or 긴 가로 매칭이 됐을때
        if (_matchedResults.direction == MatchDirection.Horizontal || _matchedResults.direction == MatchDirection.LongHorizontal)
        {
            foreach(Piece pie in _matchedResults.connectedPieces)
            {
                List<Piece> extraConnectedPieces = new();

                CheckDirection(pie, new Vector2Int(0, 1), extraConnectedPieces);

                CheckDirection(pie, new Vector2Int(-0, 1), extraConnectedPieces);

                if (extraConnectedPieces.Count >= 2)
                {
                    Debug.Log("긴 가로 매칭 성사");
                    extraConnectedPieces.AddRange(_matchedResults.connectedPieces);

                    return new MatchResult
                    {
                        connectedPieces = extraConnectedPieces,
                        direction = MatchDirection.Super,
                    };
                }
            }
            return new MatchResult
            {
                connectedPieces = _matchedResults.connectedPieces,
                direction = _matchedResults.direction
            };
        }
        // 세로 매칭 or 긴 세로 매칭이 됐을때
        else if (_matchedResults.direction == MatchDirection.Vertical || _matchedResults.direction == MatchDirection.LongVertical)
        {
            foreach (Piece pie in _matchedResults.connectedPieces)
            {
                List<Piece> extraConnectedPieces = new();

                CheckDirection(pie, new Vector2Int(1, 0), extraConnectedPieces);

                CheckDirection(pie, new Vector2Int(-1, 0), extraConnectedPieces);

                if (extraConnectedPieces.Count >= 2)
                {
                    Debug.Log("긴 세로 매칭 성사");
                    extraConnectedPieces.AddRange(_matchedResults.connectedPieces);

                    return new MatchResult
                    {
                        connectedPieces = extraConnectedPieces,
                        direction = MatchDirection.Super,
                    };
                }
            }
            return new MatchResult
            {
                connectedPieces = _matchedResults.connectedPieces,
                direction = _matchedResults.direction
            };
        }
        return null;
    }

    /// <summary>
    /// 매치 탐색을 시작하기 전에 모든 피스의 매치 플래그를 초기화한다.
    /// ProcessMatches 코루틴에서 hasMatched 값이 잘못 유지되는 문제 방지
    /// </summary>
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

    /// <summary>
    /// 특정 피스가 다른 동일한 생삭의 피스들과 연결되있는지 확인하고
    /// 연결된 피스들의 리스트와 매치 방향 정보를 반환하는 매서드
    /// </summary>
    /// <param name="piece"></param>
    /// <returns></returns>
    private MatchResult IsConnected(Piece piece)
    {
        //연결된 피스들을 저장할 리스트
        //생성과 동시에 현제 피스 추가
        List<Piece> connectedPieces = new() { piece };

        //오른쪽 방향 확인
        CheckDirection(piece, new Vector2Int(1,0), connectedPieces);
        //왼쪽 방향 확인
        CheckDirection(piece, new Vector2Int(-1,0), connectedPieces);
        // 3매치가 되었는지 확인
        if (connectedPieces.Count == 3)
        {
            Debug.Log($"가로 3매치 발생, 색깔 : {connectedPieces[0].pieceType}");

            //연결된 피스들과 매치 방향 정보를 반환
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

            //연결된 피스들과 매치 방향 정보를 반환
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

            //연결된 피스들과 매치 방향 정보를 반환
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

            //연결된 피스들과 매치 방향 정보를 반환
            return new MatchResult()
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongVertical
            };
        }
        else
        {
            //만약 매치된 피스가 없다면 빈 리스트와 None 반환
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
        //피스 타입과 시작 좌표 설정
        PieceType pieceType = originPiece.pieceType;
        int x = originPiece.xIndex + direction.x;
        int y = originPiece.yIndex + direction.y;
        
        //해당 방향으로 연결된 피스들을 확인
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            //보드의 범위를 벗어나지 않는지 확인
            if(!_boardPieces[x, y].isUsable) break;
            
            //이웃한 피스 가져오기
            Piece neighborPiece = _boardPieces[x, y].piece.GetComponent<Piece>();

            //이웃한 피스가 매치되지 않았고 동일한 타입인지 확인
            if (!neighborPiece.isMatched && neighborPiece.pieceType == pieceType)
            {
                //연결된 피스 리스트에 추가
                connectedPieces.Add(neighborPiece);
                
                //다음 위치로 이동
                x += direction.x;
                y += direction.y;
            }
            else break;
        }
    }

    #region pieceSwaping

    /// <summary>
    /// 이동시킬 피스를 선택하는 메서드
    /// </summary>
    /// <param name="piece"></param>
    public void SelectPiece(Piece piece)
    {
        if (selectedPiece is null)
        {
            Debug.Log($"{piece} 선택됨");
            selectedPiece = piece;
        }
        else if (selectedPiece == piece)
        {
            Debug.Log($"{piece} 선택 해제됨");
            selectedPiece = null;
        }
        else if (selectedPiece != piece)
        {
            SwapPieces(selectedPiece, piece);
            selectedPiece = null;
        }
    }

    /// <summary>
    /// 피스를 스왑하는 메서드
    /// </summary>
    /// <param name="currentPiece">이동시킬 대상</param>
    /// <param name="targetPiece">이동될 위치의 대상</param>
    private void SwapPieces(Piece currentPiece, Piece targetPiece)
    {
        if (!IsAdjacent(currentPiece, targetPiece)) 
            return;
        DoSwap(currentPiece, targetPiece);
        
        isProcessingMoving = true;
        
        StartCoroutine(ProcessMatches(currentPiece, targetPiece));
    }

    /// <summary>
    /// 두 노드가 가진 피스를 서로 교환하고 좌표 정보를 동기화한다.
    /// </summary>
    /// <param name="currentPiece"></param>
    /// <param name="targetPiece"></param>
    private void DoSwap(Piece currentPiece, Piece targetPiece)
    {
        Node currentNode = _boardPieces[currentPiece.xIndex, currentPiece.yIndex];
        Node targetNode = _boardPieces[targetPiece.xIndex, targetPiece.yIndex];

        GameObject temp = currentNode.piece; // 현재 노드의 피스를 임시 저장
        currentNode.SetPiece(targetNode.piece);
        targetNode.SetPiece(temp);

        Piece pieceOnCurrentNode = currentNode.piece?.GetComponent<Piece>();
        Piece pieceOnTargetNode = targetNode.piece ? targetNode.piece.GetComponent<Piece>() : null;

        if (pieceOnCurrentNode)
        {
            pieceOnCurrentNode.MoveToTarget(currentNode.transform.position);
        }

        if (pieceOnTargetNode)
        {
            pieceOnTargetNode.MoveToTarget(targetNode.transform.position);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator ProcessMatches(Piece currentPiece, Piece targetPiece)
    {
        yield return new WaitForSeconds(0.2f);

        bool hasMatchOnCurrent = HasMatchAt(currentPiece);
        bool hasMatchOnTarget = HasMatchAt(targetPiece);

        Debug.Log($"현제 매치 상태 = {hasMatchOnCurrent || hasMatchOnTarget}");

        if (!hasMatchOnCurrent && !hasMatchOnTarget)
        {
            DoSwap(currentPiece, targetPiece);
        }

        isProcessingMoving = false;
        
        //StartCoroutine(ProcessMatches(currentPiece, targetPiece));

    }

    private bool HasMatchAt(Piece piece)
    {
        if (piece == null)
        {
            return false;
        }

        ResetMatchedFlags();
        MatchResult matchResult = IsConnected(piece);

        return matchResult.connectedPieces != null && matchResult.connectedPieces.Count >= 3;
    }
    
    /// <summary>
    /// 두 피스가 인접해있는지 확인하는 메서드
    /// </summary>
    /// <param name="currentPiece"></param>
    /// <param name="targetPiece"></param>
    /// <returns></returns>
    private bool IsAdjacent(Piece currentPiece, Piece targetPiece)
    => Mathf.Abs(currentPiece.xIndex - targetPiece.xIndex)
        + Mathf.Abs(currentPiece.yIndex - targetPiece.yIndex) == 1;

    #endregion
    
    
}
