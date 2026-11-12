using UnityEngine;

public class StatusEffect_Disarm : StatusEffect
{
    public const string Id = "plasma_disarm";
    const float MaxStrip = 0.20f;

    public float step = 0.02f;

    float baseArmor;
    float strippedRatio;
    float lastShredTime = -999f;
    bool capturedArmor;

    public StatusEffect_Disarm(float shredStep)
    {
        effectId = Id;
        duration = 0f;
        step = Mathf.Max(0.02f, shredStep);
        allowMultiple = false;
        refreshDuration = false;
    }

    protected override void OnApplied(Target_Base target)
    {
        CaptureArmor(target);
    }

    protected override void OnRefreshed(StatusEffect sourceEffect)
    {
        StatusEffect_Disarm other = sourceEffect as StatusEffect_Disarm;
        if (other != null && other.step > step)
            step = other.step;
    }

    public void TryShred(Target_Base target, float shredStep)
    {
        if (target == null)
            return;

        CaptureArmor(target);
        if (shredStep > step)
            step = shredStep;
        if (Time.time - lastShredTime < 0.05f)
            return;

        lastShredTime = Time.time;
        strippedRatio = Mathf.Min(MaxStrip, strippedRatio + step);
        target.armorValue = Mathf.Max(0f, baseArmor * (1f - strippedRatio));
    }

    protected override void OnRemoved(Target_Base target)
    {
        if (target != null && capturedArmor)
            target.armorValue = baseArmor;
    }

    void CaptureArmor(Target_Base target)
    {
        if (capturedArmor || target == null)
            return;

        baseArmor = target.armorValue;
        capturedArmor = true;
    }
}
