namespace CellularRoomGrower;

using BinaryGrid;
using Graph = QuikGraph.UndirectedGraph<TileMapGenerator.RoomVertex<System.Numerics.Vector2>, TileMapGenerator.RoomEdge<System.Numerics.Vector2>>;
using Vertex = TileMapGenerator.RoomVertex<System.Numerics.Vector2>;
using Edge = TileMapGenerator.RoomEdge<System.Numerics.Vector2>;
using System.Numerics;
using NetTopologySuite.Densify;
using NetTopologySuite.Geometries;
using NetTopologySuite.Utilities;
using NetTopologySuite.Operation.Overlay.Snap;
using static Math;

public class CellularRoomGrowerSettings
{
    public Func<Graph, IEnumerable<Room>, Room?> Prioritizer{get; set;} = DefaultPrioritizer;
    public Func<IEnumerable<Vector2>, Room, Vector2> DirectionChooser{get; set;} = DefaultDirectionChooser;
    internal List<Vector2> ValidDirections{get; set;} = DefaultValidDirections;
    public static List<Vector2> DefaultValidDirections{get;} = [Vector2.UnitX, Vector2.UnitY, -Vector2.UnitX,
     -Vector2.UnitY];

    public Func<Graph, Vertex, Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>>> ShapeChooser{get; set;} = DefaultShapeChooser;
    public Func<Graph, Vertex, IEnumerable<string>>? Tagger{get; set;} = DefaultTagger;

    internal int MapArea{get; set;}
    public int MaxArea{get; set;} = 5_000;

    public Vector2 SideRatio{get; set;} = Vector2.One;

    public static Random Random{get;set;} = new Random();
    
    public static Room? DefaultPrioritizer(Graph graph, IEnumerable<Room> rooms)
    {
        if (rooms.Count() == 0)
        {
            return null;
        }
        IEnumerable<Room> small_rooms = rooms.Where(r=>(r.GetSides().Max(v=>v.X)-r.GetSides().Min(v=>v.X))<3 && (r.GetSides().Max(v=>v.Y)-r.GetSides().Min(v=>v.Y))<3);
        if (small_rooms.Count() > 0)
        {
            return Random.GetItems(small_rooms.ToArray(), 1).Single();
        }
        return Random.GetItems(rooms.ToArray(), 1).Single();
    }

    public static Vector2 DefaultDirectionChooser(IEnumerable<Vector2> directions, Room room)
    {
        return Random.GetItems(directions.ToArray(), 1).Single();
    }

    public static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> DefaultShapeChooser(Graph graph, Vertex vertex)
    {
        return (length, direction, corners) =>
        {
            List<Vector2> relevant_corners = new();

            foreach (var key in corners.Keys)
            {
                if (key.X == direction.X && key.X != 0)
                {
                    relevant_corners.Add(corners[key]);
                }
                else if (key.Y == direction.Y && key.Y != 0)
                {
                    relevant_corners.Add(corners[key]);
                }
            }
            LinkedList<Vector2> shape = new();
            Vector2 center = relevant_corners.Aggregate((v1, v2)=> v1+v2)/ 2.0f;
            shape.AddLast(new Vector2((int)(-length / 2.0 - 1.0))*Vector2.Abs(new Vector2(direction.Y, direction.X)));
            for (int i = 0; i < length; i++)
            {
                shape.AddLast(shape.Last!.Value+ Vector2.Abs(new Vector2(direction.Y, direction.X)));
            }
            shape.RemoveFirst();
            LinkedList<Vector2> temp_shape = new(shape);
            shape = new();
            foreach (Vector2 point in temp_shape)
            {
                shape.AddLast(new Vector2((float) Math.Round((point + center).X, MidpointRounding.ToPositiveInfinity), (float) Math.Round((point + center).Y, MidpointRounding.ToPositiveInfinity)));
            }
            return shape;
        };
    }

