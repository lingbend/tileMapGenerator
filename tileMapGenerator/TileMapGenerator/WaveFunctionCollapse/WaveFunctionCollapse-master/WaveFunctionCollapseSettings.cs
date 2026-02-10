// Based on code Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)
// Modified by Benjamin Lingwall 2026

namespace WaveFunctionCollapse;


public class WaveFunctionCollapseSettings
{
    public string Name;
    int Size{get; set;}= 24; // if overlapping defaults to 48 else 24
    int Width{get; set;} = 24; // defaults to size
    int Height{get; set;} = 24; // defaults to size
    bool Periodic{get; set;} = false;
    public string HeuristicString {get; set;}
    internal Model? Model {get; set;} // is OverlappingModel if overlapping else SimpleTiledModel
    Model.Heuristic heuristic; // is heuristicString == "Scanline" ? Model.Heuristic.Scanline : (heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy);


    public WaveFunctionCollapseSettings(string name, string heuristicString)
    {
        Name = name;
        HeuristicString = heuristicString;
    }

    public void InitializeOverlapping(int N = 3, bool periodicInput = true, int symmetry = 8, bool ground = false)
    {
        Size = Width = Height = 48;
        Model = new OverlappingModel(Name, N, Width, Height, periodicInput, Periodic, symmetry, ground, heuristic);
    }

    public void InitializeSimple(string? subset = null, bool blackBackground=false)
    {
        Model = new SimpleTiledModel(Name, subset, Width, Height, Periodic, blackBackground, heuristic);
    }
}