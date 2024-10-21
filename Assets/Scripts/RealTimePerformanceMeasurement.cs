using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
using System.Linq;


[Serializable]
public struct NamedBools
{
    public bool arms;
    public bool armsAndLegs;
    public bool fullBody;
}

public class RealTimePerformanceMeasurement : MonoBehaviour
{
    public NamedBools conditionSelector;
    public GameObject guidanceObject;
    [SerializeField] private float threshold = 0.1f;
    
    private MonoBehaviour activeScript;
    
    // public string[] conditionSelector = new string[] { "Arms", "Arms And Legs", "Full-body" };
    /*private AbstractGuidance abstractGuidance;*/
    private Vector3[] guidanceJointPos;
    private List<Vector3> currentJoint = new List<Vector3>();
    private int[] engagedJointList;
    
    void Start()
    {
        MonoBehaviour[] scripts = guidanceObject.GetComponents<MonoBehaviour>();
        activeScript = scripts.FirstOrDefault(script => script.enabled);
        if (activeScript is ArmsGuidance)
        {
            conditionSelector.arms = true;
            guidanceJointPos = new Vector3[6];
            engagedJointList = new int[] { 14, 15, 16, 18, 19, 20 };
        }
        if (activeScript is ArmsAndLegsGuidance)
        {
            conditionSelector.armsAndLegs = true;
            guidanceJointPos = new Vector3[12];
            engagedJointList = new int[] { 1, 2, 3, 4, 5, 6, 14, 15, 16, 18, 19, 20 };
        }

        if (activeScript is FullJoints)
        {
            conditionSelector.fullBody = true;
            guidanceJointPos = new Vector3[21];
            engagedJointList = Enumerable.Range(0, 21).ToArray();
        }
    }

    public void GetGuidanceData(GameObject[] currentGuidance)
    {
        for (int i = 0; i < guidanceJointPos.Length; i++)
        {
            guidanceJointPos[i] = currentGuidance[i].transform.position;
        }
    }

    public void GetJointPos(GameObject[] jointObjects)
    {
        currentJoint = new List<Vector3>();
        foreach (int i in engagedJointList)
        {
            currentJoint.Add(jointObjects[i].transform.position);
        }
 
        
        float distance = JointDistance();
        Debug.Log("distance: " + distance);
        if (distance < threshold)
        {
            if (conditionSelector.arms)
            {
                ArmsGuidance armsGuidance = (ArmsGuidance)activeScript;
                armsGuidance.StartAnimation();
            }
            if (conditionSelector.armsAndLegs)
            {
                ArmsAndLegsGuidance armsAndLegsGuidance = (ArmsAndLegsGuidance)activeScript;
                armsAndLegsGuidance.StartAnimation(); 
            }

            if (conditionSelector.fullBody)
            {
                FullJoints fullJoints = (FullJoints)activeScript;
                fullJoints.StartAnimation();
            }
        }
    }

    private float JointDistance()
    {
        float distance = 0;
        for (int i = 0; i < 12; i++)
        {
            distance += Vector3.Distance(currentJoint[i], guidanceJointPos[i]);
        }

        return distance;
    }
    
}
