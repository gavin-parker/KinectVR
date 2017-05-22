using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


class Launcher : MonoBehaviour
{
    public GameObject Ball;
    public GameObject LaunchPoint;
    void Start()
    {
        //ball = Instantiate(ball, launchPoint.transform.position, Quaternion.identity);
        StartCoroutine(Launch());
    }

    IEnumerator Launch()
    {
        while (true)
        {
            GameObject newBall = Instantiate(Ball, LaunchPoint.transform.position, Quaternion.identity);
            newBall.GetComponent<Rigidbody>().AddForce((LaunchPoint.transform.position - transform.position) * 5, ForceMode.VelocityChange);
            yield return new WaitForSeconds(5);
        }
    }
}

