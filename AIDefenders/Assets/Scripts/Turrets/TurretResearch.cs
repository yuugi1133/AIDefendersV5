using System;
using UnityEngine;

public class TurretResearchDef
{
    public string id;
    public string group;
    public string displayName;
    public string description;
    public int cost;
    public string[] requires;
    public float bulletSpeed;
    public float range;
    public float armorPierce;
    public float spreadAngle;
    public float reloadTime;
    public bool weakspot;
    public bool scrapAmmo;
    public float overheatCoolBonus;
    public bool emergencyVent;
    public float burnChance;
    public float killFundsBonus;
    public float heatFeedback;
    public float friendlyDamageReduce;
    public float armorShred;
    public bool vacuumPull;
    public float explosionRadius;
    public float fireRate;
    public float reloadMul;
    public float homingTurnRate;
    public bool overpenDirect;
    public bool topAttack;
}

public static class TurretResearch
{
    public const string PrecisionShot = "PrecisionShot";
    public const string MachineGun = "MachineGun";

    public const string HighVelocity = "precision_high_velocity";
    public const string Scope = "precision_scope";
    public const string Ap1 = "precision_ap1";
    public const string Ap2 = "precision_ap2";
    public const string Weakspot = "precision_weakspot";

    public const string MgStable1 = "mg_stable_1";
    public const string MgStable2 = "mg_stable_2";
    public const string MgStable3 = "mg_stable_3";
    public const string MgReload1 = "mg_reload_1";
    public const string MgReload2 = "mg_reload_2";
    public const string MgScrap = "mg_scrap_ammo";

    public const string Laser = "Laser";
    public const string LaserCool1 = "laser_cool_1";
    public const string LaserCool2 = "laser_cool_2";
    public const string LaserVent = "laser_vent";
    public const string LaserBurn1 = "laser_burn_1";
    public const string LaserBurn2 = "laser_burn_2";
    public const string LaserCutter1 = "laser_cutter_1";
    public const string LaserCutter2 = "laser_cutter_2";
    public const string LaserFeedback = "laser_feedback";

    public const string Plasma = "Plasma";
    public const string PlasmaNano = "plasma_nano";
    public const string PlasmaControl1 = "plasma_control_1";
    public const string PlasmaControl2 = "plasma_control_2";
    public const string PlasmaControl3 = "plasma_control_3";
    public const string PlasmaErosion1 = "plasma_erosion_1";
    public const string PlasmaErosion2 = "plasma_erosion_2";
    public const string PlasmaVacuum = "plasma_vacuum";

    public const string Missile = "Missile";
    public const string MissileAt1 = "missile_at_1";
    public const string MissileAt2 = "missile_at_2";
    public const string MissileAt3 = "missile_at_3";
    public const string MissileOverpen = "missile_overpen";
    public const string MissileHeatSeek = "missile_heat_seek";
    public const string MissileTopAttack = "missile_top_attack";
    public const string MissileSaturation = "missile_saturation";

