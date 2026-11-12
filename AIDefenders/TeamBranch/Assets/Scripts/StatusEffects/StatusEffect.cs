using UnityEngine;

public abstract class StatusEffect
{
    public string effectId = "status";
    public float duration = 5f;
    public bool allowMultiple;
    public bool refreshDuration = true;

    Target_Base owner;
    float remaining;

    public Target_Base Owner
    {
        get { return owner; }
    }

    public float Remaining
    {
        get { return remaining; }
    }

    public bool IsExpired
    {
        get { return duration > 0f && remaining <= 0f; }
    }

    internal void Bind(Target_Base target)
    {
        owner = target;
        remaining = Mathf.Max(0f, duration);
        OnApplied(target);
    }

    internal void RefreshFrom(StatusEffect source)
    {
        if (source != null && refreshDuration)
            remaining = Mathf.Max(remaining, source.duration);
        OnRefreshed(source);
    }

    internal void Unbind()
    {
        if (owner != null)
            OnRemoved(owner);
        owner = null;
    }

    internal bool Tick(float deltaTime)
    {
        if (owner == null)
            return false;

        OnTick(owner, deltaTime);

        if (duration <= 0f)
            return true;

        remaining -= deltaTime;
        return remaining > 0f;
    }

    protected virtual void OnApplied(Target_Base target)
    {
    }

    protected virtual void OnRefreshed(StatusEffect source)
    {
    }

    protected virtual void OnTick(Target_Base target, float deltaTime)
    {
    }

    protected virtual void OnRemoved(Target_Base target)
    {
    }

    public virtual float ModifyMoveSpeed(float speed)
    {
        return speed;
    }
}
