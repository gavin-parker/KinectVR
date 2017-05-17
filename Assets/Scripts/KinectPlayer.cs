using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VR;
using Windows.Kinect;
using NUnit.Framework.Constraints;
using System.Collections;

public class KinectPlayer : MonoBehaviour
{
    public GameObject Head;
    public Hand RightHand;
    public Hand LeftHand;
    public GameObject Torso;
    public KinectCamera Kinect;
    public ulong Id;
    public bool IsMainPlayer;

    [HideInInspector]
    public Dictionary<JointType, Transform> BodyTransforms = new Dictionary<JointType, Transform>();
    public Dictionary<JointType, KalmanFilter> BodyFilters = new Dictionary<JointType, KalmanFilter>();
    public Dictionary<JointType, Vector3> BodyPositions = new Dictionary<JointType, Vector3>();
    //holds all the hand joint objects - palm, wrist, thumb, tip
    public Dictionary<JointType, GameObject> PlayerObjects = new Dictionary<JointType, GameObject>();

    private HandFilter _rightHandFilter = new HandFilter(3);
    private HandFilter _leftHandFilter = new HandFilter(3);

    private Windows.Kinect.Body _trackedBody;
    private Vector3 _playerPositionOffset = new Vector3(0, 0, 0);

    //Warps the player to a given location ( the location should be at desired eye level)
    public void WarpToLocation(Vector3 target)
    {
        Vector3 actualHeadLocation = BodyPositions[JointType.Head];
        Vector3 difference = target - actualHeadLocation;
        _playerPositionOffset = difference;

    }

    public GameObject GetBodyPart(JointType jt)
    {
        return BodyTransforms[jt].gameObject;
    }

    public GameObject CreateBodyObject(Body kinectBody, GameObject newBody)
    {
        ulong id = kinectBody.TrackingId;
        _trackedBody = kinectBody;
        this.Id = id;
        for (JointType jt = JointType.SpineBase; jt <= JointType.ThumbRight; jt++)
        {
            GameObject jointObj = new GameObject();
            //jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            jointObj.transform.localScale = new Vector3(5f, 5f, 5f);
            jointObj.name = jt.ToString();
            jointObj.transform.parent = newBody.transform;
            this.BodyTransforms.Add(jt, jointObj.transform);
            this.BodyPositions.Add(jt, GetVector3FromJoint(kinectBody.Joints[jt]));
            this.BodyFilters.Add(jt, new KalmanFilter());
            this.PlayerObjects.Add(jt, jointObj);
        }

        RightHand.init(this, Kinect);
        LeftHand.init(this, Kinect);
        if (IsMainPlayer)
        {
            StartCoroutine(AlignTracking());
        }
        else
        {
            Head.GetComponentInChildren<Camera>().tag = "Untagged";
        }
        return newBody;
    }

    private IEnumerator AlignTracking()
    {
        yield return new WaitForSeconds(3);
        InputTracking.Recenter();
        Head.GetComponentInChildren<Camera>().tag = "MainCamera";


    }


    public void makeMainPlayer(bool mainPlayer)
    {
        if (mainPlayer)
        {
            IsMainPlayer = true;
            Head.GetComponentInChildren<Camera>().tag = "MainCamera";
        }
        else
        {
            IsMainPlayer = false;
            Head.GetComponentInChildren<Camera>().tag = "Untagged";
        }
    }

    public bool IsTracked()
    {
        return _trackedBody.IsTracked;
    }
    public void UpdateBodyObject()
    {
        foreach (JointType jt in Kinect.EssentialJoints)
        {
            Transform jointObj = BodyTransforms[jt];
            BodyPositions[jt] = BodyFilters[jt].predict();
            jointObj.localPosition = BodyPositions[jt];
        }
        foreach (JointType jt in Kinect.UnessentialJoints)
        {
            Transform jointObj = BodyTransforms[jt];
            if (jointObj != null)
            {
                jointObj.localPosition = BodyPositions[jt];
            }
        }
    }


