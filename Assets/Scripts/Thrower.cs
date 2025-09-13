using UnityEngine;
using UnityEngine.Tilemaps;

public class Thrower : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Tilemap tilemap;      // Floor用
    public Tilemap wallTilemap;  // Wall用

    public ThrownBullet FireAndReturnBullet(Vector3 mouseWorld)
    {
        if (bulletPrefab == null || tilemap == null || wallTilemap == null)
        {
            Debug.LogWarning("Thrower: 参照不足");
            return null;
        }

        mouseWorld.z = 0;
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);
        Vector3 target = tilemap.GetCellCenterWorld(cellPos);

        GameObject bulletGO = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        var bulletScript = bulletGO.GetComponent<ThrownBullet>();
        if (bulletScript == null) return null;

        bulletScript.SetTarget(target);
        bulletScript.wallTilemap = wallTilemap;

        // ★ここでは NextState() を呼ばない！
        return bulletScript;
    }
}
