using PixelMaster.Core.API;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;

using static PixelMaster.Core.API.PMRotationBuilder;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Profiles;
using PixelMaster.Core.Behaviors;
using PixelMaster.Core.Behaviors.Transport;
using PixelMaster.Services.Behaviors;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;
using AdvancedCombatClasses.Settings;

namespace CombatClasses
{
    public class ShamanEle : IPMRotation
    {
        private ShamanSettings settings => SettingsManager.Instance.Shaman;
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Shaman;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Shaman-Elemental ";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            if (player.AuraStacks("Lightning Shield", true) < 2 && IsSpellReady("Lightning Shield"))
                return CastAtPlayerLocation("Lightning Shield", isHarmfulSpell: false);
            if (targetedEnemy != null)
            {
                if (IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt");
            }
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = player.SpellBook;
            var inv = player.Inventory;
            var comboPoints = player.SecondaryPower;
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;

            if (!player.HasAura("Lightning Shield", true) && IsSpellReady("Lightning Shield"))
                return CastAtPlayerLocation("Lightning Shield", isHarmfulSpell: false);
            if (player.IsMoving && IsSpellReady("Spiritwalker's Grace"))
                return CastAtPlayerLocation("Spiritwalker's Grace", isHarmfulSpell: false);

            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }
            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Earthbind Totem") && !IsTotemLanded("Earthbind Totem"))
                    return CastAtPlayerLocation("Earthbind Totem");
                if (IsSpellReady("Stoneclaw Totem") && !IsTotemLanded("Stoneclaw Totem"))
                    return CastAtPlayerLocation("Stoneclaw Totem");
                return null;
            }
            if (settings.ElementalHeal && player.HealthPercent < 60)
            {
                if (!PlayerLearnedSpell("Healing Surge") && IsSpellReadyOrCasting("Healing Wave"))
                    return CastAtPlayer("Healing Wave");
                if (IsSpellReadyOrCasting("Healing Surge"))
                    return CastAtPlayer("Healing Surge");
            }

            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
                if (nearbyEnemies.Count >= 3)
                {
                    if (IsSpellReady("Elemental Mastery"))
                        return CastAtPlayerLocation("Elemental Mastery", isHarmfulSpell: false);
                    if (IsSpellReady("Thunderstorm"))
                        return CastAtPlayerLocation("Thunderstorm", isHarmfulSpell: true);
                }
                nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 40);
                if (nearbyEnemies.Count >= 3)
                {
                    if (IsSpellReady("Chain Lightning"))
                    {
                        var bestChainTarget = GetBestUnitForChainedSpell(inCombatEnemies, 12, 3, false, out var numHits);
                        if (numHits >= 3)
                            return CastAtUnit(bestChainTarget, "Chain Lightning");
                    }
                    else if (IsSpellCasting("Chain Lightning"))
                        return CastAtTarget("Chain Lightning");

                    if (IsSpellReady("Earthquake"))
                    {
                        var AoELocation = GetBestAoELocation(inCombatEnemies, 10f, out int numEnemiesInAoE);
                        if (numEnemiesInAoE >= 3)
                            return CastAtGround(AoELocation, "Earthquake");
                    }
                    else if (IsSpellCasting("Earthquake"))
                        return CastAtGround(LastGroundSpellLocation, "Earthquake");
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {

                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Wind Shear") && targetedEnemy.DistanceSquaredToPlayer < 25 * 25)
                        return CastAtTarget("Wind Shear");
                }
                if (targetedEnemy.IsElite && IsSpellReady("Fire Elemental Totem"))
                    return CastAtPlayerLocation("Fire Elemental Totem", isHarmfulSpell: true);

                if (targetedEnemy.DistanceSquaredToPlayer < 30 * 30 && IsSpellReady("Searing Totem") && !om.PlayerTotems.Any(t => (t.Name == "Searing Totem" && Vector3.DistanceSquared(t.Position, targetedEnemy.Position) < 35 * 35) || t.Name == "Fire Elemental Totem"))
                    return CastAtPlayerLocation("Searing Totem", isHarmfulSpell: true);
                if (!targetedEnemy.HasAura("Flame Shock", true) && IsSpellReady("Flame Shock"))
                    return CastAtTarget("Flame Shock");
                if (IsSpellReadyOrCasting("Lava Burst"))
                    return CastAtTarget("Lava Burst");
                if (IsSpellReady("Earth Shock") && player.AuraStacks("Lightning Shield") >= 7 && targetedEnemy.AuraRemainingTime("Flame Shock", true).TotalSeconds >= 6)
                    return CastAtTarget("Earth Shock");
                if (player.IsMoving && !player.HasAura("Spiritwalker's Grace") && IsSpellReady("Unleash Elements") && player.HasAura("Flametongue Weapon"))
                    return CastAtTarget("Unleash Elements");
                if (GetUnitsWithinArea(inCombatEnemies, targetedEnemy.Position, 10).Count >= 2 && IsSpellReadyOrCasting("Chain Lightning"))
                    return CastAtTarget("Chain Lightning");
                if (IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt");
            }
            return null;
        }
        private bool IsTotemLanded(string totemName)
        {
            return ObjectManager.Instance.PlayerTotems.Where(totem => totem.Name == totemName).Any();
        }
    }
}
