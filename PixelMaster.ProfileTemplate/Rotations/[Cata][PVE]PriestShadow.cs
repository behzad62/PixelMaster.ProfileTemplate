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

namespace CombatClasses
{
    public class PriestShadow : IPMRotation
    {
        private PriestSettings settings => SettingsManager.Instance.Priest;
        public short Spec => 3;
        public UnitClass PlayerClass => UnitClass.Priest;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Priest-Shadow";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            if (targetedEnemy != null)
            {
                if (settings.UseShieldPrePull && (targetedEnemy.IsElite || targetedEnemy.DistanceSquaredToPlayer < 15 * 15) && !player.HasAura("Weakened Soul") && !PlayerLearnedSpell("Mind Spike") && IsSpellReady("Power Word: Shield"))
                    return CastAtPlayer("Power Word: Shield");
                if (settings.DevouringPlagueFirst && (targetedEnemy.IsElite || !PlayerLearnedSpell("Mind Spike")) && IsSpellReadyOrCasting("Devouring Plague"))
                    return CastAtTarget("Devouring Plague");
                if ((targetedEnemy.IsElite || !PlayerLearnedSpell("Mind Spike")) && IsSpellReadyOrCasting("Vampiric Touch"))
                    return CastAtTarget("Vampiric Touch");
                if (IsSpellReadyOrCasting("Mind Blast"))
                    return CastAtTarget("Mind Blast");
                if (IsSpellReadyOrCasting("Smite"))
                    return CastAtTarget("Smite");
            }
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = player.SpellBook;
            var inv = player.Inventory;
            var comboPoints = player.SecondaryPower;
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;

            if ((player.HealthPercent < settings.ShieldHealthPercent || !PlayerLearnedSpell("Mind Spike")) && !player.HasAura("Weakened Soul") && IsSpellReady("Power Word: Shield"))
                return CastAtPlayer("Power Word: Shield");
            if (player.PowerPercent < settings.DispersionMana && IsSpellReady("Dispersion"))
                return CastAtPlayer("Dispersion");

            if (settings.UsePsychicScream && GetUnitsWithinArea(inCombatEnemies, player.Position, 10).Count >= settings.PsychicScreamAddCount && IsSpellReady("Psychic Scream"))
                return CastAtPlayerLocation("Psychic Scream");
            if (player.HealthPercent < settings.ShadowFlashHealHealth && IsSpellReadyOrCasting("Flash Heal"))
                return CastAtPlayer("Flash Heal");
            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }
            if (!player.HasAura("Shadowform") && IsSpellReady("Shadowform"))
                return CastAtPlayerLocation("Shadowform", isHarmfulSpell: false);
            if (targetedEnemy != null && (targetedEnemy.HealthPercent >= 60 || targetedEnemy.IsElite) && player.PowerPercent <= settings.ShadowfiendMana && IsSpellReady("Shadowfiend"))
                return CastAtTarget("Shadowfiend");
            if (player.IsFleeingFromTheFight)
            {
                if (settings.UsePsychicScream && GetUnitsWithinArea(inCombatEnemies, player.Position, 10).Count >= settings.PsychicScreamAddCount && IsSpellReady("Psychic Scream"))
                    return CastAtPlayerLocation("Psychic Scream");
                if (player.HealthPercent <= 25 && IsSpellReady("Dispersion"))
                    return CastAtPlayer("Dispersion");
                return null;
            }
            //if (player.HealthPercent < 30)
            //{

            //}

            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            if (inCombatEnemies.Count > 1)
            {
                if ((inCombatEnemies.Count(u => u.IsTargetingPlayer || u.IsTargetingPlayerPet) >= 2 || (targetedEnemy?.IsElite ?? false)) && IsSpellReady("Archangel"))
                    return CastAtTarget("Archangel");

                //if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                //{

                //}
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsElite || !PlayerLearnedSpell("Mind Spike"))
                {
                    if (targetedEnemy.HealthPercent <= 25 && IsSpellReady("Shadow Word: Death"))
                        return CastAtTarget("Shadow Word: Death");
                    if ((targetedEnemy.HealthPercent > 40 || targetedEnemy.IsElite) && IsSpellReady("Shadow Word: Pain") && !targetedEnemy.HasAura("Shadow Word: Pain", true))
                        return CastAtTarget("Shadow Word: Pain");
                    if (player.HasAura("Shadow Orb") && IsSpellReadyOrCasting("Mind Blast"))
                        return CastAtTarget("Mind Blast");
                    if ((targetedEnemy.HealthPercent > 40 || targetedEnemy.IsElite) && IsSpellReadyOrCasting("Vampiric Touch") && !targetedEnemy.HasAura("Vampiric Touch", true))
                        return CastAtTarget("Vampiric Touch");
                    if ((targetedEnemy.HealthPercent > 40 || targetedEnemy.IsElite) && IsSpellReadyOrCasting("Devouring Plague") && !targetedEnemy.HasAura("Devouring Plague", true))
                        return CastAtTarget("Devouring Plague");
                    if (IsSpellReadyOrCasting("Mind Blast"))
                        return CastAtTarget("Mind Blast");
                    if (player.PowerPercent >= settings.MindFlayMana && IsSpellReadyOrCasting("Mind Flay"))
                        return CastAtTarget("Mind Flay");
                }
                if (targetedEnemy.HealthPercent <= 25 && IsSpellReady("Shadow Word: Death"))
                    return CastAtTarget("Shadow Word: Death");
                if (player.AuraStacks("Mind Melt", true) >= 2 && IsSpellReadyOrCasting("Mind Blast"))
                    return CastAtTarget("Mind Blast");
                if (IsSpellReadyOrCasting("Mind Spike"))
                    return CastAtTarget("Mind Spike");
                if (player.Level < 20 && IsSpellReadyOrCasting("Smite"))
                    return CastAtTarget("Smite");
                var wand = inv.GetEquippedItemsBySlot(EquipSlot.Ranged);
                if (wand != null && IsSpellReady("Shoot"))
                    return CastAtTarget("Shoot");
            }
            return null;
        }
    }
}
