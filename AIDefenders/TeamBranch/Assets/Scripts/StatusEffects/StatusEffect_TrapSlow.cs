public class StatusEffect_TrapSlow : StatusEffect
{
    public float speedScale = 0.25f;

    public StatusEffect_TrapSlow(float durationSeconds, float remainingSpeedScale)
    {
        effectId = "trap_slow";
        duration = durationSeconds;
        speedScale = remainingSpeedScale;
        allowMultiple = false;
        refreshDuration = true;
    }

    public override float ModifyMoveSpeed(float speed)
    {
        return speed * speedScale;
    }
}
