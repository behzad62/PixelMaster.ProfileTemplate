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
    public class PriestDisc : IPMRotation
    {
        private PriestSettings settings => SettingsManager.Instance.Priest;
        public short Spec => 1;
        public UnitClass PlayerClass => UnitClass.Priest;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.Healer;
        public string Name => "[Cata][PvE]Prist-Disc";
        public string Author => "PixelMaster";
        public string Description => "General PvE";
        public List<RotationMode> AvailableRotations => new() { RotationMode.Auto, RotationMode.Normal, RotationMode.Instance, RotationMode.PvP };
        public RotationMode PreferredMode { get; set; } = RotationMode.Auto;

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            if (targetedEnemy != null)
            {
                if (settings.UseShieldPrePull && !player.HasAura("Weakened Soul") && !PlayerLearnedSpell("Mind Spike") && IsSpellReady("Power Word: Shield"))
                    return CastAtPlayer("Power Word: Shield");
                if (IsSpellReadyOrCasting("Holy Word: Chastise"))
                    return CastAtTarget("Holy Word: Chastise");
                if (IsSpellReadyOrCasting("Penance"))
                    return CastAtTarget("Penance");
                if (IsSpellReadyOrCasting("Holy Fire"))
                    return CastAtTarget("Holy Fire");
                if (IsSpellReadyOrCasting("Smite"))
                    return CastAtPlayer("Smite");
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
            //List<WowUnit>? inCombatEnemies = inCombatEnemies = om.InCombatEnemies;
            var group = om.PlayerGroup;
            var isInBG = ObjectManager.Instance.CurrentMap.MapType == MapType.Battleground;

            if (targetedEnemy != null && player.PowerPercent <= settings.ShadowfiendMana && IsSpellReady("Shadowfiend"))
                return CastAtTarget("Shadowfiend", facing: SpellFacingFlags.None);
            if (player.HealthPercent < 50 && IsSpellReady("Desperate Prayer"))
                return CastAtPlayer("Desperate Prayer");
            if (player.PowerPercent <= 15 && !IsSpellReady("Shadowfiend") && IsSpellReadyOrCasting("Hymn of Hope"))
                return CastAtPlayer("Hymn of Hope");
            if (group != null && group.Members.Values.Count(p => p.IsGroupMemberOnline && p.HealthPercent <= settings.DivineHymnHealth) >= settings.DivineHymnCount && IsSpellReadyOrCasting("Divine Hymn"))
                CastAtPlayerLocation("Divine Hymn", isHarmfulSpell: false);
            if (IsSpellReady("Archangel") && player.AuraStacks("Evangelism") >= 4)
                return CastAtPlayerLocation("Archangel", isHarmfulSpell:false);

            if (player.HealthPercent <= 35)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
            }

            if (group != null)
            {
                var groupTanks = group.Tanks;
                var healTargets = group.Members.Values.Where(FilterUnits).ToList();
                if (healTargets.Any())
                {
                    healTargets.Sort(new HealPriority() { IsInBG = isInBG, Tanks = groupTanks });
                    var healTarget = healTargets.First();
                    if (healTarget.IsInCombat && IsSpellReady("Power Word: Shield") && !healTarget.HasAura("Weakened Soul"))
                        return CastAtUnit(healTarget, "Power Word: Shield", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    if (IsSpellReadyOrCasting("Prayer of Healing")
                        && group.GetMembersInRaidGroup(healTarget.RaidGroupIndex).Count(m => m.HealthPercent <= settings.PrayerOfHealing && Vector3.DistanceSquared(m.Position, healTarget.Position) < 30 * 30) >= settings.PrayerOfHealingCount)
                    {
                        if(IsSpellReady("Inner Focus"))
                            return CastAtPlayerLocation("Inner Focus", isHarmfulSpell: false);
                        return CastAtUnit(healTarget, "Prayer of Healing", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    }
                    if (healTarget.IsInCombat && IsSpellReady("Pain Supression") && healTarget.HealthPercent < settings.PainSuppression)
                        return CastAtUnit(healTarget, "Pain Supression", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    if (IsSpellReadyOrCasting("Penance") && healTarget.HealthPercent < settings.Penance)
                        return CastAtUnit(healTarget, "Penance", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    if (healTarget.HealthPercent <= settings.HolyFlashHeal && IsSpellReadyOrCasting("Flash Heal"))
                    {
                        if (IsSpellReady("Inner Focus"))
                            return CastAtPlayerLocation("Inner Focus", isHarmfulSpell: false);
                        return CastAtUnit(healTarget, "Flash Heal", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    }
                    if (healTarget.HealthPercent <= settings.BindingHealThem && player.HealthPercent < settings.BindingHealMe && !healTarget.IsSameAs(player) && IsSpellReadyOrCasting("Binding Heal"))
                        return CastAtUnit(healTarget, "Binding Heal", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    if (healTarget.HealthPercent <= settings.HolyGreaterHeal && IsSpellReadyOrCasting("Greater Heal"))
                    {
                        if (IsSpellReady("Inner Focus"))
                            return CastAtPlayerLocation("Inner Focus", isHarmfulSpell: false);
                        return CastAtUnit(healTarget, "Greater Heal", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    }
                    if (healTarget.HealthPercent <= settings.HolyHeal && IsSpellReadyOrCasting("Heal"))
                        return CastAtUnit(healTarget, "Heal", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    if (healTarget.Health < settings.Renew && (healTarget.IsPlayer || healTarget.IsLocalPlayer) && !healTarget.HasAura("Renew", true) && IsSpellReady("Renew"))
                        return CastAtUnit(healTarget, "Renew", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                    if (healTarget.HealthPercent < 90 && IsSpellReady("Prayer of Mending") && !healTarget.HasAura("Prayer of Mending", true))
                        return CastAtUnit(healTarget, "Prayer of Mending", isHarmfulSpell: false, facing: SpellFacingFlags.None);
                }
                else
                {
                    if (targetedEnemy != null && IsSpellReadyOrCasting("Smite") && player.PowerPercent > 50)
                        return CastAtTarget("Smite");
                }
            }
            if (player.IsFleeingFromTheFight)
            {

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
            //if (inCombatEnemies.Count > 1)
            //{
            //    if (dynamicSettings.AllowBurstOnMultipleEnemies && inCombatEnemies.Count > 2)
            //    {

            //    }
            //}

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (om.CurrentMap.MapType == MapType.Normal || player.PowerPercent > 70 || group is null)
                {
                    if (IsSpellReady("Shadow Word: Pain") && !targetedEnemy.HasAura("Shadow Word: Pain", true) && targetedEnemy.HealthPercent > 15)
                        return CastAtTarget("Shadow Word: Pain");
                    if (IsSpellReadyOrCasting("Penance"))
                        return CastAtTarget("Penance");
                    if (IsSpellReadyOrCasting("Holy Fire"))
                        return CastAtTarget("Holy Fire");
                    if (IsSpellReadyOrCasting("Smite"))
                        return CastAtTarget("Smite");
                }
            }
            return null;
        }

        private bool FilterUnits(WowUnit? unit)
        {
            return unit is not null && (unit.IsPlayer || unit.IsLocalPlayer)
                && !unit.IsEnemy && !unit.IsDead && unit.HealthPercent < SettingsManager.Instance.General.IgnoreHealTargetsAboveHealth
                && (unit.DistanceSquaredToPlayer <= 40 * 40 || (ObjectManager.Instance.CurrentMap.MapType != MapType.Battleground && !SettingsManager.Instance.General.DisableAllMovement));
        }
        private WowUnit? GetHighestHealPriority(IEnumerable<WowUnit> units, bool isInBG, IEnumerable<WowUnit> tanks)
        {
            float highestScore = float.MinValue;
            WowUnit? healPrio = null;
            var player = ObjectManager.Instance.Player;
            foreach (var unit in units)
            {
                float unitScore = GetHealScore(unit, isInBG, tanks);
                if (unitScore > highestScore)
                {
                    highestScore = unitScore;
                    healPrio = unit;
                }
            }

            return healPrio;
        }

        internal static float GetHealScore(WowUnit unit, bool inBg, IEnumerable<WowUnit> tanks)
        {
            float unitScore = 500f;
            // The more health they have, the lower the score.
            // This should give -500 for units at 100%
            // And -50 for units at 10%
            unitScore -= (float)(unit.HealthPercent * 5);
            // If they're out of range, give them a bit lower score.
            if (unit.DistanceSquaredToPlayer > 40 * 40)
            {
                unitScore -= 50f;
            }
            //If unit is not a player unit, then it has lower prio
            if (!unit.IsPlayer && !unit.IsLocalPlayer)
            {
                unitScore -= 200f;
            }
            // If they're out of LOS, again, lower score!
            if (!unit.IsInPlayerSpellLineOfSight)
            {
                unitScore -= 100f;
            }
            // Give tanks more weight. If the tank dies, we all die. KEEP HIM UP.
            if (tanks.Any(t => t.IsSameAs(unit)) && unit.HealthPercent != 100 &&
                // Ignore giving more weight to the tank if we have Beacon of Light on it.
                !unit.Auras.Any(a => a.Spell != null && a.Spell.Name == "Beacon of Light" && a.SourceGUID == ObjectManager.Instance.Player.WowGuid))
            {
                unitScore += 100f;
            }

            // Give flag carriers more weight in battlegrounds. We need to keep them alive!
            if (inBg && unit.Auras.Any(a => a.Spell != null && a.Spell.Name.ToLowerInvariant().Contains("flag")))
            {
                unitScore += 100f;
            }

            return unitScore;
        }

        class HealPriority : Comparer<WowUnit>
        {
            public IEnumerable<WowUnit> Tanks { get; init; }
            public bool IsInBG { get; init; }
            public override int Compare(WowUnit? x, WowUnit? y)
            {
                if (x == null || y == null) return 0;
                if (x == null)
                    return 1;
                if (y == null)
                    return -1;
                return GetHealScore(y, IsInBG, Tanks).CompareTo(GetHealScore(x, IsInBG, Tanks));
            }
        }
    }
}
