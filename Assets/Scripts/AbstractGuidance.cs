using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/*
 * TODO::
 * 1. lineRenderer 삭제
 * 2. Sphere 생성 대신 화살표 프리팹 가져다 사용
 * 3. 화살표 방향을 다음 조인트로 향하도록 설정
 * 4. 화살표 크기 조정
 * 5. 화살표 투명도 조절
 * 6. 화살표 색상 바꿔가면서 좋은 색 찾기
 */

public class AbstractGuidance:MonoBehaviour
{
    public TextAsset csvFile; // Assign your CSV in the inspector
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private List<GameObject[]> allJoints = new List<GameObject[]>();
    private int[] engagedJointList;
    public Button startButton;
    public Button updateButton;
    public GameObject ArrowPrefab;
    
    
    private float timer;
    private int currentFrame = 0;
    private float[] transperancyArray;
    public Vector3 positionOffset = new Vector3(-100f, 1f, 0f);

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

        dataIdx = numOfGuidanceOnScreen;
        
        // 버튼 클릭 이벤트에 메서드 등록
        /*startButton.onClick.AddListener(StartAnimation);*/
        updateButton.onClick.AddListener(StartAnimation);
        /*transparentButton.onClick.AddListener(() => StartCoroutine(StartAnimationAndMakeTransparent()));*/
    }

    void ReadCSV(int[] joints)
    {
        string[] lines = csvFile.text.Split('\n');
        dataLength = lines.Length;
        Debug.Log(dataLength);
        int gap = (int)System.Math.Truncate(dataLength / (dataLength * percentage));
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
        Vector3[] nextPositions = jointPositions[i + 1];
        
        GameObject[] jointObjects = new GameObject[positions.Length];
        
        for (int j = 0; j < positions.Length; j++)
        {
            // Create a sphere at each joint position
            Vector3 direction = nextPositions[j] - positions[j];
            GameObject arrow = Instantiate(ArrowPrefab, positions[j] + positionOffset, Quaternion.LookRotation(direction));
            /*AssignTag(jointObj, i.ToString());*/
            arrow.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Adjust size as needed

            Renderer renderer = arrow.GetComponent<Renderer>();
            Material material = renderer.material;

            // Shader를 Standard Shader로 변경
            material.shader = Shader.Find("Standard");

            MeshRenderer meshRenderer = arrow.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            jointObjects[j] = arrow;
        }
        
        allJoints.Add(jointObjects);
       
        MakeObjectsTransparent(jointObjects, transperancyArray[i]);
    }

    public void UpdateJointsPositions()
    {
        foreach (var joint in allJoints[0])
        {
            Destroy(joint);
        }
        allJoints.RemoveAt(0);
        
        Vector3[] positions = jointPositions[dataIdx];

        if (dataIdx != jointPositions.Count - 2)
        {
            Vector3[] nextPositions = jointPositions[dataIdx + 1];
            GameObject[] jointObjects = new GameObject[positions.Length];

            for (int j = 0; j < positions.Length; j++)
            {
                // Create a sphere at each joint position
                Vector3 direction = nextPositions[j] - positions[j];
                GameObject arrow = Instantiate(ArrowPrefab, positions[j] + positionOffset,
                    Quaternion.LookRotation(direction));
                /*AssignTag(jointObj, dataIdx.ToString());*/
                arrow.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Adjust size as needed

                Renderer renderer = arrow.GetComponent<Renderer>();
                Material material = renderer.material;

                // Shader를 Standard Shader로 변경
                material.shader = Shader.Find("Standard");

                MeshRenderer meshRenderer = arrow.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;
                }

                jointObjects[j] = arrow;
            }

            allJoints.Add(jointObjects);
        }
        
        for (int i = 0; i < allJoints.Count; i++)
        {
            MakeObjectsTransparent(allJoints[i], transperancyArray[i]);
        }
        
        dataIdx++;
    }

    void StartAnimation()
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

   
    void MakeObjectsTransparent(GameObject[] jointObjects, float transparency) // 투명도 값 (0 = 완전 투명, 1 = 불투명)
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
    }
}