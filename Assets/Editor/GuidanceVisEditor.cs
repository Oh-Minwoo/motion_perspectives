// MyComponentEditor.cs
using UnityEngine;
using UnityEditor;

// MyComponent 클래스를 위한 커스텀 에디터를 지정
[CustomEditor(typeof(FullJoints))]
public class GuidanceVisEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector GUI 그리기
        DrawDefaultInspector();

        // 스크립트가 연결된 객체를 가져오기
        FullJoints fullJoints = (FullJoints)target;
        if(GUILayout.Button("Start!"))
        {
            fullJoints.StartAnimation(false);
        }

        if (GUILayout.Button("Demo"))
        {
            fullJoints.StartAnimation(true);
        }

        if (GUILayout.Button("Reset"))
        {
            fullJoints.ResetJoints();
        }
        
    }
    
}

