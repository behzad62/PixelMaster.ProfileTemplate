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
    public class Paladin : IPMRotation
    {
        public short Spec => 0;
        public UnitClass PlayerClass => UnitClass.Paladin;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "Paladin AOE";
        public string Author => "pacokeks";
        public string Description => "AOE Rotation for Paladin in WotLK";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            var linkedEnemies = PullingTarget?.LinkedEnemies;
            if (IsSpellReady("Hand of Reckoning"))
                return CastAtTarget("Hand of Reckoning");
            else if (IsSpellReady("Judgement of Wisdom"))
                return CastAtTarget("Judgement of Wisdom", facing: SpellFacingFlags.FaceTarget);
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
            //Health & Mana
            if (player.HealthPercent < 10)
            {
                if (IsSpellReady("Divine Protection") && !player.HasDeBuff("Forbearance"))
                    return CastAtPlayer("Divine Protection");
                if (IsSpellReadyOrCasting("Holy Light"))
                    return CastAtPlayer("Holy Light");
            }
            if (player.HealthPercent < 15)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }
            if (player.HealthPercent < 25)
            {
                if (IsSpellReady("Lay on Hands") && !player.HasDeBuff("Forbearance"))
                    return CastAtPlayer("Lay on Hands");
            }
            if (player.HealthPercent < 50)
            {
                if (IsSpellReady("Divine Shield") && !player.HasBuff("Divine Shield") && !player.HasDeBuff("Forbearance"))
                    return CastAtPlayer("Divine Shield");
                if (IsSpellReadyOrCasting("Holy Light"))
                    return CastAtPlayer("Holy Light");
            }
            if (player.PowerPercent < 15)
            {
                var manaPot = inv.GetManaPotion();
                if (manaPot != null)
                    return UseItem(manaPot);
            }
            if (player.PowerPercent < 60)
            {
                if (IsSpellReady("Divine Plea") && !player.HasBuff("Divine Plea"))
                    return CastAtPlayer("Divine Plea");
            }
            if (player.PowerPercent < 70)
            {
                if (IsSpellReady("Judgement of Wisdom"))
                    return CastAtTarget("Judgement of Wisdom", facing: SpellFacingFlags.FaceTarget);
            }

            //FleeFromFight
            if (player.IsFleeingFromTheFight)
            {
                inCombatEnemies = om.InCombatEnemies;
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 0)
                {
                    if (player.Race == UnitRace.Tauren && IsSpellReady("War Stomp"))
                        return CastAtTarget("War Stomp");
                    if (IsSpellReady("Hammer of Justice") && !targetedEnemy.HasDeBuff("Hammer of Justice") && targetedEnemy != null)
                        return CastAtTarget("Hammer of Justice");
                    if (IsSpellReady("Divine Protection") && !player.HasDeBuff("Forbearance"))
                        return CastAtPlayer("Divine Protection");
                    if (IsSpellReady("Divine Shield") && !player.HasDeBuff("Forbearance"))
                        return CastAtPlayer("Divine Shield");
                }
            }

            //Dispell
            if (player.Debuffs.Any(d => d.Spell != null && (d.Spell.DispelType == SpellDispelType.Disease || d.Spell.DispelType == SpellDispelType.Poison || d.Spell.DispelType == SpellDispelType.Magic)))
            {
                if (IsSpellReady("Cleanse"))
                    return CastAtPlayer("Cleanse");
                else if (IsSpellReady("Purify"))
                    return CastAtPlayer("Purify");
            }

            //Remove stun
            if (player.Debuffs.Any(d => d.Spell?.DispelType == SpellDispelType.Special))
            {
                if (player.Race == UnitRace.Human && IsSpellReady("Will to Survive") && targetedEnemy != null)
                    return CastAtTarget("Will to Survive");
            }

            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                if (IsSpellReady("Avenging Wrath"))
                    return CastAtPlayer("Avenging Wrath");
                if (IsSpellReady("Exorcism") && player.HasBuff("Art of War"))
                    return CastAtTarget("Exorcism");
            }

            //AoE handling
            inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 1)
                {
                    var StunCandidates = nearbyEnemies.Where(e => e.HealthPercent > 25 && (!e.CCs.HasFlag(ControlConditions.Root) || !e.CCs.HasFlag(ControlConditions.CC)) && player.HealthPercent < 25);

                    //Buffs
                    if (IsSpellReady("Retribution Aura") && !player.HasBuff("Retribution Aura"))
                        return CastAtPlayer("Retribution Aura");
                    if (IsSpellReady("Seal of Command") && !player.HasBuff("Seal of Command"))
                        return CastAtPlayer("Seal of Command");
                    if (IsSpellReady("Blessing of Might") && !player.HasBuff("Blessing of Might"))
                        return CastAtPlayer("Blessing of Might");
                    //Rotation
                    if (targetedEnemy?.HealthPercent < 20 && IsSpellReady("Hammer of Wrath"))
                        return CastAtTarget("Hammer of Wrath");
                    if (IsSpellReady("Judgement of Wisdom"))
                        return CastAtTarget("Judgement of Wisdom", facing: SpellFacingFlags.FaceTarget);
                    if (IsSpellReady("Avenging Wrath"))
                        return CastAtPlayer("Avenging Wrath");
                    if (IsSpellReady("Consecration"))
                        return CastAtPlayerLocation("Consecration");
                    if (IsSpellReady("Crusader Strike"))
                        return CastAtTarget("Crusader Strike", facing: SpellFacingFlags.FaceTarget);
                    if (IsSpellReady("Divine Storm"))
                        return CastAtTarget("Divine Storm", facing: SpellFacingFlags.FaceTarget);
                    if (IsSpellReady("Exorcism") && player.HasBuff("Art of War"))
                        return CastAtTarget("Exorcism");
                    if (targetedEnemy != null && StunCandidates.Any() && IsSpellReadyOrCasting("Hammer of Justice") && !inCombatEnemies.Any(e => e.HasDeBuff("Hammer of Justice")))
                        return CastAtUnit(StunCandidates.First(), "Hammer of Justice");
                    return CastAtTarget(sb.AutoAttack);

                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtTarget("Berserking", isHarmfulSpell: false);
                    if (IsSpellReady("Avenging Wrath"))
                        return CastAtPlayer("Avenging Wrath");
                    if (IsSpellReady("Exorcism") && player.HasBuff("Art of War"))
                        return CastAtTarget("Exorcism");
                }
            }
            
            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.HasDeBuff("Hammer of Justice") && (inCombatEnemies.Count > 1 || player.PowerPercent < 5))
                    return null;
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Hammer of Justice") && !targetedEnemy.HasDeBuff("Hammer of Justice"))
                        return CastAtTarget("Hammer of Justice");
                    if (player.Race == UnitRace.BloodElf && IsSpellReady("Arcane Torrent"))
                        return CastAtTarget("Arcane Torrent");
                }
                if (player.HasBuff("Art of War"))
                {
                    if (IsSpellReady("Exorcism"))
                        return CastAtTarget("Exorcism");
                }
                if (targetedEnemy.HealthPercent <20)
                {
                    if (IsSpellReady("Hammer of Wrath"))
                        return CastAtTarget("Hammer of Wrath");
                }
                if (targetedEnemy.IsInMeleeRange)
                {
                    var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 5);
                    if (player.HealthPercent < 50 || nearbyEnemies.Count > 1)
                    {
                        if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReady("War Stomp"))
                            return CastAtTarget("War Stomp");
                        if (IsSpellReady("Hammer of Justice") && !targetedEnemy.HasDeBuff("Hammer of Justice"))
                            return CastAtTarget("Hammer of Justice");
                        if (IsSpellReady("Divine Shield") && !player.HasDeBuff("Forbearance"))
                            return CastAtPlayer("Divine Shield");
                    }
                }
                //Buffs
                if (IsSpellReady("Seal of Command") && !player.HasBuff("Seal of Command"))
                    return CastAtPlayer("Seal of Command");
                if (IsSpellReady("Retribution Aura") && !player.HasBuff("Retribution Aura"))
                    return CastAtPlayer("Retribution Aura");
                if (IsSpellReady("Blessing of Might") && !player.HasBuff("Blessing of Might"))
                    return CastAtPlayer("Blessing of Might");
                //Single Target
                if (IsSpellReady("Hammer of Wrath") && targetedEnemy.HealthPercent < 20)
                    return CastAtTarget("Hammer of Wrath");
                if (IsSpellReady("Judgement of Wisdom"))
                    return CastAtTarget("Judgement of Wisdom", facing: SpellFacingFlags.FaceTarget);
                if (IsSpellReady("Crusader Strike"))
                    return CastAtTarget("Crusader Strike", facing: SpellFacingFlags.FaceTarget);
                if (IsSpellReady("Divine Storm"))
                    return CastAtTarget("Divine Storm", facing: SpellFacingFlags.FaceTarget);
                if (IsSpellReady("Exorcism") && player.HasBuff("Art of War"))
                    return CastAtTarget("Exorcism");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
