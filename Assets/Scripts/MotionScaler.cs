using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Configuration;
using MathNet.Numerics.LinearAlgebra;

public class MotionScaler : MonoBehaviour
{
    // 조인트 계층 구조 정의
    private readonly List<(int Start, int End)> jointHierarchy = new List<(int, int)>
    {
        (0, 1),   // Hips to RightUpLeg
        (1, 2),   // RightUpLeg to RightLeg
        (2, 3),   // RightLeg to RightFoot
        (0, 4),   // Hips to LeftUpLeg
        (4, 5),   // LeftUpLeg to LeftLeg
        (5, 6),   // LeftLeg to LeftFoot
        (0, 7),   // Hips to Spine
        (7, 8),   // Spine to Spine1
        (8, 9),   // Spine1 to Spine2
        (9, 10),  // Spine2 to Neck
        (10, 11), // Neck to Neck1
        (11, 12), // Neck1 to Head
        (9, 13),  // Spine2 to RightShoulder
        (13, 14), // RightShoulder to RightArm
        (14, 15), // RightArm to RightForeArm
        (15, 16), // RightForeArm to RightHand
        (9, 17),  // Spine2 to LeftShoulder
        (17, 18), // LeftShoulder to LeftArm
        (18, 19), // LeftArm to LeftForeArm
        (19, 20)  // LeftForeArm to LeftHand
    };

    // 신체 부위 및 좌표 정의
    private readonly List<string> bodyParts = new List<string>
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
        "Neck1",
        "Neck",
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
    
    [Header("CSV 파일")]
    // public TextAsset armsGuidanceCSV; // arms_guidance.csv
    public TextAsset badmintonCSV; // arms_and_legs_guidance.csv
    public TextAsset frontalCSV;
    public TextAsset egocentricCSV;

    [Header("JSON 파일")]
    public TextAsset jointOffsetsJSON; // joint_offsets.json
    public TextAsset practiceJointOffsetsJSON;

    [Header("피험자 번호")]
    public string subjectNumber = "sub101";

    private readonly List<string> coords = new List<string> { "x", "y", "z" };

    // 초기화 메서드
    public void MotionScaling(List<float[]> calibrationList)
    {
        // 위치 컬럼 생성
        List<string> positionCols = bodyParts.SelectMany(part => coords.Select(c => $"{part}_{c}")).ToList();

        // JSON 파일 읽기
        var jointOffsets = ReadJointOffsets(jointOffsetsJSON.text);
        var practiceJointOffsets = ReadJointOffsets(practiceJointOffsetsJSON.text);

        // 가이드 신체 크기 계산
        List<double> guideBodySize = ComputeBodySize(jointOffsets);
        List<double> practiceBodySize = ComputeBodySize(practiceJointOffsets);

        

        // CSV 파일 읽기
        // var armsMotion = ReadCsv(armsGuidanceCSV.text, positionCols);
        var armsAndLegsMotion = ReadCsv(badmintonCSV.text, positionCols);
        var frontalMotion = ReadCsv(frontalCSV.text, positionCols);
        var egocentricMotion = ReadCsv(egocentricCSV.text, positionCols);

        // 피험자 보정 데이터 읽기
        var dfRawSub = calibrationList
            .Select(floatArray => floatArray.Select(f => (double)f).ToArray())
            .ToList();

        // 피험자 신체 크기 전처리
        List<double> subBodySize = Preprocess(dfRawSub, jointHierarchy);
        Debug.Log("피험자 신체 크기:");
        Debug.Log(string.Join(", ", subBodySize));
        Debug.Log("가이드 신체 크기:");
        Debug.Log(string.Join(", ", guideBodySize));

        // 모션 스케일링
        // var armsScaled = Scaling(armsMotion, guideBodySize, subBodySize, positionCols, jointHierarchy);
        var armsAndLegsScaled = Scaling(armsAndLegsMotion, guideBodySize, subBodySize, positionCols, jointHierarchy);
        var frontalScaled = Scaling(frontalMotion, practiceBodySize, subBodySize, positionCols, jointHierarchy);
        var egocentricScaled = Scaling(egocentricMotion, practiceBodySize, subBodySize, positionCols, jointHierarchy);


        // 스케일링된 데이터 CSV로 저장
        // string armsScaledPath = Path.Combine(Application.dataPath, $"Data/{subjectNumber}_arms_guidance.csv");
        string armsAndLegsScaledPath = Path.Combine(Application.dataPath, $"Data/{subjectNumber}_arms_and_leg_guidance.csv");
        string frontalScaledPath = Path.Combine(Application.dataPath, $"Data/{subjectNumber}_frontal.csv");
        string egocentricScaledPath = Path.Combine(Application.dataPath, $"Data/{subjectNumber}_egocentric.csv");


        // WriteCsv(armsScaled, armsScaledPath);
        WriteCsv(armsAndLegsScaled, armsAndLegsScaledPath);
        WriteCsv(frontalScaled, frontalScaledPath);
        WriteCsv(egocentricScaled, egocentricScaledPath);
        
        Debug.Log("스케일링 완료 및 파일 저장됨.");
    }

