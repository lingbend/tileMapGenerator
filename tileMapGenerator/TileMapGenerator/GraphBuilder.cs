namespace TileMapGenerator;

using QuikGraph;
using System.Numerics;
using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<RoomVertex<System.Numerics.Vector2>, RoomEdge<System.Numerics.Vector2>>;
using Vertex = RoomVertex<System.Numerics.Vector2>;
using Edge = RoomEdge<System.Numerics.Vector2>;

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
        ValidateInputs(min_connections, max_connections, nodes_per_level, depth);
        // Debug.WriteLine($"first {min_connections}, {max_connections}, {nodes_per_level}");
        // (min_connections, max_connections, nodes_per_level) = CorrectGraphFeasibility(min_connections, max_connections, nodes_per_level); 
        // Debug.WriteLine($"next {min_connections}, {max_connections}, {nodes_per_level}");
        // CheckGraphFeasibility(min_connections, max_connections, nodes_per_level, depth);
        _grid = new BinaryGrid((uint) nodes_per_level[0]*4, (uint) nodes_per_level[0]*4);
        
        Graph nested_graph;
        if (central)
        {
            _graph = GenerateCentralRegion(nodes_per_level, new Vector2(nodes_per_level[0], nodes_per_level[0]), depth);
        }
        else
        {
            _graph = GenerateDecentralizedRegion(nodes_per_level, new Vector2(nodes_per_level[0], nodes_per_level[0]), depth);
        }
        UnnestGraph();
        return _graph;

    }


    private (int, int, List<int>) CorrectGraphFeasibility(int min, int max, List<int> zone_node_counts)
    {
        if (min == max && max % 2 == 1)
        {
            if (min < 4)
            {
                min++;
                max++;
            }
            else
            {
                min--;
                max--;
            }
        }
        if (min > 5)
        {
            max -= min - 5;
            min -= min - 5;
        }
        
        
        for (int i = 0; i < zone_node_counts.Count; i++)
        {
            if ((zone_node_counts[i] - 1) < min)
            {
                zone_node_counts[i] = min + 1;
            }
            if (min >( 6 - (12 / zone_node_counts[i])) && zone_node_counts[i] >= 3)
            {
                zone_node_counts[i] = (int) Math.Ceiling((double) (12.0/(6.0-min)));
            }
        }
        return (min, max, zone_node_counts);
    }

    private void CheckGraphFeasibility(int min, int max, List<int> zone_node_counts, int depth)
    {
        if (min == max && (max % 2) == 1 && zone_node_counts.Any((x) => (x % 2) == 1))
        {
            throw new ArgumentException("Impossible graph");
        }
        else if (min > 5)
        {
            throw new ArgumentException("Nonplanar graph");
        }
        foreach (int zone_num in zone_node_counts)
        {
            if ((zone_num - 1) < min || (min > (6 - (12 / zone_num)) && zone_num >= 3))
            {
                throw new ArgumentException("Likely non-planar or impossible graph");
            }
        }
    }

    private static void ValidateInputs(int min, int max, List<int> zone_node_counts, int depth)
    {
        if (depth <= 0 || zone_node_counts.Count < depth || min < 0 || max <= 0 ||
                 min > max || zone_node_counts.Any((x) => x <= 0))
        {
            throw new ArgumentException($"Bad Arguments: depth: {depth},"+
             $"room counts: {string.Concat(zone_node_counts)}, min degree: {min}, max degree: {max}");
        }
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
            layer_min = Vector2.Min(layer_min, node.Weight);
            layer_max = Vector2.Max(layer_max, node.Weight);

            if (node.InnerGraph != null)
            {
                var temp_vector = GetMaxRelativeNestedGraphSize((Graph) node.InnerGraph!);
                max_new_vector = Vector2.Max(max_new_vector, temp_vector);                
            }
        }
        Vector2 layer_size = layer_max - layer_min + Vector2.One;
        return layer_size * max_new_vector;
    }

    private Graph RemapNodes(Graph nested_graph)
    {
        List<(Vertex, Graph)> graphs = new List<(Vertex, Graph)>();
        var vertices = nested_graph.Vertices;
        if (vertices.First().InnerGraph != null)
        {
            foreach (var vertex in vertices)
            {
                graphs.Add((vertex, RemapNodes((Graph)vertex.InnerGraph!)));
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
            layer_min = Vector2.Min(layer_min, node_weight);
            layer_max = Vector2.Max(layer_max, node_weight);
        }
        Vector2 layer_size = layer_max - layer_min + Vector2.One;
        Graph new_graph = new Graph();
        List<(Vertex, Vertex)> heads = UnpackGraphWeights(graphs, layer_size, new_graph);
        AttachHeadVerticesTogether(layer_size, new_graph, heads);
        return new_graph;
    }

    private List<(Vertex, Vertex)> UnpackGraphWeights(List<(Vertex, Graph)> graphs, Vector2 layer_size, Graph new_graph)
    {
        List<(Vertex, Vertex)> heads = new List<(Vertex, Vertex)>();
        foreach (var (vertex, graph) in graphs)
        {
            foreach (var node in graph.Vertices)
            {
                if (node.Center)
                {
                    node.Weight = node.Weight * layer_size + GetOffsetWeight(vertex.Weight, layer_size);
                    heads.Add((vertex, node));
                }
                else
                {
                    node.Weight = UnpackWeightVector(node.Weight, layer_size) + GetOffsetWeight(vertex.Weight, layer_size);
                }

                new_graph.AddVertex(node);
                
                foreach (var edge in node.Edges)
                {
                    edge.Weight = UnpackWeightVector(edge.Weight, layer_size);
                    new_graph.AddEdge(edge);
                }
            }
        }

        return heads;
    }

    private void AttachHeadVerticesTogether(Vector2 layer_size, Graph new_graph, List<(Vertex, Vertex)> heads)
    {
        foreach (var (vertex, head) in heads)
        {
            foreach (var edge in vertex.Edges)
            {
                if (!new_graph.Edges.Contains(edge))
                {
                    edge.Weight = UnpackWeightVector(edge.Weight, layer_size);
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
    }

    private Vector2 UnpackWeightVector(Vector2 weight, Vector2 layer_size)
    {
        return Vector2.Normalize(weight) * layer_size;
    }

    private Vector2 GetOffsetWeight(Vector2 center, Vector2 layer_size)
    {
        return center * layer_size;
    }

    private Graph GenerateCentralRegion(List<int> sizes, Vector2 center, int current_depth=1)
    {
        // throw new NotImplementedException();
        return GenerateDecentralizedRegion(sizes, center, current_depth);
    }

    // private Graph GenerateDecentralizedRegion(List<int> sizes)
    // {
    //     return GenerateDecentralizedRegion(sizes, Vector2.Zero);
    // }

    private Graph GenerateDecentralizedRegion(List<int> sizes, Vector2 center, int current_depth=1)
    {
        var subgrid = new BinaryGrid((uint) sizes[0]*2, (uint) sizes[0]*2);
        var subgraph = new Graph();
        Vertex current_node = new Vertex(center);
        current_node.Center = true;
        subgraph.AddVertex(current_node);
        List<Vector2> directions = new List<Vector2>();
        Vector2 direction;
        while (subgraph.VertexCount != sizes[0])
        {
            if (directions.Count == 0)
            {
                (directions, current_node) = GetNextLeaf(subgraph);
            }
            direction = ChooseRandom(directions);

            (Vertex vert, _) = AddNode(current_node, direction, subgraph, subgrid);
            if (current_depth != 1)
            {
                vert.InnerGraph = GenerateDecentralizedRegion(sizes[1..], Vector2.Zero, current_depth - 1);
            }
            
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

    private (List<Vector2>, Vertex) GetNextLeaf(Graph graph)
    {
        List<Vector2> new_directions = new List<Vector2>();
        List<Vertex> vertices = new List<Vertex>(graph.Vertices);
        Vertex new_leaf;
        foreach (var vertex in vertices)
        {
            new_leaf = vertex;
            new_directions =  new List<Vector2> (GetAvailableDirections(new_leaf));
            if (new_directions.Count != 0)
            {
                return (new_directions, new_leaf);
            }
        }
        throw new Exception("Could not locate next leaf");
    }

    private (Vertex, Edge) AddNode(Vertex source_vertex, Vector2 direction_weight, Graph graph, BinaryGrid grid)
    {
        Vertex new_vertex = new Vertex(source_vertex.Weight + direction_weight);
        Edge new_edge = source_vertex.ConnectToVertex(new_vertex);
        new_edge.Weight = direction_weight;
        grid.SetCell((uint) new_vertex.Weight.Y, (uint) new_vertex.Weight.X, 1U);
        graph.AddVertex(new_vertex);
        graph.AddEdge(new_edge); 
        return (new_vertex, new_edge);
    }

    private ISet<Vector2> GetAvailableDirections(Vertex node)
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
            Vector2 new_direction = direction + relative_loc;
            if (new_direction.X > 0 && new_direction.Y > 0 &&
             _grid.GetCell((uint) new_direction.Y, (uint) new_direction.X) == 1)
            {
                used_directions.Add(direction);
            }
            else if (new_direction.X <= 0 || new_direction.Y <= 0)
            {
                used_directions.Add(direction);
            }
        }
        available_directions.ExceptWith(used_directions);

        return available_directions;
    }
}