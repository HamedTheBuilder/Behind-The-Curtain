using UnityEngine;

public class HCAMERA : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(-10, 2, 0); // ✅ الكاميرا على اليسار
    public float smoothSpeed = 10f;

    [Header("Axis Following")]
    [Tooltip("هل تتبع اللاعب على محور X؟")]
    public bool followX = false; // ✅ X ثابت

    [Tooltip("هل تتبع اللاعب على محور Y؟")]
    public bool followY = true; // ✅ Y يتبع

    [Tooltip("هل تتبع اللاعب على محور Z؟")]
    public bool followZ = true; // ✅ Z يتبع (المحور الرئيسي)

    [Header("Boundaries (Optional)")]
    public bool useBoundaries = false;
    public float minX = -100f;
    public float maxX = 100f;
    public float minY = -100f;
    public float maxY = 100f;
    public float minZ = -100f;
    public float maxZ = 100f;

    [Header("Camera Rotation")]
    [Tooltip("هل تريد دوران الكاميرا ثابت؟")]
    public bool lockRotation = true;
    public Vector3 cameraRotation = new Vector3(0, 90, 0); // ✅ تنظر من الجانب

    void LateUpdate()
    {
        if (player == null)
            return;

        // Calculate desired position
        Vector3 desiredPosition = player.position + offset;

        // Apply axis following settings
        Vector3 targetPosition = transform.position;

        if (followX)
            targetPosition.x = desiredPosition.x;

        if (followY)
            targetPosition.y = desiredPosition.y;

        if (followZ)
            targetPosition.z = desiredPosition.z;

        // Apply boundaries if enabled
        if (useBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
            targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
        }

        // Smoothly move camera to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Handle camera rotation
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(cameraRotation);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        // رسم خط من الكاميرا للاعب
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, player.position);

        // رسم الموقع المستهدف
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position + offset, 0.5f);

        // رسم الحدود إذا كانت مفعلة
        if (useBoundaries)
        {
            Gizmos.color = Color.red;
            Vector3 center = new Vector3(
                (minX + maxX) / 2f,
                (minY + maxY) / 2f,
                (minZ + maxZ) / 2f
            );
            Vector3 size = new Vector3(
                maxX - minX,
                maxY - minY,
                maxZ - minZ
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}