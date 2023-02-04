
public enum RootTileType
{
    TestTile,
    TestTileYellow,
    GrassTile,
    
    MAX
}

public struct RootTileData
{
    // TODO all the data needed for a tile to be represented in gamestate
    
    /// <summary>id of the owner player or -1 for no ownership</summary>
    public sbyte PlayerId;

    public RootTileType TileType;
}
