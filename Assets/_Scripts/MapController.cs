using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static DG.Tweening.DOTween;
using Random = System.Random;

[RequireComponent(typeof(Grid))]
public class MapController : MonoBehaviour
{
    private static Dictionary<GroundTileType, TileBase> _groundTileAssetMapping = new Dictionary<GroundTileType, TileBase>();
    private static Dictionary<(sbyte, AboveTileType), TileBase> _aboveTileAssetMapping = new Dictionary<(sbyte, AboveTileType), TileBase>();

    private ReleasePool<SpriteRenderer> _tileOverlayReleasePool;
    private readonly HashSet<Action<Vector3Int>> _onCellClickedCallbacks = new HashSet<Action<Vector3Int>>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> _overlayObjects = new Dictionary<Vector2Int, SpriteRenderer>();

    private Grid _grid;
    private RootTileData[,] _tiles;
    private Random _random;
    private Camera _camera;
    
    public Tilemap GroundTilemap;
    public Tilemap AboveTilemap;
    public GameObject HoverTile;
    public SpriteRenderer OverlayTilePrefab;
    
    public int MapWidth = 40;
    public int MapHeight = 20;
    public Vector2Int[] PlayerStartPositions; 
    
    public event Action<Vector3Int> OnHexCellClicked
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

        _tileOverlayReleasePool = new ReleasePool<SpriteRenderer>(() => Instantiate(OverlayTilePrefab));
        
        InitMap();
        
        SetMap(_tiles, new []{ new Vector2Int(0, 0), new Vector2Int(3, 4)});
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

    public void SetMap(RootTileData[,] tiles, Vector2Int[] tileOverlayPositions)
    {
        // update ground tilemap
        GetPositionsAndTiles(tiles, GetGroundAsset, out var positions, out var tileBases);
        GroundTilemap.ClearAllTiles();
        GroundTilemap.SetTiles(positions, tileBases);
        
        // update above tilemap
        GetPositionsAndTiles(tiles, GetAboveAsset, out positions, out tileBases);
        AboveTilemap.ClearAllTiles();
        AboveTilemap.SetTiles(positions, tileBases);

        foreach (var position in tileOverlayPositions)
        {
            if (!_overlayObjects.TryGetValue(position, out var overlayObject))
            {
                overlayObject = _tileOverlayReleasePool.Get();
            }

            overlayObject.transform.position = _grid.CellToWorld(new Vector3Int(position.x, position.y, 0));
            overlayObject.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            overlayObject.DOColor(new Color(1f, 1f, 1f, 0.5f), 0.6f).SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void GetPositionsAndTiles(RootTileData[,] tiles, Func<RootTileData, TileBase> getAsset, out Vector3Int[] positions, out TileBase[] tileBases)
    {
        positions = new Vector3Int[tiles.Length];
        tileBases = new TileBase[tiles.Length];

        int i = 0;
        for (int row = 0; row < tiles.GetLength(0); row++)
        {
            for (int col = 0; col < tiles.GetLength(1); col++)
            {
                positions[i] = new Vector3Int(col, row, 0);
                tileBases[i] = getAsset.Invoke(tiles[row, col]);
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
