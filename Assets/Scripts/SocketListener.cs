using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json; // JSON 파싱을 위한 Newtonsoft.Json 라이브러리 사용


public class SocketListener : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = new Vector3(-100, 1, 0);

    public FullJoints fullJoints;
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private List<float> receivedDataList = new List<float>();
    [HideInInspector] public bool isGuidanceStart = false;
    [HideInInspector] public Vector3 headPosition;
    
    private GameObject[] jointObjects;
    private Vector3[] rawJointVectors;

    public GameObject[] JointObjects => jointObjects;
    private LineRenderer[] lineRenderers; 
    
    private bool firstDataProcessed = false;
    private Vector3 rootCoord;
    
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
    
    private string[] jointNames = new string[]
    {
        "Hips",
        "RightUpLeg",
        "RightLeg",
        "RightFoot",
        "LeftUpLeg",
        "LeftLeg",
        "LeftFoot",
        "Spine",
        "Spine1",
        "Spine2",
        "Neck",
        "Neck1",
        "Head",
        "RightShoulder",
        "RightArm",
        "RightForeArm",
        "RightHand",
        "LeftShoulder",
        "LeftArm",
        "LeftForeArm",
        "LeftHand"
    };
    
    void Start()
    {
        isGuidanceStart = false;
        udpClient = new UdpClient(5005);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        rawJointVectors = new Vector3[21];

        // 데이터를 수신하는 스레드를 실행합니다.
        udpClient.BeginReceive(new AsyncCallback(OnDataReceived), udpClient);
        
        /*StartCoroutine(ProcessDataAt30Hz());*/
    }

    private void OnDataReceived(IAsyncResult result)
    {
        byte[] receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        string receivedString = Encoding.UTF8.GetString(receivedBytes);

        /*Debug.Log("수신된 데이터: " + receivedString);*/

        // JSON 문자열을 파싱하여 리스트로 변환
        try
        {
            receivedDataList = JsonConvert.DeserializeObject<List<float>>(receivedString);
            // 큐에 데이터를 저장 (큐에 접근하는 동안 다른 스레드가 접근하지 못하도록 락 사용)
        }
        catch (Exception e)
        {
            Debug.LogError("데이터 파싱 중 에러 발생: " + e.Message);
        }

        // 다시 수신 대기
        udpClient.BeginReceive(new AsyncCallback(OnDataReceived), udpClient);
    }
    
    
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            AlignJoints();
        }
        // 파싱된 데이터 출력 (리스트로 변환된 데이터 사용 가능)
        if (receivedDataList != null && receivedDataList.Count > 0)
        {
            float timestamp = receivedDataList[63];
            ;
            List<float> jointList = new List<float>(receivedDataList);
            if (jointList.Count != 0)
            {
                jointList.RemoveAt(63);
            }
            if (!firstDataProcessed)
            {
                
                CreateJointsAndConnections(jointList);
                // if (isGuidanceStart)
                // {
                //     realTimePerformanceMeasurement.GetJointPos(jointObjects, timestamp);
                // }
                firstDataProcessed = true;  // 첫 번째 데이터 처리 완료 표시
            }
            else
            {
                UpdateJointsPositions(jointList, fullJoints.mirrored);
                // if (isGuidanceStart)
                // {
                //     realTimePerformanceMeasurement.GetJointPos(jointObjects, timestamp);
                // }
            }
        }
    }
    
    void CreateJointsAndConnections(List<float> jointList)
    {
        Vector3[] positions = new Vector3[21];
        for (int i = 0; i < 21; i++)
        {
            Vector3 tempVector = new Vector3(-jointList[i*3], jointList[i*3+1], jointList[i*3+2]);
            positions[i] = tempVector;
        }

        rootCoord = positions[0];
        
        jointObjects = new GameObject[positions.Length];
        lineRenderers = new LineRenderer[jointHierarchy.GetLength(0)]; // LineRenderer 객체 배열 초기화

        for (int i = 0; i < positions.Length; i++)
        {
            // Create a sphere at each joint position
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jointObj.transform.position = positions[i] + positionOffset;
            jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); 
            jointObjects[i] = jointObj;
            
            Renderer renderer = jointObj.GetComponent<Renderer>();
            Material material = renderer.material;
            
            // Shader를 Standard Shader로 변경
            material.shader = Shader.Find("Standard");
            renderer.name = jointNames[i];
        }

        headPosition = jointObjects[12].transform.position;
        
        

        for (int i = 0; i < jointHierarchy.GetLength(0); i++)
        {
            GameObject parentObj = jointObjects[jointHierarchy[i, 0]];
            GameObject childObj = jointObjects[jointHierarchy[i, 1]];

            LineRenderer line = new GameObject("Line" + i.ToString()).AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default")); 
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.positionCount = 2;
            line.SetPosition(0, parentObj.transform.position);
            line.SetPosition(1, childObj.transform.position);

            lineRenderers[i] = line; 
        }
    }

    
    public void UpdateJointsPositions(List<float> jointList, int mirrored)
    {
        Vector3[] positions = new Vector3[21];
        for (int i = 0; i < 21; i++)
        {
            Vector3 tempVector = new Vector3(-jointList[i*3], jointList[i*3+1], mirrored * jointList[i*3+2]);
            rawJointVectors[i] = tempVector;
            positions[i] = Normalization(tempVector);
        }

        for (int i = 0; i < positions.Length; i++)
        {
            if (jointObjects[i] != null)
            {
                jointObjects[i].transform.position = positions[i] + positionOffset;
            }
        }
        headPosition = jointObjects[12].transform.position;

        // LineRenderer 위치 업데이트
        for (int i = 0; i < lineRenderers.Length; i++)
        {
            GameObject parentObj = jointObjects[jointHierarchy[i, 0]];
            GameObject childObj = jointObjects[jointHierarchy[i, 1]];
            if (lineRenderers[i] != null)
            {
                lineRenderers[i].SetPosition(0, parentObj.transform.position);
                lineRenderers[i].SetPosition(1, childObj.transform.position);
            }
        }
    }

    private void AlignJoints()
    {
        if (rawJointVectors != null && rawJointVectors.Length > 0 && rawJointVectors[0] != null)
        {
            /*
            Debug.Log(rawJointVectors[0]);
            */
            rootCoord = rawJointVectors[0];
        }
    }

    private Vector3 Normalization(Vector3 positions)
    {
        Vector3 normalizedPosition = new Vector3(); 
        normalizedPosition = (positions - rootCoord) / 100;
        return normalizedPosition;
    }

     void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}