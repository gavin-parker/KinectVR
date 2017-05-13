using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public interface Filter
{

    void record(Vector3 input);

    Vector3 predict();

}

