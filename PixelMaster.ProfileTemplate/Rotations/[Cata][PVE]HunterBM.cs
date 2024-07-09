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
    public class HunterBM : IPMRotation
    {
        private HunterSettings settings => SettingsManager.Instance.Hunter;
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Hunter;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Hunter-BM";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var inv = player.Inventory;
            var sb = player.SpellBook;
            var pet = om.PlayerPet;
            var targetedEnemy = om.AnyEnemy;

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
            if (IsSpellReady("Auto Shot"))
                return CastAtTarget("Auto Shot");
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
            var mendPetAt = !IsClassicEra || inCombatEnemies.Count(e => e.HealthPercent > 50) > 1 ? 70 : 40;
            if (pet != null && !pet.IsDead && (!IsClassicEra || !player.IsMoving) && IsSpellReadyOrCasting("Mend Pet") && (pet.HealthPercent <= mendPetAt || IsSpellCasting("Mend Pet")) && (IsClassicEra || !pet.HasBuff("Mend Pet")))
                return CastAtPet("Mend Pet");

            if (settings.UseDisengage && !IsClassicEra && inCombatEnemies.Any(e => e.IsTargetingPlayer && e.DistanceSquaredToPlayer < 40) && IsSpellReady("Disengage"))
                return CastAtPlayerLocation("Disengage", isHarmfulSpell: false);

            if (player.HealthPercent < 40)
            {
                if (inCombatEnemies.Any(e => e.IsInMeleeRange && e.IsTargetingPlayer) && IsSpellReady("Aspect of the Monkey"))
                {
                    if (!player.HasBuff("Aspect of the Monkey"))
                        return CastAtPlayer("Aspect of the Monkey");
                }
                else
                {
                    if (player.PowerPercent < 30 && inCombatEnemies.Any(e => e.IsInMeleeRange && e.IsTargetingPlayer))
                    {
                        if (IsSpellReady("Aspect of the Fox"))
                        {
                            if (!player.HasBuff("Aspect of the Fox"))
                                return CastAtPlayer("Aspect of the Fox");
                        }
                        else if (IsSpellReady("Aspect of the Hawk"))
                        {
                            if (!player.HasBuff("Aspect of the Hawk"))
                                return CastAtPlayer("Aspect of the Hawk");
                        }
                    }
                    else if (IsSpellReady("Aspect of the Hawk"))
                    {
                        if (!player.HasBuff("Aspect of the Hawk"))
                            return CastAtPlayer("Aspect of the Hawk");
                    }
                    else if (IsSpellReady("Aspect of the Monkey"))
                    {
                        if (!player.HasBuff("Aspect of the Monkey"))
                            return CastAtPlayer("Aspect of the Monkey");
                    }
                }
            }
            else
            {
                if (((player.HealthPercent < 35 && inCombatEnemies.Any(e => e.IsInMeleeRange && e.IsTargetingPlayer))
                    || (!PlayerLearnedSpell("Aspect of the Hawk") && !PlayerLearnedSpell("Aspect of the Fox")))
                    && IsSpellReady("Aspect of the Monkey") && !player.HasBuff("Aspect of the Monkey"))
                    return CastAtPlayer("Aspect of the Monkey");
                else if (player.PowerPercent < 30 && inCombatEnemies.Any(e => e.IsInMeleeRange && e.IsTargetingPlayer))
                {
                    if (IsSpellReady("Aspect of the Fox"))
                    {
                        if (!player.HasBuff("Aspect of the Fox"))
                            return CastAtPlayer("Aspect of the Fox");
                    }
                    else if (IsSpellReady("Aspect of the Hawk"))
                    {
                        if (!player.HasBuff("Aspect of the Hawk"))
                            return CastAtPlayer("Aspect of the Hawk");
                    }
                }
                else if (IsSpellReady("Aspect of the Hawk"))
                {
                    if (!player.HasBuff("Aspect of the Hawk"))
                        return CastAtPlayer("Aspect of the Hawk");
                }
            }
            if (IsClassicEra && IsSpellReady("Heart of the Lion") && !player.HasBuff("Heart of the Lion"))
                return CastAtPlayer("Heart of the Lion");
            //AoE handling
            var minRange = IsClassicEra ? 8f : 5f;
            minRange += player.CombatReach;
            if (inCombatEnemies.Count > 1)
            {
                if (PlayerLearnedSpell("Trap Launcher"))
                {
                    var trapTarget = inCombatEnemies.Where(e => !e.IsSameAs(player.Target) && (!e.IsMoving || e.IsPlayer) && e.DistanceSquaredToPlayer < 40 * 40).OrderBy(u => u.DistanceSquaredToPlayer).FirstOrDefault();
                    if (trapTarget != null)
                    {
                        if (IsSpellReady("Freezing Trap"))
                        {
                            if (player.HasBuff("Trap Launcher"))
                                return CastAtGround(trapTarget.Position, "Freezing Trap");
                            else if (IsSpellReady("Trap Launcher"))
                                return CastAtPlayerLocation("Trap Launcher", isHarmfulSpell: false);
                        }
                    }
                    trapTarget = inCombatEnemies.Where(e => (!e.IsMoving || e.IsPlayer) && e.DistanceSquaredToPlayer < 40 * 40 && e.GetNearbyInCombatEnemies(6).Count >= 2).OrderBy(u => u.DistanceSquaredToPlayer).FirstOrDefault();
                    if (trapTarget != null)
                    {
                        if (IsSpellReady("Explosive Trap"))
                        {
                            if (player.HasBuff("Trap Launcher"))
                                return CastAtGround(trapTarget.Position, "Explosive Trap");
                            else if (IsSpellReady("Trap Launcher"))
                                return CastAtPlayerLocation("Trap Launcher", isHarmfulSpell: false);
                        }
                    }
                }
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
                    var AoELocation = GetBestAoELocation(inCombatEnemies.Where(e => !e.IsTargetingPlayer), 8f, out int numEnemiesInAoE);
                    if (numEnemiesInAoE > 3)
                        return CastAtGround(AoELocation, "Volley");
                }
                if (inCombatEnemies.Count(e => e.IsTargetingPlayer || e.IsTargetingPlayerPet) >= 2)
                {
                    if (IsSpellReady("Rapid Fire") && (player.HasBuff("Call of the Wild") || !sb.PetSpells.Any(s => s.Name == "Call of the Wild" && s.GetCooldown().TotalSeconds < 60)) && !player.HasBuff("Bloodlust") && !player.HasBuff("Heroism") && !player.HasBuff("Time Warp") && !player.HasBuff("The Beast Within"))
                        return CastAtPlayer("Rapid Fire");
                    if (player.PowerPercent <= 50 && (pet is null || player.PetStatus == PetStatus.Dead || pet.PowerPercent <= 50) && IsSpellReady("Fervor"))
                        return CastAtPlayerLocation("Fervor", isHarmfulSpell: false);
                    if (IsSpellReady("Bestial Wrath") && !player.HasBuff("Bestial Wrath") && (!PlayerLearnedSpell("Kill Command") || GetSpellCooldown("Kill Command").TotalSeconds < 2))
                        return CastAtPlayerLocation("Bestial Wrath", isHarmfulSpell: false);
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                }
            }
            if (IsSpellReady("Kill Command"))
                return CastAtPlayerLocation("Kill Command");

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.DistanceSquaredToPlayer > minRange * minRange)
                {
                    if (targetedEnemy.IsCasting)
                    {
                        if (IsSpellReady("Silencing Shot"))
                            return CastAtTarget("Silencing Shot");
                        if (IsSpellReady("Scatter Shot") && targetedEnemy.DistanceSquaredToPlayer <= 20 * 20)
                            return CastAtTarget("Scatter Shot");
                        if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp") && targetedEnemy.DistanceSquaredToPlayer < 8 * 8)
                            return CastAtPlayerLocation("War Stomp");
                        if (pet != null && targetedEnemy.IsSameAs(pet.Target) && IsSpellReady("Intimidation"))
                            return CastAtTarget("Intimidation");
                    }
                    if (targetedEnemy.IsMoving && !targetedEnemy.HasDeBuff("Wing Clip") && IsSpellReady("Concussive Shot") && targetedEnemy.IsInCombatWithPlayer && targetedEnemy.IsTargetingPlayer)
                        return CastAtTarget("Concussive Shot");
                    if (!targetedEnemy.IsInCombat)
                    {
                        if (IsSpellReady("Serpent Sting"))
                            return CastAtTarget("Serpent Sting");
                        if (IsSpellReady("Arcane Shot"))
                            return CastAtTarget("Arcane Shot");
                    }
                    if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDeBuff("Hunter's Mark"))
                        return CastAtTarget("Hunter's Mark");
                    if (targetedEnemy.HealthPercent <= 20)
                    {
                        if (IsSpellReady("Kill Shot"))
                            return CastAtTarget("Kill Shot");
                    }
                    if ((targetedEnemy.IsTargetingPlayer || targetedEnemy.Target is null) && pet != null && IsSpellReady("Intimidation"))
                        return CastAtTarget("Intimidation");
                    if (player.HasActivePet && IsSpellReady("Kill Command") && pet != null && Vector3.DistanceSquared(pet.Position, targetedEnemy.Position) < 25)
                        return CastAtTarget("Kill Command");
                    if (pet != null && pet.AuraStacks("Frenzy Effect") >= 5 && !player.HasBuff("The Beast Within") && IsSpellReady("Focus Fire"))
                        return CastAtPlayerLocation("Focus Fire", isHarmfulSpell: false);
                    if (player.IsMoving && IsSpellReady("Arcane Shot"))
                        return CastAtTarget("Arcane Shot");
                    if (IsClassicEra)
                    {
                        if (targetedEnemy.HealthPercent > 30 && IsSpellReady("Serpent Sting") && !targetedEnemy.HasDeBuff("Serpent Sting"))
                            return CastAtTarget("Serpent Sting");
                        if (IsSpellReady("Chimera Shot") && (targetedEnemy.HasDeBuff("Serpent Sting") || targetedEnemy.HasDeBuff("Scorpid Sting") || targetedEnemy.HasDeBuff("Viper Sting")))
                            return CastAtTarget("Chimera Shot");
                    }
                    else if (player.PowerPercent > 50 && targetedEnemy.HealthPercent > 60
                        && IsSpellReady("Serpent Sting") && !targetedEnemy.HasDeBuff("Serpent Sting"))
                        return CastAtTarget("Serpent Sting");
                    if (!targetedEnemy.IsTargetingPlayer && player.Level < 50 && player.PowerPercent > 50
                        && targetedEnemy.HealthPercent > 40 && !player.IsMoving && IsSpellReadyOrCasting("Aimed Shot"))
                        return CastAtTarget("Aimed Shot");
                    if (IsClassicEra && !player.IsMoving && IsSpellReadyOrCasting("Multi-Shot"))
                        return CastAtTarget("Multi-Shot");
                    if (player.PowerPercent > 40 && IsSpellReady("Arcane Shot"))
                        return CastAtTarget("Arcane Shot");
                    if (!player.IsMoving || player.HasBuff("Aspect of the Fox"))
                    {
                        if (IsSpellReadyOrCasting("Cobra Shot"))
                            return CastAtTarget("Cobra Shot");
                        if (!PlayerLearnedSpell("Cobra Shot") && IsSpellReadyOrCasting("Steady Shot"))
                            return CastAtTarget("Steady Shot");
                    }
                    return CastAtTarget("Auto Shot");
                }
                else
                {
                    if (targetedEnemy.IsCasting)
                    {
                        if (IsSpellReady("Scatter Shot") && targetedEnemy.DistanceSquaredToPlayer <= 20 * 20)
                            return CastAtTarget("Scatter Shot");
                        if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp") && targetedEnemy.DistanceSquaredToPlayer < 8 * 8)
                            return CastAtPlayerLocation("War Stomp");
                        if (pet != null && targetedEnemy.IsSameAs(pet.Target) && IsSpellReady("Intimidation"))
                            return CastAtTarget("Intimidation");
                    }
                    if (player.HasActivePet && targetedEnemy.IsTargetingPlayer && IsSpellReady("Feign Death"))
                        return CastAtPlayer("Feign Death");
                    if (targetedEnemy.IsTargetingPlayer && IsSpellReady("Scatter Shot"))
                        return CastAtTarget("Scatter Shot");
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp"))
                        return CastAtPlayerLocation("War Stomp");
                    if (player.HasActivePet && pet != null && !pet.IsDead && IsSpellReady("Kill Command") && pet != null && Vector3.DistanceSquared(pet.Position, targetedEnemy.Position) < 25)
                        return CastAtTarget("Kill Command");
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
                return CastAtTarget("Auto Shot");
            }
            return null;
        }
    }
}
