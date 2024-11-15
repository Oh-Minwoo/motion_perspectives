using UnityEngine;

public class TransparentPlane : MonoBehaviour
{
    [Header("Plane Settings")]
    [Tooltip("Width of the plane.")]
    public float width = 2f;

    [Tooltip("Height of the plane.")]
    public float height = 2f;

    [Tooltip("Distance from the parent object.")]
    private float distanceFromCamera;

    private GameObject plane;


    private void Start()
    {
        distanceFromCamera = Vector3.Distance(transform.position, new Vector3(-99f, transform.position.y, 0f));
        CreateTransparentPlane();
    }

    private void CreateTransparentPlane()
    {
        // Create a quad
        plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.transform.SetParent(transform, false);

        // Set the size
        plane.transform.localScale = new Vector3(width, height, 1f);

        // Position in front of the parent object
        plane.transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);

        // Make it face the parent object's forward direction
        plane.transform.localRotation = Quaternion.identity;

        plane.layer = 3;
        
        // Make it transparent
        Renderer renderer = plane.GetComponent<Renderer>();
        
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
        renderer.material = defaultMaterial;

        Collider existingCollider = plane.GetComponent<Collider>();
        if (existingCollider != null)
        {
            Destroy(existingCollider); // 기존 MeshCollider 제거
        }

        // Add a BoxCollider and set its size to match the plane's scale
        BoxCollider boxCollider = plane.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(width, height, 0.01f); // Quad의 스케일이 (width, height, 1f)이므로 BoxCollider는 (1,1,0.01f)로 설정
    }

    /// <summary>
    /// Converts a world position to 2D coordinates on the plane.
    /// </summary>
    /// <param name="worldPosition">The world position to convert.</param>
    /// <returns>2D coordinates on the plane.</returns>
    public Vector2 WorldToPlanePosition(Vector3 worldPosition)
    {
        // Convert world position to local position relative to the plane
        Vector3 localPos = plane.transform.InverseTransformPoint(worldPosition);

        // Normalize the local position based on the plane's size
        float normalizedX = Mathf.Clamp((localPos.x / width) + 0.5f, 0f, 1f);
        float normalizedY = Mathf.Clamp((localPos.y / height) + 0.5f, 0f, 1f);

        // Convert to 2D plane coordinates
        return new Vector2(normalizedX * width, normalizedY * height);
    }
}
