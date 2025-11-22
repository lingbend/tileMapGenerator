namespace TileMapGenerator;

static internal class UIDGenerator
{
    private static int _id = 0;

    public static int GetNextID()
    {
        return _id++;
    }
}