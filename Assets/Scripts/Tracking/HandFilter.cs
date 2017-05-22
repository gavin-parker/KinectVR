using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/* Created by Gavin Parker 05/2017
 * Handles when to change hand state from open/close.
 */
public class HandFilter
{
    private readonly List<bool> _records;
    private readonly int _frameCount;

    public HandFilter(int frameCount)
    {
        this._frameCount = frameCount;
        _records = new List<bool>(frameCount);
    }

    public bool Predict()
    {
        if (_records.Count < _frameCount)
        {
            return false;
        }
        var trueCounts = _records.Count(record => record);
        return (trueCounts > (_frameCount / 2));
    }

    public void Record(bool input)
    {
        _records.Add(input);
        if (_records.Count >= _frameCount)
        {
            _records.RemoveAt(0);
        }
    }
}

