using PixelMaster.Core.API;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;
using static PixelMaster.Core.API.PMRotationBuilder;
using PixelMaster.Core.Interfaces;
using System.Collections.Generic;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Cata;
using PixelMaster.Server.Shared;

namespace CombatClasses
{
    public class DeathKnightUnholy : IPMRotation
    {
        private DeathKnightSettings settings => ((CataCombatSettings)SettingsManager.Instance.Settings).DeathKnight;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Retail };
        public short Spec => 3; // Unholy
        public UnitClass PlayerClass => UnitClass.DeathKnight;
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Retail][PvE]DeathKnight-Unholy";
        public string Author => "YourName";
        public string Description => "Optimized Unholy Death Knight PvE Rotation";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;

            if (target != null)
            {
                if (IsSpellReady("Outbreak"))
                    return CastAtUnit(target, "Outbreak");

                if (IsSpellReady("Death Coil"))
                    return CastAtUnit(target, "Death Coil");
            }

            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.PlayerTarget;

            if (target == null || !target.IsAlive || !target.IsInCombat)
                return null;

            // Defensive cooldowns
            if (player.HealthPercent < settings.DeathStrikeEmergencyPercent && IsSpellReady("Death Strike"))
                return CastAtTarget("Death Strike");

            // Apply Virulent Plague if not present
            if (!target.HasAura("Virulent Plague") && IsSpellReady("Outbreak"))
                return CastAtTarget("Outbreak");

            // Use Unholy Blight to apply diseases and add stacks
            if (IsSpellReady("Unholy Blight"))
                return CastAtPlayer("Unholy Blight");

            // Use Dark Transformation on cooldown
            if (IsSpellReady("Dark Transformation"))
                return CastAtPlayer("Dark Transformation");

            // Use Summon Gargoyle if available
            if (IsSpellReady("Summon Gargoyle"))
                return CastAtPlayer("Summon Gargoyle");

            int enemyCount = GetEnemyCountAround((WowUnit)player, 8);
            // AoE Rotation
            if (enemyCount >= settings.DeathAndDecayCount)
            {
                // Use Death and Decay
                if (IsSpellReady("Death and Decay"))
                    return CastAtGroundUnderPlayer("Death and Decay");

                // Use Epidemic to spend Runic Power
                if (player.RunicPower >= 30 && IsSpellReady("Epidemic"))
                    return CastAtPlayer("Epidemic");

                // Use Scourge Strike inside Death and Decay
                if (player.HasAura("Death and Decay") && IsSpellReady("Scourge Strike"))
                    return CastAtTarget("Scourge Strike");

                // Maintain Festering Wounds
                if (IsSpellReady("Festering Strike"))
                    return CastAtTarget("Festering Strike");
            }
            else // Single Target Rotation
            {
                int festeringWounds = target.GetAuraStackCount("Festering Wound") ?? 0;

                // Use Apocalypse when Festering Wounds ≥ 4
                if (festeringWounds >= 4 && IsSpellReady("Apocalypse"))
                    return CastAtTarget("Apocalypse");

                // Use Soul Reaper when target health < 35%
                if (target.HealthPercent < 35 && IsSpellReady("Soul Reaper"))
                    return CastAtTarget("Soul Reaper");

                // Use Death Coil if Sudden Doom is active or Runic Power ≥ 80
                if ((player.HasAura("Sudden Doom") || player.RunicPower >= 80) && IsSpellReady("Death Coil"))
                    return CastAtTarget("Death Coil");

                // Maintain Festering Wounds
                if (festeringWounds < 4 && IsSpellReady("Festering Strike"))
                    return CastAtTarget("Festering Strike");

                // Use Scourge Strike to burst Festering Wounds
                if (festeringWounds > 0 && IsSpellReady("Scourge Strike"))
                    return CastAtTarget("Scourge Strike");

                // Use Death Coil as filler
                if (IsSpellReady("Death Coil"))
                    return CastAtTarget("Death Coil");
            }

            return CastAtTarget(sb.AutoAttack);
        }

        private int GetEnemyCountAround(WowUnit unit, float radius)
        {
            return ObjectManager.Instance.GetInCombatEnemiesWithinArea(unit.Position, radius)
                .Count;
        }
    }
}
