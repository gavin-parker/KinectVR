using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public abstract class Hand : MonoBehaviour
{

    public enum HandStatus { Open, Close };
    public HandStatus status = HandStatus.Open;
    public bool holding = false;

    private bool change = false;

    private Vector3 lastPosition;
    private Vector3 velocity;
    GameObject heldObject;


    public Collider[] things;
    public GameObject grab_position;
    public KinectCamera kinect_view;

    private HashSet<Collider> onBounds;

    public bool right_hand;

    private void Awake()
    {
        onBounds = new HashSet<Collider>();
    }

    // Use this for initialization
    void Start()
    {
        if (kinect_view == null)
        {
            Debug.LogError("No kinect found");

        }
        lastPosition = transform.position;
    }


    protected abstract void updateFingers();
    // Update is called once per frame
    void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        try
        {
            updateFingers();
        }
        catch (Exception e)
        {

        }
    }

    public void openHand()
    {
        if (status == HandStatus.Close)
        {
            status = HandStatus.Open;
            change = true;
            open();
        }

    }

    public void closeHand()
    {
        if (status == HandStatus.Open)
        {
            status = HandStatus.Close;
            change = true;
            close();
        }

    }

    protected abstract void close();
    protected abstract void open();

    private GameObject getClosestObject()
    {
        GameObject closest = null;
        Vector3 p = grab_position.transform.position;
        float distance = Mathf.Infinity;
        if (onBounds.Count > 0)
        {
            foreach (Collider g in onBounds)
            {
                if (g != null)
                {
                    Vector3 diff = g.ClosestPointOnBounds(p) - p;
                    float current_distance = diff.sqrMagnitude;
                    if (current_distance < distance)
                    {
                        distance = current_distance;
                        closest = g.gameObject;
                    }
                }
            }
        }
        return closest;
    }


    //checks a surrounding sphere for objects, grabs them
    private void grabObject()
    {
        if (holding) return;
        heldObject = null;

        if (velocity.magnitude > 100)
        {
            return;
        }

        GameObject grabTarget = getClosestObject();
        if (grabTarget == null) return;
    }

    private void releaseObject()
    {
        if (heldObject == null)
        {
            onBounds.Clear();
            holding = false;
            return;
        }
    }

    //function called to snap object to palm 
    private void snapToHand(GameObject placeable)
    {
        // might need to change the positions slightly to make it nicer looking
        placeable.transform.position = gameObject.transform.position;
    }

    public float getSpeed()
    {
        return velocity.magnitude;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        GameObject gother = other.gameObject;
        if (gother.layer == 9 || gother.layer == 10 || gother.layer == 14 && !holding && onBounds != null)
        {
            onBounds.Add(other);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;
        GameObject gother = other.gameObject;
        if (gother.layer == 9 || gother.layer == 10 || gother.layer == 14 && !holding && onBounds != null)
        {
            onBounds.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        GameObject gother = other.gameObject;
        if (gother.layer == 9 || gother.layer == 10 || gother.layer == 14 && onBounds != null)
        {
            onBounds.Remove(other);
        }
    }
}
