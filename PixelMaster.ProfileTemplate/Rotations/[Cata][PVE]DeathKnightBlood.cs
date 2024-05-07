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
    public class DeathKinghtBlood : IPMRotation
    {
        private const bool UseAntiMagicShell = true;
        private const bool UseIceboundFortitude = true;
        private const bool IceboundFortitudeExclusive = true;
        private const double IceboundFortitudePercent = 35;
        private const bool UseLichborne = true;
        private const bool LichborneExclusive = true;
        private const double LichbornePercent = 55;
        private const double DeathStrikeEmergencyPercent = 70;
        private const bool UsePetSacrifice = true;
        private const bool PetSacrificeExclusive = true;
        private const double PetSacrificePercent = 70;
        private const double PetSacrificeSummonPercent = 70;
        private const int DeathAndDecayCount = 3;
        private const bool UseBoneShield = true;
        private const int BoneShieldPercent = 95;
        private const bool BoneShieldExclusive = true;
        private const bool UseDeathAndDecay = true;
        private const bool UseDancingRuneWeapon = true;
        private const bool UseVampiricBlood = true;
        private const bool VampiricBloodExclusive = true;
        private const int VampiricBloodPercent = 50;
        private const int EmpowerRuneWeaponPercent = 50;
        private const bool UseArmyOfTheDead = true;
        private const int ArmyOfTheDeadPercent = 50;
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.DeathKnight;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]DeathKnight-Blood ";
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
                if (targetedEnemy.DistanceSquaredToPlayer > 10 * 10 && IsSpellReady("Death Grip"))
                    return CastAtTarget("Death Grip", facing: SpellFacingFlags.None);
                if (IsSpellReady("Howling Blast"))
                    return CastAtTarget("Howling Blast");
                if (IsSpellReady("Icy Touch"))
                    return CastAtTarget("Icy Touch");
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
            List<WowUnit>? inCombatEnemies = inCombatEnemies = om.InCombatEnemies;

            if (!player.HasAura("Blood Presence") && IsSpellReady("Blood Presence"))
                return CastAtPlayer("Blood Presence");
            if (inCombatEnemies.Any(u => (u.IsCasting || u.ChannelingSpellID != 0) &&
                u.TargetGUID == player.WowGuid &&
                UseAntiMagicShell))
                CastAtPlayerLocation("Anti-Magic Shell", isHarmfulSpell: false);
            if (player.HealthPercent < PetSacrificePercent && player.HasActivePet && IsSpellReady("Death Pact"))
                return CastAtPlayer("Death Pact");
            if (player.HealthPercent < 90 && player.HasAura("Will of the Necropolis") && IsSpellReady("Rune Tap"))
                return CastAtPlayer("Rune Tap");
            if (player.HealthPercent < 70 && player.HasAura("Lichborne") && IsSpellReady("Death Coil"))
                return CastAtPlayer("Death Coil");
            if (UseDancingRuneWeapon && inCombatEnemies.Count > 2 && IsSpellReady("Dancing Rune Weapon"))
                CastAtPlayerLocation("Dancing Rune Weapo");
            if (UseBoneShield
                && (!BoneShieldExclusive && player.HealthPercent < BoneShieldPercent
                || (!player.HasAura("Vampiric Blood") && !player.HasAura("Dancing Rune Weapon") && !player.HasAura("Lichborne") && !player.HasAura("Icebound Fortitude")))
                && IsSpellReady("Bone Shield"))
                CastAtPlayerLocation("Bone Shield", isHarmfulSpell: false);
            if (UseVampiricBlood && player.HealthPercent < VampiricBloodPercent && (!VampiricBloodExclusive
                || (!player.HasAura("Bone Shield") && !player.HasAura("Dancing Rune Weapon") && !player.HasAura("Lichborne") && !player.HasAura("Icebound Fortitude")))
                && IsSpellReady("Vampiric Blood"))
                CastAtPlayerLocation("Vampiric Blood", isHarmfulSpell: false);
            if (UseLichborne && player.HealthPercent < LichbornePercent && player.Power >= 600 && (!LichborneExclusive
             || (!player.HasAura("Bone Shield") && !player.HasAura("Dancing Rune Weapon") && !player.HasAura("Vampiric Blood") && !player.HasAura("Icebound Fortitude")))
             && IsSpellReady("Lichborne"))
                CastAtPlayerLocation("Lichborne", isHarmfulSpell: false);
            if (UsePetSacrifice && player.HealthPercent < PetSacrificeSummonPercent && (!PetSacrificeExclusive
                 || (!player.HasAura("Bone Shield") && !player.HasAura("Dancing Rune Weapon") && !player.HasAura("Vampiric Blood") && !player.HasAura("Icebound Fortitude") && !player.HasAura("Lichborne")))
                 && IsSpellReady("Raise Dead"))
                CastAtPlayerLocation("Raise Dead", isHarmfulSpell: false);
            if (UseIceboundFortitude && player.HealthPercent < IceboundFortitudePercent && player.Power >= 600 && (!IceboundFortitudeExclusive
             || (!player.HasAura("Bone Shield") && !player.HasAura("Dancing Rune Weapon") && !player.HasAura("Vampiric Blood") && !player.HasAura("Lichborne")))
             && IsSpellReady("Icebound Fortitude"))
                CastAtPlayerLocation("Icebound Fortitude", isHarmfulSpell: false);
            if (player.HealthPercent < EmpowerRuneWeaponPercent && !IsSpellReady("Death Strike") && IsSpellReady("Empower Rune Weapon"))
                CastAtPlayerLocation("Empower Rune Weapon", isHarmfulSpell: false);
            if (player.HealthPercent < ArmyOfTheDeadPercent && UseArmyOfTheDead && IsSpellReady("Army of the Dead"))
                CastAtPlayerLocation("Army of the Dead", isHarmfulSpell: true);

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
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 12);
                var ccCandidates = nearbyEnemies.Where(e => !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root));
                foreach (var ccCandidate in ccCandidates)
                {
                    if (IsSpellReady("Strangulate") && !ccCandidate.HasDeBuff("Strangulate"))
                        return CastAtUnit(ccCandidate, "Strangulate");
                }
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
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 15);
                if (nearbyEnemies.Count >= DeathAndDecayCount)
                {
                    if (UseDeathAndDecay && IsSpellReady("Death and Decay"))
                    {
                        var AoELocation = GetBestAoELocation(inCombatEnemies, 10f, out int numEnemiesInAoE);
                        return CastAtGround(AoELocation, "Death and Decay");
                    }
                    if (targetedEnemy != null && (!targetedEnemy.HasDeBuff("Frost Fever") || !targetedEnemy.HasDeBuff("Blood Plague")) && IsSpellReady("Outbreak"))
                        return CastAtTarget("Outbreak");
                    if (targetedEnemy != null && !targetedEnemy.HasDeBuff("Frost Fever") && IsSpellReady("Icy Touch"))
                        return CastAtTarget("Icy Touch");
                    if (targetedEnemy != null && !targetedEnemy.HasDeBuff("Blood Plague") && IsSpellReady("Plague Strike"))
                        return CastAtTarget("Plague Strike");

                    if (targetedEnemy != null
                        && IsSpellReady("Pestilence")
                        && targetedEnemy.HasDeBuff("Frost Fever") && targetedEnemy.HasDeBuff("Blood Plague")
                        && targetedEnemy.GetNearbyInCombatEnemies(10).Where(e => e.HasDeBuff("Frost Fever") && e.HasDeBuff("Blood Plague")).Any())
                        return CastAtTarget("Pestilence", facing: SpellFacingFlags.None);
                    if (targetedEnemy != null && IsSpellReady("Death Strike"))
                        return CastAtTarget("Death Strike");
                    if (targetedEnemy != null && IsSpellReady("Heart Strike"))
                        return CastAtTarget("Heart Strike");
                    if (targetedEnemy != null && IsSpellReady("Rune Strike"))
                        return CastAtTarget("Rune Strike");
                    if (targetedEnemy != null && IsSpellReady("Icy Touch"))
                        return CastAtTarget("Icy Touch");
                    var ccCandidates = nearbyEnemies.Where(e => e.HealthPercent > 25 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root));
                    foreach (var ccCandidate in ccCandidates)
                    {
                        if (IsSpellReady("Strangulate") && !ccCandidate.HasDeBuff("Strangulate"))
                            return CastAtUnit(ccCandidate, "Strangulate");
                    }
                }
                //if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
                //{

                //}
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsMovingAwayFromPlayer && IsSpellReady("Chains of Ice"))
                    return CastAtTarget("Chains of Ice", facing: SpellFacingFlags.None);
                if (targetedEnemy.DistanceSquaredToPlayer > 10 * 10 && IsSpellReady("Death Grip"))
                    return CastAtTarget("Death Grip", facing: SpellFacingFlags.None);
                if (player.HealthPercent < DeathStrikeEmergencyPercent && IsSpellReady("Death Strike"))
                    return CastAtTarget("Death Strike");
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Mind Freeze") && targetedEnemy.DistanceSquaredToPlayer < 64)
                        return CastAtTarget("Mind Freeze");
                }
                if ((!targetedEnemy.HasDeBuff("Frost Fever") || !targetedEnemy.HasDeBuff("Blood Plague")) && IsSpellReady("Outbreak"))
                    return CastAtTarget("Outbreak");
                if (!targetedEnemy.HasDeBuff("Frost Fever") && IsSpellReady("Icy Touch"))
                    return CastAtTarget("Icy Touch");
                if (!targetedEnemy.HasDeBuff("Blood Plague") && IsSpellReady("Plague Strike"))
                    return CastAtTarget("Plague Strike");
                if (IsSpellReady("Death Coil") && player.Power >= 800 && !PlayerLearnedSpell("Rune Strike"))
                    return CastAtTarget("Death Coil");
                if (IsSpellReady("Death Coil") && !targetedEnemy.IsInMeleeRange)
                    return CastAtTarget("Death Coil");
                if (IsSpellReady("Rune Strike"))
                    return CastAtTarget("Rune Strike");
                if (targetedEnemy != null && IsSpellReady("Death Strike"))
                    return CastAtTarget("Death Strike");
                if (targetedEnemy != null && IsSpellReady("Heart Strike"))
                    return CastAtTarget("Heart Strike");
                if (targetedEnemy != null && IsSpellReady("Icy Touch"))
                    return CastAtTarget("Icy Touch");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
