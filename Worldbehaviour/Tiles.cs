using UnityEngine;
using UnityEngine.Tilemaps;

public class CenterTile : MonoBehaviour
{
    void Start()
    {
        Tilemap tilemap = GetComponent<Tilemap>();
        Vector3Int cellPosition = tilemap.WorldToCell(transform.position);
        transform.position = tilemap.GetCellCenterWorld(cellPosition);
        transform.localScale = new Vector3(2.5f, 2.5f, 1); // Adjust scale to fit 2.5x2.5 grid
    }
}