    /// <summary>
    /// JSON 파일에서 조인트 오프셋을 읽어옵니다.
    /// </summary>
    private Dictionary<string, double> ReadJointOffsets(string jsonText)
    {
        var data = JsonConvert.DeserializeObject<Dictionary<string, double>>(jsonText);
        return data;
    }

    /// <summary>
    /// 조인트 계층 구조와 오프셋을 기반으로 신체 크기를 계산합니다.
    /// </summary>
    private List<double> ComputeBodySize(Dictionary<string, double> jointOffsets)
    {
        List<double> bodySizes = new List<double>();
        for (int i=1; i < bodyParts.Count; i++)
        {
            string part = bodyParts[i];
            if (!jointOffsets.ContainsKey(part))
            {
                throw new Exception($"조인트 오프셋이 존재하지 않습니다: {part}");
            }
            bodySizes.Add(jointOffsets[part]);
        }

        return bodySizes;
    }
    // private List<double> ComputeBodySize(Dictionary<string, List<double>> jointOffsets, List<(int Start, int End)> hierarchy)
    // {
    //     List<double> bodySizes = new List<double>();
    //
    //     for (int i = 0; i < bodyParts.Count; i++)
    //     {
    //         string part = bodyParts[i];
    //         if (part == "Hips")
    //         {
    //             // Hips의 거리는 제외하거나 기본값을 설정
    //             continue;
    //         }
    //
    //         if (!jointOffsets.ContainsKey(part))
    //             throw new Exception($"조인트 오프셋이 존재하지 않습니다: {part}");
    //
    //         List<double> offset = jointOffsets[part];
    //         if (offset.Count != 3)
    //             throw new Exception($"조인트 오프셋 형식이 올바르지 않습니다: {part}");
    //
    //         double x = offset[0];
    //         double y = offset[1];
    //         double z = offset[2];
    //
    //         // (0,0,0)으로부터의 거리 계산 (유클리드 거리)
    //         double distance = Math.Sqrt(x * x + y * y + z * z) / 100.0; // cm to m
    //
    //         bodySizes.Add(distance);
    //     }
    //
    //     // Hips를 제외한 모든 조인트의 거리 출력 (디버깅 용도)
    //     Debug.Log("Joint Offsets Length: " + bodyParts.Count);
    //     Debug.Log("Body Length:" + bodySizes.Count);
    //     return bodySizes;
    // }

