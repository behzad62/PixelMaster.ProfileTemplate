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

namespace CombatClasses
{
    public class ShadowPriest : IPMRotation
    {
        public short Spec => 1;  // Shadow specialization
        public UnitClass PlayerClass => UnitClass.Priest;
        public CombatRole Role => CombatRole.MeleeDPS;  // Shadow Priests are Melee DPS
        public string Name => "Shadow Priest";
        public string Author => "Fractius";
        public string Description => "Rotation for Shadow Priest in WoW Classic Season of Discovery";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            if (IsSpellReady("Homunculi"))
                return CastAtTarget("Homunculi");
	    if (IsSpellReady("Void Plague"))
                return CastAtTarget("Void Plague");
             if (IsSpellReady("Penance"))
                return CastAtTarget("Penance");
	    if (IsSpellReady("Shoot"))
                return CastAtTarget("Shoot", facing: SpellFacingFlags.FaceTarget);
            return null;
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = player.SpellBook;
            var inv = player.Inventory;
            List<WowUnit>? inCombatEnemies = null;

            // Health & Mana Management
            if (player.HealthPercent < 55)
            {
                if (IsSpellReady("Renew"))
                    return CastAtPlayer("Renew");
            }
            {
                if (IsSpellReady("Desperate Prayer"))
                    return CastAtPlayer("Desperate Prayer");
            }
            if (player.HealthPercent < 10)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }
            if (player.PowerPercent < 15)
            {
                var manaPot = inv.GetManaPotion();
                if (manaPot != null)
                    return UseItem(manaPot);
            }
            if (player.PowerPercent < 40)
            {
                if (IsSpellReady("Shadowfiend"))
                    return CastAtTarget("Shadowfiend");
            }

            // FleeFromFight
            if (player.IsFleeingFromTheFight)
            {
                inCombatEnemies = om.InCombatEnemies;
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 0)
                {
                    if (IsSpellReady("Psychic Scream"))
                        return CastAtPlayer("Psychic Scream");
                    if (IsSpellReady("Power Word: Shield") && !player.HasBuff("Weakened Soul"))
                        return CastAtPlayer("Power Word: Shield");
                    if (IsSpellReady("Fade"))
                        return CastAtPlayer("Fade");
                }
            }

            // Buff
            {
	        if (IsSpellReady("Power Word: Fortitude") && !player.HasBuff("Power Word: Fortitude"))
                return CastAtPlayer("Power Word: Fortitude");
                if (IsSpellReady("Inner Fire") && !player.HasBuff("Inner Fire"))
                return CastAtPlayer("Inner Fire");
	    }

            // Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (IsSpellReady("Power Infusion"))
                    return CastAtPlayer("Power Infusion");
                if (IsSpellReady("Shadowfiend"))
                    return CastAtTarget("Shadowfiend");
            }

            // AoE handling
            inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
                if (nearbyEnemies.Count > 1)
                {
                    if (IsSpellReady("Vampiric Embrace") && !player.HasBuff("Vampiric Embrace"))
                        return CastAtPlayer("Vampiric Embrace");
                    if (IsSpellReady("Mind Sear"))
                        return CastAtTarget("Mind Sear", facing: SpellFacingFlags.FaceTarget);
                }
            }

            // Single Target
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Silence") && !targetedEnemy.HasDeBuff("Silenced"))
                        return CastAtTarget("Silence");
                }
                if (IsSpellReady("Homunculi"))
                    return CastAtTarget("Homunculi");
	        if (IsSpellReady("Void Plague"))
                    return CastAtTarget("Void Plague");
                if (IsSpellReady("Penance"))
                    return CastAtTarget("Penance");
                if (IsSpellReady("Shoot"))
                    return CastAtTarget("Shoot");
                return null;
            }
            return null;
        }
    }
}