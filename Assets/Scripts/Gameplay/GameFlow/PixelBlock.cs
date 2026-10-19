using System.Collections.Generic;
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

    // Mỗi projectile chỉ được đặt trước sát thương một lần.
    private readonly Dictionary<BulletProjectile, int> _damageReservations = new();

    public int ReservedDamage
    {
        get
        {
            int total = 0;

            foreach (int damage in _damageReservations.Values)
                total += damage;

            return total;
        }
    }

    public int AvailableHitPoints =>
        Mathf.Max(0, HitPoints - ReservedDamage);

    public void Initialize(PixelBlockData data, PixelBlockManager manager)
    {
        if (data == null || manager == null)
        {
            Debug.LogError("[PixelBlock] Missing initialization data.", this);
            return;
        }

        _damageReservations.Clear();

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

            targetRenderer.material.color = data.color;
        }

        gameObject.name = $"PixelBlock_{GridPosition.x}_{GridPosition.y}";
    }
    public void Invalidate()
    {
        IsDestroyed = true;
        _damageReservations.Clear();
        _manager = null;
    }
    public bool CanReserveDamage(int damage)
    {
        return isActiveAndEnabled
            && !IsDestroyed
            && !IsIndestructible
            && damage > 0
            && HitPoints > 0
            && AvailableHitPoints >= damage;
    }

    public bool HasDamageReservation(BulletProjectile projectile)
    {
        return projectile != null
            && _damageReservations.ContainsKey(projectile);
    }

    public bool TryReserveDamage(BulletProjectile projectile, int damage)
    {
        if (projectile == null)
            return false;

        // Cùng một projectile không thể đặt trước hai lần.
        if (_damageReservations.ContainsKey(projectile))
            return false;

        if (!CanReserveDamage(damage))
            return false;

        _damageReservations.Add(projectile, damage);
        return true;
    }

    public void ReleaseDamageReservation(BulletProjectile projectile)
    {
        if (ReferenceEquals(projectile, null))
            return;

        _damageReservations.Remove(projectile);
    }

    public bool TryApplyReservedDamage(BulletProjectile projectile)
    {
        if (projectile == null)
            return false;

        if (!_damageReservations.TryGetValue(projectile, out int damage))
            return false;

        // Tiêu thụ reservation trước khi áp dụng sát thương.
        // Nếu callback bị gọi lại, projectile không còn reservation.
        _damageReservations.Remove(projectile);

        if (!isActiveAndEnabled || IsDestroyed || IsIndestructible)
            return false;

        return TryTakeDamage(damage);
    }

    public bool TryTakeDamage(int damage = 1)
    {
        if (!isActiveAndEnabled
            || IsDestroyed
            || IsIndestructible
            || damage <= 0
            || HitPoints <= 0)
        {
            return false;
        }

        // Sát thương thông thường chỉ được dùng phần HP chưa đặt trước. Những viên đạn đã đặt trước vẫn giữ được phần HP của mình.
        int availableDamage = Mathf.Max(0, HitPoints - ReservedDamage);
        int actualDamage = Mathf.Min(damage, availableDamage);

        if (actualDamage <= 0)
            return false;

        HitPoints -= actualDamage;

        if (HitPoints <= 0)
            return TryDestroy();

        return true;
    }

    public bool TryDestroy()
    {
        if (IsDestroyed || IsIndestructible)
            return false;

        IsDestroyed = true;

        // Block không còn nhận sát thương. Các projectile đang bay sẽ tự kết thúc khi nhận ra target đã bị phá hủy.
        _damageReservations.Clear();

        if (_manager != null)
            _manager.UnregisterBlock(this);

        Destroy(gameObject);
        return true;
    }

    private void OnDisable()
    {
        // Tránh reservation bị giữ lại nếu block bị vô hiệu hóa hoặc bị hủy trong lúc còn projectile đang bay.
        _damageReservations.Clear();
    }
}