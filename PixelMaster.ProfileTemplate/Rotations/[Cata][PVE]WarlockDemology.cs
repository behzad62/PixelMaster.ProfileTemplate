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
using System.Xml.Schema;
using System.Runtime.ExceptionServices;

namespace CombatClasses
{
    public class WarlockDemo : IPMRotation
    {
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Warlock;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[Cata][PvE]Warlock-Demology";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var inv = player.Inventory;
            var sb = player.SpellBook;
            var pet = om.PlayerPet;
            var targetedEnemy = om.AnyEnemy;

            if (targetedEnemy != null)
            {
                if (pet != null && !pet.IsDead && !targetedEnemy.IsSameAs(pet.Target))
                    return PetAttack();
            }
            if (IsClassicEra && targetedEnemy != null
                && targetedEnemy.NearbyEnemies.Count <= 1)
            {
                if (IsSpellReady("Demon Charge"))
                    return CastAtTarget("Demon Charge");
            }
            if (!IsClassicEra && pet != null && !pet.IsDead && !pet.HasBuff("Dark Intent") && IsSpellReady("Dark Intent"))
                return CastAtPet("Dark Intent");
            if (IsSpellReadyOrCasting("Immolate"))
                return CastAtTarget("Immolate");
            else if(IsSpellReadyOrCasting("Bane of Agony"))
                return CastAtTarget("Bane of Agony");
            else if(IsSpellReadyOrCasting("Curse of Agony"))
                return CastAtTarget("Curse of Agony");
            else if (IsSpellReadyOrCasting("Shadow Bolt"))
                return CastAtTarget("Shadow Bolt");
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var pet = om.PlayerPet;
            var sb = player.SpellBook;
            var inv = player.Inventory;
            List<WowUnit>? inCombatEnemies = null;
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
                if (targetedEnemy != null && !targetedEnemy.HasDeBuff("Bane of Agony") && IsSpellReady("Bane of Agony"))
                    return CastAtTarget("Bane of Agony");
                if (targetedEnemy != null && !targetedEnemy.HasDeBuff("Curse of Agony") && IsSpellReady("Curse of Agony"))
                    return CastAtTarget("Curse of Agony");
                if (targetedEnemy != null && !targetedEnemy.HasDeBuff("Corruption") && IsSpellReady("Corruption"))
                    return CastAtTarget("Corruption");
                return null;
            }
            if (player.PowerPercent < 20 && !player.IsCasting)
            {
                var manaPot = inv.GetManaPotion();
                if (manaPot != null)
                    return UseItem(manaPot);
            }
            if (IsSpellReady("Metamorphosis") && !player.HasBuff("Metamorphosis"))
                return CastAtPlayer("Metamorphosis");
            if (IsSpellReady("Soulburn") && player.SecondaryPower > 0 && !player.HasBuff("Soulburn") )
                return CastAtPlayerLocation("Soulburn", isHarmfulSpell: false);
            if (pet != null && !pet.IsDead && IsSpellReady("Demon Soul") && !player.HasBuff("Demon Soul"))
                return CastAtPlayerLocation("Demon Soul", isHarmfulSpell: false);
            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                if (IsSpellReady("Demonic Grace"))
                    return CastAtPlayerLocation("Demonic Grace", isHarmfulSpell: false);
            }
            //AoE handling
            inCombatEnemies = om.InCombatEnemies;
            if(inCombatEnemies.Count > 0)
            {
                if(inCombatEnemies.Any(x => x.IsTargetingPlayer) && IsSpellReady("Soulshatter"))
                    return CastAtPlayerLocation("Soulshatter", isHarmfulSpell: false);
            }
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 5);
                if (nearbyEnemies.Count > 1)
                {
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReady("War Stomp"))
                        return CastAtPlayerLocation("War Stomp");
                }
                if (pet != null && !pet.IsDead && IsSpellReady("Felstorm"))
                {
                    nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, pet.Position, 10);
                    if(nearbyEnemies.Count > 1)
                    {
                        return CastPetAbilityAtPetLocation("Felstorm");
                    }
                }
                var inFrontCone = GetUnitsInFrontOfPlayer(inCombatEnemies, 60, 10);
                if (inFrontCone.Count >= 3 && !inFrontCone.Any(e => e.HasAura("Polymorph")) && IsSpellReady("Shadowflame"))
                    return CastAtPlayerLocation("Shadowflame");
                nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
                if (inCombatEnemies.Count > 2)
                {
                    if (IsSpellReadyOrCasting("Howl of Terror"))
                        return CastAtPlayerLocation("Howl of Terror");
                    var banishCandidates = nearbyEnemies.Where(e => e.HealthPercent > 20 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root) && (e.CreatureType == CreatureType.Demon || e.CreatureType == CreatureType.Elemental));
                    if (banishCandidates.Any() && IsSpellReadyOrCasting("Banish") && !inCombatEnemies.Any(e => e.HasDeBuff("Banish")))
                        return CastAtUnit(banishCandidates.First(), "Banish");
                    var fearCandidates = nearbyEnemies.Where(e => e.HealthPercent > 20 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root) && (e.CreatureType != CreatureType.Undead));
                    if (fearCandidates.Any() && IsSpellReadyOrCasting("Fear") && !inCombatEnemies.Any(e => e.HasDeBuff("Fear")))
                        return CastAtUnit(fearCandidates.First(), "Fear");
                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayerLocation("Berserking", isHarmfulSpell: false);
                    if (IsSpellReady("Demonic Grace"))
                        return CastAtPlayerLocation("Demonic Grace", isHarmfulSpell: false);
                }
                var multiDotTarget = inCombatEnemies.FirstOrDefault(e => e.HealthPercent > 20 && !e.HasDeBuff("Banish") && (!e.HasDeBuff("Bane of Agony") || !e.HasDeBuff("Corruption") || !e.HasDeBuff("Immolate")));
                if (multiDotTarget != null)
                {
                    //if (!multiDotTarget.HasDeBuff("Haunt") && IsSpellReadyOrCasting("Haunt"))
                    //    return CastAtUnit(multiDotTarget, "Haunt");
                    if (!multiDotTarget.HasDeBuff("Immolate") && IsSpellReadyOrCasting("Immolate"))
                        return CastAtUnit(multiDotTarget, "Immolate");
                    if (!multiDotTarget.HasDeBuff("Bane of Agony") && IsSpellReady("Bane of Agony"))
                        return CastAtUnit(multiDotTarget, "Bane of Agony");
                    if ((multiDotTarget.HealthPercent > 30 || multiDotTarget.IsElite) && !multiDotTarget.HasDeBuff("Corruption") && IsSpellReadyOrCasting("Corruption"))
                        return CastAtUnit(multiDotTarget, "Corruption");
                    if (!multiDotTarget.HasDeBuff("Curse of Agony") && IsSpellReady("Curse of Agony"))
                        return CastAtUnit(multiDotTarget, "Curse of Agony");
                }
                if (targetedEnemy != null && targetedEnemy.GetNearbyInCombatEnemies(6).Count > 0 && IsSpellReadyOrCasting("Shadow Cleave"))
                    return CastAtTarget("Shadow Cleave");
            }
            if (player.HealthPercent > 30 && player.PowerPercent <= 25 && !player.IsCasting && IsSpellReady("Life Tap"))
                return CastAtPlayerLocation("Life Tap", isHarmfulSpell: false);
            if (pet != null && !pet.IsDead && !inCombatEnemies.Any(e => e.IsTargetingPlayer) && ((pet.HealthPercent < 50 && !player.IsCasting) || pet.HealthPercent < 30 || IsSpellCasting("Health Funnel")) && IsSpellReadyOrCasting("Health Funnel"))
                return CastAtPet("Health Funnel");
            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.HealthPercent <= 25 && (IsClassicEra && inv.GetItemCountInBags(6265) < MaxSoulShards() || player.SecondaryPower <= 1) && IsSpellReadyOrCasting("Drain Soul"))
                    return CastAtTarget("Drain Soul");
                if(player.HealthPercent <= 70 && IsSpellReady("Death Coil"))
                    return CastAtTarget("Death Coil");
                if (targetedEnemy.IsInMeleeRange)
                {
                    if (!player.IsCasting && IsSpellReady("Death Coil") && !targetedEnemy.HasBuff("Death Coil"))
                        return CastAtTarget("Death Coil");
                    if(targetedEnemy.IsCasting)
                    {
                        if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReady("War Stomp"))
                            return CastAtPlayerLocation("War Stomp");
                    }
                }
                if (targetedEnemy.IsMovingAwayFromPlayer && IsSpellReady("Axe Toss"))
                    return CastPetAbilityAtTarget("Axe Toss");
                //if (!targetedEnemy.HasDeBuff("Siphon Life") && IsSpellReady("Siphon Life"))
                //    return CastAtTarget("Siphon Life");
                //if (!targetedEnemy.HasDeBuff("Haunt") && IsSpellReadyOrCasting("Haunt"))
                //    return CastAtTarget("Haunt");
                if ((targetedEnemy.HealthPercent > 30 || targetedEnemy.IsElite) && !targetedEnemy.HasAura("Immolate", true) && IsSpellReadyOrCasting("Immolate"))
                    return CastAtTarget("Immolate");
                if (IsSpellReadyOrCasting("Hand of Gul'dan"))
                    return CastAtTarget("Hand of Gul'dan");
                if (!targetedEnemy.HasDeBuff("Bane of Agony") && IsSpellReady("Bane of Agony"))
                    return CastAtTarget("Bane of Agony");
                if ((targetedEnemy.HealthPercent > 30 || targetedEnemy.IsElite) && !targetedEnemy.HasDeBuff("Corruption") && IsSpellReadyOrCasting("Corruption"))
                    return CastAtTarget("Corruption");
                if(targetedEnemy.HealthPercent < 25 && targetedEnemy.IsElite && IsSpellReadyOrCasting("Soul Fire"))
                    return CastAtTarget("Soul Fire");
                if (player.PowerPercent > 10 && (IsClassicEra && player.HasBuff("Master Channeler") || player.HealthPercent < 50 || IsSpellCasting("Drain Life")) && IsSpellReadyOrCasting("Drain Life"))
                    return CastAtTarget("Drain Life");
                if (IsSpellReadyOrCasting("Shadow Bolt") && player.HasBuff("Shadow Trance"))
                    return CastAtTarget("Shadow Bolt");
                if (IsSpellReadyOrCasting("Incinerate") && player.HasBuff("Molten Core"))
                    return CastAtTarget("Incinerate");
                if (IsClassicEra && targetedEnemy.HealthPercent > 30 && !targetedEnemy.HasDeBuff("Incinerate") && IsSpellReadyOrCasting("Incinerate"))
                    return CastAtTarget("Incinerate");
                if (IsSpellReadyOrCasting("Soul Fire") && player.HasBuff("Decimation"))
                    return CastAtTarget("Soul Fire");

                if (IsSpellReadyOrCasting("Shadow Bolt"))
                    return CastAtTarget("Shadow Bolt");
                else if (IsSpellReadyOrCasting("Shoot"))
                    return CastAtTarget("Shoot");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }

        private int MaxSoulShards()
        {
            var inv = ObjectManager.Instance.Player.Inventory;
            int totalSoulSlots = 0;
            foreach (var bag in inv.Bags.Where(b => b.BagType == BagType.SoulBag))
            {
                totalSoulSlots += bag.FreeSlots;
            }
            if (totalSoulSlots == 0)
                return 3;
            return Math.Min(10, totalSoulSlots);
        }
    }
}
