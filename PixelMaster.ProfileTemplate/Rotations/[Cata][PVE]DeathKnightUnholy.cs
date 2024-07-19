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
    public class DeathKinghtUnholy : IPMRotation
    {
        private bool UseAntiMagicShell => settings.UseAntiMagicShell;
        private bool UseIceboundFortitude => settings.UseIceboundFortitude;
        private int IceboundFortitudePercent => settings.IceboundFortitudePercent;
        private bool UseLichborne => settings.UseLichborne;
        private int LichbornePercent => settings.LichbornePercent;
        private int DeathStrikeEmergencyPercent => settings.DeathStrikeEmergencyPercent;
        private int DeathAndDecayCount => settings.DeathAndDecayCount;
        private bool UseDeathAndDecay => settings.UseDeathAndDecay;
        private bool UseSummonGargoyle => settings.UseSummonGargoyle;
        private DeathKnightSettings settings => SettingsManager.Instance.DeathKnight;
        public short Spec => 3;
        public UnitClass PlayerClass => UnitClass.DeathKnight;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]DeathKnight-Unholy ";
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
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;

            if (inCombatEnemies.Any(u => (u.IsCasting || u.ChannelingSpellID != 0) &&
                u.TargetGUID == player.WowGuid &&
                UseAntiMagicShell) && IsSpellReady("Anti-Magic Shell"))
                return CastAtPlayerLocation("Anti-Magic Shell", isHarmfulSpell: false);
            if (!player.HasActivePet && IsSpellReady("Raise Dead"))
                return CastAtPlayerLocation("Raise Dead", isHarmfulSpell: false);
            if (player.HealthPercent < IceboundFortitudePercent && UseIceboundFortitude && IsSpellReady("Icebound Fortitude"))
                return CastAtPlayer("Icebound Fortitude");
            if (UseLichborne && (player.HealthPercent < LichbornePercent || !player.HasFullControl || player.IsCCed) && IsSpellReady("Lichborne"))
                return CastAtPlayer("Lichborne");
            if (player.HealthPercent < DeathStrikeEmergencyPercent && player.HasAura("Lichborne") && IsSpellReady("Death Coil"))
                return CastAtPlayer("Death Coil");

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
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 12);
                if (nearbyEnemies.Count >= DeathAndDecayCount)
                {
                    if (UseSummonGargoyle && IsSpellReady("Summon Gargoyle"))
                        return CastAtPlayerLocation("Summon Gargoyle", isHarmfulSpell: true);

                    if (targetedEnemy != null
                        && IsSpellReady("Pestilence")
                        && targetedEnemy.HasDeBuff("Frost Fever") && targetedEnemy.HasDeBuff("Blood Plague")
                        && targetedEnemy.GetNearbyInCombatEnemies(10).Where(e => e.HasDeBuff("Frost Fever") && e.HasDeBuff("Blood Plague")).Any())
                        return CastAtTarget("Pestilence", facing: SpellFacingFlags.None);

                    if (om.PlayerPet != null && !om.PlayerPet.IsDead && !om.PlayerPet.HasAura("Dark Transformation") && IsSpellReady("Dark Transformation"))
                        return CastAtPlayerLocation("Dark Transformation");

                    if (UseDeathAndDecay && IsSpellReady("Death and Decay"))
                    {
                        var AoELocation = GetBestAoELocation(inCombatEnemies, 10f, out int numEnemiesInAoE);
                        if (numEnemiesInAoE >= DeathAndDecayCount)
                            return CastAtGround(AoELocation, "Death and Decay");
                    }

                    var ccCandidates = nearbyEnemies.Where(e => e.HealthPercent > 25 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root));
                    foreach (var ccCandidate in ccCandidates)
                    {
                        if (IsSpellReady("Strangulate") && !ccCandidate.HasDeBuff("Strangulate"))
                            return CastAtUnit(ccCandidate, "Strangulate");
                    }

                    if (targetedEnemy != null && IsSpellReady("Scourge Strike") && (player.UnholyRuneCount == 2 || player.DeathRuneCount >= 2))
                        return CastAtTarget("Scourge Strike");
                    if (player.BloodRuneCount == 2 && player.FrostRuneCount == 2 && IsSpellReady("Blood Boil"))
                        return CastAtPlayerLocation("Blood Boil");
                    if (targetedEnemy != null && player.BloodRuneCount == 2 && player.FrostRuneCount == 2 && IsSpellReady("Icy Touch"))
                        return CastAtTarget("Icy Touch");
                    if (targetedEnemy != null && (player.Power >= 800 || player.HasAura("Sudden Doom")))
                        return CastAtTarget("Death Coil");
                    if (targetedEnemy != null && IsSpellReady("Scourge Strike"))
                        return CastAtTarget("Scourge Strike");
                    if (IsSpellReady("Blood Boil"))
                        return CastAtPlayerLocation("Blood Boil");
                    if (targetedEnemy != null && IsSpellReady("Icy Touch"))
                        return CastAtTarget("Icy Touch");
                    if (IsSpellReady("Horn of Winter"))
                        return CastAtPlayerLocation("Horn of Winter");
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

                if (UseDeathAndDecay && (player.UnholyRuneCount == 2 || player.DeathRuneCount >= 2) && IsSpellReady("Death and Decay"))
                {
                    return CastAtGround(targetedEnemy.Position, "Death and Decay");
                }
                if ((player.UnholyRuneCount == 2 || player.DeathRuneCount >= 2) && IsSpellReady("Scourge Strike"))
                    return CastAtTarget("Scourge Strike");
                if (player.BloodRuneCount == 2 && player.FrostRuneCount == 2 && IsSpellReady("Festering Strike"))
                    return CastAtTarget("Festering Strike");
                if (IsSpellReady("Death Coil") && (player.Power >= 800 || player.HasAura("Sudden Doom")))
                    return CastAtTarget("Death Coil");
                if (UseDeathAndDecay && IsSpellReady("Death and Decay"))
                {
                    return CastAtGround(targetedEnemy.Position, "Death and Decay");
                }
                if (IsSpellReady("Scourge Strike"))
                    return CastAtTarget("Scourge Strike");
                if (IsSpellReady("Festering Strike"))
                    return CastAtTarget("Festering Strike");
                if (IsSpellReady("Death Coil"))
                    return CastAtTarget("Death Coil");
                if (IsSpellReady("Horn of Winter"))
                    return CastAtPlayerLocation("Horn of Winter");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
