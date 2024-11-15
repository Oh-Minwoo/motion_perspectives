using UnityEngine;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(OVREyeGaze))]
public class EyeTrackingDataRecorder : MonoBehaviour
{
    [Header("기록 설정")]
    [SerializeField]
    private bool recordEyeData = true;

    [SerializeField]
    private float frameRate = 30f; // 매 0.1초마다 기록
    private float recordInterval;
    
    [Header("저장 설정")]
    [SerializeField]
    public float saveInterval = 15f; // 매 15초마다 저장
    [SerializeField]
    public int maxBufferSize = 900; // 60 FPS * 15초

    [SerializeField]
    private string saveFolderName = "EyeTrackingData";

    private string saveFileName;

    public TransparentPlane transparentPlane;

    private OVREyeGaze eyeGaze;
    private Vector3 lastPosition;
    private float timeSinceLastRecord;
    private List<EyeDataRecord> currentBuffer = new List<EyeDataRecord>();
    private float timeSinceLastSave = 0f;

    public EyeDataRecord? LastRecordedData { get; private set; }
    public FullJoints fullJoints;
    private UnixTime _unixTime;
    public BlinkDetection blinkDetection;
    private EyeTrackingRay _eyeTrackingRay;

    private void Start()
    {
        saveFileName = $"{fullJoints.subName}_eye_tracking_data.csv";
        eyeGaze = GetComponent<OVREyeGaze>();
        _unixTime = GetComponent<UnixTime>();
        _eyeTrackingRay = GetComponent<EyeTrackingRay>();
        recordInterval = 1 / frameRate;

        if (transparentPlane == null)
        {
            Debug.LogError("TransparentPlane이 EyeTrackingDataRecorder에 할당되지 않았습니다!");
        }

        // CSV 파일 초기화 (헤더 추가)
        InitializeCSV();
    }

    private void Update()
    {
        if (recordEyeData && eyeGaze.EyeTrackingEnabled && transparentPlane != null)
        {
            RecordEyeData();
        }

        // 저장 타이머 업데이트
        if (recordEyeData)
        {
            timeSinceLastSave += Time.deltaTime;
            if (timeSinceLastSave >= saveInterval && currentBuffer.Count > 0)
            {
                SaveEyeDataToFile(currentBuffer);
                currentBuffer.Clear();
                timeSinceLastSave = 0f;
            }
        }
    }

    private void RecordEyeData()
    {
        timeSinceLastRecord += Time.deltaTime;
        

        if (timeSinceLastRecord >= recordInterval)
        {
            Vector3 currentPosition = _eyeTrackingRay.GetHitPoint();
            Vector3 velocity = (currentPosition - lastPosition) / timeSinceLastRecord;

            // 투명 평면에서의 위치 가져오기
            Vector2 planePosition = transparentPlane.WorldToPlanePosition(currentPosition);
            string unityTs = _unixTime.GetCurrentUnixTime();

            EyeDataRecord newRecord = new EyeDataRecord
            {
                timestamp = unityTs,
                worldPosition = currentPosition,
                planePosition = planePosition,
                velocity = velocity,
                eyeCloseLeft = blinkDetection.eyesClosedLeft,
                eyeCloseRight = blinkDetection.eyesClosedRight,
                confidence = eyeGaze.Confidence
            };

            currentBuffer.Add(newRecord);
            LastRecordedData = newRecord;

            lastPosition = currentPosition;
            timeSinceLastRecord = 0f;

            // 버퍼가 최대 크기에 도달했는지 확인
            if (currentBuffer.Count >= maxBufferSize)
            {
                SaveEyeDataToFile(currentBuffer);
                currentBuffer.Clear();
                timeSinceLastSave = 0f; // 저장 타이머 리셋
            }
        }
    }

    private void InitializeCSV()
    {
        string directoryPath = Path.Combine(Application.dataPath, saveFolderName);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string filePath = Path.Combine(directoryPath, saveFileName);

        if (!File.Exists(filePath))
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Timestamp,WorldX,WorldY,WorldZ,PlaneX,PlaneY,VelocityX,VelocityY,VelocityZ,BlinkLeft,BlinkRight,Confidence");
            }
            Debug.Log($"새 CSV 파일을 헤더와 함께 초기화했습니다: {filePath}");
        }
    }

    private void OnApplicationQuit()
    {
        if (recordEyeData && currentBuffer.Count > 0)
        {
            SaveEyeDataToFile(currentBuffer);
        }
    }

    private void SaveEyeDataToFile(List<EyeDataRecord> recordsToSave)
    {
        string directoryPath = Path.Combine(Application.dataPath, saveFolderName);
        string filePath = Path.Combine(directoryPath, saveFileName);

        using (StreamWriter writer = new StreamWriter(filePath, true)) // 추가 모드
        {
            foreach (var record in recordsToSave)
            {
                writer.WriteLine($"{record.timestamp}," +
                                 $"{record.worldPosition.x},{record.worldPosition.y},{record.worldPosition.z}," +
                                 $"{record.planePosition.x},{record.planePosition.y}," +
                                 $"{record.velocity.x},{record.velocity.y},{record.velocity.z}," +
                                 $"{record.eyeCloseLeft},{record.eyeCloseRight}," +
                                 $"{record.confidence}");
            }
        }
        Debug.Log($"{recordsToSave.Count}개의 시선 추적 데이터를 저장했습니다: {filePath}");
    }
}

public struct EyeDataRecord
{
    public string timestamp;
    public Vector3 worldPosition;
    public Vector2 planePosition;
    public Vector3 velocity;
    public float eyeCloseLeft;
    public float eyeCloseRight;
    public float confidence;
}
