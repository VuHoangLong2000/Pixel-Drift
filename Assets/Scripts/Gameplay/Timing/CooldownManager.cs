using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * COOLDOWN MANAGER
 *
 * Quản lý và truy xuất Cooldown bằng ID.
 *
 * Create:
 *
 * CooldownManager.Instance.Create(
 *     "Skill_Fireball",
 *     5f
 * );
 *
 * Get:
 *
 * CooldownManager.Instance.TryGet(
 *     "Skill_Fireball",
 *     out Cooldown cooldown
 * );
 *
 * Remove:
 *
 * CooldownManager.Instance.Remove(
 *     "Skill_Fireball"
 * );
 *
 * Clear:
 *
 * CooldownManager.Instance.Clear();
 *
 * Lưu ý:
 * - Không quản lý thời gian trực tiếp.
 * - Thời gian được xử lý bởi TimerManager.
 * - Không chứa gameplay logic.
 */
public class CooldownManager : MonoBehaviour
{
    public static CooldownManager Instance {  get; private set; }
    private readonly Dictionary<string, Cooldown> _cooldowns = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private Cooldown Create(string id, float duration)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        if (_cooldowns.TryGetValue(id, out Cooldown existing))
            return existing;

        Cooldown cooldown = new(duration);

        _cooldowns.Add(id, cooldown);

        return cooldown;
    }

    public bool TryGet(string id, out Cooldown cooldown)
    {
        return _cooldowns.TryGetValue(id, out cooldown);
    }
    public void Remove(string id)
    {
        if (!_cooldowns.TryGetValue(id, out Cooldown cooldown))
        {
            return;
        }
        cooldown.Stop();
        _cooldowns.Remove(id);
    }
    public void Clear()
    {
        foreach (Cooldown cooldown in _cooldowns.Values)
        {
            cooldown.Stop();
        }
        _cooldowns.Clear();
    }
    private void OnDestroy()
    {
        Clear();
    }
}
