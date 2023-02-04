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
        public int Id;
        public int TilesControled;

        /// <summary>Number of moves that a player can make in a single turn</summary>
        public int NumMoves;
    }

    private int _tilesForVictory = 20;
    private int _turnNumber = 0;

    private sbyte _numPlayers = 2;
    private sbyte _playerTurn = 0;

    private Player[] _players;

    public int MapWidth = 40;
    public int MapHeight = 20;
    public Vector2Int[] PlayerStartPositions;

    // Scene references
    public MapController MapController;

    public sbyte PlayerWon;

    // Start is called before the first frame update
    void Start()
    {
        MapController.OnHexCellClicked += OnTileClicked;

        _random = new Random();
        InitMap();
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
            HashSet<Vector2Int> playerTiles = giveValidTiles(_playerTurn);
            //List<Vector2Int> playerTilesList = playerTiles.ToList();
            MapController.SetMap(_tiles, playerTiles);
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
        else{
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
    
    private void InitMap()
    {
        _tiles = new RootTileData[MapWidth, MapHeight];
        for (int row = 0; row < _tiles.GetLength(0); row++)
        {
            for (int col = 0; col < _tiles.GetLength(1); col++)
            {
                _tiles[row, col] = new RootTileData
                {
                    PlayerId = -1,
                    GroundType = (GroundTileType)_random.Next((int)GroundTileType.MAX)
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

    private void InitializePlayers(){
        _players = new Player[_numPlayers];

        for(int i = 0; i < _numPlayers; i++){
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

    private int CheckVictory(){//TODO test to make sure this works
        int victory = -1;
        
        foreach(Player player in _players){
            if(player.TilesControled >= _tilesForVictory){
                victory = player.Id;
            }
        }
        //else case for taking base tree and way to check if base tree taken
        if(victory == -1){
            foreach (var position in PlayerStartPositions)
            {
                if(_tiles[position.x, position.y].PlayerId == -1){
                    victory = _playerTurn;//operates under the assumtion that the attacking player immediately wins the game upon capturing enemy tree
                }
            }
        }

        return victory;
    }

}
