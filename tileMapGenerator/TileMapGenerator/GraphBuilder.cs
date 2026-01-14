namespace TileMapGenerator;

using QuikGraph;
using System.Numerics;
using BinaryGrid;
using RoomAndEdges;
using Graph = QuikGraph.UndirectedGraph<RoomAndEdges.RoomVertex<System.Numerics.Vector2>, RoomAndEdges.RoomEdge<System.Numerics.Vector2>>;
using Vertex = RoomAndEdges.RoomVertex<System.Numerics.Vector2>;
using Edge = RoomAndEdges.RoomEdge<System.Numerics.Vector2>;

public class TileMapGenerator
{
    

    private void GenerateMap()
    {
        // generate weighted node tree to multiple depths making sure to have external connection points for at least higher level trees
        // check tree phase postprocessors
        // grow rooms cellularly
        //     choose room loci
        //     grows rooms iteratively
        //     connect room loci and hall loci based on node tree
        //     create doors
        // check cellular phase postprocessors
        // generate room layouts
        // check room layout postprocessors
        // pad halls with walls
        // fill in map with other things

    }

    
}