using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

[RequireComponent(typeof(Grid))]
public class MapController : MonoBehaviour
{
    private Dictionary<RootTileType, TileBase> _tileAssetMapping = new Dictionary<RootTileType, TileBase>();

    private Grid _grid;
    private RootTileData[,] _tiles;
    private Random _random;
    
    public int MapWidth = 20;
    public int MapHeight = 20;

    private void Awake()
    {
        _grid = GetComponent<Grid>();
        _tiles = new RootTileData[MapWidth, MapHeight];
        _random = new Random();
        
        for (int row = 0; row < _tiles.GetLength(0); row++)
        {
            for (int col = 0; col < _tiles.GetLength(1); col++)
            {
                _tiles[row, col] = new RootTileData
                {
                    PlayerId = -1,
                    TileType = (RootTileType)_random.Next((int)RootTileType.MAX)
                };
            }
        }
        
        GetPositionsAndTiles(out var positions, out var tileBases);
        
        var tilemap = GetComponentInChildren<Tilemap>();
        tilemap.ClearAllTiles();
        tilemap.SetTiles(positions, tileBases);
    }

    private void GetPositionsAndTiles(out Vector3Int[] positions, out TileBase[] tileBases)
    {
        positions = new Vector3Int[_tiles.Length];
        tileBases = new TileBase[_tiles.Length];

        int i = 0;
        for (int row = 0; row < _tiles.GetLength(0); row++)
        {
            for (int col = 0; col < _tiles.GetLength(1); col++)
            {
                positions[i] = new Vector3Int(col, row, 0);
                tileBases[i] = GetTileAsset(_tiles[row, col].TileType);
                i++;
            }
        }
    }

    private TileBase GetTileAsset(RootTileType rootTileType)
    {
        if (_tileAssetMapping.TryGetValue(rootTileType, out var asset))
            return asset;

        asset = Resources.Load<TileBase>($"Tiles/{Enum.GetName(typeof(RootTileType), rootTileType)}");
        _tileAssetMapping[rootTileType] = asset;

        return asset;
    }
}
