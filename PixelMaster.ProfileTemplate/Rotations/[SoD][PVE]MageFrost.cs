using PixelMaster.Core.API;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;
using static PixelMaster.Core.API.PMRotationBuilder;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Profiles;
using PixelMaster.Core.Behaviors;
using PixelMaster.Core.Behaviors.Transport;
using PixelMaster.Server.Shared;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;

namespace CombatClasses
{
    public class MageFrostRotation : IPMRotation
    {
        private MageSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Mage;
        private WowUnit? lastPolyTarget;
        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 3; // 3 for Frost spec
        public UnitClass PlayerClass => UnitClass.Mage;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[SoD][PvE]Mage-Frost";
        public string Author => "PixelMaster";
        public string Description => "Frost Mage rotation for WoW Classic Season of Discovery and Era";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;

            if (target != null)
            {
                // Apply Arcane Intellect if not active
                if (!player.HasAura("Arcane Intellect") && IsSpellReady("Arcane Intellect"))
                    return CastWithoutTargeting("Arcane Intellect", isHarmfulSpell: false);

                // Use Ice Barrier if available
                if (IsSpellReady("Ice Barrier") && !player.HasAura("Ice Barrier"))
                    return CastWithoutTargeting("Ice Barrier", isHarmfulSpell: false);

                // Open with Frostbolt to pull
                if (IsSpellReadyOrCasting("Frostbolt"))
                    return CastAtTarget("Frostbolt");

                // Use Fireball if Frostbolt is not available
                if (IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");
            }

            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var inv = om.Inventory;
            var target = om.AnyEnemy;
            var enemies = om.InCombatEnemies.ToList();

            // Apply Arcane Intellect if not active
            if (!player.HasAura("Arcane Intellect") && IsSpellReady("Arcane Intellect"))
                return CastWithoutTargeting("Arcane Intellect", isHarmfulSpell: false);

            // Use Ice Barrier if available
            if (!player.IsCasting && IsSpellReady("Ice Barrier") && !player.HasAura("Ice Barrier"))
                return CastWithoutTargeting("Ice Barrier", isHarmfulSpell: false);

            // Use Evocation if mana is low
            if (settings.UseEvocation && (player.PowerPercent < settings.EvocationManaPercent || IsSpellCasting("Evocation")) && IsSpellReadyOrCasting("Evocation"))
                return CastWithoutTargeting("Evocation", isHarmfulSpell: false);

            // Use Ice Block if health is low
            if (player.HealthPercent < 15 && IsSpellReady("Ice Block"))
                return CastWithoutTargeting("Ice Block", isHarmfulSpell: false);

            // Use Cold Snap if available and major cooldowns are on cooldown
            if (!player.IsCasting && IsSpellReady("Cold Snap") && !IsSpellReady("Ice Barrier") && !IsSpellReady("Icy Veins"))
                return CastWithoutTargeting("Cold Snap", isHarmfulSpell: false);



            // Use offensive cooldowns
            if (!player.IsCasting && enemies.Count > 2 && IsSpellReady("Icy Veins"))
                return CastWithoutTargeting("Icy Veins");

            if (!player.IsCasting && enemies.Count > 2 && IsSpellReady("Mirror Image"))
                return CastWithoutTargeting("Mirror Image");

            // Enhanced AoE handling
            if (enemies.Count >= 3)
            {
                // Use Blizzard for large AoE
                var inMeleeRange = enemies.Count(u => u.IsTargetingPlayer && u.IsInPlayerMeleeRange);
                if (IsSpellCasting("Blizzard") || enemies.Count > 3 && inMeleeRange < 3)
                {
                    if (IsSpellCasting("Blizzard"))
                        return CastAtGround(LastGroundSpellLocation, "Blizzard");
                    if (!player.IsMoving && IsSpellReadyOrCasting("Blizzard"))
                    {
                        var AoELocation = GetBestAoELocation(enemies, 10f, out int numEnemiesInAoE);
                        if (numEnemiesInAoE >= 4)
                            return CastAtGround(AoELocation, "Blizzard");
                    }
                }

                // Use Frozen Orb for AoE
                if (IsSpellReadyOrCasting("Frozen Orb"))
                    return CastAtTarget("Frozen Orb");

                if (inMeleeRange >= 3 || player.IsStunned || player.IsSapped || player.IsRooted)
                {
                    if (IsSpellReady("Blink"))
                    {
                        Vector3? blinkTarget = GetSafePlaceAroundPlayer(20);
                        if (blinkTarget.HasValue)
                        {
                            return CastAtDirection(blinkTarget.Value, "Blink");
                        }
                        else
                            return CastWithoutTargeting("Blink", isHarmfulSpell: false);
                    }
                    if (!player.HasAura("Mana Shield") && IsSpellReady("Mana Shield"))
                        return CastWithoutTargeting("Mana Shield", isHarmfulSpell: false);
                }
            }

            if (!player.IsCasting)
            {
                if (enemies.Count > 2 && IsSpellReady("Mirror Image"))
                    return CastWithoutTargeting("Mirror Image");
                var inFrontCone = GetUnitsInFrontOfPlayer(enemies, 60, 8);
                if ((inFrontCone.Count >= 1 && player.HasAura("Improved Cone of Cold") || inFrontCone.Count > 1) && !inFrontCone.Any(e => e.HasAura("Polymorph")) && IsSpellReady("Cone of Cold"))
                    return CastWithoutTargeting("Cone of Cold");
                var closeEnemies = GetUnitsWithinArea(enemies, player.Position, 10);
                if (!closeEnemies.Any(e => e.HasAura("Polymorph")) && closeEnemies.Where(u => !u.HasAnyDebuff(false, "Freeze", "Frost Nova", "Dragon's Breath", "Improved Cone of Cold", "Deep Freeze") && !u.IsCCed).Count() >= 1 && (closeEnemies.Count > 1 || closeEnemies[0].HealthPercent > 50 || closeEnemies[0].IsElite))
                {
                    if (IsSpellReady("Frost Nova"))
                        return CastWithoutTargeting("Frost Nova");
                }
            }
            
            // Use Polymorph for crowd control
            if(lastPolyTarget != null && !enemies.Any(e => e.HasDebuff("Polymorph")) && enemies.Count(e=>e.IsInPlayerMeleeRange) <= 1)
                return CastAtUnit(lastPolyTarget, "Polymorph");
            else if (enemies.Count > 2 && IsSpellReady("Polymorph") && !enemies.Any(e => e.HasDebuff("Polymorph")) && enemies.Count(e => e.IsInPlayerMeleeRange) <= 1) 
            {
                lastPolyTarget = enemies.FirstOrDefault(e => IsViableForPolymorph(e, target));
                if (lastPolyTarget != null)
                    return CastAtUnit(lastPolyTarget, "Polymorph");
            }
            lastPolyTarget = null;

            if (target != null)
            {
                // Interrupt enemy casting
                if (target.IsCasting && IsSpellReady("Counterspell"))
                    return CastAtTarget("Counterspell");
                // Use Deep Freeze if target is frozen
                if (target.HasAura("Frost Nova", castByPlayer: true) && IsSpellReady("Deep Freeze"))
                    return CastAtTarget("Deep Freeze");

                // Use Brain Freeze procs
                if (player.HasAura("Brain Freeze") && IsSpellReadyOrCasting("Frostfire Bolt"))
                    return CastAtTarget("Frostfire Bolt");

                // Use Ice Lance when Fingers of Frost is active
                if (player.HasAura("Fingers of Frost") && IsSpellReady("Ice Lance"))
                    return CastAtTarget("Ice Lance");

                // Use Fire Blast when moving or if close
                if (player.IsMoving && IsSpellReady("Fire Blast"))
                    return CastAtTarget("Fire Blast");

                // Main filler: Frostbolt
                //if (IsSpellReadyOrCasting("Frostbolt"))
                //    return CastAtTarget("Frostbolt");

                if((target.HealthPercent <= 15 && !target.IsElite && !target.IsCasting 
                    || (player.PowerPercent < 30 && (target.IsInPlayerMeleeRange || target.HealthPercent < 30))
                     || (player.PowerPercent < 60 && target.HealthPercent < 30))
                     && enemies.Count <= 1 && IsSpellReadyOrCasting("Shoot"))
                    return CastAtTarget("Shoot");
                if (IsSpellReadyOrCasting("Frostbolt"))
                    return CastAtTarget("Frostbolt");
                if (IsSpellReadyOrCasting("Shoot"))
                    return CastAtTarget("Shoot");
                if (!PlayerLearnedSpell("Frostbolt") && IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");
                if (!player.IsCasting && !target.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }

            return null;
        }

        private static bool IsViableForPolymorph(WowUnit unit, WowUnit? currentTarget)
        {
            return unit != null &&
                   unit.IsAlive &&
                   //!unit.IsImmuned &&
                   !unit.HasDebuff("Polymorph") &&
                   (unit.CreatureType == CreatureType.Beast ||
                    unit.CreatureType == CreatureType.Humanoid) &&
                   (currentTarget == null || unit.WowGuid != currentTarget.WowGuid);
        }
    }
}
