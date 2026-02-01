using UnityEngine;

/// <summary>
/// مدير Checkpoints - يتتبع آخر checkpoint مفعّل
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    private Checkpoint currentCheckpoint;

    void Awake()
    {
        // Singleton pattern - checkpoint manager واحد فقط
        CheckpointManager[] managers = FindObjectsOfType<CheckpointManager>();
        if (managers.Length > 1)
        {
            Debug.LogWarning("Multiple CheckpointManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("CheckpointManager initialized");
    }

    // ⭐ تسجيل checkpoint جديد
    public void SetCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null)
        {
            Debug.LogWarning("Attempted to set null checkpoint!");
            return;
        }

        currentCheckpoint = checkpoint;
        Debug.Log($"Checkpoint set to: {checkpoint.gameObject.name}");
    }

    // ⭐ الحصول على آخر checkpoint
    public Checkpoint GetCurrentCheckpoint()
    {
        return currentCheckpoint;
    }

    // دالة للتحقق من وجود checkpoint
    public bool HasCheckpoint()
    {
        return currentCheckpoint != null;
    }

    // دالة لإعادة ضبط جميع checkpoints (للتجربة)
    public void ResetAllCheckpoints()
    {
        Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();

        foreach (Checkpoint cp in checkpoints)
        {
            cp.ResetCheckpoint();
        }

        currentCheckpoint = null;

        Debug.Log("All checkpoints reset");
    }
}