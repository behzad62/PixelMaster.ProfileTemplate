using PixelMaster.Core.API;
using PixelMaster.Core.Managers;
using PixelMaster.Core.Wow.Objects;
using static PixelMaster.Core.API.PMRotationBuilder;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Profiles;
using PixelMaster.Core.Behaviors;
using PixelMaster.Core.Behaviors.Transport;
using PixelMaster.Server.Shared;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;
using AdvancedCombatClasses.Settings;
using AdvancedCombatClasses.Settings.Era;

namespace CombatClasses
{
    public class WarlockDemoSoD : IPMRotation
    {
        private WarlockSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Warlock;

        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Era, WowVersion.Classic_Ptr };
        public short Spec => 2; // 2 for Demonology spec
        public UnitClass PlayerClass => UnitClass.Warlock;
        public CombatRole Role => CombatRole.RangeDPS;
        public string Name => "[SoD][PvE]Warlock-Demonology";
        public string Author => "PixelMaster";
        public string Description => "Demonology Warlock rotation for WoW Classic Season of Discovery and Era";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var pet = om.PlayerPet;
            var targetedEnemy = om.AnyEnemy;

            // Ensure we have the preferred pet summoned
            //if (!player.HasActivePet && IsSpellReady(settings.PreferredPet.ToString()))
            //    return CastWithoutTargeting(settings.PreferredPet.ToString(), isHarmfulSpell: false);

            // Apply buffs
            if (!player.HasAura("Fel Armor") && IsSpellReady("Fel Armor"))
                return CastWithoutTargeting("Fel Armor", isHarmfulSpell: false);

            if (targetedEnemy != null)
            {
                if (settings.StartPullWithPetAttack && pet != null && !pet.IsDead && !targetedEnemy.IsSameAs(pet.Target))
                    return PetAttack();
            }
            if (IsClassicEra && targetedEnemy != null
                && targetedEnemy.NearbyEnemies.Count <= 1)
            {
                if (IsSpellReady("Demon Charge"))
                    return CastAtTarget("Demon Charge");
            }
            if (IsSpellReadyOrCasting("Curse of Agony"))
                return CastAtTarget("Curse of Agony");
            else if (IsClassicEra && IsSpellReadyOrCasting("Immolate"))
                return CastAtTarget("Immolate");
            if (targetedEnemy != null)
            {
                // Open with Shadow Bolt to pull
                if (IsSpellReadyOrCasting("Shadow Bolt"))
                    return CastAtTarget("Shadow Bolt");
            }
            else if (IsSpellReadyOrCasting("Shoot"))
                return CastAtTarget("Shoot");
            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var target = om.AnyEnemy;
            var inv = om.Inventory;
            // Ensure we have the preferred pet summoned
            //if (!player.HasActivePet && IsSpellReady(settings.PreferredPet.ToString()))
            //    return CastWithoutTargeting(settings.PreferredPet.ToString(), isHarmfulSpell: false);

            // Apply Fel Armor if not active
            //if (!player.HasAura("Fel Armor") && IsSpellReady("Fel Armor"))
            //    return CastWithoutTargeting("Fel Armor", isHarmfulSpell: false);

            //if (player.HealthPercent < 30)
            //{
            //    var healingPot = inv.GetHealingPotion();
            //    if (healingPot != null)
            //        return UseItem(healingPot);
            //}
            // Use Healthstone if low health
            if (player.HealthPercent < settings.HealthstoneHealth && settings.UseHealthstone)
            {
                var healthStone = om.Inventory.GetHealthstones().FirstOrDefault();
                if (healthStone != null && IsItemReady(healthStone))
                    return UseItem(healthStone);
            }
            if (om.IsPlayerFleeingFromCombat)
            {
                if (target != null && !target.HasDebuff("Curse of Agony") && IsSpellReady("Curse of Agony"))
                    return CastAtTarget("Curse of Agony");
                if (target != null && !target.HasDebuff("Corruption") && IsSpellReady("Corruption"))
                    return CastAtTarget("Corruption");
                return null;
            }

