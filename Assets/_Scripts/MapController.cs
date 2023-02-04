﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Random = System.Random;

[RequireComponent(typeof(Grid))]
public class MapController : MonoBehaviour
{
    private static Dictionary<GroundTileType, TileBase> _groundTileAssetMapping = new Dictionary<GroundTileType, TileBase>();
    private static Dictionary<(sbyte, AboveTileType), TileBase> _aboveTileAssetMapping = new Dictionary<(sbyte, AboveTileType), TileBase>();

    private HashSet<Action<Vector3Int>> _onCellClickedCallbacks = new HashSet<Action<Vector3Int>>();

    private Grid _grid;
    private RootTileData[,] _tiles;
    private Random _random;
    private Camera _camera;

    public Tilemap GroundTilemap;
    public Tilemap AboveTilemap;
    public GameObject HoverTile;
    
    public int MapWidth = 40;
    public int MapHeight = 20;
    public Vector2Int[] PlayerStartPositions; 
    
    public event Action<Vector3Int> OnCellClicked
    {
        add => _onCellClickedCallbacks.Add(value);
        remove => _onCellClickedCallbacks.Remove(value);
    }

    private void Awake()
    {
        _grid = GetComponent<Grid>();
        _tiles = new RootTileData[MapWidth, MapHeight];
        _random = new Random();
        _camera = Camera.main;
        
        InitMap();
        
        // update ground tilemap
        GetPositionsAndTiles(GetGroundAsset, out var positions, out var tileBases);
        GroundTilemap.ClearAllTiles();
        GroundTilemap.SetTiles(positions, tileBases);
        
        // update above tilemap
        GetPositionsAndTiles(GetAboveAsset, out positions, out tileBases);
        AboveTilemap.ClearAllTiles();
        AboveTilemap.SetTiles(positions, tileBases);
    }

    private void Update()
    {
        var mouseWorldPosition = _camera.ScreenToWorldPoint(Input.mousePosition);
        var mouseCellPosition = _grid.WorldToCell(mouseWorldPosition);
        
        var mouseGridCenterWorldPosition = _grid.CellToWorld(mouseCellPosition);
        mouseGridCenterWorldPosition.z = 0;
        HoverTile.transform.position = mouseGridCenterWorldPosition;

        if (Input.GetMouseButtonUp((int)MouseButton.LeftMouse))
        {
            Debug.Log($"Clicked {mouseCellPosition}");
            foreach (var callback in _onCellClickedCallbacks)
            {
                callback.Invoke(mouseCellPosition);
            }
        }
    }

    private void InitMap()
    {
        // TODO:  initializing map should probably be in game controller
        
        for (int row = 0; row < _tiles.GetLength(0); row++)
        {
            for (int col = 0; col < _tiles.GetLength(1); col++)
            {
                _tiles[row, col] = new RootTileData
                {
                    PlayerId = -1,
                    GroundType = (GroundTileType)_random.Next((int)AboveTileType.MAX)
                };
            }
        }
        
        // place player trees
        sbyte i = 0;
        foreach (var position in PlayerStartPositions)
        {
            _tiles[position.x, position.y].PlayerId = i++;
            _tiles[position.x, position.y].AboveType = AboveTileType.Tree;
        }
    }

    private void GetPositionsAndTiles(Func<RootTileData, TileBase> getAsset, out Vector3Int[] positions, out TileBase[] tileBases)
    {
        positions = new Vector3Int[_tiles.Length];
        tileBases = new TileBase[_tiles.Length];

        int i = 0;
        for (int row = 0; row < _tiles.GetLength(0); row++)
        {
            for (int col = 0; col < _tiles.GetLength(1); col++)
            {
                positions[i] = new Vector3Int(col, row, 0);
                tileBases[i] = getAsset.Invoke(_tiles[row, col]);
                i++;
            }
        }
    }

    private TileBase GetGroundAsset(RootTileData tileData)
    {
        if (_groundTileAssetMapping.TryGetValue(tileData.GroundType, out var asset))
            return asset;

        asset = Resources.Load<TileBase>($"GroundTiles/{Enum.GetName(typeof(GroundTileType), tileData.GroundType)}");
        _groundTileAssetMapping[tileData.GroundType] = asset;

        return asset;
    }
    
    private TileBase GetAboveAsset(RootTileData tileData)
    {
        if (_aboveTileAssetMapping.TryGetValue((tileData.PlayerId, tileData.AboveType), out var asset))
            return asset;

        var suffix = tileData.PlayerId >= 0 ? tileData.PlayerId.ToString() : string.Empty;
        asset = Resources.Load<TileBase>($"AboveTiles/{Enum.GetName(typeof(AboveTileType), tileData.AboveType)}{suffix}");
        _aboveTileAssetMapping[(tileData.PlayerId, tileData.AboveType)] = asset;

        return asset;
    }
}
