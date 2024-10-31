// MyComponentEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections;

// MyComponent 클래스를 위한 커스텀 에디터를 지정
[CustomEditor(typeof(Calibration))]
public class ScalingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector GUI 그리기
        DrawDefaultInspector();

        // 스크립트가 연결된 객체를 가져오기
        Calibration calibration = (Calibration)target;
        if(GUILayout.Button("Calibration start"))
        {
            calibration.StartCalibration();
        }
    }
    
}