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
using AdvancedCombatClasses.Settings.Era;
using PixelMaster.Server.Shared;

namespace CombatClasses
{
    public class ShamanElementalRotation : IPMRotation
    {
        private ShamanSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Shaman;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 1; // 1 for Elemental Spec
        public UnitClass PlayerClass => UnitClass.Shaman;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Era][PvE]Shaman-Elemental";
        public string Author => "PixelMaster";
        public string Description => "Elemental Shaman rotation for WoW Classic Era and Season of Discovery";
        public List<RotationMode> AvailableRotations => new() { RotationMode.Auto, RotationMode.Normal, RotationMode.Instance };
        public RotationMode PreferredMode { get; set; } = RotationMode.Auto;

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;

            if (target != null)
            {
                // Open with Flame Shock
                if (IsSpellReady("Flame Shock") && !target.HasAura("Flame Shock"))
                {
                    return CastAtTarget("Flame Shock");
                }

                // Start casting Lightning Bolt
                if (IsSpellReadyOrCasting("Lightning Bolt"))
                {
                    return CastAtTarget("Lightning Bolt");
                }
            }

            return null;
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;
            var inCombatEnemies = om.InCombatEnemies.ToList();

            // Ensure pre-fight buffs are active
            if (!player.HasAura("Flametongue Weapon") && IsSpellReady("Flametongue Weapon"))
            {
                return CastAtPlayer("Flametongue Weapon");
            }

            // Healing logic
            if (settings.ElementalHeal)
            {
                if (player.HealthPercent <= 30 && IsSpellReadyOrCasting("Healing Surge"))
                {
                    return CastAtPlayer("Healing Surge");
                }
                if (player.HealthPercent <= 50 && IsSpellReadyOrCasting("Healing Wave"))
                {
                    return CastAtPlayer("Healing Wave");
                }
            }

            // Dispel logic
            if (player.HasDebuffType(SpellDispelType.Curse) && IsSpellReady("Cleanse Spirit"))
            {
                return CastAtPlayer("Cleanse Spirit");
            }

            if (target != null)
            {
                if (IsSpellReady("Feral Spirit"))
                {
                    return CastWithoutTargeting("Feral Spirit");
                }
                // Ensure we have enough mana
                if (player.PowerPercent <= 20 && IsSpellReady("Shamanistic Rage"))
                {
                    return CastWithoutTargeting("Shamanistic Rage");
                }

                // Apply Lightning Shield if not present
                if (!player.HasAura("Lightning Shield") && IsSpellReady("Lightning Shield"))
                {
                    return CastWithoutTargeting("Lightning Shield");
                }

                // Apply Flame Shock if not present
                if (!target.HasAura("Flame Shock") && IsSpellReady("Flame Shock"))
                {
                    return CastAtTarget("Flame Shock");
                }

                // Use Earth Shock if Rolling Thunder stacks are >= 8
                if (player.AuraStacks("Rolling Thunder") >= 8 && IsSpellReady("Earth Shock"))
                {
                    return CastAtTarget("Earth Shock");
                }

                // Use Lava Burst if Flame Shock is on the target
                if (target.HasAura("Flame Shock") && IsSpellReadyOrCasting("Lava Burst"))
                {
                    return CastAtTarget("Lava Burst");
                }

                // Chain Lightning for AoE
                if (settings.IncludeAoeRotation && inCombatEnemies.Count >= 3 && IsSpellReadyOrCasting("Chain Lightning"))
                {
                    return CastAtTarget("Chain Lightning");
                }

                // Use Fire Nova if Chain Lightning crits
                if (settings.IncludeAoeRotation && inCombatEnemies.Count >= 3 && IsSpellReady("Fire Nova") && player.HasAura("Chain Lightning Crit"))
                {
                    return CastAtTarget("Fire Nova");
                }

                // Replace Searing Totem with Magma Totem if there are 3 or more targets
                if (settings.IncludeAoeRotation && inCombatEnemies.Count >= 3 && IsSpellReady("Magma Totem") && !player.HasAura("Magma Totem"))
                {
                    return CastWithoutTargeting("Magma Totem");
                }

                // Spread Flame Shock to multiple targets
                foreach (var enemy in inCombatEnemies)
                {
                    if (!enemy.HasAura("Flame Shock") && IsSpellReady("Flame Shock"))
                    {
                        return CastAtUnit(enemy, "Flame Shock");
                    }
                }

                // Lightning Bolt as filler
                if (IsSpellReadyOrCasting("Lightning Bolt"))
                {
                    return CastAtTarget("Lightning Bolt");
                }

                // Use Earth Shock while moving
                if (player.IsMoving && IsSpellReady("Earth Shock"))
                {
                    return CastAtTarget("Earth Shock");
                }
                if (!player.IsCasting && !target.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }

            return null;
        }
    }
}
