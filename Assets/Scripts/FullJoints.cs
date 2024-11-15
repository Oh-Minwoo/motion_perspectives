using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

[Serializable]
public struct Motions
{
    public bool frontal;
    public bool peripheral;
    public bool taichi;
    
    public override bool Equals(object obj)
    {
        if (!(obj is Motions))
            return false;
        Motions other = (Motions)obj;
        return frontal == other.frontal &&
               peripheral == other.peripheral &&
               taichi == other.taichi;
    }

    public override int GetHashCode()
    {
        // 각 bool을 비트로 표현하여 해시코드 생성
        return (frontal ? 1 : 0) |
               (peripheral ? 2 : 0) |
               (taichi ? 4 : 0);
    }
}

[Serializable]
public struct Perspectives
{
    public bool firstPerson;
    public bool thirdPerson;
    public bool mirror;
    public bool multiView;
    
    public bool Equals(Perspectives other)
    {
        return firstPerson == other.firstPerson &&
               thirdPerson == other.thirdPerson &&
               mirror == other.mirror &&
               multiView == other.multiView;
    }

    // Object.Equals 오버라이드
    public override bool Equals(object obj)
    {
        if (obj is Perspectives other)
        {
            return Equals(other);
        }
        return false;
    }

    // GetHashCode 오버라이드
    public override int GetHashCode()
    {
        // 각 bool을 비트로 표현하여 해시코드 생성
        return (firstPerson ? 1 : 0) |
               (thirdPerson ? 2 : 0) |
               (mirror ? 4 : 0) |
               (multiView ? 8 : 0);
    }
}


public class FullJoints : MonoBehaviour
{
    public string subName;
    public Motions motions;
    private Motions previousMotions;
    [Space(10)]
    public Perspectives perspectives;
    private Perspectives previousPerspectives;
    
    [Space(10)]
    [Header("System Settings")]
    
    public TextAsset frontalCSV;
    public TextAsset peripheralCSV;
    public TextAsset taichiCSV;

    [Space(10)] public float speed = 0.5f;
    
    private bool isDemo = false;
    private TimestampRecoder timestampRecoder;
    
    public SocketListener socketListener;
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private List<GameObject[]> allJoints = new List<GameObject[]>();
    [HideInInspector] public GameObject[] currentGuidance;
    private List<LineRenderer[]> allLines = new List<LineRenderer[]>(); // LineRenderer 참조를 저장할 배열
    private int[] engagedJointList;
    private int[] engagedLineList;

    private Vector3 originalCameraPos = new Vector3(-99f, 1f, -3.5f);
    private bool isFirstPerson = false;
    
    

    public TMP_Text textUI;
    public RawImage leftImg;
    public RawImage rightImg;
    private Vector3 originalTextPos;
    private int frameCount = 0;
    private float[] transperencyArray;
    public Vector3 positionOffset = new Vector3(-100f, 1f, 0f);
    public Vector3 cameraPosOffset = new Vector3(0f, 0f, -0.01f);
    [HideInInspector] public int mirrored = 1;
    public Color color;
    public GameObject camera;
    
    [Space(10)]
    [Header("Tools for SEQ Survey")]
    public GameObject surveyPanel;
    public GameObject ovrInteractionPrefab;
    