    /// <summary>
    /// CSV 파일을 읽어와서 각 프레임의 조인트 데이터를 반환합니다.
    /// </summary>
    private List<double[]> ReadCsv(string csvText, List<string> headers)
    {
        var records = new List<double[]>();

        using (var reader = new StringReader(csvText))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            // 헤더 읽기
            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                double[] row = new double[headers.Count];
                for (int i = 0; i < headers.Count; i++)
                {
                    row[i] = csv.GetField<double>(i);
                }
                records.Add(row);

                // 디버그 로그 (필요시 활성화)
                // Debug.Log($"읽은 행: {string.Join(", ", row)}");
            }
        }

        return records;
    }

    // /// <summary>
    // /// 피험자 보정 데이터를 읽고 전처리합니다.
    // /// </summary>
    // private List<double[]> ReadSubjectCalibration(string csvText, List<string> headers)
    // {
    //     var records = new List<double[]>();
    //
    //     // CsvConfiguration 설정 (HasHeaderRecord = false)
    //     var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    //     {
    //         HasHeaderRecord = false
    //     };
    //
    //     using (var reader = new StringReader(csvText))
    //     using (var csv = new CsvReader(reader, config))
    //     {
    //         while (csv.Read())
    //         {
    //             var row = new List<double>();
    //             for (int i = 0; i < headers.Count; i++)
    //             {
    //                 // 64번째 열 (인덱스 63) 제외
    //                 if (i == 63)
    //                     continue;
    //                 row.Add(csv.GetField<double>(i));
    //             }
    //             records.Add(row.ToArray());
    //
    //             // 디버그 로그 (필요시 활성화)
    //             // Debug.Log($"읽은 보정 데이터 행: {string.Join(", ", row)}");
    //         }
    //     }
    //
    //     return records;
    // }

    /// <summary>
    /// 피험자 보정 데이터를 전처리하여 신체 크기를 계산합니다.
    /// </summary>
    private List<double> Preprocess(List<double[]> df, List<(int Start, int End)> hierarchy)
    {
        int numJoints = bodyParts.Count;
        Vector<double>[] jointMeans = new Vector<double>[numJoints];

        for (int j = 0; j < numJoints; j++)
        {
            double sumX = 0, sumY = 0, sumZ = 0;
            foreach (var row in df)
            {
                sumX += row[j * 3];
                sumY += row[j * 3 + 1];
                sumZ += row[j * 3 + 2];
            }
            int count = df.Count;
            jointMeans[j] = Vector<double>.Build.Dense(new double[] { sumX / count / 100.0, sumY / count / 100.0, sumZ / count / 100.0 }); // cm to m
        }

        // 계층 구조에 따라 거리 계산
        List<double> bodySizes = new List<double>();
        foreach (var (start, end) in hierarchy)
        {
            double distance = (jointMeans[end] - jointMeans[start]).L2Norm();
            bodySizes.Add(distance);
        }

        return bodySizes;
    }

    /// <summary>
    /// 가이드 모션 데이터를 피험자의 신체 크기에 맞게 스케일링합니다.
    /// </summary>
    private List<double[]> Scaling(List<double[]> guideMotion, List<double> guideFeatures, List<double> subjectFeatures, List<string> columns, List<(int Start, int End)> hierarchy)
    {
        // 비율 계산
        List<double> ratio = guideFeatures.Select((g, i) => (subjectFeatures[i] - g) / g + 1.0).ToList();

        // 각 프레임에 대해 스케일링 적용
        List<double[]> scaledMotion = new List<double[]>();
        foreach (var frame in guideMotion)
        {
            double[] scaledFrame = ProcessFrame(frame, hierarchy, ratio);
            scaledMotion.Add(scaledFrame);
        }

        return scaledMotion;
    }

    /// <summary>
    /// 단일 프레임의 조인트 위치를 스케일링합니다.
    /// </summary>
    private double[] ProcessFrame(double[] frame, List<(int Start, int End)> hierarchy, List<double> ratio)
    {
        int numJoints = bodyParts.Count;
        Vector<double>[] jointList = new Vector<double>[numJoints];
        for (int i = 0; i < numJoints; i++)
        {
            jointList[i] = Vector<double>.Build.Dense(new double[] { frame[i * 3], frame[i * 3 + 1], frame[i * 3 + 2] });
        }

        // 스케일링된 조인트 위치 초기화
        Vector<double>[] scaledJoints = new Vector<double>[numJoints];
        for (int i = 0; i < numJoints; i++)
        {
            scaledJoints[i] = Vector<double>.Build.Dense(3);
        }

        // 루트 조인트 설정 (가정: 조인트 0이 루트)
        scaledJoints[0] = jointList[0].Clone();

        // 계층 구조에 따라 각 조인트 스케일링
        for (int idx = 0; idx < hierarchy.Count; idx++)
        {
            var (start, end) = hierarchy[idx];
            Vector<double> direction = jointList[end] - jointList[start];
            double magnitude = direction.L2Norm();
            Vector<double> normalizedDirection = magnitude == 0 ? Vector<double>.Build.Dense(3) : direction / magnitude;
            double scaledMagnitude = ratio[idx] * magnitude;
            Vector<double> scaledVectorEnd = scaledJoints[start] + scaledMagnitude * normalizedDirection;
            scaledJoints[end] = scaledVectorEnd;
        }

        // 스케일링된 조인트 위치를 단일 배열로 변환
        List<double> flattened = new List<double>();
        foreach (var joint in scaledJoints)
        {
            flattened.Add(joint[0]);
            flattened.Add(joint[1]);
            flattened.Add(joint[2]);
        }

        return flattened.ToArray();
    }

    /// <summary>
    /// 스케일링된 모션 데이터를 CSV 파일로 저장합니다.
    /// </summary>
    private void WriteCsv(List<double[]> data, string filePath)
    {
        try
        {
            // 파일이 이미 존재하는지 확인하고, 존재하면 삭제
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"기존 파일을 삭제했습니다: {filePath}");
            }

            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // 헤더 작성

                // 데이터 작성
                foreach (var row in data)
                {
                    foreach (var field in row)
                    {
                        csv.WriteField(field);
                    }
                    csv.NextRecord();
                }
            }
            Debug.Log($"CSV 파일이 성공적으로 저장되었습니다: {filePath}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.LogError($"파일에 접근할 권한이 없습니다: {filePath}. 오류: {ex.Message}");
        }
        catch (IOException ex)
        {
            Debug.LogError($"파일 삭제 또는 쓰기 중 I/O 오류가 발생했습니다: {filePath}. 오류: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"CSV 파일 저장 중 예기치 않은 오류가 발생했습니다: {filePath}. 오류: {ex.Message}");
        }
    }
}
