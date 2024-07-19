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

            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsFlying)
                {
                    if (IsSpellReady("Moonfire"))
                        return CastAtTarget("Moonfire");
                    if (IsSpellReadyOrCasting("Wrath"))
                        return CastAtTarget("Wrath");
                }
                if(player.Level < 20)
                {
                    if (IsSpellReady("Feral Charge", "Bear Form"))
                        return CastAtTarget("Feral Charge", "Bear Form");
                    if (IsSpellReady("Moonfire"))
                        return CastAtTarget("Moonfire");
                    if (IsSpellReadyOrCasting("Wrath"))
                        return CastAtTarget("Wrath");
                }
                if(player.Level < 46)
                {
                    if(player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                        return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                    if (!player.IsStealthed && IsSpellReady("Prowl") && targetedEnemy.DistanceSquaredToPlayer < 40 * 40)
                        return CastAtPlayerLocation("Prowl", isHarmfulSpell: false);
                    if (IsSpellReady("Feral Charge (Cat)", "Cat Form"))
                        return CastAtTarget("Feral Charge (Cat)");
                    if(player.IsBehindTarget(targetedEnemy) && IsSpellReady("Ravage"))
                        return CastAtTarget("Ravage", facing: SpellFacingFlags.BehindAndFaceTarget);
                    if (IsSpellReady("Pounce"))
                        return CastAtTarget("Pounce");
                    if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Mangle", "Bear Form"))
                        return CastAtTarget("Mangle", "Bear Form");
                    if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Mangle", "Cat Form"))
                        return CastAtTarget("Mangle", "Cat Form");
                }

                if (player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                    return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                if (targetedEnemy.Level >= player.Level - 4 && !player.IsStealthed && IsSpellReady("Prowl", "Cat Form") && targetedEnemy.DistanceSquaredToPlayer < 30 * 30)
                    return CastAtPlayerLocation("Prowl", "Cat Form", isHarmfulSpell: false);
                if (IsSpellReady("Feral Charge", "Cat Form"))
                    return CastAtTarget("Feral Charge", "Cat Form");
                if(targetedEnemy.DistanceSquaredToPlayer > 8 * 8 && !player.HasAura("Stampeding Roar") && !IsSpellReady("Feral Charge", "Cat Form") && IsSpellReady("Dash", "Cat Form"))
                    return CastAtPlayerLocation("Dash", "Cat Form", isHarmfulSpell: false);
                if (targetedEnemy.DistanceSquaredToPlayer > 8 * 8 && IsSpellReady("Stampeding Roar", "Cat Form") && !IsSpellReady("Feral Charge", "Cat Form") && !IsSpellReady("Dash") && !player.HasAura("Dash"))
                    return CastAtPlayerLocation("Stampeding Roar", "Cat Form", isHarmfulSpell: false);
                if (player.IsBehindTarget(targetedEnemy) && IsSpellReady("Ravage"))
                    return CastAtTarget("Ravage", facing: SpellFacingFlags.BehindAndFaceTarget);
                if (IsSpellReady("Pounce"))
                    return CastAtTarget("Pounce");
                if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Mangle", "Bear Form"))
                    return CastAtTarget("Mangle", "Bear Form");
                if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Mangle", "Cat Form"))
                    return CastAtTarget("Mangle", "Cat Form");
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
                if (!om.CurrentMap.IsDungeon)
                {
                    var healingPot = inv.GetHealingPotion();
                    if (healingPot != null)
                        return UseItem(healingPot);
                }
            }

            if (!settings.NoHealBalanceAndFeral || player.Form != ShapeshiftForm.MoonkinForm && player.Form != ShapeshiftForm.Cat && player.Form != ShapeshiftForm.BearForm && player.Form != ShapeshiftForm.DireBearForm)
            {
                if(player.HealthPercent <= settings.NonRestoRejuvenation && !player.HasAura("Rejuvenation", true) && IsSpellReady("Rejuvenation") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInMeleeRange) <= 4)
                    return CastAtPlayer("Rejuvenation");
                if (player.HealthPercent <= settings.NonRestoRegrowth && !player.HasAura("Regrowth", true) && IsSpellReadyOrCasting("Regrowth") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInMeleeRange) < 4)
                    return CastAtPlayer("Regrowth");
                if (player.HealthPercent <= settings.NonRestoHealingTouch && IsSpellReadyOrCasting("Healing Touch") && om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInMeleeRange) < 3)
                    return CastAtPlayer("Healing Touch");
            }

            if(player.HealthPercent <= settings.FeralBarkskin && IsSpellReady("Barkskin"))
                return CastAtPlayer("Barkskin");
            if (player.HealthPercent <= settings.SurvivalInstinctsHealth && (player.Form == ShapeshiftForm.Cat || player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Survival Instincts"))
                return CastAtPlayer("Survival Instincts");

            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Nature's Grasp") && !player.HasAura("Nature's Grasp", true))
                    return CastAtPlayerLocation("Nature's Grasp");
                if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Dash", "Cat Form"))
                    return CastAtPlayerLocation("Dash", "Cat Form");
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
                    if((player.Form == ShapeshiftForm.Cat || player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Berserk"))
                        return CastAtPlayerLocation("Berserk", isHarmfulSpell: false);
                    if (IsSpellReady("Thorns") && !player.HasBuff("Thorns"))
                        return CastAtPlayerLocation("Thorns");
                    if(nearbyEnemies.Count >= 6)
                    {
                        if (player.HealthPercent > 40)
                        {
                            if (player.Energy > 50 && player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                                return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                            if (player.Form == ShapeshiftForm.Cat && player.Energy >= 90 && IsSpellReady("Berserk"))
                                return CastAtPlayerLocation("Berserk", isHarmfulSpell: false);
                            if (player.Form == ShapeshiftForm.Cat && !player.HasAura("Tiger's Fury") && IsSpellReady("Tiger's Fury", "Cat Form") && player.Energy <= 40)
                                return CastAtPlayerLocation("Tiger's Fury", "Cat Form", isHarmfulSpell: false);
                            if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Swipe", "Cat Form"))
                                return CastAtPlayerLocation("Swipe", "Cat Form", isHarmfulSpell: true);
                        }
                        else
                        {
                            if (IsSpellReady("Dire Bear Form") && player.Form != ShapeshiftForm.DireBearForm)
                                return CastAtPlayerLocation("Dire Bear Form", isHarmfulSpell: true);
                            if (IsSpellReady("Bear Form") && player.Form != ShapeshiftForm.BearForm && player.Form != ShapeshiftForm.DireBearForm)
                                return CastAtPlayerLocation("Bear Form", isHarmfulSpell: true);
                            if ((player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Berserk"))
                                return CastAtPlayerLocation("Berserk", isHarmfulSpell: false);
                            if ((player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Thrash", "Bear Form") && nearbyEnemies.Count(e => e.HasDeBuff("Thrash")) < 3)
                                return CastAtPlayerLocation("Thrash", "Bear Form", isHarmfulSpell: true);
                            if ((player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Swipe", "Bear Form"))
                                return CastAtPlayerLocation("Swipe", "Bear Form", isHarmfulSpell: true);
                        }
                    }
                    if (player.HealthPercent < 40)
                    {
                        if (IsSpellReady("Dire Bear Form") && player.Form != ShapeshiftForm.DireBearForm)
                            return CastAtPlayerLocation("Dire Bear Form", isHarmfulSpell: true);
                        if (IsSpellReady("Bear Form") && player.Form != ShapeshiftForm.BearForm && player.Form != ShapeshiftForm.DireBearForm)
                            return CastAtPlayerLocation("Bear Form", isHarmfulSpell: true);
                        if (IsSpellReady("Demoralizing Roar", "Bear Form") && nearbyEnemies.Any(e => !e.HasDeBuff("Demoralizing Roar")))
                            return CastAtPlayerLocation("Demoralizing Roar", "Bear Form");
                        if ((player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Thrash", "Bear Form") && !nearbyEnemies.Any(e => e.HasDeBuff("Thrash")))
                            return CastAtPlayerLocation("Thrash", "Bear Form", isHarmfulSpell: true);
                        if ((player.Form == ShapeshiftForm.BearForm || player.Form == ShapeshiftForm.DireBearForm) && IsSpellReady("Swipe", "Bear Form"))
                            return CastAtPlayerLocation("Swipe", "Bear Form", isHarmfulSpell: true);
                    }
                    else
                    {
                        if (player.Form != ShapeshiftForm.Cat && IsSpellReady("Cat Form"))
                            return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                        if (IsSpellReady("Rake", "Cat Form"))
                        {
                            var rakeTarget = nearbyEnemies.FirstOrDefault(e => !e.HasAura("Rake", true));
                            if (rakeTarget != null)
                                return CastAtUnit(rakeTarget, "Rake", "Cat Form");
                        }
                        if (player.Form == ShapeshiftForm.Cat && targetedEnemy != null && (player.ComboPoints == 5 || player.ComboPoints >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite", "Cat Form"))
                            return CastAtTarget("Ferocious Bite", "Cat Form");
                        if (targetedEnemy != null && IsSpellReady("Mangle", "Cat Form"))
                            return CastAtTarget("Mangle", "Cat Form");
                    }
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (player.Form != ShapeshiftForm.Cat)
                {
                    if (IsSpellReady("Cat Form"))
                        return CastAtPlayerLocation("Cat Form", isHarmfulSpell: false);
                    if (IsSpellReady("Dire Bear Form") && player.Form != ShapeshiftForm.DireBearForm)
                        return CastAtPlayerLocation("Dire Bear Form", isHarmfulSpell: true);
                    if (IsSpellReady("Bear Form") && player.Form != ShapeshiftForm.BearForm && player.Form != ShapeshiftForm.DireBearForm)
                        return CastAtPlayerLocation("Bear Form", isHarmfulSpell: true);
                }
                if (targetedEnemy.IsCasting && targetedEnemy.DistanceSquaredToPlayer < 10 * 10)
                {
                    if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Bash", "Bear Form"))
                        return CastAtTarget("Bash", "Bear Form");
                    if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Skull Bash", "Cat Form"))
                        return CastAtTarget("Skull Bash", "Cat Form");
                    if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Skull Bash", "Bear Form"))
                        return CastAtTarget("Skull Bash", "Bear Form");
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp") && targetedEnemy.DistanceSquaredToPlayer < 8 * 8)
                        return CastAtPlayerLocation("War Stomp");
                }

                if (player.Level < 20)
                {
                    if ((player.ComboPoints == 5 || player.ComboPoints >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite", "Cat Form"))
                        return CastAtTarget("Ferocious Bite", "Cat Form");
                    if (targetedEnemy.IsElite && IsSpellReady("Rake", "Cat Form"))
                        return CastAtTarget("Rake", "Cat Form");
                    if (player.Form == ShapeshiftForm.BearForm && IsSpellReady("Mangle", "Bear Form"))
                        return CastAtTarget("Mangle", "Bear Form");
                    if (player.Form == ShapeshiftForm.Cat && IsSpellReady("Mangle", "Cat Form"))
                        return CastAtTarget("Mangle", "Cat Form");
                }
                else if (player.Level < 46) 
                {
                    if ((player.ComboPoints == 5 || player.ComboPoints >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite", "Cat Form"))
                        return CastAtTarget("Ferocious Bite" , "Cat Form");
                    if(player.HasAura("Stampede") && IsSpellReady("Ravage", "Cat Form"))
                        return CastAtTarget("Ravage", "Cat Form");
                    if (targetedEnemy.IsElite && IsSpellReady("Rake", "Cat Form"))
                        return CastAtTarget("Rake", "Cat Form");
                    if (IsSpellReady("Mangle", "Cat Form"))
                        return CastAtTarget("Mangle", "Cat Form");
                }

                if (!player.HasAura("Tiger's Fury") && IsSpellReady("Tiger's Fury", "Cat Form") && player.Energy <= 40)
                    return CastAtPlayerLocation("Tiger's Fury", "Cat Form", isHarmfulSpell: false);

                if (targetedEnemy.IsElite)
                {
                    if (player.Energy >= 90 && IsSpellReady("Berserk"))
                        return CastAtPlayerLocation("Berserk", isHarmfulSpell: false);
                    if (!targetedEnemy.HasAura("Mangle", true) && IsSpellReady("Mangle", "Cat Form"))
                        return CastAtTarget("Mangle", "Cat Form");
                    if (player.ComboPoints == 5 && IsSpellReady("Rip", "Cat Form") && !targetedEnemy.HasAura("Rip", true))
                        return CastAtTarget("Rip", "Cat Form");
                    if (player.ComboPoints == 5 && IsSpellReady("Savage Roar", "Cat Form") && !player.HasAura("Savage Roar", true))
                        return CastAtPlayerLocation("Savage Roar", "Cat Form", isHarmfulSpell:false);
                    if (IsSpellReady("Rake", "Cat Form") && !targetedEnemy.HasAura("Rake", true))
                        return CastAtTarget("Rake", "Cat Form");
                }

                if ((player.ComboPoints == 5 || player.ComboPoints >= 2 && targetedEnemy.HealthPercent < 20) && IsSpellReady("Ferocious Bite", "Cat Form"))
                    return CastAtTarget("Ferocious Bite", "Cat Form");

                if(player.IsBehindTarget(targetedEnemy) && IsSpellReady("Shred", "Cat Form"))
                    return CastAtTarget("Shred", "Cat Form", facing: SpellFacingFlags.BehindAndFaceTarget);
                if (player.AuraStacks("Stampede") > 0 && IsSpellReady("Ravage", "Cat Form"))
                    return CastAtTarget("Ravage", "Cat Form");
                if (IsSpellReady("Rake", "Cat Form") && !targetedEnemy.HasAura("Rake", true) && targetedEnemy.HasAura("Mangle"))
                    return CastAtTarget("Rake", "Cat Form");
                if ((!player.IsBehindTarget(targetedEnemy) || !targetedEnemy.HasAura("Mangle", true)) && IsSpellReady("Mangle", "Cat Form"))
                    return CastAtTarget("Mangle", "Cat Form");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }
    }
}
