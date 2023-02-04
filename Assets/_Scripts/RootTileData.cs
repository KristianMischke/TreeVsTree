
public enum GroundTileType
{
    TestTile,           // TODO: remove 
    TestTileYellow,     // TODO: remove
    
    GrassTile,
    WaterTile,
    MountainTile,
    
    MAX
}

public enum AboveTileType
{
    Tree,
    TreeRoots,
    
    MAX
}

public struct RootTileData
{
    // TODO all the data needed for a tile to be represented in gamestate
    
    /// <summary>id of the owner player or -1 for no ownership</summary>
    public sbyte PlayerId;

    public GroundTileType GroundType;
    public AboveTileType AboveType;
}
