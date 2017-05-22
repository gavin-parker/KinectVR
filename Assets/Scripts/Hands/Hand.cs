using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
/* Created by Gavin Parker 03/2017
 * Handles interaction of hands and virtual environment
 * 
 */
public abstract class Hand : MonoBehaviour
{

    public enum HandStatus { Open, Close, Point };
    public HandStatus status = HandStatus.Open;
    public bool holding = false;
    public float speed;
    public Windows.Kinect.TrackingConfidence trackingConfidence;
    public KinectPlayer player;

    private Vector3 lastPosition;
    public Vector3 velocity;
    Grabbable heldObject;


    public Collider[] things;
    public GameObject grab_position;
    public KinectCamera kinect_view;

    private HashSet<Collider> _onBounds;
    private bool inGracePeriod = false;
    private readonly Filter velocityFilter = new KalmanFilter();
    public bool right_hand;
    private Renderer[] _myRenderers;
    private Collider[] _myColliders;
    private Collider grabCollider;
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

        _myRenderers = GetComponentsInChildren<Renderer>();
        _myColliders = GetComponentsInChildren<Collider>();
        grabCollider = GetComponent<Collider>();
        lastPosition = transform.position;
    }


    // Update is called once per frame
    void Update()
    {
        velocity = (transform.position - lastPosition) / Time.deltaTime;
        velocityFilter.record(velocity);
        lastPosition = transform.position;
    }

    public void OpenHand()
    {
        if (status != HandStatus.Open)
        {
            status = HandStatus.Open;
            open();
            ReleaseObject();
        }
        SetVisible(true);
    }

    public void CloseHand()
    {
        if (status != HandStatus.Close)
        {
            status = HandStatus.Close;
            close();
            GrabObject();
        }

    }

    public void PointHand()
    {
        if (status != HandStatus.Point)
        {
            status = HandStatus.Point;
            point();
        }
    }


    public Vector3 GetVelocity()
    {
        return velocityFilter.predict();
    }

    protected abstract void close();
    protected abstract void open();
    protected abstract void point();

    private IEnumerator gracePeriod()
    {
        if (inGracePeriod) yield break;
        inGracePeriod = true;
        yield return new WaitForSeconds(0.1f);
        inGracePeriod = false;
    }

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

    public void SetVisible(bool visible)
    {
        if (_myRenderers == null) return;
        foreach (Renderer rend in _myRenderers)
        {
            if (rend != null)
                rend.enabled = visible;

        }
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
        if (grabTarget == null)
        {
            StartCoroutine(gracePeriod());
            return;
        }
        grabTarget.Grab(this);
        heldObject = grabTarget;
        foreach (Collider col in _myColliders)
        {
            if (col != grabCollider)
            {
                col.enabled = false;
            }
        }
    }

    private void ReleaseObject()
    {
        if (heldObject == null) return;
        _onBounds.Clear();
        holding = false;
        heldObject.Release(this);
        StartCoroutine(SetPhysicalColliders(true, 0.2f));
        return;
    }


    private IEnumerator SetPhysicalColliders(bool enabled, float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (Collider col in _myColliders)
        {
            if (col != grabCollider)
            {
                col.enabled = enabled;
            }
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
        if (!gother.CompareTag("Hand") && !holding && _onBounds != null)
        {
            _onBounds.Add(other);
            if (inGracePeriod)
            {
                GrabObject();
            }
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
