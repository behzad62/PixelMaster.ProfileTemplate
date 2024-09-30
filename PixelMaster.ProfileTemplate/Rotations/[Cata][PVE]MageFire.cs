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
    public class MageFire : IPMRotation
    {
        private MageSettings settings => SettingsManager.Instance.Mage;
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Mage;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Mage-Fire";
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
                if (IsSpellReadyOrCasting("Pyroblast"))
                    return CastAtTarget("Pyroblast");
                if (IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");
            }
            return CastAtTarget(sb.AutoAttack);
        }
        private WowUnit? polyTarget;
        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = player.SpellBook;
            var inv = player.Inventory;
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;

            // Defensive stuff
            if (player.HasAura("Ice Block"))
                return null;
            if(!player.HasAura("Molten Armor") && IsSpellReady("Molten Armor"))
                return CastAtPlayerLocation("Molten Armor", isHarmfulSpell: false);
            if (player.HealthPercent < 20 && !player.HasAura("Hypothermia") && IsSpellReady("Ice Block"))
                return CastAtPlayerLocation("Ice Block", isHarmfulSpell: false);

            // Cooldowns
            if ((player.PowerPercent < 30 || IsSpellCasting("Evocation") || player.HealthPercent < 50 && player.HasAura("Glyph of Evocation")) && IsSpellReadyOrCasting("Evocation"))
                return CastAtPlayerLocation("Evocation", isHarmfulSpell: false);
            if (player.HealthPercent <= 80 && !player.HasAura("Mage Ward") && IsSpellReady("Mage Ward"))
                return CastAtPlayerLocation("Mage Ward", isHarmfulSpell: false);
            if (player.HealthPercent <= 60 && !player.HasAura("Mana Shield") && IsSpellReady("Mana Shield"))
                return CastAtPlayerLocation("Mana Shield", isHarmfulSpell: false);

            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                if (!om.CurrentMap.IsDungeon)
                {
                    var healingPot = inv.GetHealingPotion();
                    if (healingPot != null)
                        return UseItem(healingPot);
                }
            }
            if (player.PowerPercent < 80)
            {
                var manaGem = inv.GetBagsItemsByID(36799).FirstOrDefault();
                if (manaGem != null)
                    return UseItem(manaGem);
            }
            if (player.PowerPercent < 20 && !om.CurrentMap.IsDungeon)
            {
                var manaPotion = inv.GetManaPotion();
                if (manaPotion != null)
                    return UseItem(manaPotion);
            }

            if (player.IsFleeingFromTheFight)
            {
                if (GetUnitsWithinArea(inCombatEnemies, player.Position, 8).Count >= 1 && IsSpellReady("Frost Nova"))
                    return CastAtPlayerLocation("Frost Nova");
                if (IsSpellReady("Blink"))
                    return CastAtPlayerLocation("Blink", isHarmfulSpell: false);
                if (IsSpellReady("Ring of Frost"))
                    return CastAtGround(player.Position, "Ring of Frost");
                return null;
            }
            if (player.Debuffs.Any(d => d.Spell != null && d.Spell.DispelType == SpellDispelType.Curse))
            {
                if (IsSpellReady("Remove Curse"))
                    return CastAtPlayer("Remove Curse");
                if (IsSpellReady("Remove Lesser Curse"))
                    return CastAtPlayer("Remove Lesser Curse");
            }
            //if (targetedEnemy != null && targetedEnemy.HasBuff("Spell Reflection") && IsSpellReady("Spellsteal"))
            //    return CastAtTarget("Spellsteal");
            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            if (inCombatEnemies.Count > 1)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                if ((inCombatEnemies.Count(u => u.IsTargetingPlayer || u.IsTargetingPlayerPet) >= 3 || (targetedEnemy?.IsElite ?? false)) && IsSpellReady("Mirror Image"))
                    return CastAtTarget("Mirror Image");
                if (targetedEnemy != null && inCombatEnemies.Count > 2 && IsSpellReady("Living Bomb") && !targetedEnemy.HasDeBuff("Living Bomb"))
                    return CastAtTarget("Living Bomb");
                var inMeleeRange = inCombatEnemies.Count(u => u.IsTargetingPlayer && u.IsInMeleeRange);
                if (IsSpellCasting("Flamestrike") || inCombatEnemies.Count(e=>!e.HasDeBuff("Flamestrike")) > 4 && inMeleeRange < 3)
                {
                    if (IsSpellCasting("Flamestrike"))
                        return CastAtGround(LastGroundSpellLocation, "Flamestrike");
                    if (!player.IsMoving && IsSpellReadyOrCasting("Flamestrike"))
                    {
                        var AoELocation = GetBestAoELocation(inCombatEnemies, 10f, out int numEnemiesInAoE);
                        if (numEnemiesInAoE >= 5)
                            return CastAtGround(AoELocation, "Flamestrike");
                        else
                            LogInfo($"{numEnemiesInAoE} enemies in Flamestrike!");
                    }
                }
                if (polyTarget != null && IsSpellCasting("Polymorph"))
                    return CastAtUnit(polyTarget, "Polymorph", isHarmfulSpell: true, facing: SpellFacingFlags.None);
                if (!player.IsMoving && player.HealthPercent < 55 && inMeleeRange < 3 && IsSpellReady("Polymorph") && !inCombatEnemies.Any(e=> e.HasAura("Polymorph", true)))
                {
                    var polymorphCandidates = inCombatEnemies.Where(e => IsViableForPolymorph(e, targetedEnemy)).ToList().OrderByDescending(u => u.HealthPercent);
                    if (polymorphCandidates.Any())
                    {
                        polyTarget = polymorphCandidates.First();
                        return CastAtUnit(polyTarget, "Polymorph", isHarmfulSpell: true, facing: SpellFacingFlags.None);
                    }
                }
            }
            if (!player.IsCasting)
            {
                var inFrontCone = GetUnitsInFrontOfPlayer(inCombatEnemies, 60, 8);
                if (inFrontCone.Count >= 1 && !inFrontCone.Any(e => e.HasAura("Polymorph")) && IsSpellReady("Dragon's Breath"))
                    return CastAtPlayerLocation("Dragon's Breath");
                var closeEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (!closeEnemies.Any(e => e.HasAura("Polymorph")) && closeEnemies.Where(u => !u.HasAura("Freeze") &&
                                                !u.HasAura("Frost Nova") && !u.HasAura("Dragon's Breath") && (u.CCs & ControlConditions.CC) == 0).Count() >= 1 && IsSpellReady("Frost Nova"))
                    return CastAtPlayerLocation("Frost Nova");
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if(targetedEnemy.IsCasting && IsSpellReady("Counterspell"))
                    return CastAtTarget("Counterspell");
                if (IsSpellReadyOrCasting("Fire Blast") && player.AuraStacks("Impact") > 0)
                    return CastAtTarget("Fire Blast");
                if (!targetedEnemy.IsInCombat)
                {
                    if (IsSpellReady("Living Bomb") && !targetedEnemy.HasDeBuff("Living Bomb"))
                        return CastAtTarget("Living Bomb");
                    if (IsSpellReady("Fire Blast"))
                        return CastAtTarget("Fire Blast");
                    if (IsSpellReadyOrCasting("Scorch"))
                        return CastAtTarget("Scorch");
                }
                if (targetedEnemy.HasBuff("Spell Reflection"))
                    return null;
                if (IsSpellReady("Combustion") && targetedEnemy.HasAura("Pyroblast!") && (targetedEnemy.HasAura("Living Bomb", true) || !PlayerLearnedSpell("Living Bomb")) && 
                    (targetedEnemy.HasAura("Ignite", true) || !player.HasAura("Ignite", true)))
                    return CastAtTarget("Combustion");
                if(IsSpellReadyOrCasting("Scorch") && (player.IsMoving && player.HasAura("Firestarter", true) || targetedEnemy.AuraRemainingTime("Critical Mass").TotalSeconds < 1 && player.HasAura("Critical Mass", true)))
                    return CastAtTarget("Scorch");
                if(IsSpellReadyOrCasting("Pyroblast") && player.HasAura(48108))
                    return CastAtTarget("Pyroblast");
                if (player.AuraCharges("Hot Streak")== 1 && !IsSpellReady("Pyroblast"))
                    LogInfo("Pyroblast not ready!");
                if (player.AuraCharges("Hot Streak") == 1)
                    LogWarning("Hot Streak!!!!");
                if (IsSpellReadyOrCasting("Frostfire Bolt") && player.AuraStacks("Fireball!") > 0)
                    return CastAtTarget("Frostfire Bolt");
                if (IsSpellReady("Flame Orb"))
                    return CastAtTarget("Flame Orb");
                if ((targetedEnemy.IsElite || targetedEnemy.HealthPercent >= 50) && IsSpellReady("Living Bomb") && !targetedEnemy.HasDeBuff("Living Bomb"))
                    return CastAtTarget("Living Bomb");
                if(player.IsMoving && IsSpellReady("Fire Blast"))
                    return CastAtTarget("Fire Blast");
                if (IsSpellReadyOrCasting("Fireball"))
                    return CastAtTarget("Fireball");
                if (IsSpellReady("Shoot"))
                    return CastAtTarget("Shoot");
                else
                    return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
        private static bool IsViableForPolymorph(WowUnit unit, WowUnit? currentTarget)
        {
            if (unit.HealthPercent < 20)
                return false;
            if ((unit.CCs & ControlConditions.CC) != 0)
                return false;
            if (unit.IsBoss)
                return false;
            if (unit.Level - ObjectManager.Instance.Player.Level <= -8)
                return false;
            if (unit.IsRooted || unit.IsStunned || unit.IsSapped)
                return false;
            if (unit.CreatureType != CreatureType.Beast && unit.CreatureType != CreatureType.Humanoid)
                return false;

            if (currentTarget != null && currentTarget.IsSameAs(unit))
                return false;

            if (!unit.IsInCombat)
                return false;

            if (unit.HasDeBuff("Living Bomb"))
                return false;

            if (!unit.IsTargetingPlayer && !unit.IsTargetingPlayerPet && !unit.IsTargetingMyPartyMember)
                return false;
            var group = ObjectManager.Instance.PlayerGroup;

            if (group != null && group.PlayerGroupMembers.Any(p => unit.IsSameAs(p.Target)))
                return false;

            return true;
        }
    }
}
