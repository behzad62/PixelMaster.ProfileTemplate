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
using AdvancedCombatClasses.Settings.Cata;
using PixelMaster.Server.Shared;

namespace CombatClasses
{
    public class ShamanEle : IPMRotation
    {
        private ShamanSettings settings => ((CataCombatSettings)SettingsManager.Instance.Settings).Shaman;
        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Cata, WowVersion.Classic_Cata_Ptr };
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
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            if (player.AuraStacks("Lightning Shield", true) < 2 && IsSpellReady("Lightning Shield"))
                return CastWithoutTargeting("Lightning Shield", isHarmfulSpell: false);
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
            var sb = om.SpellBook;
            var inv = om.Inventory;
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies.ToList();

            if (!player.HasAura("Lightning Shield", true) && IsSpellReady("Lightning Shield"))
                return CastWithoutTargeting("Lightning Shield", isHarmfulSpell: false);
            if (player.IsMoving && IsSpellReady("Spiritwalker's Grace"))
                return CastWithoutTargeting("Spiritwalker's Grace", isHarmfulSpell: false);

            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                if (!om.CurrentMap.IsDungeon)
                {
                    var healingPot = inv.GetHealingPotion();
                    if (healingPot != null)
                        return UseItem(healingPot);
                }
            }
            if (!om.CurrentMap.IsDungeon && player.Debuffs.Any(d => d.Spell != null && d.Spell.DispelType == SpellDispelType.Curse))
            {
                if (IsSpellReady("Cleanse Spirit"))
                    return CastAtPlayer("Cleanse Spirit");
            }
            if (om.IsPlayerFleeingFromCombat)
            {
                if (IsSpellReady("Earthbind Totem") && !IsTotemLanded("Earthbind Totem"))
                    return CastWithoutTargeting("Earthbind Totem");
                if (IsSpellReady("Stoneclaw Totem") && !IsTotemLanded("Stoneclaw Totem"))
                    return CastWithoutTargeting("Stoneclaw Totem");
                return null;
            }
            if (settings.ElementalHeal)
            {
                if ((player.HealthPercent < 30 || IsSpellCasting("Healing Surge") && player.HealthPercent < 90) && IsSpellReadyOrCasting("Healing Surge") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 8)
                    return CastAtPlayer("Healing Surge");
                if ((player.HealthPercent < 40 || IsSpellCasting("Greater Healing Wave")) && IsSpellReadyOrCasting("Greater Healing Wave") && !IsSpellCasting("Healing Wave") && !IsSpellCasting("Healing Surge") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 2)
                    return CastAtPlayer("Greater Healing Wave");
                if (!PlayerLearnedSpell("Healing Surge") && (player.HealthPercent < 50 || IsSpellCasting("Healing Wave")) && IsSpellReadyOrCasting("Healing Wave") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 2)
                    return CastAtPlayer("Healing Wave");
                if ((player.HealthPercent < 50 || IsSpellCasting("Healing Surge")) && IsSpellReadyOrCasting("Healing Surge") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 3)
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
                        return CastWithoutTargeting("Elemental Mastery", isHarmfulSpell: false);
                    if (IsSpellReady("Earthbind Totem") && !om.PlayerTotems.Any(t => t.Name == "Earthbind Totem" && t.DistanceSquaredToPlayer < 100))
                        return CastWithoutTargeting("Earthbind Totem", isHarmfulSpell: false);
                    if (IsSpellReady("Thunderstorm"))
                        return CastWithoutTargeting("Thunderstorm", isHarmfulSpell: true);
                    if (!player.IsCasting && nearbyEnemies.Count > 5 && IsSpellReady("Magma Totem") && !om.PlayerTotems.Any(t => (t.Name == "Magma Totem" && Vector3.DistanceSquared(t.Position, player.Position) < 8 * 8) || t.Name == "Fire Elemental Totem"))
                        return CastWithoutTargeting("Magma Totem", isHarmfulSpell: true);
                }
                nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 40);
                if (nearbyEnemies.Count >= 3)
                {
                    if (nearbyEnemies.Count() >= 10 && IsSpellReady("Fire Elemental Totem"))
                        return CastWithoutTargeting("Fire Elemental Totem");
                    if (!player.IsCasting && IsSpellReady("Wrath of Air Totem") && !player.HasAura("Wrath of Air Totem"))
                        return CastWithoutTargeting("Wrath of Air Totem", isHarmfulSpell: false);
                    if (!player.IsCasting && IsSpellReady("Flame Shock"))
                    {
                        var flameTarget = nearbyEnemies.FirstOrDefault(e => !e.HasAura("Flame Shock", true));
                        if (flameTarget != null)
                            return CastAtUnit(flameTarget, "Flame Shock");
                    }
                    if (!player.IsCasting && IsSpellReady("Fire Nova"))
                    {
                        var fireNovaTargets = inCombatEnemies.Where(e => e.HasAura("Flame Shock", true));
                        fireNovaTargets = fireNovaTargets.SelectMany(e => e.GetNearbyInCombatEnemies(10));
                        if (fireNovaTargets.Count() > 3)
                        {
                            return CastWithoutTargeting("Fire Nova");
                        }
                    }
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

                if (!player.IsCasting && targetedEnemy.DistanceSquaredToPlayer < 30 * 30 && IsSpellReady("Searing Totem") && !om.PlayerTotems.Any(t => (t.Name == "Searing Totem" && Vector3.DistanceSquared(t.Position, targetedEnemy.Position) < 35 * 35) || t.Name == "Fire Elemental Totem"))
                    return CastWithoutTargeting("Searing Totem", isHarmfulSpell: true);
                if (!player.IsCasting && !targetedEnemy.HasAura("Flame Shock", true) && IsSpellReady("Flame Shock"))
                    return CastAtTarget("Flame Shock");
                if (IsSpellReadyOrCasting("Lava Burst"))
                    return CastAtTarget("Lava Burst");
                if (!player.IsCasting && IsSpellReady("Earth Shock") && player.AuraStacks("Lightning Shield") >= 7 && targetedEnemy.AuraRemainingTime("Flame Shock", true).TotalSeconds >= 6)
                    return CastAtTarget("Earth Shock");
                if (IsSpellReady("Chain Lightning"))
                {
                    var bestChainTarget = GetBestUnitForChainedSpell(inCombatEnemies, 12, 3, false, out var numHits);
                    if (numHits >= 3)
                        return CastAtUnit(bestChainTarget, "Chain Lightning");
                }
                else if (IsSpellCasting("Chain Lightning"))
                    return CastAtTarget("Chain Lightning");
                if (player.IsMoving && !player.HasAura("Spiritwalker's Grace") && IsSpellReady("Unleash Elements") && player.HasAura("Flametongue Weapon"))
                    return CastAtTarget("Unleash Elements");
                if (GetUnitsWithinArea(inCombatEnemies, targetedEnemy.Position, 10).Count >= 2 && IsSpellReadyOrCasting("Chain Lightning"))
                    return CastAtTarget("Chain Lightning");
                if (IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt");
                if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
        private bool IsTotemLanded(string totemName)
        {
            return ObjectManager.Instance.PlayerTotems.Where(totem => totem.Name == totemName).Any();
        }
    }
}
