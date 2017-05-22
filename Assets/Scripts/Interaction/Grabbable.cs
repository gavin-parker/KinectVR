using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/*  Created by Gavin Parker 05/2017
 *  Determines interactions with grabbable objects, handles highlighting and provides virtual grab/release options
 *  Add/ implement this to any object if you want to be able to interact with it
 */

public abstract class Grabbable : Highlighter
{
    private Transform _originalParent;
    private bool _kinematic = false;
    public float ThrowModifier = 1.0f;
    protected Hand GrabbingHand;
    public bool SnapToHand = false;
    public bool ShowHand = true;
    public virtual void Grab(Hand hand)
    {
        _originalParent = transform.parent;
        transform.parent = hand.transform;
        if (GetComponent<Rigidbody>() != null)
        {
            _kinematic = GetComponent<Rigidbody>().isKinematic;
            GetComponent<Rigidbody>().isKinematic = true;
        }
        if (SnapToHand)
        {
            transform.position = hand.transform.position;
        }
        if (!ShowHand)
        {
            hand.SetVisible(false);
        }
        GrabbingHand = hand;
    }

    public virtual void Release(Hand hand)
    {
        transform.parent = _originalParent;
        if (GetComponent<Rigidbody>() == null) return;
        GetComponent<Rigidbody>().isKinematic = _kinematic;
        GetComponent<Rigidbody>().AddForce(hand.GetVelocity() * ThrowModifier, ForceMode.VelocityChange);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Hand"))
        {
            SetOutline(Color.green);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Hand"))
        {
            RemoveOutline();
        }
    }
}

