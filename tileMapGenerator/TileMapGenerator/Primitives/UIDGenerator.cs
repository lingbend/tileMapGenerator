namespace TileMapGenerator
{
    using CRandom;

    static internal class UIDGenerator
    {
        // private static int _id = 0;
        private static CRandom _random = new CRandom(4593264);

        public static int GetNextID(object unique_identifier)
        {
            return _random.Next(unique_identifier);
            // return Interlocked.Increment(ref _id);
        }

        // TODO: Implement this before presentation
        // public static int GetNextConcurrentID()
        // {
        //     return _random.Next(_id);
        // }
    }

    internal interface ID
    {
        int ID{get; set;}
    }
}