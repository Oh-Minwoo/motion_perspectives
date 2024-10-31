using System;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WindowMonitor : MonoBehaviour
{
    [DllImport("User32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    private bool wasWindowOpen = false;
    private string targetWindowName = "Posture Calibration";  // 감지할 창 이름
    private bool isTextOpen = false;

    [HideInInspector]
    public bool isWindowOpen;
    
    
    [SerializeField] private GameObject displayImage;
    [SerializeField] private GameObject instructions;
    void Start()
    {
        displayImage.SetActive(false);
        instructions.SetActive(false);
    }
    
    void Update()
    {
        // 창이 열려있는지 확인
        IntPtr windowHandle = FindWindow(null, targetWindowName);
        isWindowOpen = windowHandle != IntPtr.Zero;

        if (isWindowOpen && !wasWindowOpen)
        {
            Debug.Log("Window Open");
            OnWindowOpen();
        }

        if (!isWindowOpen && wasWindowOpen)
        {
            isTextOpen = true;
            OnWindowClosed();
        }
        
        // 창이 닫혔을 때만 동작 수행

        wasWindowOpen = isWindowOpen;
        if (!wasWindowOpen && isTextOpen)
        {
            if (OVRInput.GetDown(OVRInput.RawButton.A))
            {
                instructions.SetActive(false);
                isTextOpen = false;
            }
        }
    }

    private void OnWindowOpen()
    {
        displayImage.SetActive(true);
        instructions.SetActive(false);
    }

    private void OnWindowClosed()
    {
        displayImage.SetActive(false);
        instructions.SetActive(true);
    }
}