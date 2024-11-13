using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class SEQRecoder : MonoBehaviour
{
    public List<Button> optionButtons; // 7개의 옵션 버튼
    public Button submitButton;

    private int selectedOption = 0;
    // public TimestampRecoder timestampRecoder;
    public FullJoints fullJoints;
    
    private string csvFilePath;
    private string subName;
    private string motionName;
    private string conditionName;
    private string[] SEQArray;
    public UnixTime unixTime;
    

    void Start()
    {
        subName = fullJoints.subName;
        FilePathGenerator();
        CheckAndCreateCSV(csvFilePath);
        // 각 버튼에 리스너 추가
        for(int i = 0; i < optionButtons.Count; i++)
        {
            int index = i + 1; // 1부터 7까지
            optionButtons[i].onClick.AddListener(() => SelectOption(index));
        }

        // Submit 버튼 리스너 추가
        submitButton.onClick.AddListener(SubmitAnswer);

        // 초기 상태 설정
        UpdateButtonVisuals();
    }
    
    private void FilePathGenerator()
    {
        string csvFileName = $"{subName}_seq.csv";
        csvFilePath = Path.Combine("C:\\Users\\Administrator\\Desktop\\Ohminwoo\\theis\\Assets\\SEQ_res", csvFileName);
        // csvFilePath = Path.Combine("D:\\OMW\\Research\\Thesis\\Implementation\\PNS_To_Unity_live-master\\Assets\\Timestamp_res", csvFileName);
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

    private void SubmitAnswer()
    {
        if(selectedOption == 0)
        {
            Debug.Log("옵션을 선택해주세요.");
            return;
        }

        // 답변 저장 (예: PlayerPrefs 사용)
        PlayerPrefs.SetInt("LikertAnswer", selectedOption);
        PlayerPrefs.Save();

        Debug.Log("답변 제출됨: " + selectedOption);

        // 추가 동작 (예: 다음 질문으로 이동, UI 비활성화 등)
    }
    
    public void SEQRecording()
    {
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
}
