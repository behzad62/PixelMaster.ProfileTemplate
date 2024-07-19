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
    public class ShamanEnch : IPMRotation
    {
        private ShamanSettings settings => SettingsManager.Instance.Shaman;
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Shaman;
        // 0 - Melee DPS : Will try to stick to the target
        // 1 - Range: Will try to kite target if it got too close.
        // 2 - Healer: Will try to target party/raid members and get in range to heal them
        // 3 - Tank: Will try to engage nearby enemies who targeting alies
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[Cata][PvE]Shaman-Enhancement";
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
                if (IsSpellReady("Call of the Elements") && !ObjectManager.Instance.PlayerTotems.Any())
                    return CastAtPlayerLocation("Call of the Elements");
                if (IsSpellReady("Earth Shock"))
                    return CastAtTarget("Earth Shock");
                if (IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt");
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
            int maelstormStacks = player.AuraStacks("Maelstrom Weapon", true);
            if (!player.HasAura("Lightning Shield", true) && IsSpellReady("Lightning Shield"))
                return CastAtPlayerLocation("Lightning Shield", isHarmfulSpell: false);
            if (player.IsMoving && IsSpellReady("Spiritwalker's Grace"))
                return CastAtPlayerLocation("Spiritwalker's Grace", isHarmfulSpell: false);


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
            if (player.Debuffs.Any(d => d.Spell != null && d.Spell.DispelType == SpellDispelType.Curse))
            {
                if (IsSpellReady("Cleanse Spirit"))
                    return CastAtPlayer("Cleanse Spirit");
            }
            if (player.IsFleeingFromTheFight)
            {
                if (IsSpellReady("Earthbind Totem") && !IsTotemLanded("Earthbind Totem"))
                    return CastAtPlayerLocation("Earthbind Totem");
                if (IsSpellReady("Stoneclaw Totem") && !IsTotemLanded("Stoneclaw Totem"))
                    return CastAtPlayerLocation("Stoneclaw Totem");
                return null;
            }
            if (settings.EnhancementHeal && (player.HealthPercent < 40 && maelstormStacks >= 4) || player.HealthPercent < 60 && maelstormStacks >= 5)
            {
                if (IsSpellReadyOrCasting("Greater Healing Wave") && (maelstormStacks >= 5 || om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInMeleeRange) <= 2))
                    return CastAtPlayer("Greater Healing Wave");
                if (!PlayerLearnedSpell("Healing Surge") && IsSpellReadyOrCasting("Healing Wave") && (maelstormStacks >= 5 || om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInMeleeRange) <= 2))
                    return CastAtPlayer("Healing Wave");
                if (IsSpellReadyOrCasting("Healing Surge") && (maelstormStacks >= 5 || om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInMeleeRange) <= 3))
                    return CastAtPlayer("Healing Surge");
            }
            //Burst
            //if (dynamicSettings.BurstEnabled)
            //{

            //}
            //AoE handling
            if (inCombatEnemies.Count > 1)
            {
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
                if (nearbyEnemies.Count >= 3)
                {
                    if (IsSpellReady("Shamanistic Rage"))
                        return CastAtPlayerLocation("Shamanistic Rage", isHarmfulSpell: false);
                    if (IsSpellReady("Magma Totem") && !om.PlayerTotems.Any(t => t.Name == "Magma Totem" && t.DistanceSquaredToPlayer <= 100 || t.Name == "Fire Elemental Totem"))
                        return CastAtPlayerLocation("Magma Totem", isHarmfulSpell: true);
                    if (IsSpellReady("Strength of Earth Totem") && !player.HasAura("Strength of Earth"))
                        return CastAtPlayerLocation("Strength of Earth Totem", isHarmfulSpell: false);
                    if (IsSpellReady("Windfury Totem") && !player.HasBuff("Windfury Totem"))
                        return CastAtPlayerLocation("Windfury Totem", isHarmfulSpell: false);
                    if (IsSpellReady("Flame Shock"))
                    {
                        var flameTarget = nearbyEnemies.FirstOrDefault(e => !e.HasAura("Flame Shock", true));
                        if (flameTarget != null)
                            return CastAtUnit(flameTarget, "Flame Shock");
                    }
                }
            }

            //Targeted enemy
            if (targetedEnemy != null)
            {

                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Wind Shear") && targetedEnemy.DistanceSquaredToPlayer < 25 * 25)
                        return CastAtTarget("Wind Shear");
                }
                var enemiesTargetingMe = inCombatEnemies.Where(e => e.IsTargetingPlayer || e.IsTargetingPlayerPet);
                if ((targetedEnemy.IsElite || enemiesTargetingMe.Count() >= 3))
                {
                    if (IsSpellReady("Shamanistic Rage"))
                        return CastAtPlayerLocation("Shamanistic Rage", isHarmfulSpell: false);
                    if (IsSpellReady("Feral Spirit"))
                        return CastAtTarget("Feral Spirit");
                    if (enemiesTargetingMe.Count() >= 10 && IsSpellReady("Fire Elemental Totem"))
                        return CastAtPlayerLocation("Fire Elemental Totem");
                }
                if (targetedEnemy.DistanceSquaredToPlayer < 30 * 30 && IsSpellReady("Searing Totem") && !om.PlayerTotems.Any(t => t.Name == "Searing Totem" && Vector3.DistanceSquared(t.Position, targetedEnemy.Position) < 20 * 20 || t.Name == "Fire Elemental Totem" || t.Name == "Magma Totem" && t.DistanceSquaredToPlayer <= 20 * 20))
                    return CastAtPlayerLocation("Searing Totem", isHarmfulSpell: true);

                if(player.Level < 20)
                {
                    if (IsSpellReadyOrCasting("Lava Lash"))
                        return CastAtTarget("Lava Lash");
                    if (IsSpellReady("Primal Strike"))
                        return CastAtTarget("Primal Strike");
                    if (IsSpellReady("Earth Shock"))
                        return CastAtTarget("Earth Shock");
                    if (IsSpellReadyOrCasting("Lightning Bolt"))
                        return CastAtTarget("Lightning Bolt");
                }
                if (IsSpellReady("Stormstrike"))
                    return CastAtTarget("Stormstrike");
                if (IsSpellReady("Primal Strike") && !PlayerLearnedSpell("Stormstrike"))
                    return CastAtTarget("Primal Strike");

                if (!targetedEnemy.HasAura("Flame Shock", true) && IsSpellReady("Flame Shock") && (player.HasAura("Unleash Wind") || !PlayerLearnedSpell("Unleash Elements")) && (targetedEnemy.IsElite || inCombatEnemies.Count(u => Vector3.DistanceSquared(targetedEnemy.Position, u.Position) < 10 * 10) >= 3 && PlayerLearnedSpell("Fire Nova")))
                    return CastAtTarget("Flame Shock", facing: SpellFacingFlags.None);
                if (IsSpellReady("Earth Shock") && (!targetedEnemy.IsElite || targetedEnemy.AuraRemainingTime("Flame Shock", true).TotalSeconds >= 6))
                    return CastAtTarget("Earth Shock");
                var offhandWep = player.Inventory.GetEquippedItemsBySlot(EquipSlot.OffHand);
                if (offhandWep != null && offhandWep.Class == ItemClass.Weapon && IsSpellReadyOrCasting("Lava Lash"))
                    return CastAtTarget("Lava Lash");
                if (IsSpellReadyOrCasting("Fire Nova") && targetedEnemy.HasAura("Flame Shock", true) && inCombatEnemies.Count(u => Vector3.DistanceSquared(targetedEnemy.Position, u.Position) < 10 * 10) >= 3)
                    return CastAtTarget("Fire Nova");
                if (maelstormStacks >= 5 && GetUnitsWithinArea(inCombatEnemies, targetedEnemy.Position, 10).Count >= 2 && IsSpellReadyOrCasting("Chain Lightning"))
                    return CastAtTarget("Chain Lightning");
                if (maelstormStacks >= 5 && IsSpellReadyOrCasting("Lightning Bolt"))
                    return CastAtTarget("Lightning Bolt");
                if (IsSpellReady("Unleash Elements"))
                    return CastAtTarget("Unleash Elements");
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
