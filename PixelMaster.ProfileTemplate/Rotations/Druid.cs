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
using System.Diagnostics;
using PixelMaster.Core.Behaviors.QuestBehaviors;


namespace CombatClasses
{
    public class EraDruidFeralRotation : IPMRotation
    {
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Druid;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "Era-Druid-Feral Leveling PvE";
        public string Author => "Cava";
        public string Description => "";

        private WowUnit MainTarget, CCTarget;

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            if (player.IsInCombat)
                RotationSpell();
            //var inv = player.Inventory;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            if (targetedEnemy != null)
                MainTarget = targetedEnemy;
            //Debugger.Log(1, "","-------------------------distance  ");
            //LogInfo("Casting pulling ability");

            if (IsSpellReady("Mark of the Wild") && !player.HasBuff("Mark of the Wild"))
                return CastAtPlayer("Mark of the Wild");

            if (IsSpellReady("Thorns") && !player.HasBuff("Thorns"))
                return CastAtPlayer("Thorns");

            if (IsSpellReady("Omen of Clarity") && !player.HasBuff("Omen of Clarity"))
                return CastAtPlayer("Omen of Clarity");

            if (MainTarget != null && MainTarget.NearbyEnemies.Count > 1)
            {
                if (IsSpellReadyOrCasting("Healing Touch"))
                    return CastAtPlayer("Healing Touch");
                if (IsSpellReadyOrCasting("Regrowth"))
                    return CastAtPlayer("Regrowth");

                if (IsSpellReadyOrCasting("Entangling Roots"))
                {
                    var enemyToRoot = MainTarget.NearbyEnemies.Where(e => e.HealthPercent > 25 && e.DistanceSquaredToPlayer > 64 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root)).OrderByDescending(e => e.Health).FirstOrDefault();//.ToList();
                    if (enemyToRoot != null && !player.IsMoving)
                        return CastAtUnit(enemyToRoot, "Entangling Roots");
                }
                if (IsSpellReadyOrCasting("Hibernate"))
                {
                    var enemyToHibernate = MainTarget.NearbyEnemies.Where(e => e.HealthPercent > 25 && e.DistanceSquaredToPlayer > 64 && !e.CCs.HasFlag(ControlConditions.CC) && (e.CreatureType == CreatureType.Beast || e.CreatureType == CreatureType.Dragonkin)).OrderByDescending(e => e.Health).FirstOrDefault();//.ToList();
                    if (enemyToHibernate != null && !player.IsMoving)
                        return CastAtUnit(enemyToHibernate, "Hibernate");
                }

                if (player.Target != null && player.Target != MainTarget)


                if (IsSpellReady("Dire Bear Form") && !DireBearForm())
                    return CastAtPlayer("Dire Bear Form");
                if (IsSpellReady("Bear Form") && !BearForm())
                    return CastAtPlayer("Bear Form");

                if (IsSpellReadyOrCasting("Wrath"))
                {
                    //if(MainTarget != null && player.Target != null && player.Target != MainTarget )
                    //{
                    //    CastAtUnit(MainTarget, "Wrath");
                    //}
                    //else
                    if (player.Target != null && player.Target != MainTarget)
                    {
                        CastAtUnit(MainTarget, "Wrath");
                    }
                    else
                    {
                        return CastAtTarget("Wrath");
                    }                       
                }
                    
            }
            else
            {
                if (IsSpellReady("Cat Form") && !CatForm() && !BearForm() && !DireBearForm())
                    return CastAtPlayer("Cat Form");

                if (IsSpellReady("Ravage") || IsSpellReady("Shred"))
                {
                    /*if (IsSpellReady("Prowl") && CatForm() && !player.HasAura("Prowl"))
                        return CastAtPlayer("Prowl");
                    if (IsSpellReady("Ravage") && CatForm() && player.HasAura("Prowl"))
                        return CastAtTarget("Ravage", facing: SpellFacingFlags.BehindAndFaceTarget);
                    else if (IsSpellReady("Shred") && CatForm() && player.HasAura("Prowl"))
                        return CastAtTarget("Shred", facing: SpellFacingFlags.BehindAndFaceTarget);*/
                }
            }
            if (IsSpellReadyOrCasting("Wrath") && player.Level < 20)
                return CastAtTarget("Wrath");

