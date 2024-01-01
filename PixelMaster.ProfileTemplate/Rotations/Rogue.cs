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

namespace CombatClasses
{
    public class RogueRotation : IPMRotation
    {
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Rogue;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "Rogue-Combat General PvE";
        public string Author => "PixelMaster";
        public string Description => "";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var inv = player.Inventory;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            var mainHand = inv.GetEquippedItemsBySlot(EquipSlot.MainHand);
            if (targetedEnemy != null 
                && targetedEnemy.DistanceSquaredToPlayer < 36 * 36 
                && mainHand != null && mainHand.Subclass == ItemSubclass.Dagger 
                && targetedEnemy.NearbyEnemies.Count <= 1
                && PlayerLearnedSpell("Ambush"))
            {
                if (!player.IsStealthed && IsSpellReady("Stealth"))
                    return CastAtPlayer("Stealth");
                if(targetedEnemy.CreatureType == CreatureType.Humanoid && IsSpellReady("Sap") && !targetedEnemy.HasDeBuff("Sap"))
                    return CastAtTarget("Sap");
                if (player.IsStealthed && (IsSpellReady("Ambush") || player.Power < 62))
                    return CastAtTarget("Ambush", facing: SpellFacingFlags.BehindAndFaceTarget);
            }
            if (IsSpellReady("Shoot"))
                return CastAtTarget("Shoot");
            else if (IsSpellReady("Throw"))
                return CastAtTarget("Throw");
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
            List<WowUnit>? inCombatEnemies = null;
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
                if (IsSpellReady("Vanish"))
                    return CastAtPlayer("Vanish");
                if (targetedEnemy != null)
                {
                    if (IsSpellReady("Blind") && !targetedEnemy.HasDeBuff("Blind"))
                        return CastAtTarget("Blind");
                    if (targetedEnemy.IsInFrontOfPlayer && IsSpellReady("Gouge") && !targetedEnemy.HasDeBuff("Gouge") && !targetedEnemy.HasDeBuff("Blind"))
                        return CastAtTarget("Gouge");
                }
                if (!player.HasBuff("Vanish") && !player.IsStealthed && IsSpellReady("Sprint"))
                    return CastAtPlayer("Sprint");
                return null;
            }
            if (player.HealthPercent < 30)
            {
                if (IsSpellReady("Evasion") && !player.HasBuff("Evasion"))
                    return CastAtPlayer("Evasion");
            }

            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayer("Berserking");
                if (player.PowerPercent < 10)
                {
                    var tea = inv.GetReadyItemByName("Thistle Tea");
                    if (tea != null)
                        return UseItem(tea);
                }
                if (IsSpellReady("Blade Flurry"))
                    return CastAtPlayer("Blade Flurry");
                if (IsSpellReady("Adrenaline Rush"))
                    return CastAtPlayer("Adrenaline Rush");
                if (IsSpellReady("Killing Spree"))
                    return CastAtPlayer("Killing Spree");
                if (IsSpellReady("Slice and Dice") && comboPoints > 2 && !player.HasBuff("Slice and Dice"))
                    return CastAtPlayer("Slice and Dice");
            }
            //AoE handling
            inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 1)
                {
                    var ccCandidates = nearbyEnemies.Where(e => e.HealthPercent > 25 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root));
                    foreach (var ccCandidate in ccCandidates)
                    {
                        if (IsSpellReady("Blind") && !ccCandidate.HasDeBuff("Blind"))
                            return CastAtUnit(ccCandidate, "Blind");
                        if (IsSpellReady("Gouge") && !ccCandidate.HasDeBuff("Gouge") && !ccCandidate.HasDeBuff("Blind"))
                            return CastAtUnit(ccCandidate, "Gouge");
                    }
                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayer("Berserking");
                    if (player.PowerPercent < 10)
                    {
                        var tea = inv.GetReadyItemByName("Thistle Tea");
                        if (tea != null)
                            return UseItem(tea);
                    }
                    if (IsSpellReady("Blade Flurry"))
                        return CastAtPlayer("Blade Flurry");
                    if (IsSpellReady("Adrenaline Rush"))
                        return CastAtPlayer("Adrenaline Rush");
                    if (IsSpellReady("Killing Spree"))
                        return CastAtPlayer("Killing Spree");
                    if (IsSpellReady("Slice and Dice") && comboPoints > 2 && !player.HasBuff("Slice and Dice"))
                        return CastAtPlayer("Slice and Dice");
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Kick") && targetedEnemy.DistanceSquaredToPlayer < 64)
                        return CastAtTarget("Kick");
                }
                if (!targetedEnemy.IsInMeleeRange && targetedEnemy.IsMoving && targetedEnemy.HealthPercent < 15)
                {
                    if (IsSpellReady("Shoot") && IsPlayerTargetInSpellRange("Shoot"))
                        return CastAtTarget("Shoot");
                }
                if (IsSpellReady("Expose Armor") && comboPoints > 1 && !targetedEnemy.HasDeBuff("Expose Armor") && targetedEnemy.HealthPercent > 60)
                    return CastAtTarget("Expose Armor");
                if (IsSpellReady("Slice and Dice") && comboPoints > 1 && !player.HasBuff("Slice and Dice") && targetedEnemy.HealthPercent > 50)
                    return CastAtPlayer("Slice and Dice");
                if (IsSpellReady("Eviscerate") && (comboPoints > 4 || (comboPoints > 2 && targetedEnemy.HealthPercent < 30)))
                    return CastAtPlayer("Eviscerate");
                if (IsSpellReady("Riposte"))
                    return CastAtTarget("Riposte");
                if (IsSpellReady("Sinister Strike"))
                    return CastAtTarget("Sinister Strike");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
