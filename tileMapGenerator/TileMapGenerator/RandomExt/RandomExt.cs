using ConcurrentRandom;
using System;
using System.Collections.Generic;

public static class RandomExt
{
    public static void Shuffle<T>(this Random r, Span<T> array)
    {
        int size = array.Length;
        int[] indices = new int[size];
        List<int> indexOptions = new List<int>();
        
        for (int i = 0; i < size; i++)
        {
            indexOptions.Add(i);
        }

        for (int i = 0; i < size; i++)
        {
            int optionChoice = r.Next(indexOptions.Count);
            indices[i] = indexOptions[optionChoice];
            indexOptions.RemoveAt(optionChoice);
        }

        List<T> newSpan = new List<T>();
        foreach (int index in indices){
            newSpan.Add(array[index]);
        }

        array = newSpan.ToArray();
    }
}