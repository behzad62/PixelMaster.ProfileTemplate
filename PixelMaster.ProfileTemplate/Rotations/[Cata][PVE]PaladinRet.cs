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
    public class PaladinRet : IPMRotation
    {
        private PaladinSettings settings => SettingsManager.Instance.Paladin;
        public short Spec => 3;
        public UnitClass PlayerClass => UnitClass.Paladin;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]Paladin-Ret";
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
                if (player.AuraStacks("The Art of War") > 0 && IsSpellReady("Exorcism"))
                    return CastAtTarget("Exorcism");
                if (IsSpellReady("Judgement"))
                    return CastAtTarget("Judgement");
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
            if (player.HealthPercent < 15 && IsSpellReady("Lay on Hands"))
                return CastAtPlayer("Lay on Hands");
            if (player.HealthPercent < 30 && IsSpellReadyOrCasting("Flash of Light"))
                return CastAtPlayer("Flash of Light");
            if (player.HealthPercent < 30 && !PlayerLearnedSpell("Flash of Light") && IsSpellReadyOrCasting("Holy Light"))
                return CastAtPlayer("Holy Light");
            if (player.HealthPercent < 50 && player.SecondaryPower == 3 && IsSpellReady("Word of Glory"))
                return CastAtPlayer("Word of Glory");

            if (player.Auras.Any(a=>a.Spell != null && IsImpairingSpell(a.Spell)) && IsSpellReady("Hand of Freedom"))
                return CastAtPlayer("Hand of Freedom");

            if(player.PowerPercent < 20)
                return CastAtPlayer("Divine Plea");

            if (player.IsFleeingFromTheFight)
            {
                if(IsSpellReady("Divine Shield"))
                    return CastAtPlayerLocation("Divine Shield", isHarmfulSpell:false);
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
                if (nearbyEnemies.Count >= settings.ConsecrationCount)
                {
                    if(IsSpellReady("Zealotry"))
                        return CastAtPlayerLocation("Zealotry", isHarmfulSpell: false);
                    if (IsSpellReady("Avenging Wrath"))
                        return CastAtPlayerLocation("Avenging Wrath", isHarmfulSpell: false);
                    if (IsSpellReady("Guardian of Ancient Kings"))
                        return CastAtPlayerLocation("Guardian of Ancient Kings", isHarmfulSpell: false);
                    if (IsSpellReady("Divine Storm"))
                        return CastAtPlayerLocation("Divine Storm", isHarmfulSpell: true);
                    if (IsSpellReady("Consecration"))
                        return CastAtPlayerLocation("Consecration", isHarmfulSpell: true);
                    if (IsSpellReady("Holy Wrath"))
                        return CastAtPlayerLocation("Holy Wrath", isHarmfulSpell: true);
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Rebuke") && targetedEnemy.DistanceSquaredToPlayer < 10 * 10)
                        return CastAtTarget("Rebuke");
                    if (IsSpellReady("Hammer of Justice") && targetedEnemy.DistanceSquaredToPlayer < 15 * 15)
                        return CastAtTarget("Hammer of Justice");
                }


                if (targetedEnemy.IsElite)
                {
                    if (IsSpellReady("Zealotry"))
                        return CastAtPlayerLocation("Zealotry", isHarmfulSpell: false);
                    if (IsSpellReady("Avenging Wrath"))
                        return CastAtPlayerLocation("Avenging Wrath", isHarmfulSpell: false);
                }

                if (player.SecondaryPower == 3 && IsSpellReady("Inquisition"))
                    return CastAtPlayerLocation("Inquisition");
                if (IsSpellReady("Hammer of Justice") && player.HealthPercent <= 40)
                    return CastAtTarget("Hammer of Justice");
                if (IsSpellReady("Crusader Strike"))
                    return CastAtTarget("Crusader Strike");
                if (IsSpellReady("Hammer of Wrath"))
                    return CastAtTarget("Hammer of Wrath");
                if(player.SecondaryPower == 3 && IsSpellReady("Templar's Verdict") && (player.HasAura("Inquisition") || !PlayerLearnedSpell("Inquisition")))
                    return CastAtTarget("Templar's Verdict");
                if(player.AuraStacks("The Art of War") > 0 && IsSpellReady("Exorcism"))
                    return CastAtTarget("Exorcism");
                if (IsSpellReady("Judgement"))
                    return CastAtTarget("Judgement");
                return CastAtTarget(sb.AutoAttack);
            }
            return null;
        }

        static bool IsImpairingSpell(Spell spell)
        {
            if (spell.Categories != null)
            {
                if (spell.Categories.Any(c=>IsImpairingMechanic(c.Mechanic)))
                    return true;
            }
            if (spell.Effects != null)
            {
                return spell.Effects.Any(e => IsImpairingMechanic(e.EffectMechanic));
            }
            return false;

            static bool IsImpairingMechanic(SpellMechanic mechanic)
            {
                return mechanic switch
                {
                    SpellMechanic.Dazed => true,
                    SpellMechanic.Disoriented => true,
                    SpellMechanic.Frozen => true,
                    SpellMechanic.Incapacitated => true,
                    SpellMechanic.Rooted => true,
                    SpellMechanic.Slowed => true,
                    SpellMechanic.Snared => true,
                    _ => false,
                };
            }
        }
    }
}
