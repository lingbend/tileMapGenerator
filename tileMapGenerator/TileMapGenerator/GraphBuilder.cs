namespace TileMapGenerator;

using QuikGraph;
using System.Numerics;
using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<RoomVertex<System.Numerics.Vector2>, RoomEdge<System.Numerics.Vector2>>;

public class TileMapGenerator
{
    public HashSet<Vector2> Valid_directions {get; private set;}= [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX,
     -Vector2.UnitY, new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1,-1)];
    // X = width, Y = height, Z = weight towards this size. Sizes will be on a scale between given points
    public List<Vector3> Room_Sizes_Weights{get; internal set;} = [new Vector3(10, 10, 5), new Vector3(15, 15, 3), new Vector3(20, 20, 1)];
    
    private Graph _graph = new Graph();
    private BinaryGrid _grid;

    internal Random _random{get; set;} = new Random();


    public TileMapGenerator(int size)
    {
        _grid = new BinaryGrid((uint) size, (uint) size);
    }

    public void SetValidDirections(HashSet<Vector2> new_directions)
    {
        foreach (var direction in new_directions)
        {
            if (direction.X + direction.Y != 1 && direction.X + direction.Y != 2 && direction.X != 1 && direction.Y != 1 && direction != Vector2.Zero)
            {
                throw new ArgumentException("Direction must not be zero vector. Must have components of 1 or 0.");
            }
        }
    }

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

    internal Graph GenerateNodeTree(int depth,
     List<int> nodes_per_level, int min_connections, int max_connections,bool central=false, bool connected=true)
    {
        if (depth <=0 || nodes_per_level.Count < depth || min_connections < 0 || max_connections < 0 || min_connections > max_connections)
        {
            throw new ArgumentException();
        }
        
        Graph nested_graph;
        if (central)
        {
            _graph = GenerateCentralRegion(nodes_per_level, Vector2.Zero, depth);
        }
        else
        {
            _graph = GenerateDecentralizedRegion(nodes_per_level, Vector2.Zero, depth);
        }
        UnnestGraph();
        return _graph;

    }

    private void UnnestGraph()
    {
        _graph= RemapNodes(_graph);
        Vector2 size = GetMaxRelativeNestedGraphSize(_graph);
        _grid = new BinaryGrid((uint) size.Y, (uint) size.X);
        foreach (var vertex in _graph.Vertices)
        {
            _grid.SetCell((uint) vertex.Weight.Y, (uint) vertex.Weight.X, 1U);
        }
    }

    private Vector2 GetMaxRelativeNestedGraphSize(Graph nested_graph)
    {
        Vector2 layer_min = Vector2.Zero;
        Vector2 layer_max = Vector2.Zero;
        Vector2 max_new_vector = Vector2.One;
        foreach (var node in nested_graph.Vertices)
        {
            if (node.Weight.X > layer_max.X)
            {
                layer_max.X = node.Weight.X;
            }
            else if (node.Weight.X < layer_min.X)
            {
                layer_min.X = node.Weight.X;
            }

            if (node.Weight.Y > layer_max.Y)
            {
                layer_max.Y = node.Weight.Y;
            }
            else if (node.Weight.Y < layer_min.Y)
            {
                layer_min.Y = node.Weight.Y;
            }
            if (node["inner_graph"] != null)
            {
                var temp_vector = GetMaxRelativeNestedGraphSize((Graph) node["inner_graph"]!);
                if (temp_vector.X > max_new_vector.X)
                {
                    max_new_vector.X = temp_vector.X;
                }
                if (temp_vector.Y > max_new_vector.Y)
                {
                    max_new_vector.Y = temp_vector.Y;
                }
                
            }
        }
        Vector2 layer_size = layer_max - layer_min + Vector2.One;
        return layer_size * max_new_vector;
    }

    private Graph RemapNodes(Graph nested_graph)
    {
        List<(RoomVertex<Vector2>, Graph)> graphs = new List<(RoomVertex<Vector2>, Graph)>();
        var vertices = nested_graph.Vertices;
        if (vertices.First()["inner_graph"] != null)
        {
            foreach(var vertex in vertices)
            {
                graphs.Add((vertex, RemapNodes((Graph) vertex["inner_graph"]!)));
            }
        }
        else
        {
            return nested_graph;
        }

        Vector2 layer_min = Vector2.Zero;
        Vector2 layer_max = Vector2.Zero;
        foreach (var graph in graphs)
        {
            var node_weight = GetMaxRelativeNestedGraphSize(graph.Item2);
            if (node_weight.X > layer_max.X)
            {
                layer_max.X = node_weight.X;
            }
            else if (node_weight.X < layer_min.X)
            {
                layer_min.X = node_weight.X;
            }

            if (node_weight.Y > layer_max.Y)
            {
                layer_max.Y = node_weight.Y;
            }
            else if (node_weight.Y < layer_min.Y)
            {
                layer_min.Y = node_weight.Y;
            }
        }
        Vector2 layer_size = layer_max - layer_min + Vector2.One;
        Graph new_graph = new Graph();
        List<(RoomVertex<Vector2>,RoomVertex<Vector2>)> heads = new List<(RoomVertex<Vector2>, RoomVertex<Vector2>)>();
        foreach (var (vertex, graph) in graphs)
        {
            foreach(var node in graph.Vertices)
            {
                if (node.Weight == Vector2.Zero)
                {
                   node.Weight = node.Weight * layer_size + vertex.Weight * layer_size;
                   new_graph.AddVertex(node);
                   heads.Add((vertex, node));
                }
                else
                {
                    node.Weight = Vector2.Normalize(node.Weight) * layer_size + vertex.Weight * layer_size;
                    new_graph.AddVertex(node);
                }
                foreach(var edge in node.Edges)
                {
                    edge.Weight = Vector2.Normalize(edge.Weight) * layer_size;
                    new_graph.AddEdge(edge);
                }
            }
        }
        foreach (var (vertex, head) in heads)
        {
            foreach (var edge in vertex.Edges)
            {
                if (!new_graph.Edges.Contains(edge))
                {
                    edge.Weight = Vector2.Normalize(edge.Weight) * layer_size;
                    new_graph.AddEdge(edge);
                }
                if (edge.Source == vertex) 
                {
                    edge.Source = head;
                }
                else 
                {
                    edge.Target = head;
                }
            }
        }
        return new_graph;
    }

    private Graph GenerateCentralRegion(List<int> sizes, Vector2 center, int current_depth=1)
    {
        throw new NotImplementedException();
    }

    // private Graph GenerateDecentralizedRegion(List<int> sizes)
    // {
    //     return GenerateDecentralizedRegion(sizes, Vector2.Zero);
    // }

    private Graph GenerateDecentralizedRegion(List<int> sizes, Vector2 center, int current_depth=1)
    {
        var subgraph = new Graph();
        RoomVertex<Vector2> current_node = new RoomVertex<Vector2>(center);
        subgraph.AddVertex(current_node);
        List<Vector2> directions = new List<Vector2>();
        Vector2 direction;
        while (subgraph.VertexCount != sizes[1])
        {
            if (directions.Count == 0)
            {
                (directions, current_node) = GetNextLeaf(subgraph);
            }
            direction = ChooseRandom(directions);

            (RoomVertex<Vector2> vert, _) = AddNode(current_node, direction, subgraph);
            vert["inner_graph"] = GenerateDecentralizedRegion(sizes[1..], Vector2.Zero, current_depth - 1);
        }
        return subgraph;
    }

    private T ChooseRandom<T>(IList<T> coll)
    {
        int rand = _random.Next(coll.Count - 1);
        T item = coll[rand];
        coll.RemoveAt(rand);
        return item;
    }

    private (List<Vector2>, RoomVertex<Vector2>) GetNextLeaf(Graph graph)
    {
        List<Vector2> new_directions = new List<Vector2>();
        List<RoomVertex<Vector2>> vertices = (List<RoomVertex<Vector2>>) graph.Vertices;
        RoomVertex<Vector2> new_leaf;
        foreach (var vertex in vertices)
        {
            new_leaf = vertex;
            new_directions = (List<Vector2>) GetAvailableDirections(new_leaf);
            if (new_directions.Count != 0)
            {
                return (new_directions, new_leaf);
            }
        }
        throw new Exception("Could not locate next leaf");
    }

    private (RoomVertex<Vector2>, RoomEdge<Vector2>) AddNode(RoomVertex<Vector2> source_vertex, Vector2 direction_weight, Graph graph)
    {
        RoomVertex<Vector2> new_vertex = new RoomVertex<Vector2>(source_vertex.Weight + direction_weight);
        RoomEdge<Vector2> new_edge = source_vertex.ConnectToVertex(new_vertex);
        _grid.SetCell((uint) new_vertex.Weight.Y, (uint) new_vertex.Weight.X, 1U);
        graph.AddVertex(new_vertex);
        graph.AddEdge(new_edge); 
        return (new_vertex, new_edge);
    }

    private ISet<Vector2> GetAvailableDirections(RoomVertex<Vector2> node)
    {
        HashSet<Vector2> used_directions = new HashSet<Vector2>();
        foreach (var edge in node.Edges)
        {
            if (edge.Source == node)
            {
                used_directions.Add(edge.Weight);
            }
            else
            {
                used_directions.Add(-edge.Weight);
            }
        }
        HashSet<Vector2> available_directions = new HashSet<Vector2>(Valid_directions);
        available_directions.ExceptWith(used_directions);
        used_directions.Clear();
        
        Vector2 relative_loc = node.Weight;
        foreach (var direction in available_directions)
        {
            if (_grid.GetCell((uint) (direction.Y + relative_loc.Y), (uint) (direction.X + relative_loc.X)) == 1)
            {
                used_directions.Add(direction);
            }
        }
        available_directions.ExceptWith(used_directions);

        return available_directions;
    }
}