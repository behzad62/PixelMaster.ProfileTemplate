using PixelMaster.Core.API;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;

using static PixelMaster.Core.API.PMRotationBuilder;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Profiles;
using PixelMaster.Core.Behaviors;
using PixelMaster.Core.Behaviors.Transport;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;
using PixelMaster.Server.Shared;

namespace CombatClasses
{
    public class HunterBeastMasteryRotation : IPMRotation
    {
        private HunterSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Hunter;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 1; // 1 for Beast Mastery
        public UnitClass PlayerClass => UnitClass.Hunter;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Era][PvE]Hunter-BeastMastery";
        public string Author => "PixelMaster";
        public string Description => "Beast Mastery Hunter rotation for PvE content in WoW Classic Era";

        public List<RotationMode> AvailableRotations => new() { RotationMode.Auto, RotationMode.Normal, RotationMode.Instance };
        public RotationMode PreferredMode { get; set; } = RotationMode.Auto;

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var pet = om.PlayerPet;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                // Apply Hunter's Mark if not present
                if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDebuff("Hunter's Mark"))
                    return CastAtTarget("Hunter's Mark");

                // Send pet to attack
                if (settings.StartPullWithPetAttack && pet != null && !pet.IsDead && !targetedEnemy.IsSameAs(pet.Target))
                    return PetAttack();

                // Use Serpent Sting to apply DoT
                if (IsSpellReady("Serpent Sting") && !targetedEnemy.HasDebuff("Serpent Sting"))
                    return CastAtTarget("Serpent Sting");

                // Use Arcane Shot as a focus dump
                if (IsSpellReady("Arcane Shot") && player.PowerPercent > 50)
                    return CastAtTarget("Arcane Shot");
                if (IsSpellReady("Auto Shot"))
                    return CastAtTarget("Auto Shot");
            }

            // Default to Auto Shot
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var pet = om.PlayerPet;
            var sb = om.SpellBook;
            var inv = om.Inventory;
            var targetedEnemy = om.AnyEnemy;
            List<WowUnit>? inCombatEnemies = null;

            // Pet management
            if (pet == null || pet.IsDead)
            {
                if (settings.AutoRevivePet && IsSpellReadyOrCasting("Revive Pet"))
                    return CastWithoutTargeting("Revive Pet");
                if (IsSpellReady("Call Pet"))
                    return CastAtPlayer("Call Pet");
            }
            else
            {
                if ((pet.HealthPercent <= settings.MendPetPercent || IsSpellCasting("Mend Pet")) && !player.IsMoving && IsSpellReadyOrCasting("Mend Pet"))
                    return CastWithoutTargeting("Mend Pet");
            }
            inCombatEnemies = om.InCombatEnemies.ToList();
            // Aspect switching based on mana
            if(player.HealthPercent < 40 && inCombatEnemies.Any(e => e.IsInPlayerMeleeRange && e.IsTargetingPlayer) && IsSpellReady("Aspect of the Monkey"))
            {
                if (!player.HasBuff("Aspect of the Monkey"))
                    return CastWithoutTargeting("Aspect of the Monkey", isHarmfulSpell: false);
            }
            else if (player.PowerPercent < settings.AspectSwitchManaPercent && settings.UseAspectOfTheViper && IsSpellReady("Aspect of the Viper") && !player.HasAura("Aspect of the Viper"))
                return CastWithoutTargeting("Aspect of the Viper");
            else if (player.PowerPercent >= settings.AspectSwitchManaPercent && settings.UseAspectOfTheHawk && IsSpellReady("Aspect of the Hawk") && !player.HasAura("Aspect of the Hawk"))
                return CastWithoutTargeting("Aspect of the Hawk");

            // Misdirection to pet or focus target (not available in Classic Era)
            // Defensive cooldown: Disengage (not available in Classic Era)

            // Offensive cooldowns
            if (settings.UseBestialWrath && IsSpellReady("Bestial Wrath"))
                return CastWithoutTargeting("Bestial Wrath");

            // Kill Command when available
            if (IsSpellReady("Kill Command"))
                return CastAtTarget("Kill Command");

            var minRange = 8f;
            minRange += player.CombatReach;

            if (settings.UseBestialWrath)
            {
                if (player.HasActivePet && IsSpellReady("Bestial Wrath") && !player.HasBuff("Bestial Wrath"))
                    return CastWithoutTargeting("Bestial Wrath", isHarmfulSpell: false);
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastWithoutTargeting("Berserking", isHarmfulSpell: false);
            }

            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (pet != null && pet.DistanceSquaredToPlayer < 225 && nearbyEnemies.Count(e => e.IsTargetingPlayer) > 2)
                {
                    if (IsSpellReady("Feign Death"))
                        return CastWithoutTargeting("Feign Death", isHarmfulSpell: false);
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

            // Main rotation
            if (targetedEnemy != null)
            {
                // Apply or refresh Serpent Sting
                if (targetedEnemy.DistanceSquaredToPlayer > minRange * minRange)
                {
                    if (targetedEnemy.IsCasting)
                    {
                        if (IsSpellReady("Scatter Shot") && targetedEnemy.DistanceSquaredToPlayer <= 20 * 20)
                            return CastAtTarget("Scatter Shot");
                        if (pet != null && targetedEnemy.IsSameAs(pet.Target) && IsSpellReady("Intimidation"))
                            return CastAtTarget("Intimidation");
                    }
                    if (targetedEnemy.IsMoving && !targetedEnemy.HasDebuff("Wing Clip") && IsSpellReady("Concussive Shot") && targetedEnemy.IsInCombatWithPlayer && targetedEnemy.IsTargetingPlayer)
                        return CastAtTarget("Concussive Shot");
                    if (IsSpellReady("Hunter's Mark") && !targetedEnemy.HasDebuff("Hunter's Mark"))
                        return CastAtTarget("Hunter's Mark");
                    if (IsSpellReady("Serpent Sting") && (settings.UseSerpentSting || PlayerLearnedSpell("Chimera Shot")) && !targetedEnemy.HasDebuff("Serpent Sting"))
                        return CastAtTarget("Serpent Sting");
                    if (IsSpellReady("Chimera Shot") && (player.Level < 10 || targetedEnemy.HasDebuff("Serpent Sting") || targetedEnemy.HasDebuff("Scorpid Sting") || targetedEnemy.HasDebuff("Viper Sting")))
                        return CastAtTarget("Chimera Shot");
                    // Focus dump with Arcane Shot
                    if (IsSpellReady("Arcane Shot") && (player.PowerPercent > 70 || player.IsMoving))
                        return CastAtTarget("Arcane Shot");

                    // Use Multi-Shot if multiple enemies are present
                    if (IsSpellReady("Multi-Shot") && (player.PowerPercent >= settings.MultiShotManaPercent || om.InCombatEnemies.Count() > 1))
                        return CastAtTarget("Multi-Shot");
                    if (!targetedEnemy.IsTargetingPlayer && player.Level < 50 && player.PowerPercent > 50
                        && targetedEnemy.HealthPercent > 40 && !player.IsMoving && IsSpellReadyOrCasting("Aimed Shot"))
                        return CastAtTarget("Aimed Shot");
                    // Use Steady Shot to regenerate focus (Not available in Classic Era)
                    // Default to Auto Shot
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
                        if (inv.IsMeleeWeaponReady && !targetedEnemy.IsPlayerAttacking)
                            return CastAtTarget(sb.AutoAttack);
                    }
                    if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking && inv.IsMeleeWeaponReady)
                        return CastAtTarget(sb.AutoAttack);
                }
            }

            return null;
        }
    }
}
