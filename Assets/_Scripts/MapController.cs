using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using static DG.Tweening.DOTween;

[RequireComponent(typeof(Grid))]
public class MapController : MonoBehaviour
{
    private static Dictionary<GroundTileType, TileBase> _groundTileAssetMapping = new Dictionary<GroundTileType, TileBase>();
    private static Dictionary<(sbyte, AboveTileType), TileBase> _aboveTileAssetMapping = new Dictionary<(sbyte, AboveTileType), TileBase>();

    private ReleasePool<SpriteRenderer> _tileOverlayReleasePool;
    private readonly HashSet<Action<Vector3Int>> _onCellClickedCallbacks = new HashSet<Action<Vector3Int>>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> _overlayObjects = new Dictionary<Vector2Int, SpriteRenderer>();

    private Grid _grid;
    private Camera _camera;
    
    public Tilemap GroundTilemap;
    public Tilemap AboveTilemap;
    public GameObject HoverTile;
    public SpriteRenderer OverlayTilePrefab;

    public event Action<Vector3Int> OnHexCellClicked
    {
        add => _onCellClickedCallbacks.Add(value);
        remove => _onCellClickedCallbacks.Remove(value);
    }

    private void Awake()
    {
        _grid = GetComponent<Grid>();
        _camera = Camera.main;

        _tileOverlayReleasePool = new ReleasePool<SpriteRenderer>(
            () => Instantiate(OverlayTilePrefab),
            sr => sr.gameObject.SetActive(true),
            sr => sr.gameObject.SetActive(false)
            );
    }

    public void GetGameStateFromTilemap(out RootTileData[,] tiles)
    {
        var bounds = GroundTilemap.cellBounds;
        if (AboveTilemap.cellBounds.min.x < bounds.min.x
            || AboveTilemap.cellBounds.min.y < bounds.min.y
            || AboveTilemap.cellBounds.max.x < bounds.max.x
            || AboveTilemap.cellBounds.max.y < bounds.max.y)
        {
            Debug.LogWarning("Above tilemap bounds extend beyond Ground tilemap bounds!");
        }

        Vector3Int offset = bounds.min;
        tiles = new RootTileData[bounds.size.y, bounds.size.x];
        var rect = new RectInt(0, 0, tiles.GetLength(0), tiles.GetLength(1));
        foreach (var pos in rect.allPositionsWithin)
        {
            tiles[pos.x, pos.y] = TileAssetToData(new Vector3Int(pos.y, pos.x, 0), offset);
        }
    }

    private RootTileData TileAssetToData(Vector3Int pos, Vector3Int offset)
    {
        var groundTileBase = GroundTilemap.GetTile(pos + offset);
        var groundType = GroundTileType.None;
        if (groundTileBase != null)
        {
            Enum.TryParse(groundTileBase.name, out groundType);
        }
        
        var aboveTileBase = AboveTilemap.GetTile(pos + offset);
        var aboveType = AboveTileType.None;
        sbyte playerId = -1;
        if (aboveTileBase != null)
        {
            var playerIdPattern = new Regex(@"^\w+(?<playerId>\d+)$");
            var match = playerIdPattern.Match(aboveTileBase.name);
            if (match.Success)
            {
                sbyte.TryParse(match.Groups["playerId"].Value, out playerId);
            }
            Enum.TryParse(aboveTileBase.name, out aboveType);
        }
        
        return new RootTileData
        {
            PlayerId = playerId,
            GroundType = groundType,
            AboveType = aboveType,
        };
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
                callback.Invoke(new Vector3Int(mouseCellPosition.y, mouseCellPosition.x, 0));
            }
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

        HashSet<Vector2Int> remainingOverlays = new HashSet<Vector2Int>(_overlayObjects.Keys);
        
        foreach (var position in tileOverlayPositions)
        {
            if (!_overlayObjects.TryGetValue(position, out var overlayObject))
            {
                overlayObject = _tileOverlayReleasePool.Get();
                _overlayObjects[position] = overlayObject;
            }

            remainingOverlays.Remove(position);
            overlayObject.transform.position = _grid.CellToWorld(new Vector3Int(position.y, position.x, 0));
            overlayObject.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            overlayObject.DOColor(new Color(1f, 1f, 1f, 0.5f), 0.6f).SetLoops(-1, LoopType.Yoyo);
        }

        foreach (var position in remainingOverlays)
        {
            _tileOverlayReleasePool.Release(_overlayObjects[position]);
            _overlayObjects.Remove(position);
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
