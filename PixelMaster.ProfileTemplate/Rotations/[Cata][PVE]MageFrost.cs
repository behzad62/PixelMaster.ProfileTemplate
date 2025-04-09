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
using AdvancedCombatClasses.Settings.Cata;
using PixelMaster.Server.Shared;

namespace CombatClasses
{
    public class MageFrost : IPMRotation
    {
        private MageSettings settings => ((CataCombatSettings)SettingsManager.Instance.Settings).Mage;
        private WowUnit? polyTarget;
        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Cata, WowVersion.Classic_Cata_Ptr };
        public short Spec => 3;
        public UnitClass PlayerClass => UnitClass.Mage;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Mage-Frost";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            var pet = om.PlayerPet;
            if (IsSpellReadyOrCasting("Summon Water Elemental") && (pet is null || pet.IsDead))
                return CastWithoutTargeting("Summon Water Elemental");
            if (targetedEnemy != null)
            {
                if (IsSpellReadyOrCasting("Frostfire Bolt"))
                    return CastAtTarget("Frostfire Bolt");
                if (IsSpellReadyOrCasting("Frostbolt"))
                    return CastAtTarget("Frostbolt");
            }
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = om.SpellBook;
            var inv = om.Inventory;
            var pet = om.PlayerPet;
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies.ToList();

            // Defensive stuff
            if (player.HasAura("Ice Block"))
                return null;
            if (IsSpellReadyOrCasting("Summon Water Elemental") && (pet is null || pet.IsDead))
                return CastWithoutTargeting("Summon Water Elemental");
            if (!player.HasAura("Ice Armor") && IsSpellReady("Ice Armor"))
                return CastWithoutTargeting("Ice Armor", isHarmfulSpell: false);
            if (player.HealthPercent < 20 && !player.HasBuff("Hypothermia") && IsSpellReady("Ice Block"))
                return CastWithoutTargeting("Ice Block", isHarmfulSpell: false);
            // Cooldowns
            if (settings.UseEvocation && (player.PowerPercent < settings.EvocationManaPercent || IsSpellCasting("Evocation") || player.HealthPercent < 50 && player.HasAura("Glyph of Evocation")) && IsSpellReadyOrCasting("Evocation"))
                return CastWithoutTargeting("Evocation", isHarmfulSpell: false);
            if (player.HealthPercent <= 80 && !player.HasAura("Mage Ward") && IsSpellReady("Mage Ward"))
                return CastWithoutTargeting("Mage Ward", isHarmfulSpell: false);
            if (player.HealthPercent <= 60 && !player.HasAura("Mana Shield") && IsSpellReady("Mana Shield"))
                return CastWithoutTargeting("Mana Shield", isHarmfulSpell: false);

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

            if (om.IsPlayerFleeingFromCombat)
            {
                if (GetUnitsWithinArea(inCombatEnemies, player.Position, 8).Count >= 1 && IsSpellReady("Frost Nova"))
                    return CastWithoutTargeting("Frost Nova");
                if (IsSpellReady("Blink"))
                    return CastWithoutTargeting("Blink", isHarmfulSpell: false);
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
            //Burst
            if (settings.UseArcanePower && dynamicSettings.BurstEnabled && IsSpellReady("Arcane Power"))
            {
                return CastWithoutTargeting("Arcane Power", isHarmfulSpell: false);
            }
            //AoE handling
            var inMeleeRange = inCombatEnemies.Count(u => u.IsTargetingPlayer && u.IsInPlayerMeleeRange);
            if (inCombatEnemies.Count > 1)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastWithoutTargeting("Berserking", isHarmfulSpell: false);
                if (inCombatEnemies.Count(u => u.IsTargetingPlayer || u.IsTargetingPlayerPet) >= 3 || (targetedEnemy?.IsElite ?? false))
                {
                    if (IsSpellReady("Mirror Image"))
                        return CastWithoutTargeting("Mirror Image");
                    if (IsSpellReady("Icy Veins"))
                        return CastWithoutTargeting("Icy Veins", isHarmfulSpell: false);
                }
                if (IsSpellCasting("Blizzard") || inCombatEnemies.Count > 4 && inMeleeRange < 3)
                {
                    if (IsSpellCasting("Blizzard"))
                        return CastAtGround(LastGroundSpellLocation, "Blizzard");
                    if (!player.IsMoving && IsSpellReadyOrCasting("Blizzard"))
                    {
                        var AoELocation = GetBestAoELocation(inCombatEnemies, 10f, out int numEnemiesInAoE);
                        if (numEnemiesInAoE >= 5)
                            return CastAtGround(AoELocation, "Blizzard");
                    }
                }
                if (inMeleeRange >= 3 || player.IsStunned || player.IsSapped || player.IsRooted)
                {
                    if (IsSpellReady("Blink"))
                    {
                        Vector3? blinkTarget = GetSafePlaceAroundPlayer(20);
                        if (blinkTarget.HasValue)
                        {
                            return CastAtDirection(blinkTarget.Value, "Blink");
                        }
                        else
                            return CastWithoutTargeting("Blink", isHarmfulSpell: false);
                    }
                    if (!player.HasAura("Mana Shield") && IsSpellReady("Mana Shield"))
                        return CastWithoutTargeting("Mana Shield", isHarmfulSpell: false);
                }
            }
            if (!player.IsCasting)
            {
                var inFrontCone = GetUnitsInFrontOfPlayer(inCombatEnemies, 60, 8);
                if ((inFrontCone.Count >= 1 && player.HasAura("Improved Cone of Cold") || inFrontCone.Count > 1) && !inFrontCone.Any(e => e.HasAura("Polymorph")) && IsSpellReady("Cone of Cold"))
                    return CastWithoutTargeting("Cone of Cold");
                var closeEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (!closeEnemies.Any(e => e.HasAura("Polymorph")) && closeEnemies.Where(u => !u.HasAnyDebuff(false, "Freeze", "Frost Nova", "Dragon's Breath", "Improved Cone of Cold", "Deep Freeze") && !u.IsCCed).Count() >= 1)
                {
                    if (IsSpellReady("Freeze"))
                        return CastPetAbilityAtGround(player.Position, "Freeze");

                    if (IsSpellReady("Frost Nova"))
                        return CastWithoutTargeting("Frost Nova");
                }
            }

