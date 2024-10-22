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
    public SocketListener socketListener;
    public bool isArmsMotion = true;
    public TextAsset armsMotionCSV;
    public TextAsset armsAndLegsMotionCSV;
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private List<GameObject[]> allJoints = new List<GameObject[]>();
    [HideInInspector] public GameObject[] currentGuidance;
    private List<List<LineRenderer[]>> allLines = new List<List<LineRenderer[]>>(); // LineRenderer 참조를 저장할 배열
    private int[] engagedJointList;
    private int[] engagedLineList;
    private UnixTime unixTime;
    private string[] timestampRecorder = new string[4];
    public string subName = "sub01";
    private string conditionName;
    private string csvFilePath;
    
    public RealTimePerformanceMeasurement realTimePerformanceMeasurement;

    public TMP_Text frameLeft;
    private int frameCount = 1;
    private float[] transperancyArray;
    public Vector3 positionOffset = new Vector3(-100f, 1f, 0f);
    public Color color;

    [SerializeField] private float percentage = 0.3f; // 0~1 사이의 값
    [SerializeField] private int numOfGuidanceOnScreen = 5; // Continuous guidance에서 한 화면에 보여질 guidance 개수
    [SerializeField] private bool isDiscrete = true;
    [SerializeField] private float transparency = 0.5f;

    private int dataIdx;

    [HideInInspector]
    public int dataLength;

    // Joint hierarchy mapping example
    private int[,] rightArmHierarchy =
    {
        { 0, 1 }, // RightUpLeg to RightLeg
        { 1, 2 }, // RightLeg to RightFoot
    };
    
    private int[,] leftArmHierarchy =
    {
        {3, 4}, // LeftUpLeg to LeftLeg
        {4, 5}, // LeftLeg to LeftFoot
    };

    private int[][,] jointHierarchy;
    

    private Coroutine animationCoroutine;

    void Start()
    {
        ConditionNameGenerator();
        FilePathGenerator();

        // 파일 확인 및 생성
        CheckAndCreateCSV(csvFilePath);
        
        if (isDiscrete)
        {
            numOfGuidanceOnScreen = 2;
        }
        engagedJointList = new int[] { 14, 15, 16, 18, 19, 20 };
        jointHierarchy = new int[][,]
        {
            rightArmHierarchy,
            leftArmHierarchy
        };
        if (isArmsMotion)
        {
            ReadCSV(engagedJointList, armsMotionCSV);
        }
        else
        {
            ReadCSV(engagedJointList, armsAndLegsMotionCSV);
        }
        transperancyArray = CalculateTransparency(numOfGuidanceOnScreen, 0.8f, 0.2f);
        Debug.Log(transperancyArray);
        
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
    
    private void FilePathGenerator()
    {
        string csvFileName = $"{subName}_timestamp.csv";
        csvFilePath = Path.Combine("C:\\Users\\HCIS\\Desktop\\OhMinwoo_Thesis\\theis\\Assets\\Timestamp_res", csvFileName);
    }
    
    private void CheckAndCreateCSV(string path)
    {
        if (!File.Exists(path))
        {
            // 파일 생성
            File.WriteAllText(path, "Subject Name, Condition, Action, Unix Time\n"); // 헤더 추가
            Debug.Log("CSV 파일이 생성되었습니다: " + path);
        }
        else
        {
            Debug.Log("CSV 파일이 이미 존재합니다: " + path);
        }
    }

    public void StartAnimation()
    {
        for (int i = 0; i < numOfGuidanceOnScreen; i++)
        {
            CreateJointsAndConnections(i);
        }

        Debug.Log(allJoints.Count);
        Debug.Log(allLines.Count);

        MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
        MakeObjectsTransparent(allJoints[1], allLines[1], 0.1f);

        string str = $"{frameCount}/{jointPositions.Count}";
        frameLeft.text = str;

        currentGuidance = allJoints[0];
        realTimePerformanceMeasurement.GetGuidanceData(currentGuidance);

        dataIdx = numOfGuidanceOnScreen;
        
        socketListener.isGuidanceStart = true;
    }

    private void ConditionNameGenerator()
    {
        if (isArmsMotion)
        {
            conditionName = "armsMo + fullVis";
        }
        else
        {
            conditionName = "armsLegsMo + fullVis";
        }
    }

    void ReadCSV(int[] joints, TextAsset csvFile)
    {
        string[] lines = csvFile.text.Split('\n');
        dataLength = lines.Length;
        Debug.Log(dataLength);
        int gap = (int)System.Math.Truncate((dataLength / (dataLength * percentage)));
        for (int i=gap; i<dataLength; i += gap)
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
        Debug.Log(jointPositions.Count);
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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartAnimation();
        }
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
        // foreach (var joint in allJoints[0])
        // {
        //     Destroy(joint);
        // }
        // foreach (var line in allLines[0])
        // {
        //     foreach (var l in line)
        //     {
        //         Destroy(l.gameObject);
        //     }
        // }
        
        allJoints.RemoveAt(0);
        allLines.RemoveAt(0);
        /*DestroyObjectsByTag((dataIdx - numOfGuidanceOnScreen).ToString());*/

        if (allJoints.Count > 0)
        {
            currentGuidance = allJoints[0];
        }
        realTimePerformanceMeasurement.GetGuidanceData(currentGuidance);
        
        string str = $"{++frameCount}/{jointPositions.Count}";
        frameLeft.text = str;
        
        // GameObject[] jointObjects = new GameObject[positions.Length];
        // List<LineRenderer[]> lineRenderers = new List<LineRenderer[]>(jointHierarchy.Length); // LineRenderer 객체 배열 초기화
        
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
        
        MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
        MakeObjectsTransparent(allJoints[1], allLines[1], 0.1f);
        dataIdx++;
    }

    public void UpdateAnimation()
    {
        if (dataIdx <= jointPositions.Count-1)
        {
            UpdateJointsPositions();
        }
        else
        {
            Debug.Log("Motion is over");
            TimestampRecording("end");
        }
    }
    
    
    void MakeObjectsTransparent(GameObject[] jointObjects, List<LineRenderer[]> lineRenderers, float transparency) // 투명도 값 (0 = 완전 투명, 1 = 불투명)
    {
        // 모든 조인트 오브젝트의 material 투명도 조정
        foreach (GameObject jointObj in jointObjects)
        {
            Renderer renderer = jointObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color currentColor = renderer.material.color;
                currentColor.a = transparency; // 50% 투명
                renderer.material.color = currentColor;
            }
        }

        // 모든 라인 렌더러의 material 투명도 조정
        foreach (LineRenderer[] lineRenderer in lineRenderers)
        {
            foreach (LineRenderer line in lineRenderer)
            {
                if (line != null)
                {
                    // Start Color와 End Color에서 알파 값을 설정하여 투명도 적용
                    Color startCol = line.startColor;
                    startCol.a = Mathf.Clamp01(transparency); // 원하는 투명도 설정
                    line.startColor = startCol;

                    Color endCol = line.endColor;
                    endCol.a = Mathf.Clamp01(transparency); // 원하는 투명도 설정
                    line.endColor = endCol;

                    // Material이 제대로 적용되었는지 확인 (Particles/Standard Unlit 사용)
                    line.material.renderQueue = 3000; // Transparent 렌더링 순서 보장
                }
            }
        }
    }
    public void TimestampRecording(string startOrEnd)
    {
        timestampRecorder = new string[4];
        unixTime = GetComponent<UnixTime>();
        string currentTime = unixTime.GetCurrentUnixTime();
        timestampRecorder[0] = subName;
        timestampRecorder[1] = conditionName;
        timestampRecorder[2] = startOrEnd;
        timestampRecorder[3] = currentTime;
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