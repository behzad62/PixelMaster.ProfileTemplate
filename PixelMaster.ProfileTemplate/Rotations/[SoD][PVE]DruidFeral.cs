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
    public class DruidFeralRotation : IPMRotation
    {
        private DruidSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Druid;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 2; // 2 for Feral Spec
        public UnitClass PlayerClass => UnitClass.Druid;
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Era][PvE]Druid-Feral";
        public string Author => "PixelMaster";
        public string Description => "Feral Druid rotation for WoW Classic Era and Season of Discovery";
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
                // Ensure in Cat Form
                if (IsSpellReady("Cat Form") && !player.HasAura("Cat Form"))
                {
                    return CastAtPlayer("Cat Form");
                }
                // Use Prowl if not in combat
                if (IsSpellReady("Prowl") && !player.IsInCombat && !player.HasAura("Prowl"))
                {
                    return CastAtPlayer("Prowl");
                }
                // Open with Ravage if stealthed and behind target
                if (IsSpellReady("Ravage") && player.HasAura("Prowl") && IsBehindTarget(targetedEnemy))
                {
                    return CastAtTarget("Ravage", facing: SpellFacingFlags.BehindAndFaceTarget);
                }
                // Use Rake if not stealthed
                if (IsSpellReady("Rake") && !player.HasAura("Prowl"))
                {
                    return CastAtTarget("Rake");
                }
                // Use Mangle if available
                if (IsSpellReady("Mangle") && !player.HasAura("Prowl"))
                {
                    return CastAtTarget("Mangle");
                }
            }
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var comboPoints = player.SecondaryPower;
            var targetedEnemy = om.AnyEnemy;
            var inCombatEnemies = om.InCombatEnemies.ToList();

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
            // Check if need to switch to Bear Form
            if (ShouldUseBearForm(player, targetedEnemy, inCombatEnemies))
            {
                if (IsSpellReady("Dire Bear Form") && player.Form != ShapeshiftForm.DireBearForm)
                    return CastWithoutTargeting("Dire Bear Form", isHarmfulSpell: true);
                if (IsSpellReady("Bear Form") && player.Form != ShapeshiftForm.BearForm && player.Form != ShapeshiftForm.DireBearForm)
                    return CastWithoutTargeting("Bear Form", isHarmfulSpell: true);
            }
            else
            {
                // Ensure in Cat Form
                if (IsSpellReady("Cat Form") && player.Form != ShapeshiftForm.Cat)
                {
                    return CastAtPlayer("Cat Form");
                }
            }

            // Defensive Cooldowns
            if (player.HealthPercent <= settings.SurvivalInstinctsHealth && IsSpellReady("Survival Instincts"))
            {
                return CastAtPlayer("Survival Instincts");
            }
            if (player.HealthPercent <= settings.FeralBarkskin && IsSpellReady("Barkskin"))
            {
                return CastAtPlayer("Barkskin");
            }

            if (targetedEnemy != null)
            {
                // Bear Form Rotation
                if (player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm)
                {
                    if (IsSpellReady("Bash") && targetedEnemy.IsInPlayerMeleeRange && targetedEnemy.IsCasting)
                        return CastAtTarget("Bash");
                    // Use Mangle (Bear)
                    if (IsSpellReady("Mangle", "Bear Form"))
                    {
                        return CastAtTarget("Mangle", "Bear Form");
                    }
                    // Use Maul
                    if (IsSpellReady("Maul"))
                    {
                        return CastAtTarget("Maul");
                    }
                    // Use Swipe if multiple enemies
                    if (inCombatEnemies.Count > 1 && IsSpellReady("Swipe", "Bear Form"))
                    {
                        return CastAtTarget("Swipe", "Bear Form");
                    }
                }
                else if (player.Form == ShapeshiftForm.Cat) // Cat Form Rotation
                {
                    // Use Tiger's Fury
                    if (player.Energy <= settings.TigersFuryEnergy && IsSpellReady("Tiger's Fury"))
                    {
                        return CastAtPlayer("Tiger's Fury");
                    }
                    // Use Berserk
                    if (settings.UseBerserk && IsSpellReady("Berserk"))
                    {
                        return CastAtPlayer("Berserk");
                    }
                    // Apply Mangle (Cat)
                    if (!targetedEnemy.HasAura("Mangle") && IsSpellReady("Mangle", "Cat Form"))
                    {
                        return CastAtTarget("Mangle", "Cat Form");
                    }
                    // Apply Rake
                    if (!targetedEnemy.HasAura("Rake") && IsSpellReady("Rake"))
                    {
                        return CastAtTarget("Rake");
                    }
                    // Apply Rip at 5 combo points
                    if (comboPoints == 5 && IsSpellReady("Rip") && !targetedEnemy.HasAura("Rip"))
                    {
                        return CastAtTarget("Rip");
                    }
                    // Use Ferocious Bite when Rip is active
                    if (comboPoints == 5 && targetedEnemy.HasAura("Rip") && IsSpellReady("Ferocious Bite"))
                    {
                        return CastAtTarget("Ferocious Bite");
                    }
                    // Build Combo Points with Mangle or Shred
                    if (IsSpellReady("Shred") && IsBehindTarget(targetedEnemy))
                    {
                        return CastAtTarget("Shred");
                    }
                    else if (IsSpellReady("Mangle", "Cat Form"))
                    {
                        return CastAtTarget("Mangle", "Cat Form");
                    }
                    else if (IsSpellReady("Claw"))
                        return CastAtTarget("Claw");
                }
                else
                {
                    if (IsSpellReady("Moonfire"))
                        return CastAtTarget("Moonfire");
                    if (IsSpellReadyOrCasting("Wrath"))
                        return CastAtTarget("Wrath");
                }
                if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }

        private bool ShouldUseBearForm(ILocalPlayer player, WowUnit? targetedEnemy, List<WowUnit> inCombatEnemies)
        {
            // Switch to Bear Form if:
            // - More than one enemy and can endanger the player
            // - Player's health is low and enemy has considerable health
            bool catFormNotLearned = !PlayerLearnedSpell("Cat Form");
            bool multipleEnemies = inCombatEnemies.Count > 1;
            bool lowHealth = player.HealthPercent <= 40;
            bool enemyHighHealth = targetedEnemy != null && targetedEnemy.HealthPercent > 50;

            if (catFormNotLearned || (multipleEnemies && EnemyIsThreatening(player, inCombatEnemies)) || (lowHealth && enemyHighHealth))
            {
                return true;
            }
            return false;
        }

        private bool EnemyIsThreatening(ILocalPlayer player, List<WowUnit> enemies)
        {
            // Simple logic to determine if enemies are a threat
            double totalEnemyHealth = enemies.Sum(e => e.HealthPercent);
            return totalEnemyHealth > player.HealthPercent;
        }

        private bool IsBehindTarget(WowUnit target)
        {
            // Logic to determine if the player is behind the target
            return ObjectManager.Instance.Player.IsBehindTarget(target);
        }
    }
}
