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
    public class HunterBM : IPMRotation
    {
        private HunterSettings settings => ((CataCombatSettings)SettingsManager.Instance.Settings).Hunter;
        public IEnumerable<WowVersion> SupportedVersions => [WowVersion.Classic_Cata, WowVersion.Classic_Cata_Ptr];
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Hunter;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Hunter-BM";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var inv = om.Inventory;
            var sb = om.SpellBook;
            var pet = om.PlayerPet;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDebuff("Hunter's Mark"))
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
            if (IsSpellReady("Arcane Shot") && player.PowerPercent > 50)
                return CastAtTarget("Arcane Shot");
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
            var sb = om.SpellBook;
            var inv = om.Inventory;
            var comboPoints = player.SecondaryPower;
            List<WowUnit>? inCombatEnemies = null;
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
            if (om.IsPlayerFleeingFromCombat)
            {
                if (IsSpellReady("Feign Death"))
                    return CastWithoutTargeting("Feign Death", isHarmfulSpell: false);
                if (!IsClassicEra)
                {
                    if (IsSpellReady("Frost Trap"))
                        return CastWithoutTargeting("Frost Trap");
                    if (IsSpellReady("Freezing Trap"))
                        return CastWithoutTargeting("Freezing Trap");
                }
                return null;
            }
            if (player.PetStatus == PetStatus.Unknown)
            {
                if (IsSpellReady("Call Pet"))
                    return CastWithoutTargeting("Call Pet", isHarmfulSpell: false);
            }
            // Pet management
            if (pet == null || pet.IsDead)
            {
                if (settings.AutoRevivePet && IsSpellReadyOrCasting("Revive Pet"))
                    return CastAtPlayer("Revive Pet");
                if (IsSpellReady("Call Pet"))
                    return CastAtPlayer("Call Pet");
            }
            else
            {
                if (pet.HealthPercent <= settings.MendPetPercent && IsSpellReady("Mend Pet") && !pet.HasBuff("Mend Pet"))
                    return CastWithoutTargeting("Mend Pet");

            }
            // Misdirection to pet or focus target
            if (settings.UseMisdirection && IsSpellReady("Misdirection"))
            {
                if (om.FocusedUnit != null && targetedEnemy != null && !targetedEnemy.IsSameAs(om.FocusedUnit.Target) && !player.HasAura("Misdirection"))
                    return CastAtFocus("Misdirection");
                if (pet != null && !pet.IsDead && !player.HasAura("Misdirection"))
                    return CastAtPet("Misdirection");
            }
            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (IsSpellReady("Bestial Wrath") && !player.HasBuff("Bestial Wrath"))
                    return CastWithoutTargeting("Bestial Wrath", isHarmfulSpell: false);
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastWithoutTargeting("Berserking", isHarmfulSpell: false);
                if (IsSpellReady("Rapid Fire"))
                    return CastWithoutTargeting("Rapid Fire", isHarmfulSpell: false);
            }
            inCombatEnemies = om.InCombatEnemies.ToList();
            var mendPetAt = !IsClassicEra || inCombatEnemies.Count(e => e.HealthPercent > 50) > 1 ? 70 : 40;
            if (pet != null && !pet.IsDead && (!IsClassicEra || !player.IsMoving) && IsSpellReadyOrCasting("Mend Pet") && (pet.HealthPercent <= mendPetAt || IsSpellCasting("Mend Pet")) && (IsClassicEra || !pet.HasBuff("Mend Pet")))
                return CastAtPet("Mend Pet");

            if (settings.UseDisengage && !IsClassicEra && inCombatEnemies.Any(e => e.IsTargetingPlayer && e.DistanceSquaredToPlayer < 40) && IsSpellReady("Disengage"))
                return CastWithoutTargeting("Disengage", isHarmfulSpell: false);

            if (player.HealthPercent < 40)
            {
                if (inCombatEnemies.Any(e => e.IsInPlayerMeleeRange && e.IsTargetingPlayer) && IsSpellReady("Aspect of the Monkey"))
                {
                    if (!player.HasBuff("Aspect of the Monkey"))
                        return CastWithoutTargeting("Aspect of the Monkey", isHarmfulSpell: false);
                }
                else
                {
                    if (player.PowerPercent < settings.AspectSwitchFocusPercent && inCombatEnemies.Any(e => e.IsInPlayerMeleeRange && e.IsTargetingPlayer))
                    {
                        if (settings.UseAspectOfTheFox && IsSpellReady("Aspect of the Fox"))
                        {
                            if (!player.HasBuff("Aspect of the Fox"))
                                return CastWithoutTargeting("Aspect of the Fox", isHarmfulSpell: false);
                        }
                        else if (settings.UseAspectOfTheHawk && IsSpellReady("Aspect of the Hawk"))
                        {
                            if (!player.HasBuff("Aspect of the Hawk"))
                                return CastWithoutTargeting("Aspect of the Hawk", isHarmfulSpell: false);
                        }
                    }
                    else if (settings.UseAspectOfTheHawk && IsSpellReady("Aspect of the Hawk"))
                    {
                        if (!player.HasBuff("Aspect of the Hawk"))
                            return CastWithoutTargeting("Aspect of the Hawk", isHarmfulSpell: false);
                    }
                    else if (IsSpellReady("Aspect of the Monkey"))
                    {
                        if (!player.HasBuff("Aspect of the Monkey"))
                            return CastWithoutTargeting("Aspect of the Monkey", isHarmfulSpell: false);
                    }
                }
            }
            else
            {
                if (((player.HealthPercent < 35 && inCombatEnemies.Any(e => e.IsInPlayerMeleeRange && e.IsTargetingPlayer))
                    || (!PlayerLearnedSpell("Aspect of the Hawk") && !PlayerLearnedSpell("Aspect of the Fox")))
                    && IsSpellReady("Aspect of the Monkey") && !player.HasBuff("Aspect of the Monkey"))
                    return CastWithoutTargeting("Aspect of the Monkey", isHarmfulSpell: false);
                else if (player.PowerPercent < settings.AspectSwitchFocusPercent && inCombatEnemies.Any(e => e.IsInPlayerMeleeRange && e.IsTargetingPlayer))
                {
                    if (settings.UseAspectOfTheFox && IsSpellReady("Aspect of the Fox"))
                    {
                        if (!player.HasBuff("Aspect of the Fox"))
                            return CastWithoutTargeting("Aspect of the Fox", isHarmfulSpell: false);
                    }
                    else if (settings.UseAspectOfTheHawk && IsSpellReady("Aspect of the Hawk"))
                    {
                        if (!player.HasBuff("Aspect of the Hawk"))
                            return CastWithoutTargeting("Aspect of the Hawk", isHarmfulSpell: false);
                    }
                }
                else if (settings.UseAspectOfTheHawk && IsSpellReady("Aspect of the Hawk"))
                {
                    if (!player.HasBuff("Aspect of the Hawk"))
                        return CastWithoutTargeting("Aspect of the Hawk", isHarmfulSpell: false);
                }
            }
            if (IsClassicEra && IsSpellReady("Heart of the Lion") && !player.HasBuff("Heart of the Lion"))
                return CastWithoutTargeting("Heart of the Lion");

            // Offensive cooldowns
            if (settings.UseBestialWrath && IsSpellReady("Bestial Wrath"))
                return CastAtPlayer("Bestial Wrath");

            if (settings.UseRapidFire && IsSpellReady("Rapid Fire"))
                return CastAtPlayer("Rapid Fire");
            //AoE handling
            var minRange = IsClassicEra ? 8f : 5f;
            minRange += player.CombatReach;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (pet != null && pet.DistanceSquaredToPlayer < 225 && nearbyEnemies.Count(e => e.IsTargetingPlayer) > 2)
                {
                    if (IsSpellReady("Feign Death"))
                        return CastWithoutTargeting("Feign Death", isHarmfulSpell: false);
                }
                if (nearbyEnemies.Count > 4)
                {
                    if (!player.HasBuff("Trap Launcher") && IsSpellReady("Explosive Trap"))
                        return CastWithoutTargeting("Explosive Trap");
                }
                if (PlayerLearnedSpell("Trap Launcher"))
                {
                    if (inCombatEnemies.Count <= 4)
                    {
                        var freezeTarget = inCombatEnemies.Where(e => !e.IsSameAs(player.Target) && (!e.IsMoving || e.IsPlayer) && e.DistanceSquaredToPlayer < 40 * 40).OrderBy(u => u.DistanceSquaredToPlayer).FirstOrDefault();
                        if (freezeTarget != null)
                        {
                            if (IsSpellReady("Freezing Trap"))
                            {
                                if (player.HasBuff("Trap Launcher"))
                                    return CastAtGround(freezeTarget.Position, "Freezing Trap");
                                else if (IsSpellReady("Trap Launcher"))
                                    return CastWithoutTargeting("Trap Launcher", isHarmfulSpell: false);
                            }
                        }
                    }
                    var trapTarget = inCombatEnemies.Where(e => (!e.IsMoving || e.IsPlayer) && e.DistanceSquaredToPlayer < 40 * 40 && e.GetNearbyInCombatEnemies(6).Count >= 2).OrderBy(u => u.DistanceSquaredToPlayer).FirstOrDefault();
                    if (trapTarget != null)
                    {
                        if (IsSpellReady("Explosive Trap"))
                        {
                            if (player.HasBuff("Trap Launcher"))
                                return CastAtGround(trapTarget.Position, "Explosive Trap");
                            else if (IsSpellReady("Trap Launcher"))
                                return CastWithoutTargeting("Trap Launcher", isHarmfulSpell: false);
                        }
                    }
                    else if (player.HasBuff("Trap Launcher"))
                    {
                        return CastAtGround(inCombatEnemies[0].Position, "Explosive Trap");
                    }
                }
                if (IsSpellReady("Freezing Arrow"))
                {
                    var ccCandidates = inCombatEnemies.Where(e => e.DistanceSquaredToPlayer > 64 && e.HealthPercent > 25 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root));
                    foreach (var ccCandidate in ccCandidates)
                    {
                        if (!ccCandidate.HasDebuff("Freezing Arrow"))
                            return CastAtUnit(ccCandidate, "Freezing Arrow");
                    }
                }
                if (nearbyEnemies.Count > 2)
                {
                    if (!player.HasBuff("Trap Launcher") && IsSpellReady("Explosive Trap"))
                        return CastWithoutTargeting("Explosive Trap");
                }
                if (inCombatEnemies.Count(e => e.IsTargetingPlayer || e.IsTargetingPlayerPet) >= 2)
                {
                    if (IsSpellReady("Rapid Fire") && (player.HasBuff("Call of the Wild") || !sb.PetSpells.Any(s => s.Name == "Call of the Wild" && s.GetCooldown().TotalSeconds < 60)) && !player.HasBuff("Bloodlust") && !player.HasBuff("Heroism") && !player.HasBuff("Time Warp") && !player.HasBuff("The Beast Within"))
                        return CastAtPlayer("Rapid Fire");
                    if (player.PowerPercent <= 50 && (pet is null || player.PetStatus == PetStatus.Dead || pet.PowerPercent <= 50) && IsSpellReady("Fervor"))
                        return CastWithoutTargeting("Fervor", isHarmfulSpell: false);
                    if (IsSpellReady("Bestial Wrath") && !player.HasBuff("Bestial Wrath") && (!PlayerLearnedSpell("Kill Command") || GetSpellCooldown("Kill Command").TotalSeconds < 2))
                        return CastWithoutTargeting("Bestial Wrath", isHarmfulSpell: false);
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastWithoutTargeting("Berserking", isHarmfulSpell: false);
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
            }
            if (IsSpellReady("Kill Command"))
                return CastWithoutTargeting("Kill Command");

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
                            return CastWithoutTargeting("War Stomp");
                        if (pet != null && targetedEnemy.IsSameAs(pet.Target) && IsSpellReady("Intimidation"))
                            return CastAtTarget("Intimidation");
                    }
                    if (targetedEnemy.IsMoving && !targetedEnemy.HasDebuff("Wing Clip") && IsSpellReady("Concussive Shot") && targetedEnemy.IsInCombatWithPlayer && targetedEnemy.IsTargetingPlayer)
                        return CastAtTarget("Concussive Shot");
                    if (!targetedEnemy.IsInCombat)
                    {
                        if (IsSpellReady("Serpent Sting"))
                            return CastAtTarget("Serpent Sting");
                        if (IsSpellReady("Arcane Shot"))
                            return CastAtTarget("Arcane Shot");
                    }
                    if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDebuff("Hunter's Mark"))
                        return CastAtTarget("Hunter's Mark");
                    if (targetedEnemy.HealthPercent <= settings.KillShotHealthPercent)
                    {
                        if (IsSpellReady("Kill Shot"))
                            return CastAtTarget("Kill Shot");
                    }
                    if ((targetedEnemy.IsTargetingPlayer || targetedEnemy.Target is null) && pet != null && IsSpellReady("Intimidation"))
                        return CastAtTarget("Intimidation");
                    if (player.HasActivePet && IsSpellReady("Kill Command") && pet != null && Vector3.DistanceSquared(pet.Position, targetedEnemy.Position) < 25)
                        return CastAtTarget("Kill Command");
                    if (pet != null && pet.AuraStacks("Frenzy Effect") >= 5 && !player.HasBuff("The Beast Within") && IsSpellReady("Focus Fire"))
                        return CastWithoutTargeting("Focus Fire", isHarmfulSpell: false);
                    if (player.IsMoving && IsSpellReady("Arcane Shot"))
                        return CastAtTarget("Arcane Shot");
                    if (IsClassicEra)
                    {
                        if (targetedEnemy.HealthPercent > 30 && IsSpellReady("Serpent Sting") && !targetedEnemy.HasDebuff("Serpent Sting"))
                            return CastAtTarget("Serpent Sting");
                        if (IsSpellReady("Chimera Shot") && (player.Level < 10 || targetedEnemy.HasDebuff("Serpent Sting") || targetedEnemy.HasDebuff("Scorpid Sting") || targetedEnemy.HasDebuff("Viper Sting")))
                            return CastAtTarget("Chimera Shot");
                    }
                    else if (player.PowerPercent > 50 && targetedEnemy.HealthPercent > 60
                        && IsSpellReady("Serpent Sting") && !targetedEnemy.HasDebuff("Serpent Sting"))
                        return CastAtTarget("Serpent Sting");
                    if (!targetedEnemy.IsTargetingPlayer && player.Level < 50 && player.PowerPercent > 50
                        && targetedEnemy.HealthPercent > 40 && !player.IsMoving && IsSpellReadyOrCasting("Aimed Shot"))
                        return CastAtTarget("Aimed Shot");
                    if (IsClassicEra && !player.IsMoving && player.PowerPercent > 60 && IsSpellReadyOrCasting("Multi-Shot"))
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
                            return CastWithoutTargeting("War Stomp");
                        if (pet != null && targetedEnemy.IsSameAs(pet.Target) && IsSpellReady("Intimidation"))
                            return CastAtTarget("Intimidation");
                    }
                    if (player.HasActivePet && targetedEnemy.IsTargetingPlayer && IsSpellReady("Feign Death"))
                        return CastAtPlayer("Feign Death");
                    if (targetedEnemy.IsTargetingPlayer && IsSpellReady("Scatter Shot"))
                        return CastAtTarget("Scatter Shot");
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp"))
                        return CastWithoutTargeting("War Stomp");
                    if (pet != null && targetedEnemy.IsSameAs(pet.Target) && IsSpellReady("Intimidation"))
                        return CastAtTarget("Intimidation");
                    if (player.HasActivePet && targetedEnemy.IsTargetingPlayer && IsSpellReady("Feign Death"))
                        return CastAtPlayer("Feign Death");
                    if (targetedEnemy.IsTargetingPlayer && IsSpellReady("Scatter Shot"))
                        return CastAtTarget("Scatter Shot");
                    if (player.HasActivePet && pet != null && !pet.IsDead && IsSpellReady("Kill Command") && pet != null && Vector3.DistanceSquared(pet.Position, targetedEnemy.Position) < 25)
                        return CastAtTarget("Kill Command");
                    if (targetedEnemy.IsInPlayerMeleeRange)
                    {
                        if (IsClassicEra && IsSpellReady("Flanking Strike"))
                            return CastAtTarget("Flanking Strike");
                        if (IsSpellReady("Raptor Strike"))
                            return CastAtTarget("Raptor Strike");
                        if (!targetedEnemy.HasDebuff("Wing Clip") && IsSpellReady("Wing Clip"))
                            return CastAtTarget("Wing Clip");
                        if (!IsClassicEra && IsSpellReady("Frost Trap"))
                            return CastWithoutTargeting("Frost Trap");
                        if (!IsClassicEra && IsSpellReady("Freezing Trap"))
                            return CastWithoutTargeting("Freezing Trap");
                        if (inv.IsMeleeWeaponReady)
                            return CastAtTarget(sb.AutoAttack);
                    }
                    if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                        return CastAtTarget(sb.AutoAttack);
                }
            }
            return null;
        }
    }
}
