using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class BoardSystem : MonoBehaviour
{
    /*
     * 작동 알고리즘 요약
     * 0.싱글톤 인스턴스 생성
     * 1.보드 초기화(InitializeBoard)
     *  - 기존 피스 삭제
     *  - 보드 칸 배열 초기화
     *  - 모든 칸 순회하면서 피스 생성
     * 2.업데이트 루프에서 마우스 입력 처리
     *  - 마우스 클릭 시 피스 선택 처리(SelectPiece)
     * 3.피스 선택 처리(SelectPiece)
     *  - 첫 선택 시 피스 저장
     *  - 동일 피스 선택 시 선택 해제
     *  - 두 번째 선택 시 피스 교환 시도(SwapPieces)
     * 4.피스 교환 처리(SwapPieces)
     *  - 두 피스가 인접해있는지 확인(IsAdjacent)
     *  - 피스 교환(DoSwap)
     *      - 보드 칸 배열에서 두 피스 위치 교환
     *      - 피스 인덱스 교환
     *      - 피스 위치 이동
     *  - 매치 처리 코루틴 시작(ProcessMatches)
     * 5.매치 처리 코루틴(ProcessMatches)
     *  - 피스 이동 대기
     *  - 매치 검사 및 처리(CheckBoardToMatches)
     *  - 매치가 없으면 피스 원래 위치로 되돌리기(DoSwap)
     *  - 피스 이동 처리 완료 상태 설정
     * 6.보드 매치 검사 및 처리(CheckBoardToMatches)
     *  - 모든 피스 순회하면서 매치 검사(IsConnected)
     *  - 매치된 피스가 있으면 제거 및 리필 처리 코루틴 시작(ProcessMatchedBoard)
     * 7.매치된 보드 처리 코루틴(ProcessMatchedBoard)
     *  - 매치된 피스들의 매치 플래그 초기화
     *  - 매치된 피스 제거 및 리필 처리(RemoveAndRefill)
     *  - 매치 검사 및 처리 반복
     * 8.피스 제거 및 리필 처리(RemoveAndRefill)
     *  - 제거할 피스들 순회하면서 제거 처리
     *  - 모든 칸 순회하면서 빈 칸 리필 처리(RefillPiece)
     * 9.특정 칸 리필 처리(RefillPiece)
     *  - 위쪽 칸부터 내려오면서 빈 칸 찾기
     *  - 빈 칸 위에 피스가 있으면 아래로 이동
     *  - 만약 위쪽에 피스가 없어서 빈 칸까지 도달했으면 새 피스 생성(SpawnPieceAtTop)
     * 10.특정 X 위치의 맨 위에 새 피스 생성(SpawnPieceAtTop)
     *  - 해당 열에서 가장 낮은 빈 칸 인덱스 찾기(FindIndexOfLowestNull)
     *  - 새 피스 생성
     *  - 피스 이동 처리
     * 11.특정 피스가 매치되었는지 검사(IsConnected)
     *  - 특정 방향으로 매치된 피스들 검사(CheckDirection)
     *  - 매치된 피스 개수에 따라 매치 결과 반환
     * 12.특정 방향으로 매치된 피스들 검사(CheckDirection)
     *  - 해당 방향으로 매치된 피스들 검사 및 리스트에 추가
     * 13.피스 이동처리 완료 콜백(OnPieceMoveComplete)
     */
    
    
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
        //마우스 입력 처리
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        //마우스 위치에서 피스 선택 처리
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = _mainCam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0f));
        Collider2D overlapPoint = Physics2D.OverlapPoint(worldPos);

        //겹친 오브젝트가 없으면 종료
        if (overlapPoint == null)
            return;

        Piece piece = overlapPoint.gameObject.GetComponent<Piece>();//겹친 오브젝트의 피스 컴포넌트 참조
        //피스가 없거나 피스가 이동중이면 종료
        if (piece == null || isProcessingMoving)
            return;

        SelectPiece(piece); //피스 선택 처리
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
    
    /// <summary>
    /// 제거할 피스들을 파라미터로 받아 제거하고 리필하는 함수
    /// </summary>
    /// <param name="removeTargets">제거할 피스들을 받아오는 파라미터</param>
    private void RemoveAndRefill(List<Piece> removeTargets)
    {
        //제거할 피스들 순회하면서 제거 처리
        foreach (Piece piece in removeTargets)
        {
            int xIndex = piece.xIndex;
            int yIndex = piece.yIndex;

            Destroy(piece.gameObject);
            
            _boardPieces[xIndex, yIndex] = new Node(true, null); //보드 칸 배열에서 해당 피스 삭제
        }

        //모든 칸 순회하면서 빈 칸 리필 처리
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //빈 칸이면 리필 처리
                if (_boardPieces[x, y].piece == null && _boardPieces[x, y].isUsable)
                {
                    RefillPiece(x, y);
                }
            }
        }
    }
    
    /// <summary>
    /// 특정 칸을 리필하는 함수
    /// </summary>
    /// <param name="x">리필할 위치 X</param>
    /// <param name="y">리필할 위치 Y</param>
    private void RefillPiece(int x, int y)
    {
        int yOffset = 1;
        
        //위쪽 칸부터 내려오면서 빈 칸 찾기
        while (y + yOffset < height && _boardPieces[x, y + yOffset].piece == null)
        {
            yOffset++; //오프셋 증가
        }
        
        //빈 칸 위에 피스가 있으면 아래로 이동
        if (y + yOffset < height && _boardPieces[x, y + yOffset].piece != null)
        {
            Piece pieceAbove = _boardPieces[x, y + yOffset].piece.GetComponent<Piece>();//위쪽 피스 컴포넌트 참조

            Vector3 targetPos = GetPiecePosition(x, y, pieceAbove.transform.position.z);//이동할 위치 계산
            pieceAbove.MoveToTarget(targetPos);//피스 이동
            pieceAbove.SetIndices(x, y);//피스 인덱스 업데이트

            _boardPieces[x, y] = _boardPieces[x, y + yOffset];//보드 칸 배열 업데이트
            _boardPieces[x, y + yOffset] = new Node(true, null);//이전 칸은 빈 칸으로 설정
        }
        
        //만약 위쪽에 피스가 없어서 빈 칸까지 도달했으면 새 피스 생성
        if (y + yOffset == height)
        {
            SpawnPieceAtTop(x);
        }
    }
    
    /// <summary>
    /// 파라미터로 받은 X 위치의 맨 위에 새 피스 생성
    /// </summary>
    /// <remarks>피스 리필 시 사용됨</remarks>
    /// <param name="x"></param>
    private void SpawnPieceAtTop(int x)
    {
        int index = FindIndexOfLowestNull(x);//해당 열에서 가장 낮은 빈 칸 인덱스 찾기
        
        //만약 빈 칸이 없으면 종료
        if (index < 0)
            return;

        int locationToMoveTo = height - index; //이동할 위치 계산

        int randomIndex = Random.Range(0, piecePrefabs.Length); //랜덤 피스 프리팹 선택
        Vector2 spawnPos = new(x - spacingX, height - spacingY); //피스 생성 위치 계산

        Transform parentForPiece = piecesRoot != null ? piecesRoot : transform; //피스 부모 오브젝트 설정
        GameObject newPiece = Instantiate(piecePrefabs[randomIndex], spawnPos, Quaternion.identity, parentForPiece); //새 피스 생성
        Piece pieceComp = newPiece.GetComponent<Piece>(); //생성된 피스 컴포넌트 참조
        pieceComp.SetIndices(x, index); //피스 인덱스 설정

        _boardPieces[x, index] = new Node(true, newPiece); //보드 칸 배열에 새 피스 할당
        _piecesToDestroy.Add(newPiece); //삭제할 피스 리스트에 추가

        Vector3 targetPosition
            = new Vector3(newPiece.transform.position.x, newPiece.transform.position.y - locationToMoveTo,
                newPiece.transform.position.z); //이동할 위치 계산
        pieceComp.MoveToTarget(targetPosition); //피스 이동
    }

    /// <summary>
    /// 파라미터로 받은 X 위치에서 가장 낮은 빈 칸 인덱스 찾기
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    private int FindIndexOfLowestNull(int x)
    {
        int lowestNull = -1; //가장 낮은 빈 칸 인덱스 초기화
        //해당 열에서 가장 낮은 빈 칸 인덱스 찾기
        for (int y = height - 1; y >= 0; y--)
        {
            if (_boardPieces[x, y].piece == null && _boardPieces[x, y].isUsable)
            {
                lowestNull = y;
            }
        }

        return lowestNull;
    }
    
    /// <summary>
    /// 5개 이상 매치되었는지 검사하는 함수
    /// </summary>
    /// <param name="matchedResults">매치된 피스 결과</param>
    /// <returns></returns>
    private MatchResult SuperMatch(MatchResult matchedResults)
    {
        //가로로 매치된 경우
        if (matchedResults.direction == MatchDirection.Horizontal || matchedResults.direction == MatchDirection.LongHorizontal)
        {
            //매치된 피스들 순회하면서 세로 방향으로 추가 매치 검사
            foreach (Piece pie in matchedResults.connectedPieces)
            {
                List<Piece> extraConnectedPieces = new();//추가로 매치된 피스들 리스트
                CheckDirection(pie, new Vector2Int(0, 1), extraConnectedPieces);//위쪽 방향 검사
                CheckDirection(pie, new Vector2Int(0, -1), extraConnectedPieces);//아래쪽 방향 검사

                //만약 추가로 2개 이상 매치되었으면 슈퍼 매치로 처리
                if (extraConnectedPieces.Count >= 2)
                {
                    extraConnectedPieces.AddRange(matchedResults.connectedPieces);//기존 매치된 피스들도 추가

                    //슈퍼 매치 결과 반환
                    return new MatchResult
                    {
                        connectedPieces = extraConnectedPieces,
                        direction = MatchDirection.Super,
                    };
                }
            }

            //추가 매치가 없으면 기존 매치 결과 반환
            return new MatchResult
            {
                connectedPieces = matchedResults.connectedPieces,
                direction = matchedResults.direction
            };
        }
        //세로로 매치된 경우
        else if (matchedResults.direction == MatchDirection.Vertical || matchedResults.direction == MatchDirection.LongVertical)
        {
            //매치된 피스들 순회하면서 가로 방향으로 추가 매치 검사
            foreach (Piece pie in matchedResults.connectedPieces)
            {
                List<Piece> extraConnectedPieces = new(); //추가로 매치된 피스들 리스트
                CheckDirection(pie, new Vector2Int(1, 0), extraConnectedPieces); //오른쪽 방향 검사
                CheckDirection(pie, new Vector2Int(-1, 0), extraConnectedPieces); //왼쪽 방향 검사

                //만약 추가로 2개 이상 매치되었으면 슈퍼 매치로 처리
                if (extraConnectedPieces.Count >= 2)
                {
                    extraConnectedPieces.AddRange(matchedResults.connectedPieces); //기존 매치된 피스들도 추가

                    //슈퍼 매치 결과 반환
                    return new MatchResult
                    {
                        connectedPieces = extraConnectedPieces,
                        direction = MatchDirection.Super,
                    };
                }
            }

            //추가 매치가 없으면 기존 매치 결과 반환
            return new MatchResult
            {
                connectedPieces = matchedResults.connectedPieces,
                direction = matchedResults.direction
            };
        }

        //기타 경우 기존 매치 결과 반환
        return matchedResults;
    }

    /// <summary>
    /// 모든 피스의 매치 플래그를 초기화하는 함수
    /// </summary>
    private void ResetMatchedFlags()
    {
        //보드 피스 배열이 null이면 종료(보드 없음)
        if (_boardPieces == null)
            return;

        //모든 피스 순회하면서 매치 플래그 초기화
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = _boardPieces[x, y];//현재 노드 참조
                //노드가 null이거나 사용 불가이거나 피스가 없으면 다음 칸으로
                if (node == null || !node.isUsable || node.piece == null)
                    continue;
                
                Piece piece = node.piece.GetComponent<Piece>();//피스 컴포넌트 참조
                //피스가 null이 아니면 매치 플래그 초기화
                if (piece != null)
                {
                    piece.isMatched = false;
                }
            }
        }
    }

    /// <summary>
    /// 특정 피스가 매치되었는지 검사하는 함수
    /// </summary>
    /// <param name="piece"></param>
    /// <returns></returns>
    private MatchResult IsConnected(Piece piece)
    {
        List<Piece> connectedPieces = new() { piece }; //매치된 피스들의 리스트

        CheckDirection(piece, new Vector2Int(1, 0), connectedPieces); //오른쪽 방향 검사
        CheckDirection(piece, new Vector2Int(-1, 0), connectedPieces); //왼쪽 방향 검사
        //가로 매치 일반 검사
        if (connectedPieces.Count == 3)
        {
            //가로 매치 결과 반환
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.Horizontal
            };
        }
        //가로 매치 롱 검사
        else if (connectedPieces.Count > 3)
        {
            //가로 롱 매치 결과 반환
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongHorizontal
            };
        }
        
        connectedPieces.Clear(); //매치된 피스 리스트 초기화
        connectedPieces.Add(piece); //현재 피스 추가

        CheckDirection(piece, new Vector2Int(0, 1), connectedPieces); //위쪽 방향 검사
        CheckDirection(piece, new Vector2Int(0, -1), connectedPieces); //아래쪽 방향 검사
        
        //세로 매치 일반 검사
        if (connectedPieces.Count == 3)
        {
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.Vertical
            };
        }
        //세로 매치 롱 검사
        else if (connectedPieces.Count > 3)
        {
            return new MatchResult
            {
                connectedPieces = connectedPieces,
                direction = MatchDirection.LongVertical
            };
        }

        //매치되지 않음 결과 반환
        return new MatchResult
        {
            connectedPieces = connectedPieces,
            direction = MatchDirection.None
        };
    }

    /// <summary>
    /// 특정 방향으로 매치된 피스들을 검사하는 함수
    /// </summary>
    /// <param name="originPiece">검사 시작 피스</param>
    /// <param name="direction">검사할 방향 벡터</param>
    /// <param name="connectedPieces">매치된 피스들을 담는 리스트</param>
    private void CheckDirection(Piece originPiece, Vector2Int direction, List<Piece> connectedPieces)
    {
        PieceType pieceType = originPiece.pieceType; //검사할 피스 타입
        int x = originPiece.xIndex + direction.x; //검사 시작 X 좌표
        int y = originPiece.yIndex + direction.y; //검사 시작 Y 좌표

        //해당 방향으로 매치된 피스들 검사
        while (x >= 0 && x < width && y >= 0 && y < height)
        {
            //노드가 사용 불가이거나 피스가 없으면 종료
            if (!_boardPieces[x, y].isUsable || _boardPieces[x, y].piece == null)
                break;

            Piece neighborPiece = _boardPieces[x, y].piece.GetComponent<Piece>();//이웃 피스 컴포넌트 참조

            //이웃 피스가 매치되지 않았고 타입이 동일하면 매치된 피스 리스트에 추가
            if (!neighborPiece.isMatched && neighborPiece.pieceType == pieceType)
            {
                connectedPieces.Add(neighborPiece); //매치된 피스 리스트에 추가

                x += direction.x; //다음 칸으로 이동
                y += direction.y; //다음 칸으로 이동
            }
            else break;
        }
    }

    /// <summary>
    /// 피스 선택 처리 함수
    /// </summary>
    /// <param name="piece">선택된 피스</param>
    public void SelectPiece(Piece piece)
    {
        //선택된 피스가 없으면 현재 피스를 선택
        if (selectedPiece == null)
        {
            selectedPiece = piece;
        }
        //선택된 피스가 현재 피스와 같으면 선택 해제
        else if (selectedPiece == piece)
        {
            selectedPiece = null;
        }
        //선택된 피스가 현재 피스와 다르면 피스 교환 시도
        else if (selectedPiece != piece)
        {
            SwapPieces(selectedPiece, piece);
            selectedPiece = null;
        }
    }

    /// <summary>
    /// 두개의 피스를 서로 교환하는 함수
    /// </summary>
    /// <param name="currentPiece">현제 선택된 피스</param>
    /// <param name="targetPiece">이동될 피스</param>
    private void SwapPieces(Piece currentPiece, Piece targetPiece)
    {
        //두 피스가 인접해있는지 확인
        if (!IsAdjacent(currentPiece, targetPiece))
            return;

        DoSwap(currentPiece, targetPiece); //피스 교환 처리

        isProcessingMoving = true; //피스 이동 처리 중 상태 설정

        StartCoroutine(ProcessMatches(currentPiece, targetPiece)); //매치 처리 코루틴 시작
    }

    /// <summary>
    /// 두 피스를 실제로 교환하는 함수
    /// </summary>
    /// <param name="currentPiece"></param>
    /// <param name="targetPiece"></param>
    private void DoSwap(Piece currentPiece, Piece targetPiece)
    {
        //보드 칸 배열에서 두 피스의 위치 교환
        //HACK: 구조 스왑 사용
        (_boardPieces[currentPiece.xIndex, currentPiece.yIndex].piece, _boardPieces[targetPiece.xIndex, targetPiece.yIndex].piece)
            = (_boardPieces[targetPiece.xIndex, targetPiece.yIndex].piece, _boardPieces[currentPiece.xIndex, currentPiece.yIndex].piece);

        int tempXIndex = currentPiece.xIndex; //피스 인덱스 교환
        int tempYIndex = currentPiece.yIndex; //피스 인덱스 교환
        currentPiece.xIndex = targetPiece.xIndex; //피스 인덱스 교환
        currentPiece.yIndex = targetPiece.yIndex; //피스 인덱스 교환
        targetPiece.xIndex = tempXIndex; //피스 인덱스 교환
        targetPiece.yIndex = tempYIndex; //피스 인덱스 교환
        
        //피스 위치 이동(currentPiece 가 targetPiece 위치로)
        currentPiece.MoveToTarget(GetPiecePosition(currentPiece.xIndex, currentPiece.yIndex, currentPiece.transform.position.z));
        //피스 위치 이동(targetPiece 가 currentPiece 위치로)
        targetPiece.MoveToTarget(GetPiecePosition(targetPiece.xIndex, targetPiece.yIndex, targetPiece.transform.position.z));
    }

    /// <summary>
    /// 매치 처리 코루틴
    /// currentPiece 와 targetPiece 를 교환한 후 매치 검사 및 처리
    /// </summary>
    /// <param name="currentPiece">현제 선택된 피스</param>
    /// <param name="targetPiece">이동될 피스</param>
    /// <returns></returns>
    private IEnumerator ProcessMatches(Piece currentPiece, Piece targetPiece)
    {
        yield return new WaitForSeconds(0.2f);//피스 이동 대기

        //매치 검사 및 처리
        if (CheckBoardToMatches(true))
        {
            yield return null;
        }
        //매치가 없으면 피스 원래 위치로 되돌리기
        else
        {
            DoSwap(currentPiece, targetPiece);
        }

        //피스 이동 처리 완료 상태 설정
        isProcessingMoving = false;
    }

    /// <summary>
    /// 두 피스가 인접해있는지 확인하는 함수
    /// </summary>
    /// <param name="currentPiece"></param>
    /// <param name="targetPiece"></param>
    /// <returns></returns>
    private bool IsAdjacent(Piece currentPiece, Piece targetPiece)
    {
        return Mathf.Abs(currentPiece.xIndex - targetPiece.xIndex) + Mathf.Abs(currentPiece.yIndex - targetPiece.yIndex) == 1;
    }
    
    /// <summary>
    /// 피스의 위치를 계산하는 함수
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    private Vector3 GetPiecePosition(int x, int y, float z = 0f)
    {
        return new Vector3(x - spacingX, y - spacingY, z);
    }
}
