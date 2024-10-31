using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus;

public class CameraRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("회전 속도")]
    public float rotationSpeed = 100f;

    [Header("Rotation Limits")]
    [Tooltip("상하 회전 제한 (Degrees)")]
    public float pitchLimit = 80f; // 상하 회전 제한
    private float currentPitch = 0f;

    void Update()
    {
        // 오른쪽 조이스틱 입력 받기 (Secondary Thumbstick)
        Vector2 input = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);

        // 디버그 로그 추가 (입력 확인용)
        Debug.Log("Right Thumbstick Input: " + input.ToString());

        // 입력이 있는지 확인
        if (input != Vector2.zero)
        {
            // Y축 입력은 좌우 회전에 사용 (Yaw)
            float yaw = input.x * rotationSpeed * Time.deltaTime;

            // X축 입력은 상하 회전에 사용 (Pitch)
            float pitchDelta = -input.y * rotationSpeed * Time.deltaTime;
            currentPitch += pitchDelta;
            currentPitch = Mathf.Clamp(currentPitch, -pitchLimit, pitchLimit);

            // OVRCameraRig 오브젝트 자체의 회전 적용
            // Yaw는 월드 스페이스 기준으로 회전
            transform.Rotate(0, yaw, 0, Space.World);

            // Pitch는 로컬 스페이스 기준으로 회전
            Vector3 localEuler = transform.localEulerAngles;
            // 로컬 EulerAngles는 0~360 범위이므로 -180~180으로 변환
            float adjustedPitch = localEuler.x > 180 ? localEuler.x - 360 : localEuler.x;
            adjustedPitch = currentPitch;
            localEuler.x = adjustedPitch;
            transform.localEulerAngles = localEuler;
        }
    }
}
