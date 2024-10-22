// MyComponentEditor.cs
using UnityEngine;
using UnityEditor;

// MyComponent 클래스를 위한 커스텀 에디터를 지정
[CustomEditor(typeof(GuidanceVisualization))]
public class GuidanceVisEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector GUI 그리기
        DrawDefaultInspector();

        // 스크립트가 연결된 객체를 가져오기
        GuidanceVisualization guidanceVisualization = (GuidanceVisualization)target;
        if(GUILayout.Button("Start!"))
        {
            if (guidanceVisualization.isEnabled2)
            {
                if (guidanceVisualization.fullJoints.enabled)
                {
                    guidanceVisualization.fullJoints.StartAnimation();
                    guidanceVisualization.fullJoints.TimestampRecording("start");

                }
                else if (guidanceVisualization.armsGuidance.enabled)
                {
                    guidanceVisualization.armsGuidance.StartAnimation();
                    guidanceVisualization.armsGuidance.TimestampRecording("start");
                }
                else if (guidanceVisualization.armsAndLegsGuidance.enabled)
                {
                    guidanceVisualization.armsAndLegsGuidance.StartAnimation();
                    guidanceVisualization.armsAndLegsGuidance.TimestampRecording("start");
                }
            }

        }
        
        
    }
    
}

