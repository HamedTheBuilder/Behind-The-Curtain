using UnityEngine;
using System.Collections;

/// <summary>
/// نظام الموت - عقبات تقتل اللاعب مع تأثيرات
/// </summary>
public class DeathObstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] private float knockbackForce = 10f; // قوة الدفع عند الاصطدام
    [SerializeField] private Vector3 knockbackDirection = new Vector3(0, 5f, -5f); // اتجاه الدفع

    [Header("Layer")]
    [SerializeField] private LayerMask playerLayer;

    void OnCollisionEnter(Collision collision)
    {
        // التحقق من أنه اللاعب
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            PlayerDeath playerDeath = collision.gameObject.GetComponent<PlayerDeath>();

            if (playerDeath != null)
            {
                // ⭐ تطبيق قوة الدفع
                Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                }

                // ⭐ تفعيل الموت
                playerDeath.Die();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // نفس الشيء للـ Triggers
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            PlayerDeath playerDeath = other.GetComponent<PlayerDeath>();

            if (playerDeath != null)
            {
                Rigidbody playerRb = other.GetComponent<Rigidbody>();
                if (playerRb != null)
                {
                    playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                }

                playerDeath.Die();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.matrix = transform.localToWorldMatrix;

        // رسم العقبة
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider)
            {
                BoxCollider box = (BoxCollider)col;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider)
            {
                SphereCollider sphere = (SphereCollider)col;
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
        }
    }
}