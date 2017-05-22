using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class HandModelSwap : Hand
{
    public GameObject openModel;
    public GameObject closeModel;


    protected override void open()
    {
        openModel.SetActive(true);
        closeModel.SetActive(false);
    }

    protected override void point()
    {
        
    }

    protected override void close()
    {
        openModel.SetActive(false);
        closeModel.SetActive(true);
    }

}

