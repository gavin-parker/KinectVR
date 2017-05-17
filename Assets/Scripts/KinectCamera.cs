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
using System.Text;

public class KinectCamera : MonoBehaviour
{
    public GameObject BodySourceManager;
    public Hand right_hand;
    public Hand left_hand;
    public GameObject torso;
    public GameObject head;
    private GameObject bodyRoot;
    private StringBuilder csv;
    private Vector3 playerPositionOffset = new Vector3(0, 0, 0);
    public KinectStartState startState;
    //private ulong player_id = 99;
    bool runningThread = true;
    private bool started = true;
    private Thread bodyThread = null;
    public enum TrackingContext { Slow, Medium, Fast };


    private Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _BodyManager;

    private Kinect.Body trackedBody;
    private GameObject trackedBodyObject;
    public Kinect.JointType[] essentialJoints;
    public Kinect.JointType[] unessentialJoints;
    public Dictionary<Kinect.JointType, Kinect.JointType> _BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
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

    private List<KinectPlayer> players = new List<KinectPlayer>();

    private void Start()
    {
        csv = new StringBuilder();
        csv.AppendLine("thumbDist, fingerDist, time");
        List<Kinect.JointType> importantJoints = essentialJoints.ToList<Kinect.JointType>();
        List<Kinect.JointType> unimportantJoints = new List<Kinect.JointType>();
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
                    GameObject newBody = new GameObject();
                    KinectPlayer newPlayer = Instantiate(Resources.Load("BlockPlayer") as GameObject, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<KinectPlayer>();
                    newPlayer.kinect = this;
                    _Bodies[body.TrackingId] = newPlayer.CreateBodyObject(body, newBody);
                    trackedBody = body;
                    trackedBodyObject = _Bodies[body.TrackingId];
                    players.Add(newPlayer);
                    bodyThread = new Thread(new ThreadStart(refreshBody));
                    bodyThread.Start();
                    StartCoroutine(waitToAlign(2));

                }

                foreach (KinectPlayer player in players)
                {
                    try
                    {
                        player.UpdateBodyObject();
                        player.adjustBodyParts();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error getting body");
                        Debug.LogError(e);
                    }

                }
                break;
            }
        }
        if (started)
        {
            try
            {
                logData();
            }
            catch (Exception e)
            {

            }

        }
    }

    IEnumerator waitToAlign(float delay)
    {
        yield return new WaitForSeconds(delay);
        alignKinect();
        Debug.Log("aligned");
    }


    void alignKinect()
    {
        if (startState.useStartState)
        {
            //warpToLocation(startState.startLocation.transform.position);
        }

    }



    void OnApplicationQuit()
    {
        File.WriteAllText("log.csv", csv.ToString());
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

            foreach (KinectPlayer player in players)
            {
                try
                {
                    player.RefreshBodyObject();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Thread closed" + e);
                    Thread.CurrentThread.Abort();
                }

            }
            Thread.Sleep(3);
        }
    }


    private void logData()
    {

        //before your loop

        //float thumbDist = Vector3.Magnitude(bodyPositions[Windows.Kinect.JointType.ThumbRight] - bodyPositions[Windows.Kinect.JointType.HandRight]);
        //float fingerDist = Vector3.Magnitude(bodyPositions[Windows.Kinect.JointType.HandTipRight] - bodyPositions[Windows.Kinect.JointType.HandRight]);
        //float time = Time.realtimeSinceStartup;



        //var newLine = thumbDist.ToString() + "," + fingerDist.ToString() + "," + time.ToString();
        //csv.AppendLine(newLine);

        //after your loop
    }

    //takes the tracking context of the hand and returns the number of continuous frames required to make a state change
    private int getTrackingFrames(bool rightHand)
    {
        return 1;
    }

}
