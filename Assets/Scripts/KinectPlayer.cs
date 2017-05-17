using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VR;
using Windows.Kinect;


public class KinectPlayer : MonoBehaviour
{
    public GameObject head;
    public Hand rightHand;
    public Hand leftHand;
    public GameObject torso;
    public KinectCamera kinect;
    public ulong id;
    public bool isMainPlayer;

    [HideInInspector]
    public Dictionary<JointType, Transform> bodyTransforms = new Dictionary<JointType, Transform>();
    public Dictionary<JointType, KalmanFilter> bodyFilters = new Dictionary<JointType, KalmanFilter>();
    public Dictionary<JointType, Vector3> bodyPositions = new Dictionary<JointType, Vector3>();
    //holds all the hand joint objects - palm, wrist, thumb, tip
    public Dictionary<JointType, GameObject> player_objects = new Dictionary<JointType, GameObject>();

    private Windows.Kinect.Body trackedBody;
    private Vector3 playerPositionOffset = new Vector3(0, 0, 0);

    //Warps the player to a given location ( the location should be at desired eye level)
    public void warpToLocation(Vector3 target)
    {
        Vector3 actualHeadLocation = bodyPositions[JointType.Head];
        Vector3 difference = target - actualHeadLocation;
        playerPositionOffset = difference;

    }

    public GameObject getBodyPart(JointType jt)
    {
        return bodyTransforms[jt].gameObject;
    }

    public GameObject CreateBodyObject(Body kinectBody, GameObject newBody)
    {
        ulong id = kinectBody.TrackingId;
        trackedBody = kinectBody;
        this.id = id;
        for (JointType jt = JointType.SpineBase; jt <= JointType.ThumbRight; jt++)
        {
            GameObject jointObj = new GameObject();
            //jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            jointObj.transform.localScale = new Vector3(5f, 5f, 5f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = newBody.transform;
            this.bodyTransforms.Add(jt, jointObj.transform);
            this.bodyPositions.Add(jt, GetVector3FromJoint(kinectBody.Joints[jt]));
            this.bodyFilters.Add(jt, new KalmanFilter());
            this.player_objects.Add(jt, jointObj);
        }

        rightHand.init(this, kinect);
        leftHand.init(this, kinect);
        InputTracking.Recenter();
        return newBody;
    }


    public void UpdateBodyObject()
    {
        foreach (JointType jt in kinect.essentialJoints)
        {
            Transform jointObj = bodyTransforms[jt];
            bodyPositions[jt] = bodyFilters[jt].predict();
            jointObj.localPosition = bodyPositions[jt];
        }
        foreach (JointType jt in kinect.unessentialJoints)
        {
            Transform jointObj = bodyTransforms[jt];
            jointObj.localPosition = bodyPositions[jt];
        }
    }


    //I'm really sorry about this
    public void adjustBodyParts()
    {

        head.transform.position = Vector3.Slerp(head.transform.position, player_objects[JointType.Head].transform.position, Time.deltaTime * 10.0f);
        rightHand.speed = (player_objects[JointType.HandRight].transform.position - rightHand.transform.position).magnitude / Time.deltaTime;
        leftHand.speed = (player_objects[JointType.HandLeft].transform.position - leftHand.transform.position).magnitude / Time.deltaTime;
        rightHand.transform.position = Vector3.Slerp(rightHand.transform.position, player_objects[JointType.HandRight].transform.position, Time.deltaTime * 10.0f);
        leftHand.transform.position = Vector3.Slerp(leftHand.transform.position, player_objects[JointType.HandLeft].transform.position, Time.deltaTime * 10.0f);

        //Adjust body rotation
        Vector3 spine = player_objects[JointType.SpineShoulder].transform.position - player_objects[JointType.SpineMid].transform.position;
        Vector3 spine_rotation = player_objects[JointType.ShoulderLeft].transform.position - player_objects[JointType.ShoulderRight].transform.position;
        Vector3 spine_forward = Vector3.Cross(spine_rotation, spine);

        torso.transform.position = player_objects[JointType.SpineMid].transform.position;
        torso.transform.rotation = Quaternion.Slerp(torso.transform.rotation, Quaternion.LookRotation(spine_forward, spine), Time.deltaTime * 10.0f);



        rightHand.trackingConfidence = trackedBody.HandRightConfidence;
        leftHand.trackingConfidence = trackedBody.HandLeftConfidence;


        if (rightHand.trackingConfidence == TrackingConfidence.High)
        {
            if (trackedBody.HandRightState == HandState.Closed)
            {
                rightHand.closeHand();
            }
            else
            {
                rightHand.openHand();
            }
        }

        if (leftHand.trackingConfidence == TrackingConfidence.High)
        {
            if (trackedBody.HandLeftState == HandState.Closed)
            {
                leftHand.closeHand();
            }
            else
            {
                leftHand.openHand();
            }
        }
        adjustHands();

    }



