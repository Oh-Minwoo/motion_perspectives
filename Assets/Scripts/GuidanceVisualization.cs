using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public struct ROV
{
    public bool arms;
    public bool armsAndLegs;
    public bool fullBody;
}

[Serializable]
public struct ROM
{
    public bool arms;
    public bool armsAndLegs;
}

public class GuidanceVisualization : MonoBehaviour
{
    [Header("Experiment Conditions")] 
    public string subName = "sub00";
    public ROV RangeOfVisulization;
    public ROM RangeOfMotion;
    
    [Space(10)]
    [Header("System Settings")]

    public TextAsset armsMotionCSV;

    public TextAsset armsAndLegsMotionCSV;
    
    [HideInInspector] public FullJoints fullJoints;
    [HideInInspector] public ArmsGuidance armsGuidance;
    [HideInInspector] public ArmsAndLegsGuidance armsAndLegsGuidance;

    [HideInInspector] public bool isEnabled = false;
    [HideInInspector] public bool isEnabled2 = false;
    
    
    // Start is called before the first frame update
    void Start()
    {
        fullJoints = GetComponent<FullJoints>();
        armsGuidance = GetComponent<ArmsGuidance>();
        armsAndLegsGuidance = GetComponent<ArmsAndLegsGuidance>();
        
        if (RangeOfVisulization.arms)
        {
            armsGuidance.enabled = true;
        }
        else if (RangeOfVisulization.armsAndLegs)
        {
            armsAndLegsGuidance.enabled = true;
        }
        else if (RangeOfVisulization.fullBody)
        {
            fullJoints.enabled = true;
        }

        isEnabled = true;
        isEnabled2 = true;


    }
}
