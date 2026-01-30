using UnityEngine;

public class HCAMERA : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(0, 2, -10);
    public float smoothSpeed = 10f;

    [Header("Boundaries (Optional)")]
    public bool useBoundaries = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minY = -100f;
    public float maxY = 100f;

    void LateUpdate()
    {
        if (player == null)
            return;

        // Calculate desired position
        Vector3 desiredPosition = player.position + offset;

        // Apply boundaries if enabled
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Smoothly move camera to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Keep camera rotation fixed (never rotate with player)
        // You can adjust this if you want a specific camera angle
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}