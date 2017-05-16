using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FingerState
{
    public Vector3 fingerTip;
    public Vector3 thumbTip;

    public FingerState(Vector3 fingerTip, Vector3 thumbTip)
    {
        this.fingerTip = fingerTip;
        this.thumbTip = thumbTip;
    }
}

