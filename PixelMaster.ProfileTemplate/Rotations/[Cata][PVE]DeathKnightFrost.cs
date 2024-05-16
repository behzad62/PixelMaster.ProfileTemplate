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
    public class DeathKinghtFrost : IPMRotation
    {
        private bool UseAntiMagicShell => settings.UseAntiMagicShell;
        private bool UseIceboundFortitude => settings.UseIceboundFortitude;
        private int IceboundFortitudePercent => settings.IceboundFortitudePercent;
        private bool UseLichborne => settings.UseLichborne;
        private int LichbornePercent => settings.LichbornePercent;
        private int DeathStrikeEmergencyPercent => settings.DeathStrikeEmergencyPercent;
        private int DeathAndDecayCount => settings.DeathAndDecayCount;
        private bool UseRaiseDead => settings.UseRaiseDead;
        private bool UsePillarOfFrost => settings.UsePillarOfFrost;
        private bool UseDeathAndDecay => settings.UseDeathAndDecay;
        private bool UseEmpowerRuneWeapon => settings.UseEmpowerRuneWeapon;

        private DeathKnightSettings settings => SettingsManager.Instance.DeathKnight;
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.DeathKnight;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]DeathKnight-Frost";
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
                UseAntiMagicShell))
                return CastAtPlayerLocation("Anti-Magic Shell", isHarmfulSpell: false);
            //if (!player.HasActivePet && IsSpellReady("Raise Dead"))
            //    return CastAtPlayerLocation("Raise Dead", isHarmfulSpell: false);
            if (player.HealthPercent < IceboundFortitudePercent && UseIceboundFortitude && IsSpellReady("Icebound Fortitude"))
                return CastAtPlayer("Icebound Fortitude");
            if (UseLichborne && (player.HealthPercent < LichbornePercent || !player.HasFullControl || player.CCs.HasFlag(ControlConditions.CC)) && IsSpellReady("Lichborne"))
                return CastAtPlayer("Lichborne");
            if (player.HealthPercent < DeathStrikeEmergencyPercent && player.HasAura("Lichborne") && IsSpellReady("Death Coil"))
                return CastAtPlayer("Death Coil");

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
            if (UsePillarOfFrost && IsSpellReady("Pillar of Frost"))
                return CastAtPlayer("Pillar of Frost");
            if (UseRaiseDead && targetedEnemy != null && targetedEnemy.IsElite && !player.HasActivePet && IsSpellReady("Raise Dead") && player.HasAura("Pillar of Frost"))
                return CastAtPlayerLocation("Raise Dead", isHarmfulSpell: false);
            if (UseEmpowerRuneWeapon && targetedEnemy != null && targetedEnemy.IsElite && player.UnholyRuneCount == 0 && player.FrostRuneCount == 0 && player.DeathRuneCount == 0 && !IsSpellReady("Frost Strike"))
                return CastAtPlayerLocation("Empower Rune Weapon", isHarmfulSpell: false);
            //AoE handling
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 12);
                if (nearbyEnemies.Count >= DeathAndDecayCount)
                {

                    if (targetedEnemy != null
                        && (player.FrostRuneCount == 2 || player.DeathRuneCount == 2)
                        && IsSpellReady("Howling Blast"))
                        return CastAtTarget("Howling Blast");
                    if (targetedEnemy != null && IsSpellReady("Plague Strike") && player.UnholyRuneCount == 2)
                        return CastAtTarget("Plague Strike");
                    if (targetedEnemy != null && player.Power == player.MaxPower && IsSpellReady("Frost Strike"))
                        return CastAtTarget("Frost Strike");
                    if (IsSpellReady("Horn of Winter"))
                        return CastAtPlayerLocation("Horn of Winter");
                    //if (om.PlayerPet != null && !om.PlayerPet.IsDead && !om.PlayerPet.HasAura("Dark Transformation") && IsSpellReady("Dark Transformation"))
                    //    return CastAtPlayerLocation("Dark Transformation");

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
                    if (targetedEnemy != null && IsSpellReady("Plague Strike"))
                        return CastAtTarget("Plague Strike");
                    if (targetedEnemy != null && IsSpellReady("Frost Strike"))
                        return CastAtTarget("Frost Strike");

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
                if (!targetedEnemy.HasDeBuff("Frost Fever") && IsSpellReady("Howling Blast"))
                    return CastAtTarget("Howling Blast");
                if (!targetedEnemy.HasDeBuff("Frost Fever") && IsSpellReady("Icy Touch"))
                    return CastAtTarget("Icy Touch");
                if (!targetedEnemy.HasDeBuff("Blood Plague") && IsSpellReady("Plague Strike"))
                    return CastAtTarget("Plague Strike");
                if (!PlayerLearnedSpell("Obliterate") && IsSpellReady("Blood Strike"))
                    return CastAtTarget("Blood Strike");
                if (IsSpellReady("Obliterate"))
                    return CastAtTarget("Obliterate");
                if (IsSpellReady("Frost Strike"))
                    return CastAtTarget("Frost Strike");
                if (player.HasAura("Freezing Fog") && IsSpellReady("Howling Blast"))
                    return CastAtTarget("Howling Blast");
                if (IsSpellReady("Horn of Winter"))
                    return CastAtPlayerLocation("Horn of Winter");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
