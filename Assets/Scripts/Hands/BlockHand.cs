using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class BlockHand : Hand
{
    public GameObject thumb;
    public GameObject fingers;
    public Animator animator;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }


    protected override void close()
    {
        animator.SetBool("closed", true);
    }

    protected override void open()
    {
        animator.SetBool("closed", false);

    }

    protected override void updateFingers()
    {
        FingerState fingerState = player.getFingers(right_hand);
        //thumb.transform.rotation = Quaternion.LookRotation(fingerState.thumbTip - transform.position, Vector3.up);
        //thumb.transform.LookAt(fingerState.thumbTip);
        //fingers.transform.LookAt(fingerState.fingerTip);
        Debug.DrawLine(transform.position, fingerState.fingerTip, Color.red, 1f);
        Debug.DrawLine(transform.position, fingerState.thumbTip, Color.red, 1f);

    }
}

