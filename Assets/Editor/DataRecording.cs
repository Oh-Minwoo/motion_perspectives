using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class LogWindow : EditorWindow
{
    private List<string> logMessages = new List<string>();
    private Vector2 scrollPos;

    [MenuItem("Window/Log Window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(LogWindow));
    }

    private void OnGUI()
    {
        GUILayout.Label("Log Window", EditorStyles.boldLabel);

        // 로그 추가 버튼
        if (GUILayout.Button("Add Log Message"))
        {
            AddLogMessage("This is a new log message.");
        }

        // 로그를 표시할 박스
        GUILayout.BeginVertical("Box");
        GUILayout.Label("Logs:", EditorStyles.boldLabel);

        // 스크롤 가능한 로그 박스
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        foreach (string log in logMessages)
        {
            GUILayout.Label(log);
        }
        GUILayout.EndScrollView();
        
        GUILayout.EndVertical();
    }

    private void AddLogMessage(string message)
    {
        logMessages.Add(message);
        Repaint(); // GUI를 갱신하여 최신 로그가 보이도록 함
    }
}