    public static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> CircularShapeChooser(Graph graph, Vertex vertex)
    {
        return (length, direction, corners) =>
        {
            Vector2 center = corners.Values.Aggregate((v1, v2)=>v1+v2)/4.0f;
            Vector2 min = corners.Values.Aggregate((v1, v2)=>Vector2.Min(v1, v2));
            Vector2 max = corners.Values.Aggregate((v1, v2)=>Vector2.Max(v1, v2));
            if (!(max.X - min.X >= 5 && max.Y - min.Y >= 6 || max.X - min.X >= 6 && max.Y - min.Y >= 5))
            {
                List<Vector2> relevant_corners = new();

                foreach (var key in corners.Keys)
                {
                    if (key.X == direction.X && key.X != 0)
                    {
                        relevant_corners.Add(corners[key]);
                    }
                    else if (key.Y == direction.Y && key.Y != 0)
                    {
                        relevant_corners.Add(corners[key]);
                    }
                }
                LinkedList<Vector2> shape2 = new();
                Vector2 center2 = relevant_corners.Aggregate((v1, v2)=> v1+v2)/ 2.0f;
                shape2.AddLast(new Vector2((int)(-length / 2.0 - 1.0))*Vector2.Abs(new Vector2(direction.Y, direction.X)));
                for (int i = 0; i < length; i++)
                {
                    shape2.AddLast(shape2.Last!.Value+ Vector2.Abs(new Vector2(direction.Y, direction.X)));
                }
                shape2.RemoveFirst();
                LinkedList<Vector2> temp_shape2 = new(shape2);
                shape2 = new();
                foreach (Vector2 point in temp_shape2)
                {
                    shape2.AddLast(new Vector2((float) Math.Round((point + center2).X, MidpointRounding.ToPositiveInfinity), (float) Math.Round((point + center2).Y, MidpointRounding.ToPositiveInfinity)));
                }
                return shape2;
            }

            double x_diameter = max.X - min.X;
            double y_diameter = max.Y - min.Y;
            double num_points = 2* ((2 * x_diameter) + (2 * y_diameter));
            double x_radius = .5 * x_diameter;
            double y_radius = .5 * y_diameter;

            HashSet<Vector2> points = new();
            

            if (direction.X != 0)
            {
                for (double i = min.X; i <= max.X; i += x_diameter / num_points)
                {
                    double i_offset = i - center.X;
                    double parametric_term =  y_radius * Sqrt(1 - (Pow(i_offset, 2) / Pow(x_radius, 2)));
                    
                    points.Add(new Vector2((float) i, center.Y + (float) (Double.Sign(direction.X) * parametric_term)));
                }
            }
            else
            {
                for (double j = min.Y; j <= max.Y; j += y_diameter / num_points)
                {
                    double j_offset = j - center.Y;
                    double parametric_term = x_radius * Sqrt(1 - (Pow(j_offset, 2) / Pow(y_radius, 2)));

                    points.Add(new Vector2(center.X + (float) (Double.Sign(direction.Y) * parametric_term), (float) j));
                }
            }
            
            HashSet<Vector2> temp_points = new(points);
            points.Clear();
            foreach (var point in temp_points)
            {
                if (Double.IsNormal(point.X) && Double.IsNormal(point.Y))
                {
                    points.Add(new Vector2((float) Round(point.X), (float) Round(point.Y)));
                }
            }
            return points;
        };
        






        // return NetEllipse();
    }

    private static Func<int, Vector2, Dictionary<Vector2, Vector2>, IEnumerable<Vector2>> NetEllipse()
    {
        return (length, direction, corners) =>
        {
            HashSet<Vector2> shape = new();
            List<Vector2> corners_vecs = new(corners.OrderBy(pair =>
            {
                if (pair.Key == new Vector2(1, 1))
                {
                    return 4;
                }
                else if (pair.Key == new Vector2(-1, 1))
                {
                    return 3;
                }
                else if (pair.Key == new Vector2(-1, -1))
                {
                    return 2;
                }
                else if (pair.Key == new Vector2(1, -1))
                {
                    return 1;
                }
                return 0;
            }).Select(pair => pair.Value));
            Coordinate[] coordinates = new Coordinate[4];

            for (int i = 0; i < 4; i++)
            {
                coordinates[i] = new Coordinate(corners_vecs[i].X, corners_vecs[i].Y);
            }
            var factory = GeometryFactory.Fixed;
            Envelope envelope = new Envelope(coordinates);
            List<Coordinate> ring_coordinates = new(coordinates);
            var shape_factory = new GeometricShapeFactory();
            shape_factory.Envelope = envelope;
            var ellipse = shape_factory.CreateEllipse();
            var coords = Densifier.Densify(ellipse, .5);
            var snapper = new GeometrySnapper(coords);
            snapper.SnapToSelf(.4, true);

            ring_coordinates = new(coords.Boundary.Coordinates);

            shape = new();

            foreach (Coordinate coord in ring_coordinates)
            {
                shape.Add(new Vector2((float)Math.Round(coord.X), (float)Math.Round(coord.Y)));
            }

            return shape;
        };
    }

