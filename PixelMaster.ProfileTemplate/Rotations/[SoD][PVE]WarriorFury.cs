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
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;

namespace CombatClasses
{
    public class SoDPVEWarriorFuryRotation : IPMRotation
    {
        private WarriorSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Warrior;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 2; // Fury specialization
        public UnitClass PlayerClass => UnitClass.Warrior;
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[SoD][PvE]Warrior-Fury";
        public string Author => "PixelMaster";
        public string Description => "Fury Warrior rotation for WoW Season of Discovery";

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
                    return CastAtPlayer("Battle Shout");

                // Charge to engage
                if (settings.UseWarriorCloser && IsSpellReady("Charge") && targetedEnemy.DistanceSquaredToPlayer >= 8 * 8 && targetedEnemy.DistanceSquaredToPlayer <= 25 * 25)
                    return CastAtTarget("Charge");

                // Use Bloodrage to generate additional rage
                if (IsSpellReady("Bloodrage") && player.Rage < 50)
                    return CastAtPlayer("Bloodrage");
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
                return CastAtPlayer("Enraged Regeneration");

            if (player.HealthPercent < settings.WarriorProtShieldWallHealth && IsSpellReady("Shield Wall"))
                return CastAtPlayer("Shield Wall");

            // Maintain Battle Shout
            if (settings.UseWarriorShouts && IsSpellReady("Battle Shout") && !player.HasBuff("Battle Shout"))
                return CastAtPlayer("Battle Shout");

            // Use Bloodrage if low on rage
            if (IsSpellReady("Bloodrage") && player.Rage < 50)
                return CastAtPlayer("Bloodrage");

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

                // 5. Raging Blow
                if (IsSpellReady("Raging Blow"))
                    return CastAtTarget("Raging Blow");

                // 6. Slam with Bloodsurge proc
                if (player.HasAura("Bloodsurge") && IsSpellReady("Slam"))
                    return CastAtTarget("Slam");

                // 7. Overpower
                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");

                // 8. Hamstring for procs
                if (settings.UseWarriorSlows && player.Rage > 50 && IsSpellReady("Hamstring"))
                    return CastAtTarget("Hamstring");
            }
            var closeEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
            if (IsSpellReady("Thunder Clap") && closeEnemies.Count(e => !e.HasDebuff("Thunder Clap")) > 1)
                return CastWithoutTargeting("Thunder Clap");
            if (targetedEnemy != null) // Single Target Rotation
            {
                // 1. Execute
                if (targetedEnemy.HealthPercent <= 20 && IsSpellReady("Execute"))
                    return CastAtTarget("Execute");

                // 2. Bloodthirst
                if (IsSpellReady("Bloodthirst"))
                    return CastAtTarget("Bloodthirst");

                // 3. Raging Blow
                if (IsSpellReady("Raging Blow"))
                    return CastAtTarget("Raging Blow");

                // 4. Slam with Bloodsurge proc
                if (player.HasAura("Bloodsurge") && IsSpellReady("Slam"))
                    return CastAtTarget("Slam");

                if (IsSpellReady("Victory Rush"))
                    return CastAtTarget("Victory Rush");

                if (!targetedEnemy.HasDebuff("Rend") && IsSpellReady("Rend"))
                    return CastAtTarget("Rend");

                // 5. Heroic Strike when high on rage
                if (player.Rage > 50 && IsSpellReady("Heroic Strike"))
                    return CastAtTarget("Heroic Strike");

                // 6. Whirlwind
                if (IsSpellReady("Whirlwind"))
                    return CastAtTarget("Whirlwind");

                // 7. Overpower
                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");

                // 8. Hamstring for procs
                if (settings.UseWarriorSlows && (player.Rage > 50 || targetedEnemy.IsMoving) && IsSpellReady("Hamstring") && !targetedEnemy.HasDebuff("Hamstring"))
                    return CastAtTarget("Hamstring");
                if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }

            return null;
        }
    }
}
