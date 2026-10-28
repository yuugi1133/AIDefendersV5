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