    [Space(10)]
    [Header("Deprecated. DO NO TOUCH")]
    [SerializeField] private float percentage = 0.3f; // 0~1 사이의 값
    [SerializeField] private int numOfGuidanceOnScreen = 5; // Continuous guidance에서 한 화면에 보여질 guidance 개수
    // [SerializeField] private bool isDiscrete = true;
    // [SerializeField] private float transparency = 0.5f;

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
        originalCameraPos = camera.transform.position;
        originalTextPos = textUI.transform.position;
        timestampRecoder = GetComponent<TimestampRecoder>();
        PerspectiveChange();
        MotionChange();
        previousMotions = motions;
        previousPerspectives = perspectives;
        // transperencyArray = CalculateTransparency(numOfGuidanceOnScreen, 0.8f, 0.2f);
        dataIdx = numOfGuidanceOnScreen;
        surveyPanel.SetActive(false);
        ovrInteractionPrefab.SetActive(false);
    }

    // void OnValidate()
    // {
    //     MotionChange();       
    //     PerspectiveChange();
    //
    //     if (timestampRecoder != null)
    //     {
    //         timestampRecoder.ResetConditions();
    //     }
    // }

    private void MotionChange()
    {
        
        if (motions.frontal)
        {
            ReadCSV(frontalCSV);
        }
        else if (motions.peripheral)
        {
            ReadCSV(peripheralCSV);
        }
        else if (motions.taichi)
        {
            ReadCSV(taichiCSV);
        }
    }

    private void PerspectiveChange()
    {
        if (perspectives.firstPerson)
        {
            isFirstPerson = true;
            textUI.transform.position += new Vector3(1.5f, 0f, 7f);
            mirrored = 1;
            leftImg.gameObject.SetActive(false);
            rightImg.gameObject.SetActive(false);
            
        }
        else if (perspectives.thirdPerson)
        {
            isFirstPerson = false;
            camera.transform.position = originalCameraPos;
            textUI.transform.position = originalTextPos;
            mirrored = 1;
            leftImg.gameObject.SetActive(false);
            rightImg.gameObject.SetActive(false);
        }
        else if (perspectives.mirror)
        {
            isFirstPerson = false;
            camera.transform.position = originalCameraPos;
            textUI.transform.position = originalTextPos;
            mirrored = -1;
            leftImg.gameObject.SetActive(false);
            rightImg.gameObject.SetActive(false);
        }
        else if (perspectives.multiView)
        {
            isFirstPerson = false;
            camera.transform.position = originalCameraPos;
            textUI.transform.position = originalTextPos;
            mirrored = 1;
            leftImg.gameObject.SetActive(true);
            rightImg.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (!perspectives.Equals(previousPerspectives) || !motions.Equals(previousMotions))
        {
            PerspectiveChange();
            MotionChange();       
            if (timestampRecoder != null)
            {
                timestampRecoder.ResetConditions();
            }
            previousMotions = motions;
            previousPerspectives = perspectives;
        }
        if (isFirstPerson)
        {
            camera.transform.position = socketListener.headPosition + cameraPosOffset;
        }
    }

    


    public void StartAnimation(bool isDemonstraction)
    {
        isDemo = isDemonstraction;
        for (int i = 0; i < numOfGuidanceOnScreen; i++)
        {
            CreateJointsAndConnections(i);
        }

        currentGuidance = allJoints[0];
        
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
        
        textUI.text = $"Start!";
        StartCoroutine(Countdown1s());
        StartCoroutine(UpdateAutomously());
        if (!isDemo)
        {
            timestampRecoder.TimestampRecording("start", -1);
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
    
    void ReadCSV(TextAsset csvFile)
    {
        jointPositions = new List<Vector3[]>();
        string[] lines = csvFile.text.Split('\n');
        dataLength = lines.Length;
        for (int i=0; i<dataLength; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = lines[i].Split(',');
            Vector3[] positions = new Vector3[values.Length / 3];
            for (int j = 0; j < positions.Length; j++)   
            {
                float x = float.Parse(values[3 * j]) * -1; // 모션 좌우반전
                float y = float.Parse(values[3 * j + 1]);
                float z = float.Parse(values[3 * j + 2]) * mirrored;
                positions[j] = new Vector3(x, y, z);
            }
            
            jointPositions.Add(positions);
        }
    }
    
    // float[] CalculateTransparency(int count, float start, float end)
    // {
    //     // 배열 생성
    //     float[] values = new float[count];
    //     
    //     // 로그 기반 비율 계산 (지수 함수를 사용하기 위한 log 변환)
    //     float logStart = Mathf.Log(start);
    //     float logEnd = Mathf.Log(end);
    //     
    //     // x축 간격 계산 (0 ~ 1 사이에서 일정한 간격)
    //     float step = start / (count - 1);
    //
    //     // 지수 함수에 따라 값 생성
    //     for (int i = 0; i < count; i++)
    //     {
    //         // x축을 일정한 간격으로 이동시키고, 그에 맞는 y값(지수적)을 계산
    //         float t = i * step;
    //         float logValue = Mathf.Lerp(logStart, logEnd, t);
    //         values[i] = Mathf.Exp(logValue);
    //     }
    //
    //     return values;
    // }

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
            line.startWidth = 0.08f;
            line.endWidth = 0.08f;
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


    private void UpdateJointsPositions()
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
        
        // string str = $"{++frameCount}/{jointPositions.Count}";
        // frameLeft.text = str;
        
        
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
        
        // MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
        // MakeObjectsTransparent(allJoints[1], allLines[1], 0.1f);
        dataIdx++;
    }

    public IEnumerator UpdateAutomously()
    {
        float duration = (1 / timestampRecoder.frameRate) * jointPositions.Count * (1 / speed);
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
                Debug.Log("Motion is over");
                if (!isDemo)
                {
                    timestampRecoder.TimestampRecording("end", -1);
                }
                
                yield break;
            }
            yield return new WaitForSeconds(durationForOneFrame);
        }
        
    }

    // private void UpdateWithoutAppending()
    // {
    //     foreach (var joint in allJoints[0])
    //     {
    //         Destroy(joint);
    //     }
    //     
    //     foreach (var line in allLines[0])
    //     {
    //         Destroy(line);
    //     }
    //     
    //     allJoints.RemoveAt(0);
    //     allLines.RemoveAt(0);
    //     
    //     // string str = $"{++frameCount}/{jointPositions.Count}";
    //     // frameLeft.text = str;
    //     
    //     // MakeObjectsTransparent(allJoints[0], allLines[0], 1.0f);
    // }
    

    // public void UpdateAnimation()
    // {
    //     if (dataIdx <= jointPositions.Count - 1 && frameCount < dataIdx-1)
    //     {
    //         UpdateJointsPositions();
    //     }
    //     else if (dataIdx > jointPositions.Count - 1 && frameCount < dataIdx-1)
    //     {
    //         UpdateWithoutAppending();
    //     }
    //     else
    //     {
    //         string str = "Task is over. Please take off the HMD";
    //         textUI.text = str;
    //         guidanceVisualization.isMotionDone = true;
    //         Debug.Log("Motion is over");
    //         guidanceVisualization.TimestampRecording("end", -1);
    //     }
    // }

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

        foreach (var line in allLines)
        {
            for (int i = 0; i < line.GetLength(0); i++)
            {
                Destroy(line[i]);
            }
        }
        allJoints = new List<GameObject[]>();
        allLines = new List<LineRenderer[]>();
        textUI.text = "";
    }

    public void StartSEQ()
    {
        surveyPanel.SetActive(true);
        ovrInteractionPrefab.SetActive(true);
    }

    // void MakeObjectsTransparent(GameObject[] jointObjects, LineRenderer[] lineRenderers, float transparency)
    // {
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
    //
    //     foreach (LineRenderer line in lineRenderers)
    //     {
    //         if (line != null)
    //         {
    //             // Start Color와 End Color에서 알파 값을 설정하여 투명도 적용
    //             Color startCol = line.startColor;
    //             startCol.a = Mathf.Clamp01(transparency); // 원하는 투명도 설정
    //             line.startColor = startCol;
    //
    //             Color endCol = line.endColor;
    //             endCol.a = Mathf.Clamp01(transparency); // 원하는 투명도 설정
    //             line.endColor = endCol;
    //
    //             // Material이 제대로 적용되었는지 확인 (Particles/Standard Unlit 사용)
    //             line.material.renderQueue = 3000; // Transparent 렌더링 순서 보장
    //         }
    //     }
    // }
}