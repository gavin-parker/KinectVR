using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
/* Created by Gavin Parker 05/2017
 * An example animated hand to handle open/close motions
 */
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
        animator.SetBool("Pointing", false);
    }

    protected override void open()
    {
        animator.SetBool("Closed", false);
        animator.SetBool("Pointing", false);

    }

    protected override void point()
    {
        animator.SetBool("Closed", false);
        animator.SetBool("Pointing", true);
    }

    void OnApplicationQuit()
    {

    }

}