            // Use Dark Pact if mana is low and pet has sufficient mana
            if (player.ManaPercent < 50 && IsSpellReady("Dark Pact"))
            {
                var pet = om.PlayerPet;
                if (pet != null && pet.ManaPercent > 20)
                    return CastWithoutTargeting("Dark Pact", isHarmfulSpell: false);
            }

            // Use Life Tap if mana is low and health is sufficient
            if (player.ManaPercent < 40 && player.HealthPercent > 65 && settings.UseLifeTap && IsSpellReady("Life Tap"))
                return CastWithoutTargeting("Life Tap", isHarmfulSpell: false);

            // Use Drain Life if health is low
            if (player.HealthPercent < 50 && IsSpellReadyOrCasting("Drain Life"))
                return CastAtTarget("Drain Life");


            // Use Metamorphosis if available
            if (IsSpellReady("Metamorphosis") && !player.HasAura("Metamorphosis"))
                return CastWithoutTargeting("Metamorphosis");

            var inCombatEnemies = om.InCombatEnemies.ToList();
            if (inCombatEnemies.Count > 1)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastWithoutTargeting("Berserking", isHarmfulSpell: false);
                if (IsSpellReady("Demonic Grace"))
                    return CastWithoutTargeting("Demonic Grace", isHarmfulSpell: false);
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 1)
                {
                    if (IsSpellReadyOrCasting("Howl of Terror"))
                        return CastWithoutTargeting("Howl of Terror");
                }
                if (inCombatEnemies.Count > 2)
                {
                    var banishCandidates = nearbyEnemies.Where(e => e.HealthPercent > 20 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root) && (e.CreatureType == CreatureType.Demon || e.CreatureType == CreatureType.Elemental));
                    if (banishCandidates.Any() && IsSpellReadyOrCasting("Banish") && !inCombatEnemies.Any(e => e.HasDebuff("Banish")))
                        return CastAtUnit(banishCandidates.First(), "Banish");
                    var fearCandidates = nearbyEnemies.Where(e => e.HealthPercent > 20 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root) && (e.CreatureType != CreatureType.Undead));
                    if (fearCandidates.Any() && IsSpellReadyOrCasting("Fear") && !inCombatEnemies.Any(e => e.HasDebuff("Fear")))
                        return CastAtUnit(fearCandidates.First(), "Fear");
                }

                var multiDotTarget = inCombatEnemies.FirstOrDefault(e => e.HealthPercent > 20 && !e.HasDebuff("Banish"));
                if (multiDotTarget != null)
                {
                    if (!multiDotTarget.HasDebuff("Haunt") && IsSpellReadyOrCasting("Haunt"))
                        return CastAtUnit(multiDotTarget, "Haunt");
                    if (!multiDotTarget.HasDebuff("Curse of Agony") && IsSpellReady("Curse of Agony"))
                        return CastAtUnit(multiDotTarget, "Curse of Agony");
                    if (multiDotTarget.HealthPercent > 30 && !multiDotTarget.HasDebuff("Corruption") && IsSpellReadyOrCasting("Corruption"))
                        return CastAtUnit(multiDotTarget, "Corruption");
                }
                if (target != null && target.GetNearbyInCombatEnemies(6).Count > 0 && IsSpellReadyOrCasting("Shadow Cleave"))
                    return CastAtTarget("Shadow Cleave");
            }

            if (target != null)
            {
                // 1. Assigned Curse
                switch (settings.AssignedCurse)
                {
                    case WarlockSettings.CurseType.CurseOfTheElements:
                        if (!target.HasAura("Curse of the Elements", castByPlayer: true) && IsSpellReady("Curse of the Elements"))
                            return CastAtTarget("Curse of the Elements");
                        break;
                    case WarlockSettings.CurseType.CurseOfRecklessness:
                        if (!target.HasAura("Curse of Recklessness", castByPlayer: true) && IsSpellReady("Curse of Recklessness"))
                            return CastAtTarget("Curse of Recklessness");
                        break;
                    case WarlockSettings.CurseType.CurseOfAgony:
                        if (!target.HasAura("Curse of Agony", castByPlayer: true) && IsSpellReady("Curse of Agony"))
                            return CastAtTarget("Curse of Agony");
                        break;
                    case WarlockSettings.CurseType.CurseOfDoom:
                        if (!target.HasAura("Curse of Doom", castByPlayer: true) && IsSpellReady("Curse of Doom"))
                            return CastAtTarget("Curse of Doom");
                        break;
                }

                if (target.HealthPercent < 30 && inv.GetItemCountInBags(6265) < MaxSoulShards() && IsSpellReadyOrCasting("Drain Soul"))
                    return CastAtTarget("Drain Soul");
                if (target.IsInPlayerMeleeRange)
                {
                    var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 5);
                    if (nearbyEnemies.Count > 1)
                    {
                        if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReady("War Stomp"))
                            return CastWithoutTargeting("War Stomp");
                    }
                    if (!player.IsCasting && IsSpellReady("Death Coil") && !target.HasBuff("Death Coil"))
                        return CastAtTarget("Death Coil");
                }

                if (target.HasDebuff("Shadow Vulnerability") && IsSpellReady("Siphon Life"))
                    return CastAtTarget("Siphon Life");

                // 2. Shadowflame
                if (!target.HasAura("Shadowflame", castByPlayer: true) && IsSpellReady("Shadowflame"))
                    return CastAtTarget("Shadowflame");

                // 3. Corruption
                if ((target.HealthPercent > 30 || target.IsElite) && !target.HasAura("Corruption", castByPlayer: true) && IsSpellReadyOrCasting("Corruption"))
                    return CastAtTarget("Corruption");

                if (player.PowerPercent > 10 && (IsClassicEra && player.HasBuff("Master Channeler") || player.HealthPercent < 70 || IsSpellCasting("Drain Life")) && IsSpellReadyOrCasting("Drain Life"))
                    return CastAtTarget("Drain Life");

                // 4. Immolate
                if (IsClassicEra && target.HealthPercent > 30 && !target.HasAura("Immolate", castByPlayer: true) && IsSpellReadyOrCasting("Immolate"))
                    return CastAtTarget("Immolate");
                if (IsSpellReadyOrCasting("Chaos Bolt"))
                    return CastAtTarget("Chaos Bolt");
                if (IsClassicEra && target.HealthPercent > 30 && !target.HasDebuff("Incinerate") && IsSpellReadyOrCasting("Incinerate"))
                    return CastAtTarget("Incinerate");
                // Finisher: Shadowburn
                if (target.HealthPercent < 20 && IsSpellReady("Shadowburn"))
                    return CastAtTarget("Shadowburn");

                // 6. Soul Fire (if Decimation proc is active)
                if (player.HasAura("Decimation") && IsSpellReadyOrCasting("Soul Fire"))
                    return CastAtTarget("Soul Fire");

                // 7. Haunt
                if (settings.UseHaunt && IsSpellReady("Haunt"))
                    return CastAtTarget("Haunt");

                // 8. Shadow Bolt (if Shadow Trance proc is active)
                if (player.HasAura("Shadow Trance") && IsSpellReadyOrCasting("Shadow Bolt"))
                    return CastAtTarget("Shadow Bolt");


                // Searing Pain during movement or if mob will die soon
                //if ((target.HealthPercent < 15 || player.IsMoving) && IsSpellReadyOrCasting("Searing Pain"))
                //    return CastAtTarget("Searing Pain");

                // Filler: Shadow Bolt
                if ((player.PowerPercent > 30 || target.DistanceSquaredToPlayer > 64) && IsSpellReadyOrCasting("Shadow Bolt"))
                    return CastAtTarget("Shadow Bolt");
                else if (IsSpellReadyOrCasting("Shoot"))
                    return CastAtTarget("Shoot");
                if (IsSpellReadyOrCasting("Shadow Bolt"))
                    return CastAtTarget("Shadow Bolt");
                if (!player.IsCasting && !target.IsPlayerAttacking)
                    return CastAtTarget(sb.AutoAttack);
            }

            return null;
        }
        private int MaxSoulShards()
        {
            var inv = ObjectManager.Instance.Inventory;
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
