using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kinect = Windows.Kinect;
using System.IO;
using UnityEngine.VR;
using UnityEngine.UI;
using System.Threading;
using System;
using System.Linq;


public class KinectCamera : MonoBehaviour
{
    public GameObject BodySourceManager;
    public Hand right_hand;
    public Hand left_hand;
    public GameObject torso;
    public GameObject head;
    private GameObject bodyRoot;
    public int tracking_frames = 8;
    public bool rightHandClosed = false;
    public bool leftHandClosed = false;
    public bool rightHandTracked = false;
    public bool leftHandTracked = false;
    private float rightHandVelocity = 0;
    private float leftHandVelocity = 0;
    private int r_hand_closed_frames = 0;
    private int r_hand_open_frames = 0;
    private int l_hand_closed_frames = 0;
    private int l_hand_open_frames = 0;
    private ulong player_id = 99;
    bool runningThread = true;
    private bool started = true;
    private TrackingContext leftHandContext = TrackingContext.Medium;
    private TrackingContext rightHandContext = TrackingContext.Medium;
    private Thread bodyThread = null;
    public enum TrackingContext { Slow, Medium, Fast };

    //holds all the hand joint objects - palm, wrist, thumb, tip
    public Dictionary<Kinect.JointType, GameObject> player_objects = new Dictionary<Kinect.JointType, GameObject>();

    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    private Kinect.Body trackedBody;
    private GameObject trackedBodyObject;
    private Dictionary<Kinect.JointType, Transform> bodyTransforms = new Dictionary<Kinect.JointType, Transform>();
    private Dictionary<Kinect.JointType, KalmanFilter> bodyFilters = new Dictionary<Kinect.JointType, KalmanFilter>();
    private Dictionary<Kinect.JointType, Vector3> bodyPositions = new Dictionary<Kinect.JointType, Vector3>();

