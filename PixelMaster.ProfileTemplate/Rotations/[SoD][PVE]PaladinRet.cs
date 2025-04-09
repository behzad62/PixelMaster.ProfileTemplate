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
using System.Linq;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;

namespace CombatClasses
{
    public class SoDPVEPaladinRetributionRotation : IPMRotation
    {
        private PaladinSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Paladin;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 3; // 3 for Retribution
        public UnitClass PlayerClass => UnitClass.Paladin;
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[SoD][PvE]Paladin-Retribution";
        public string Author => "PixelMaster";
        public string Description => "Retribution Paladin rotation for WoW Season of Discovery and Classic Era";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                // 1. Use Exorcism as opener
                if (IsSpellReady("Exorcism"))
                    return CastAtTarget("Exorcism");

                // 2. Use Judgement as opener
                if (IsSpellReady("Judgement"))
                    return CastAtTarget("Judgement");

                // 3. Use Crusader Strike if Judgement is not available
                if (IsSpellReady("Crusader Strike"))
                    return CastAtTarget("Crusader Strike");
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

            // 1. Self-Healing and Defensive Cooldowns
            if (player.HealthPercent < 15 && IsSpellReady("Lay on Hands"))
                return CastAtTarget("Lay on Hands");

            if (player.HealthPercent < 20 && IsSpellReady("Divine Shield"))
                return CastAtTarget("Divine Shield");

            if (player.HealthPercent < 25 && IsSpellReadyOrCasting("Flash of Light"))
                return CastAtTarget("Flash of Light");

            if (player.HealthPercent < settings.HPThreshold && IsSpellReadyOrCasting("Divine Light") && !IsSpellCasting("Flash of Light"))
                return CastAtTarget("Divine Light");

            if (player.HealthPercent < settings.HPThreshold && IsSpellReadyOrCasting("Holy Light") && !IsSpellCasting("Flash of Light") && !IsSpellCasting("Divine Light"))
                return CastAtTarget("Holy Light");

            // 2. Remove Debuffs
            if (player.HasDebuffTypes(SpellDispelType.Magic, SpellDispelType.Disease, SpellDispelType.Poison) && IsSpellReady("Cleanse"))
                return CastAtTarget("Cleanse");

            // 3. Maintain Auras
            // if (IsSpellReady("Seal of Vengeance") && !player.HasBuff("Seal of Vengeance"))
            //     return CastAtPlayer("Seal of Vengeance");

            // 4. Offensive Cooldowns
            if (settings.UseAvengingWrath && IsSpellReady("Avenging Wrath"))
                return CastAtTarget("Avenging Wrath");

            // 5. AoE Handling
            if (inCombatEnemies.Count >= settings.AoECounter && IsSpellReady("Divine Storm"))
                return CastAtTarget("Divine Storm");
            if (inCombatEnemies.Count >= settings.AoECounter && IsSpellReady("Consecration"))
                return CastAtTarget("Consecration");
            if (inCombatEnemies.Count >= settings.AoECounter && IsSpellReady("Hammer of Justice"))
                return CastAtTarget("Hammer of Justice");


            // 6. Main Rotation
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Rebuke") && targetedEnemy.DistanceSquaredToPlayer < 10 * 10)
                        return CastAtTarget("Rebuke");
                    if (IsSpellReady("Hammer of Justice") && targetedEnemy.DistanceSquaredToPlayer < 15 * 15)
                        return CastAtTarget("Hammer of Justice");
                }

                // 7. Execute Phase
                if (targetedEnemy.HealthPercent <= settings.ExecuteThreshold && IsSpellReadyOrCasting("Hammer of Wrath"))
                    return CastAtTarget("Hammer of Wrath");

                if (IsSpellReady("Seal of Martyrdom") && !player.HasBuff("Seal of Martyrdom") && !player.HasBuff("Seal of Righteousness"))
                    return CastAtTarget("Seal of Martyrdom");
                if (IsSpellReady("Seal of Righteousness") && !player.HasBuff("Seal of Righteousness") && !player.HasBuff("Seal of Martyrdom"))
                    return CastAtTarget("Seal of Righteousness");

                if (IsSpellReady("Crusader Strike"))
                    return CastAtTarget("Crusader Strike");

                if (IsSpellReady("Exorcism"))
                    return CastAtTarget("Exorcism");

                if (IsSpellReady("Judgement"))
                    return CastAtTarget("Judgement");
                if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }


            return null;
        }
    }
}
