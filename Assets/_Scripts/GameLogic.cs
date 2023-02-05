using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameLogic
{
    System.Random rnd = new System.Random();
    public struct Player
    {
        public sbyte Id;
        public int TilesControlled;

        /// <summary>Number of moves that a player can make in a single turn</summary>
        public int NumMoves;
    }

    public enum PlayerActions
    {
        GrowRoot
    }

    [Serializable]
    public struct GameParameters
    {
        public string MapName;
        
        public byte NumPlayers;
        public int TilesForVictory;
        public bool FogOfWarEnabled;
        public bool RabbitEnabled;
        
        public int FirstPlayerFirstTurnCount;
        public int PlayerDefaultTurnCount;
    }

    public static GameParameters DefaultParameters = new GameParameters()
    {
        MapName = "Map1",
        
        NumPlayers = 2,
        TilesForVictory = 20,
        FogOfWarEnabled = false,
        RabbitEnabled = true,

        PlayerDefaultTurnCount = 2,
        FirstPlayerFirstTurnCount = 1
    };

    private readonly GameParameters _parameters;
    private readonly RootTileData[,] _tiles;
    private Player[] _players;
    private readonly bool _zeroIsOddColumn;
    private int _turnNumber = 0;
    private int _remainingMovesThisTurn;
    private sbyte _currentPlayer;
    private sbyte _victoryPlayer;

    public bool GameOver => _victoryPlayer >= 0;
    public sbyte Winner => _victoryPlayer;
    public sbyte CurrentTurn => _currentPlayer;

    public bool _rabbitEnraged = false;

    public int RemainingMovesThisTurn => _remainingMovesThisTurn;

    public int MapWidth => _tiles.GetLength(0);
    public int MapHeight => _tiles.GetLength(1);

    public RectInt AllTilesBounds =>
        new RectInt(Vector2Int.zero, new Vector2Int(MapWidth, MapHeight));

    public RectInt.PositionEnumerator AllTilesPositionIter => AllTilesBounds.allPositionsWithin;
    public RootTileData[,] Tiles => _tiles;

    public GameLogic(GameParameters parameters, RootTileData[,] map, bool zeroIsOddColumn)
    {
        _parameters = parameters;
        _tiles = map;
        _zeroIsOddColumn = zeroIsOddColumn;

        InitializePlayers();
        _victoryPlayer = -1;
        _currentPlayer = 0;
        _remainingMovesThisTurn = _parameters.FirstPlayerFirstTurnCount;
        if(_parameters.RabbitEnabled == false)
        {
            RemoveRabbitAndCarrot(_tiles);
        }
    }

    private void RemoveRabbitAndCarrot(RootTileData[,] map)
    {
        for(int i = 0; i < MapWidth; i++)
        {
            for(int j = 0; j < MapHeight; j++)
            {
                if(map[i,j].GroundType == GroundTileType.CarrotTile)
                {
                    Debug.Log(i + " " + j);
                    map[i, j].GroundType = GroundTileType.GrassTile;
                }
                if(map[i,j].AboveType == AboveTileType.Rabbit)
                {
                    Debug.Log(i + " " + j);
                    map[i, j].AboveType = AboveTileType.None;
                }
            }
        }
    }

    public Vector2Int FindRabbit(RootTileData[,] map)
    {
        //Debug.Log("Runs findRabbit");
        for (int i = 0; i < MapWidth; i++)
        {
            for (int j = 0; j < MapHeight; j++)
            {
                
                if (map[i,j].AboveType == AboveTileType.Rabbit)
                {
                    //Debug.Log("returns a rabbit");
                    return new Vector2Int(i, j);
                }
            }
        }
        return new Vector2Int(0,0); // Should never return
    }

    public Vector2Int FindCarrot(RootTileData[,] map)
    {
        //Debug.Log("Runs findRabbit");
        for (int i = 0; i < MapWidth; i++)
        {
            for (int j = 0; j < MapHeight; j++)
            {

                if (map[i, j].GroundType == GroundTileType.CarrotTile)
                {
                    //Debug.Log("returns a rabbit");
                    return new Vector2Int(i, j);
                }
            }
        }
        return new Vector2Int(0, 0); // Should never return
    }

    public bool DoAction(PlayerActions playerActions, Vector2Int position)
    {
        bool valid = true;
        switch (playerActions)
        {
            case PlayerActions.GrowRoot:
                valid = GrowRoot(_currentPlayer, position);
                break;
        }

        if (!valid)
            return false;

        _victoryPlayer = CheckVictory();

        _remainingMovesThisTurn--;
        if (_remainingMovesThisTurn <= 0)
        {
            NextTurn();
        }

        return valid;
    }


    private void NextTurn()
    {
        _turnNumber++;
        _currentPlayer++;
        if (_currentPlayer >= _parameters.NumPlayers)
        {
            _currentPlayer = 0;
        }
        if (_parameters.RabbitEnabled == true) 
        {
            Vector2Int RabbitPos = FindRabbit(_tiles);
            moveRabbit(RabbitPos);
        }
        Vector2Int playerTree = GetPlayerTreePosition(_currentPlayer)!.Value;
        HashSet<Vector2Int> connectedTiles = GetConnectedRoots(playerTree, _currentPlayer);
        KillRoots(connectedTiles, _currentPlayer);

        _remainingMovesThisTurn = _players[_currentPlayer].NumMoves;
    }

    private bool GrowRoot(sbyte playerId, Vector2Int position)
    {
        var validTiles = GetValidRootGrowthTiles(_currentPlayer);
        if (!validTiles.Contains(position))
            return false;

        //if empty tile, fill in with this player's roots
        if (_tiles[position.x, position.y].AboveType != AboveTileType.TreeRootsDead
            && _tiles[position.x, position.y].PlayerId == -1)
        {
            AddResource(playerId, _tiles[position.x, position.y]);
            _tiles[position.x, position.y].PlayerId = playerId;
            _tiles[position.x, position.y].AboveType = AboveTileType.TreeRoots;
        }
        //else, if another player's roots, remove them
        else if (_tiles[position.x, position.y].PlayerId != playerId)
        {
            RemoveResource(_tiles[position.x, position.y]);
            _tiles[position.x, position.y].PlayerId = -1;
            _tiles[position.x, position.y].AboveType = AboveTileType.MAX;
        }
        else
        {
            Debug.LogWarning("Does this even happen?");
        }

        return true;
    }

    private static bool IsResource(RootTileData tile)
    {
        return tile.GroundType == GroundTileType.WaterTile
               || tile.GroundType == GroundTileType.RichSoilTile;
    }

    private static bool IsCarrot(RootTileData tile)
    {
        return tile.GroundType == GroundTileType.CarrotTile;
    }

    private void AddResource(sbyte playerId, RootTileData tile)
    {
        if (IsResource(tile))
        {
            _players[playerId].NumMoves++;
        }
        else if(IsCarrot(tile))
        {
            _players[playerId].NumMoves++;
            _rabbitEnraged = true;
        }
    }

    private void RemoveResource(RootTileData tile)
    {
        if (IsResource(tile) && tile.PlayerId >= 0)
        {
            _players[tile.PlayerId].NumMoves--;
        }
        else if (IsCarrot(tile) && tile.PlayerId >= 0)
        {
            _players[tile.PlayerId].NumMoves--;
            _rabbitEnraged = false;
        }
    }

    private List<Vector2Int> GetAdjacentPositions(Vector2Int pos)
    {
        var result = new List<Vector2Int>();
        var polarity = _zeroIsOddColumn ? 1 : 0;
        if (pos.x % 2 == polarity) // Not an offset column
        {
            result.Add(pos + Vector2Int.down + Vector2Int.left); // Lower left tile
            result.Add(pos + Vector2Int.left); // Upper left tile
            result.Add(pos + Vector2Int.down); // Tile below
            result.Add(pos + Vector2Int.up); // Tile above
            result.Add(pos + Vector2Int.down + Vector2Int.right); // Lower right tile
            result.Add(pos + Vector2Int.right); // Upper right tile
        }
        else // Offset column
        {
            result.Add(pos + Vector2Int.left); // Lower left tile
            result.Add(pos + Vector2Int.left + Vector2Int.up); // Upper left tile
            result.Add(pos + Vector2Int.down); // Tile below
            result.Add(pos + Vector2Int.up); // Tile above
            result.Add(pos + Vector2Int.right); // Lower right tile
            result.Add(pos + Vector2Int.right + Vector2Int.up); // Upper right tile
        }

        return result;
    }

    public HashSet<Vector2Int> GetValidRootGrowthTiles(sbyte playerID)
    {
        HashSet<Vector2Int> validTiles = new HashSet<Vector2Int>();
        foreach (var pos in AllTilesPositionIter)
        {
            if (_tiles[pos.x, pos.y].PlayerId == playerID)
            {
                foreach (var adjacentPos in GetAdjacentPositions(pos))
                {
                    TryAdd(validTiles, adjacentPos);
                }
            }
        }

        // Loops to remove non-traversable tiles
        foreach (var pos in AllTilesPositionIter)
        {
            if (_tiles[pos.x, pos.y].PlayerId == playerID
                || _tiles[pos.x, pos.y].GroundType == GroundTileType.MountainTile
                || _tiles[pos.x, pos.y].GroundType == GroundTileType.None
                || _tiles[pos.x, pos.y].AboveType == AboveTileType.Rabbit)
            {
                validTiles.Remove(pos);
            }
        }

        return validTiles;
    }

    private void TryAdd(HashSet<Vector2Int> setToAdd, Vector2Int pos) // Adds position to a list while checking if it's in bounds
    {
        if (AllTilesBounds.Contains(pos))
        {
            setToAdd.Add(pos);
        }
    }

    //Recursive function that catalogues all roots connected to a player's tree. Initial position value should be position of the players tree
    private HashSet<Vector2Int> GetConnectedRoots(Vector2Int position, int playerId,
        HashSet<Vector2Int> connectedTiles = null)
    {
        if (connectedTiles == null)
        {
            connectedTiles = new HashSet<Vector2Int>();
        }

        if (connectedTiles.Contains(position))
            return connectedTiles;

        if (!AllTilesBounds.Contains(position))
            return connectedTiles;

        connectedTiles.Add(position);
        foreach (var adjacentPos in GetAdjacentPositions(position))
        {
            if (AllTilesBounds.Contains(adjacentPos)
                && _tiles[adjacentPos.x, adjacentPos.y].PlayerId == playerId)
            {
                GetConnectedRoots(adjacentPos, playerId, connectedTiles);
            }
        }

        return connectedTiles;
    }

    // Turns unconnected roots into dead roots
    private void KillRoots(HashSet<Vector2Int> connectedRoots, int playerId)
    {
        foreach (var pos in AllTilesPositionIter)
        {
            if (_tiles[pos.x, pos.y].PlayerId == playerId
                && !connectedRoots.Contains(pos))
            {
                RemoveResource(_tiles[pos.x, pos.y]);
                _tiles[pos.x, pos.y].AboveType = AboveTileType.TreeRootsDead;
                _tiles[pos.x, pos.y].PlayerId = -1;
            }
        }
    }

    public HashSet<Vector2Int> GetSeenTiles(sbyte playerId)
    {
        HashSet<Vector2Int> seenTiles = new HashSet<Vector2Int>();
        foreach (var pos in AllTilesPositionIter) // Adds valid tiles
        {
            if (_tiles[pos.x, pos.y].PlayerId == playerId)
            {
                TryAdd(seenTiles, pos);
                foreach (var adjacentPos in GetAdjacentPositions(pos))
                {
                    TryAdd(seenTiles, adjacentPos);
                    foreach (var twiceAdjacentPos in GetAdjacentPositions(adjacentPos))
                    {
                        TryAdd(seenTiles, twiceAdjacentPos);
                    }
                }
            }
        }

        return seenTiles;
    }

    public HashSet<Vector2Int> GetFogPositions(sbyte playerId)
    {
        if (!_parameters.FogOfWarEnabled)
            return new HashSet<Vector2Int>();
        
        var visibleTiles = GetSeenTiles(playerId);
        HashSet<Vector2Int> fogTiles = new HashSet<Vector2Int>();
        foreach (Vector2Int tile in AllTilesPositionIter)
        {
            if (!visibleTiles.Contains(new Vector2Int(tile.x, tile.y))
                && _tiles[tile.x, tile.y].AboveType != AboveTileType.Tree // don't include tree or none in fog
                && _tiles[tile.x, tile.y].GroundType != GroundTileType.None)
            {
                fogTiles.Add(new Vector2Int(tile.x, tile.y));
            }
        }

        return fogTiles;
    }

    private void moveRabbit(Vector2Int RabbitPos)
    {
        int numValidPos = 5;
        List<Vector2Int> ValidRabbitMoves = GetAdjacentPositions(RabbitPos);
        if (_rabbitEnraged == false)
        {
            for (int i = 0; i < ValidRabbitMoves.Count; i++)
            {
                if (!AllTilesBounds.Contains(new Vector2Int(ValidRabbitMoves[i].x, ValidRabbitMoves[i].y))
                    || _tiles[ValidRabbitMoves[i].x, ValidRabbitMoves[i].y].AboveType != AboveTileType.None
                    || _tiles[ValidRabbitMoves[i].x, ValidRabbitMoves[i].y].GroundType == GroundTileType.MountainTile
                    || _tiles[ValidRabbitMoves[i].x, ValidRabbitMoves[i].y].GroundType == GroundTileType.None)
                {
                    ValidRabbitMoves.RemoveAt(i);
                    numValidPos--;
                    i--;
                }
            }
            if (numValidPos >= 0) // Rabbit only attempts to move if it can
            {
                int directionToMove = rnd.Next(numValidPos); // 0 is lower left, goes clockwise from there
                _tiles[RabbitPos.x, RabbitPos.y].AboveType = AboveTileType.None;
                _tiles[ValidRabbitMoves[directionToMove].x, ValidRabbitMoves[directionToMove].y].AboveType = AboveTileType.Rabbit;
            }
        }
        else if (_rabbitEnraged == true)
        {
            foreach(Vector2Int pos in ValidRabbitMoves)
            {
                if(Vector2Int.Distance(pos, FindCarrot(_tiles)) < Vector2Int.Distance(RabbitPos, FindCarrot(_tiles)))
                {
                    _tiles[RabbitPos.x, RabbitPos.y].AboveType = AboveTileType.None;
                    if(_tiles[pos.x, pos.y].GroundType == GroundTileType.CarrotTile)
                    {
                        RemoveResource(_tiles[pos.x, pos.y]);
                    }
                    _tiles[pos.x, pos.y].AboveType = AboveTileType.Rabbit;
                    _tiles[pos.x, pos.y].PlayerId = -1;
                    break;
                }
            }
        }
        /*var polarity = _zeroIsOddColumn ? 1 : 0;
        if (RabbitPos.x % 2 == polarity) // Not an offset column
        {
            switch (directionToMove)
            {
                
                case 0: // lower left
                    _tiles[RabbitPos.x - 1, RabbitPos.y - 1].AboveType = AboveTileType.Rabbit;
                    break;
                case 1: // upper left
                    _tiles[RabbitPos.x - 1, RabbitPos.y].AboveType = AboveTileType.Rabbit;
                    break;
                case 2: // tile above
                    _tiles[RabbitPos.x, RabbitPos.y + 1].AboveType = AboveTileType.Rabbit;
                    break;
                case 3: // upper right
                    _tiles[RabbitPos.x + 1, RabbitPos.y].AboveType = AboveTileType.Rabbit;
                    break;
                case 4: // lower right
                    _tiles[RabbitPos.x + 1, RabbitPos.y - 1].AboveType = AboveTileType.Rabbit;
                    break;
                case 5: // tile below
                    _tiles[RabbitPos.x, RabbitPos.y - 1].AboveType = AboveTileType.Rabbit;
                    break;
                
            }
        }
        else // Offset column
        {
            switch (directionToMove)
            {
                case 0: // lower left
                    _tiles[RabbitPos.x - 1, RabbitPos.y].AboveType = AboveTileType.Rabbit;
                    break;
                case 1: // upper left
                    _tiles[RabbitPos.x - 1, RabbitPos.y + 1].AboveType = AboveTileType.Rabbit;
                    break;
                case 2: // tile above
                    _tiles[RabbitPos.x, RabbitPos.y + 1].AboveType = AboveTileType.Rabbit;
                    break;
                case 3: // upper right
                    _tiles[RabbitPos.x + 1, RabbitPos.y + 1].AboveType = AboveTileType.Rabbit;
                    break;
                case 4: // lower right
                    _tiles[RabbitPos.x + 1, RabbitPos.y].AboveType = AboveTileType.Rabbit;
                    break;
                case 5: // tile below
                    _tiles[RabbitPos.x, RabbitPos.y - 1].AboveType = AboveTileType.Rabbit;
                    break;
            }

        }*/
    }

    private void InitializePlayers()
    {
        _players = new Player[_parameters.NumPlayers];

        for (sbyte i = 0; i < _parameters.NumPlayers; i++)
        {
            _players[i] = new Player
            {
                Id = i,
                TilesControlled = 1,
                NumMoves = _parameters.PlayerDefaultTurnCount,
            };
        }
    }

    private Vector2Int? GetPlayerTreePosition(sbyte playerId)
    {
        foreach (var pos in AllTilesPositionIter)
        {
            if (_tiles[pos.x, pos.y].PlayerId == playerId && _tiles[pos.x, pos.y].AboveType == AboveTileType.Tree)
            {
                return pos;
            }
        }

        return null;
    }

    public void PlayerLeft(sbyte playerId)
    {
        var treePosition = GetPlayerTreePosition(playerId);
        if (treePosition.HasValue)
        {
            _tiles[treePosition.Value.x, treePosition.Value.y].PlayerId = -1;
            _tiles[treePosition.Value.x, treePosition.Value.y].AboveType = AboveTileType.None;
        }
    }

    private sbyte CheckVictory()
    {
        sbyte victory = -1;

        foreach (Player player in _players)
        {
            if (player.TilesControlled >= _parameters.TilesForVictory)
            {
                victory = player.Id;
            }
        }

        //else case for taking base tree and way to check if base tree taken
        if (victory == -1)
        {
            HashSet<sbyte> remainingPlayers = new HashSet<sbyte>();
            for (sbyte i = 0; i < _parameters.NumPlayers; i++)
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