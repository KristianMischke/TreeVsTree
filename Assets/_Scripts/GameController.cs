using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

public class GameController : MonoBehaviour
{
    private Random _random;
    private RootTileData[,] _tiles;
    private bool _zeroIsOddColumn = false;
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
    private int _movesThisTurn;

    private sbyte _numPlayers = 2;
    private sbyte _playerTurn = 0;

    private Player[] _players;

    public int MapWidth => _tiles.GetLength(0);
    public int MapHeight => _tiles.GetLength(1);
    public RectInt AllTilesBounds =>
        new RectInt(Vector2Int.zero, new Vector2Int(MapWidth, MapHeight));
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
        MapController.GetGameStateFromTilemap(out _tiles, out _zeroIsOddColumn);
        InitializePlayers();

        _movesThisTurn = _players[_playerTurn].NumMoves;
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
            //Vector2Int playerTree = (Vector2Int)GetPlayerTreePosition(_playerTurn);
            //HashSet<Vector2Int> connectedTiles = findConnectedRoots(playerTree, _playerTurn);
            var victoryCheck = CheckVictory();
            if (victoryCheck != -1)
            {
                Debug.Log($"PLAYER {victoryCheck} WON");
            }
            
            _areTilesDirty = false;
            //KillRoots(connectedTiles, _playerTurn);
            _playerTiles = giveValidTiles(_playerTurn);
            //List<Vector2Int> playerTilesList = playerTiles.ToList();
            //MapController.SetMap(_tiles, findConnectedRoots((Vector2Int)GetPlayerTreePosition(_playerTurn), _playerTurn)); // Used for testing, highlights connected roots
            MapController.SetMap(_tiles, _playerTiles);
            //MapController.SetMap(_tiles, new[] { new Vector2Int(_random.Next(MapWidth), _random.Next(MapHeight)), new Vector2Int(3, 4) });
        }
    }

    private void OnTileClicked(Vector2Int position)
    {
        _areTilesDirty = AttackTile(position);
        
        PlayerWon = (sbyte) CheckVictory();

        if(_areTilesDirty){
            _movesThisTurn--;

            if(_movesThisTurn <= 0){
                NextTurn();
            }
        }
    }

    private bool AttackTile(Vector2Int position){
        bool validMove = true;
        
        Vector2Int positionxy = new Vector2Int(position.x, position.y);

        if(_playerTiles.Contains(positionxy))
        {
            //if empty tile, fill in with this player's roots
            if(_tiles[position.x, position.y].AboveType != AboveTileType.TreeRootsDead && _tiles[position.x, position.y].PlayerId == -1)
            {        
                AddResource(_tiles[position.x, position.y]);
                _tiles[position.x, position.y].PlayerId = _playerTurn;
                _tiles[position.x, position.y].AboveType = AboveTileType.TreeRoots;
            }
            //else, if another player's roots, remove them
            else if(_tiles[position.x, position.y].PlayerId != _playerTurn)
            {
                RemoveResource(_tiles[position.x, position.y]);
                _tiles[position.x, position.y].PlayerId = -1;
                _tiles[position.x, position.y].AboveType = AboveTileType.MAX;
            }
            else if(_tiles[position.x, position.y].AboveType == AboveTileType.TreeRootsDead)
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

    private void AddResource(RootTileData tile){
        if(tile.GroundType == GroundTileType.WaterTile || tile.GroundType == GroundTileType.RichSoilTile){
            _players[_playerTurn].NumMoves++;
        }
    }

    private void RemoveResource(RootTileData tile){
        if(tile.GroundType == GroundTileType.WaterTile || tile.GroundType == GroundTileType.RichSoilTile){
            if(tile.AboveType != AboveTileType.TreeRootsDead){
                _players[tile.PlayerId].NumMoves--;
            }
        }
    }

    private HashSet<Vector2Int> giveValidTiles(int playerID)
    {
        HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
        for(int i = 0; i < MapWidth; i++) // Adds valid tiles
        {
            for(int j = 0; j < MapHeight; j++)
            {
                if(_tiles[i,j].PlayerId == _playerTurn)
                {
                    var polarity = _zeroIsOddColumn ? 1 : 0;
                    if(i%2 == polarity) // Not an offset column
                    {
                        tryAdd(validTiles, i - 1, j - 1); // Lower left tile
                        tryAdd(validTiles, i - 1, j); // Upper left tile
                        tryAdd(validTiles, i, j - 1); // Tile below
                        tryAdd(validTiles, i, j + 1); // Tile above
                        tryAdd(validTiles, i + 1, j - 1); // Lower right tile
                        tryAdd(validTiles, i + 1, j); // Upper right tile
                        /*validTiles.Add(new Vector2Int(i - 1, j - 1)); // Lower left tile
                        validTiles.Add(new Vector2Int(i - 1, j)); // Upper left tile
                        validTiles.Add(new Vector2Int(i, j - 1)); // Tile below
                        validTiles.Add(new Vector2Int(i, j + 1)); // Tile above
                        validTiles.Add(new Vector2Int(i + 1, j - 1)); // Lower right tile
                        validTiles.Add(new Vector2Int(i + 1, j)); // Upper right tile*/
                    }
                    else // Offset column
                    {
                        tryAdd(validTiles, i - 1, j); // Lower left tile
                        tryAdd(validTiles, i - 1, j + 1); // Upper left tile
                        tryAdd(validTiles, i, j - 1); // Tile below
                        tryAdd(validTiles, i, j + 1); // Tile above
                        tryAdd(validTiles, i + 1, j); // Lower right tile
                        tryAdd(validTiles, i + 1, j + 1); // Upper right tile
                        /*validTiles.Add(new Vector2Int(i - 1, j)); // Lower left tile
                        validTiles.Add(new Vector2Int(i - 1, j + 1)); // Upper left tile
                        validTiles.Add(new Vector2Int(i, j - 1)); // Tile below
                        validTiles.Add(new Vector2Int(i, j + 1)); // Tile above
                        validTiles.Add(new Vector2Int(i + 1, j)); // Lower right tile
                        validTiles.Add(new Vector2Int(i + 1, j + 1)); // Upper right tile*/
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
    private void tryAdd(HashSet<Vector2Int> setToAdd, int x, int y) // Adds position to a list while checking if it's in bounds
    {
        if (AllTilesBounds.Contains(new Vector2Int(x, y))){
            setToAdd.Add(new Vector2Int(x, y));
        }
    }

    //Recursive function that catalogues all roots connected to a player's tree. Initial position value should be position of the players tree
    private HashSet<Vector2Int> findConnectedRoots(Vector2Int position, int playerID, HashSet<Vector2Int> connectedTiles = null)
    {
        if(connectedTiles == null)
        {
            connectedTiles = new HashSet<Vector2Int>();
        }
        if (!connectedTiles.Contains(position))
        {
            connectedTiles.Add(position);
            var polarity = _zeroIsOddColumn ? 1 : 0;
            if (position.x%2 == polarity) // Not an offset column
            {
                if(AllTilesBounds.Contains(new Vector2Int(position.x - 1, position.y - 1)) && _tiles[position.x - 1, position.y - 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x - 1, position.y - 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x - 1, position.y)) && _tiles[position.x - 1, position.y].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x - 1, position.y), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x, position.y - 1)) && _tiles[position.x, position.y - 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x, position.y - 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x, position.y + 1)) && _tiles[position.x, position.y + 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x, position.y + 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x + 1, position.y - 1)) && _tiles[position.x + 1, position.y - 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x + 1, position.y - 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x + 1, position.y)) && _tiles[position.x + 1, position.y].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x + 1, position.y), playerID, connectedTiles);
                }
            }
            else // offset column
            {
                if (AllTilesBounds.Contains(new Vector2Int(position.x - 1, position.y)) && _tiles[position.x - 1, position.y].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x - 1, position.y), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x - 1, position.y + 1)) && _tiles[position.x - 1, position.y + 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x - 1, position.y + 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x, position.y - 1)) && _tiles[position.x, position.y - 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x, position.y - 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x, position.y + 1)) && _tiles[position.x, position.y + 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x, position.y + 1), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x + 1, position.y)) && _tiles[position.x + 1, position.y].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x + 1, position.y), playerID, connectedTiles);
                }
                if (AllTilesBounds.Contains(new Vector2Int(position.x + 1, position.y + 1)) && _tiles[position.x + 1, position.y + 1].PlayerId == playerID)
                {
                    connectedTiles = findConnectedRoots(new Vector2Int(position.x + 1, position.y + 1), playerID, connectedTiles);
                }
            }
        }
        return connectedTiles;
    }

    

    // Turns unconnected roots into dead roots
    private void KillRoots(HashSet<Vector2Int> connectedRoots, int playerID)
    {
        Debug.Log(playerID);
        //HashSet<Vector2Int> tilesToKill = new HashSet<Vector2Int>();
        for (int i = 0; i < MapWidth; i++) // Adds valid tiles
        {
            for (int j = 0; j < MapHeight; j++)
            {
                if (_tiles[i, j].PlayerId == _playerTurn && !(connectedRoots.Contains(new Vector2Int(i, j))))
                {
                    RemoveResource(_tiles[i, j]);
                    _tiles[i, j].AboveType = AboveTileType.TreeRootsDead;
                    _tiles[i, j].PlayerId = -1;
                    //tilesToKill.Add(new Vector2Int(i, j));
                }
            }
        }
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
        
        Vector2Int playerTree = (Vector2Int)GetPlayerTreePosition(_playerTurn);
        HashSet<Vector2Int> connectedTiles = findConnectedRoots(playerTree, _playerTurn);
        KillRoots(connectedTiles, _playerTurn);

        _movesThisTurn = _players[_playerTurn].NumMoves;
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