    static readonly TurretResearchDef[] All =
    {
        new TurretResearchDef
        {
            id = HighVelocity,
            group = PrecisionShot,
            displayName = "고속 탄환",
            description = "정밀 사격 탄속 +60",
            cost = 300,
            bulletSpeed = 60f
        },
        new TurretResearchDef
        {
            id = Scope,
            group = PrecisionShot,
            displayName = "개선된 조준경",
            description = "사거리 +10",
            cost = 800,
            requires = new[] { HighVelocity },
            range = 10f
        },
        new TurretResearchDef
        {
            id = Ap1,
            group = PrecisionShot,
            displayName = "철갑탄 I",
            description = "AP +0.5",
            cost = 950,
            requires = new[] { HighVelocity },
            armorPierce = 0.5f
        },
        new TurretResearchDef
        {
            id = Ap2,
            group = PrecisionShot,
            displayName = "철갑탄 II",
            description = "AP +0.5",
            cost = 1200,
            requires = new[] { Ap1 },
            armorPierce = 0.5f
        },
        new TurretResearchDef
        {
            id = Weakspot,
            group = PrecisionShot,
            displayName = "약점 파악 시스템",
            description = "식별된 적에게 신뢰도 비례 고정 피해",
            cost = 3000,
            requires = new[] { Ap2, Scope },
            weakspot = true
        },
        new TurretResearchDef
        {
            id = MgStable1,
            group = MachineGun,
            displayName = "안정된 사격 I",
            description = "탄퍼짐 -2.5°",
            cost = 700,
            spreadAngle = -2.5f
        },
        new TurretResearchDef
        {
            id = MgStable2,
            group = MachineGun,
            displayName = "안정된 사격 II",
            description = "탄퍼짐 -2.5°",
            cost = 1300,
            requires = new[] { MgStable1 },
            spreadAngle = -2.5f
        },
        new TurretResearchDef
        {
            id = MgStable3,
            group = MachineGun,
            displayName = "안정된 사격 III",
            description = "탄퍼짐 -2.5°",
            cost = 1800,
            requires = new[] { MgStable2 },
            spreadAngle = -2.5f
        },
        new TurretResearchDef
        {
            id = MgReload1,
            group = MachineGun,
            displayName = "신속 장전 장치 I",
            description = "재장전 -1.0초",
            cost = 1200,
            requires = new[] { MgStable1 },
            reloadTime = -1f
        },
        new TurretResearchDef
        {
            id = MgReload2,
            group = MachineGun,
            displayName = "신속 장전 장치 II",
            description = "재장전 -1.0초",
            cost = 1500,
            requires = new[] { MgReload1 },
            reloadTime = -1f
        },
        new TurretResearchDef
        {
            id = MgScrap,
            group = MachineGun,
            displayName = "급조 탄약 제조기",
            description = "적 처치 시 장탄 +2~4",
            cost = 2500,
            requires = new[] { MgStable2, MgReload1 },
            scrapAmmo = true
        },
        new TurretResearchDef
        {
            id = LaserCool1,
            group = Laser,
            displayName = "예비 냉각 장치 I",
            description = "과열 상태 시 냉각 속도 +25%",
            cost = 1400,
            overheatCoolBonus = 0.25f
        },
        new TurretResearchDef
        {
            id = LaserCool2,
            group = Laser,
            displayName = "예비 냉각 장치 II",
            description = "과열 상태 시 냉각 속도 +50%",
            cost = 1800,
            requires = new[] { LaserCool1 },
            overheatCoolBonus = 0.50f
        },
        new TurretResearchDef
        {
            id = LaserVent,
            group = Laser,
            displayName = "긴급 방열",
            description = "과열 중 0.5초마다 5% 확률로 과열 해제",
            cost = 3300,
            requires = new[] { LaserCool2 },
            emergencyVent = true
        },
        new TurretResearchDef
        {
            id = LaserBurn1,
            group = Laser,
            displayName = "고열 광선 I",
            description = "도트 적중 시 5% 확률로 화상(3초, 초당 고정 50)",
            cost = 1500,
            burnChance = 0.05f
        },
        new TurretResearchDef
        {
            id = LaserCutter1,
            group = Laser,
            displayName = "산업용 고열 절단기 I",
            description = "직접 처치 자금 +20%",
            cost = 1400,
            requires = new[] { LaserBurn1 },
            killFundsBonus = 0.20f
        },
        new TurretResearchDef
        {
            id = LaserBurn2,
            group = Laser,
            displayName = "고열 광선 II",
            description = "화상 부여 확률 30%",
            cost = 1800,
            requires = new[] { LaserBurn1 },
            burnChance = 0.30f
        },
        new TurretResearchDef
        {
            id = LaserCutter2,
            group = Laser,
            displayName = "산업용 고열 절단기 II",
            description = "직접 처치 자금 +20%",
            cost = 1400,
            requires = new[] { LaserCutter1 },
            killFundsBonus = 0.20f
        },
        new TurretResearchDef
        {
            id = LaserFeedback,
            group = Laser,
            displayName = "과열 피드백",
            description = "과열 수치에 따라 피해·AP 최대 +100%",
            cost = 4600,
            requires = new[] { LaserBurn2, LaserCool2 },
            heatFeedback = 1f
        },
        new TurretResearchDef
        {
            id = PlasmaNano,
            group = Plasma,
            displayName = "나노 기술 도입",
            description = "장갑 관통력 +0.2",
            cost = 700,
            armorPierce = 0.2f
        },
        new TurretResearchDef
        {
            id = PlasmaControl1,
            group = Plasma,
            displayName = "입자 컨트롤 I",
            description = "아군으로 식별된 대상 피해 -30%",
            cost = 1200,
            requires = new[] { PlasmaNano },
            friendlyDamageReduce = 0.30f
        },
        new TurretResearchDef
        {
            id = PlasmaControl2,
            group = Plasma,
            displayName = "입자 컨트롤 II",
            description = "아군으로 식별된 대상 피해 -30%",
            cost = 1500,
            requires = new[] { PlasmaControl1 },
            friendlyDamageReduce = 0.30f
        },
        new TurretResearchDef
        {
            id = PlasmaControl3,
            group = Plasma,
            displayName = "입자 컨트롤 III",
            description = "아군으로 식별된 대상 피해 -30%",
            cost = 1900,
            requires = new[] { PlasmaControl2 },
            friendlyDamageReduce = 0.30f
        },
        new TurretResearchDef
        {
            id = PlasmaErosion1,
            group = Plasma,
            displayName = "반물질 침식 I",
            description = "적 식별 대상에 무장해제. 플라즈마 피격 시 AV -2%, 최대 -20%",
            cost = 1900,
            requires = new[] { PlasmaNano },
            armorShred = 0.02f
        },
        new TurretResearchDef
        {
            id = PlasmaErosion2,
            group = Plasma,
            displayName = "반물질 침식 II",
            description = "무장해제 장갑 감소 -2% → -4%",
            cost = 2600,
            requires = new[] { PlasmaErosion1 },
            armorShred = 0.04f
        },
        new TurretResearchDef
        {
            id = PlasmaVacuum,
            group = Plasma,
            displayName = "진공 수축",
            description = "착탄 지점으로 적 식별 대상을 약하게 끌어당김",
            cost = 2500,
            requires = new[] { PlasmaControl1 },
            vacuumPull = true
        },
        new TurretResearchDef
        {
            id = MissileAt1,
            group = Missile,
            displayName = "대전차 특화 I",
            description = "관통력 +0.3, 폭발 범위 -20%",
            cost = 1000,
            armorPierce = 0.3f,
            explosionRadius = -0.20f
        },
        new TurretResearchDef
        {
            id = MissileAt2,
            group = Missile,
            displayName = "대전차 특화 II",
            description = "관통력 +0.3, 폭발 범위 -20%",
            cost = 1400,
            requires = new[] { MissileAt1 },
            armorPierce = 0.3f,
            explosionRadius = -0.20f
        },
        new TurretResearchDef
        {
            id = MissileAt3,
            group = Missile,
            displayName = "대전차 특화 III",
            description = "관통력 +0.3, 폭발 범위 -20%",
            cost = 1900,
            requires = new[] { MissileAt2 },
            armorPierce = 0.3f,
            explosionRadius = -0.20f
        },
        new TurretResearchDef
        {
            id = MissileOverpen,
            group = Missile,
            displayName = "과관통 로켓",
            description = "직격 피해의 AP-AV 보정 상한을 200%까지 확장",
            cost = 2800,
            requires = new[] { MissileAt3 },
            overpenDirect = true
        },
        new TurretResearchDef
        {
            id = MissileHeatSeek,
            group = Missile,
            displayName = "열추적 컴퓨터",
            description = "미사일에 약한 유도 추가",
            cost = 1900,
            requires = new[] { MissileAt1 },
            homingTurnRate = 80f
        },
        new TurretResearchDef
        {
            id = MissileTopAttack,
            group = Missile,
            displayName = "탑 어택",
            description = "45도 상공 발사 후 상승, 대상 상부를 향해 강유도 강하",
            cost = 3400,
            requires = new[] { MissileHeatSeek, MissileAt2 },
            topAttack = true,
            homingTurnRate = 160f
        },
        new TurretResearchDef
        {
            id = MissileSaturation,
            group = Missile,
            displayName = "포화",
            description = "발사 속도 +100%, 재장전 시간 -50%",
            cost = 2200,
            requires = new[] { MissileAt1 },
            fireRate = 1f,
            reloadMul = -0.50f
        }
    };

