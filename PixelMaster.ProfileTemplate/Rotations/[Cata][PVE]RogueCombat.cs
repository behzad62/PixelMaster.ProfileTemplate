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
    public class RogueCombat : IPMRotation
    {
        private RogueSettings settings => SettingsManager.Instance.Rogue;
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Rogue;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]Rogue-Combat";
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
                if (targetedEnemy.IsFlying || (targetedEnemy.Position.Z - player.Position.Z) > 5 && targetedEnemy.DistanceSquaredToPlayer < 25)
                {
                    if (IsSpellReadyOrCasting("Throw"))
                        return CastAtTarget("Throw");
                    if (IsSpellReadyOrCasting("Shoot"))
                        return CastAtTarget("Shoot");
                    if (targetedEnemy.Level >= player.Level - 4 && !player.IsStealthed && IsSpellReady("Stealth"))
                        return CastAtPlayerLocation("Stealth", isHarmfulSpell: false);
                }

                if (player.IsStealthed && IsSpellReady("Sprint") && targetedEnemy.DistanceSquaredToPlayer > 35 * 35)
                    return CastAtPlayerLocation("Sprint", isHarmfulSpell: false);

                if (targetedEnemy.Level >= player.Level - 4 && !player.IsStealthed && IsSpellReady("Stealth") && targetedEnemy.DistanceSquaredToPlayer < 40 * 40)
                    return CastAtPlayerLocation("Stealth", isHarmfulSpell: false);
                if (player.IsStealthed)
                {
                    if (targetedEnemy.IsMoving && (targetedEnemy.CreatureType == CreatureType.Beast || targetedEnemy.CreatureType == CreatureType.Humanoid || targetedEnemy.CreatureType == CreatureType.Demon || targetedEnemy.CreatureType == CreatureType.Dragonkin)
                        && targetedEnemy.DistanceSquaredToPlayer < 10 * 10 && IsSpellReady("Sap"))
                        return CastAtTarget("Sap");
                    if (targetedEnemy.IsMoving
                            && targetedEnemy.DistanceSquaredToPlayer < 20 * 20 && IsSpellReady("Distract"))
                        return CastAtGround(NavigationHelpers.GetTargetPoint(player.Position, targetedEnemy.DistanceToPlayer + 5, NavigationHelpers.GetTargetVectorYaw(player.Position, targetedEnemy.Position), false), "Distract");
                }
                if ((player.IsBehindTarget(targetedEnemy) || targetedEnemy.HasDeBuff("Sap") || !targetedEnemy.IsMoving) && IsSpellReady("Garrote"))
                    return CastAtTarget("Garrote", facing: SpellFacingFlags.BehindAndFaceTarget);
                if ((!PlayerLearnedSpell("Garrote") || !player.IsBehindTarget(targetedEnemy)) && IsSpellReady("Cheap Shot"))
                    return CastAtTarget("Cheap Shot");
                if (player.IsBehindTarget(targetedEnemy) && IsSpellReady("Ambush") && !PlayerLearnedSpell("Cheap Shot"))
                    return CastAtTarget("Ambush", facing: SpellFacingFlags.BehindAndFaceTarget);
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

            if (player.HasAura("Vanish"))
                return null;
            if (player.HealthPercent <= 15 && IsSpellReady("Smoke Bomb"))
                return CastAtPlayerLocation("Smoke Bomb", isHarmfulSpell:false);
            if (player.HealthPercent <= 20 && IsSpellReady("Vanish"))
                return CastAtPlayerLocation("Vanish", isHarmfulSpell: false);


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



            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Evasion") && !player.HasBuff("Evasion"))
                    return CastAtPlayerLocation("Evasion", isHarmfulSpell: false);
                if (IsSpellReady("Cloak of Shadows"))
                    return CastAtPlayerLocation("Cloak of Shadows", isHarmfulSpell: false);
                return null;
            }

            if (player.Energy < 20 && IsSpellReady("Adrenaline Rush") && !player.HasBuff("Killing Spree"))
                return CastAtPlayerLocation("Adrenaline Rush", isHarmfulSpell: false);
            if (player.Energy < 30 && IsSpellReady("Killing Spree") && player.HasBuff("Deep Insight") && !player.HasBuff("Adrenaline Rush"))
                return CastAtPlayerLocation("Killing Spree", isHarmfulSpell: false);

            //AoE handling
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;
            if (player.HealthPercent < 80 && inCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsCasting) >= 1 && IsSpellReady("Cloak of Shadows"))
                return CastAtPlayerLocation("Cloak of Shadows", isHarmfulSpell: false);
            var bladeFlurryIsActive = player.HasBuff("Blade Flurry");
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count(e=> e.IsTargetingPlayer && e.IsInMeleeRange) >= 2)
                {
                    if (player.HealthPercent < 80 && IsSpellReady("Evasion") && !player.HasBuff("Evasion"))
                        return CastAtPlayerLocation("Evasion", isHarmfulSpell: false);
                    if(player.HealthPercent < 80 && IsSpellReady("Combat Readiness") && !player.HasBuff("Evasion"))
                        return CastAtPlayerLocation("Combat Readiness", isHarmfulSpell: false);
                }
                if (nearbyEnemies.Count >= 5)
                {
                    if (IsSpellReady("Adrenaline Rush") && !player.HasBuff("Adrenaline Rush"))
                        return CastAtPlayerLocation("Adrenaline Rush", isHarmfulSpell: false);
                }
                var closeEnemies = nearbyEnemies.Where(e => e.DistanceSquaredToPlayer < 64);
                if (closeEnemies.Count() >= 10)
                {
                    if (bladeFlurryIsActive && IsSpellReady("Blade Flurry"))
                        return CastAtPlayerLocation("Blade Flurry", isHarmfulSpell: false);
                    if (IsSpellReady("Fan of Knives"))
                        return CastAtPlayerLocation("Fan of Knives", isHarmfulSpell: false);
                }
                else if (nearbyEnemies.Count(e => !e.HasAura("Blind")) > 1)
                {
                    if (!bladeFlurryIsActive && IsSpellReady("Blade Flurry"))
                        return CastAtPlayerLocation("Blade Flurry", isHarmfulSpell: false);
                }
                var blindTarget = inCombatEnemies.FirstOrDefault(e => e.IsTargetingPlayer && !e.Debuffs.Any(a => a.Spell.IsDoT) && (!bladeFlurryIsActive || e.DistanceSquaredToPlayer > 64));
                if (blindTarget != null && IsSpellReady("Blind"))
                    return CastAtUnit(blindTarget, "Blind");
            }
            else if (bladeFlurryIsActive)
            {
                return CastAtPlayerLocation("Blade Flurry", isHarmfulSpell: false);
            }
            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.HasAura("Blind"))
                    return null;
                if (targetedEnemy.IsCasting && targetedEnemy.DistanceSquaredToPlayer < 10 * 10)
                {
                    if (IsSpellReady("Kick"))
                        return CastAtTarget("Kick");
                }
                if (player.ComboPoints == 4 && IsSpellReady("Revealing Strike"))
                    return CastAtTarget("Revealing Strike");
                if (settings.CombatUseRuptureFinisher && player.ComboPoints >= 4 && targetedEnemy.IsElite && !player.HasBuff("Blade Flurry") && IsSpellReady("Revealing Strike") && !targetedEnemy.HasDeBuff("Rupture"))
                    return CastAtTarget("Rupture");
                if (player.ComboPoints > 0 && IsSpellReady("Slice and Dice") && player.AuraRemainingTime("Slice and Dice").TotalSeconds < 3)
                    return CastAtPlayerLocation("Slice and Dice");
                if ((player.ComboPoints == 5 || (player.ComboPoints >= 2 && targetedEnemy.HealthPercent < 40)) && IsSpellReady("Eviscerate"))
                    return CastAtTarget("Eviscerate");
                if (player.ComboPoints < 4 || !PlayerLearnedSpell("Revealing Strike"))
                    return CastAtTarget("Sinister Strike");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
