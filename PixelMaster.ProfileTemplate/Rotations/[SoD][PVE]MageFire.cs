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
    public class MageFireRotation : IPMRotation
    {
        private MageSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Mage;
        private WowUnit? lastPolyTarget;
        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 2; // 2 for Fire specialization
        public UnitClass PlayerClass => UnitClass.Mage;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Era][PvE]Mage-Fire";
        public string Author => "PixelMaster";
        public string Description => "Fire Mage rotation for PvE content in WoW Classic Era and Season of Discovery";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                // Ensure Molten Armor is active
                if (!player.HasAura("Molten Armor") && IsSpellReady("Molten Armor"))
                    return CastWithoutTargeting("Molten Armor", isHarmfulSpell: false);

                // Start pull with Pyroblast if available
                if (IsSpellReadyOrCasting("Pyroblast"))
                    return CastAtTarget("Pyroblast");

                // Use Fireball as the main nuke
                if (IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");

                // Use Fire Blast if in range
                if (IsSpellReady("Fire Blast"))
                    return CastAtTarget("Fire Blast");
            }

            // Default to Auto Attack
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;

            // Apply Arcane Intellect if not active
            if (!player.HasAura("Arcane Intellect") && IsSpellReady("Arcane Intellect"))
                return CastWithoutTargeting("Arcane Intellect", isHarmfulSpell: false);

            // Maintain Molten Armor
            if (!player.HasAura("Molten Armor") && IsSpellReady("Molten Armor"))
                return CastWithoutTargeting("Molten Armor", isHarmfulSpell: false);

            // Use Evocation if mana is low
            if (settings.UseEvocation && (player.PowerPercent < settings.EvocationManaPercent || IsSpellCasting("Evocation")) && IsSpellReadyOrCasting("Evocation"))
                return CastWithoutTargeting("Evocation", isHarmfulSpell: false);

            // Use Ice Block if health is low
            if (player.HealthPercent < 15 && IsSpellReady("Ice Block"))
                return CastWithoutTargeting("Ice Block", isHarmfulSpell: false);

            // Use Combustion if enabled
            if (settings.UseCombustion && IsSpellReady("Combustion"))
                return CastWithoutTargeting("Combustion", isHarmfulSpell: false);


            var enemies = om.InCombatEnemies.ToList();
            if (!player.IsCasting)
            {
                if (enemies.Count > 2 && IsSpellReady("Mirror Image"))
                    return CastWithoutTargeting("Mirror Image");
                var inFrontCone = GetUnitsInFrontOfPlayer(enemies, 60, 8);
                if (inFrontCone.Count >= 2 && !inFrontCone.Any(e => e.HasAura("Polymorph")) && IsSpellReady("Dragon's Breath"))
                    return CastWithoutTargeting("Dragon's Breath");
                var closeEnemies = GetUnitsWithinArea(enemies, player.Position, 10);
                if (!closeEnemies.Any(e => e.HasAura("Polymorph")) && closeEnemies.Where(u => !u.HasAnyDebuff(false, "Freeze", "Frost Nova", "Dragon's Breath", "Improved Cone of Cold", "Deep Freeze") && !u.IsCCed).Count() >= 1 && (closeEnemies.Count > 1 || closeEnemies[0].HealthPercent > 50 || closeEnemies[0].IsElite))
                {
                    if (IsSpellReady("Frost Nova"))
                        return CastWithoutTargeting("Frost Nova");
                }
            }

            // Use Polymorph for crowd control
            if (lastPolyTarget != null && !enemies.Any(e => e.HasDebuff("Polymorph")) && enemies.Count(e => e.IsInPlayerMeleeRange) <= 1)
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
                // Apply Scorch debuff if not present
                if (settings.UseScorch && IsSpellReadyOrCasting("Scorch") && !target.HasDebuff("Fire Vulnerability"))
                    return CastAtTarget("Scorch");

                // Cast Pyroblast if Hot Streak is active
                if (player.HasAura("Hot Streak") && IsSpellReadyOrCasting("Pyroblast"))
                    return CastAtTarget("Pyroblast");

                // Use Fire Blast when moving or for instant damage
                if (player.IsMoving && IsSpellReady("Fire Blast"))
                    return CastAtTarget("Fire Blast");

                // Cast Fireball as the main spell
                if ((target.HealthPercent <= 15 && !target.IsElite && !target.IsCasting
                    || (player.PowerPercent < 30 && (target.IsInPlayerMeleeRange || target.HealthPercent < 30))
                     || (player.PowerPercent < 60 && target.HealthPercent < 30))
                     && enemies.Count <= 1 && IsSpellReadyOrCasting("Shoot"))
                    return CastAtTarget("Shoot");
                if (IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");
                if (IsSpellReadyOrCasting("Shoot"))
                    return CastAtTarget("Shoot");
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
