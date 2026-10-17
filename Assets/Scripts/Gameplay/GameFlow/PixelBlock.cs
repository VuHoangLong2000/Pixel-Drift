using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PixelBlock : MonoBehaviour
{
    public string ColorId { get; private set; }
    public int HitPoints { get; private set; }
    public bool IsIndestructible { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsDestroyed { get; private set; }

    private PixelBlockManager _manager;
    private Renderer[] _renderers;

    public void Initialize(PixelBlockData data, PixelBlockManager manager)
    {
        if (data == null || manager == null)
        {
            Debug.LogError("[PixelBlock] Missing initialization data.", this);
            return;
        }

        _manager = manager;

        ColorId = data.colorId;
        HitPoints = Mathf.Max(1, data.hitPoints);
        IsIndestructible = data.isIndestructible;
        GridPosition = data.position;
        IsDestroyed = false;

        _renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in _renderers)
        {
            if (targetRenderer == null)
                continue;

            // Không sửa trực tiếp sharedMaterial của asset.
            targetRenderer.material.color = data.color;
        }

        gameObject.name = $"PixelBlock_{GridPosition.x}_{GridPosition.y}";
    }

    public bool TryDestroy()
    {
        if (IsDestroyed || IsIndestructible)
            return false;

        IsDestroyed = true;

        if (_manager != null)
            _manager.UnregisterBlock(this);

        Destroy(gameObject);
        return true;
    }
}
