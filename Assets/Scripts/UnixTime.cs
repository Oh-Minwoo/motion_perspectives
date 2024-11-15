using System;
using UnityEngine;

public static class UnixTimeHelper
{
    // Unix Epoch (1970-01-01T00:00:00Z)
    private static readonly DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // 현재 Unix Time을 초 단위의 부동 소수점 숫자로 반환
    public static double GetCurrentUnixTime()
    {
        return (DateTimeOffset.UtcNow - epoch).TotalSeconds;
    }
}

public class UnixTime : MonoBehaviour
{
    public string GetCurrentUnixTime()
    {
        double currentUnixTime = UnixTimeHelper.GetCurrentUnixTime();
        /*Debug.Log("현재 Unix Time (초, 부동 소수점): " + currentUnixTime.ToString("F6"));*/ // 소수점 이하 6자리까지 표시
        return currentUnixTime.ToString("F6");
    }
}