    public Kinect.JointType[] essentialJoints;
    private Kinect.JointType[] unessentialJoints;
    private Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },

        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },

        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },

        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    private void Start()
    {
        List<Kinect.JointType> importantJoints = essentialJoints.ToList<Kinect.JointType>();
        List<Kinect.JointType> unimportantJoints = new List<Kinect.JointType>();
        bodyRoot = new GameObject();
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            if (!importantJoints.Contains(jt))
            {
                unimportantJoints.Add(jt);
            }
        }
        unessentialJoints = unimportantJoints.ToArray<Kinect.JointType>();
    }
    void Update()
    {
        if (BodySourceManager == null)
        {
            return;
        }

        _BodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_BodyManager == null)
        {
            return;
        }

        Kinect.Body[] data = _BodyManager.GetData();
        if (data == null)
        {
            return;
        }

        List<ulong> trackedIds = new List<ulong>();
        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                trackedIds.Add(body.TrackingId);
                break;
            }
        }

        List<ulong> knownIds = new List<ulong>(_Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(_Bodies[trackingId]);
                _Bodies.Remove(trackingId);
            }
        }

        foreach (var body in data)
        {
            if (body == null)
            {
                continue;
            }

            if (body.IsTracked)
            {
                if (!_Bodies.ContainsKey(body.TrackingId))
                {
                    if (player_id == 99)
                    {
                        _Bodies[body.TrackingId] = CreateBodyObject(body);
                        Vector3 headObject = GetVector3FromJoint(body.Joints[Kinect.JointType.Head]);
                        Kinect.JointOrientation headOrientation = body.JointOrientations[Kinect.JointType.Head];
                        Debug.Log(headOrientation.Orientation);
                        float headHeight = headObject.y;
                        float idealHeight = 20;
                        float feetOffset = headHeight - idealHeight;
                        player_id = body.TrackingId;
                        started = false;
                        trackedBody = body;
                        trackedBodyObject = _Bodies[body.TrackingId];
                        bodyThread = new Thread(new ThreadStart(refreshBody));
                        bodyThread.Start();

                    }
                }
                if (body.TrackingId == player_id)
                {
                    UpdateBodyObject(body, _Bodies[body.TrackingId]);
                    adjustBodyParts(body, _Bodies[body.TrackingId]);
                    //Debug.DrawLine(playerBounds.min, playerBounds.max, Color.red, 2f);

                }
                break;
            }
        }
    }

    void OnApplicationQuit()
    {
        runningThread = false;
        if (bodyThread != null)
        {
            try
            {
                bodyThread.Join();
            }
            catch (Exception e)
            {
                bodyThread.Abort();

            }
        }
    }
    void OnDestroyed()
    {
        runningThread = false;
        if (bodyThread != null)
        {
            try
            {
                bodyThread.Join();
            }
            catch (Exception e)
            {
                bodyThread.Abort();

            }
        }
    }

    private void refreshBody()
    {
        while (runningThread)
        {
            try
            {

                RefreshBodyObject(trackedBody);
                Thread.Sleep(3);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Thread closed" + e);
                Thread.CurrentThread.Abort();
            }
        }
    }



    //I'm really sorry about this
    private void adjustBodyParts(Kinect.Body body, GameObject bodyObject)
    {

        head.transform.position = Vector3.Slerp(head.transform.position, player_objects[Kinect.JointType.Head].transform.position, Time.deltaTime * 10.0f);

        if (!started)
        {
            started = true;
        }
        rightHandVelocity = (player_objects[Kinect.JointType.HandRight].transform.position - right_hand.transform.position).magnitude / Time.deltaTime;
        leftHandVelocity = (player_objects[Kinect.JointType.HandLeft].transform.position - left_hand.transform.position).magnitude / Time.deltaTime;
        right_hand.transform.position = Vector3.Slerp(right_hand.transform.position, player_objects[Kinect.JointType.HandRight].transform.position, Time.deltaTime * 10.0f);
        left_hand.transform.position = Vector3.Slerp(left_hand.transform.position, player_objects[Kinect.JointType.HandLeft].transform.position, Time.deltaTime * 10.0f);

        //Adjust body rotation
        Vector3 spine = player_objects[Kinect.JointType.SpineShoulder].transform.position - player_objects[Kinect.JointType.SpineMid].transform.position;
        Vector3 spine_rotation = player_objects[Kinect.JointType.ShoulderLeft].transform.position - player_objects[Kinect.JointType.ShoulderRight].transform.position;
        Vector3 spine_forward = Vector3.Cross(spine_rotation, spine);

        torso.transform.position = player_objects[Kinect.JointType.SpineMid].transform.position;
        torso.transform.rotation = Quaternion.Slerp(torso.transform.rotation, Quaternion.LookRotation(spine_forward, spine), Time.deltaTime * 10.0f);

        Vector3 r_handVector = player_objects[Kinect.JointType.HandTipRight].transform.position - player_objects[Kinect.JointType.HandRight].transform.position;
        Vector3 l_handVector = player_objects[Kinect.JointType.HandTipLeft].transform.position - player_objects[Kinect.JointType.HandLeft].transform.position;
        Vector3 r_wristVector = player_objects[Kinect.JointType.HandTipRight].transform.position - player_objects[Kinect.JointType.WristRight].transform.position;
        Vector3 l_wristVector = player_objects[Kinect.JointType.HandTipLeft].transform.position - player_objects[Kinect.JointType.WristLeft].transform.position;

        Vector3 r_handRotation = player_objects[Kinect.JointType.ThumbRight].transform.position - player_objects[Kinect.JointType.HandRight].transform.position;
        Vector3 l_handRotation = player_objects[Kinect.JointType.ThumbLeft].transform.position - player_objects[Kinect.JointType.HandLeft].transform.position;

        Vector3 r_handUp = Vector3.Cross(r_handRotation, r_handVector);
        Vector3 l_handUp = Vector3.Cross(l_handVector, l_handRotation);


        if (body.HandRightConfidence == Windows.Kinect.TrackingConfidence.Low)
        {
            rightHandTracked = false;
        }
        else
        {
            rightHandTracked = true;
        }

        if (body.HandLeftConfidence == Windows.Kinect.TrackingConfidence.Low)
        {
            leftHandTracked = false;
        }
        else
        {
            leftHandTracked = true;
        }


        //Adjust hand rotations
        if (body.HandRightState == Windows.Kinect.HandState.Closed)
        {
            r_hand_closed_frames++;
            if (r_hand_closed_frames > getTrackingFrames(true) || body.HandRightConfidence == Kinect.TrackingConfidence.High)
            {
                rightHandClosed = true;
                r_hand_open_frames = 0;
            }


        }
        else
        {
            r_hand_open_frames++;
            if (r_hand_open_frames > getTrackingFrames(true) || body.HandRightConfidence == Kinect.TrackingConfidence.High)
            {
                rightHandClosed = false;
                r_hand_closed_frames = 0;
            }


        }

        if (body.HandLeftState == Windows.Kinect.HandState.Closed)
        {
            l_hand_closed_frames++;
            if (l_hand_closed_frames > getTrackingFrames(false) || body.HandLeftConfidence == Kinect.TrackingConfidence.High)
            {
                leftHandClosed = true;
                l_hand_open_frames = 0;
            }

        }
        else
        {
            l_hand_open_frames++;
            if (l_hand_open_frames > getTrackingFrames(false) || body.HandLeftConfidence == Kinect.TrackingConfidence.High)
            {
                leftHandClosed = false;
                l_hand_closed_frames = 0;
            }

        }


        if (rightHandClosed)
        {
            Quaternion target = Quaternion.LookRotation(r_wristVector);
            right_hand.transform.rotation = Quaternion.Slerp(right_hand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        else
        {
            Quaternion target = Quaternion.LookRotation(r_handVector, r_handUp);
            float diff = Mathf.Abs(target.eulerAngles.z - right_hand.transform.rotation.eulerAngles.z);
            right_hand.transform.rotation = Quaternion.Slerp(right_hand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        if (leftHandClosed)
        {
            Quaternion target = Quaternion.LookRotation(l_wristVector);
            left_hand.transform.rotation = Quaternion.Slerp(left_hand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        else
        {
            Quaternion target = Quaternion.LookRotation(l_handVector, l_handUp);
            left_hand.transform.rotation = Quaternion.Slerp(left_hand.transform.rotation, target, Time.deltaTime * 10.0f);
        }

        //Debug.Log("Right hand spread: " + (r_handVector.sqrMagnitude + r_handRotation.sqrMagnitude));
    }

    public void nDrawGizmos()
    {
        Vector3 r_handVector = player_objects[Kinect.JointType.HandTipRight].transform.position - player_objects[Kinect.JointType.HandRight].transform.position;
        Vector3 l_handVector = player_objects[Kinect.JointType.HandTipLeft].transform.position - player_objects[Kinect.JointType.HandLeft].transform.position;
        Gizmos.DrawLine(player_objects[Kinect.JointType.HandRight].transform.position, player_objects[Kinect.JointType.HandTipRight].transform.position);
    }

    //takes the tracking context of the hand and returns the number of continuous frames required to make a state change
    private int getTrackingFrames(bool rightHand)
    {
        int speedAdjust = 2;
        if (rightHand)
        {

            if (rightHandVelocity > 500)
            {

                speedAdjust = 10;

            }
            switch (rightHandContext)
            {
                case TrackingContext.Medium:
                    return tracking_frames;
                case TrackingContext.Fast:
                    return tracking_frames / 2;
                case TrackingContext.Slow:
                    return tracking_frames + speedAdjust;
            }
        }
        else
        {
            if (leftHandVelocity > 500)
            {
                speedAdjust = 10;

            }
            switch (leftHandContext)
            {
                case TrackingContext.Medium:
                    return tracking_frames;
                case TrackingContext.Fast:
                    return tracking_frames / 2;
                case TrackingContext.Slow:
                    return tracking_frames + speedAdjust;
            }
        }
        return tracking_frames;
    }

    public void setTrackingContext(TrackingContext newContext, bool rightHand)
    {
        if (rightHand)
        {
            rightHandContext = newContext;
        }
        else
        {
            leftHandContext = newContext;
        }
    }



    public GameObject getBodyPart(Kinect.JointType jt)
    {
        return bodyTransforms[jt].gameObject;
    }

    private GameObject CreateBodyObject(Kinect.Body kinectBody)
    {
        ulong id = kinectBody.TrackingId;
        GameObject body = bodyRoot;
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            GameObject jointObj = new GameObject();
            //jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            jointObj.transform.localScale = new Vector3(5f, 5f, 5f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = body.transform;
            bodyTransforms.Add(jt, jointObj.transform);
            bodyPositions.Add(jt, GetVector3FromJoint(kinectBody.Joints[jt]));
            bodyFilters.Add(jt, new KalmanFilter());
            player_objects.Add(jt, jointObj);
        }
        InputTracking.Recenter();
        return body;
    }

    private void UpdateBodyObject(Kinect.Body body, GameObject bodyObject)
    {
        foreach (Kinect.JointType jt in essentialJoints)
        {
            Transform jointObj = bodyTransforms[jt];
            bodyPositions[jt] = bodyFilters[jt].predict();
            jointObj.localPosition = bodyPositions[jt];
        }
        foreach (Kinect.JointType jt in unessentialJoints)
        {
            Transform jointObj = bodyTransforms[jt];
            jointObj.localPosition = bodyPositions[jt];
        }
    }

    private void RefreshBodyObject(Kinect.Body body)
    {

        foreach (Kinect.JointType jt in essentialJoints)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;


            if (_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }

            Transform jointObj = bodyTransforms[jt];
            Vector3 pos = GetVector3FromJoint(sourceJoint);
            bodyFilters[jt].record(pos);

        }
        foreach (Kinect.JointType jt in unessentialJoints)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Joint? targetJoint = null;


            if (_BoneMap.ContainsKey(jt))
            {
                targetJoint = body.Joints[_BoneMap[jt]];
            }

            Transform jointObj = bodyTransforms[jt];
            Vector3 pos = GetVector3FromJoint(sourceJoint);
            bodyPositions[jt] = GetVector3FromJoint(sourceJoint);
        }

    }

    private static Vector3 GetVector3FromJoint(Kinect.Joint joint)
    {
        return new Vector3(-joint.Position.X * 10, joint.Position.Y * 10, joint.Position.Z * 10);
    }
}
