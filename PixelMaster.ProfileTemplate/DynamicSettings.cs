using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelMaster.ProfileTemplate
{
    /// <summary>
    /// List of dynamic settings that can be set by the user in the profile
    /// </summary>
    internal class DynamicSettings
    {
        #region Behavior Flags
        /// <summary>
        /// If set to true then player will not engage in combat
        /// </summary>
        public bool DisableCombat { get; set; }
        /// <summary>
        /// If set to true then player will not engage in combat if mounted
        /// </summary>
        public bool DisableCombatIfMounted { get; set; }
        /// <summary>
        /// If set then resurrection behavior will be disabled and player will not try to resurrect if dead
        /// </summary>
        public bool DisableResurrection { get; set; }
        /// <summary>
        /// If set then player wont perform preparation actions like Drink/Eat or heal or summon pet out of combat
        /// </summary>
        public bool DisablePreparation { get; set; }
        /// <summary>
        /// If set to true then player will not try to summon its pet if it is dismissed. Currently it is not implemented in the bot
        /// </summary>
        public bool DisableUsePet { get; set; }
        /// <summary>
        /// If set to true then emptying bags will be disabled and bot will not try to empty bags
        /// </summary>
        public bool DisableEmptyingBags { get; set; }
        /// <summary>
        /// If set to true then bot will not initiate repair routine if player gear are broken
        /// </summary>
        public bool DisableRepairing { get; set; }
        /// <summary>
        /// If set to true then bot will not try to loot any item
        /// </summary>
        public bool DisableLooting { get; set; }
        /// <summary>
        /// If set to true then bot will not initiate restock routine when it is needed
        /// </summary>
        public bool DisableRestocking { get; set; }
        /// <summary>
        /// If set then player wont try to fly while restocking
        /// </summary>
        public bool DisableFlyingWhileRestocking { get; set; }
        /// <summary>
        /// Set to true to disable the bot behavior to run to safe place when it detects there are nearby enemies that might engage in combat with the player.
        /// </summary>
        public bool DisableRunToSafePlaceInCombat { get; set; }
        /// <summary>
        /// Set to true to disable the bot behavior for range classes that tries to kite target enemies in combat.
        /// </summary>
        public bool DisableKiteTargetInCombat { get; set; }
        /// <summary>
        /// Set to true to disable the bot behavior to flee from the fight when it detects there are too many enemies fighting the player and player can not win the fight. 
        /// </summary>
        public bool DisableFleeFromTheFight { get; set; }
        /// <summary>
        /// If set to true then bot will to avoid nearby enemies while navigating
        /// </summary>
        public bool DisableAvoidEnemiesWhileMoving { get; set; }
        #endregion
    }
}
