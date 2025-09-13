using System.Collections.Generic;
using UnityEngine;

public class AttackProfile : MonoBehaviour
{
    public enum Shape { Cross, DiagonalsOnly, Square, Diamond }
    // Cross=�㉺���E, DiagonalsOnly=�΂߂̂�, Square=8�ߖT�n(Chebyshev), Diamond=�H�`(Manhattan)

    [Min(1)] public int range = 1;
    public Shape shape = Shape.Cross;

    public IEnumerable<Vector3Int> EnumerateCells(Vector3Int origin)
    {
        yield return origin; // ���S���܂߂����Ȃ���΍폜

        switch (shape)
        {
            case Shape.Cross:
                for (int r = 1; r <= range; r++)
                {
                    yield return origin + new Vector3Int(r, 0, 0);
                    yield return origin + new Vector3Int(-r, 0, 0);
                    yield return origin + new Vector3Int(0, r, 0);
                    yield return origin + new Vector3Int(0, -r, 0);
                }
                break;

            case Shape.DiagonalsOnly:
                for (int r = 1; r <= range; r++)
                {
                    yield return origin + new Vector3Int(r, r, 0);
                    yield return origin + new Vector3Int(r, -r, 0);
                    yield return origin + new Vector3Int(-r, r, 0);
                    yield return origin + new Vector3Int(-r, -r, 0);
                }
                break;

            case Shape.Square: // Chebyshev���� <= range
                for (int x = -range; x <= range; x++)
                    for (int y = -range; y <= range; y++)
                        yield return origin + new Vector3Int(x, y, 0);
                break;

            case Shape.Diamond: // Manhattan���� <= range
                for (int x = -range; x <= range; x++)
                {
                    int yMax = range - Mathf.Abs(x);
                    for (int y = -yMax; y <= yMax; y++)
                        yield return origin + new Vector3Int(x, y, 0);
                }
                break;
        }
    }
}