    public static TurretResearchDef[] ForGroup(string group)
    {
        if (string.IsNullOrEmpty(group))
            return Array.Empty<TurretResearchDef>();

        int count = 0;
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].group == group)
                count++;
        }

        TurretResearchDef[] list = new TurretResearchDef[count];
        int write = 0;
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].group != group)
                continue;
            list[write] = All[i];
            write++;
        }

        return list;
    }

    public static string GroupOf(TurretController turret)
    {
        if (turret == null)
            return "";
        if (!string.IsNullOrWhiteSpace(turret.researchGroup))
            return turret.researchGroup;
        return GroupOfPrefab(turret.sourcePrefab != null ? turret.sourcePrefab : turret.gameObject);
    }

    public static string GroupOfPrefab(GameObject prefab)
    {
        if (prefab == null)
            return "";

        TurretController turret = prefab.GetComponentInChildren<TurretController>(true);
        if (turret != null && !string.IsNullOrWhiteSpace(turret.researchGroup))
            return turret.researchGroup;

        string name = prefab.name;
        if (name.IndexOf("Precision", StringComparison.OrdinalIgnoreCase) >= 0)
            return PrecisionShot;
        if (name.IndexOf("MachineGun", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("Turret_MG", StringComparison.OrdinalIgnoreCase) >= 0)
            return MachineGun;
        if (name.IndexOf("Laser", StringComparison.OrdinalIgnoreCase) >= 0)
            return Laser;
        if (name.IndexOf("Plasma", StringComparison.OrdinalIgnoreCase) >= 0)
            return Plasma;
        if (name.IndexOf("Missile", StringComparison.OrdinalIgnoreCase) >= 0)
            return Missile;
        return "";
    }

    public static TurretResearchDef Find(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].id == id)
                return All[i];
        }

        return null;
    }

    public static bool IsComplete(string id)
    {
        return GameProgress.HasResearch(id);
    }

    public static bool PrerequisitesMet(TurretResearchDef research)
    {
        if (research == null)
            return false;
        if (research.requires == null || research.requires.Length == 0)
            return true;

        for (int i = 0; i < research.requires.Length; i++)
        {
            if (!IsComplete(research.requires[i]))
                return false;
        }

        return true;
    }

    public static string PrerequisiteLabel(TurretResearchDef research)
    {
        if (research == null || research.requires == null || research.requires.Length == 0)
            return "";

        string label = "";
        for (int i = 0; i < research.requires.Length; i++)
        {
            TurretResearchDef required = Find(research.requires[i]);
            string name = required != null ? required.displayName : research.requires[i];
            label = i == 0 ? name : label + ", " + name;
        }

        return label;
    }

    public static bool TryUnlock(string id)
    {
        TurretResearchDef research = Find(id);
        if (research == null || IsComplete(id) || !PrerequisitesMet(research))
            return false;
        if (!GameProgress.TrySpendResearchPoints(research.cost))
            return false;

        GameProgress.UnlockResearch(id);
        return true;
    }

    public static float RangeBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.range);
    }

    public static float ArmorPierceBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.armorPierce);
    }

    public static float BulletSpeedBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.bulletSpeed);
    }

    public static float SpreadBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.spreadAngle);
    }

    public static float ReloadBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.reloadTime);
    }

    public static void TryScrapAmmo(TurretController turret, Target_Base target)
    {
        if (turret == null || !(target is Target_Enemy) || !HasFlag(GroupOf(turret), r => r.scrapAmmo))
            return;

        WeaponSystem weapon = turret.weapon;
        if (weapon == null)
            weapon = turret.GetComponent<WeaponSystem>();
        if (weapon != null)
            weapon.RestoreAmmo(UnityEngine.Random.Range(2, 5));
    }

    public static float OverheatCoolBonus(TurretController turret)
    {
        return Max(GroupOf(turret), r => r.overheatCoolBonus);
    }

    public static bool HasEmergencyVent(TurretController turret)
    {
        return HasFlag(GroupOf(turret), r => r.emergencyVent);
    }

    public static float BurnChance(TurretController turret)
    {
        return Max(GroupOf(turret), r => r.burnChance);
    }

    public static float KillFundsBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.killFundsBonus);
    }

    public static float HeatFeedbackBonus(TurretController turret)
    {
        return Max(GroupOf(turret), r => r.heatFeedback);
    }

    public static float FriendlyDamageScale(TurretController turret, Target_Base target)
    {
        float reduce = Sum(GroupOf(turret), r => r.friendlyDamageReduce);
        if (reduce <= 0f)
            return 1f;

        bool asEnemy;
        if (!TryGetIdentification(turret, target, out asEnemy) || asEnemy)
            return 1f;

        return Mathf.Clamp01(1f - reduce);
    }

    public static float ArmorShredStep(TurretController turret)
    {
        return Max(GroupOf(turret), r => r.armorShred);
    }

    public static bool HasVacuumPull(TurretController turret)
    {
        return HasFlag(GroupOf(turret), r => r.vacuumPull);
    }

    public static float ExplosionRadiusScale(TurretController turret)
    {
        return Mathf.Clamp(1f + Sum(GroupOf(turret), r => r.explosionRadius), 0.2f, 1f);
    }

    public static float FireRateBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.fireRate);
    }

    public static float ReloadTimeScale(TurretController turret)
    {
        return Mathf.Clamp(1f + Sum(GroupOf(turret), r => r.reloadMul), 0.2f, 1f);
    }

    public static float HomingTurnBonus(TurretController turret)
    {
        return Sum(GroupOf(turret), r => r.homingTurnRate);
    }

    public static bool HasTopAttack(TurretController turret)
    {
        return HasFlag(GroupOf(turret), r => r.topAttack);
    }

    public static float DirectPierceCap(TurretController turret)
    {
        return HasFlag(GroupOf(turret), r => r.overpenDirect) ? 2f : 1f;
    }

    public static void OnDealtDamage(TurretController turret, Target_Base target)
    {
        float step = ArmorShredStep(turret);
        if (step <= 0f || target == null || target.isDead)
            return;

        bool asEnemy;
        if (TryGetIdentification(turret, target, out asEnemy) && asEnemy)
            target.ApplyStatus(new StatusEffect_Disarm(step));

        StatusEffect_Disarm disarm = target.GetStatus<StatusEffect_Disarm>(StatusEffect_Disarm.Id);
        if (disarm != null)
            disarm.TryShred(target, step);
    }

    public static void TryVacuumPull(TurretController turret, Target_Base target, Vector3 point)
    {
        if (!HasVacuumPull(turret) || target == null || target.isDead)
            return;

        bool asEnemy;
        if (!TryGetIdentification(turret, target, out asEnemy) || !asEnemy)
            return;

        target.PullToward(point, 6.5f, 0.22f);
    }

    public static bool TryGetIdentification(TurretController turret, Target_Base target, out bool asEnemy)
    {
        asEnemy = false;
        if (turret == null || target == null)
            return false;

        CCTVController cctv = turret.AssignedCctv;
        if (cctv == null)
            cctv = UnityEngine.Object.FindFirstObjectByType<CCTVController>();
        if (cctv == null)
            return false;

        float confidence;
        return cctv.TryGetIdentification(target, out asEnemy, out confidence);
    }

    public static int WeakspotBonus(TurretController turret, Target_Base target)
    {
        if (turret == null || target == null || !HasFlag(GroupOf(turret), r => r.weakspot))
            return 0;

        CCTVController cctv = turret.AssignedCctv;
        if (cctv == null)
            cctv = UnityEngine.Object.FindFirstObjectByType<CCTVController>();
        if (cctv == null)
            return 0;

        bool asEnemy;
        float confidence;
        if (!cctv.TryGetIdentification(target, out asEnemy, out confidence) || !asEnemy)
            return 0;

        return Mathf.FloorToInt(10f * Mathf.Clamp01(confidence));
    }

    static bool HasFlag(string group, Func<TurretResearchDef, bool> flag)
    {
        if (string.IsNullOrEmpty(group))
            return false;

        for (int i = 0; i < All.Length; i++)
        {
            TurretResearchDef research = All[i];
            if (research.group != group || !flag(research))
                continue;
            if (IsComplete(research.id))
                return true;
        }

        return false;
    }

    static float Max(string group, Func<TurretResearchDef, float> pick)
    {
        if (string.IsNullOrEmpty(group))
            return 0f;

        float best = 0f;
        for (int i = 0; i < All.Length; i++)
        {
            TurretResearchDef research = All[i];
            if (research.group != group || !IsComplete(research.id))
                continue;
            float value = pick(research);
            if (value > best)
                best = value;
        }

        return best;
    }

    static float Sum(string group, Func<TurretResearchDef, float> pick)
    {
        if (string.IsNullOrEmpty(group))
            return 0f;

        float total = 0f;
        for (int i = 0; i < All.Length; i++)
        {
            TurretResearchDef research = All[i];
            if (research.group != group || !IsComplete(research.id))
                continue;
            total += pick(research);
        }

        return total;
    }
}