            if (polyTarget != null && IsSpellCasting("Polymorph") && !inCombatEnemies.Any(e => e.HasDebuff("Polymorph")) && inMeleeRange <= 2)
                return CastAtUnit(polyTarget, "Polymorph", isHarmfulSpell: true, facing: SpellFacingFlags.None);
            else if (!player.IsMoving && inCombatEnemies.Count > 2 && !player.IsMoving && player.HealthPercent < 55 && inMeleeRange <= 2 && IsSpellReady("Polymorph") && !inCombatEnemies.Any(e => e.HasAura("Polymorph", true)))
            {
                var polymorphCandidates = inCombatEnemies.Where(e => IsViableForPolymorph(e, targetedEnemy)).ToList().OrderByDescending(u => u.HealthPercent);
                if (polymorphCandidates.Any())
                {
                    polyTarget = polymorphCandidates.First();
                    return CastAtUnit(polyTarget, "Polymorph", isHarmfulSpell: true, facing: SpellFacingFlags.None);
                }
            }
            polyTarget = null;

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting && IsSpellReady("Counterspell"))
                    return CastAtTarget("Counterspell");

                if (!targetedEnemy.IsInCombat)
                {
                    if (IsSpellReady("Ice Lance"))
                        return CastAtTarget("Ice Lance");
                    if (IsSpellReady("Fire Blast"))
                        return CastAtTarget("Fire Blast");
                }
                if (IsSpellReady("Deep Freeze") && (player.HasBuff("Fingers of Frost") || targetedEnemy.HasAnyDebuff(false, "Freeze", "Frost Nova", "Improved Cone of Cold")))
                    return CastAtTarget("Deep Freeze");
                if (IsSpellReady("Frostfire Orb"))
                    return CastAtTarget("Frostfire Orb");
                if (player.HasBuff("Brain Freeze"))
                {
                    if (IsSpellReadyOrCasting("Frostfire Bolt"))
                        return CastAtTarget("Frostfire Bolt");
                    if (IsSpellReadyOrCasting("Fireball"))
                        return CastAtTarget("Fireball");
                }
                if (IsSpellReady("Ice Lance") && (player.HasBuff("Fingers of Frost") || targetedEnemy.HasAnyDebuff(false, "Freeze", "Frost Nova", "Improved Cone of Cold")))
                    return CastAtTarget("Ice Lance");
                if (IsSpellReadyOrCasting("Frostbolt"))
                    return CastAtTarget("Frostbolt");
                if (IsSpellReady("Shoot"))
                    return CastAtTarget("Shoot");
                else if (!player.IsCasting && !targetedEnemy.IsPlayerAttacking)
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

            if (unit.HasDebuff("Living Bomb"))
                return false;

            if (!unit.IsTargetingPlayer && !unit.IsTargetingPlayerPet && !unit.IsTargetingMyPartyMember)
                return false;
            var group = ObjectManager.Instance.PlayerGroup;

            if (group != null && group.MemberGUIDs.Any(p => unit.WowGuid == p))
                return false;

            return true;
        }
    }
}
