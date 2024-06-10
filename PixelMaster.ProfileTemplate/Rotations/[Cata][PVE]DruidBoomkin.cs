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
using PixelMaster.Services.Logging;

namespace CombatClasses
{
    public class DruidBoomkin : IPMRotation
    {
        private DruidSettings settings => SettingsManager.Instance.Druid;
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Druid;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Druid-Boomkin";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                if (IsSpellReady("Moonfire"))
                    return CastAtTarget("Moonfire");
                if (IsSpellReadyOrCasting("Wrath"))
                    return CastAtTarget("Wrath");
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

            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }
            if (player.PowerPercent < 15)
            {
                if (!player.HasAura("Innervate") && IsSpellReady("Innervate"))
                    return CastAtPlayer("Innervate");
            }

            if (!settings.NoHealBalanceAndFeral)
            {
                if(player.HealthPercent <= settings.NonRestoRejuvenation && !player.HasAura("Rejuvenation", true) && IsSpellReady("Rejuvenation"))
                    return CastAtPlayer("Rejuvenation");
                if (player.HealthPercent <= settings.NonRestoRegrowth && !player.HasAura("Regrowth", true) && IsSpellReadyOrCasting("Regrowth"))
                    return CastAtPlayer("Regrowth");
                if (player.HealthPercent <= settings.NonRestoHealingTouch && IsSpellReadyOrCasting("Healing Touch"))
                    return CastAtPlayer("Healing Touch");
            }

            if (player.Form != ShapeshiftForm.MoonkinForm && IsSpellReady("Moonkin Form"))
                return CastAtPlayerLocation("Moonkin Form", isHarmfulSpell: false);

            if(player.HealthPercent <= settings.Barkskin && IsSpellReady("Barkskin"))
                return CastAtPlayer("Barkskin");


            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Nature's Grasp") && !player.HasAura("Nature's Grasp", true))
                    return CastAtPlayerLocation("Nature's Grasp");
                if (IsSpellReady("Dash", "Cat Form"))
                    return CastAtPlayerLocation("Dash", "Cat Form");
                return null;
            }
            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, targetedEnemy != null? targetedEnemy.Position: player.Position, 10);
                if (nearbyEnemies.Count >= 3)
                {
                    // If we got 3 shrooms out. Pop 'em
                    if (MushroomCount >= 3 && IsSpellReady("Wild Mushroom: Detonate"))
                        return CastAtPlayerLocation("Wild Mushroom: Detonate", isHarmfulSpell: true);

                    if (IsSpellReadyOrCasting("Wild Mushroom") && GetSpellCooldown("Wild Mushroom: Detonate").TotalSeconds <= 5)
                    {
                        // If Detonate is coming off CD, make sure we drop some more shrooms. 3 seconds is probably a little late, but good enough.
                        var AoELocation = GetBestAoELocation(inCombatEnemies.Where(e => !IsCrowdControlled(e)), 8, out int numEnemiesInAoE);
                        if (numEnemiesInAoE >= 1)
                            return CastAtGround(AoELocation, "Wild Mushroom");
                    }
                    if(targetedEnemy != null && IsSpellReady("Force of Nature") && player.HasAura("Eclipse (Solar)", true))
                        return CastAtGround(targetedEnemy.Position, "Force of Nature");
                    if (settings.UseStarfall && IsSpellReadyOrCasting("Starfall") && player.HasAura("Eclipse (Lunar)", true))
                        return CastPetAbilityAtPlayer("Starfall");
                    var moonFireTarget = nearbyEnemies.Where(e=>!IsCrowdControlled(e) && !e.HasAura("Moonfire", true) && !e.HasAura("Sunfire", true)).FirstOrDefault();
                    if (moonFireTarget != null && IsSpellReady("Moonfire"))
                        return CastAtUnit(moonFireTarget, "Moonfire");
                    var swarmTarget = nearbyEnemies.Where(e => !IsCrowdControlled(e) && !e.HasAura("Insect Swarm", true)).FirstOrDefault();
                    if (swarmTarget != null && IsSpellReady("Insect Swarm"))
                        return CastAtUnit(swarmTarget, "Insect Swarm");
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (player.Form == ShapeshiftForm.MoonkinForm && IsSpellReady("Solar Beam"))
                        return CastAtGround(targetedEnemy.Position, "Solar Beam");
                    if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Bash", "Bear Form") && targetedEnemy.DistanceSquaredToPlayer < 10 * 10)
                        return CastAtTarget("Bash", "Bear Form");
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp") && targetedEnemy.DistanceSquaredToPlayer < 8 * 8)
                        return CastAtPlayerLocation("War Stomp");
                }
                if (targetedEnemy.IsElite && IsSpellReady("Force of Nature") && player.HasAura("Eclipse (Solar)", true))
                    return CastAtGround(targetedEnemy.Position, "Force of Nature");
                // Starsurge on every proc. Plain and simple.
                if (IsSpellReadyOrCasting("Starsurge"))
                    return CastAtTarget("Starsurge");
                // Refresh MF/SF
                if (IsSpellReady("Moonfire") && 
                    (player.IsMoving || 
                    (targetedEnemy.AuraRemainingTime("Moonfire", true).TotalSeconds < 3 && targetedEnemy.AuraRemainingTime("Sunfire", true).TotalSeconds < 3)))
                    return CastAtTarget("Moonfire");
                if (IsSpellReady("Sunfire") &&
                    (player.IsMoving ||
                    (targetedEnemy.AuraRemainingTime("Moonfire", true).TotalSeconds < 3 && targetedEnemy.AuraRemainingTime("Sunfire", true).TotalSeconds < 3)))
                    return CastAtTarget("Sunfire");
                // Make sure we keep IS up. Clip the last tick. (~3s)
                if (IsSpellReady("Insect Swarm") && targetedEnemy.AuraRemainingTime("Insect Swarm", true).TotalSeconds < 3)
                    return CastAtTarget("Insect Swarm");
                if ((targetedEnemy.IsInMeleeRange && targetedEnemy.IsTargetingPlayer || player.HasAura("Eclipse (Solar)") || !player.HasAura("Eclipse (Lunar)") && player.Eclipse <= 0 || !PlayerLearnedSpell("Starfire")) && IsSpellReadyOrCasting("Wrath"))
                    return CastAtTarget("Wrath");
                if (IsSpellReadyOrCasting("Starfire") && !IsSpellCasting("Wrath"))
                    return CastAtTarget("Starfire");

                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
        static bool IsCrowdControlled(WowUnit unit)
        {

            return unit.Auras.Any(
                a => a.Spell != null && (a.Spell.IsBreakableCC ||
                     // Really want to ignore hexed mobs.
                     a.Spell.Name == "Hex")

                     );
        }
        static int MushroomCount
        {
            get { return ObjectManager.Instance.GetVisibleUnits().Where(o => o.Name == "Wild Mushroom" && o.DistanceSquaredToPlayer <= 40 * 40 && o.CreatorGuid == ObjectManager.Instance.PlayerGUID).Count(); }
        }


    }
}
