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
    private GameObject _poolParent;
    private GameObject _overlayParent;
    private readonly HashSet<Action<Vector2Int>> _onCellClickedCallbacks = new HashSet<Action<Vector2Int>>();
    private readonly Dictionary<Vector3Int, SpriteRenderer> _overlayObjects = new Dictionary<Vector3Int, SpriteRenderer>();

    private Grid _grid;
    private Camera _camera;
    
    
    public Tilemap GroundTilemap;
    public Tilemap AboveTilemap;
    public GameObject HoverTile;
    public SpriteRenderer OverlayTilePrefab;
    
    public Vector3Int HexOffset = Vector3Int.zero;

    public Vector2Int HexToGridPos(Vector3Int hexPos) => new Vector2Int(hexPos.y - HexOffset.y, hexPos.x - HexOffset.x);
    public Vector3Int GridToHexPos(Vector2Int gridPos) => new Vector3Int(gridPos.y, gridPos.x, 0) + HexOffset;

    public event Action<Vector2Int> OnHexCellClicked
    {
        add => _onCellClickedCallbacks.Add(value);
        remove => _onCellClickedCallbacks.Remove(value);
    }

    private void Awake()
    {
        _grid = GetComponent<Grid>();
        _camera = Camera.main;

        _poolParent = new GameObject("releasePool");
        _poolParent.transform.SetParent(transform);
        _overlayParent = new GameObject("activeOverlays");
        _overlayParent.transform.SetParent(transform);
        _tileOverlayReleasePool = new ReleasePool<SpriteRenderer>(
            () => Instantiate(OverlayTilePrefab),
            sr =>
            {
                sr.transform.SetParent(_overlayParent.transform);
                sr.gameObject.SetActive(true);
            },
            sr =>
            {
                sr.transform.SetParent(_poolParent.transform);
                sr.gameObject.SetActive(false);
            }
            );
    }

    public void GetGameStateFromTilemap(out RootTileData[,] tiles, out bool zeroIsOddColumn)
    {
        GroundTilemap.CompressBounds();
        AboveTilemap.CompressBounds();
        
        var bounds = GroundTilemap.cellBounds;
        if (AboveTilemap.cellBounds.min.x < bounds.min.x
            || AboveTilemap.cellBounds.min.y < bounds.min.y
            || AboveTilemap.cellBounds.max.x < bounds.max.x
            || AboveTilemap.cellBounds.max.y < bounds.max.y)
        {
            Debug.LogWarning("Above tilemap bounds extend beyond Ground tilemap bounds!");
        }

        HexOffset = bounds.min;
        zeroIsOddColumn = bounds.min.y % 2 != 0;
        
        tiles = new RootTileData[bounds.size.y, bounds.size.x];
        var rect = new RectInt(0, 0, tiles.GetLength(0), tiles.GetLength(1));
        foreach (var pos in rect.allPositionsWithin)
        {
            tiles[pos.x, pos.y] = TileAssetToData(GridToHexPos(pos));
        }
    }

    private RootTileData TileAssetToData(Vector3Int hexPos)
    {
        var groundTileBase = GroundTilemap.GetTile(hexPos);
        var groundType = GroundTileType.None;
        if (groundTileBase != null)
        {
            Enum.TryParse(groundTileBase.name, out groundType);
        }
        
        var aboveTileBase = AboveTilemap.GetTile(hexPos);
        var aboveType = AboveTileType.None;
        sbyte playerId = -1;
        if (aboveTileBase != null)
        {
            var playerIdPattern = new Regex(@"^(?<tileName>\w+)(?<playerId>\d+)?$");
            var match = playerIdPattern.Match(aboveTileBase.name);
            if (match.Success)
            {
                sbyte.TryParse(match.Groups["playerId"].Value, out playerId);
            }
            Enum.TryParse(match.Groups["tileName"].Value, out aboveType);
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
        var mouseHexPosition = _grid.WorldToCell(mouseWorldPosition);
        
        var mouseGridCenterWorldPosition = _grid.CellToWorld(mouseHexPosition);
        mouseGridCenterWorldPosition.z = 0;
        HoverTile.transform.position = mouseGridCenterWorldPosition;

        if (Input.GetMouseButtonUp((int)MouseButton.LeftMouse))
        {
            Debug.Log($"Clicked {mouseHexPosition}");
            foreach (var callback in _onCellClickedCallbacks)
            {
                callback.Invoke(HexToGridPos(mouseHexPosition));
            }
        }
    }

    public void SetMap(RootTileData[,] tiles, IEnumerable<Vector2Int> tileOverlayPositions, HashSet<Vector2Int> tileFogPositions)
    {
        // update ground tilemap
        GetPositionsAndTiles(tiles, GetGroundAsset, tileFogPositions, false, out var positions, out var tileBases);
        GroundTilemap.ClearAllTiles();
        GroundTilemap.SetTiles(positions, tileBases);

        // update above tilemap
        /*if (tileFogPositions == null)
        {
            GetPositionsAndTiles(tiles, GetAboveAsset, tileFogPositions, false, out positions, out tileBases);
        }
        else
        {*/
        GetPositionsAndTiles(tiles, GetAboveAsset, tileFogPositions, true, out positions, out tileBases);
        AboveTilemap.ClearAllTiles();
        AboveTilemap.SetTiles(positions, tileBases);

        HashSet<Vector3Int> remainingOverlays = new HashSet<Vector3Int>(_overlayObjects.Keys);
        
        foreach (var gridPos in tileOverlayPositions)
        {
            var hexPosition = GridToHexPos(gridPos);
            if (!_overlayObjects.TryGetValue(hexPosition, out var overlayObject))
            {
                overlayObject = _tileOverlayReleasePool.Get();
                _overlayObjects[hexPosition] = overlayObject;
            }

            remainingOverlays.Remove(hexPosition);
            overlayObject.transform.position = _grid.CellToWorld(hexPosition);
            overlayObject.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            overlayObject.DOKill();
            overlayObject.DOColor(new Color(1f, 1f, 1f, 0.5f), 0.6f).SetLoops(-1, LoopType.Yoyo);
        }

        foreach (var gridPos in tileFogPositions)
        {
            var hexPosition = GridToHexPos(gridPos);
            if (!_overlayObjects.TryGetValue(hexPosition, out var overlayObject))
            {
                overlayObject = _tileOverlayReleasePool.Get();
                _overlayObjects[hexPosition] = overlayObject;
            }

            remainingOverlays.Remove(hexPosition);
            overlayObject.transform.position = _grid.CellToWorld(hexPosition);
            overlayObject.color = new Color(0f, 0f, 0f, 0.5f);
            overlayObject.DOKill();
            //overlayObject.DOColor(new Color(1f, 1f, 1f, 0.5f), 0.6f).SetLoops(-1, LoopType.Yoyo);
        }

        foreach (var hexPos in remainingOverlays)
        {
            _tileOverlayReleasePool.Release(_overlayObjects[hexPos]);
            _overlayObjects.Remove(hexPos);
        }
    }

    private void GetPositionsAndTiles(RootTileData[,] tiles, Func<RootTileData, TileBase> getAsset, HashSet<Vector2Int> fogTiles, bool calcFog, out Vector3Int[] positions, out TileBase[] tileBases)
    {
        if(calcFog == true)
        {
            positions = new Vector3Int[tiles.Length - fogTiles.Count];
            tileBases = new TileBase[tiles.Length - fogTiles.Count];
        }
        else
        {
            positions = new Vector3Int[tiles.Length];
            tileBases = new TileBase[tiles.Length];
        }
        

        int i = 0;
        var rect = new RectInt(Vector2Int.zero, new Vector2Int(tiles.GetLength(0), tiles.GetLength(1)));
        foreach (var gridPos in rect.allPositionsWithin)
        {
            if (calcFog == false || !fogTiles.Contains(gridPos)) {
                positions[i] = GridToHexPos(gridPos);
                tileBases[i] = getAsset.Invoke(tiles[gridPos.x, gridPos.y]);
                i++;
            }
        }
    }

    private TileBase GetGroundAsset(RootTileData tileData)
    {
        if (_groundTileAssetMapping.TryGetValue(tileData.GroundType, out var asset))
            return asset;

        if (tileData.GroundType == GroundTileType.None)
            return null;
        
        asset = Resources.Load<TileBase>($"GroundTiles/{Enum.GetName(typeof(GroundTileType), tileData.GroundType)}");
        _groundTileAssetMapping[tileData.GroundType] = asset;

        return asset;
    }
    
    private TileBase GetAboveAsset(RootTileData tileData)
    {
        if (_aboveTileAssetMapping.TryGetValue((tileData.PlayerId, tileData.AboveType), out var asset))
            return asset;

        if (tileData.AboveType == AboveTileType.None)
            return null;
        
        var suffix = tileData.PlayerId >= 0 ? tileData.PlayerId.ToString() : string.Empty;
        asset = Resources.Load<TileBase>($"AboveTiles/{Enum.GetName(typeof(AboveTileType), tileData.AboveType)}{suffix}");
        _aboveTileAssetMapping[(tileData.PlayerId, tileData.AboveType)] = asset;

        return asset;
    }
}
