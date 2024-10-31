using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
using System.Linq;


[Serializable]

public class RealTimePerformanceMeasurement : MonoBehaviour
{
    public GuidanceVisualization guidanceVisualization;
    public GameObject guidanceObject;
    [SerializeField] private float threshold = 0.1f;
    [SerializeField] private float duration = 0.1f;

    
    private MonoBehaviour activeScript;
    
    // public string[] conditionSelector = new string[] { "Arms", "Arms And Legs", "Full-body" };
    /*private AbstractGuidance abstractGuidance;*/
    private Vector3[] guidanceJointPos;
    private float timestamp;
    private List<Vector3> currentJoint = new List<Vector3>();
    private int[] engagedJointList;
    
    void Update()
    {
        if (guidanceVisualization.isEnabled)
        {
            if (guidanceVisualization.RangeOfVisulization.arms)
            {
                guidanceJointPos = new Vector3[8];
                engagedJointList = new int[] { 2, 3, 5, 6, 15, 16, 19, 20 };
            }
            if (guidanceVisualization.RangeOfVisulization.armsAndLegs)
            {
                guidanceJointPos = new Vector3[12];
                engagedJointList = new int[] { 1, 2, 3, 4, 5, 6, 14, 15, 16, 18, 19, 20 };
            }

            if (guidanceVisualization.RangeOfVisulization.fullBody)
            {
                guidanceJointPos = new Vector3[21];
                engagedJointList = Enumerable.Range(0, 21).ToArray();
            }
            guidanceVisualization.isEnabled = false;
            print("guidanceJointPos: " + guidanceJointPos.Length);
        }
    }

    public void GetGuidanceData(GameObject[] currentGuidance)
    {
        for (int i = 0; i < guidanceJointPos.Length; i++)
        {
            guidanceJointPos[i] = currentGuidance[i].transform.position;
        }
    }

    public void GetJointPos(GameObject[] jointObjects, float ts)
    {
        currentJoint = new List<Vector3>();
        foreach (int i in engagedJointList)
        {
            currentJoint.Add(jointObjects[i].transform.position);
        }
        
        float distance = RelativeJointDistance();
        if (distance < threshold && !guidanceVisualization.isMotionDone && guidanceVisualization.updateMethods.interactive)
        {
            Debug.Log("distance: " + distance);
            if (guidanceVisualization.RangeOfVisulization.arms)
            {
                ArmsGuidance armsGuidance = guidanceVisualization.armsGuidance;
                armsGuidance.UpdateAnimation();
                
            }
            if (guidanceVisualization.RangeOfVisulization.armsAndLegs)
            {
                ArmsAndLegsGuidance armsAndLegsGuidance = guidanceVisualization.armsAndLegsGuidance;
                armsAndLegsGuidance.UpdateAnimation(); 
            }

            if (guidanceVisualization.RangeOfVisulization.fullBody)
            {
                FullJoints fullJoints = guidanceVisualization.fullJoints;;
                fullJoints.UpdateAnimation();
            }
            StartCoroutine(ChangeVariableRoutine(0));
        }
    }

    private float AbsoluteJointDistance()
    {
        float distance = 0;
        for (int i = 0; i < guidanceJointPos.Length; i++)
        {
            distance += Vector3.Distance(currentJoint[i], guidanceJointPos[i]);
        }

        return distance / guidanceJointPos.Length;
    }

    private float RelativeJointDistance()
    {
        Vector3[] NormalizedGuidance = Normalization(guidanceJointPos);
        Vector3[] NormalizedSubject = Normalization(currentJoint.ToArray());
        float distance = 0;
        for (int i = 0; i < NormalizedGuidance.Length; i++)
        {
            distance += Vector3.Distance(NormalizedSubject[i], NormalizedGuidance[i]);
        }

        return distance / NormalizedGuidance.Length; 
    }
    
    private Vector3[] Normalization(Vector3[] positions)
    {
        Vector3[] normalizedPosition = new Vector3[positions.Length];
        Vector3 rootCoord = positions[0];

        for (int i = 0; i < positions.Length; i++)
        {
            normalizedPosition[i] = (positions[i] - rootCoord);
        }
        return normalizedPosition;
    }
    
    private IEnumerator ChangeVariableRoutine(float newThreshold)
    {
        float originalThreshold = threshold;
        threshold = newThreshold;
        
        yield return new WaitForSeconds(duration);
        
        threshold = originalThreshold;
    }
    
}
