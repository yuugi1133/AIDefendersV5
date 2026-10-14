using System;
using UnityEngine;
using UnityEngine.Events;

public class CCTVSkillContext
{
    public CCTVController Cctv;
    public Camera ViewCamera;
    public Transform Origin;
    public Ray AimRay;
}

public abstract class CCTVActiveSkill : MonoBehaviour
{
    public string displayName = "스킬";
    public float cooldown = 8f;
    public bool startCooldownOnUse = true;

    public UnityEvent onCooldownStarted;
    public UnityEvent onCooldownEnded;

    public event Action CooldownStarted;
    public event Action CooldownEnded;

    float readyAt = -999f;
    bool coolingDown;

    public bool IsReady
    {
        get { return Time.time >= readyAt; }
    }

    public virtual bool IsPrimed
    {
        get { return false; }
    }

    public float CooldownRemaining
    {
        get { return Mathf.Max(0f, readyAt - Time.time); }
    }

    public float CooldownNormalized
    {
        get
        {
            if (cooldown <= 0.01f)
                return 0f;
            return Mathf.Clamp01(CooldownRemaining / cooldown);
        }
    }

    public bool TryUse(CCTVSkillContext context)
    {
        if (!isActiveAndEnabled || !IsReady)
            return false;

        if (context == null || !CanUse(context))
            return false;

        OnUse(context);

        if (startCooldownOnUse)
            StartCooldown();

        return true;
    }

    protected virtual bool CanUse(CCTVSkillContext context)
    {
        return true;
    }

    protected abstract void OnUse(CCTVSkillContext context);

    public void StartCooldown()
    {
        float duration = Mathf.Max(0f, cooldown);
        readyAt = Time.time + duration;
        coolingDown = duration > 0.01f;
        CooldownStarted?.Invoke();
        if (onCooldownStarted != null)
            onCooldownStarted.Invoke();

        if (!coolingDown)
            NotifyCooldownEnded();
    }

    protected virtual void Update()
    {
        if (!coolingDown)
            return;

        if (IsReady)
            NotifyCooldownEnded();
    }

    void NotifyCooldownEnded()
    {
        coolingDown = false;
        CooldownEnded?.Invoke();
        if (onCooldownEnded != null)
            onCooldownEnded.Invoke();
    }
}
