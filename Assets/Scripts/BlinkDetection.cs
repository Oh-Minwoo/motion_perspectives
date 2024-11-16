using UnityEngine;

public class BlinkDetection : MonoBehaviour
{
    private OVRFaceExpressions faceExpressions;
    [HideInInspector] public float eyesClosedLeft;
    [HideInInspector] public float eyesClosedRight;

    /*public GameObject cube1;
    public GameObject cube2;*/

    void Start()
    {
        // OVRFaceExpressions 컴포넌트 가져오기
        faceExpressions = GetComponent<OVRFaceExpressions>();
    }

    void Update()
    {
        if (faceExpressions.ValidExpressions)
        {
            // 왼쪽 눈을 감는 표정의 가중치 가져오기
            eyesClosedLeft = faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedL];
            
            // 오른쪽 눈을 감는 표정의 가중치 가져오기
            eyesClosedRight = faceExpressions[OVRFaceExpressions.FaceExpression.EyesClosedR];
            
            // 가중치를 사용하여 로직 구현
            /*if (eyesClosedLeft > 0.5f && eyesClosedRight < 0.5f)
            {

                var renderer1 = cube1.GetComponent<Renderer>();
                renderer1.material.color = Color.red;
                
                var renderer2 = cube2.GetComponent<Renderer>();
                renderer2.material.color = Color.white;

            }
            if (eyesClosedLeft < 0.5f && eyesClosedRight > 0.5f)
            {
                var renderer1 = cube1.GetComponent<Renderer>();
                renderer1.material.color = Color.white;
                
                var renderer2 = cube2.GetComponent<Renderer>();
                renderer2.material.color = Color.red;
            }
            if (eyesClosedLeft > 0.5f && eyesClosedRight > 0.5f)
            {
                var renderer1 = cube1.GetComponent<Renderer>();
                renderer1.material.color = Color.red;
                
                var renderer2 = cube2.GetComponent<Renderer>();
                renderer2.material.color = Color.red;
            }*/
        }
    }
}