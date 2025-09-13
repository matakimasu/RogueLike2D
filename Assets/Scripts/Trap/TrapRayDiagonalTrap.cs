using System.Collections.Generic;
using UnityEngine;

public class TrapRayDiagonalTrap : TrapBaseSequenced
{
    protected override IEnumerable<Vector3Int> EnumerateAttackCells(Vector3Int origin)
    {
        foreach (var c in RayCells(origin, new Vector3Int(1, 1, 0))) yield return c;
        foreach (var c in RayCells(origin, new Vector3Int(1, -1, 0))) yield return c;
        foreach (var c in RayCells(origin, new Vector3Int(-1, 1, 0))) yield return c;
        foreach (var c in RayCells(origin, new Vector3Int(-1, -1, 0))) yield return c;
    }
}
