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
using AdvancedCombatClasses.Settings.Era;
using PixelMaster.Server.Shared;

namespace CombatClasses
{
    public class ShamanEnchSoD : IPMRotation
    {
        private ShamanSettings settings => ((EraCombatSettings)SettingsManager.Instance.Settings).Shaman;
        public IEnumerable<WowVersion> SupportedVersions => new[] { WowVersion.Classic_Cata, WowVersion.Classic_Cata_Ptr };
        public short Spec => 2;
        public UnitClass PlayerClass => UnitClass.Shaman;
        public CombatRole Role => CombatRole.MeleeDPS;
        public string Name => "[SoD][PvE]Shaman-Enhancement";
        public string Author => "PixelMaster";
        public string Description => "General PvE";

        public SpellCastInfo PullSpell()
        {
            var om = ObjectManager.Instance;
            var player = om.Player;
            var sb = om.SpellBook;
            var targetedEnemy = om.AnyEnemy;
            // Pre-fight preparation
            if (!player.HasBuff("Windfury Weapon"))
                return CastWithoutTargeting("Windfury Weapon", isHarmfulSpell: false);
            if (!player.HasBuff("Flametongue Weapon"))
                return CastWithoutTargeting("Flametongue Weapon", isHarmfulSpell: false);

            // Precast totems
            if (!IsTotemLanded("Strength of Earth Totem") && IsSpellReady("Strength of Earth Totem"))
                return CastWithoutTargeting("Strength of Earth Totem", isHarmfulSpell: false);
            if (!IsTotemLanded("Grace of Air Totem") && IsSpellReady("Grace of Air Totem"))
                return CastWithoutTargeting("Grace of Air Totem", isHarmfulSpell: false);
            if (!IsTotemLanded("Mana Spring Totem") && IsSpellReady("Mana Spring Totem"))
                return CastWithoutTargeting("Mana Spring Totem", isHarmfulSpell: false);
            if (!IsTotemLanded("Searing Totem") && IsSpellReady("Searing Totem"))
                return CastWithoutTargeting("Searing Totem", isHarmfulSpell: true);

            // Precast Lightning Shield
            if (!player.HasBuff("Lightning Shield", true) && IsSpellReady("Lightning Shield"))
                return CastWithoutTargeting("Lightning Shield", isHarmfulSpell: false);

            // On pull
            if (targetedEnemy != null)
            {
                if (IsSpellReady("Flame Shock"))
                    return CastAtTarget("Flame Shock");
                if (IsSpellReady("Feral Spirit"))
                    return CastAtTarget("Feral Spirit");
                if (IsSpellReady("Totemic Projection"))
                    return CastWithoutTargeting("Totemic Projection", isHarmfulSpell: false);
            }

            return CastAtTarget(sb.AutoAttack);
        }

