using System.Collections.Immutable;
using System.Numerics;
using QuikGraph;
using BinaryGrid;
using TileMapGenerator;

namespace TileMapGenerator;

internal interface IDedThing
{
    int ID{get; internal set;}
}

