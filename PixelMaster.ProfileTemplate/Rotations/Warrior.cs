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
using System.Diagnostics;

namespace CombatClasses
{
    public class WarriorRotation : IPMRotation
    {
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Warrior;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "Warrior-Arms General PvE";
        public string Author => "PixelMaster";
        public string Description => "";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var inv = player.Inventory;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
         
            if (player.HealthPercent > 70 && IsSpellReady("Bloodrage") && !player.HasBuff("Bloodrage"))
                return CastAtPlayer("Bloodrage");
            if (targetedEnemy != null
                && targetedEnemy.NearbyEnemies.Count <= 1)
            {
                if (player.Power <= 50 && player.Form != ShapeshiftForm.BattleStance && IsSpellReady("Battle Stance"))
                    return CastAtPlayer("Battle Stance");
                if (IsSpellReady("Charge"))
                    return CastAtTarget("Charge");
            }
            else if (IsSpellReady("Shoot"))
                return CastAtTarget("Shoot");
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
            List<WowUnit>? inCombatEnemies = null;
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
                if (IsSpellReady("Defensive Stance") && player.Form != ShapeshiftForm.DefensiveStance)
                    return CastAtPlayer("Defensive Stance");
                if (targetedEnemy != null)
                {
                    if (targetedEnemy.DistanceSquaredToPlayer < 25 && IsSpellReady("Hamstring") && !targetedEnemy.HasDeBuff("Hamstring"))
                        return CastAtPlayer("Hamstring");
                }

                return null;
            }
            if (player.HealthPercent < 30)
            {
                if (IsSpellReady("Defensive Stance") && player.Form != ShapeshiftForm.DefensiveStance)
                {
                    return CastAtPlayer("Defensive Stance");
                }
            }
            else
            {
                if (IsSpellReady("Battle Stance") && player.Form != ShapeshiftForm.BattleStance)
                {
                    return CastAtPlayer("Battle Stance");
                }
            }
            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayer("Berserking");
                else if (player.Race == UnitRace.Orc && IsSpellReady("Blood Fury"))
                    return CastAtPlayer("Blood Fury");
                if (player.HealthPercent > 50)
                {
                    if (IsSpellReady("Recklessness"))
                        return CastAtPlayer("Recklessness");
                }
                if (IsSpellReady("Bladestorm"))
                    return CastAtPlayer("Bladestorm");
                
            }

            if (player.HealthPercent > 75 && IsSpellReady("Bloodrage") && !player.HasBuff("Bloodrage"))
                return CastAtPlayer("Bloodrage");
            if (IsSpellReady("Battle Shout") && !player.HasBuff("Battle Shout"))
                return CastAtPlayer("Battle Shout");

            //AoE handling
            inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 1)
                {
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp"))
                        CastAtPlayerLocation("War Stomp");
                    if (IsSpellReady("Demoralizing Shout") && nearbyEnemies.Any(e => !e.HasDeBuff("Demoralizing Shout")))
                        return CastAtPlayerLocation("Demoralizing Shout");
                    if (IsSpellReady("Thunder Clap") && nearbyEnemies.Any(e => !e.HasDeBuff("Thunder Clap")))
                        return CastAtPlayerLocation("Thunder Clap");
                    if ((player.Form == ShapeshiftForm.BattleStance || player.Form == ShapeshiftForm.BerserkerStance) && IsSpellReady("Sweeping Strikes"))
                        CastAtPlayerLocation("Sweeping Strikes", isHarmfulSpell:false);
                    if (IsSpellReady("Bladestorm"))
                        return CastAtPlayer("Bladestorm");
                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayer("Berserking");
                    else if (player.Race == UnitRace.Orc &&  IsSpellReady("Blood Fury"))
                        return CastAtPlayer("Blood Fury");
                    if ((player.HealthPercent <= 40 || nearbyEnemies.Count > 2) && IsSpellReady("Retaliation"))
                        return CastAtPlayer("Retaliation");
                    if (player.HealthPercent > 50)
                    {
                        if (IsSpellReady("Recklessness"))
                            return CastAtPlayer("Recklessness");
                    }
                }
                if (IsSpellReady("Cleave") && targetedEnemy != null && targetedEnemy.GetNearbyEnemies(4).Count > 0)
                    return CastAtTarget("Cleave");
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if(targetedEnemy.DistanceSquaredToPlayer < 25 * 25 && targetedEnemy.IsInSpellRange("Charge"))
                {
                    if (IsSpellReady("Charge"))
                        return CastAtTarget("Charge");
                }
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Pummel") && targetedEnemy.DistanceSquaredToPlayer < 64)
                        return CastAtTarget("Pummel");
                    if (player.Form == ShapeshiftForm.DefensiveStance && IsSpellReady("Shield Bash") && targetedEnemy.DistanceSquaredToPlayer < 64)
                        return CastAtTarget("Shield Bash");
                }
                if (!targetedEnemy.IsInMeleeRange && targetedEnemy.IsMoving && targetedEnemy.HealthPercent < 15)
                {
                    if (IsSpellReady("Shoot") && IsPlayerTargetInSpellRange("Shoot"))
                        return CastAtTarget("Shoot");
                }
                if((targetedEnemy.HealthPercent < 30 || targetedEnemy.IsMovingAwayFromPlayer) && targetedEnemy.DistanceSquaredToPlayer < 25 && IsSpellReady("Hamstring") && !targetedEnemy.HasDeBuff("Hamstring"))
                    return CastAtPlayer("Hamstring");
                if (targetedEnemy.HealthPercent <= 20)
                {
                    if (IsSpellReady("Execute"))
                        return CastAtTarget("Execute");
                    if (IsSpellReady("Overpower"))
                        return CastAtTarget("Overpower");
                }
                if (IsSpellReady("Raging Blow"))
                    return CastAtTarget("Raging Blow");
                if (IsSpellReady("Victory Rush"))
                    return CastAtTarget("Victory Rush");
                if ((targetedEnemy.HealthPercent > 30 || targetedEnemy.IsPlayer || targetedEnemy.IsElite) && IsSpellReady("Rend") && !targetedEnemy.HasDeBuff("Rend"))
                    return CastAtTarget("Rend");
                if (player.HasBuff("Sudden Death") && IsSpellReady("Execute"))
                    return CastAtTarget("Execute");
                if ((targetedEnemy.IsPlayer || targetedEnemy.IsElite || targetedEnemy.MaxHealth > player.MaxHealth) && IsSpellReady("Sunder Armor") && (targetedEnemy.AuraStacks("Sunder Armor") < 5 || targetedEnemy.AuraRemainingTime("Sunder Armor", true) < TimeSpan.FromSeconds(5)))
                    return CastAtTarget("Sunder Armor");
                if (IsSpellReady("Overpower"))
                    return CastAtTarget("Overpower");
                if (IsSpellReady("Mortal Strike"))
                    return CastAtTarget("Mortal Strike");
                if (IsSpellReady("Quick Strike"))
                    return CastAtTarget("Quick Strike");
                if (!player.IsMoving && player.Power >= 30 && IsSpellReadyOrCasting("Slam"))
                    return CastAtTarget("Slam");
                if (player.Level <= 40 && (player.Form == ShapeshiftForm.BattleStance || player.Form == ShapeshiftForm.DefensiveStance) && IsSpellReady("Thunder Clap"))
                    return CastAtPlayerLocation("Thunder Clap");
                if((player.Level <= 5 || player.Power >= 50) && IsSpellReady("Heroic Strike"))
                    return CastAtTarget("Heroic Strike");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
