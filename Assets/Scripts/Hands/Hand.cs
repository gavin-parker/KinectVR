using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public abstract class Hand : MonoBehaviour
{

    public enum HandStatus { Open, Close };
    public HandStatus status = HandStatus.Open;
    public bool holding = false;
    public float speed;
    public Windows.Kinect.TrackingConfidence trackingConfidence;
    public KinectPlayer player;
    private bool change = false;

    private Vector3 lastPosition;
    public Vector3 velocity;
    Grabbable heldObject;


    public Collider[] things;
    public GameObject grab_position;
    public KinectCamera kinect_view;

    private HashSet<Collider> _onBounds;

    public bool right_hand;

    public void init(KinectPlayer player, KinectCamera camera)
    {
        this.player = player;
        this.kinect_view = camera;
        _onBounds = new HashSet<Collider>();

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


    // Update is called once per frame
    void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;

    }

    public void OpenHand()
    {
        if (status == HandStatus.Close)
        {
            status = HandStatus.Open;
            change = true;
            open();
            ReleaseObject();
        }

    }

    public void CloseHand()
    {
        if (status == HandStatus.Open)
        {
            status = HandStatus.Close;
            change = true;
            close();
            GrabObject();

        }

    }


    protected abstract void close();
    protected abstract void open();

    private Grabbable GetClosestObject()
    {
        Grabbable closest = null;
        Vector3 p = grab_position.transform.position;
        float distance = Mathf.Infinity;
        if (_onBounds.Count > 0)
        {
            foreach (Collider g in _onBounds)
            {
                if (g != null)
                {
                    Vector3 diff = g.ClosestPointOnBounds(p) - p;
                    float current_distance = diff.sqrMagnitude;
                    Grabbable grabbable = g.gameObject.GetComponent<Grabbable>();
                    if (current_distance < distance && grabbable != null)
                    {
                        distance = current_distance;
                        closest = grabbable;
                    }
                }
            }
        }
        return closest;
    }


    //checks a surrounding sphere for objects, grabs them
    private void GrabObject()
    {
        if (holding) return;
        heldObject = null;

        if (velocity.magnitude > 100)
        {
            return;
        }

        Grabbable grabTarget = GetClosestObject();
        if (grabTarget == null) return;
        grabTarget.Grab(this);
        heldObject = grabTarget;
    }

    private void ReleaseObject()
    {
        if (heldObject == null) return;
        _onBounds.Clear();
        holding = false;
        heldObject.Release(this);
        return;
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
        if (!gother.CompareTag("Hand") && !holding && _onBounds != null)
        {
            _onBounds.Add(other);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;
        GameObject gother = other.gameObject;
        if (!gother.CompareTag("Hand") && !holding && _onBounds != null)
        {
            _onBounds.Add(other);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        GameObject gother = other.gameObject;
        if (holding && gother == heldObject.gameObject) return;
        if (!gother.CompareTag("Hand") && _onBounds != null)
        {
            _onBounds.Remove(other);
        }
    }
}
