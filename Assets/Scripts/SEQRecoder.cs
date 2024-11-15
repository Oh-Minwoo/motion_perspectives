using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class SEQRecoder : MonoBehaviour
{
    public List<Toggle> optionButtons; // 7개의 옵션 버튼
    public Toggle submitButton;
    public Toggle exitButton;

    private int selectedOption = 0;
    // public TimestampRecoder timestampRecoder;
    public FullJoints fullJoints;
    public GameObject surveyPanel;
    public GameObject exitPanel;
    public GameObject ovrInteractionPrefab;
    
    private string csvFilePath;
    private string subName;
    private string motionName;
    private string conditionName;
    private string[] SEQArray;
    private UnixTime unixTime;
    

    void Start()
    {
        unixTime = GetComponent<UnixTime>();
        subName = fullJoints.subName;
        FilePathGenerator();
        CheckAndCreateCSV(csvFilePath);
        exitPanel.SetActive(false);
        // 각 버튼에 리스너 추가
        for(int i = 0; i < optionButtons.Count; i++)
        {
            int index = i + 1; // 1부터 7까지
            optionButtons[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    SelectOption(index);
                }
            });
        }

        // Submit 버튼 리스너 추가
        submitButton.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                SEQRecording();
            }
        });
        
        exitButton.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                PanelOff();
            }
        });

        // 초기 상태 설정
        /*UpdateButtonVisuals();*/
    }

    public void GetConditions(string motion, string condition)
    {
        motionName = motion;
        conditionName = condition;
    }
    
    private void FilePathGenerator()
    {
        string csvFileName = $"{subName}_seq.csv";
        /*csvFilePath = Path.Combine("C:\\Users\\Administrator\\Desktop\\Ohminwoo\\theis\\Assets\\SEQ_res", csvFileName);*/
        csvFilePath = Path.Combine("D:\\OMW\\Research\\Thesis\\Implementation\\motion_perspective\\motion_perspectives\\Assets\\SEQ_res", csvFileName);
    }
    
    private void CheckAndCreateCSV(string path)
    {
        if (!File.Exists(path))
        {
            // 파일 생성
            File.WriteAllText(path, "Subject Name,Motion,Condition,Score,Timestamp\n"); // 헤더 추가
            Debug.Log("CSV 파일이 생성되었습니다: " + path);
        }
        else
        {
            Debug.Log("CSV 파일이 이미 존재합니다: " + path);
        }
    }

    public void SelectOption(int option)
    {
        selectedOption = option;
        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        for(int i = 0; i < optionButtons.Count; i++)
        {
            ColorBlock cb = optionButtons[i].colors;
            if (selectedOption == i + 1)
            {
                cb.normalColor = Color.green; // 선택된 버튼 색상
            }
            else
            {
                cb.normalColor = Color.white; // 기본 버튼 색상
            }
            optionButtons[i].colors = cb;
        }
    }
    
    public void SEQRecording()
    {
        submitButton.isOn = false;
        surveyPanel.SetActive(false);
        exitPanel.SetActive(true);
        
        
        SEQArray = new string[5];
        unixTime = GetComponent<UnixTime>();
        string unityTs = unixTime.GetCurrentUnixTime();
        SEQArray[0] = subName;
        SEQArray[1] = motionName;
        SEQArray[2] = conditionName;
        SEQArray[3] = selectedOption.ToString();
        SEQArray[4] = unityTs;
        
        WriteToCSV(csvFilePath, SEQArray);
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
    
    private void PanelOff()
    {
        foreach (var button in optionButtons)
        {
                button.isOn = false;
        }
        submitButton.isOn = false;
        exitButton.isOn = false;
        
        exitPanel.SetActive(false);
        ovrInteractionPrefab.SetActive(false);
    }
}
