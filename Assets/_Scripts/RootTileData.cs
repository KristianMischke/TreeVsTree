
public enum GroundTileType
{
    None,
    
    DesertTile,
    GrassTile,
    MesaTileDark,
    MesaTileLight,
    MountainTile,
    RichSoilTile,
    TestTile,           // TODO: remove 
    TestTileYellow,     // TODO: remove
    WaterTile,

    MAX
}

public enum AboveTileType
{
    None,
    Tree,
    TreeRoots,
    TreeRootsDead,
    
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
