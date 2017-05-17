using Kalman;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class KalmanFilter : Filter
{
    IKalmanWrapper kalman;
    Vector3 recording = new Vector3(0, 0, 0);

    public KalmanFilter()
    {
        kalman = new MatrixKalmanWrapper();
    }


    public Vector3 predict()
    {
        return recording;
    }

    public void record(Vector3 input)
    {
        recording = kalman.Update(input);
    }
}

