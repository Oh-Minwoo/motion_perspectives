using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class JointVisualization2 : MonoBehaviour
{
    public TextAsset csvFile; // Assign your CSV in the inspector
    public Vector3 positionOffsets = new Vector3(-100, 1, 0);
    private List<Vector3[]> jointPositions = new List<Vector3[]>();
    private GameObject[] jointObjects;
    private LineRenderer[] lineRenderers; 
    private float timer;
    private int currentFrame = 0;
    public float UpdateSeconds = 0.033f; // 30FPS =  0.033
    private Vector3 hipPosition;

    // joint별 오브젝트 정의
    /*
    public GameObject Hips;
    public GameObject RightUpLeg;
    public GameObject RightLeg;
    public GameObject RightFoot;
    public GameObject LeftUpLeg;
    public GameObject LeftLeg;
    public GameObject LeftFoot;
    public GameObject Spine;
    public GameObject Spine1;
    public GameObject Spine2;
    public GameObject Neck;
    public GameObject Neck1;
    public GameObject Head;
    public GameObject RightShoulder;
    public GameObject RightArm;
    public GameObject RightForeArm;
    public GameObject RightHand;
    public GameObject LeftShoulder;
    public GameObject LeftArm;
    public GameObject LeftForeArm;
    public GameObject LeftHand;
    */

    // joint별 오브젝트 매핑
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
        ReadCSV();
        CreateJointsAndConnections();
        /*AssignJointObjects();*/
        /*hipPosition = Hips.transform.position;*/
    }

    void ReadCSV()
    {
        string[] lines = csvFile.text.Split('\n');
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = line.Split(',');
            Vector3[] positions = new Vector3[values.Length / 3];
            for (int i = 0; i < positions.Length; i++)
            {
                float x = - float.Parse(values[3 * i]);
                float y = float.Parse(values[3 * i + 1]);
                float z = float.Parse(values[3 * i + 2]);
                positions[i] = new Vector3(x, y, z);
            }
            jointPositions.Add(positions);
        }
    }

    void CreateJointsAndConnections()
    {
        Vector3[] positions = jointPositions[0];

        jointObjects = new GameObject[positions.Length];
        lineRenderers = new LineRenderer[jointHierarchy.GetLength(0)]; // LineRenderer 객체 배열 초기화

        for (int i = 0; i < positions.Length; i++)
        {
            // Create a sphere at each joint position
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jointObj.name = jointNames[i];
            jointObj.transform.position = positions[i] + positionOffsets;
            jointObj.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); 
            jointObjects[i] = jointObj;
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


    // void AssignJointObjects()
    // {
    //     if (this.gameObject.name == "Expert_Avatar")
    //     {
    //         Hips = GameObject.Find("Hips");
    //         RightUpLeg = GameObject.Find("RightUpLeg");
    //         RightLeg = GameObject.Find("RightLeg");
    //         RightFoot = GameObject.Find("RightFoot");
    //         LeftUpLeg = GameObject.Find("LeftUpLeg");
    //         LeftLeg = GameObject.Find("LeftLeg");
    //         LeftFoot = GameObject.Find("LeftFoot");
    //         Spine = GameObject.Find("Spine");
    //         Spine1 = GameObject.Find("Spine1");
    //         Spine2 = GameObject.Find("Spine2");
    //         Neck = GameObject.Find("Neck");
    //         Neck1 = GameObject.Find("Neck1");
    //         Head = GameObject.Find("Head");
    //         RightShoulder = GameObject.Find("RightShoulder");
    //         RightArm = GameObject.Find("RightArm");
    //         RightForeArm = GameObject.Find("RightForeArm");
    //         RightHand = GameObject.Find("RightHand");
    //         LeftShoulder = GameObject.Find("LeftShoulder");
    //         LeftArm = GameObject.Find("LeftArm");
    //         LeftForeArm = GameObject.Find("LeftForeArm");
    //         LeftHand = GameObject.Find("LeftHand");
    //     }
    //     else
    //     {
    //         Hips = GameObject.Find("Hips1");
    //         RightUpLeg = GameObject.Find("RightUpLeg1");
    //         RightLeg = GameObject.Find("RightLeg1");
    //         RightFoot = GameObject.Find("RightFoot1");
    //         LeftUpLeg = GameObject.Find("LeftUpLeg1");
    //         LeftLeg = GameObject.Find("LeftLeg1");
    //         LeftFoot = GameObject.Find("LeftFoot1");
    //         Spine = GameObject.Find("Spine10");
    //         Spine1 = GameObject.Find("Spine11");
    //         Spine2 = GameObject.Find("Spine12");
    //         Neck = GameObject.Find("Neck10");
    //         Neck1 = GameObject.Find("Neck11");
    //         Head = GameObject.Find("Head1");
    //         RightShoulder = GameObject.Find("RightShoulder1");
    //         RightArm = GameObject.Find("RightArm1");
    //         RightForeArm = GameObject.Find("RightForeArm1");
    //         RightHand = GameObject.Find("RightHand1");
    //         LeftShoulder = GameObject.Find("LeftShoulder1");
    //         LeftArm = GameObject.Find("LeftArm1");
    //         LeftForeArm = GameObject.Find("LeftForeArm1");
    //         LeftHand = GameObject.Find("LeftHand1");
    //     }
    //    
    // }


    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= UpdateSeconds)
        {
            timer -= UpdateSeconds;
            UpdateJointsPositions();
            /*UpdateJointsRotations();*/
            //HideJointAndLine();
            currentFrame = (currentFrame + 1) % jointPositions.Count; 
        }
    }

    void UpdateJointsPositions()
    {
        Vector3[] positions = jointPositions[currentFrame];
        for (int i = 0; i < positions.Length; i++)
        {
            if (jointObjects[i] != null)
            {
                jointObjects[i].transform.position = positions[i] + positionOffsets;
            }
        }

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

        /*Hips.transform.position = jointObjects[0].transform.position;
        RightUpLeg.transform.position = jointObjects[1].transform.position;
        RightLeg.transform.position = jointObjects[2].transform.position; 
        RightFoot.transform.position = jointObjects[3].transform.position;
        LeftUpLeg.transform.position = jointObjects[4].transform.position;
        LeftLeg.transform.position = jointObjects[5].transform.position;
        LeftFoot.transform.position = jointObjects[6].transform.position;
        Spine.transform.position = jointObjects[7].transform.position;
        Spine1.transform.position = jointObjects[8].transform.position;
        Spine2.transform.position = jointObjects[9].transform.position;
        Neck.transform.position = jointObjects[10].transform.position;
        Neck1.transform.position = jointObjects[11].transform.position;
        Head.transform.position = jointObjects[12].transform.position;
        RightShoulder.transform.position = jointObjects[13].transform.position;
        RightArm.transform.position = jointObjects[14].transform.position;
        RightForeArm.transform.position = jointObjects[15].transform.position;
        RightHand.transform.position = jointObjects[16].transform.position;
        LeftShoulder.transform.position = jointObjects[17].transform.position;
        LeftArm.transform.position = jointObjects[18].transform.position;
        LeftForeArm.transform.position = jointObjects[19].transform.position;
        LeftHand.transform.position = jointObjects[20].transform.position;*/
    }

    /*void UpdateJointsRotations()
    {
        for (int i = 0; i < lineRenderers.Length; i++)
        {
            GameObject parentObj = null;
            GameObject childObj = null;

            switch (i)
            {
                case 0:
                    parentObj = Hips;
                    childObj = RightUpLeg;
                    break;
                case 1:
                    parentObj = RightUpLeg;
                    childObj = RightLeg;
                    break;
                case 2:
                    parentObj = RightLeg;
                    childObj = RightFoot;
                    break;
                case 3:
                    parentObj = Hips;
                    childObj = LeftUpLeg;
                    break;
                case 4:
                    parentObj = LeftUpLeg;
                    childObj = LeftLeg;
                    break;
                case 5:
                    parentObj = LeftLeg;
                    childObj = LeftFoot;
                    break;
                case 6:
                    parentObj = Hips;
                    childObj = Spine;
                    break;
                case 7:
                    parentObj = Spine;
                    childObj = Spine1;
                    break;
                case 8:
                    parentObj = Spine1;
                    childObj = Spine2;
                    break;
                case 9:
                    parentObj = Spine2;
                    childObj = Neck;
                    break;
                case 10:
                    parentObj = Neck;
                    childObj = Neck1;
                    break;
                case 11:
                    parentObj = Neck1;
                    childObj = Head;
                    break;
                case 12:
                    parentObj = Spine2;
                    childObj = RightShoulder;
                    break;
                case 13:
                    parentObj = RightShoulder;
                    childObj = RightArm;
                    break;
                case 14:
                    parentObj = RightArm;
                    childObj = RightForeArm;
                    break;
                case 15:
                    parentObj = RightForeArm;
                    childObj = RightHand;
                    break;
                case 16:
                    parentObj = Spine2;
                    childObj = LeftShoulder;
                    break;
                case 17:
                    parentObj = LeftShoulder;
                    childObj = LeftArm;
                    break;
                case 18:
                    parentObj = LeftArm;
                    childObj = LeftForeArm;
                    break;
                case 19:
                    parentObj = LeftForeArm;
                    childObj = LeftHand;
                    break;
                default:
                    break;
            }

            Vector3 start = lineRenderers[i].GetPosition(0);
            Vector3 end = lineRenderers[i].GetPosition(1);
            Vector3 direction = end - start;
            //Vector3 direction = parentObj.transform.position - childObj.transform.position;
            //Debug.Log("direction[" + i + "]: " + direction);

            switch (i)
            { 
                case 0:
                    break;
                case 1:
                    RightUpLeg.transform.up = -direction;
                    break;
                case 2:
                    RightLeg.transform.up = -direction;
                    break;
                case 3:
                    break;
                case 4:
                    LeftUpLeg.transform.up = -direction;
                    break;
                case 5:
                    LeftLeg.transform.up = -direction;
                    break;
                case 6:
                    Hips.transform.up = direction;
                    break;
                case 7:
                    Spine.transform.up = direction;
                    break;
                case 8:
                    Spine1.transform.up = direction;
                    break;
                case 9:
                    Spine2.transform.up = direction;     
                    break;
                case 10:
                    Neck.transform.up = direction;     
                    break;
                case 11:
                    Neck1.transform.up = direction;
                    break;
                case 12:
                    break;
                case 13:
                    RightShoulder.transform.right = direction;
                    break;
                case 14:
                    RightArm.transform.right = direction;
                    break;
                case 15:
                    RightForeArm.transform.right = direction;    
                    break;
                case 16:
                    break;
                case 17:
                    LeftShoulder.transform.right = -direction;
                    break;
                case 18:
                    LeftArm.transform.right = -direction;
                    break;
                case 19:
                    LeftForeArm.transform.right = -direction;
                    break;
                default:
                    break;
            }
        }
    }*/

    void HideJointAndLine()
    {
        for (int i = 0; i < jointPositions[currentFrame].Length; i++)
        {
            jointObjects[i].SetActive(false);
        }

        for (int i = 0; i < lineRenderers.Length; i++)
        {
            lineRenderers[i].enabled = false;
        }
    }

    public void ChangeSpeed(float newValue)
    {
        UpdateSeconds = newValue;
        Debug.Log("Updated speed: " + UpdateSeconds);
    }
}


