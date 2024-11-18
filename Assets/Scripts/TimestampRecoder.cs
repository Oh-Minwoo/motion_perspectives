using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
using System.IO;
using System.Linq;


public class TimestampRecoder : MonoBehaviour
{
    private FullJoints fullJoints;
    [HideInInspector] public float frameRate;
    
    private UnixTime unixTime;
    private string subName;
    private string[] timestampRecorder = new string[4];
    private string motionName;
    private string conditionName;
    private string csvFilePath;
    public SEQRecoder seqRecoder;
    
    
    // Start is called before the first frame update
    void Start()
    {
        fullJoints = GetComponent<FullJoints>();
        subName = fullJoints.subName;
        
        MotionNameGenerator();
        ConditionNameGenerator();
        FilePathGenerator();
        seqRecoder.GetConditions(motionName, conditionName);
        

        // 파일 확인 및 생성
        CheckAndCreateCSV(csvFilePath);
    }

    public void ResetConditions()
    {
        Debug.Log("Reset start!");
        MotionNameGenerator();
        ConditionNameGenerator();
        seqRecoder.GetConditions(motionName, conditionName);
    }
    

    private void FilePathGenerator()
    {
        string csvFileName = $"{subName}_timestamp.csv";
        csvFilePath = Path.Combine("C:\\Users\\Administrator\\Desktop\\Ohminwoo\\theis\\Assets\\Timestamp_res", csvFileName);
        // csvFilePath = Path.Combine("D:\\OMW\\Research\\Thesis\\Implementation\\motion_perspective\\motion_perspectives\\Assets\\Timestamp_res", csvFileName);
    }
    
    private void CheckAndCreateCSV(string path)
    {
        if (!File.Exists(path))
        {
            // 파일 생성
            File.WriteAllText(path, "Subject Name,Motion,Condition,Action,Unix Time\n"); // 헤더 추가
            Debug.Log("CSV 파일이 생성되었습니다: " + path);
        }
        else
        {
            Debug.Log("CSV 파일이 이미 존재합니다: " + path);
        }
    }

    private void MotionNameGenerator()
    {
        if (fullJoints.motions.frontal)
        {
            motionName = "frontal";
            frameRate = 96f;
        }
        else if (fullJoints.motions.peripheral)
        {
            motionName = "peripheral";
            frameRate = 96f;
        }
        else if (fullJoints.motions.taichi)
        {
            motionName = "taichi";
            frameRate = 125f;
        }
    }
    private void ConditionNameGenerator()
    {
        if (fullJoints.perspectives.firstPerson)
        {
            conditionName = "1PP";
        }
        else if (fullJoints.perspectives.thirdPerson)
        {
            conditionName = "3PP";
        }
        else if (fullJoints.perspectives.mirror)
        {
            conditionName = "Mirror";
        }
        else if (fullJoints.perspectives.multiView)
        {
            conditionName = "Multi View";
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
