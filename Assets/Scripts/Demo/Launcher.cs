using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


class Launcher : MonoBehaviour
{
    public GameObject ball;
    public GameObject launchPoint;
    void Start()
    {
        //ball = Instantiate(ball, launchPoint.transform.position, Quaternion.identity);
        StartCoroutine(launch());
    }

    IEnumerator launch()
    {
        while (true)
        {
            GameObject newBall = Instantiate(ball, launchPoint.transform.position, Quaternion.identity);
            newBall.GetComponent<Rigidbody>().AddForce((launchPoint.transform.position - transform.position) * 5, ForceMode.VelocityChange);
            yield return new WaitForSeconds(5);
        }
    }
}