        public SpellCastInfo? RotationSpell()
        {
            var om = ObjectManager.Instance;
            var dynamicSettings = BottingSessionManager.Instance.DynamicSettings;
            var targetedEnemy = om.AnyEnemy;
            var player = om.Player;
            var sb = om.SpellBook;
            var inv = om.Inventory;
            var comboPoints = player.SecondaryPower;
            List<WowUnit>? inCombatEnemies = om.InCombatEnemies.ToList();

            // Cooldowns and Utility/Defensive Abilities
            if (IsSpellReady("Feral Spirit"))
                return CastAtTarget("Feral Spirit");

            if (IsSpellReady("Shamanistic Rage") && (player.ManaPercent < 20 || player.HealthPercent < 50))
                return CastWithoutTargeting("Shamanistic Rage", isHarmfulSpell: false);

            if (IsSpellReady("Ancestral Guidance"))
                return CastWithoutTargeting("Ancestral Guidance", isHarmfulSpell: false);

            if (player.IsCasting && IsSpellReady("Grounding Totem"))
                return CastWithoutTargeting("Grounding Totem");

            //if (player.IsTakingNatureDamage && IsSpellReady("Nature Resistance Totem"))
            //    return CastWithoutTargeting("Nature Resistance Totem");

            //if (player.IsTakingFireDamage && IsSpellReady("Fire Resistance Totem"))
            //    return CastWithoutTargeting("Fire Resistance Totem");

            if (player.IsPoisoned && IsSpellReady("Poison Cleansing Totem"))
                return CastWithoutTargeting("Poison Cleansing Totem");

            if (player.IsDead && IsSpellReady("Reincarnation"))
                return CastWithoutTargeting("Reincarnation");

            // Healing and Defensive
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
            if (om.IsPlayerFleeingFromCombat)
            {
                if (IsSpellReady("Earthbind Totem") && !IsTotemLanded("Earthbind Totem"))
                    return CastWithoutTargeting("Earthbind Totem");
                if (IsSpellReady("Stoneclaw Totem") && !IsTotemLanded("Stoneclaw Totem"))
                    return CastWithoutTargeting("Stoneclaw Totem");
                return null;
            }
            int maelstromStacks = player.AuraStacks("Maelstrom Weapon", true);
            if (settings.EnhancementHeal && (player.HealthPercent < 40 && maelstromStacks >= 4) || player.HealthPercent < 60 && maelstromStacks >= 5)
            {
                if (IsSpellReadyOrCasting("Greater Healing Wave") && (maelstromStacks >= 5 || om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 2))
                    return CastAtPlayer("Greater Healing Wave");
                if (!PlayerLearnedSpell("Healing Surge") && IsSpellReadyOrCasting("Healing Wave") && (maelstromStacks >= 5 || om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 2))
                    return CastAtPlayer("Healing Wave");
                if (IsSpellReadyOrCasting("Healing Surge") && (maelstromStacks >= 5 || om.InCombatEnemies.Count(e => e.IsTargetingPlayer && e.IsInPlayerMeleeRange) <= 3))
                    return CastAtPlayer("Healing Surge");
            }

            // AoE handling
            if (inCombatEnemies.Count > 1)
            {
                if (maelstromStacks == 5 && IsSpellReady("Chain Lightning"))
                    return CastAtTarget("Chain Lightning");
                if (IsSpellReady("Stormstrike"))
                    return CastAtTarget("Stormstrike");
                var nearbyEnemies = GetUnitsWithinArea(inCombatEnemies, player.Position, 10);
                if (nearbyEnemies.Count >= 3)
                {
                    if (IsSpellReady("Fire Nova Totem"))
                        return CastWithoutTargeting("Fire Nova Totem");
                    if (IsSpellReady("Shamanistic Rage"))
                        return CastWithoutTargeting("Shamanistic Rage", isHarmfulSpell: false);
                    if (IsSpellReady("Magma Totem") && !om.PlayerTotems.Any(t => t.Name == "Magma Totem" && t.DistanceSquaredToPlayer <= 100 || t.Name == "Fire Elemental Totem"))
                        return CastWithoutTargeting("Magma Totem", isHarmfulSpell: true);
                    if (IsSpellReady("Strength of Earth Totem") && !player.HasAura("Strength of Earth"))
                        return CastWithoutTargeting("Strength of Earth Totem", isHarmfulSpell: false);
                    if (IsSpellReady("Windfury Totem") && !player.HasBuff("Windfury Totem"))
                        return CastWithoutTargeting("Windfury Totem", isHarmfulSpell: false);
                }
                if (IsSpellReady("Flame Shock"))
                {
                    var flameTarget = nearbyEnemies.FirstOrDefault(e => !e.HasAura("Flame Shock", true));
                    if (flameTarget != null)
                        return CastAtUnit(flameTarget, "Flame Shock");
                }
                if (IsSpellReady("Lava Lash"))
                    return CastAtTarget("Lava Lash");

                if (!player.HasAura("Lightning Shield", true) && IsSpellReady("Lightning Shield"))
                    return CastWithoutTargeting("Lightning Shield", isHarmfulSpell: false);
            }

            // Single-target rotation
            if (targetedEnemy != null)
            {
                if (targetedEnemy.IsCasting)
                {
                    if (IsSpellReady("Wind Shear") && targetedEnemy.DistanceSquaredToPlayer < 25 * 25)
                        return CastAtTarget("Wind Shear");
                    if (IsSpellReady("Grounding Totem"))
                        return CastWithoutTargeting("Grounding Totem");
                }
                var enemiesTargetingMe = inCombatEnemies.Where(e => e.IsTargetingPlayer || e.IsTargetingPlayerPet);
                if ((targetedEnemy.IsElite || enemiesTargetingMe.Count() >= 3))
                {
                    if (IsSpellReady("Shamanistic Rage"))
                        return CastWithoutTargeting("Shamanistic Rage", isHarmfulSpell: false);
                    if (IsSpellReady("Feral Spirit"))
                        return CastAtTarget("Feral Spirit");
                    if (enemiesTargetingMe.Count() >= 10 && IsSpellReady("Fire Elemental Totem"))
                        return CastWithoutTargeting("Fire Elemental Totem");
                }


                // In-combat rotation
                if (IsSpellReady("Stormstrike"))
                    return CastAtTarget("Stormstrike");

                if (maelstromStacks == 5 && targetedEnemy.HasAura("Flame Shock", true))
                {
                    if (IsSpellReady("Lava Burst"))
                        return CastAtTarget("Lava Burst");
                    if (IsSpellReady("Lightning Bolt"))
                        return CastAtTarget("Lightning Bolt");
                }

                if (!targetedEnemy.HasAura("Flame Shock", true) && IsSpellReady("Flame Shock"))
                    return CastAtTarget("Flame Shock");

                if (IsSpellReady("Earth Shock"))
                    return CastAtTarget("Earth Shock");

                if (IsSpellReady("Lava Lash"))
                    return CastAtTarget("Lava Lash");

                if (!player.HasAura("Lightning Shield", true) && IsSpellReady("Lightning Shield"))
                    return CastWithoutTargeting("Lightning Shield", isHarmfulSpell: false);

                if (targetedEnemy.DistanceSquaredToPlayer < 30 * 30 && IsSpellReady("Searing Totem") && !om.PlayerTotems.Any(t => t.Name == "Searing Totem" && Vector3.DistanceSquared(t.Position, targetedEnemy.Position) < 20 * 20 || t.Name == "Fire Elemental Totem" || t.Name == "Magma Totem" && t.DistanceSquaredToPlayer <= 20 * 20))
                    return CastWithoutTargeting("Searing Totem", isHarmfulSpell: true);

                // Maintain uptime on enemies with auto-attacks
                if (!player.IsCasting && targetedEnemy.IsInPlayerMeleeRange && !targetedEnemy.IsPlayerAttacking)
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
