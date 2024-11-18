using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class EyeTrackingRay : MonoBehaviour
{
    [SerializeField]
    private float rayDistance = 1.0f;

    [SerializeField]
    private float rayWidth = 0.01f;

    [SerializeField]
    private LayerMask layersToInclude;
    
    private LineRenderer lineRenderer;
    
    private List<EyeInteractable> eyeInteractables = new List<EyeInteractable>();
    
    private Vector3 hitPoint;
    
    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupRay();
    }

    void SetupRay()
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        

        // 투명 Material 설정
        Material defaultMaterial = new Material(Shader.Find("Standard"));
        defaultMaterial.SetFloat("_Mode", 3); // Transparent
        defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        defaultMaterial.SetInt("_ZWrite", 0);
        defaultMaterial.DisableKeyword("_ALPHATEST_ON");
        defaultMaterial.EnableKeyword("_ALPHABLEND_ON");
        defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        defaultMaterial.renderQueue = 3000;
        defaultMaterial.color = new Color(1f, 1f, 1f, 0f); // Adjustable transparency
        lineRenderer.material = defaultMaterial;
        
        /*lineColor.a = 0.5f;
        transparentMaterial.color = lineColor;*/

        // LineRenderer에 Material 적용


        
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * rayDistance);
    }
    
    void FixedUpdate()
    {
        PerformRaycast();
    }


    void PerformRaycast()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, origin + direction * rayDistance);
        
        // 로컬 좌표계 기준으로 설정
        /*lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, new Vector3(0, 0, rayDistance));*/

        // Raycast 방향도 로컬 좌표계를 기준으로 설정
        RaycastHit hit;
        /*Vector3 rayCastDirection = transform.TransformDirection(Vector3.forward); // 로컬 기준*/
        
        bool isHit = Physics.Raycast(origin, direction, out hit, rayDistance, layersToInclude);
        if (isHit)
        {
            // 충돌 지점의 글로벌 위치 가져오기
            hitPoint = hit.point;

            // LineRenderer의 끝점을 충돌 지점으로 설정하여 시각적으로 표시
            lineRenderer.SetPosition(1, hitPoint);


        }
        else
        {
            // 충돌이 없을 경우, LineRenderer의 끝점을 최대 거리로 설정
            lineRenderer.SetPosition(1, origin + direction * rayDistance);
        }
        
        /*if (Physics.Raycast(transform.position, rayCastDirection, out hit, rayDistance, layersToInclude))
        {
            UnSelect();
            var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
            if (eyeInteractable != null && !eyeInteractables.Contains(eyeInteractable))
            {
                eyeInteractables.Add(eyeInteractable);
                eyeInteractable.IsHovered = true;
            }
        }
        else
        {
            UnSelect(true);
        }*/
    }
    
    
    /// <summary>
    /// 충돌 지점의 글로벌 위치를 반환합니다.
    /// </summary>
    /// <returns>충돌 지점의 Vector3 위치</returns>
    public Vector3 GetHitPoint()
    {
        return hitPoint;
    }

    void UnSelect(bool clear = false)
    {
        foreach (var interactable in eyeInteractables)
        {
            interactable.IsHovered = false;
        }
        if (clear)
        {
            eyeInteractables.Clear();
        }
    }
}
