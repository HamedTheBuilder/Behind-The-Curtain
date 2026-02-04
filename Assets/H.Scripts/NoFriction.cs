using UnityEngine;

/// <summary>
/// يطبق No Friction تلقائياً - اللاعب ما يعلق على الجدران
/// </summary>
public class NoFriction : MonoBehaviour
{
    [Header("Physics Material")]
    [SerializeField] private PhysicsMaterial noFrictionMaterial;
    [SerializeField] private bool createMaterialAutomatically = true;
    
    [Header("Settings")]
    [SerializeField] private float friction = 0f; // صفر = لا احتكاك
    [SerializeField] private float bounciness = 0f; // صفر = لا ارتداد
    [SerializeField] private PhysicsMaterialCombine frictionCombine = PhysicsMaterialCombine.Minimum;
    [SerializeField] private PhysicsMaterialCombine bounceCombine = PhysicsMaterialCombine.Minimum;
    
    [Header("Apply To")]
    [SerializeField] private bool applyToAllColliders = true; // تطبيق على كل الكولايدرات
    
    void Start()
    {
        // إنشاء المادة إذا ما كانت موجودة
        if (noFrictionMaterial == null && createMaterialAutomatically)
        {
            CreateNoFrictionMaterial();
        }
        
        // تطبيق المادة
        ApplyMaterial();
    }
    
    void CreateNoFrictionMaterial()
    {
        noFrictionMaterial = new PhysicsMaterial("NoFriction");
        noFrictionMaterial.dynamicFriction = friction;
        noFrictionMaterial.staticFriction = friction;
        noFrictionMaterial.bounciness = bounciness;
        noFrictionMaterial.frictionCombine = frictionCombine;
        noFrictionMaterial.bounceCombine = bounceCombine;
        
        Debug.Log("✅ Created No Friction material");
    }
    
    void ApplyMaterial()
    {
        if (noFrictionMaterial == null)
        {
            Debug.LogError("No Friction Material is null!");
            return;
        }
        
        if (applyToAllColliders)
        {
            // تطبيق على كل الكولايدرات
            Collider[] colliders = GetComponentsInChildren<Collider>();
            
            foreach (Collider col in colliders)
            {
                col.material = noFrictionMaterial;
                Debug.Log($"Applied No Friction to: {col.gameObject.name}");
            }
        }
        else
        {
            // تطبيق على الكولايدر على نفس الأوبجكت فقط
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.material = noFrictionMaterial;
                Debug.Log($"Applied No Friction to: {gameObject.name}");
            }
        }
    }
    
    // دالة لتطبيق المادة من الكود
    public void ApplyNoFriction()
    {
        ApplyMaterial();
    }
}
