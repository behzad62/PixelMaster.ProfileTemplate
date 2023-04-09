using static PixelMaster.Core.API.PMProfileBuilder;
using PixelMaster.Core.API;
using PixelMaster.Core.Interfaces;
using PixelMaster.Core.Profiles;
using PixelMaster.Core.Behaviors;
using PixelMaster.Core.Behaviors.Transport;
using PixelMaster.Core.Managers;
using PixelMaster.Services.Behaviors;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Threading;
using System.Threading.Tasks;
using PixelMaster.Server.Shared;
using FluentBehaviourTree;
using PixelMaster.Core.Behaviors.QuestBehaviors;
using PixelMaster.Core.Wow.Objects;
using PixelMaster.Core.Behaviors.Looting;

namespace PixelMaster.ProfileTemplate;
//Do not touch lines above this

public class MyProfile : IPMProfile //it is important to implement 'IPMProfile' interface, but u can change 'MyProfile' to any name
{
    /// <summary>
    /// Here add mobs that bot should avoid. i.e. dangerous elite mobs that bot should not try to fight.
    /// If player get in combat to any of these mobs, will try to run away
    /// Also, player will try to avoid these mobs while navigating
    /// </summary>
    List<Mob> avoidMobs = new List<Mob>()
    {
        new Mob{Id=10356, MapId=0, Name="Bayne"},
        new Mob{Id=1911, MapId=0, Name="Deeb"},
    };
    /// <summary>
    /// Black spots in the navmesh. Add these if you want bot not pass these zones.
    /// Or there are navmesh problems at these spots.
    /// </summary>
    List<Blackspot> blackspots = new List<Blackspot>()
    {
        new Blackspot{Position= new Vector3(-2774.54f, -703.32f, 5.86f), MapID= 1, Radius=20f},//too many mobs bot die alot
    };
    /// <summary>
    /// Here you can add mailboxes that bot can use.
    /// </summary>
    List<MailBox> mailboxes = new List<MailBox>()
    {
        new MailBox{Name="Brill", MapId=0, Position= new Vector3(2238.56f, 254.50f, 34.01f)},//143990
        new MailBox{Name="Undercity", MapId=0, Position= new Vector3(1554.97f, 235.11f, -43.20f)},//195629
    };
    /// <summary>
    /// Here you can add vendors that bot can use.
    /// </summary>
    List<Vendor> vendors = new List<Vendor>()
    {
        new Vendor{Id=2115, Name="Joshua Kien", MapId=0, Position=new Vector3(1866.02f, 1574.45f, 94.31f), Type=VendorType.Food},//start
        new Vendor{Id=2116, Name="Blacksmith Rand", MapId=0, Position=new Vector3(1842.44f, 1570.19f, 96.58f), Type=VendorType.Repair},//start
        new Vendor{Id=5688, Name="Innkeeper Renee", MapId=0, Position=new Vector3(2269.51f, 244.94f, 34.26f), Type=VendorType.Food},//Brill
        new Vendor{Id=2137, Name="Eliza Callen", MapId=0, Position=new Vector3(2246.33f, 308.24f, 35.19f), Type=VendorType.Repair},//Brill
        new Vendor{Id=4554, Name="Tawny Grisette", MapId=0, Position=new Vector3(1611.00f, 273.79f, -43.10f), Type=VendorType.Food},//Undercity
        new Vendor{Id=4556, Name="Gordon Wendham", MapId=0, Position=new Vector3(1610.45f, 283.26f, -43.10f), Type=VendorType.Repair},//Undercity
    };
    /// <summary>
    /// Creates profile settings for this profile.
    /// </summary>
    /// <returns></returns>
    PMProfileSettings CreateSettings()
    {
        return new PMProfileSettings()
        {
            ProfileName = "[H-Quest](01-14)Undead",
            Author = "PixelMaster",
            Description = "Quest Leveling Undead Level 1 to 14!",
            //Objects
            AvoidMobs = avoidMobs,  //sets to the list defined above
            Blackspots = blackspots,//sets to the list defined above
            Mailboxes = mailboxes,  //sets to the list defined above
            Vendors = vendors,      //sets to the list defined above
            //Player Settings
            MinPlayerLevel = 1,     //Min. player level for this profile. Profile will finish for player bellow this level
            MaxPlayerLevel = 100,   //Max. player level for this profile. Profile will finish for players above this level
            MinDurabilityPercent = 15,  //If any of player items durabilities fell bellow this percent, bot will try will go to vendor to repair/sell/mail/restock items
            MinFreeBagSlots = 1,        //If player free general bag slots reach this number, bot will go to vendor to sell/mail/restock items
            //Death Settings
            MaxDeathsByOtherPlayersBeforeStop = 0, //if this number is greater than zero and player is killed by other players more than this number, current profile fails and stops.
            MaxDeathsBeforeStop = 0,    //if this number is greater than zero and player dies more than this number, current profile fails and stops.
            //Sell Settings
            SellGrey = true,    //If true, player will sell grey items when going to to sell/mail sequence
            SellWhite = true,   //If true, player will sell white items when going to to sell/mail sequence
            SellGreen = true,  //If true, player will sell green items when going to to sell/mail sequence
            SellBlue = false,   //If true, player will sell blue items when going to to sell/mail sequence
            SellPurple = false, //If true, player will sell purple items when going to to sell/mail sequence
            SellIncludesBOEs = false,   //If true, player will also sell BoE items when going to to sell/mail sequence
            SellIncludesRecipies = false, //If true, player will also sell recipies when going to to sell/mail sequence
            SellIncludesTradeGoodItems = false, //If true, player will also sell trade good items when going to to sell/mail sequence
            //Mail Settings
            MailGrey = false, //If true, player will mail grey items when going to to sell/mail sequence. Mailing happens after selling items.
            MailWhite = true, //If true, player will mail white items when going to to sell/mail sequence. Mailing happens after selling items.
            MailGreen = true, //If true, player will mail green items when going to to sell/mail sequence. Mailing happens after selling items.
            MailBlue = true,  //If true, player will mail blue items when going to to sell/mail sequence. Mailing happens after selling items.
            MailPurple = true,//If true, player will mail purple items when going to to sell/mail sequence. Mailing happens after selling items.
            MailTradeGoodItems = true, //If true, player will also mail trade good items when going to to sell/mail sequence. Mailing happens after selling items.
            MailRecipies = true, //If true, player will also mail recipies when going to to sell/mail sequence. Mailing happens after selling items.
            //Restock
            //Bellow you can define restock list for items you want to restock every time bot is doing repair/sell/mail sequence

            //'restockAmount' it means bot will try to restock amount equal to ('restockAmount' - sum of existing item counts in the bags).
            //i.e. restockAmount = 20, and there are 5x Food A and 10x Food B already in the bags,
            //then, bot will buy only 5x of the best food found from the closest vendor in the list.

            //'vendorIDs' are list of vendors player will try to restock from.
            //restocking only happens if vendor is close enough to the player when bot is doing sell/mail/repair sequence
            //so if none of these vendors are close enough to the player (150 yards range) then restokcing will be skipped
            Foods = (20, new int[] { 829,1247,1237,6734 }),//Tough Hunk of Bread,
            Drinks = (20, new [] { 6791, 6928, 6746, 7714, 3934 }),
            Arrows = (1000, new int[] { 829,1691,1682,7976 }),
            Bullets = (1000, new int[] { 829,1691,1682,7976 }),

            //Keep items are items you want to skip from selling or mailing
            KeepItems = new List<int> {
                2886,   //Crag Boar Rib (quest 384 = Beer Basted Boar Ribs)
                769,  //Chunk of Boar Meat(quest 317 = Stocking Jetsteam) 
                2589, //Linen Cloth Paladin quest
            },
            //Failure behavior
            //This indicates what profile should do if any tasks in it fails.
            //'Continue' means continue doing other tasks
            //'ReturnSuccess' means finish current profile but return success
            //'ReturnFailure' means finish current profile and return failure
            //Last two options are useful when this profile is the child of a parent profile.
            OnTaskFailure = TaskFailureBehavior.ReturnFailure,
        };
    }
    /// <summary>
    /// Here is the main method that you create your profile inside
    /// </summary>
    /// <returns></returns>
    public IPMProfileContext Create()
    {
        var ME = ObjectManager.Instance.Player;//just a shortcut to use inside profile
        var settings = CreateSettings(); //Creates profile settings from the above method
        StartProfile(settings); //Starting the profile using the settings
        //-------------------------------START PROFILE-------------------------------

        //here you add list of tasks/behaviors to the profile. i.e.
        PickUp(0, 363, 1568, "Rude Awakening");

        //Conditional tasks can be defined like this. Note than u should add conditions after () =>
        IF(() => ME.QuestLog.HasQuest(363) && ME.QuestLog.IsCompleted(363));//starts if
            TurnIn(0, 363, 1569, "Rude Awakening");
            RunMacro(0, "/dance", "for fun");
        EndIF();//ends if

        //Loops example:
        While(() => ME.QuestLog.IsCompleted(6002) == false);//starts while
            IF(() => ME.Inventory.HasItem(15710), onChildFailure: TaskFailureBehavior.Continue);
                UseItem(1, 0, "Cenarion Lunardust", NumTimes: 1, TargetMethod: TargettingMethod.POSITION, Hotspots: "(-2497.33, -1633.08, 91.76)");
                Wait(0, 10, "Body and Heart");
            EndIF();
        EndWhile();//ends while

        //Creating groups example
        //Groups are useful when you want to do group of tasks and control the failing behavior.
        //Failure behavior
        //This indicates what group should do if any tasks in it fails.
        //'ReturnFailure' means finish current group as soon as a task faild and return failure or when all tasks are done returns success
        //'Continue' means continue doing other tasks and when all done return success
        //'ReturnSuccess' means finish current group as soon as a task faild (or all tasks done) but return success
        //Last two options are useful when we dont want to stop the profile if i.e. training a skill or buying items fails.
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
            SellBuyStuff1(3882, 3882, 3160);
            IF(() => ME.Class == UnitClass.Warrior);
                TrainSkill(1, 3153, "6673");
                EndIF();
                IF(() => ME.Class == UnitClass.Shaman);
                TrainSkill(1, 3157, "8017");
                EndIF();
                IF(() => ME.Class == UnitClass.Rogue);
                TrainSkill(1, 3155, "1784");
                EndIF();
                IF(() => ME.Class == UnitClass.Hunter);
                TrainSkill(1, 3154, "1494");
                EndIF();
                IF(() => ME.Class == UnitClass.Warlock);
                TrainSkill(1, 3156, "688");
                EndIF();
                IF(() => ME.Class == UnitClass.Priest);
                TrainSkill(1, 3707, "1243");
                EndIF();
                IF(() => ME.Class == UnitClass.Mage);
                TrainSkill(1, 5884, "1459");
            EndIF();
        EndGroup();

        //To load nested profiles
        //This loads this profile inside the 'Profiles' folder (inside the bot folder)
        LoadProfileFromFile(@"Relative\Path\Inside\ProfilesFolder\ProfileFileName.cs");
        //To loads profiles from the server
        //You get product id when you upload a profile inside the bot server
        //Product key is like a password for your profile and should be unique amoung all products in the server.
        LoadProfileFromServer(productId: 10, productKey: "Your product key");

        //To load a custom behavior
        //This loads this behavior inside the 'Behaviors' folder (inside the bot folder)
        //If your behaviors needs parameters, you can pass parameters dictionary like bellow to your behavior.
        LoadBehaviorFromFile(@"Relative\Path\Inside\BehaviorsFolder\BehaviorName.cs", parameters: new Dictionary<string, string>() { ["param1"] = "123"});

        //To see list of supported APIs/Tasks check bellow
        //To see a custome behavior check SampleBehavior.cs file

        //-------------------------------END PROFILE-------------------------------
        return EndProfile();
    }
    //You can create methods to organize group of tasks
    private void SellBuyStuff1(int drinkVendor, int foodVendor, int repair)
    {
        var ME = ObjectManager.Instance.Player;
        SellItems(0, repair, "Sell Stuff", SellWhiteItems: true, SellGreenItems: true, SellTradeGoodItems: true);
        IF(() => (ME.Class == UnitClass.Paladin || ME.Class == UnitClass.Druid || ME.Class == UnitClass.Shaman || ME.Class == UnitClass.Priest || ME.Class == UnitClass.Warlock), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(0, drinkVendor, "159", 20, "Refreshing Spring Water");
        EndIF();
        IF(() => (ME.Class == UnitClass.Hunter || ME.Class == UnitClass.Rogue || ME.Class == UnitClass.Warlock), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(0, foodVendor, "4540", 20, "Tough Hunk of Bread");
        EndIF();
    }

    #region API/Tasks
    //For the sake of categorization, documentation for the groups of related behaviors are placed inside methods

    public void NavigationBehaviors()
    {
        //Description:
        //  Makes player follow the given waypoints. This behavior is useful when you want to record a path (i.e. gathering route) and make the character to traverse it.
        //Parameters:
        //  Mapid: Map of the waypoints, i.e. 530 - Outland
        //  QuestId: Quest id for this task. If greater than 0, then player only moves if has the quest and quest is not completed, else will return success and finishes.
        //  QuestName (optional): Set the quest name for logging and debugging purposes.
        //  Waypoints: path waypoints seperated with ','
        //  StartFromClosestWaypointToPlayer: If set to true, player follows the path from the closest waypoint to the player.
        //  IsFlyingPath: Set true to one are more recorded waypoints are in the air. i.e. a flying route
        //  CanUseMount: Set true to allow using a ground mount while moving.
        //  IgnoreCombat: Set true to ignore combat while moving to the destination.
        //  IgnoreCombatIfMounted: Set true to ignore combat if mounted while moving to the destination.
        //  AvoidEnemies: Set true to let the bot try to avoid enemies on the path.
        FollowPath(MapId: 530, QuestId: 9485, QuestName: "Taming the Beast", Waypoints: "(9039.843, -7456.657, 83.334),(9245.210, -7428.717, 35.168)", StartFromClosestWaypointToPlayer: false, IsFlyingPath:true);

        //Description:
        //  Moves the character to the last hotspot, starting from the closest hotspot to the player.
        //Parameters:
        //  Mapid: Map of the hotspot(s), i.e. 530 - Outland
        //  QuestId: Quest id for this task. If greater than 0, then player only moves if has the quest and quest is not completed, else will return success and finishes.
        //  QuestName (optional): Set the quest name for logging and debugging purposes.
        //  Hotspots: One are more hotspots seperated with ','
        //  MaxDistanceToHotspot: The distance that will be considered it is close enough to the hotspot and bot will stop moving. Don't set this value to numbers smaller than 1.0f
        //  CanFly: Set true to let character using flying if supported for this map.
        //  CanUseMount: Set true to allow using a ground mount while moving.
        //  IgnoreCombat: Set true to ignore combat while moving to the destination.
        //  IgnoreCombatIfMounted: Set true to ignore combat if mounted while moving to the destination.
        //  AvoidEnemies: Set true to let the bot try to avoid enemies on the path.
        MoveTo(MapId: 530, QuestId: 9485, QuestName: "Taming the Beast", Hotspots: "(9039.843, -7456.657, 83.334),(9245.210, -7428.717, 35.168)", MaxDistanceToHotspot: 4f);

        //Description:
        //  Moves the character to each hotspot defined in the 'Hotspots' starting from the first to the last.
        //Parameters:
        //  Mapid: Map of the hotspot(s), i.e. 571 - Northrend
        //  Hotspots: One are more hotspots seperated with ','
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  CloseEnoughDistance: The distance that will be considered it is close enough to the hotspot and bot will stop moving. Don't set this value to numbers smaller than 1.0f
        //  CanFly: Set true to let character using flying if supported for this map.
        //  CanUseMount: Set true to allow using a ground mount while moving.
        //  CanUseTaxi: Set true to allow taking taxis. Use this option if you know player character knows taxi paths.
        //  IgnoreCombat: Set true to ignore combat while moving to the destination.
        //  IgnoreCombatIfMounted: Set true to ignore combat if mounted while moving to the destination.
        //  AvoidEnemies: Set true to let the bot try to avoid enemies on the path.
        MoveTo(MapId: 571, Hotspots: "(9039.843, -7456.657, 83.334),(9245.210, -7428.717, 35.168)", TaskName: "Move to x", CloseEnoughDistance: 3f);

        //Description:
        //  Moves the character on the ground to each hotspot defined in the 'Hotspots' starting from the first to the last. 
        //Parameters:
        //  Mapid: Map of the hotspot(s), i.e. 571 - Northrend
        //  Hotspots: One are more hotspots seperated with ','
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  CloseEnoughDistance: The distance that will be considered it is close enough to the hotspot and bot will stop moving. Don't set this value to numbers smaller than 1.0f
        //  CanUseMount: Set true to allow using a ground mount while moving.
        //  CanUseTaxi: Set true to allow taking taxis. Use this option if you know player character knows taxi paths.
        //  IgnoreCombat: Set true to ignore combat while moving to the destination.
        //  IgnoreCombatIfMounted: Set true to ignore combat if mounted while moving to the destination.
        //  AvoidEnemies: Set true to let the bot try to avoid enemies on the path.
        RunTo(MapId: 571, Hotspots: "(9039.843, -7456.657, 83.334),(9245.210, -7428.717, 35.168)", TaskName: "Move to x", CloseEnoughDistance: 3f);

        //Description:
        //  Moves the character using flying mount to each hotspot defined in the 'Hotspots' starting from the first to the last. 
        //Parameters:
        //  Mapid: Map of the hotspot(s), i.e. 571 - Northrend
        //  Hotspots: One are more hotspots seperated with ','
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  CloseEnoughDistance: The distance that will be considered it is close enough to the hotspot and bot will stop moving. Don't set this value to numbers smaller than 1.0f
        //  CanUseMount: Set true to allow using a ground mount while moving.
        //  IgnoreCombat: Set true to ignore combat while moving to the destination.
        //  IgnoreCombatIfMounted: Set true to ignore combat if mounted while moving to the destination.
        //  AvoidEnemies: Set true to let the bot try to avoid enemies on the path.
        FlyTo(MapId: 571, Hotspots: "(9039.843, -7456.657, 83.334),(9245.210, -7428.717, 35.168)", TaskName: "Move to x", CloseEnoughDistance: 3f);

        //Description:
        //  Moves to the start Flight Master and takes taxi to the end Flight Master if player learned the taxi path.
        //Parameters:
        //  StartFlightMasterId: Start Flight Master NPC id
        //  EndFlightMasterId: End Flight Master NPC id
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  CloseEnoughDistance: The distance that will be considered it is close enough to the hotspot and bot will stop moving. Don't set this value to numbers smaller than 1.0f
        //  CanUseMount: Set true to allow using a ground mount while moving.
        //  CanUseFlyingMount: Set true to allow using a flying mount while moving.
        //  IgnoreCombat: Set true to ignore combat while moving to the destination.
        //  IgnoreCombatIfMounted: Set true to ignore combat if mounted while moving to the destination.
        //  AvoidEnemies: Set true to let the bot try to avoid enemies on the path.
        TakeTaxi(StartFlightMasterId: 17554, EndFlightMasterId: 17555, TaskName: "Fly Exodar");
    }

    public void QuestPickUpTurnIn()
    {
        //Description:
        //  Accepts the given quest from the given NPC. If player already accepted the quest or already completed it, this behavior does nothing.
        //Parameters:
        //  Mapid: Map of the hotspots, i.e. 530 - Outland
        //  QuestId: Quest id of the quest to pickup.
        //  EntryId: NPC id of the quest giver.
        //  QuestName (optional): Set the quest name for logging and debugging purposes.
        //  Hotspots: (optional) quest giver locations seperated by ','. If not given then quest giver location will be retrived from the Database.
        //  MinDistance: Min distance to the quest giver. Not used at the moment.
        //  MaxDistance: Max. distance to the quest giver before trying to interact with it.
        //  WaitTime: amount of time in milliseconds to wait after interacted with the quest giver. Must be a number betweem 500 to 900000
        PickUp(MapId: 0, QuestId: 7, EntryId: 197, QuestName: "Kobold Camp Cleanup");

        //Description:
        //  Turns in the given quest to the given NPC. If player dont have the quest this behavior does nothing. If this quest is not completed and
        //  the 'IgnoreCheck' parameter is not set, then this behacior will fail.
        //Parameters:
        //  Mapid: Map of the hotspots, i.e. 530 - Outland
        //  QuestId: Quest id of the quest to turnin.
        //  EntryId: NPC id of the quest giver.
        //  QuestName (optional): Set the quest name for logging and debugging purposes.
        //  IgnoreCheck: Set to true to ignore checking if player already completed this quest. It is useful for quests which requires nothing to do and does not show completed status.
        //  Hotspots: (optional) quest giver locations seperated by ','. If not given then quest giver location will be retrived from the Database.
        //  MinDistance: Min distance to the quest giver. Not used at the moment.
        //  MaxDistance: Max. distance to the quest giver before trying to interact with it.
        //  WaitTime: amount of time in milliseconds to wait after interacted with the quest giver. Must be a number betweem 500 to 900000
        TurnIn(MapId: 0, QuestId: 7, EntryId: 197, QuestName: "Kobold Camp Cleanup", IgnoreCheck: false);
    }

    public void GrindingAndKilling()
    {
        //Description:
        //  Kills the given NPCs until the given quest is completed. If player does not have the quest or this quest is already completed, this behavior ends.
        //Parameters:
        //  Mapid: Map of the quest, i.e. 530 - Outland
        //  QuestId: Quest id for this task. This behavior completes if player does not have this quest or quest is completed.
        //  MobIDs: ',' seperated list of NPCs to kill.
        //  QuestName (optional): Set the quest name for logging and debugging purposes.
        //  ObjectiveIndex: If greater than 0, then this behavior completes as soon as this objective is completed.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  MinHealthPercentBeforePull: Set the min. health percent player must have before trying to engage the enemies. 
        //  MinPowerPercentBeforePull: Set the min. power percent player must have before trying to engage the enemies. Only applicable for mana users.
        //  BlacklistTime: Amount of time in seconds to blacklist enemies if pulling failed.
        //  VisitOrder: The hotspot visit order logic. Either random or in order specified.
        //  AddHotspotsOnlyForFirstMob: if set to true, hotspots are only set (from the database) for the first given mob id. It is useful if you only want to go to the hotspots of the first NPC in the list.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  Safespots: If set, when player is in danger while fighting enemies, it will run to the closest spot in the list.
        //  PriorityTargets: ',' seperated list of high priority NPCs to kill.
        //  MinMobLevel: Min. mob level to pull.
        //  MaxMobLevel: Max. mob level to pull.
        //  LootMode: If set, bot will loot mobs that killed even if the used disabled looting in the settings.
        //  PullRange: Bot only pulls enemies within this range.
        //  HotSpotRange: Bot only starts searching for enemies when any hotspot distance to the player is less than this value.
        //  IgnoreCombat: Set true to ignore combat while not in range of the hotspots.
        //  IgnoreCombatIfMounted: Set true to ignore combat while mounted and not in the range of hotspots.
        //  AvoidEnemies: Set true to let the bot try avoiding enemies while not in the range of hotspots.
        KillMobs(MapId: 0, QuestId: 33, MobIDs: "299,69", QuestName: "Wolves Across the Border", ObjectiveIndex: 1, LootMobs: true, PullRange: 85, HotSpotRange: 55);

        //Description:
        //  Grinds until player level is reached.
        //Parameters:
        //  Mapid: Map of the hotspots, i.e. 530 - Outland
        //  PlayerLevelReached: Grinds until player reached this level. Floating values can be used to indicate level fractions.
        //  MobIDs: ',' seperated list of NPCs to grind.
        //  MinPlayerLevel: Min. level the player must have to do this task.
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  KillAllMobs: If set then player will grind any mob in pull range.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  MinHealthPercentBeforePull: Set the min. health percent player must have before trying to engage the enemies. 
        //  MinPowerPercentBeforePull: Set the min. power percent player must have before trying to engage the enemies. Only applicable for mana users.
        //  BlacklistTime: Amount of time in seconds to blacklist enemies if pulling failed.
        //  VisitOrder: The hotspot visit order logic. Either random or in order specified.
        //  AddHotspotsOnlyForFirstMob: if set to true, hotspots are only set (from the database) for the first given mob id. It is useful if you only want to go to the hotspots of the first NPC in the list.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  Safespots: If set, when player is in danger while fighting enemies, it will run to the closest spot in the list.
        //  PriorityTargets: ',' seperated list of high priority NPCs to kill.
        //  MinMobLevel: Min. mob level to pull.
        //  MaxMobLevel: Max. mob level to pull.
        //  LootMobs: If set, bot will loot mobs that killed even if the used disabled looting in the settings.
        //  PullRange: Bot only pulls enemies within this range.
        //  HotSpotRange: Bot only starts searching for enemies when any hotspot distance to the player is less than this value.
        //  IgnoreCombat: Set true to ignore combat while not in range of the hotspots.
        //  IgnoreCombatIfMounted: Set true to ignore combat while mounted and not in the range of hotspots.
        //  AvoidEnemies: Set true to let the bot try avoiding enemies while not in the range of hotspots.
        GrindMobsUntil(MapId: 0, PlayerLevelReached: 4.5f, MobIDs: "299,69", TaskName: "Grind to level 4.5", KillAllMobs: false, LootMobs: false, PullRange: 85, HotSpotRange: 55);

        //Description:
        //  Grinds until the given items are collected.
        //Parameters:
        //  Mapid: Map of the hotspots, i.e. 530 - Outland
        //  ItemsCollected: Grinds until given items are collected. Format is "itemID:Count" and list of items are seperated with ','
        //  MobIDs: ',' seperated list of NPCs to grind.
        //  MinPlayerLevel: Min. level the player must have to do this task.
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  KillAllMobs: If set then player will grind any mob in pull range.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  MinHealthPercentBeforePull: Set the min. health percent player must have before trying to engage the enemies. 
        //  MinPowerPercentBeforePull: Set the min. power percent player must have before trying to engage the enemies. Only applicable for mana users.
        //  BlacklistTime: Amount of time in seconds to blacklist enemies if pulling failed.
        //  VisitOrder: The hotspot visit order logic. Either random or in order specified.
        //  AddHotspotsOnlyForFirstMob: if set to true, hotspots are only set (from the database) for the first given mob id. It is useful if you only want to go to the hotspots of the first NPC in the list.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  Safespots: If set, when player is in danger while fighting enemies, it will run to the closest spot in the list.
        //  PriorityTargets: ',' seperated list of high priority NPCs to kill.
        //  MinMobLevel: Min. mob level to pull.
        //  MaxMobLevel: Max. mob level to pull.
        //  LootMobs: If set, bot will loot mobs that killed even if the used disabled looting in the settings.
        //  PullRange: Bot only pulls enemies within this range.
        //  HotSpotRange: Bot only starts searching for enemies when any hotspot distance to the player is less than this value.
        //  IgnoreCombat: Set true to ignore combat while not in range of the hotspots.
        //  IgnoreCombatIfMounted: Set true to ignore combat while mounted and not in the range of hotspots.
        //  AvoidEnemies: Set true to let the bot try avoiding enemies while not in the range of hotspots.
        GrindMobsUntil(MapId: 0, ItemsCollected: "1234:5,4213:9", MobIDs: "299,69", TaskName: "Grind to level 4.5", KillAllMobs: false, LootMobs: false, PullRange: 85, HotSpotRange: 55);

        //Description:
        //  Grinds for the given amount of time.
        //Parameters:
        //  Mapid: Map of the hotspots, i.e. 530 - Outland
        //  TimePassed: Time in seconds to grind. Floating values can be used to indicate seconds fractions.
        //  MobIDs: ',' seperated list of NPCs to grind.
        //  MinPlayerLevel: Min. level the player must have to do this task.
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  KillAllMobs: If set then player will grind any mob in pull range.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  MinHealthPercentBeforePull: Set the min. health percent player must have before trying to engage the enemies. 
        //  MinPowerPercentBeforePull: Set the min. power percent player must have before trying to engage the enemies. Only applicable for mana users.
        //  BlacklistTime: Amount of time in seconds to blacklist enemies if pulling failed.
        //  VisitOrder: The hotspot visit order logic. Either random or in order specified.
        //  AddHotspotsOnlyForFirstMob: if set to true, hotspots are only set (from the database) for the first given mob id. It is useful if you only want to go to the hotspots of the first NPC in the list.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  Safespots: If set, when player is in danger while fighting enemies, it will run to the closest spot in the list.
        //  PriorityTargets: ',' seperated list of high priority NPCs to kill.
        //  MinMobLevel: Min. mob level to pull.
        //  MaxMobLevel: Max. mob level to pull.
        //  LootMobs: If set, bot will loot mobs that killed even if the used disabled looting in the settings.
        //  PullRange: Bot only pulls enemies within this range.
        //  HotSpotRange: Bot only starts searching for enemies when any hotspot distance to the player is less than this value.
        //  IgnoreCombat: Set true to ignore combat while not in range of the hotspots.
        //  IgnoreCombatIfMounted: Set true to ignore combat while mounted and not in the range of hotspots.
        //  AvoidEnemies: Set true to let the bot try avoiding enemies while not in the range of hotspots.
        GrindMobsUntil(MapId: 0, TimePassed: 120.5f, MobIDs: "299,69", TaskName: "Grind to level 4.5", KillAllMobs: false, LootMobs: false, PullRange: 85, HotSpotRange: 55);

        //Description:
        //  Grinds until certain number of mobs are pulled.
        //Parameters:
        //  Mapid: Map of the hotspots, i.e. 530 - Outland
        //  MobsKilled: Number of mob to pull for this task.
        //  MobIDs: ',' seperated list of NPCs to grind.
        //  MinPlayerLevel: Min. level the player must have to do this task.
        //  TaskName (optional): Set the task name for logging and debugging purposes.
        //  KillAllMobs: If set then player will grind any mob in pull range.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  MinHealthPercentBeforePull: Set the min. health percent player must have before trying to engage the enemies. 
        //  MinPowerPercentBeforePull: Set the min. power percent player must have before trying to engage the enemies. Only applicable for mana users.
        //  BlacklistTime: Amount of time in seconds to blacklist enemies if pulling failed.
        //  VisitOrder: The hotspot visit order logic. Either random or in order specified.
        //  AddHotspotsOnlyForFirstMob: if set to true, hotspots are only set (from the database) for the first given mob id. It is useful if you only want to go to the hotspots of the first NPC in the list.
        //  Hotspots: (optional) mob locations seperated by ','. If not given then mob locations will be retrived from the Database.
        //  Safespots: If set, when player is in danger while fighting enemies, it will run to the closest spot in the list.
        //  PriorityTargets: ',' seperated list of high priority NPCs to kill.
        //  MinMobLevel: Min. mob level to pull.
        //  MaxMobLevel: Max. mob level to pull.
        //  LootMobs: If set, bot will loot mobs that killed even if the used disabled looting in the settings.
        //  PullRange: Bot only pulls enemies within this range.
        //  HotSpotRange: Bot only starts searching for enemies when any hotspot distance to the player is less than this value.
        //  IgnoreCombat: Set true to ignore combat while not in range of the hotspots.
        //  IgnoreCombatIfMounted: Set true to ignore combat while mounted and not in the range of hotspots.
        //  AvoidEnemies: Set true to let the bot try avoiding enemies while not in the range of hotspots.
        GrindMobsUntil(MapId: 0, MobsKilled: 120, MobIDs: "299,69", TaskName: "Grind to level 4.5", KillAllMobs: false, LootMobs: false, PullRange: 85, HotSpotRange: 55);
    }

    public void InteractBehaviors()
    {
        //BEHAVIOR Description:
        //  Interacts with the given object(s).
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used. must the the first parameter of behavior
        //              Example InteractWithObject(1, x, x, "Text");  1 is the id of Map = Kalindor
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. must the the second parameter of behavior. Must be value 0 if NumTimes is used
        //              Example InteractWithObject(x, 4402, x, "text");  4402 is the id of Quest, behavior consider its done when quest id its completed 
        //          ObjectIds
        //              REQUIRED
        //              Identifies the object or objects wich behavior will interact. must the the third parameter of behavior.
        //              Can be just one or multiple example "123" "123,456,789"
        //              Example InteractWithObject(x, x, "171938", "text");  171938 is the if of object to interact
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running . must the the fourth parameter of behavior or be used with QuestName: "Quest Name"
        //              Example InteractWithObject(x, x, x, "Galgar's Cactus Apple Surprise");  "Galgar's Cactus Apple Surprise" is the text to show in logs
        //          NumTimes 
        //              [optional; Default: 0]  REQUIRED if QuestId=0
        //              Identifies attempts at interacting with diferent objects before the behavior consider its done. used with Numtimes: parameter
        //              Example InteractWithObject(x, x, x, "Text", NumTimes: 4);  bot will interact with 4 gameobjects with id = x then consider its done
        //          ObjectiveIndex
        //              [optional; Default: ignored]
        //              Identifies the objective Index for  a quest, behavior consider its done when ObjectivIndex for QuestID its completed . used with ObjectiveIndex: parameter
        //              Example InteractWithObject(x, x, x, "Text", ObjectiveIndex: 2);  bot will interact with gameobjects with id = x until ObjectIndex for quest x is completed
        //          MinDistance
        //              [optional; Default: 0]
        //              Identifies the minimum distance to interact with objects. used with MinDistance: parameter
        //              Example InteractWithObject(x, x, x, "Text", MinDistance: 1);  bot will move to a minimum distance before interact with gameobjects with id = x
        //          MaxDistance
        //              [optional; Default: 5]
        //              Identifies the maximum distance to interact with objects. used with MaxDistance: parameter
        //              Example InteractWithObject(x, x, x, "Text", MaxDistance: 4);  bot will move to a maximum distance before interact with gameobjects with id = x
        //          CollectionDistance
        //              [optional; Default: 100.0 ]
        //              Specifies the maximum distance that should be searched when looking for a viable Object with which to interact. used with CollectionDistance: parameter
        //              Example InteractWithObject(x, x, x, "Text", CollectionDistance: 50.0);  bot will only look for objects near 50 yards from player
        //          WaitTime
        //              [optional; Default: 500ms ]
        //              Identifies the time to wait after interact with objects. used with WaitTime: parameter
        //              Example InteractWithObject(x, x, x, "Text", WaitTime: 4000);  bot will wait 4000ms = 4 seconds after interact with gameobjects with id = x
        //          BlacklistTime
        //              [optional; Default: 180 seconds]
        //              Identifies the time for blacklist objects after interact. used with BlacklistTime: parameter
        //              Example InteractWithObject(x, x, x, "Text", BlacklistTime: 60);  bot will interact with object and blacklist it for 60 seconds
        //           MeHaveAura
        //              [optional; Default: 0 ]
        //              Terminate behavior if player dont have aura MeHaveAura. used with MeHaveAura: parameter
        //              Example InteractWithObject(x, x, x, "Text", MeHaveAura: 9060);  bot will terminate behavior if player dont have aura id 9060, 0 default mean behavior ignore check for aura
        //           MeMissAura
        //              [optional; Default: 0 ]
        //              Terminate behavior if player have aura MeMissAura. used with MeMissAura: parameter
        //              Example InteractWithObject(x, x, x, "Text", MeMissAura: 9060);  bot will terminate behavior if player have aura id 9060, 0 default mean behavior ignore check for aura
        //          VisitHotspots
        //              [optional; Default: HotspotVisitOrder.Order]
        //              Identifies the strategy that bot use to visit each waypoint. used VisitHotspots: parameter
        //              Example InteractWithObject(x, x, x, "Text", VisitHotspots: HotspotVisitOrder.Order);  options: HotspotVisitOrder.Order, HotspotVisitOrder.Random
        //          WaitForRespaw
        //              [optional; Default: true]
        //              Identifies the strategy that bot use to wait for respaw or terminate behavior. used WaitForRespaw: parameter. only works with 1 hotspot
        //              Example InteractWithObject(x, x, x, "Text", WaitForRespaw: false);  if theres is no object will terminate behavior
        //          DisableFlags
        //              [optional; Default: ""]
        //              Not Implemented
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit. used with Hotspots: parameter
        //              Example InteractWithObject(x, x, x, "Text", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given ObjectId
        InteractWithObject(MapId: 0, QuestId: 3904, ObjectIds: "161557", QuestName: "Milly's Harvest");

        //BEHAVIOR Description:
        //  Interacts with the given NPC(s).
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used. must the the first parameter of behavior
        //              Example InteractWithNpc(1, x, x, "Text");  1 is the id of Map = Kalindor
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. must the the second parameter of behavior. Must be value 0 if NumTimes is used
        //              Example InteractWithNpc(x, 4402, x, "text");  4402 is the id of Quest, behavior consider its done when quest id its completed 
        //          MobId
        //              REQUIRED
        //              Identifies the npc wich behavior will interact. must the the third parameter of behavior.
        //              Example InteractWithNpc(x, x, 1719, "text");  1719 is the id of Npc to interact
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running . must the the fourth parameter of behavior or be used with QuestName: "Quest Name"
        //              Example InteractWithNpc(x, x, x, "Galgar's Cactus Apple Surprise");  "Galgar's Cactus Apple Surprise" is the text to show in logs
        //          NumTimes 
        //              [optional; Default: 0]  REQUIRED if QuestId=0
        //              Identifies attempts at interacting with diferent npcs before the behavior consider its done. used with Numtimes: parameter
        //              Example InteractWithNpc(x, x, x, "Text", NumTimes: 4);  bot will interact 4 times with Npc with id = x then consider its done
        //          ObjectiveIndex
        //              [optional; Default: ignored]
        //              Identifies the objective Index for  a quest, behavior consider its done when ObjectivIndex for QuestID its completed . used with ObjectiveIndex: parameter
        //              Example InteractWithNpc(x, x, x, "Text", ObjectiveIndex: 2);  bot will interact with Npc with id = x until ObjectiveIndex 2 for quest x is completed
        //          MinDistance
        //              [optional; Default: 0]
        //              Identifies the minimum distance to interact with npcs. used with MinDistance: parameter
        //              Example InteractWithNpc(x, x, x, "Text", MinDistance: 1);  bot will move to a minimum distance before interact with npc with id = x
        //          MaxDistance
        //              [optional; Default: 5]
        //              Identifies the maximum distance to interact with npcs. used with MaxDistance: parameter
        //              Example InteractWithNpc(x, x, x, "Text", MaxDistance: 4);  bot will move to a maximum distance before interact with npc with id = x
        //          CollectionDistance
        //              [optional; Default: 100.0 ]
        //              Specifies the maximum distance that should be searched when looking for viable Objects to interact with. used with CollectionDistance: parameter
        //              Example InteractWithNpc(x, x, x, "Text", CollectionDistance: 50.0);  bot will only look for objects in 50 yards range from player
        //          WaitTime
        //              [optional; Default: 500ms ]
        //              Identifies the time to wait after interact with npcs. used with WaitTime: parameter
        //              Example InteractWithNpc(x, x, x, "Text", WaitTime: 4000);  bot will wait 4000ms = 4 seconds after interact with npc with id = x
        //          GossipOptions
        //              [optional; Default: "" ]
        //              Identifies the gossip action to send after interact with NPC. used with GossipOptions: "some gossips here, separated by ,"
        //              Example InteractWithNpc(x, x, x, "Text", GossipOptions: "GossipTitleButton1:Click(),GossipTitleButton1:Click()");  bot click first Gossip, wait 1,5 secs then click again the first Gossip option in the next page
        //          BlacklistTime
        //              [optional; Default: 180 seconds]
        //              Identifies the time for blacklist npcs after interact. used with BlacklistTime: parameter
        //              Example InteractWithNpc(x, x, x, "Text", BlacklistTime: 60);  bot will interact with npc and blacklist it for 60 seconds. This NPC wont be used as a target of interaction for 60 seconds.
        //           MeHaveAura
        //              [optional; Default: 0 ]
        //              Terminate behavior if player dont have aura MeHaveAura. used with MeHaveAura: parameter
        //              Example InteractWithObject(x, x, x, "Text", MeHaveAura: 9060);  bot will terminate behavior if player dont have aura id 9060, 0 default mean behavior ignore check for aura
        //           MeMissAura
        //              [optional; Default: 0 ]
        //              Terminate behavior if player have aura MeMissAura. used with MeMissAura: parameter
        //           MobHaveAuraId
        //              [optional; Default: 0 ]
        //              Only select Mobs if have specified aura. used with MobHaveAuraId: parameter
        //              Example InteractWithNpc(x, x, x, "Text", MobHaveAura: 9060);  bot will only select NPCs with that aura, 0 default mean behavior ignore check for aura
        //           MobMissAuraId
        //              [optional; Default: 0 ]
        //              Ignoring Mobs if have specified aura. used with MobMissAuraId: parameter
        //              Example InteractWithNpc(x, x, x, "Text", MobMissAura: 9060);  bot will ignore mobs with  that aura, 0 default mean behavior ignore check for aura
        //          VisitHotspots
        //              [optional; Default: HotspotVisitOrder.Order]
        //              Identifies the strategy that bot use to visit each waypoint. used VisitHotspots: parameter
        //              Example InteractWithNpc(x, x, x, "Text", VisitHotspots: HotspotVisitOrder.Order);  options: HotspotVisitOrder.Order, HotspotVisitOrder.Random
        //          WaitForRespaw
        //              [optional; Default: true]
        //              Identifies the strategy that bot use to wait for respawn or terminate the behavior. Used WaitForRespaw: parameter. only works with 1 hotspot
        //              Example InteractWithNpc(x, x, x, "Text", WaitForRespaw: false);  if theres is no npc then will terminate the behavior
        //          DisableFlags
        //              [optional; Default: ""]
        //              Not Implemented
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit. used with Hotspots: parameter
        //              Example InteractWithNpc(x, x, x, "Text", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given MobId
        InteractWithNpc(MapId: 1, QuestId: 0, MobId: 3841, QuestName: "Auberdine FP", NumTimes: 1);

        //BEHAVIOR Description:
        //  Interacts once with the first nearby object found or waits for objects to respawn. This behavior is useful when just want to interact with an object close to the player. 
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. 
        //              Example InteractWithObject(x, 4402, x, "text");  4402 is the id of Quest, behavior consider its done when quest id its completed 
        //          ObjectIds
        //              REQUIRED
        //              Identifies the object wich behavior will interact. 
        //              Example JustInteract(x, 171938, "text");  171938 is the if of object to interact
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running. 
        //              Example JustInteract(x, x, "Galgar's Cactus Apple Surprise");  "Galgar's Cactus Apple Surprise" is the text to show in logs
        JustInteract(QuestId: 8345, ObjectIds: "180516", QuestName: "The Shrine of Dath'Remar");
    }

    public void Run()
    {
        //BEHAVIOR Description:
        //  Runs the given macro in game.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. 
        //              Example RunMacro(4402, "/run ...");  4402 is the id of Quest, behavior considered is done when quest id its completed 
        //          Macro
        //              REQUIRED
        //              Macro to run.
        //              Example RunMacro(4402, "/dance");  171938 is the if of object to interact
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running. 
        //              Example RunMacro(9486, "/Run PetDismiss();", "Dismiss the pet!");  "Dismiss the pet!" is the text to show in logs
        RunMacro(9486, "/Run PetDismiss();", "Dismiss the pet!");

        //Description:
        //  Performs an action. Can be used to set parameters such as dynamic settings. Async methods should not be called here.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          Action
        //              REQUIRED 
        //              The action to run when running this script in profile.
        //          ActionName
        //              [optional; Default: ""]
        //              Identifies the text of this task to show in logs when running. 
        //              Example RunAction(() => BottingSessionManager.Instance.DynamicSettings.CombatDisabled = false, "Re enable combat");  "Diasble combat behavior" is the text to show in logs
        RunAction(() => BottingSessionManager.Instance.DynamicSettings.CombatDisabled = true, ActionName: "Diasble combat behavior");
    
        //Description:
        //  Performs an async task. Can be used to call async bot methods.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          Task
        //              REQUIRED 
        //              The async task to run when running this script in profile. 
        //              Example RunTask(4402, "/run ...");  4402 is the id of Quest, behavior considered is done when quest id its completed 
        //          TaskName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running. 
        RunTask(Task.Delay(5000), TaskName: "Wait 5 seconds");
    }
#endregion



}
