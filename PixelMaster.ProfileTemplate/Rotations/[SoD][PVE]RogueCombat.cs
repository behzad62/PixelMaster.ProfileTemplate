using System.Collections.Generic;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;
using PixelMaster.Core.API;
using PixelMaster.Core.Behaviors.Transport;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;
using PixelMaster.Server.Shared;
using static PixelMaster.Core.API.PMRotationBuilder;

namespace AdvancedCombatClasses.Rotations.Era
{
    public class RogueCombatRotation : IPMRotation
    {
        private RogueSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Rogue;

        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Rogue;
        public CombatRole Role => CombatRole.MeleeDPS;
        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public string Name => "Rogue-Combat Rotation (SoD/Era)";
        public string Author => "PixelMaster";
        public string Description => "Combat rotation for Rogue in SoD/Era.";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsFlying || (targetedEnemy.Position.Z - player.Position.Z) > 5 && targetedEnemy.DistanceSquaredToPlayer < 25)
                {
                    if (IsSpellReadyOrCasting("Throw"))
                        return CastAtTarget("Throw");
                    if (IsSpellReadyOrCasting("Shoot"))
                        return CastAtTarget("Shoot");
                }

                if (player.IsStealthed && IsSpellReady("Sprint") && targetedEnemy.DistanceSquaredToPlayer > 45 * 45)
                    return CastWithoutTargeting("Sprint", isHarmfulSpell: false);

                if (targetedEnemy.Level >= player.Level - 4 && !player.IsStealthed && IsSpellReady("Stealth") && targetedEnemy.DistanceSquaredToPlayer < 30 * 30)
                    return CastWithoutTargeting("Stealth", isHarmfulSpell: false);
                if (player.IsStealthed)
                {
                    if (targetedEnemy.IsMoving && (targetedEnemy.CreatureType == CreatureType.Beast || targetedEnemy.CreatureType == CreatureType.Humanoid || targetedEnemy.CreatureType == CreatureType.Demon || targetedEnemy.CreatureType == CreatureType.Dragonkin)
                        && targetedEnemy.DistanceSquaredToPlayer < 10 * 10 && IsSpellReady("Sap"))
                        return CastAtTarget("Sap");
                    if (targetedEnemy.IsMoving
                            && targetedEnemy.DistanceSquaredToPlayer < 20 * 20 && IsSpellReady("Distract"))
                        return CastAtGround(NavigationHelpers.GetTargetPoint(player.Position, targetedEnemy.DistanceToPlayer + 5, NavigationHelpers.GetTargetVectorYaw(player.Position, targetedEnemy.Position), false), "Distract");
                }

                if (player.IsStealthed && IsSpellReady("Ambush") && player.IsBehindTarget(targetedEnemy))
                    return CastAtTarget("Ambush", facing: SpellFacingFlags.BehindAndFaceTarget);

                if (player.IsStealthed && IsSpellReady("Cheap Shot"))
                    return CastAtTarget("Cheap Shot");

                // Use Throw if target is at range
                if (IsSpellReadyOrCasting("Throw") && targetedEnemy.DistanceToPlayer > 8)
                    return CastAtTarget("Throw");
            }

            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            var enemies = om.InCombatEnemies.ToList();
            var comboPoints = player.ComboPoints;

            if (player.IsDead || targetedEnemy == null)
                return null;

            // Interrupt spells
            if (settings.InterruptSpells && targetedEnemy.IsCasting && IsSpellReady("Kick"))
                return CastAtTarget("Kick");

            // Use Evasion if health is low
            if (player.HealthPercent <= settings.EvasionHealthPercent && IsSpellReady("Evasion"))
                return CastWithoutTargeting("Evasion");

            // Use Cloak of Shadows if needed
            if (player.HasDebuffType(SpellDispelType.Magic) && IsSpellReady("Cloak of Shadows"))
                return CastWithoutTargeting("Cloak of Shadows");

            // Use Adrenaline Rush on cooldown
            if (IsSpellReady("Adrenaline Rush"))
                return CastWithoutTargeting("Adrenaline Rush");

            // Use Blade Flurry for AoE
            if (enemies.Count >= settings.AoEEnemyCount && IsSpellReady("Blade Flurry"))
                return CastAtPlayer("Blade Flurry");

            // Use Fan of Knives for AoE
            if (enemies.Count >= settings.AoEEnemyCount && IsSpellReady("Fan of Knives"))
                return CastAtTarget("Fan of Knives");

            // Use Killing Spree for burst damage
            if (IsSpellReady("Killing Spree"))
                return CastAtTarget("Killing Spree");

            // Use Slice and Dice if not active or about to expire
            if (!player.HasBuff("Slice and Dice") || player.AuraRemainingTime("Slice and Dice").TotalSeconds < 2)
            {
                if (comboPoints > 0 && IsSpellReady("Slice and Dice"))
                    return CastWithoutTargeting("Slice and Dice");
            }

            // Use Rupture if enabled and target doesn't have it
            if (settings.UseRupture && comboPoints >= 2 && !targetedEnemy.HasDebuff("Rupture") && IsSpellReady("Rupture"))
                return CastAtTarget("Rupture");

            // Use Eviscerate as finisher
            if (comboPoints >= 5 && IsSpellReady("Eviscerate"))
                return CastAtTarget("Eviscerate");

            // Build combo points with Sinister Strike
            if (IsSpellReady("Sinister Strike"))
                return CastAtTarget("Sinister Strike");

            if (!targetedEnemy.IsPlayerAttacking)
                return CastAtTarget(sb.AutoAttack);
            return null;
        }
    }
}
