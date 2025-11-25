using System;

[Serializable]
public class ArrayLayout
{
    public Row[] rows;

    /// <summary>
    /// 이동할려는 칸이 막혀있는지 확인
    /// </summary>
    /// <param name="x">이동할려는 x 값</param>
    /// <param name="y">이동할려는 y 값</param>
    /// <returns></returns>
    public bool IsBlocked(int x, int y)
    {
        // 배열 범위 밖이면 막힌 것으로 간주
        if (rows == null || y < 0 || y >= rows.Length)
            return false;

        // 해당 행이 null이거나 열 범위 밖이면 막힌 것으로 간주
        Row row = rows[y];
        
        // 해당 위치가 막혀있는지 반환
        if (row?.row == null || x < 0 || x >= row.row.Length)
            return false;

        // 실제 막힘 상태 반환
        return row.row[x];
    }
}

[Serializable]
public class Row
{
    public bool[] row;
}
