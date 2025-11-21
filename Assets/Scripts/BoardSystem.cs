using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class BoardSystem : MonoBehaviour
{
    [SerializeField] private int width = 6;
    [SerializeField] private int height = 8;

    [SerializeField] private float spacingX;
    [SerializeField] private float spacingY;

    [SerializeField] private GameObject[] piecePrefabs;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private ArrayLayout arrayLayout;

    [SerializeField] private Piece selectedPiece;
    [SerializeField] private bool isProcessingMoving;

    [SerializeField] private List<Piece> piecesToRemove = new();

    private Node[,] _boardPieces;
    private readonly List<GameObject> _piecesToDestroy = new();

    private Camera _mainCam;

    public static BoardSystem Instance;

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

    private void InitializeBoard()
    {
        DestroyPieces();
        _boardPieces = new Node[width, height];

        spacingX = (float)(width - 1) / 2f;
        spacingY = (float)(height - 1) / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new(x - spacingX, y - spacingY);

                if (arrayLayout != null && arrayLayout.IsBlocked(x, y))
                {
                    _boardPieces[x, y] = new Node(false, null);
                    continue;
                }

                int randomIndex = Random.Range(0, piecePrefabs.Length);
                Transform parentForPiece = piecesRoot != null ? piecesRoot : transform;
                GameObject pieceGo = Instantiate(piecePrefabs[randomIndex], position, Quaternion.identity, parentForPiece);
                Piece pieceComp = pieceGo.GetComponent<Piece>();
                if (pieceComp != null)
                {
                    pieceComp.SetIndices(x, y);
                }

                _boardPieces[x, y] = new Node(true, pieceGo);
                _piecesToDestroy.Add(pieceGo);
            }
        }

        if (CheckBoardToMatches(false))
        {
            InitializeBoard();
        }
    }

    private void DestroyPieces()
    {
        if (_piecesToDestroy.Count == 0)
            return;

        foreach (GameObject piece in _piecesToDestroy)
        {
            if (piece != null)
            {
                Destroy(piece);
            }
        }

        _piecesToDestroy.Clear();
    }

    public bool CheckBoardToMatches(bool takeAction)
    {
        if (_boardPieces == null)
            return false;

        bool hasMatched = false;

        piecesToRemove.Clear();
        ResetMatchedFlags();

        foreach (Node nodePiece in _boardPieces)
        {
            if (nodePiece != null && nodePiece.piece != null)
            {
                nodePiece.piece.GetComponent<Piece>().isMatched = false;
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!_boardPieces[x, y].isUsable || _boardPieces[x, y].piece == null)
                    continue;

                Piece piece = _boardPieces[x, y].piece.GetComponent<Piece>();
                if (piece.isMatched)
                    continue;

                MatchResult matchedPieces = IsConnected(piece);
                if (matchedPieces.connectedPieces.Count >= 3)
                {
                    MatchResult superMatchedPieces = SuperMatch(matchedPieces);

                    piecesToRemove.AddRange(superMatchedPieces.connectedPieces);

                    foreach (Piece pie in superMatchedPieces.connectedPieces)
                    {
                        pie.isMatched = true;
                    }

                    hasMatched = true;
                }
            }
        }

        if (hasMatched && takeAction)
        {
            StartCoroutine(ProcessMatchedBoard());
        }

        return hasMatched;
    }

    private IEnumerator ProcessMatchedBoard()
    {
        foreach (Piece piece in piecesToRemove)
        {
            piece.isMatched = false;
        }

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
