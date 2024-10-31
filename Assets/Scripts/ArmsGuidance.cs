using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ArmsGuidance : MonoBehaviour
{
    private GuidanceVisualization guidanceVisualization;
    public SocketListener socketListener;
    private bool isBadminton = false;
    private bool isDemo;
    private TextAsset badmintonCSV;
    private TextAsset taichiCSV;
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private List<GameObject[]> allJoints = new List<GameObject[]>();
    [HideInInspector] public GameObject[] currentGuidance;
    private List<List<LineRenderer[]>> allLines = new List<List<LineRenderer[]>>(); // LineRenderer 참조를 저장할 배열
    private int[] engagedJointList;
    private int[] engagedLineList;
    
    public RealTimePerformanceMeasurement realTimePerformanceMeasurement;

    public TMP_Text textUI;
    private int frameCount = 1;
    private float[] transperancyArray;
    public Vector3 positionOffset = new Vector3(-100f, 1f, 0f);
    public Color color;

    [SerializeField] private float percentage = 0.3f; // 0~1 사이의 값
    [SerializeField] private int numOfGuidanceOnScreen = 5; // Continuous guidance에서 한 화면에 보여질 guidance 개수
    // [SerializeField] private bool isDiscrete = true;
    // [SerializeField] private float transparency = 0.5f;

    private int dataIdx;

    [HideInInspector]
    public int dataLength;

    // Joint hierarchy mapping example
    private int[,] rightLegHierarchy =
    {
        { 0, 1 }, // RightLeg to RightFoot
    };
    
    private int[,] leftLegHierarchy =
    {
        {2, 3}, // LeftLeg to LeftFoot
    };
    
    private int[,] rightArmHierarchy =
    {
        {4, 5}, // RightForeArm to RightHand
    };

    private int[,] leftArmHierarchy =
    {
        {6, 7}, // LeftForeArm to LeftHand
    };

    // private int[,] headHierarchy =
    // {
    //     {8, 9 } //Neck1 to Head
    // };

    private int[][,] jointHierarchy;
    

    private Coroutine animationCoroutine;

    void Start()
    {
        guidanceVisualization = GetComponent<GuidanceVisualization>();
        isBadminton = guidanceVisualization.TypeOfMotion.badminton;
        badmintonCSV = guidanceVisualization.badmintonCSV;
        taichiCSV = guidanceVisualization.taichiCSV;

        // if (isDiscrete)
        // {
        //     numOfGuidanceOnScreen = 2;
        // }
        engagedJointList = new int[] { 2, 3, 5, 6, 15, 16, 19, 20 };
        jointHierarchy = new int[][,]
        {
            rightLegHierarchy,
            leftLegHierarchy,
            rightArmHierarchy,
            leftArmHierarchy
        };
        if (isBadminton)
        {
            ReadCSV(engagedJointList, badmintonCSV);
        }
        else
        {
            ReadCSV(engagedJointList, taichiCSV);
        }
        // transperancyArray = CalculateTransparency(numOfGuidanceOnScreen, 0.8f, 0.2f);
        // Debug.Log(transperancyArray);
        
        // for (int i = 0; i < numOfGuidanceOnScreen; i++)
        // {
        //     CreateJointsAndConnections(i);
        // }
        //
        // MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
        // MakeObjectsTransparent(allJoints[1], allLines[1], 0.1f);
        //
        // string str = $"{frameCount}/{jointPositions.Count}";
        // frameLeft.text = str;
        //
        // currentGuidance = allJoints[0];
        // realTimePerformanceMeasurement.GetGuidanceData(currentGuidance);
        //
        // dataIdx = numOfGuidanceOnScreen;
        
    }
    
    public void StartAnimation(bool isDemonstration)
    {
        isDemo = isDemonstration;
        for (int i = 0; i < numOfGuidanceOnScreen; i++)
        {
            CreateJointsAndConnections(i);
        }

        currentGuidance = allJoints[0];
        realTimePerformanceMeasurement.GetGuidanceData(currentGuidance);

        dataIdx = numOfGuidanceOnScreen;
        
        socketListener.isGuidanceStart = true;
        StartCoroutine(Countdown(3f));
    }
    
    IEnumerator Countdown(float countdownTime)
    {
        float remainingTime = countdownTime;

        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= countdownTime & remainingTime > 2)
            {
                string count = $"3";
                textUI.text = count;
            }
            else if (remainingTime <= 2 & remainingTime > 1)
            {
                string count = $"2";
                textUI.text = count;
            }
            else if (remainingTime <= 1 & remainingTime > 0)
            {
                string count = $"1";
                textUI.text = count;
            }
            yield return null; // 다음 프레임까지 대기
        }
        if (guidanceVisualization.updateMethods.autonomous)
        {
            string count = $"Start!";
            textUI.text = count;
            StartCoroutine(Countdown1s());
            StartCoroutine(UpdateAutomously(guidanceVisualization.speed));
            if (!isDemo)
            {
                guidanceVisualization.TimestampRecording("start", -1);
            }
        }
    }
    
    IEnumerator Countdown1s()
    {
        float remainingTime = 1f;

        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        textUI.text = "";
    }
    

    void ReadCSV(int[] joints, TextAsset csvFile)
    {
        string[] lines = csvFile.text.Split('\n');
        dataLength = lines.Length;
        // Debug.Log(dataLength);
        // int gap = (int)System.Math.Truncate((dataLength / (dataLength * percentage)));
        for (int i=0; i<dataLength; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            Vector3[] positions = new Vector3[joints.Length];
            for (int j = 0; j < joints.Length; j++)   
            {
                float x = float.Parse(values[3 * joints[j]]) * -1; // 모션 좌우반전
                float y = float.Parse(values[3 * joints[j] + 1]);
                float z = float.Parse(values[3 * joints[j] + 2]);
                positions[j] = new Vector3(x, y, z);
            }
            
            jointPositions.Add(positions);
        }
        // Debug.Log(jointPositions.Count);
    }
    
    float[] CalculateTransparency(int count, float start, float end)
    {
        // 배열 생성
        float[] values = new float[count];
        
        // 로그 기반 비율 계산 (지수 함수를 사용하기 위한 log 변환)
        float logStart = Mathf.Log(start);
        float logEnd = Mathf.Log(end);
        
        // x축 간격 계산 (0 ~ 1 사이에서 일정한 간격)
        float step = start / (count - 1);

        // 지수 함수에 따라 값 생성
        for (int i = 0; i < count; i++)
        {
            // x축을 일정한 간격으로 이동시키고, 그에 맞는 y값(지수적)을 계산
            float t = i * step;
            float logValue = Mathf.Lerp(logStart, logEnd, t);
            values[i] = Mathf.Exp(logValue);
        }

        return values;
    }
    

    void CreateJointsAndConnections(int i)
    {
        // Assuming the first frame for visualization
        
        Vector3[] positions = jointPositions[i];
        
        GameObject[] jointObjects = new GameObject[positions.Length];
        List<LineRenderer[]> lineRenderers = new List<LineRenderer[]>(jointHierarchy.Length); // LineRenderer 객체 배열 초기화
        
        for (int j = 0; j < positions.Length; j++)
        {
            // Create a sphere at each joint position
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            /*AssignTag(jointObj, i.ToString());*/
            jointObj.transform.position = positions[j] + positionOffset;
            jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Adjust size as needed

            Renderer renderer = jointObj.GetComponent<Renderer>();
            Material material = renderer.material;

            // Shader를 Standard Shader로 변경
            material.shader = Shader.Find("Standard");

            if (material != null)
            {
                SetMaterialToTransparent(material);
            }
            
            material.color = color;
            

            MeshRenderer meshRenderer = jointObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            jointObjects[j] = jointObj;
        }
        
        allJoints.Add(jointObjects);

        for (int j = 0; j < jointHierarchy.Length; j++)
        {
            int[,] hierarchy = jointHierarchy[j];
            LineRenderer[] lines = new LineRenderer[hierarchy.GetLength(0)];
            for (int k = 0; k < hierarchy.GetLength(0); k++)
            {
                GameObject parentObj = jointObjects[hierarchy[k, 0]];
                GameObject childObj = jointObjects[hierarchy[k, 1]];

                // Create a line between the parent and child joints
                LineRenderer line = new GameObject("Line" + i.ToString()).AddComponent<LineRenderer>();
                /*AssignTagToLine(line, i.ToString());*/
                line.material = new Material(Shader.Find("Particles/Standard Unlit")); // 재질 설정
                SetMaterialToTransparent(line.material);
                line.startColor = color;
                line.endColor = color;
                line.startWidth = 0.05f;
                line.endWidth = 0.05f;
                line.positionCount = 2;
                line.SetPosition(0, parentObj.transform.position);
                line.SetPosition(1, childObj.transform.position);

                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;

                lines[k] = line;
            }
            
            lineRenderers.Add(lines);
        }

        allLines.Add(lineRenderers);
        
    }
    
    private void SetMaterialToTransparent(Material material)
    {
        material.SetFloat("_Mode", 3); // Transparent 모드 설정
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000; // Transparent 모드에 맞는 렌더링 순서 설정
    }
    
    
    public void UpdateJointsPositions()
    {
        Vector3[] positions = jointPositions[dataIdx];
        GameObject[] tempJoints = allJoints[0];
        List<LineRenderer[]> tempLines = allLines[0];
        
        allJoints.RemoveAt(0);
        allLines.RemoveAt(0);
        
        if (allJoints.Count > 0)
        {
            currentGuidance = allJoints[0];
        }
        realTimePerformanceMeasurement.GetGuidanceData(currentGuidance);
        
        // string str = $"{++frameCount}/{jointPositions.Count}";
        // textUI.text = str;

        for (int i = 0; i < positions.Length; i++)
        {
            if (tempJoints[i] != null)
            {
                tempJoints[i].transform.position = positions[i] + positionOffset;
            }
        }
        allJoints.Add(tempJoints);
    

        for (int j = 0; j < jointHierarchy.Length; j++)
        {
            int[,] hierarchy = jointHierarchy[j];
            LineRenderer[] tempLine = tempLines[j];
            for (int i = 0; i < hierarchy.GetLength(0); i++)
            {
                GameObject parentObj = tempJoints[hierarchy[i, 0]];
                GameObject childObj = tempJoints[hierarchy[i, 1]];
                if (tempLine[i] != null)
                {
                    tempLine[i].SetPosition(0, parentObj.transform.position);
                    tempLine[i].SetPosition(1, childObj.transform.position);
                }
            }
            tempLines.Add(tempLine);
        }
        allLines.Add(tempLines);
    
        // MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
        // MakeObjectsTransparent(allJoints[1], allLines[1], 0.1f);
        dataIdx++;
    }
    
    public IEnumerator UpdateAutomously(float speed)
    {
        float duration = (1 / guidanceVisualization.frameRate) * jointPositions.Count * (1 / speed);
        float durationForOneFrame = duration / jointPositions.Count;
        while (true)
        {
            if (dataIdx <= jointPositions.Count - 1)
            {
                UpdateJointsPositions();
            }
            // else if (dataIdx > jointPositions.Count - 1 && frameCount < dataIdx-1)
            // {
            //     UpdateWithoutAppending();
            // }
            else
            {
                string str = "동작이\n종료되었습니다.";
                textUI.text = str;
                guidanceVisualization.isMotionDone = true;
                Debug.Log("Motion is over");
                if (!isDemo)
                {
                    guidanceVisualization.TimestampRecording("end", -1);
                }
                yield break;
            }
            yield return new WaitForSeconds(durationForOneFrame);
        }
        
    }
    
    private void UpdateWithoutAppending()
    {
        foreach (var joint in allJoints[0])
        {
            Destroy(joint);
        }
        
        foreach (var lines in allLines[0])
        {
            foreach (var line in lines)
            {
                Destroy(line);
            }
        }
        
        allJoints.RemoveAt(0);
        allLines.RemoveAt(0);
        
        // string str = $"{++frameCount}/{jointPositions.Count}";
        // frameLeft.text = str;
        
        // MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
    }

    public void UpdateAnimation()
    {
        if (dataIdx <= jointPositions.Count - 1 && frameCount < dataIdx-1)
        {
            UpdateJointsPositions();
        }
        else if (dataIdx > jointPositions.Count - 1 && frameCount < dataIdx-1)
        {
            UpdateWithoutAppending();
        }
        else
        {
            string str = "Task is over. Please take off the HMD";
            textUI.text = str;
            guidanceVisualization.isMotionDone = true;
            Debug.Log("Motion is over");
            guidanceVisualization.TimestampRecording("end", -1);
        }
    }
    
    public void ResetJoints()
    {
        dataIdx = numOfGuidanceOnScreen;
        foreach (var joints in allJoints)
        {
            for (int i = 0; i < joints.GetLength(0); i++)
            {
                Destroy(joints[i]);
            }
        }

        foreach (var lines in allLines)
        {
            foreach (var line in lines)
            {
                for (int i = 0; i < line.GetLength(0); i++)
                {
                    Destroy(line[i]);
                }
            }
        }
        allJoints = new List<GameObject[]>();
        allLines = new List<List<LineRenderer[]>>();
        textUI.text = "";
    }
    
    
    // void MakeObjectsTransparent(GameObject[] jointObjects, List<LineRenderer[]> lineRenderers, float transparency) // 투명도 값 (0 = 완전 투명, 1 = 불투명)
    // {
    //     // 모든 조인트 오브젝트의 material 투명도 조정
    //     foreach (GameObject jointObj in jointObjects)
    //     {
    //         Renderer renderer = jointObj.GetComponent<Renderer>();
    //         if (renderer != null)
    //         {
    //             Color currentColor = renderer.material.color;
    //             currentColor.a = transparency; // 50% 투명
    //             renderer.material.color = currentColor;
    //         }
    //     }
    //
    //     // 모든 라인 렌더러의 material 투명도 조정
    //     foreach (LineRenderer[] lineRenderer in lineRenderers)
    //     {
    //         foreach (LineRenderer line in lineRenderer)
    //         {
    //             if (line != null)
    //             {
    //                 // Start Color와 End Color에서 알파 값을 설정하여 투명도 적용
    //                 Color startCol = line.startColor;
    //                 startCol.a = Mathf.Clamp01(transparency); // 원하는 투명도 설정
    //                 line.startColor = startCol;
    //
    //                 Color endCol = line.endColor;
    //                 endCol.a = Mathf.Clamp01(transparency); // 원하는 투명도 설정
    //                 line.endColor = endCol;
    //
    //                 // Material이 제대로 적용되었는지 확인 (Particles/Standard Unlit 사용)
    //                 line.material.renderQueue = 3000; // Transparent 렌더링 순서 보장
    //             }
    //         }
    //     }
    // }
}