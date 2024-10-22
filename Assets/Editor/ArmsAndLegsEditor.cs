// MyComponentEditor.cs
using UnityEngine;
using UnityEditor;

// MyComponent 클래스를 위한 커스텀 에디터를 지정
[CustomEditor(typeof(ArmsAndLegsGuidance))]

public class ArmsAndLegsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector GUI 그리기
        DrawDefaultInspector();

        // 스크립트가 연결된 객체를 가져오기
        ArmsAndLegsGuidance armsAndLegsGuidance = (ArmsAndLegsGuidance)target;

        // 버튼 추가
        if(GUILayout.Button("Start!"))
        {
            // 버튼 클릭 시 메서드 호출
            armsAndLegsGuidance.StartAnimation();
            armsAndLegsGuidance.TimestampRecording("start");
        }
    }
}

