using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class ExplicitGuidance : MonoBehaviour
{
    public TextAsset csvFile; // Assign your CSV in the inspector
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private List<GameObject[]> allJoints = new List<GameObject[]>();
    [HideInInspector] public GameObject[] currentGuidance;
    private List<List<LineRenderer[]>> allLines = new List<List<LineRenderer[]>>(); // LineRenderer 참조를 저장할 배열
    private int[] engagedJointList;
    private int[] engagedLineList;

    public TMP_Text frameLeft;
    private int frameCount = 1;
    private float[] transperancyArray;
    public Vector3 positionOffset = new Vector3(-100f, 1f, 0f);
    public Color color;

    [SerializeField] private float percentage = 0.1f; // 0~1 사이의 값
    [SerializeField] private int numOfGuidanceOnScreen = 5;
    private int dataIdx;

    [HideInInspector]
    public int dataLength;

    // Joint hierarchy mapping example
    private int[,] rightLegHierarchy =
    {
        { 0, 1 }, // RightUpLeg to RightLeg
        { 1, 2 }, // RightLeg to RightFoot
    };
    
    private int[,] leftLegHierarchy =
    {
        {3, 4}, // LeftUpLeg to LeftLeg
        {4, 5}, // LeftLeg to LeftFoot
    };
    
    private int[,] rightArmHierarchy =
    {
        {6, 7}, // RightArm to RightForeArm
        {7, 8}, // RightForeArm to RightHand
    };

    private int[,] leftArmHierarchy =
    {
        {9, 10}, // LeftArm to LeftForeArm
        {10, 11}, // LeftForeArm to LeftHand
    };

    private int[][,] jointHierarchy;
    

    private Coroutine animationCoroutine;

    void Start()
    {
        engagedJointList = new int[] { 1, 2, 3, 4, 5, 6, 14, 15, 16, 18, 19, 20 };
        jointHierarchy = new int[][,]
        {
            rightLegHierarchy,
            leftLegHierarchy,
            rightArmHierarchy,
            leftArmHierarchy
        };
        ReadCSV(engagedJointList);
        transperancyArray = CalculateTransparency(numOfGuidanceOnScreen, 0.8f, 0.2f);
        Debug.Log(transperancyArray);
        
        for (int i = 0; i < numOfGuidanceOnScreen; i++)
        {
            CreateJointsAndConnections(i);
        }

        string str = $"{frameCount}/{jointPositions.Count}";
        frameLeft.text = str;

        currentGuidance = allJoints[0];

        dataIdx = numOfGuidanceOnScreen;
        
    }

    void ReadCSV(int[] joints)
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
            jointObj.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // Adjust size as needed

            Renderer renderer = jointObj.GetComponent<Renderer>();
            Material material = renderer.material;

            // Shader를 Standard Shader로 변경
            material.shader = Shader.Find("Standard");
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
                line.material = new Material(Shader.Find("Standard")); // 재질 설정
                line.material.color = color;
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
    
    
    public void UpdateJointsPositions()
    {
        Vector3[] positions = jointPositions[dataIdx];
        foreach (var joint in allJoints[0])
        {
            Destroy(joint);
        }
        foreach (var line in allLines[0])
        {
            foreach (var l in line)
            {
                Destroy(l.gameObject);
            }
        }
        
        allJoints.RemoveAt(0);
        allLines.RemoveAt(0);
        /*DestroyObjectsByTag((dataIdx - numOfGuidanceOnScreen).ToString());*/

        if (allJoints.Count > 0)
        {
            currentGuidance = allJoints[0];
        }
        
        string str = $"{++frameCount}/{jointPositions.Count}";
        frameLeft.text = str;
        
        GameObject[] jointObjects = new GameObject[positions.Length];
        List<LineRenderer[]> lineRenderers = new List<LineRenderer[]>(jointHierarchy.Length); // LineRenderer 객체 배열 초기화
        
        for (int j = 0; j < positions.Length; j++)
        {
            // Create a sphere at each joint position
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            /*AssignTag(jointObj, dataIdx.ToString());*/
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
        

        for (int j = 0; j < jointHierarchy.Length; j++)
        {
            int[,] hierarchy = jointHierarchy[j];
            LineRenderer[] lines = new LineRenderer[hierarchy.GetLength(0)];
            for (int k = 0; k < hierarchy.GetLength(0); k++)
            {
                GameObject parentObj = jointObjects[hierarchy[k, 0]];
                GameObject childObj = jointObjects[hierarchy[k, 1]];

                // Create a line between the parent and child joints
                LineRenderer line = new GameObject("Line" + dataIdx.ToString()).AddComponent<LineRenderer>();
                /*AssignTagToLine(line, dataIdx.ToString());*/
                line.material = new Material(Shader.Find("Standard")); // 재질 설정
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
        
        for (int i = 0; i < allJoints.Count; i++)
        {
            MakeObjectsTransparent(allJoints[i], allLines[i], transperancyArray[i]);
        }
        
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

   
    void MakeObjectsTransparent(GameObject[] jointObjects, List<LineRenderer[]> lineRenderers, float transparency) // 투명도 값 (0 = 완전 투명, 1 = 불투명)
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
        foreach (LineRenderer[] lineRenderer in lineRenderers)
        {
            foreach (LineRenderer line in lineRenderer)
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
        }
    }
    
    
}