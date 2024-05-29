﻿using PixelMaster.Core.API;
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
    public class WarriorArms : IPMRotation
    {
        private WarriorSettings settings => SettingsManager.Instance.Warrior;
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Warrior;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]Warrior-Arms";
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
                if (player.AuraStacks("The Art of War") > 0 && IsSpellReady("Exorcism"))
                    return CastAtTarget("Exorcism");
                if (IsSpellReady("Judgement"))
                    return CastAtTarget("Judgement");
            }
            return CastAtTarget(sb.AutoAttack);
        }

        private static string[] _slows = { "Hamstring", "Piercing Howl", "Crippling Poison", "Hand of Freedom", "Infected Wounds" };
    public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = player.SpellBook;
            var inv = player.Inventory;
            var isEra = WowProcessManager.Instance.WowVersion == PixelMaster.Server.Shared.WowVersion.Classic_Era;

            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }


            if (player.IsFleeingFromTheFight)
            {

                return null;
            }
            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);

            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if ((targetedEnemy.IsPlayer || targetedEnemy.IsElite) && settings.UseWarriorThrowdown && IsSpellReady("Throwdown"))
                        return CastAtTarget("Throwdown");
                    if (targetedEnemy.IsPlayer && !settings.UseWarriorBasicRotation && IsSpellReady("Intimidating Shout") && targetedEnemy.DistanceSquaredToPlayer < 8 * 8)
                        return CastAtTarget("Intimidating Shout");
                }


                if (targetedEnemy.IsElite)
                {

                }
                // Dispel Bubbles
                if (!settings.UseWarriorBasicRotation && targetedEnemy.IsPlayer && IsSpellReadyOrCasting("Shattering Throw") && (targetedEnemy.Class == UnitClass.Mage && targetedEnemy.HasAura("Ice Block") || targetedEnemy.HasAura("Hand of Protection") || targetedEnemy.HasAura("Divine Shield")))
                    return CastAtTarget("Shattering Throw");
                //Rocket belt!
                if (targetedEnemy.IsPlayer && targetedEnemy.DistanceSquaredToPlayer > 20 * 20 && IsEquippedItemReady(EquipSlot.Waist))
                    return UseEquippedItem(EquipSlot.Waist);
                // Use Engineering Gloves
                if (IsEquippedItemReady(EquipSlot.Hands))
                    return UseEquippedItem(EquipSlot.Hands);
                //Stance Dancing
                //Pop over to Zerker
                if (player.Form != ShapeshiftForm.BerserkerStance && player.PowerPercent <= 75 && settings.UseWarriorStanceDance && targetedEnemy.IsBoss && IsSpellReady("Berserker Stance") && targetedEnemy.HasAura("Rend")  && !player.HasAura("Taste for Blood"))
                    return CastAtPlayerLocation("Berserker Stance", isHarmfulSpell: false);
                //Keep in Battle Stance
                if (player.Form != ShapeshiftForm.BattleStance && IsSpellReady("Battle Stance") && (!targetedEnemy.HasAura("Rend") || player.PowerPercent <= 75 && settings.UseWarriorKeepStance && ((player.HasAura("Overpower") || player.HasAura("Taste for Blood")) && GetSpellCooldown("Mortal Strike").TotalSeconds > 0)))
                    return CastAtPlayerLocation("Battle Stance", isHarmfulSpell: false);
                if (!isEra && !settings.UseWarriorBasicRotation && settings.UseWarriorCloser && targetedEnemy.DistanceSquaredToPlayer > 10 * 10 && targetedEnemy.DistanceSquaredToPlayer < 25 * 25 && IsSpellReady("Charge"))
                    return CastAtTarget("Charge");
                if (!isEra && !settings.UseWarriorBasicRotation && settings.UseWarriorCloser && targetedEnemy.DistanceSquaredToPlayer > 9 * 9 && targetedEnemy.DistanceSquaredToPlayer < 35 * 35 && IsSpellReady("Heroic Leap") && targetedEnemy.AuraStacks("Charge Stun") == 0)
                    return CastAtGround(targetedEnemy.Position, "Heroic Leap");
                //use it or lose it
                if (player.AuraStacks("Sudden Death") > 0 && IsSpellReady("Colossus Smash"))
                    return CastAtTarget("Colossus Smash");
                // ranged slow
                if (settings.UseWarriorSlows && !settings.UseWarriorBasicRotation &&
                    targetedEnemy.IsPlayer && targetedEnemy.DistanceSquaredToPlayer < 10 * 10 && IsSpellReady("Piercing Howl") && !_slows.Any(a => targetedEnemy.HasAura(a)))
                    return CastAtPlayerLocation("Piercing Howl");
                // Melee slow
                if (settings.UseWarriorSlows && !settings.UseWarriorBasicRotation &&
                    targetedEnemy.IsPlayer && IsSpellReady("Hamstring") && !_slows.Any(a => targetedEnemy.HasAura(a)))
                    return CastAtTarget("Hamstring");
                if(player.HealthPercent < 80 && IsSpellReady("Victory Rush"))
                    return CastAtTarget("Victory Rush");
                if(targetedEnemy.IsBoss && targetedEnemy.HealthPercent <= 25 && IsSpellReadyOrCasting("Shattering Throw"))
                    return CastAtTarget("Shattering Throw");
                //Execute under 20%
                if (targetedEnemy.HealthPercent < 20 && IsSpellReady("Execute"))
                    return CastAtTarget("Execute");
                //Default Rotatiom
                if (IsSpellReady("Rend"))
                    return CastAtTarget("Rend");
                if (IsSpellReady("Colossus Smash"))
                    return CastAtTarget("Colossus Smash");
                if (IsSpellReady("Mortal Strike"))
                    return CastAtTarget("Mortal Strike");
                if(settings.UseWarriorBladestorm && targetedEnemy.IsPlayer && targetedEnemy.DistanceSquaredToPlayer < 36)
                    return CastAtTarget("Bladestorm");
                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");
                if(player.PowerPercent > 40 && settings.UseWarriorSlamTalent && IsSpellReady("Slam"))
                    return CastAtTarget("Slam");

                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
        static bool CanUseRageDump()
        {
            // Pooling rage for upcoming CS. If its > 8s, make sure we have 60 rage. < 8s, only pop it at 85 rage.
            if (PlayerLearnedSpell("Colossus Smash"))
                return GetSpellCooldown("Colossus Smash").TotalSeconds > 8 ? ObjectManager.Instance.Player.PowerPercent > 60 : ObjectManager.Instance.Player.PowerPercent > 85;

            // We don't know CS. So just check if we have 60 rage to use cleave.
            return ObjectManager.Instance.Player.PowerPercent > 60;
        }
        static bool IsImpairingSpell(Spell spell)
        {
            if (spell.Categories != null)
            {
                if (spell.Categories.Any(c=>IsImpairingMechanic(c.Mechanic)))
                    return true;
            }
            if (spell.Effects != null)
            {
                return spell.Effects.Any(e => IsImpairingMechanic(e.EffectMechanic));
            }
            return false;

            static bool IsImpairingMechanic(SpellMechanic mechanic)
            {
                return mechanic switch
                {
                    SpellMechanic.Dazed => true,
                    SpellMechanic.Disoriented => true,
                    SpellMechanic.Frozen => true,
                    SpellMechanic.Incapacitated => true,
                    SpellMechanic.Rooted => true,
                    SpellMechanic.Slowed => true,
                    SpellMechanic.Snared => true,
                    _ => false,
                };
            }
        }
    }
}