    private void adjustHands()
    {
        Vector3 r_handVector = player_objects[JointType.HandTipRight].transform.position - player_objects[JointType.HandRight].transform.position;
        Vector3 l_handVector = player_objects[JointType.HandTipLeft].transform.position - player_objects[JointType.HandLeft].transform.position;
        Vector3 r_wristVector = player_objects[JointType.HandTipRight].transform.position - player_objects[JointType.WristRight].transform.position;
        Vector3 l_wristVector = player_objects[JointType.HandTipLeft].transform.position - player_objects[JointType.WristLeft].transform.position;

        Vector3 r_handRotation = player_objects[JointType.ThumbRight].transform.position - player_objects[JointType.HandRight].transform.position;
        Vector3 l_handRotation = player_objects[JointType.ThumbLeft].transform.position - player_objects[JointType.HandLeft].transform.position;

        Vector3 r_handUp = Vector3.Cross(r_handRotation, r_handVector);
        Vector3 l_handUp = Vector3.Cross(l_handVector, l_handRotation);

        if (rightHand.status == Hand.HandStatus.Close)
        {
            Quaternion target = Quaternion.LookRotation(r_wristVector);
            rightHand.transform.rotation = Quaternion.Slerp(rightHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        else
        {
            Quaternion target = Quaternion.LookRotation(r_handVector, r_handUp);
            rightHand.transform.rotation = Quaternion.Slerp(rightHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        if (leftHand.status == Hand.HandStatus.Close)
        {
            Quaternion target = Quaternion.LookRotation(l_wristVector);
            leftHand.transform.rotation = Quaternion.Slerp(leftHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        else
        {
            Quaternion target = Quaternion.LookRotation(l_handVector, l_handUp);
            leftHand.transform.rotation = Quaternion.Slerp(leftHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
    }

    public void RefreshBodyObject()
    {
        foreach (JointType jt in kinect.essentialJoints)
        {
            Windows.Kinect.Joint sourceJoint = trackedBody.Joints[jt];
            Windows.Kinect.Joint? targetJoint = null;


            if (kinect._BoneMap.ContainsKey(jt))
            {
                targetJoint = trackedBody.Joints[kinect._BoneMap[jt]];
            }

            Transform jointObj = bodyTransforms[jt];
            Vector3 pos = GetVector3FromJoint(sourceJoint);
            bodyFilters[jt].record(pos);

        }
        foreach (JointType jt in kinect.unessentialJoints)
        {
            Windows.Kinect.Joint sourceJoint = trackedBody.Joints[jt];
            Windows.Kinect.Joint? targetJoint = null;


            if (kinect._BoneMap.ContainsKey(jt))
            {
                targetJoint = trackedBody.Joints[kinect._BoneMap[jt]];
            }

            Transform jointObj = bodyTransforms[jt];
            Vector3 pos = GetVector3FromJoint(sourceJoint);
            bodyPositions[jt] = GetVector3FromJoint(sourceJoint);
        }

    }


    private Vector3 GetVector3FromJoint(Windows.Kinect.Joint joint)
    {
        return playerPositionOffset + new Vector3(-joint.Position.X * 2, joint.Position.Y * 2, joint.Position.Z * 2);
    }

    public FingerState getFingers(bool is_right_hand)
    {
        if (is_right_hand)
        {
            return new FingerState(bodyPositions[JointType.HandTipRight], bodyPositions[JointType.ThumbRight]);
        }
        else
        {
            return new FingerState(bodyPositions[JointType.HandTipLeft], bodyPositions[JointType.ThumbLeft]);

        }
    }
}

