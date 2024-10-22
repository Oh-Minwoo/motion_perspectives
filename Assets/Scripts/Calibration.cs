using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json; // JSON 파싱을 위한 Newtonsoft.Json 라이브러리 사용

public class Calibration : MonoBehaviour
{
    [SerializeField] private Vector3 positionOffset = new Vector3(-100, 1, 0);
    /*public RealTimePerformanceMeasurement realTimePerformanceMeasurement;*/
    
    public List<float[]> calibrationList = new List<float[]>();
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    private List<float> receivedDataList = new List<float>();

    private GameObject[] jointObjects;

    public GameObject[] JointObjects => jointObjects;
    private LineRenderer[] lineRenderers; 
    
    private bool firstDataProcessed = false;
    private Vector3 rootCoord;

    public string subNum;
    private bool isFunctionRunning = false;
    
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

    private Coroutine calibrationCoroutine = null;
    
    void Start()
    {
        udpClient = new UdpClient(5005);
        remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        // 데이터를 수신하는 스레드를 실행합니다.
        udpClient.BeginReceive(new AsyncCallback(OnDataReceived), udpClient);

        StartCoroutine(Countdown(3f));

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
    
    /*private IEnumerator ProcessDataAt30Hz()
    {
        while (true)
        {
            // 매 0.033초(30Hz 주기)마다 실행
            yield return new WaitForSeconds(1.0f / 30.0f);

            // 큐에서 데이터를 처리
            lock (queueLock)
            {
                if (dataQueue.Count > 0)
                {
                    List<float> dataToProcess = dataQueue.Dequeue();  // 큐에서 데이터 하나를 가져옴
                    if (dataToProcess != null && dataToProcess.Count > 0)
                    {
                        object timestamp = dataToProcess[0];
                        List<float> jointList = new List<float>(dataToProcess);
                        if (jointList.Count != 0)
                        {
                            jointList.RemoveAt(0);
                        }
                        
                        if (!firstDataProcessed)
                        {
                            
                            // 첫 번째 데이터는 A 함수에서 처리
                            CreateJointsAndConnections(jointList);
                            firstDataProcessed = true;  // 첫 번째 데이터 처리 완료 표시
                        }
                        else
                        {
                            // 이후 데이터는 B 함수에서 처리
                            UpdateJointsPositions(jointList);
                        }
                    } 
                }
            }
        }
    }*/
    void Update()
    {
        // 파싱된 데이터 출력 (리스트로 변환된 데이터 사용 가능)
        if (receivedDataList != null && receivedDataList.Count > 0)
        {
            /*float timestamp = receivedDataList[63];*/
            ;
            List<float> jointList = new List<float>(receivedDataList);
            if (jointList.Count != 0)
            {
                jointList.RemoveAt(63);
            }
            if (!firstDataProcessed)
            {
                            
                // 첫 번째 데이터는 A 함수에서 처리
                CreateJointsAndConnections(jointList);
                /*realTimePerformanceMeasurement.GetJointPos(jointObjects);*/
                firstDataProcessed = true;  // 첫 번째 데이터 처리 완료 표시
            }
            else
            {
                // 이후 데이터는 B 함수에서 처리
                UpdateJointsPositions(jointList);
                /*realTimePerformanceMeasurement.GetJointPos(jointObjects);*/
            }
            
        }
        
        /*if (Input.GetKeyDown(KeyCode.S))
        {
            if (StartCalibration() != null) // 코루틴이 실행 중일 때만 종료
            {
                StopCoroutine(calibrationCoroutine);
                calibrationCoroutine = null;
                Debug.Log("Calibration stopped");
                SaveCalibration($"{subNum}_calibration_data.csv"); // 다른 함수 실행
            }
        }*/
    }
    
    IEnumerator Countdown(float countdownTime)
    {
        float remainingTime = countdownTime;

        while (remainingTime > 0)
        {
            Debug.Log($"Countdown: {remainingTime:F1} seconds"); // 소수점 첫째 자리까지 시간 표시
            remainingTime -= Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }
        
        isFunctionRunning = true; // 카운트다운이 끝나면 함수 실행 플래그 설정
        StartCoroutine(ExcuteCalibrationForSeconds(1.5f));
        
    }

    private IEnumerator ExcuteCalibrationForSeconds(float duration)
    {
        Debug.Log("Calibration Starts");
        float elapsedTime = 0f;
        isFunctionRunning = true;

        while (elapsedTime < duration)
        {
            List<float> calibrationData = new List<float>(receivedDataList);
            calibrationList.Add(calibrationData.ToArray());
            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        isFunctionRunning = false;
        Debug.Log("Calibration Ends");
        SaveCalibration($"{subNum}_calibration_data.csv");
    }
    
    // private void ReadDataFromServer()
    // {
    //     float[] tempData = new float[63];
    //     for (int i = 0; i < jointObjects.Length; i++)
    //     {
    //         if (jointObjects[i] != null)
    //         {
    //             tempData[3 * i] = jointObjects[i].transform.position.x;
    //             tempData[3 * i + 1] = jointObjects[i].transform.position.y;
    //             tempData[3 * i + 2] = jointObjects[i].transform.position.z;
    //         }
    //     }
    //     calibrationList.Add(tempData);
    // }
    
    private void SaveCalibration(string fileName)
    {
        string path = Path.Combine("C:\\Users\\HCIS\\Desktop\\OhMinwoo_Thesis\\ActionNet Driving\\recording_data\\Calibration_data", fileName); // 파일 경로 지정

        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (float[] array in calibrationList)
            {
                writer.WriteLine(string.Join(",", array)); // 배열을 CSV 형식으로 변환
            }
        }

        Debug.Log($"CSV 파일이 저장되었습니다: {path}");
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

    
    public void UpdateJointsPositions(List<float> jointList)
    {
        Vector3[] positions = new Vector3[21];
        for (int i = 0; i < 21; i++)
        {
            Vector3 tempVector = new Vector3(-jointList[i*3], jointList[i*3+1], jointList[i*3+2]);
            positions[i] = Normalization(tempVector);
        }

        for (int i = 0; i < positions.Length; i++)
        {
            if (jointObjects[i] != null)
            {
                jointObjects[i].transform.position = positions[i] + positionOffset;
            }
        }

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