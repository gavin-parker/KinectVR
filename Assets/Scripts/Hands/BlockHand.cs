using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

class BlockHand : Hand
{
    public GameObject thumb;
    public GameObject fingers;
    public Animator animator;
    private StringBuilder logger;


    private float _lastFingerDist = 0f;
    private float _minFingerDist = 1.0f;
    private float _maxFingerDist = 0.0f;


    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        logger = new StringBuilder();
    }

    protected override void close()
    {
        animator.SetBool("Closed", true);
    }

    protected override void open()
    {
        animator.SetBool("Closed", false);

    }

    void OnApplicationQuit()
    {
        if (right_hand)
        {
            File.WriteAllText("fingerDist.csv", logger.ToString());
        }
    }

}

