using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Random = ConcurrentRandom.ConcurrentRandom;
public static class RandomHelpers
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
            int optionChoice = r.Next(ArrayToString(array.ToArray()), 0, indexOptions.Count - 1);
            indices[i] = indexOptions[optionChoice];
            indexOptions.RemoveAt(optionChoice);
        }

        List<T> newSpan = new List<T>();
        foreach (int index in indices){
            newSpan.Add(array[index]);
        }

        array = newSpan.ToArray();
    }

    public static T ChooseRandom<T>(this ICollection<T> coll, Random random)
    {
        if (coll.Count == 0)
        {
            return default(T);
        }
        int rand = random.Next(ArrayToString(coll), 0, coll.Count - 1);
        T item = coll.OrderBy(i=>i.GetHashCode()).ToArray()[rand];
        coll.Remove(item);
        return item;
    }

    private static string ArrayToString<T>(ICollection<T> array)
    {
        return new List<T>(array.ToArray()).Select((t)=>t.ToString()).Aggregate((t1, t2)=>t1+t2)+"";
    }
}