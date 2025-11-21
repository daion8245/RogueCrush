using System;

[Serializable]
public class ArrayLayout
{
    public Row[] rows;

    public bool IsBlocked(int x, int y)
    {
        if (rows == null || y < 0 || y >= rows.Length)
            return false;

        Row row = rows[y];
        if (row?.row == null || x < 0 || x >= row.row.Length)
            return false;

        return row.row[x];
    }
}

[Serializable]
public class Row
{
    public bool[] row;
}
