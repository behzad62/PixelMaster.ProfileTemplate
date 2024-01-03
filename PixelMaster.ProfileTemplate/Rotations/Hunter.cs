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
    public class BMHunter : IPMRotation
    {
        public short Spec => 0;
        public UnitClass PlayerClass => UnitClass.Hunter;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "Hunter General PvE";
        public string Author => "PixelMaster";
        public string Description => "";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var inv = player.Inventory;
            var sb = player.SpellBook;
            var pet = om.PlayerPet;
            var targetedEnemy = om.AnyEnemy;
            var mainHand = inv.GetEquippedItemsBySlot(EquipSlot.MainHand);
            if (targetedEnemy != null)
            {
                if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDeBuff("Hunter's Mark"))
                    return CastAtTarget("Hunter's Mark");
                if (pet != null && !pet.IsDead && !targetedEnemy.IsSameAs(pet.Target))
                    return PetAttack();
            }
            if (IsSpellReadyOrCasting("Aimed Shot"))
                return CastAtTarget("Aimed Shot");
            if (IsSpellReady("Concussive Shot"))
                return CastAtTarget("Concussive Shot");
            if (IsSpellReady("Serpent Sting"))
                return CastAtTarget("Serpent Sting");
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var pet = om.PlayerPet;
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
            if (pet != null && !pet.IsDead && IsSpellReadyOrCasting("Mend Pet") && (pet.HealthPercent <= 70 || IsSpellCasting("Mend Pet")) && (IsClassicEra || !pet.HasBuff("Mend Pet")))
                return CastAtPet("Mend Pet");
            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Feign Death"))
                    return CastAtPlayer("Feign Death");
                if (!IsClassicEra)
                {
                    if (IsSpellReady("Frost Trap"))
                        return CastAtPlayerLocation("Frost Trap");
                    if (IsSpellReady("Freezing Trap"))
                        return CastAtPlayerLocation("Freezing Trap");
                }
                return null;
            }
            if (player.PetStatus == PetStatus.Unknown)
            {
                if (IsSpellReady("Call Pet"))
                    return CastAtPlayerLocation("Call Pet", isHarmfulSpell: false);
            }

            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (IsSpellReady("Bestial Wrath") && !player.HasBuff("Bestial Wrath"))
                    return CastAtPlayer("Bestial Wrath");
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayer("Berserking");
                if (IsSpellReady("Rapid Fire"))
                    return CastAtPlayer("Rapid Fire");
            }
            inCombatEnemies = om.InCombatEnemies;
            if (player.HealthPercent < 40)
            {
                if (inCombatEnemies.Any(e => e.IsInMeleeRange) && IsSpellReady("Aspect of the Monkey") && !player.HasBuff("Aspect of the Monkey"))
                {
                    return CastAtPlayer("Aspect of the Monkey");
                }
                else
                {
                    if (player.PowerPercent < 40)
                    {
                        if (IsSpellReady("Aspect of the Viper") && !player.HasBuff("Aspect of the Viper"))
                            return CastAtPlayer("Aspect of the Viper");
                        else if (IsSpellReady("Aspect of the Hawk") && !player.HasBuff("Aspect of the Hawk"))
                            return CastAtPlayer("Aspect of the Hawk");
                    }
                    else if (IsSpellReady("Aspect of the Hawk") && !player.HasBuff("Aspect of the Hawk"))
                        return CastAtPlayer("Aspect of the Hawk");
                    else if (IsSpellReady("Aspect of the Monkey") && !player.HasBuff("Aspect of the Monkey"))
                        return CastAtPlayer("Aspect of the Monkey");
                }
            }
            else
            {
                if (((player.HealthPercent < 35 && inCombatEnemies.Any(e => e.IsInMeleeRange && e.IsTargetingPlayer))
                    || (!PlayerLearnedSpell("Aspect of the Hawk") && !PlayerLearnedSpell("Aspect of the Viper")))
                    && IsSpellReady("Aspect of the Monkey") && !player.HasBuff("Aspect of the Monkey"))
                    return CastAtPlayer("Aspect of the Monkey");
                else if (player.PowerPercent < 40)
                {
                    if (IsSpellReady("Aspect of the Viper") && !player.HasBuff("Aspect of the Viper"))
                        return CastAtPlayer("Aspect of the Viper");
                    else if (IsSpellReady("Aspect of the Hawk") && !player.HasBuff("Aspect of the Hawk"))
                        return CastAtPlayer("Aspect of the Hawk");
                }
                else if (IsSpellReady("Aspect of the Hawk") && !player.HasBuff("Aspect of the Hawk"))
                {
                    return CastAtPlayer("Aspect of the Hawk");
                }
            }
            if (IsClassicEra && IsSpellReady("Heart of the Lion") && !player.HasBuff("Heart of the Lion"))
                return CastAtPlayer("Heart of the Lion");
            //AoE handling
            var minRange = IsClassicEra ? 8f : 5f;
            if (inCombatEnemies.Count > 1)
            {
                var ccCandidates = inCombatEnemies.Where(e => e.DistanceSquaredToPlayer > 64 && e.HealthPercent > 25 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root));
                foreach (var ccCandidate in ccCandidates)
                {
                    if (IsSpellReady("Freezing Arrow") && !ccCandidate.HasDeBuff("Freezing Arrow"))
                        return CastAtUnit(ccCandidate, "Freezing Arrow");
                }
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 2)
                {
                    if (IsSpellReady("Explosive Trap"))
                        return CastAtPlayerLocation("Explosive Trap");
                }
                if (targetedEnemy != null && targetedEnemy.DistanceSquaredToPlayer > minRange * minRange && targetedEnemy.GetNearbyInCombatEnemies(6).Count > 0 && IsSpellReadyOrCasting("Multi-Shot"))
                    return CastAtTarget("Multi-Shot");
                if (IsClassicEra && targetedEnemy != null && targetedEnemy.GetNearbyInCombatEnemies(8).Count > 0 && IsSpellReady("Explosive Shot"))
                    return CastAtTarget("Explosive Shot");
                if (IsSpellCasting("Volley") && GetUnitsWithinArea(inCombatEnemies, LastGroundSpellLocation, 8).Count > 2)
                    return CastAtGround(LastGroundSpellLocation, "Volley");
                else if (IsSpellReady("Volley"))
                {
                    var AoELocation = GetBestAoELocation(inCombatEnemies, 8f, out int numEnemiesInAoE);
                    if (numEnemiesInAoE > 3)
                        return CastAtGround(AoELocation, "Volley");
                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                {
                    if (IsSpellReady("Bestial Wrath") && !player.HasBuff("Bestial Wrath"))
                        return CastAtPlayer("Bestial Wrath");
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayer("Berserking");
                    if (IsSpellReady("Rapid Fire"))
                        return CastAtPlayer("Rapid Fire");
                }
            }
            if (IsSpellReady("Kill Command"))
                return CastAtPlayerLocation("Kill Command");

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsMoving && !targetedEnemy.HasDeBuff("Wing Clip") && IsSpellReady("Concussive Shot") && targetedEnemy.IsInCombatWithPlayer)
                    return CastAtTarget("Concussive Shot");
                if (targetedEnemy.DistanceSquaredToPlayer >= minRange * minRange)
                {
                    if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDeBuff("Hunter's Mark"))
                        return CastAtTarget("Hunter's Mark");
                    if (targetedEnemy.HealthPercent <= 20)
                    {
                        if (IsSpellReady("Kill Shot"))
                            return CastAtTarget("Kill Shot");
                    }
                    if (IsSpellReady("Arcane Shot") && player.IsMoving)
                        return CastAtTarget("Arcane Shot");
                    if (IsClassicEra)
                    {
                        if (targetedEnemy.HealthPercent > 30 && IsSpellReady("Serpent Sting") && !targetedEnemy.HasDeBuff("Serpent Sting"))
                            return CastAtTarget("Serpent Sting");
                        if(IsSpellReady("Chimera Shot") && (targetedEnemy.HasDeBuff("Serpent Sting") || targetedEnemy.HasDeBuff("Scorpid Sting") || targetedEnemy.HasDeBuff("Viper Sting")))
                            return CastAtTarget("Chimera Shot");
                    }
                    else if (player.Level < 50 && player.PowerPercent > 50 && targetedEnemy.HealthPercent > 60
                        && IsSpellReady("Serpent Sting") && !targetedEnemy.HasDeBuff("Serpent Sting"))
                        return CastAtTarget("Serpent Sting");
                    if (!targetedEnemy.IsTargetingPlayer && player.Level < 50 && player.PowerPercent > 50
                        && targetedEnemy.HealthPercent > 40 && !player.IsMoving && IsSpellReadyOrCasting("Aimed Shot"))
                        return CastAtTarget("Aimed Shot");
                    if (IsClassicEra && !player.IsMoving && IsSpellReadyOrCasting("Multi-Shot"))
                        return CastAtTarget("Multi-Shot");
                    if (player.Level < 50 && player.PowerPercent > 50 && IsSpellReady("Arcane Shot"))
                        return CastAtTarget("Arcane Shot");
                    if (!player.IsMoving && IsSpellReadyOrCasting("Steady Shot"))
                        return CastAtTarget("Steady Shot");
                    return CastAtTarget("Auto Shot");
                }
                else
                {
                    if (player.HasActivePet && targetedEnemy.IsTargetingPlayer && IsSpellReady("Feign Death"))
                        return CastAtPlayer("Feign Death");
                    if (targetedEnemy.IsTargetingPlayer && IsSpellReady("Scatter Shot"))
                        return CastAtTarget("Scatter Shot");
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp"))
                        return CastAtPlayerLocation("War Stomp");
                    if (targetedEnemy.IsInMeleeRange)
                    {
                        if (IsClassicEra && IsSpellReady("Flanking Strike"))
                            return CastAtTarget("Flanking Strike");
                        if (IsSpellReady("Raptor Strike"))
                            return CastAtTarget("Raptor Strike");
                        if (!targetedEnemy.HasDeBuff("Wing Clip") && IsSpellReady("Wing Clip"))
                            return CastAtTarget("Wing Clip");
                        if (!IsClassicEra && IsSpellReady("Frost Trap"))
                            return CastAtPlayerLocation("Frost Trap");
                        if (!IsClassicEra && IsSpellReady("Freezing Trap"))
                            return CastAtPlayerLocation("Freezing Trap");
                        if (player.IsMeleeWeaponReady)
                            return CastAtTarget(sb.AutoAttack);
                    }
                }
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
