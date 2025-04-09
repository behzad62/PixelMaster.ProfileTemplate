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
    public class SoDPriestShadowRotation : IPMRotation
    {
        private PriestSettings Settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Priest;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 3; // Shadow specialization
        public UnitClass PlayerClass => UnitClass.Priest;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[SoD][PvE]Priest-Shadow";
        public string Author => "PixelMaster";
        public string Description => "Shadow Priest rotation for WoW Classic Season of Discovery and Era";

        public SpellCastInfo PullSpell()
        {
            var player = ObjectManager.Instance.Player;
            var target = ObjectManager.Instance.AnyEnemy;

            if (target != null)
            {
                // Ensure Shadowform is active
                if (IsSpellReady("Shadowform") && !player.HasAura("Shadowform"))
                    return CastAtPlayer("Shadowform");

                // Start with Vampiric Touch if available
                if (IsSpellReadyOrCasting("Vampiric Touch"))
                    return CastAtTarget("Vampiric Touch");

                // Use Mind Blast
                if (IsSpellReadyOrCasting("Mind Blast"))
                    return CastAtTarget("Mind Blast");

                // Apply Shadow Word: Pain
                if (IsSpellReady("Shadow Word: Pain"))
                    return CastAtTarget("Shadow Word: Pain");

                if (IsSpellReadyOrCasting("Smite"))
                    return CastAtTarget("Smite");
            }

            // Default to auto attack
            return CastAtTarget(ObjectManager.Instance.SpellBook.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;
            var enemies = om.InCombatEnemies;

            // 1. Self-Healing and Defensive Cooldowns
            if (player.HealthPercent < Settings.ShieldHealthPercent && !player.HasAura("Weakened Soul") && IsSpellReady("Power Word: Shield"))
                return CastAtPlayer("Power Word: Shield");

            if (player.HealthPercent < Settings.ShadowFlashHealHealth && IsSpellReadyOrCasting("Flash Heal"))
                return CastAtPlayer("Flash Heal");

            if (player.HealthPercent < Settings.HealThreshold)
            {
                if (IsSpellReady("Desperate Prayer"))
                    return CastWithoutTargeting("Desperate Prayer");

                if (IsSpellReady("Dispersion"))
                    return CastWithoutTargeting("Dispersion");
            }

            if (Settings.UsePsychicScream && enemies.Count(e => e.IsInPlayerMeleeRange) >= Settings.PsychicScreamAddCount && IsSpellReady("Psychic Scream"))
                return CastWithoutTargeting("Psychic Scream");

            if (player.PowerPercent < Settings.DispersionMana && IsSpellReady("Dispersion"))
                return CastWithoutTargeting("Dispersion");


            // 2. Remove Debuffs
            if (player.HasDebuffType(SpellDispelType.Magic) && IsSpellReady("Dispel Magic"))
                return CastAtPlayer("Dispel Magic");
            if (player.HasDebuffType(SpellDispelType.Disease) && IsSpellReady("Cure Disease"))
                return CastAtPlayer("Cure Disease");

            // 3. Maintain Auras and Buffs
            if (IsSpellReady("Shadowform") && !player.HasAura("Shadowform"))
                return CastAtPlayer("Shadowform");

            // 4. Offensive Cooldowns
            if (Settings.UseShadowfiend && IsSpellReady("Shadowfiend"))
                return CastAtTarget("Shadowfiend");

            // 4. AoE Handling
            if (target != null && IsSpellCasting("Mind Sear"))
                return CastAtTarget("Mind Sear");
            if (Settings.UseAoE && target != null && target.GetNearbyInCombatEnemies(10).Count >= Settings.AoEEnemyCount)
            {
                if (IsSpellReadyOrCasting("Mind Sear"))
                    return CastAtTarget("Mind Sear");
            }

            // 5. Main Rotation
            if (target != null)
            {
                // Execute phase
                if (target.HealthPercent <= Settings.ExecuteThreshold && IsSpellReady("Shadow Word: Death"))
                    return CastAtTarget("Shadow Word: Death");

                // Reapply DoTs
                if (IsSpellReadyOrCasting("Vampiric Touch") && !target.HasAura("Vampiric Touch", castByPlayer: true))
                    return CastAtTarget("Vampiric Touch");

                if (IsSpellReady("Shadow Word: Pain") && !target.HasAura("Shadow Word: Pain", castByPlayer: true))
                    return CastAtTarget("Shadow Word: Pain");

                if (IsSpellReady("Devouring Plague") && !target.HasAura("Devouring Plague", castByPlayer: true))
                    return CastAtTarget("Devouring Plague");

                if (IsSpellReady("Inner Focus") && IsSpellReady("Mind Blast"))
                    return CastWithoutTargeting("Inner Focus");
                // Use Mind Blast on cooldown
                if (player.HasBuff("Inner Focus") && IsSpellReadyOrCasting("Mind Blast"))
                    return CastAtTarget("Mind Blast");

                if (player.ManaPercent > 25 && IsSpellReadyOrCasting("Mind Blast"))
                    return CastAtTarget("Mind Blast");

                // Use Mind Flay as filler
                if (IsSpellReadyOrCasting("Mind Flay"))
                    return CastAtTarget("Mind Flay");
                if (IsSpellReadyOrCasting("Mind Flay", 1))
                    return CastAtTarget("Mind Flay");


                if (Settings.UseWand)
                {
                    var wand = om.Inventory.GetEquippedItemsBySlot(EquipSlot.Ranged);
                    if (wand != null && IsSpellReady("Shoot"))
                        return CastAtTarget("Shoot");
                    else if (!player.IsCasting)
                        return CastAtTarget(sb.AutoAttack);
                }
                if (!player.IsCasting && !target.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }

            return null;
        }

    }
}
