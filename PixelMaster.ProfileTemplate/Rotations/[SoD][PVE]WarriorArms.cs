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
using System.Linq;
using System;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;

namespace CombatClasses
{
    public class SoDPVEWarriorArmsRotation : IPMRotation
    {
        private WarriorSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Warrior;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 1; // Arms specialization
        public UnitClass PlayerClass => UnitClass.Warrior;
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[SoD][PvE]Warrior-Arms";
        public string Author => "PixelMaster";
        public string Description => "Arms Warrior rotation for WoW Season of Discovery";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                // Apply Battle Shout if not active
                if (settings.UseWarriorShouts && IsSpellReady("Battle Shout") && !player.HasBuff("Battle Shout"))
                    return CastWithoutTargeting("Battle Shout");

                if (player.Form != ShapeshiftForm.BattleStance)
                    return CastWithoutTargeting("Battle Stance");

                if (targetedEnemy.IsFlying)
                {
                    if (IsSpellReadyOrCasting("Throw"))
                        return CastAtTarget("Throw");
                    if (IsSpellReadyOrCasting("Shoot"))
                        return CastAtTarget("Shoot");
                }

                // Charge to engage
                if (settings.UseWarriorCloser && IsSpellReady("Charge") && targetedEnemy.DistanceSquaredToPlayer >= 8 * 8 && targetedEnemy.DistanceSquaredToPlayer <= 25 * 25 && targetedEnemy.LinkedEnemies.Count < 1)
                    return CastAtTarget("Charge");
            }

            // Default to Auto Attack
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            var inCombatEnemies = om.InCombatEnemies.ToList();

            // Self-healing and defensive cooldowns
            if (player.HealthPercent < settings.WarriorEnragedRegenerationHealth && IsSpellReady("Enraged Regeneration"))
                return CastWithoutTargeting("Enraged Regeneration");

            if (player.HealthPercent < settings.WarriorProtShieldWallHealth && IsSpellReady("Shield Wall"))
                return CastWithoutTargeting("Shield Wall");

            // Maintain Battle Shout
            if (settings.UseWarriorShouts && IsSpellReady("Battle Shout") && !player.HasBuff("Battle Shout"))
                return CastWithoutTargeting("Battle Shout");

            // AoE Rotation
            if (settings.UseWarriorAOE && inCombatEnemies.Count >= 3)
            {
                // 1. Whirlwind
                if (IsSpellReady("Whirlwind"))
                    return CastAtTarget("Whirlwind");

                // 2. Execute
                if (targetedEnemy != null && targetedEnemy.HealthPercent <= 20 && IsSpellReady("Execute"))
                    return CastAtTarget("Execute");

                // 3. Cleave
                if (player.Rage > 50 && IsSpellReady("Cleave"))
                    return CastAtTarget("Cleave");

                // 4. Bloodthirst
                if (IsSpellReady("Bloodthirst"))
                    return CastAtTarget("Bloodthirst");

                // 5. Slam with Blood Surge proc
                if (player.HasAura("Blood Surge") && IsSpellReady("Slam"))
                    return CastAtTarget("Slam");

                // 6. Overpower
                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");

                // 7. Raging Blow
                if (IsSpellReady("Raging Blow"))
                    return CastAtTarget("Raging Blow");

                // 8. Hamstring
                if (settings.UseWarriorSlows && player.Rage > 50 && IsSpellReady("Hamstring"))
                    return CastAtTarget("Hamstring");
            }
            var closeEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
            if (IsSpellReady("Thunder Clap") && closeEnemies.Count(e=> !e.HasDebuff("Thunder Clap")) > 1)
                return CastWithoutTargeting("Thunder Clap");
            if ((closeEnemies.Count > 2 || closeEnemies.Count > 1 && player.HealthPercent < 40) && IsSpellReady("Retaliation"))
                return CastWithoutTargeting("Retaliation");
            if (closeEnemies.Count > 1 && IsSpellReady("Sweeping Strikes"))
                return CastWithoutTargeting("Sweeping Strikes");

            if (targetedEnemy != null) // Single Target Rotation
            {
                //if (targetedEnemy.CreatureType == CreatureType.Humanoid && player.Rage > 20 && targetedEnemy.HealthPercent > 30 && targetedEnemy.IsInMeleeRange && player.Form != ShapeshiftForm.DefensiveStance && PlayerLearnedSpell("Disarm") && GetSpellCooldown("Disarm") == TimeSpan.Zero)
                //    return CastWithoutTargeting("Defensive Stance");
                //if (targetedEnemy.CreatureType == CreatureType.Humanoid && targetedEnemy.HealthPercent > 30 && targetedEnemy.IsInMeleeRange && player.Form == ShapeshiftForm.DefensiveStance && IsSpellReady("Disarm"))
                //    return CastAtTarget("Disarm");

                if (player.Form != ShapeshiftForm.BattleStance && (player.Rage > 20 || GetSpellCooldown("Disarm") != TimeSpan.Zero))
                    return CastWithoutTargeting("Battle Stance");
                // 1. Execute
                if (targetedEnemy.HealthPercent <= 20 && IsSpellReady("Execute"))
                    return CastAtTarget("Execute");

                // 2. Slam with Blood Surge proc
                if (player.HasAura("Blood Surge") && IsSpellReady("Slam"))
                    return CastAtTarget("Slam");

                // 3. Bloodthirst
                if (IsSpellReady("Bloodthirst"))
                    return CastAtTarget("Bloodthirst");

                if (IsSpellReady("Victory Rush"))
                    return CastAtTarget("Victory Rush");

                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");

                if (!targetedEnemy.HasDebuff("Rend") && IsSpellReady("Rend"))
                    return CastAtTarget("Rend");

                if (player.HealthPercent < 50 && targetedEnemy.IsInPlayerMeleeRange && IsSpellReady("Thunder Clap"))
                    return CastAtTarget("Thunder Clap");

                // 4. Heroic Strike when high on rage
                if (player.Rage > 50 && IsSpellReady("Heroic Strike"))
                    return CastAtTarget("Heroic Strike");

                // 5. Whirlwind
                if (IsSpellReady("Whirlwind"))
                    return CastAtTarget("Whirlwind");

                // 6. Overpower
                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");

                // 7. Raging Blow
                if (IsSpellReady("Raging Blow"))
                    return CastAtTarget("Raging Blow");

                // 8. Hamstring
                if (settings.UseWarriorSlows && (player.Rage > 50 || targetedEnemy.IsMoving) && IsSpellReady("Hamstring") && !targetedEnemy.HasDebuff("Hamstring"))
                    return CastAtTarget("Hamstring");
                if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }

            return null;
        }
    }
}
