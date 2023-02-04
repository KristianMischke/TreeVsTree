using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class GameController : MonoBehaviour
{
    private Random _random;
    private RootTileData[,] _tiles;//TODO keep track of tiles in here
    private bool _areTilesDirty = true;

    public struct Player
    {
        public sbyte Id;
        public int TilesControled;

        /// <summary>Number of moves that a player can make in a single turn</summary>
        public int NumMoves;
    }

    private int _tilesForVictory = 20;
    private int _turnNumber = 0;

    private sbyte _numPlayers = 2;
    private sbyte _playerTurn = 0;

    private Player[] _players;

    public int MapWidth => _tiles.GetLength(0);
    public int MapHeight => _tiles.GetLength(1);
    public RectInt.PositionEnumerator AllTilesIter =>
        new RectInt(Vector2Int.zero, new Vector2Int(MapWidth, MapHeight)).allPositionsWithin;

    // Scene references
    public MapController MapController;

    public sbyte PlayerWon;

    private HashSet<Vector2Int> _playerTiles;

    // Start is called before the first frame update
    void Start()
    {
        MapController.OnHexCellClicked += OnTileClicked;

        _random = new Random();
        MapController.GetGameStateFromTilemap(out _tiles);
        InitializePlayers();
    }

    private void OnDestroy()
    {
        MapController.OnHexCellClicked -= OnTileClicked;
    }

    // Update is called once per frame
    void Update()
    {
        if (_areTilesDirty)
        {
            _areTilesDirty = false;
            _playerTiles = giveValidTiles(_playerTurn);
            //List<Vector2Int> playerTilesList = _playerTiles.ToList();
            MapController.SetMap(_tiles, _playerTiles);
            //MapController.SetMap(_tiles, new[] { new Vector2Int(_random.Next(MapWidth), _random.Next(MapHeight)), new Vector2Int(3, 4) });
        }
    }

    private void OnTileClicked(Vector3Int position)
    {
        _areTilesDirty = AttackTile(position);
        
        PlayerWon = (sbyte) CheckVictory();

        if(_areTilesDirty){
            NextTurn();
        }
    }

    private bool AttackTile(Vector3Int position){
        bool validMove = true;
        
        Vector2Int positionxy = new Vector2Int(position.x, position.y);

        if(_playerTiles.Contains(positionxy))
        {
            //if empty tile, fill in with this player's roots
            if(_tiles[position.x, position.y].PlayerId == -1)
            {        
                _tiles[position.x, position.y].PlayerId = _playerTurn;
                _tiles[position.x, position.y].AboveType = AboveTileType.TreeRoots;
            }
            //else, if another player's roots, remove them
            else if(_tiles[position.x, position.y].PlayerId != _playerTurn)
            {
                _tiles[position.x, position.y].PlayerId = -1;
                _tiles[position.x, position.y].AboveType = AboveTileType.MAX;
            }
            else
            {
                validMove = false;
            }
        }
        else
        {
            validMove = false;
        }

        _areTilesDirty = validMove;

        return validMove;
    }

    private HashSet<Vector2Int> giveValidTiles(int playerID)
    {
        HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
        for(int i = 0; i < MapWidth; i++) // Adds valid tiles
        {
            for(int j = 0; j < MapHeight; j++)
            {
                if(_tiles[i,j].PlayerId == _playerTurn){
                    if(i%2 == 0) // Not an offset column
                    {
                        validTiles.Add(new Vector2Int(i - 1, j - 1)); // Lower left tile
                        validTiles.Add(new Vector2Int(i - 1, j)); // Upper left tile
                        validTiles.Add(new Vector2Int(i, j - 1)); // Tile below
                        validTiles.Add(new Vector2Int(i, j + 1)); // Tile above
                        validTiles.Add(new Vector2Int(i + 1, j - 1)); // Lower right tile
                        validTiles.Add(new Vector2Int(i + 1, j)); // Upper right tile
                    }
                    else // Offset column
                    {
                        validTiles.Add(new Vector2Int(i - 1, j)); // Lower left tile
                        validTiles.Add(new Vector2Int(i - 1, j + 1)); // Upper left tile
                        validTiles.Add(new Vector2Int(i, j - 1)); // Tile below
                        validTiles.Add(new Vector2Int(i, j + 1)); // Tile above
                        validTiles.Add(new Vector2Int(i + 1, j)); // Lower right tile
                        validTiles.Add(new Vector2Int(i + 1, j + 1)); // Upper right tile
                    }
                    
                }
            }
        }

        for (int i = 0; i < MapWidth; i++) // Loops to remove occupied tiles
        {
            for (int j = 0; j < MapHeight; j++)
            {
                if (_tiles[i, j].PlayerId == _playerTurn || _tiles[i,j].GroundType == GroundTileType.MountainTile)
                {
                    validTiles.Remove(new Vector2Int(i, j));
                }
            }
        }
        return validTiles;
    }

    private void InitializePlayers(){
        _players = new Player[_numPlayers];

        for(sbyte i = 0; i < _numPlayers; i++){
            _players[i] = new Player
            {
                Id = i,
                TilesControled = 1,
                NumMoves = 2
            };
        }
    }

    private void NextTurn(){
        _turnNumber++;
        _playerTurn++;
        if(_playerTurn >= _numPlayers){
            _playerTurn = 0;
        }
    }

    private Vector2Int? GetPlayerTreePosition(sbyte playerId)
    {
        foreach (var pos in AllTilesIter)
        {
            if (_tiles[pos.x, pos.y].PlayerId == playerId && _tiles[pos.x, pos.y].AboveType == AboveTileType.Tree)
            {
                return pos;
            }
        }

        return null;
    }

    private sbyte CheckVictory(){//TODO test to make sure this works
        sbyte victory = -1;
        
        foreach(Player player in _players){
            if(player.TilesControled >= _tilesForVictory){
                victory = player.Id;
            }
        }
        //else case for taking base tree and way to check if base tree taken
        if(victory == -1)
        {
            HashSet<sbyte> remainingPlayers = new HashSet<sbyte>();
            for (sbyte i = 0; i < _numPlayers; i++)
            {
                var treePosition = GetPlayerTreePosition(i);
                if (treePosition != null)
                {
                    remainingPlayers.Add(i);
                }
            }

            if (remainingPlayers.Count == 1)
            {
                victory = remainingPlayers.First();
            }
        }

        return victory;
    }

}