            if (IsSpellReady("Dire Bear Form") && !CatForm() && !BearForm() && !DireBearForm())
                return CastAtPlayer("Dire Bear Form");
            if (IsSpellReady("Bear Form") && !CatForm() && !BearForm() && !DireBearForm())
                return CastAtPlayer("Bear Form");
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
            var comboPoints = player.ComboPoints;
            var rage = player.PowerPercent;
            //Debugger.Log(1, "","Rage " + rage);
            List<WowUnit>? inCombatEnemies = null;




            //Survive
            if (player.HealthPercent < 45)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);

            }
            if (IsSpellReady("Rejuvenation") && player.HealthPercent < 30 && !player.HasAura("Rejuvenation"))
                return CastAtPlayer("Rejuvenation");

            if (IsSpellReady("Faerie Fire") && !CatForm() && !BearForm() && !DireBearForm() && targetedEnemy != null &&
                !targetedEnemy.IsInMeleeRange && !targetedEnemy.HasAura("Faerie Fire") && targetedEnemy.HealthPercent > 50)
                //return CastAtTarget("Faerie Fire");
                return CastAtUnit(targetedEnemy, "Faerie Fire");

            //Flee
            if (player.IsFleeingFromTheFight)
            {
                //Bear Form
                if (IsSpellReady("Dire Bear Form") && !DireBearForm())
                    return CastAtPlayer("Dire Bear Form");

                if (IsSpellReady("Bear Form") && !BearForm() && !DireBearForm())
                    return CastAtPlayer("Bear Form");

                return null;
            }

            //Survive 2
            if (IsSpellReadyOrCasting("Healing Touch") && player.HealthPercent < 30 && !player.IsMoving)
                return CastAtPlayer("Healing Touch");
            if (IsSpellReadyOrCasting("Regrowth") && player.HealthPercent < 40 && !player.HasAura("Regrowth") && !player.IsMoving)
                return CastAtPlayer("Regrowth");

            //Buffs
            if (IsSpellReady("Mark of the Wild") && !player.HasBuff("Mark of the Wild"))
                return CastAtPlayer("Mark of the Wild");
            
            if (IsSpellReady("Thorns") && !player.HasBuff("Thorns"))
                return CastAtPlayer("Thorns");

            if (IsSpellReady("Omen of Clarity") && !player.HasBuff("Omen of Clarity"))
                return CastAtPlayer("Omen of Clarity");




            if (targetedEnemy != null)
            {
                
                //var nearCombatEnemies = targetedEnemy.NearbyEnemies.Where(e => e.DistanceSquaredToPlayer < 25*25 && !e.CCs.HasFlag(ControlConditions.CC)).OrderByDescending(e => e.Health).FirstOrDefault();//.ToList();





                //Burst
                if (dynamicSettings.BurstEnabled)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayer("Berserking");
                    else if (player.Race == UnitRace.Orc && IsSpellReady("Blood Fury"))
                        return CastAtPlayer("Blood Fury");
                }

                //AoE handling
                inCombatEnemies = om.InCombatEnemies;
                if (inCombatEnemies.Count > 1)
                {
                    if (IsSpellReadyOrCasting("War Stomp") && !player.IsMoving && player.Race == UnitRace.Tauren)
                        return CastAtPlayerLocation("War Stomp");
                    var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 25);
                    if (nearbyEnemies.Count > 1)
                    {
                        /*if (player.IsOutdoors)
                        {
                            var enemyToRoot = nearbyEnemies.Where(e => e.HealthPercent > 25 && e.DistanceSquaredToPlayer > 64 && !e.CCs.HasFlag(ControlConditions.CC) && !e.CCs.HasFlag(ControlConditions.Root)).FirstOrDefault();//.ToList();
                            if (enemyToRoot != null && IsSpellReadyOrCasting("Entangling Roots") && !player.IsMoving)
                                return CastAtUnit(enemyToRoot, "Entangling Roots");
                        }
                        var enemyToHibernate = nearbyEnemies.Where(e => e.HealthPercent > 25 && e.DistanceSquaredToPlayer > 64 && !e.CCs.HasFlag(ControlConditions.CC) && (e.CreatureType == CreatureType.Beast || e.CreatureType == CreatureType.Dragonkin)).FirstOrDefault();//.ToList();
                        if (enemyToHibernate != null && IsSpellReadyOrCasting("Hibernate") && !player.IsMoving)
                            return CastAtUnit(enemyToHibernate, "Hibernate");*/
                    }
                    if (IsSpellReady("Demoralizing Roar") && nearbyEnemies.Any(e => !e.HasDeBuff("Demoralizing Roar")))
                        return CastAtPlayerLocation("Demoralizing Roar");

                    if (IsSpellReadyOrCasting("War Stomp") && !player.IsMoving && player.Race == UnitRace.Tauren && player.HealthPercent < 50)
                        return CastAtPlayerLocation("War Stomp");

                    if (IsSpellReady("Dire Bear Form") && !DireBearForm())
                        return CastAtPlayer("Dire Bear Form");
                    if (IsSpellReady("Bear Form") && !BearForm() && !DireBearForm())
                        return CastAtPlayer("Bear Form");

                    if (IsSpellReady("Swipe") && (BearForm()|| DireBearForm()) )
                        //return CastAtTarget("Swipe");
                        return CastAtUnit(targetedEnemy, "Swipe");
                }

                //Cat Form
                if (IsSpellReady("Cat Form") && !CatForm() && inCombatEnemies.Count < 2)
                    return CastAtPlayer("Cat Form");

                if (IsSpellReady("Ferocious Bite") && CatForm() && (comboPoints >= 5 || comboPoints >= targetedEnemy.HealthPercent/20))
                    return CastAtPlayer("Ferocious Bite");

                if (IsSpellReady("Faerie Fire(Feral)") && CatForm() && !targetedEnemy.HasAura("Faerie Fire"))
                    //return CastAtTarget("Faerie Fire(Feral)");
                    return CastAtUnit(targetedEnemy, "Faerie Fire(Feral)");

                if (IsSpellReady("Rake") && CatForm() && !targetedEnemy.HasAura("Rake"))
                    //return CastAtTarget("Rake");
                    return CastAtUnit(targetedEnemy, "Rake");

                if (IsSpellReady("Tiger's Fury") && CatForm() && !player.HasAura("Tiger's Fury"))
                    return CastAtPlayer("Tiger's Fury");

                if (IsSpellReady("Claw") && CatForm())
                    return CastAtTarget("Claw");

                if (IsSpellReady("Mangle") && CatForm() && comboPoints < 3)
                    return CastAtTarget("Mangle");


                //Bear Form
                if (IsSpellReady("Dire Bear Form") && !CatForm() && !BearForm() && !DireBearForm())
                    return CastAtPlayer("Dire Bear Form");

                if (IsSpellReady("Bear Form") && !CatForm() && !BearForm() && !DireBearForm())
                    return CastAtPlayer("Bear Form");

                if(BearForm() || DireBearForm())
                {
                    if (IsSpellReady("Maul") && targetedEnemy.IsInMeleeRange)
                        return CastAtTarget("Maul");

                    if (IsSpellReady("Bash") && targetedEnemy.IsInMeleeRange && targetedEnemy.IsCasting)
                        return CastAtTarget("Bash");
                }

                if (IsSpellReadyOrCasting("Wrath") && targetedEnemy.HealthPercent <= 20 && !targetedEnemy.IsInMeleeRange && targetedEnemy.IsMoving && !player.IsMoving)
                    return CastAtTarget("Wrath");

                //finaly if can use a form make sure u have it to ignore noform
                if (!CatForm() && !BearForm() && !DireBearForm())
                {
                    if (IsSpellReady("Cat Form"))
                        return CastAtPlayer("Cat Form");
                    if (IsSpellReady("Dire Bear Form"))
                        return CastAtPlayer("Dire Bear Form");
                    if (IsSpellReady("Bear Form"))
                        return CastAtPlayer("Bear Form");
                    return null;
                }

                //ignoring Noform
                if(IsSpellReady("Cat Form") || IsSpellReady("Dire Bear Form") || IsSpellReady("Bear Form"))
                    return CastAtTarget(sb.AutoAttack);

                //No Form
                if (IsSpellReady("Moonfire") && targetedEnemy.HealthPercent > 40 && !targetedEnemy.HasDeBuff("Moonfire") && !BearForm() && !CatForm() && !DireBearForm())
                    return CastAtTarget("Moonfire");
                if (IsSpellReadyOrCasting("Wrath") && targetedEnemy.HealthPercent > 40 && !BearForm() && !CatForm() && !DireBearForm())
                    return CastAtTarget("Wrath");
                if (IsSpellReadyOrCasting("Wrath") && targetedEnemy.HealthPercent <= 40 && !player.IsMoving &&
                    !targetedEnemy.IsInMeleeRange && !BearForm() && !CatForm() && !DireBearForm())
                    return CastAtTarget("Wrath");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;




        }


        bool CatForm()
        {
            return ObjectManager.Instance.Player.Form == ShapeshiftForm.Cat;
        }
        bool BearForm()
        {
            return ObjectManager.Instance.Player.Form == ShapeshiftForm.BearForm;
        }
        bool MoonkinForm()
        {
            return ObjectManager.Instance.Player.Form == ShapeshiftForm.MoonkinForm;
        }
        bool DireBearForm()
        {
            return ObjectManager.Instance.Player.Form == ShapeshiftForm.DireBearForm;
        }
    }
}
/*
Levels 10 to 14 — 5 points in Ferocity Icon Ferocity
Levels 15 to 19 — 5 points in Feral Aggression Icon Feral Aggression
Levels 20 to 21 — 2 points in Feline Swiftness Icon Feline Swiftness
Levels 22 to 26 — 5 points in Furor Icon Furor
Level 27 — 1 point in Feral Charge Icon Feral Charge
Levels 28 & 29 — 2 points in Sharpened Claws Icon Sharpened Claws
Level 30 — 1 point in Sharpened Claws Icon Sharpened Claws
Levels 31 & 32 — 2 points in Blood Frenzy Icon Blood Frenzy
Levels 33 to 35 — 3 points in Predatory Strikes Icon Predatory Strikes
Level 36 — 1 point in Faerie Fire (Feral) Icon Faerie Fire (Feral)
Levels 37 & 38 — 2 points in Savage Fury Icon Savage Fury
Level 39 — 1 point in Improved Shred Icon Improved Shred
Levels 40 to 44 — 5 points in Heart of the Wild Icon Heart of the Wild
Level 45 — 1 point in Leader of the Pack Icon Leader of the Pack
Level 46 — 1 point in Nature's Grasp Icon Nature's Grasp
Levels 47 to 49 — 3 points in Improved Nature's Grasp Icon Improved Nature's Grasp
Level 50 — 1 point in Improved Nature's Grasp Icon Improved Nature's Grasp
Levels 51 to 55 — 5 points in Natural Weapons Icon Natural Weapons
Level 56 — 1 point in Omen of Clarity Icon Omen of Clarity
Level 57 — 1 point in Improved Shred Icon Improved Shred
Levels 58 to 60 — 3 points in Natural Shapeshifter Icon Natural Shapeshifter
*/
