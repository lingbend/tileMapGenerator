namespace TileMapGenerator;
using ConcurrentRandom;

static internal class UIDGenerator
{
    // private static int _id = 0;
    private static ConcurrentRandom _random = new(4593264);

    public static int GetNextID(object unique_identifier)
    {
        return _random.Next(unique_identifier);
        // return Interlocked.Increment(ref _id);
    }

    // public static int GetNextConcurrentID()
    // {
    //     return _random.Next(_id);
    // }
}