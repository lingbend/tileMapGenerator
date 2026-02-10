// Based on code Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)
// Modified by Benjamin Lingwall 2026

namespace WaveFunctionCollapse;


public class WaveFunctionCollapseSettings
{
    public string Name;
    public int Size{get; set;}= 24; // if overlapping defaults to 48 else 24
    public int Width{get; set;} = 24; // defaults to size
    public int Height{get; set;} = 24; // defaults to size
    public bool Periodic{get; set;} = false;
    internal Model? Model {get; set;} // is OverlappingModel if overlapping else SimpleTiledModel
    internal Model.Heuristic Heuristic;
    public string HeuristicString{get; private set;}


    public WaveFunctionCollapseSettings(string name, string heuristicString="Model.Heuristic.Entropy")
    {
        Name = name;
        SetHeuristic(heuristicString);
    }

    public void SetHeuristic(string heuristicString)
    {
        HeuristicString = heuristicString;
        Heuristic = heuristicString == "Scanline" ? Model.Heuristic.Scanline : (heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy);
    }

    public void InitializeOverlapping(int N = 3, bool periodicInput = true, int symmetry = 8, bool ground = false)
    {
        Size = Width = Height = 48;
        Model = new OverlappingModel(Name, N, Width, Height, periodicInput, Periodic, symmetry, ground, Heuristic);
    }

    public void InitializeSimple(string? subset = null, bool blackBackground=false)
    {
        Model = new SimpleTiledModel(Name, subset, Width, Height, Periodic, blackBackground, Heuristic);
    }
}