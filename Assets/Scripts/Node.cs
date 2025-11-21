using UnityEngine;

[System.Serializable]
public class Node
{
    public bool isUsable;
    public GameObject piece;

    public int x;
    public int y;

    public bool IsEmpty => piece == null;

    public Node()
    {
    }

    public Node(bool usable, GameObject attachedPiece)
    {
        isUsable = usable;
        piece = attachedPiece;
    }

    public void Initialize(int kX, int kY, bool kUsable = true)
    {
        x = kX;
        y = kY;
        isUsable = kUsable;
    }

    public void SetPiece(GameObject go)
    {
        piece = go;
        var p = go != null ? go.GetComponent<Piece>() : null;
        if (p != null)
        {
            p.SetIndices(x, y);
        }
    }

    public void Clear() => piece = null;
}
