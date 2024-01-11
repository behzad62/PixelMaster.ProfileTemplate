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
    public class FrostMage : IPMRotation
    {
        public short Spec => 3;
        public UnitClass PlayerClass => UnitClass.Mage;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "Mage-Frost General PvE";
        public string Author => "PixelMaster";
        public string Description => "";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var linkedEnemies = PullingTarget?.LinkedEnemies;
            if (linkedEnemies != null && linkedEnemies.Count > 2)
            {
                if (IsSpellReadyOrCasting("Blizzard"))
                    return CastAtGround(GetBestAoELocation(linkedEnemies, 8f, out int numEnemiesInAoE), "Blizzard");
            }
            if (PlayerLearnedSpell("Frostbolt"))
                return CastAtTarget("Frostbolt");
            else if (PlayerLearnedSpell("Fireball"))
                return CastAtTarget("Fireball");
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
            List<WowUnit>? inCombatEnemies = null;
            if (player.HealthPercent < 15)
            {
                if (IsSpellReady("Ice Block"))
                    return CastAtPlayer("Ice Block");
            }
            if (player.HealthPercent < 30)
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
                inCombatEnemies = om.InCombatEnemies;
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 0)
                {
                    if (IsSpellReady("Frost Nova"))
                        return CastAtPlayerLocation("Frost Nova");
                    if (IsSpellReady("Cone of Cold") && GetUnitsInFrontOfPlayer(nearbyEnemies, 90, 8).Count > 0)
                        return CastAtPlayerLocation("Cone of Cold");
                    if (GetUnitsInFrontOfPlayer(nearbyEnemies, 60, 8).Count > 0)
                    {
                        if (IsSpellReady("Dragon's Breath"))
                            return CastAtPlayerLocation("Dragon's Breath");
                        if (targetedEnemy != null && IsSpellReady("Fire Blast"))
                            return CastAtTarget("Fire Blast");
                    }
                }
                if (IsSpellReady("Blink"))
                    return CastAtPlayer("Blink");
                return null;
            }
            if (player.PowerPercent < 20)
            {
                var manaPot = inv.GetManaPotion();
                if (manaPot != null)
                    return UseItem(manaPot);
            }
            if (player.Debuffs.Any(d => d.Spell?.DispelType == SpellDispelType.Curse))
            {
                if (IsSpellReady("Remove Curse"))
                    return CastAtPlayer("Remove Curse");
                else if (IsSpellReady("Remove Lesser Curse"))
                    return CastAtPlayer("Remove Lesser Curse");
            }
            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                if (IsSpellReady("Summon Water Elemental"))
                    return CastAtPlayerLocation("Summon Water Elemental", isHarmfulSpell: false);
                if (IsSpellReady("Icy Veins"))
                    return CastAtPlayer("Icy Veins");
                if (IsSpellReady("Mirror Image"))
                    return CastAtPlayer("Mirror Image");
            }
            //AoE handling
            inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 1)
                {
                    if (IsSpellReady("Frost Nova"))
                        return CastAtPlayerLocation("Frost Nova");
                    var polyCandidates = nearbyEnemies.Where(e => e.HealthPercent > 25 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root) && (e.CreatureType == CreatureType.Beast || e.CreatureType == CreatureType.Humanoid));
                    if (polyCandidates.Any() && IsSpellReadyOrCasting("Polymorph") && !inCombatEnemies.Any(e => e.HasDeBuff("Polymorph")))
                        return CastAtUnit(polyCandidates.First(), "Polymorph");
                    if (IsSpellReady("Cone of Cold") && GetUnitsInFrontOfPlayer(nearbyEnemies, 90, 10).Count > 1)
                        return CastAtPlayerLocation("Cone of Cold");

                }
                if (IsSpellCasting("Blizzard") && GetUnitsWithinArea(inCombatEnemies, LastGroundSpellLocation, 8).Count > 1)
                    return CastAtGround(LastGroundSpellLocation, "Blizzard");
                else if (IsSpellReady("Blizzard"))
                {
                    var AoELocation = GetBestAoELocation(inCombatEnemies, 8f, out int numEnemiesInAoE);
                    if (numEnemiesInAoE > 2)
                        return CastAtGround(AoELocation, "Blizzard");
                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                    if (IsSpellReady("Summon Water Elemental"))
                        return CastAtPlayerLocation("Summon Water Elemental", isHarmfulSpell: false);
                    if (IsSpellReady("Icy Veins"))
                        return CastAtPlayer("Icy Veins");
                    if (IsSpellReady("Mirror Image"))
                        return CastAtPlayer("Mirror Image");
                }
            }
            if (player.HealthPercent < 50)
            {
                if (IsSpellReady("Ice Barrier") && !player.HasBuff("Ice Barrier"))
                    return CastAtPlayer("Ice Barrier");
                if (IsSpellReady("Mana Shield") && !player.HasBuff("Mana Shield"))
                    return CastAtPlayer("Mana Shield");
            }
            if (IsSpellCasting("Evocation"))
                return CastAtPlayer("Evocation");
            else if (IsSpellReady("Evocation") && (player.PowerPercent < 5 || (player.PowerPercent < 15 && GetUnitsWithinArea(inCombatEnemies, player.Position, 8).Count == 0)))
                return CastAtPlayer("Evocation");

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.HasDeBuff("Polymorph") && (inCombatEnemies.Count > 1 || player.PowerPercent < 20))
                    return null;
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Counterspell") && IsPlayerTargetInSpellRange("Counterspell"))
                        return CastAtTarget("Counterspell");
                }

                {
                    if (player.IsMoving && IsSpellReady("Fire Blast"))
                        return CastAtTarget("Fire Blast");
                }
                if (targetedEnemy.HasDeBuff("Frostbite") || targetedEnemy.HasDeBuff("Frost Nova") || player.HasBuff("Fingers of Frost"))
                {
                    if (IsSpellReady("Deep Freeze"))
                        return CastAtTarget("Deep Freeze");
                    if (IsSpellReady("Ice Lance"))
                        return CastAtTarget("Ice Lance");
                }
                if (player.HasBuff("Fireball!") && IsSpellReadyOrCasting("Frostfire Bolt"))
                    return CastAtTarget("Frostfire Bolt");
                if (targetedEnemy.IsInMeleeRange)
                {
                    var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 5);
                    if (player.HealthPercent < 30 || nearbyEnemies.Count > 1)
                    {
                        if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReady("War Stomp"))
                            return CastAtPlayerLocation("War Stomp");
                        if (IsSpellReady("Ice Barrier") && !player.HasBuff("Ice Barrier"))
                            return CastAtPlayer("Ice Barrier");
                        if (IsSpellReady("Frost Nova"))
                            return CastAtPlayerLocation("Frost Nova");
                        if (IsSpellReady("Dragon's Breath"))
                            return CastAtDirection(targetedEnemy.Position, "Dragon's Breath");
                        if (IsSpellReady("Cone of Cold"))
                            return CastAtDirection(targetedEnemy.Position, "Cone of Cold");
                    }
                    else if(targetedEnemy.HealthPercent > 60)
                    {
                        if (IsSpellReady("Frost Nova"))
                            return CastAtPlayerLocation("Frost Nova");
                    }
                }
                if (IsSpellReady("Living Flame"))
                    return CastAtTarget("Living Flame");
                else if (IsSpellReadyOrCasting("Frostbolt") && !IsSpellCasting("Fireball"))
                    return CastAtTarget("Frostbolt");
                else if (IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");
                else if (IsSpellReady("Shoot"))
                    return CastAtTarget("Shoot");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
