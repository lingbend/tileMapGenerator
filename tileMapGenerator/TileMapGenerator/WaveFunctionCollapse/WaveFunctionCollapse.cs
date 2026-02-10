// Based on code Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)
// Modified by Benjamin Lingwall 2026

namespace WaveFunctionCollapse;

public class WaveFunctionCollapse
{
    public Random Random{get; set;}
    public WaveFunctionCollapseSettings Settings {get; set;}

    public WaveFunctionCollapse(WaveFunctionCollapseSettings settings, int seed = 0)
    {
        Settings = settings;
        if (seed == 0)
        {
            Random = new Random();
        }
        else
        {
            Random = new Random(seed);
        }
    }

    public WaveFunctionCollapse(string name, string heuristicString="", int seed = 0)
    {
        if (heuristicString == "")
        {
            Settings = new WaveFunctionCollapseSettings(name);
        }
        else
        {
            Settings = new WaveFunctionCollapseSettings(name, heuristicString);
        }
        
        Settings.InitializeSimple();
        if (seed == 0)
        {
            Random = new Random();
        }
        else
        {
            Random = new Random(seed);
        }
    }

    public WaveFunctionCollapse()
    {
        Settings = new WaveFunctionCollapseSettings("default", "Scanline");
        Settings.InitializeSimple();
        Random = Random = new Random();
    }

    // This should be thread determinant iff a thread determinant seed is provided
    public void Run(int? seed = null, int screenshots = 2, int limit = -1)
    {
        if (seed is null)
        {
            seed = Random.Next();
        }
        for (int i = 0; i < screenshots; i++)
        {
            for (int k = 0; k < 10; k++)
            {
                bool success = Settings.Model.Run((int) seed, limit);
                if (success)
                {
                    Settings.Model.Save($"../../../WaveFunctionCollapse/output/{Settings.Name} {seed}.png");
                    break;
                }
            }
        }
    }
}