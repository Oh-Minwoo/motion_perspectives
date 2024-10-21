using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class FullJoints : MonoBehaviour
{
    public bool isArmsMotion = true;
    public TextAsset armsMotionCSV;
    public TextAsset armsAndLegsMotionCSV;
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private List<GameObject[]> allJoints = new List<GameObject[]>();
    [HideInInspector] public GameObject[] currentGuidance;
    private List<LineRenderer[]> allLines = new List<LineRenderer[]>(); // LineRenderer 참조를 저장할 배열
    private int[] engagedJointList;
    private int[] engagedLineList;
    
    public RealTimePerformanceMeasurement realTimePerformanceMeasurement;

    public TMP_Text frameLeft;
    private int frameCount = 0;
    private float[] transperencyArray;
    public Vector3 positionOffset = new Vector3(-100f, 1f, 0f);
    public Color color;

    [SerializeField] private float percentage = 0.3f; // 0~1 사이의 값
    [SerializeField] private int numOfGuidanceOnScreen = 5; // Continuous guidance에서 한 화면에 보여질 guidance 개수
    [SerializeField] private bool isDiscrete = true;
    [SerializeField] private float transparency = 0.5f;

    private int dataIdx;

    [HideInInspector]
    public int dataLength;

    private int[,] jointHierarchy = new int[,]
    {
        {0, 1}, // Hips to RightUpLeg
        {1, 2}, // RightUpLeg to RightLeg
        {2, 3}, // RightLeg to RightFoot
        {0, 4}, // Hips to LeftUpLeg
        {4, 5}, // LeftUpLeg to LeftLeg
        {5, 6}, // LeftLeg to LeftFoot
        {0, 7}, // Hips to Spine
        {7, 8}, // Spine to Spine1
        {8, 9}, // Spine1 to Spine2
        {9, 10}, // Spine2 to Neck
        {10,11}, // Neck to Neck1
        {11,12}, // Neck1 to Head
        {9, 13}, // Spine2 to RightShoulder
        {13, 14}, // RightShoulder to RightArm
        {14, 15}, // RightArm to RightForeArm
        {15, 16}, // RightForeArm to RightHand
        {9, 17}, // Spine2 to LeftShoulder
        {17, 18}, // LeftShoulder to LeftArm
        {18, 19}, // LeftArm to LeftForeArm
        {19, 20}, // LeftForeArm to LeftHand
    };
    

    private Coroutine animationCoroutine;

    void Start()
    {
        if (isDiscrete)
        {
            numOfGuidanceOnScreen = 2;
        }

        if (isArmsMotion)
        {
            ReadCSV(armsMotionCSV);
        }
        else
        {
            ReadCSV(armsAndLegsMotionCSV);
        }
        
        transperencyArray = CalculateTransparency(numOfGuidanceOnScreen, 0.8f, 0.2f);
        Debug.Log(transperencyArray);
        
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
        
    }

    void ReadCSV(TextAsset csvFile)
    {
        string[] lines = csvFile.text.Split('\n');
        dataLength = lines.Length;
        Debug.Log(dataLength);
        int gap = (int)System.Math.Truncate((dataLength / (dataLength * percentage)));
        for (int i=gap; i<dataLength; i += gap)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            Vector3[] positions = new Vector3[values.Length / 3];
            for (int j = 0; j < positions.Length; j++)   
            {
                float x = float.Parse(values[3 * j]) * -1; // 모션 좌우반전
                float y = float.Parse(values[3 * j + 1]);
                float z = float.Parse(values[3 * j + 2]);
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
        LineRenderer[] lineRenderers = new LineRenderer[jointHierarchy.Length]; // LineRenderer 객체 배열 초기화

        for (int j = 0; j < positions.Length; j++)
        {
            // Create a sphere at each joint position
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            /*AssignTag(jointObj, i.ToString());*/
            jointObj.transform.position = positions[j] + positionOffset;
            jointObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // Adjust size as needed

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


        for (int k = 0; k < jointHierarchy.GetLength(0); k++)
        {
            GameObject parentObj = jointObjects[jointHierarchy[k, 0]];
            GameObject childObj = jointObjects[jointHierarchy[k, 1]];

            // Create a line between the parent and child joints
            LineRenderer line = new GameObject("Line" + i.ToString()).AddComponent<LineRenderer>();
            /*AssignTagToLine(line, i.ToString());*/
            line.material = new Material(Shader.Find("Particles/Standard Unlit")); // 재질 설정
            SetMaterialToTransparent(line.material);
            line.startColor = color;
            line.endColor = color;
            // line.material.color = color;
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.positionCount = 2;
            line.SetPosition(0, parentObj.transform.position);
            line.SetPosition(1, childObj.transform.position);

            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            lineRenderers[k] = line;
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
        LineRenderer[] tempLines = allLines[0];
        
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
        // LineRenderer[] lineRenderers = new LineRenderer[jointHierarchy.Length]; // LineRenderer 객체 배열 초기화
        for (int i = 0; i < positions.Length; i++)
        {
            if (tempJoints[i] != null)
            {
                tempJoints[i].transform.position = positions[i] + positionOffset;
            }
        }
        allJoints.Add(tempJoints);

        for (int i = 0; i < jointHierarchy.GetLength(0); i++)
        {
            GameObject parentObj = tempJoints[jointHierarchy[i, 0]];
            GameObject childObj = tempJoints[jointHierarchy[i, 1]];
            if (tempLines[i] != null)
            {
                tempLines[i].SetPosition(0, parentObj.transform.position);
                tempLines[i].SetPosition(1, childObj.transform.position);
            }
        }
        allLines.Add(tempLines);
        
        /*for (int j = 0; j < positions.Length; j++)
        {
            // Create a sphere at each joint position
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            /*AssignTag(jointObj, dataIdx.ToString());#1#
            jointObj.transform.position = positions[j] + positionOffset;
            jointObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // Adjust size as needed

            Renderer renderer = jointObj.GetComponent<Renderer>();
            Material material = renderer.material;

            // Shader를 Standard Shader로 변경
            material.shader = Shader.Find("Standard");

            MeshRenderer meshRenderer = jointObj.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            jointObjects[j] = jointObj;
        }
        
        allJoints.Add(jointObjects);
        
        
        for (int k = 0; k < jointHierarchy.GetLength(0); k++)
        {
            GameObject parentObj = jointObjects[jointHierarchy[k, 0]];
            GameObject childObj = jointObjects[jointHierarchy[k, 1]];

            // Create a line between the parent and child joints
            LineRenderer line = new GameObject("Line" + dataIdx.ToString()).AddComponent<LineRenderer>();
            /*AssignTagToLine(line, dataIdx.ToString());#1#
            line.material = new Material(Shader.Find("Standard")); // 재질 설정
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.positionCount = 2;
            line.SetPosition(0, parentObj.transform.position);
            line.SetPosition(1, childObj.transform.position);

            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            lineRenderers[k] = line;
        }

        allLines.Add(lineRenderers);*/
        Debug.Log(allJoints[0]);
        Debug.Log(allLines[0]);
        MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
        MakeObjectsTransparent(allJoints[1], allLines[1], 0.1f);
        
        // for (int i = 0; i < allJoints.Count; i++)
        // {
        //     if (numOfGuidanceOnScreen == 2)
        //     {
        //         float[] transparencies = new float[2] { 1.0f, 0.3f }; 
        //         MakeObjectsTransparent(allJoints[i], allLines[i], transparencies[i]);
        //     }
        //     else
        //     {
        //         MakeObjectsTransparent(allJoints[i], allLines[i], transperencyArray[i]);
        //     }
        // }
        
        
        

        dataIdx++;
    }

    public void StartAnimation()
    {
        if (dataIdx <= jointPositions.Count-1)
        {
            UpdateJointsPositions();
        }
        else
        {
            Debug.Log("Motion is over");
        }
    }

    void MakeObjectsTransparent(GameObject[] jointObjects, LineRenderer[] lineRenderers, float transparency)
    {
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

        foreach (LineRenderer line in lineRenderers)
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
    
    
    /*void MakeObjectsTransparent(GameObject[] jointObjects, LineRenderer[] lineRenderers, float transparency) // 투명도 값 (0 = 완전 투명, 1 = 불투명)
    {
        // 모든 조인트 오브젝트의 material 투명도 조정
        foreach (GameObject jointObj in jointObjects)
        {
            Renderer renderer = jointObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = renderer.material;
                Color color = material.color;
                color.a = transparency;
                material.color = color;

                // Alpha blending 설정
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
        }

        // 모든 라인 렌더러의 material 투명도 조정

        foreach (LineRenderer line in lineRenderers)
        {
            Material material = line.material;
            Color color = material.color;
            color.a = transparency;
            material.color = color;

            // Alpha blending 설정
            material.SetFloat("_Mode", 3);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }*/
    
    
}