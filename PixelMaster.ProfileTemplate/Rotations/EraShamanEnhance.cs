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
    public class EraShamanEnhanceRotation : IPMRotation
    {
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Shaman;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "Era-Shaman-Enhance Leveling PvE";
        public string Author => "Cava";
        public string Description => "";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            //var inv = player.Inventory;
            var sb = player.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            //Debugger.Log(1, "","-------------------------distance  ");
            //LogInfo("Casting pulling ability");
            if (targetedEnemy != null && targetedEnemy.NearbyEnemies.Count == 0)
            {
                if (IsSpellReady("Flame Shock"))
                    return CastAtTarget("Flame Shock");
            }
            else if (IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt",1);
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
            var rage = player.PowerPercent;
            //Debugger.Log(1, "","Rage " + rage);
            List<WowUnit> inCombatEnemies = om.InCombatEnemies;
            if (player.HealthPercent < 25)
            {
                var healthStone = inv.GetHealthstone();
                if (healthStone != null)
                    return UseItem(healthStone);
                var healingPot = inv.GetHealingPotion();
                if (healingPot != null)
                    return UseItem(healingPot);
            }
            if (player.HealthPercent < 35 && !IsSpellCasting("Healing Wave"))
            {
                if (IsSpellReadyOrCasting("Lesser Healing Wave"))
                    return CastAtPlayer("Lesser Healing Wave");
            }
            if (player.HealthPercent < 55 )
            {
                if((player.PowerPercent > 60|| inCombatEnemies.Count > 1 || IsSpellCasting("Lesser Healing Wave")) && IsSpellReadyOrCasting("Lesser Healing Wave"))
                    return CastAtPlayer("Lesser Healing Wave");
                if ((targetedEnemy is null || inCombatEnemies.Count > 1 || targetedEnemy.HealthPercent > 0.8 * player.HealthPercent) && IsSpellReadyOrCasting("Healing Wave"))
                    return CastAtPlayer("Healing Wave");
            }
            if (player.PowerPercent < 20)
            {
                var manaPot = inv.GetManaPotion();
                if (manaPot != null)
                    return UseItem(manaPot);
            }
            else if (player.PowerPercent < 30 && !player.HasBuff("Water Shield") && IsSpellReady("Water Shield"))
            {
                return CastAtPlayer("Water Shield");
            }
            else if (player.PowerPercent < 60 && !player.HasBuff("Water Shield"))
            {
                if (IsSpellReady("Shamanistic Rage"))
                    return CastAtPlayer("Shamanistic Rage");
            }
            //Dispell
            if (player.Debuffs.Any(d => d.Spell != null && (d.Spell.DispelType == SpellDispelType.Poison)))
            {
                if (IsSpellReady("Cure Poison"))
                    return CastAtPlayer("Cure Poison");
            }
            if (player.Debuffs.Any(d => d.Spell != null && (d.Spell.DispelType == SpellDispelType.Disease)))
            {
                if (IsSpellReady("Cure Disease"))
                    return CastAtPlayer("Cure Disease");
            }

            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Earthbind Totem") && !isEarthbindTotemLanded())
                    return CastAtPlayerLocation("Earthbind Totem");
                if (IsSpellReady("Stoneclaw Totem") && !isStoneclawTotemLanded())
                    return CastAtPlayerLocation("Stoneclaw Totem");
                return null;
            }

            //Burst
            if (dynamicSettings.BurstEnabled)
            {
                if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                    return CastAtPlayer("Berserking");
                else if (player.Race == UnitRace.Orc && IsSpellReady("Blood Fury"))
                    return CastAtPlayer("Blood Fury");
            }

            //AoE handling

            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 8);
                if (nearbyEnemies.Count > 2)
                {
                    if (IsSpellReadyOrCasting("Feral Spirit"))
                        return CastAtPlayerLocation("Feral Spirit");
                }
                if (nearbyEnemies.Count > 1)
                {
                    //Casting Stoneclaw Totem more enemys
                    if (!player.IsMoving && player.Race == UnitRace.Tauren && IsSpellReadyOrCasting("War Stomp"))
                        CastAtPlayerLocation("War Stomp");
                    if (IsSpellReadyOrCasting("Magma Totem") && !isMagmaTotemLanded())
                        return CastAtPlayerLocation("Magma Totem");
                    else if (IsSpellReady("Fire Nova Totem") && !isFireNovaTotemLanded() && !isMagmaTotemLanded()) 
                        return CastAtPlayerLocation("Fire Nova Totem");
                    if (IsSpellReadyOrCasting("Strength of Earth Totem") && !isStrengthofEarthTotemLanded())
                        return CastAtPlayerLocation("Strength of Earth Totem");
                    if (IsSpellReadyOrCasting("Chain Lightning") && player.HasBuff("Maelstrom Weapon") && player.AuraStacks(45) >= 3 && IsPlayerTargetInSpellRange("Chain Lightning"))//TODO check for auraID
                        return CastAtTarget("Chain Lightning");
                }
                if (dynamicSettings.AllowBurstOnMultipleEnemies)
                {
                    if (player.Race == UnitRace.Troll && IsSpellReady("Berserking"))
                        return CastAtPlayer("Berserking");
                    else if (player.Race == UnitRace.Orc && IsSpellReady("Blood Fury"))
                        return CastAtPlayer("Blood Fury");

                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Earth Shock"))
                        return CastAtTarget("Earth Shock");
                    else if (IsSpellReady("Grounding Totem") && !isGroundingTotemLanded())
                        return CastAtPlayerLocation("Grounding Totem");
                }
                if (IsSpellReady("Earth Shock") && targetedEnemy.HasDeBuff("Stormstrike") && (player.HasBuff("Focused") || player.HasBuff("Elemental Focus")))
                    return CastAtTarget("Earth Shock");
                if (IsSpellReadyOrCasting("Lightning Bolt") && IsPlayerTargetInSpellRange("Lightning Bolt") &&!targetedEnemy.IsInMeleeRange && targetedEnemy.IsMoving && targetedEnemy.HealthPercent < 15)
                    return CastAtTarget("Lightning Bolt");

                if (IsSpellReady("Water Shield") && !player.HasBuff("Water Shield"))
                    return CastAtPlayer("Water Shield");
                else if (IsSpellReady("Lightning Shield") && !player.HasBuff("Water Shield") && !player.HasBuff("Lightning Shield"))
                    return CastAtPlayer("Lightning Shield");

                //Stoneskin Totem
                //Searing Totem
                //Casting Stoneclaw Totem more enemys

                if (IsSpellReady("Flame Shock") && targetedEnemy.HealthPercent > 35 && (inCombatEnemies.Count > 1 || player.PowerPercent > player.HealthPercent) && IsPlayerTargetInSpellRange("Flame Shock"))//targetedEnemy.HealthPercent > 50
                    return CastAtTarget("Flame Shock");
                //else if (IsSpellReady("Earth Shock") && IsPlayerTargetInSpellRange("Earth Shock"))
                //    return CastAtTarget("Earth Shock");
                if (IsSpellReady("Stormstrike") && !targetedEnemy.HasDeBuff("Stormstrike") && IsPlayerTargetInSpellRange("Stormstrike") )
                    return CastAtTarget("Stormstrike");
                if (IsSpellReady("Lava Lash") && IsPlayerTargetInSpellRange("Lava Lash"))
                    return CastAtTarget("Lava Lash");
                if (player.Level < 4 && IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }


        private bool isSearingTotemLanded()
        {
            var totensIds = new List<int>() { 2523, 3902, 3903, 3904, 7400, 7402 };
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => totensIds.Contains(npc.Id) && npc.IsPet).ToList();
            if (mobs.Count > 0)
                return true;
            return false;
        }
        private bool isGroundingTotemLanded()
        {
            var totensIds = new List<int>() { 5925 };
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => totensIds.Contains(npc.Id) && npc.IsPet).ToList();
            if (mobs.Count > 0)
                return true;
            return false;
        }
        private bool isMagmaTotemLanded()
        {
            var totensIds = new List<int>() { 5929, 7464, 7465, 7466 };
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => totensIds.Contains(npc.Id) && npc.IsPet).ToList();
            if (mobs.Count > 0)
                return true;
            return false;
        }
        private bool isFireNovaTotemLanded()
        {
            var totensIds = new List<int>() { 5879, 6110, 6111, 7844, 7845 };
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => totensIds.Contains(npc.Id) && npc.IsPet).ToList();
            if (mobs.Count > 0)
                return true;
            return false;
        }
        private bool isEarthbindTotemLanded()
        {
            var totensIds = new List<int>() { 2630 };
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => totensIds.Contains(npc.Id) && npc.IsPet).ToList();
            if (mobs.Count > 0)
                return true;
            return false;
        }
        private bool isStoneclawTotemLanded()
        {
            var totensIds = new List<int>() { 3579, 3911, 3912, 3913, 7398, 7399 };
            var mobs = ObjectManager.Instance.GetVisibleUnits().Where(npc => totensIds.Contains(npc.Id) && npc.IsPet).ToList();
            if (mobs.Count > 0)
                return true;
            return false;
        }
        private bool isStrengthofEarthTotemLanded()
        {
            return ObjectManager.Instance.Player.HasAura("Strength of Earth");
        }
    }
}
/*
10Shield Specialization Rank 1
11Shield Specialization Rank 2
12Shield Specialization Rank 3
13Shield Specialization Rank 4
14Shield Specialization Rank 5
15Thundering Strikes Rank 1
16Thundering Strikes Rank 2
17Thundering Strikes Rank 3
18Thundering Strikes Rank 4
19Thundering Strikes Rank 5
20Improved Ghost Wolf Rank 1
21Improved Ghost Wolf Rank 2
22Two-Handed Axes and Maces
23Enhancing Totems Rank 1
24Enhancing Totems Rank 2
25Flurry Rank 1
26Flurry Rank 2
27Flurry Rank 3
28Flurry Rank 4
29Flurry Rank 5
30Parry
31Elemental Weapons Rank 1
32Elemental Weapons Rank 2
33Elemental Weapons Rank 3
34Anticipation Rank 1
35Weapon Mastery Rank 1
36Weapon Mastery Rank 2
37Weapon Mastery Rank 3
38Weapon Mastery Rank 4
39Weapon Mastery Rank 5
40Stormstrike
41Improved Healing Wave Rank 1
42Improved Healing Wave Rank 2
43Improved Healing Wave Rank 3
44Improved Healing Wave Rank 4
45Improved Healing Wave Rank 5
46Totemic Focus Rank 1
47Totemic Focus Rank 2
48Totemic Focus Rank 3
49Totemic Focus Rank 4
50Totemic Focus Rank 5
51Totemic Mastery
52Nature's Guidance Rank 1
53Nature's Guidance Rank 2
54Nature's Guidance Rank 3
55Anticipation Rank 2
56Anticipation Rank 3
57Anticipation Rank 4
58Anticipation Rank 5
59Improved Weapon Totems Rank 1
60Improved Weapon Totems Rank 2
*/