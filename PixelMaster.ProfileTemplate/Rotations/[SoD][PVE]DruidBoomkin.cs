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
    public class DruidBalanceRotation : IPMRotation
    {
        private DruidSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Druid;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 1; // 1 for Balance Spec
        public UnitClass PlayerClass => UnitClass.Druid;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Era][PvE]Druid-Balance";
        public string Author => "PixelMaster";
        public string Description => "Balance Druid rotation for WoW Classic Era and Season of Discovery";
        public List<RotationMode> AvailableRotations => new() { RotationMode.Auto, RotationMode.Normal, RotationMode.Instance };
        public RotationMode PreferredMode { get; set; } = RotationMode.Auto;

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                // Use Moonfire to pull
                if (IsSpellReady("Moonfire"))
                {
                    return CastAtTarget("Moonfire");
                }
                // Use Wrath if Moonfire is not available
                if (IsSpellReadyOrCasting("Wrath"))
                {
                    return CastAtTarget("Wrath");
                }
            }
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            var inCombatEnemies = om.InCombatEnemies;

            // Dispel logic
            if (settings.RemoveCurseInCombat && player.HasDebuffType(SpellDispelType.Curse) && IsSpellReady("Remove Curse"))
            {
                return CastAtPlayer("Remove Curse");
            }
            if (settings.RemovePoisonInCombat && player.HasDebuffType(SpellDispelType.Poison) && IsSpellReady("Cure Poison"))
            {
                return CastAtPlayer("Cure Poison");
            }

            // Healing logic
            if (!settings.NoHealBalanceAndFeral)
            {
                if (player.HealthPercent <= settings.NonRestoHealingTouch && IsSpellReadyOrCasting("Healing Touch"))
                {
                    return CastAtPlayer("Healing Touch");
                }
                if (player.HealthPercent <= settings.NonRestoRegrowth && IsSpellReadyOrCasting("Regrowth"))
                {
                    return CastAtPlayer("Regrowth");
                }
                if (player.HealthPercent <= settings.NonRestoRejuvenation && IsSpellReady("Rejuvenation"))
                {
                    return CastAtPlayer("Rejuvenation");
                }
            }

            // Defensive Cooldowns
            if (player.HealthPercent <= settings.BarkskinHealth && IsSpellReady("Barkskin"))
            {
                return CastAtPlayer("Barkskin");
            }

            if (targetedEnemy != null)
            {
                // Use Starfall if enabled in settings
                if (settings.UseStarfall && IsSpellReady("Starfall"))
                {
                    return CastAtPlayer("Starfall");
                }
                // Use Force of Nature if enabled
                if (settings.UseForceOfNature && IsSpellReady("Force of Nature"))
                {
                    return CastAtGround(targetedEnemy.Position, "Force of Nature");
                }
                // Apply Moonfire if not present
                if (!targetedEnemy.HasAura("Moonfire") && IsSpellReady("Moonfire"))
                {
                    return CastAtTarget("Moonfire");
                }
                // Apply Insect Swarm if not present
                if (!targetedEnemy.HasAura("Insect Swarm") && IsSpellReady("Insect Swarm"))
                {
                    return CastAtTarget("Insect Swarm");
                }
                // Cast Wrath as main filler
                if (IsSpellReadyOrCasting("Wrath"))
                {
                    return CastAtTarget("Wrath");
                }
                if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
