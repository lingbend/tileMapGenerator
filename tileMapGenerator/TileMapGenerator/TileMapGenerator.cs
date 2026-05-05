namespace TileMapGenerator
{
    using QuikGraph;
    using System.Numerics;
    using BinaryGrid;
    using MapPrimitives;
    using Graph = QuikGraph.UndirectedGraph<MapPrimitives.RoomVertex<System.Numerics.Vector2>, MapPrimitives.RoomEdge<System.Numerics.Vector2>>;
    using Vertex = MapPrimitives.RoomVertex<System.Numerics.Vector2>;
    using Edge = MapPrimitives.RoomEdge<System.Numerics.Vector2>;
    using DoorChoser;
    using HallMaker;
    using CellularRoomGrower;
    using NodeTreeGenerator;
    using Vector2Extensions;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class TileMapGenerator
    {
        public static NodeTreeGeneratorSettings NodeTreeGeneratorSettings{get; set;} = new NodeTreeGeneratorSettings();
        public static CellularRoomGrowerSettings CellularRoomGrowerSettings{get; set;} = new CellularRoomGrowerSettings();
        public static int RoomNumber{get; set;} = 15;
        public static int MapArea{get; set;} = 30*30;
    
        public static (BinaryGrid, IEnumerable<Vector2>) GenerateMap(int seed = -1)
        {
            if (seed != -1)
            {
                NodeTreeGeneratorSettings.Random = new Random(seed);   
            }   
            var nodeTree = GenerateNodeTree();
            var (graph, grid, rooms, halls) = GenerateSizedRooms(nodeTree, NodeTreeGeneratorSettings.Random);
            (graph, grid, rooms, halls) = GenerateHalls(graph, grid, rooms, halls);
            var (new_grid,  doors) = GenerateDoors(grid, rooms, halls);
            return (new_grid, doors);
        }

        private static Graph GenerateNodeTree()
        {
            var generator = new NodeTreeGenerator();
            generator.Settings = NodeTreeGeneratorSettings;
            Graph output = generator.GenerateNodeTree(RoomNumber);
            return output;
        }

        private static (Graph, BinaryGrid, IEnumerable<Room>, IEnumerable<Hall>) GenerateSizedRooms(Graph nodeTree, Random rand)
        {
            var room_grower = new CellularRoomGrower();
            room_grower.Settings = CellularRoomGrowerSettings;
            CellularRoomGrowerSettings.Random = rand;
            return room_grower.GenerateSizedRooms(nodeTree, MapArea);
        }

        private static (Graph, BinaryGrid, IEnumerable<Room>, IEnumerable<Hall>) GenerateHalls(Graph roomGraph, BinaryGrid roomGrid, IEnumerable<Room> rooms, IEnumerable<Hall> halls)
        {
            return new HallMaker().GenerateHalls(roomGraph, roomGrid, rooms, halls);
        }

        private static (BinaryGrid, IEnumerable<Vector2>) GenerateDoors(BinaryGrid roomGrid, IEnumerable<Room> rooms, IEnumerable<Hall> halls)
        {
            var doors = DoorChoser.ChooseDoors(rooms, halls);
            var patches = DoorChoser.PatchDoorframes(doors, roomGrid);
            foreach (var point in patches)
            {
                roomGrid.SetCell(point.Reverse(), 1);
            }
            return (roomGrid, doors);
        }
    
    }
}