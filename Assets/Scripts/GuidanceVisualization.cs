using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

[Serializable]
public struct ROV
{
    public bool arms;
    public bool armsAndLegs;
    public bool fullBody;
}

[Serializable]
public struct TOM
{
    public bool badminton;
    public bool taichi;
}

[Serializable]
public struct UpdateMethods
{
    public bool interactive;
    public bool autonomous;
}


public class GuidanceVisualization : MonoBehaviour
{
    [Header("Experiment Conditions")] 
    public string subName = "sub00";
    public ROV RangeOfVisulization;
    public TOM TypeOfMotion;
    public UpdateMethods updateMethods;
    
    [Space(10)]
    [Header("System Settings")]

    public TextAsset badmintonCSV;

    public TextAsset taichiCSV;

    [Header("Select playback speed")]
    public float speed = 0.5f;

    [HideInInspector] public float frameRate = 60f;
    [HideInInspector] public FullJoints fullJoints;
    [HideInInspector] public ArmsGuidance armsGuidance;
    [HideInInspector] public ArmsAndLegsGuidance armsAndLegsGuidance;

    [HideInInspector] public bool isEnabled = false;
    [HideInInspector] public bool isEnabled2 = false;
    [HideInInspector] public bool isMotionDone = false;
    
    private UnixTime unixTime;
    private string[] timestampRecorder = new string[4];
    private string motionName;
    private string conditionName;
    private string csvFilePath;
    
    
    // Start is called before the first frame update
    void Start()
    {
        fullJoints = GetComponent<FullJoints>();
        armsGuidance = GetComponent<ArmsGuidance>();
        armsAndLegsGuidance = GetComponent<ArmsAndLegsGuidance>();
        
        MotionNameGenerator();
        ConditionNameGenerator();
        FilePathGenerator();

        // 파일 확인 및 생성
        CheckAndCreateCSV(csvFilePath);
        
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

    private void FilePathGenerator()
    {
        string csvFileName = $"{subName}_timestamp.csv";
        csvFilePath = Path.Combine("C:\\Users\\HCIS\\Desktop\\OhMinwoo_Thesis\\theis\\Assets\\Timestamp_res", csvFileName);
        // csvFilePath = Path.Combine("D:\\OMW\\Research\\Thesis\\Implementation\\PNS_To_Unity_live-master\\Assets\\Timestamp_res", csvFileName);
    }
    
    private void CheckAndCreateCSV(string path)
    {
        if (!File.Exists(path))
        {
            // 파일 생성
            File.WriteAllText(path, "Subject Name, Motion, Condition, Action, Unix Time\n"); // 헤더 추가
            Debug.Log("CSV 파일이 생성되었습니다: " + path);
        }
        else
        {
            Debug.Log("CSV 파일이 이미 존재합니다: " + path);
        }
    }

    private void MotionNameGenerator()
    {
        if (TypeOfMotion.badminton & !TypeOfMotion.taichi)
        {
            motionName = "badminton";
            frameRate = 60f;
        }
        else if (TypeOfMotion.taichi & !TypeOfMotion.badminton)
        {
            motionName = "taichi";
            frameRate = 125f;
        }
    }
    private void ConditionNameGenerator()
    {
        if (RangeOfVisulization.arms)
        {
            conditionName = "Arms";
        }
        else if (RangeOfVisulization.armsAndLegs)
        {
            conditionName = "Arms and Legs";
        }
        else
        {
            conditionName = "Full Body";
        }
    }
    
    public void TimestampRecording(string startOrEnd, float ts)
    {
        timestampRecorder = new string[5];
        unixTime = GetComponent<UnixTime>();
        string unity_ts = unixTime.GetCurrentUnixTime();
        timestampRecorder[0] = subName;
        timestampRecorder[1] = motionName;
        timestampRecorder[2] = conditionName;
        timestampRecorder[3] = startOrEnd;
        timestampRecorder[4] = unity_ts;
        
        Debug.Log(timestampRecorder.ToString());
        WriteToCSV(csvFilePath, timestampRecorder);
    }
    
    private void WriteToCSV(string path, string[] data)
    {
        // 데이터를 쉼표로 구분하여 한 줄로 만듭니다.
        string newLine = string.Join(",", data);

        // 파일에 추가 모드로 쓰기
        using (StreamWriter sw = new StreamWriter(path, append: true))
        {
            sw.WriteLine(newLine);
        }

        Debug.Log("CSV 파일에 데이터가 추가되었습니다: " + csvFilePath);
    }
}
