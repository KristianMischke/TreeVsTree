using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class GameLogic
{
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
        public sbyte NumPlayers;
        public int TilesForVictory;
        public bool FogOfWarEnabled;
        
        public int FirstPlayerFirstTurnCount;
        public int PlayerDefaultTurnCount;
    }

    public static GameParameters DefaultParameters = new GameParameters()
    {
        NumPlayers = 2,
        TilesForVictory = 20,
        FogOfWarEnabled = true,

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

    private void AddResource(sbyte playerId, RootTileData tile)
    {
        if (IsResource(tile))
        {
            _players[playerId].NumMoves++;
        }
    }

    private void RemoveResource(RootTileData tile)
    {
        if (IsResource(tile) && tile.PlayerId >= 0)
        {
            _players[tile.PlayerId].NumMoves--;
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
                || _tiles[pos.x, pos.y].GroundType == GroundTileType.None)
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