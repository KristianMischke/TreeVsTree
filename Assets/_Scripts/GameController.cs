using System;
using System.Collections;
using System.Collections.Generic;
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

    private int _tilesForVictory;
    private int _turnNumber = 0;

    private sbyte _numPlayers = 2;
    private sbyte _playerTurn = 0;

    private Player[] _players;
    
    public int MapWidth = 40;
    public int MapHeight = 20;
    public Vector2Int[] PlayerStartPositions; 
    
    // Scene references
    public MapController MapController;

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
            MapController.SetMap(_tiles, new []{ new Vector2Int(_random.Next(MapWidth), _random.Next(MapHeight)), new Vector2Int(3, 4)});
        }
    }

    private void OnTileClicked(Vector3Int position)
    {
        _tiles[position.x, position.y].PlayerId = _playerTurn;
        _tiles[position.x, position.y].AboveType = AboveTileType.TreeRoots;
        _areTilesDirty = true;
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

    private int CheckVictory(){
        int victory = -1;
        
        foreach(Player player in _players){
            if(player.TilesControled >= _tilesForVictory){
                victory = player.Id;
            }
        }
        // TODO else case for taking base tree and way to check if base tree taken

        return victory;
    }

}
