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
    private GameObject _bodyRoot;
    public KinectStartState StartState;
    //private ulong player_id = 99;
    bool _runningThread = true;
    private bool _started = true;
    private Thread _bodyThread = null;
    public enum TrackingContext { Slow, Medium, Fast };


    private readonly Dictionary<ulong, GameObject> _Bodies = new Dictionary<ulong, GameObject>();
    private BodySourceManager _bodyManager;

    public Kinect.JointType[] EssentialJoints;
    public Kinect.JointType[] UnessentialJoints;
    public Dictionary<Kinect.JointType, Kinect.JointType> BoneMap = new Dictionary<Kinect.JointType, Kinect.JointType>()
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

    private readonly List<KinectPlayer> players = new List<KinectPlayer>();

    public Dictionary<ulong, GameObject> Bodies
    {
        get
        {
            return Bodies1;
        }
    }

    public Dictionary<ulong, GameObject> Bodies1
    {
        get
        {
            return _Bodies;
        }
    }

    private void Start()
    {
        List<Kinect.JointType> importantJoints = EssentialJoints.ToList<Kinect.JointType>();
        List<Kinect.JointType> unimportantJoints = new List<Kinect.JointType>();
        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            if (!importantJoints.Contains(jt))
            {
                unimportantJoints.Add(jt);
            }
        }
        UnessentialJoints = unimportantJoints.ToArray<Kinect.JointType>();
    }
    void Update()
    {
        if (BodySourceManager == null)
        {
            return;
        }

        _bodyManager = BodySourceManager.GetComponent<BodySourceManager>();
        if (_bodyManager == null)
        {
            return;
        }

        Kinect.Body[] data = _bodyManager.GetData();
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

        List<ulong> knownIds = new List<ulong>(Bodies.Keys);

        // First delete untracked bodies
        foreach (ulong trackingId in knownIds)
        {
            if (!trackedIds.Contains(trackingId))
            {
                Destroy(Bodies[trackingId]);
                Bodies.Remove(trackingId);
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
                if (!Bodies.ContainsKey(body.TrackingId))
                {
                    GameObject newBody = new GameObject();
                    KinectPlayer newPlayer = Instantiate(Resources.Load("BlockPlayer") as GameObject, new Vector3(0, 0, 0), Quaternion.identity).GetComponent<KinectPlayer>();
                    newPlayer.Kinect = this;
                    if (players.Count == 0)
                    {
                        newPlayer.makeMainPlayer(true);
                    }
                    Bodies[body.TrackingId] = newPlayer.CreateBodyObject(body, newBody);
                    newPlayer.WarpToLocation(new Vector3(0, 1.5f, 0));
                    players.Add(newPlayer);

                    _bodyThread = new Thread(new ThreadStart(RefreshBody));
                    _bodyThread.Start();

                }

                foreach (KinectPlayer player in players)
                {
                    try
                    {
                        if (player.IsTracked())
                        {
                            player.gameObject.SetActive(true);
                            player.UpdateBodyObject();
                            player.AdjustBodyParts();
                        }
                        else
                        {
                            player.gameObject.SetActive(false);
                        }
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
    }

    void OnApplicationQuit()
    {
        _runningThread = false;
        if (_bodyThread != null)
        {
            try
            {
                _bodyThread.Join();
            }
            catch (Exception e)
            {
                _bodyThread.Abort();

            }
        }
    }
    void OnDestroyed()
    {
        _runningThread = false;
        if (_bodyThread == null) return;
        try
        {
            _bodyThread.Join();
        }
        catch (Exception e)
        {
            _bodyThread.Abort();

        }
    }

    private void RefreshBody()
    {
        while (_runningThread)
        {

            foreach (KinectPlayer player in players)
            {
                try
                {
                    if (player.IsTracked())
                    {
                        player.RefreshBodyObject();
                    }
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


}
