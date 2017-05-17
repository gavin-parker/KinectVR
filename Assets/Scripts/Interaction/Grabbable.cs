using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public abstract class Grabbable : Highlighter
{
    private Transform _originalParent;
    private bool kinematic = false;
    public float throwModifier = 1.0f;
    public virtual void Grab(Hand hand)
    {
        _originalParent = transform.parent;
        transform.parent = hand.transform;
        if (GetComponent<Rigidbody>() != null)
        {
            kinematic = GetComponent<Rigidbody>().isKinematic;
            GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public virtual void Release(Hand hand)
    {
        transform.parent = _originalParent;
        if (GetComponent<Rigidbody>() != null)
        {
            GetComponent<Rigidbody>().isKinematic = kinematic;
            GetComponent<Rigidbody>().AddForce(hand.velocity * throwModifier, ForceMode.VelocityChange);
        }
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

