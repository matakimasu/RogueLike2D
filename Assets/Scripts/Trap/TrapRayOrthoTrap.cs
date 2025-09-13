using System.Collections.Generic;
using UnityEngine;

public class TrapRayOrthoTrap : TrapBaseSequenced
{
    protected override IEnumerable<Vector3Int> EnumerateAttackCells(Vector3Int origin)
    {
        foreach (var c in RayCells(origin, Vector3Int.up)) yield return c;
        foreach (var c in RayCells(origin, Vector3Int.down)) yield return c;
        foreach (var c in RayCells(origin, Vector3Int.left)) yield return c;
        foreach (var c in RayCells(origin, Vector3Int.right)) yield return c;
    }
}
