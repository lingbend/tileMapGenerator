namespace TileMapGenerator;

using System.Data;
using System.Numerics;
using QuikGraph;


public class BasicGraphRules : IRoomGraphRules
{
    public BasicGraphRules()
    {
        _new_node_rules = new LinkedList<NodeRule>();
        _whole_graph_rules = new LinkedList<GraphRule>();
    }

    NodeRule.rule_delegate max_vertex_degree_4;
    GraphRule.rule_delegate number_of_rooms;
    GraphRule.rule_delegate all_rooms_connected_traversible;
    GraphRule.rule_delegate unique_room_ids;
    GraphRule.rule_delegate goldilocks_room_area;
    GraphRule.rule_delegate no_overlapping_rooms;
}

public abstract class IRoomGraphRules
{
    protected IEnumerable<NodeRule> _new_node_rules;
    protected IEnumerable<GraphRule> _whole_graph_rules;  

    public IEnumerable<NodeRule> GetNewNodeRules()
    {
        return _new_node_rules;
    }

    // TODO: update edge type here 
    public IEnumerable<GraphRule> GetWholeGraphRules()
    {
        return _whole_graph_rules;
    }

    public void CheckNewNode(Vector2 old_node, Vector2 new_node)
    {
        foreach (NodeRule rule in _new_node_rules)
        {
            rule.CheckRule((old_node, new_node));
        }
    }

    public void CheckWholeGraph(UndirectedBidirectionalGraph<int, UndirectedEdge<int>> graph)
    {
        foreach (GraphRule rule in _whole_graph_rules)
        {
            rule.CheckRule(graph);
        }
    }
}

public class NodeRule : IRule<(Vector2, Vector2)>
{
    public NodeRule (rule_delegate rule) : base(rule){}
}

public class GraphRule : IRule<UndirectedBidirectionalGraph<int, UndirectedEdge<int>>>
{
    public GraphRule (rule_delegate rule) : base(rule){}
}


public abstract class IRule<Input>
{
    public delegate bool rule_delegate (Input input, out string output_message);
    protected rule_delegate _rule;

    protected IRule (rule_delegate rule)
    {
        _rule = rule;
    }

    public void CheckRule(Input input)
    {
        if (!_rule(input, out string output)){
            throw new RoomGraphRulesException(output);
        }
    }
}