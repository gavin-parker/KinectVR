using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class MovingAverageFilter : Filter
{
    List<Vector3> recordings;
    int count;

    public MovingAverageFilter(int count)
    {
        recordings = new List<Vector3>();
        this.count = count;
    }


    public Vector3 predict()
    {
        Vector3 average = Vector3.zero;
        foreach(Vector3 v in recordings)
        {
            average += v;
        }
        return average;
    }

    public void record(Vector3 input)
    {
        recordings.Add(input);
        if (recordings.Count >= count)
        {
            recordings.RemoveAt(0);
        }
    }
}