    //I'm really sorry about this
    public void AdjustBodyParts()
    {

        Head.transform.position = Vector3.Slerp(Head.transform.position, PlayerObjects[JointType.Head].transform.position, Time.deltaTime * 10.0f);
        RightHand.speed = (PlayerObjects[JointType.HandRight].transform.position - RightHand.transform.position).magnitude / Time.deltaTime;
        LeftHand.speed = (PlayerObjects[JointType.HandLeft].transform.position - LeftHand.transform.position).magnitude / Time.deltaTime;
        RightHand.transform.position = Vector3.Slerp(RightHand.transform.position, PlayerObjects[JointType.HandRight].transform.position, Time.deltaTime * 10.0f);
        LeftHand.transform.position = Vector3.Slerp(LeftHand.transform.position, PlayerObjects[JointType.HandLeft].transform.position, Time.deltaTime * 10.0f);

        //Adjust body rotation
        Vector3 spine = PlayerObjects[JointType.SpineShoulder].transform.position - PlayerObjects[JointType.SpineMid].transform.position;
        Vector3 spine_rotation = PlayerObjects[JointType.ShoulderLeft].transform.position - PlayerObjects[JointType.ShoulderRight].transform.position;
        Vector3 spine_forward = Vector3.Cross(spine_rotation, spine);

        Torso.transform.position = PlayerObjects[JointType.SpineMid].transform.position;
        Torso.transform.rotation = Quaternion.Slerp(Torso.transform.rotation, Quaternion.LookRotation(spine_forward, spine), Time.deltaTime * 10.0f);



        RightHand.trackingConfidence = _trackedBody.HandRightConfidence;
        LeftHand.trackingConfidence = _trackedBody.HandLeftConfidence;


        if (RightHand.trackingConfidence == TrackingConfidence.High)
        {
            if (_trackedBody.HandRightState == HandState.Closed)
            {
                RightHand.CloseHand();
            }
            else
            {
                RightHand.OpenHand();
            }
        }
        else
        {
            if (_rightHandFilter.Predict())

            {
                RightHand.CloseHand();
            }
            else
            {
                RightHand.OpenHand();
            }

        }


        if (LeftHand.trackingConfidence == TrackingConfidence.High)
        {
            if (_trackedBody.HandLeftState == HandState.Closed)
            {
                LeftHand.CloseHand();
            }
            else
            {
                LeftHand.OpenHand();
            }
        }
        else
        {
            if (_leftHandFilter.Predict())
            {
                LeftHand.CloseHand();
            }
            else
            {
                LeftHand.OpenHand();
            }
        }
        AdjustHands();
    }

    private void AdjustHands()
    {
        Vector3 rHandVector = PlayerObjects[JointType.HandTipRight].transform.position - PlayerObjects[JointType.HandRight].transform.position;
        Vector3 lHandVector = PlayerObjects[JointType.HandTipLeft].transform.position - PlayerObjects[JointType.HandLeft].transform.position;
        Vector3 rWristVector = PlayerObjects[JointType.HandTipRight].transform.position - PlayerObjects[JointType.WristRight].transform.position;
        Vector3 lWristVector = PlayerObjects[JointType.HandTipLeft].transform.position - PlayerObjects[JointType.WristLeft].transform.position;

        Vector3 rHandRotation = PlayerObjects[JointType.ThumbRight].transform.position - PlayerObjects[JointType.HandRight].transform.position;
        Vector3 lHandRotation = PlayerObjects[JointType.ThumbLeft].transform.position - PlayerObjects[JointType.HandLeft].transform.position;

        Vector3 rHandUp = Vector3.Cross(rHandRotation, rHandVector);
        Vector3 lHandUp = Vector3.Cross(lHandVector, lHandRotation);

        if (RightHand.status == Hand.HandStatus.Close)
        {
            Quaternion target = Quaternion.LookRotation(rWristVector);
            RightHand.transform.rotation = Quaternion.Slerp(RightHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        else
        {
            Quaternion target = Quaternion.LookRotation(rHandVector, rHandUp);
            RightHand.transform.rotation = Quaternion.Slerp(RightHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        if (LeftHand.status == Hand.HandStatus.Close)
        {
            Quaternion target = Quaternion.LookRotation(lWristVector);
            LeftHand.transform.rotation = Quaternion.Slerp(LeftHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
        else
        {
            Quaternion target = Quaternion.LookRotation(lHandVector, lHandUp);
            LeftHand.transform.rotation = Quaternion.Slerp(LeftHand.transform.rotation, target, Time.deltaTime * 10.0f);
        }
    }

    public void RefreshBodyObject()
    {
        foreach (JointType jt in Kinect.EssentialJoints)
        {
            Windows.Kinect.Joint sourceJoint = _trackedBody.Joints[jt];
            Windows.Kinect.Joint? targetJoint = null;


            if (Kinect.BoneMap.ContainsKey(jt))
            {
                targetJoint = _trackedBody.Joints[Kinect.BoneMap[jt]];
            }

            Transform jointObj = BodyTransforms[jt];
            Vector3 pos = GetVector3FromJoint(sourceJoint);
            BodyFilters[jt].record(pos);

        }
        foreach (JointType jt in Kinect.UnessentialJoints)
        {
            Windows.Kinect.Joint sourceJoint = _trackedBody.Joints[jt];
            Transform jointObj = BodyTransforms[jt];
            Vector3 pos = GetVector3FromJoint(sourceJoint);
            BodyPositions[jt] = GetVector3FromJoint(sourceJoint);
        }
        _rightHandFilter.Record(_trackedBody.HandRightState == HandState.Closed);
        _leftHandFilter.Record(_trackedBody.HandLeftState == HandState.Closed);

    }


    private Vector3 GetVector3FromJoint(Windows.Kinect.Joint joint)
    {
        return _playerPositionOffset + new Vector3(-joint.Position.X * 2, joint.Position.Y * 2, joint.Position.Z * 2);
    }

    public FingerState GetFingers(bool isRightHand)
    {
        return isRightHand ? new FingerState(BodyPositions[JointType.HandTipRight], BodyPositions[JointType.ThumbRight]) : new FingerState(BodyPositions[JointType.HandTipLeft], BodyPositions[JointType.ThumbLeft]);
    }
}