    private void Test()
    {
        Vector2 direction = Vector2.Zero;
        double length = 0f;
        HashSet<Vector2> shape = new();
        List<Vector2> relevant_corners = new();
        List<Vector2> irrelevant_corners = new();
        Dictionary<Vector2, Vector2> corners = new();
        Func<double, double> ellipse;
        if (direction.X == 0)
        {
            double a = length / 2.0f;
            double b = Math.Abs(relevant_corners[0].Y - irrelevant_corners[0].Y) / 2.0f;
            double abinv = Math.ReciprocalEstimate(a*b);
            double bsquare = b * b;

            ellipse = (y=>
            {
                double bminyinv = Math.ReciprocalEstimate(b - y);
                double ysquare = y*y;
                double x = Math.ReciprocalEstimate((bsquare+ysquare)*abinv*bminyinv);
                return x;
            });
        }
        else
        {
            double a = Math.Abs(relevant_corners[0].Y - irrelevant_corners[0].Y) / 2.0f;
            double b = length / 2.0f;
            double b2inv = Math.ReciprocalEstimate(b*2);
            double asquare = a * a;

            ellipse = (x=>
            {
                double invx = Math.ReciprocalEstimate(x);
                double xsquare = x * x;
                double y = Math.ReciprocalEstimate((asquare+xsquare)*invx*b2inv);
                return y;
            });
        }
        for (float i = (float) -(length / 2.0f); i <= length / 2.0f; i += .25f)
        {
            shape.Add(new Vector2((float) ellipse(i))*Vector2.Abs(direction) + new Vector2(i) * Vector2.Abs(new Vector2(direction.Y, direction.X)) + (corners.Values.Aggregate((v1, v2)=>v1+v2) / 4.0f));                
        }
    }

    private static (float, float) GetQuarterCircleRadianArc(Vector2 direction)
    {
        float base_angle = (float) (Math.PI / 4.0f);
        // if x != 0 : sign y = sign y2
        // if y != 0 : sign x = sign x2
        // if x or y < 0 : includes the double negative one (3*)
        // if x or y > 0 : includes the double positive one
        (float, float) arc = (base_angle, base_angle);
        if (direction.X + direction.Y < 0)
        {
            arc.Item1 = 5.0f * base_angle;
            if (Math.Round(Math.Abs(direction.X), 1) > .1)
            {
                arc.Item2 = 3.0f * base_angle;
                // arc = (5.0f * arc.Item1, 7.0f * arc.Item2);;
            }
            else if (Math.Round(Math.Abs(direction.Y), 1) > .1)
            {
                arc.Item2 = 7.0f * base_angle;
            }
        }
        else if (direction.X + direction.Y > 0)
        {
            // arc.Item1 = base_angle; (already defined as such)
            if (Math.Round(direction.X, 1) > .1)
            {
                arc.Item2 = -1.0f * base_angle;
            }
            else if (Math.Round(direction.Y, 1) > .1)
            {
                arc.Item2 = 3.0f * base_angle;
            }
        }
        arc = (Math.Min(arc.Item1, arc.Item2), Math.Max(arc.Item1, arc.Item2));
        return arc;

    }
    

    public static IEnumerable<string> DefaultTagger(Graph graph, Vertex vertex)
    {
        return [];
    }
}