using UnityEngine;
using UnityEngine.UI;

public class OBSVirtualCameraDisplay : MonoBehaviour
{
    public RawImage displayImage;  // UI의 RawImage 컴포넌트에 연결
    private WebCamTexture webcamTexture;

    void Start()
    {
        // OBS 가상 카메라 장치 찾기
        foreach (var device in WebCamTexture.devices)
        {
            if (device.name == "OBS Virtual Camera")
            {
                webcamTexture = new WebCamTexture(device.name);
                displayImage.texture = webcamTexture;
                webcamTexture.Play();
                break;
            }
        }
    }

    void OnDisable()
    {
        // 필요하지 않으면 재생 중지
        if (webcamTexture != null)
        {
            webcamTexture.Stop();
        }
    }
}