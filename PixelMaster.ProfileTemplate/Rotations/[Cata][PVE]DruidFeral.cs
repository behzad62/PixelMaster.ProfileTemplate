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
    public class DruidFeral : IPMRotation
    {
        private DruidSettings settings => SettingsManager.Instance.Druid;
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Druid;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]Druid-Feral";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;

            if (player.AuraStacks("Lightning Shield", true) < 2 && IsSpellReady("Lightning Shield"))
                return CastAtPlayerLocation("Lightning Shield", isHarmfulSpell: false);
            if (targetedEnemy != null)
            {
                if(player.Level < 20)
                {
                    if (IsSpellReady("Moonfire"))
                        return CastAtTarget("Moonfire");
                    if (IsSpellReadyOrCasting("Wrath"))
                        return CastAtTarget("Wrath");
                }
                if(player.Level < 46)
                {
                    if(player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                        return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                    if (!player.IsStealthed && IsSpellReady("Prowl") && targetedEnemy.DistanceSquaredToPlayer < 30 * 30)
                        return CastAtPlayerLocation("Prowl", isHarmfulSpell: false);
                    if (IsSpellReady("Feral Charge (Cat)", "Cat Form"))
                        return CastAtTarget("Feral Charge (Cat)");
                    if(targetedEnemy.IsPositionBehind(player.Position) && IsSpellReady("Ravage"))
                        return CastAtTarget("Ravage");
                    if (IsSpellReady("Pounce"))
                        return CastAtTarget("Pounce");
                    if (IsSpellReady("Mangle"))
                        return CastAtTarget("Mangle");
                }

                if (player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                    return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                if (!player.IsStealthed && IsSpellReady("Prowl") && targetedEnemy.DistanceSquaredToPlayer < 30 * 30)
                    return CastAtPlayerLocation("Prowl", isHarmfulSpell: false);
                if (IsSpellReady("Feral Charge (Cat)"))
                    return CastAtTarget("Feral Charge (Cat)");
                if(targetedEnemy.DistanceSquaredToPlayer > 8 * 8 && !player.HasAura("Stampeding Roar") && !IsSpellReady("Feral Charge (Cat)") && IsSpellReady("Dash"))
                    return CastAtPlayerLocation("Dash", isHarmfulSpell: false);
                if (targetedEnemy.DistanceSquaredToPlayer > 8 * 8 && !player.HasAura("Stampeding Roar") && IsSpellReady("Feral Charge (Cat)") && !IsSpellReady("Dash"))
                    return CastAtPlayerLocation("Stampeding Roar(Cat Form)", isHarmfulSpell: false);
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

            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }

            if (!settings.NoHealBalanceAndFeral)
            {
                if(player.HealthPercent <= settings.NonRestoRejuvenation && !player.HasAura("Rejuvenation", true) && IsSpellReady("Rejuvenation"))
                    return CastAtPlayer("Rejuvenation");
                if (player.HealthPercent <= settings.NonRestoRegrowth && !player.HasAura("Regrowth", true) && IsSpellReadyOrCasting("Regrowth"))
                    return CastAtPlayer("Regrowth");
                if (player.HealthPercent <= settings.NonRestoHealingTouch && IsSpellReadyOrCasting("Healing Touch"))
                    return CastAtPlayer("Healing Touch");
            }

            if (player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);

            if(player.HealthPercent <= settings.FeralBarkskin && IsSpellReady("Barkskin"))
                return CastAtPlayer("Barkskin");
            if (player.HealthPercent <= settings.SurvivalInstinctsHealth && IsSpellReady("Survival Instincts"))
                return CastAtPlayer("Survival Instincts");

            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Nature's Grasp") && !player.HasAura("Nature's Grasp", true))
                    return CastAtPlayerLocation("Nature's Grasp");
                if (IsSpellReady("Dash"))
                    return CastAtPlayerLocation("Dash");
                return null;
            }
            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies;
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count >= 3)
                {
                    if (targetedEnemy != null && (player.SecondaryPower == 5 || player.SecondaryPower >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite"))
                        return CastAtTarget("Ferocious Bite");
                    if (IsSpellReady("Swipe (Cat)"))
                        return CastAtPlayerLocation("Swipe (Cat)", isHarmfulSpell: true);
                    if (targetedEnemy != null && IsSpellReady("Mangle"))
                        return CastAtTarget("Mangle");
                    if (player.HealthPercent < 50)
                    {
                        if (IsSpellReady("Dire Bear Form") && player.Form != ShapeshiftForm.DireBearForm)
                            return CastAtPlayer("Dire Bear Form");
                        if (IsSpellReady("Bear Form") && player.Form != ShapeshiftForm.BearForm && player.Form != ShapeshiftForm.DireBearForm)
                            return CastAtPlayer("Bear Form");
                        if (IsSpellReady("Demoralizing Roar") && nearbyEnemies.Any(e => !e.HasDeBuff("Demoralizing Roar")))
                            return CastAtPlayerLocation("Demoralizing Roar");
                        if (IsSpellReady("Swipe (Bear)"))
                            return CastAtPlayerLocation("Swipe (Bear)", isHarmfulSpell: true);
                    }
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting && targetedEnemy.DistanceSquaredToPlayer < 10 * 10)
                {
                    if (IsSpellReady("Bash"))
                        return CastAtTarget("Bash");
                    if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Skull Bash (Cat)"))
                        return CastAtTarget("Skull Bash (Cat)");
                    if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Skull Bash (Bear)"))
                        return CastAtTarget("Skull Bash (Bear)");
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp") && targetedEnemy.DistanceSquaredToPlayer < 8 * 8)
                        return CastAtPlayerLocation("War Stomp");
                }

                if (player.Level < 20)
                {
                    if ((player.SecondaryPower == 5 || player.SecondaryPower >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite"))
                        return CastAtTarget("Ferocious Bite");
                    if (targetedEnemy.IsElite && IsSpellReady("Rake"))
                        return CastAtTarget("Rake");
                    if (IsSpellReady("Mangle"))
                        return CastAtTarget("Mangle");
                }
                else if (player.Level < 46) 
                {
                    if ((player.SecondaryPower == 5 || player.SecondaryPower >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite"))
                        return CastAtTarget("Ferocious Bite");
                    if(player.HasAura("Stampede") && IsSpellReady("Ravage!"))
                        return CastAtTarget("Ravage!");
                    if (targetedEnemy.IsElite && IsSpellReady("Rake"))
                        return CastAtTarget("Rake");
                    if (IsSpellReady("Mangle"))
                        return CastAtTarget("Mangle");
                }

                if (!player.HasAura("Tiger's Fury") && IsSpellReady("Tiger's Fury"))
                    return CastAtPlayerLocation("Tiger's Fury", isHarmfulSpell: false);
                if ((player.SecondaryPower == 5 || player.SecondaryPower >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite"))
                    return CastAtTarget("Ferocious Bite");
                if(!targetedEnemy.IsTargetingPlayer && IsSpellReady("Shred"))
                    return CastAtTarget("Shred", facing: SpellFacingFlags.BehindAndFaceTarget);
                if (player.HasAura("Stampede") && IsSpellReady("Ravage!"))
                    return CastAtTarget("Ravage!");
                if (targetedEnemy.IsElite && IsSpellReady("Rake"))
                    return CastAtTarget("Rake");
                if (targetedEnemy.IsPositionBehind(player.Position) && IsSpellReady("Mangle"))
                    return CastAtTarget("Mangle");

                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
        private bool IsTotemLanded(string totemName)
        {
            return ObjectManager.Instance.PlayerTotems.Where(totem => totem.Name == totemName).Any();
        }
    }
}
