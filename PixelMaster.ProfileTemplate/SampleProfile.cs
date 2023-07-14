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
        new Blackspot{Position= new Vector3(-2774.54f, -703.32f, 5.86f), MapID= 1, Radius=1f},//too many mobs bot die alot
    };
    /// <summary>
    /// Ignores mobs and gathering nodes in this area. It is useful to make  bot ignore some gathering nodes in unwanted areas
    /// </summary>
    List<Blackspot> ignoredAreas = new List<Blackspot>()
    {
                new Blackspot{Position= new Vector3(-2774.54f, -703.32f, 5.86f), MapID= 1, Radius=20f},//nodes are unreachable here
    };
    /// <summary>
    /// Define object IDs of wanted objects around the map. Bot will try to interact with any object in this list found nearby the player
    /// </summary>
    List<int> wantedObjects = new List<int>()
    {
        1234,
        5678,
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
            IgnoredAreas = ignoredAreas,//sets to the list defined above
            Mailboxes = mailboxes,  //sets to the list defined above
            Vendors = vendors,      //sets to the list defined above
            WantedObjects = wantedObjects, //sets to the list defined above
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
            //Restocking
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
        //  IgnoreCheck: Set to true to ignore checking if player already completed this quest. It is useful for repeatable quests.
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
        //  MaxPullCount: [optional; Default: 1] Bot will try to choose targets that will not cause more enemies than this value to be pulled.  
        //  CanFly: [optional; Default: true] Set true to let character using flying while moving and flying is supported in this map.
        //  CanUseMount: [optional; Default: true] Set true to allow using a ground mount while moving.
        //  CanUseTaxi: [optional; Default: true] Set true to allow taking taxis while moving to locations. Bot assumes player does know the taxi paths. 
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
        //  MaxPullCount: [optional; Default: 1] Bot will try to choose targets that will not cause more enemies than this value to be pulled.  
        //  CanFly: [optional; Default: true] Set true to let character using flying while moving and flying is supported in this map.
        //  CanUseMount: [optional; Default: true] Set true to allow using a ground mount while moving.
        //  CanUseTaxi: [optional; Default: true] Set true to allow taking taxis while moving to locations. Bot assumes player does know the taxi paths. 
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
        //  MaxPullCount: [optional; Default: 1] Bot will try to choose targets that will not cause more enemies than this value to be pulled.  
        //  CanFly: [optional; Default: true] Set true to let character using flying while moving and flying is supported in this map.
        //  CanUseMount: [optional; Default: true] Set true to allow using a ground mount while moving.
        //  CanUseTaxi: [optional; Default: true] Set true to allow taking taxis while moving to locations. Bot assumes player does know the taxi paths. 
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
        //  MaxPullCount: [optional; Default: 1] Bot will try to choose targets that will not cause more enemies than this value to be pulled.  
        //  CanFly: [optional; Default: true] Set true to let character using flying while moving and flying is supported in this map.
        //  CanUseMount: [optional; Default: true] Set true to allow using a ground mount while moving.
        //  CanUseTaxi: [optional; Default: true] Set true to allow taking taxis while moving to locations. Bot assumes player does know the taxi paths. 
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
        //  MaxPullCount: [optional; Default: 1] Bot will try to choose targets that will not cause more enemies than this value to be pulled.  
        //  CanFly: [optional; Default: true] Set true to let character using flying while moving and flying is supported in this map.
        //  CanUseMount: [optional; Default: true] Set true to allow using a ground mount while moving.
        //  CanUseTaxi: [optional; Default: true] Set true to allow taking taxis while moving to locations. Bot assumes player does know the taxi paths. 
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
        //              Example InteractWithObject(x, x, "171938", "text");  171938 is the id of object to interact
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
        //              Example InteractWithNpc(x, x, x, "Text", GossipOptions: "SelectGossipOption(1),SelectGossipOption(1)");  bot click first Gossip, wait 1,5 secs then click again the first Gossip option in the next page
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
        //              Example JustInteract(x, 4402, x, "text");  4402 is the id of Quest, behavior consider its done when quest id its completed 
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

    public void RunScript()
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

    public void Train()
    {
        //BEHAVIOR Description:
        //  Trains the player with the given spells. Can be class spells or other skills i.e. professions
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //              Example InteractWithObject(1, x, x, "Text");  1 is the id of Map = Kalindor
        //          TrainerID
        //              REQUIRED
        //              Trainer NPC Id.
        //              Example TrainSkill(1, 16272, "13163");  16272 is the id of the trainer NPC.
        //          SpellINames
        //              REQUIRED
        //              Spell Names to learn seperated by ';'
        //              Example TrainSkill(1, 7088, "Apprentice Skinner", TrainerName: "Thuwd", TaskName: "Train Apprentice Skinning"); 7088 is trainer ID and 'Apprentice Skinner' is the skill to train
        //              TrainSkill(1, x, "Blizzard(Rank 1)"); For spells with subnames, subname should be put inside '()' immediately after spell name. i.e. Parry(Passive)
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          TrainerName
        //              [optional; Default: ""]
        //              Name of the trainer NPC
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit. used with Hotspots: parameter
        //              Example TrainSkill(x, x, x, "", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given trainer
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while going to spots if supported for this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving to spots.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to the fishing spots.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to the fishing spots.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        TrainSkill(530, 16272,  "13163,1978,3044,1130,5116,14280,3127,13165,13549,14281,20736");
    }

    public void Use()
    {
        //BEHAVIOR Description:
        //  Uses the given item. This behavior can be used to use an item at the given position or on a NPC or just at player position.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //              Example UseItem(1, x, x, x);  1 is the id of Map = Kalindor
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. Must be value 0 if NumTimes is used
        //              Example UseItem(x, 9684, x, x, x);  9684 is the id of Quest, behavior consider its done when quest with this id is completed or player dont have this quest
        //          ItemName
        //              REQUIRED
        //              Name of the item to use.
        //              Example UseItem(x, x, "Shimmering Vessel", x);,  'Shimmering Vessel' is the name of the item to use.
        //          MobId
        //              [optional; Default: 0]
        //              Id of the NPC to use item on. This value is required when TargetMethod is TargettingMethod.MOB or TargettingMethod.MOB_POSITION
        //              Example UseItem(x, x, x, 15274, TargetMethod: TargettingMethod.MOB);,  '15274' is the id of the mob to use item on.
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running . It is the fifth parameter of the behavior or can be used as QuestName: "Quest Name"
        //              Example UseItem(x, x, x, QuestName: "Claiming the Light");  "Claiming the Light" is the text to show in logs
        //          NumTimes 
        //              [optional; Default: 0]  REQUIRED if QuestId=0
        //              Identifies number of times item must be used before behavior is considered done.
        //              Example UseItem(x, x, x, "x", NumTimes: 4);  bot will use the item 4 times then consider it is done.
        //          ObjectiveIndex
        //              [optional; Default: 0]
        //              Identifies the objective Index for the quest and behavior is considered done when this objective of the quest is completed.
        //              It will be ignored if 'NumTimes' is a non-zero value
        //              Example UseItem(x, x, x, x, ObjectiveIndex: 2);  behavior will finish as soon as objective index 2 of the quest is completed.
        //          MinDistance
        //              [optional; Default: 0]
        //              Identifies the minimum distance to the target before using the item.
        //          MaxDistance
        //              [optional; Default: 5]
        //              Identifies the maximum distance to the target before using the item.
        //              Example UseItem(x, x, x, x, MaxDistance: 4);  bot will make sure distance to the target is closer than this distance before trying to use the item.
        //          MobHpPercentLeft
        //              [optional; Default: 100.0 ]
        //              Specifies the maximum HP the target mob must have before it is considered a valid target for item use.
        //              Example UseItem(x, x, x, x, MobHpPercentLeft: 50.0);  bot will only use item on a mob with Health percent less than or equal to 50
        //          MobState
        //              [optional; Default: Alive ]
        //              When targetting a mob for item usage, then this value identifies the state a mob must have to be considered a valid target.
        //              MobStateType.Alive - Means mob must be alive
        //              MobStateType.AliveNotInCombat - Means mob must be alive and not in combat.
        //              AliveNotInCombat.Dead - Means mob must be dead.
        //              AliveNotInCombat.DontCare - Means mob state does not matter.
        //          TargetMethod
        //              [optional; Default: MOB ]
        //              This value identifies the target of the item usage.
        //              TargettingMethod.None - Means just use the item where the player is.
        //              TargettingMethod.GROUND - Means use the item on a ground position. i.e. on a fire on a building
        //              TargettingMethod.MOB - Means use the item on a mob by targetting it. In this case 'MobId' must have been set.
        //              TargettingMethod.MOB_POSITION - Means go to the mob position and use the item. In this case 'MobId' must have been set.
        //              TargettingMethod.POSITION - Maans go to the position and then use the item. In this case hotspot must have been set. 
        //          WaitTime
        //              [optional; Default: 500ms ]
        //              Identifies the time to wait after item has been used.
        //              Example UseItem(x, x, x, x, WaitTime: 4000);  bot will wait 4000ms = 4 seconds after item has been used.
        //          BlacklistTime
        //              [optional; Default: 180 seconds]
        //              Identifies the time for blacklist mobs after item is used.
        //              Example UseItem(x, x, x, x, BlacklistTime: 60);  bot will use the item on a mob and blacklist it for 60 seconds.
        //           MobHavedAuraId
        //              [optional; Default: 0 ]
        //              If set to a value other than zero, then only mobs that have this aura are considered valid item targets.
        //           MobMissdAuraId
        //              [optional; Default: 0 ]
        //              If set to a value other than zero, then only mobs that do not have this aura are considered valid item targets.
        //          VisitOrder
        //              [optional; Default: HotspotVisitOrder.Order]
        //              Identifies the strategy that bot use to visit each waypoint.
        //              HotspotVisitOrder.Order - Player will go to hotspots in order they are defined.
        //              HotspotVisitOrder.Random - Player will go to hotspots randomly.
        //          IsChanneling
        //              [optional; Default: false]
        //              Identifies the item effect is a channeling spell and player is expected to channel it for a time equal to 'WaitTime' parameter.
        //              If player is not channeling in 'WaitTime' duration, bot will use item again.
        //          DisableFlags
        //              [optional; Default: ""]
        //              Options:
        //                  "Combat" - if this flag is set then bot will disable combat right before using the item.
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit.
        //              Example InteractWithObject(x, x, x, "Text", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given Mob Id
        UseItem(530, 9684, "Shimmering Vessel", QuestName: "Claiming the Light", TargetMethod: TargettingMethod.POSITION, WaitTime: 10000, Hotspots: "(9851.299, -7522.248, -9.15)");
    }

    public void Cast()
    {
        //BEHAVIOR Description:
        //  Casts the requested spell. This behavior can be used cast a spell at the given position or on a NPC or just at player position.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //              Example CastSpell(1, x, x, x);  1 is the id of Map = Kalindor
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. Must be value 0 if NumTimes is used
        //              Example CastSpell(x, 9684, x, x, x);  9684 is the id of Quest, behavior consider its done when quest with this id is completed or player dont have this quest
        //          SpellName
        //              REQUIRED
        //              Name of the spell to cast.
        //              Example CastSpell(x, x, "Arcane Torrent", x);,  'Arcane Torrent' is the name of the spell.
        //          MobId
        //              [optional; Default: 0]
        //              Id of the NPC to cast spell on. This value is required when TargetMethod is TargettingMethod.MOB or TargettingMethod.MOB_POSITION
        //              Example CastSpell(x, x, x, 15274, TargetMethod: TargettingMethod.MOB);,  '15274' is the id of the mob to cast spell at.
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running . It is the fifth parameter of the behavior or can be used as QuestName: "Quest Name"
        //              Example CastSpell(x, x, x, QuestName: "Claiming the Light");  "Claiming the Light" is the text to show in logs
        //          NumTimes 
        //              [optional; Default: 0]  REQUIRED if QuestId=0
        //              Identifies number of times spell must be cast before behavior is considered done.
        //              Example CastSpell(x, x, x, "x", NumTimes: 4);  bot will cast the spell 4 times then consider it is done.
        //          ObjectiveIndex
        //              [optional; Default: 0]
        //              Identifies the objective Index for the quest and behavior is considered done when this objective of the quest is completed.
        //              It will be ignored if 'NumTimes' is a non-zero value
        //              Example CastSpell(x, x, x, x, ObjectiveIndex: 2);  behavior will finish as soon as objective index 2 of the quest is completed.
        //          MinDistance
        //              [optional; Default: 0]
        //              Identifies the minimum distance to the target before casting the spell.
        //          MaxDistance
        //              [optional; Default: 5]
        //              Identifies the maximum distance to the target before casting the spell.
        //              Example CastSpell(x, x, x, x, MaxDistance: 4);  bot will make sure distance to the target is closer than this distance before trying to cast the spell.
        //          MobHpPercentLeft
        //              [optional; Default: 100.0 ]
        //              Specifies the maximum HP the target mob must have before it is considered a valid target for item use.
        //              Example CastSpell(x, x, x, "Text", MobHpPercentLeft: 90.5f);  bot will cast spell at mob id which HP is percent 90.5 or lower.
        //          MobState
        //              [optional; Default: Alive ]
        //              When targetting a mob for spell cast, then this value identifies the state a mob must have to be considered a valid target.
        //              MobStateType.Alive - Means mob must be alive
        //              MobStateType.AliveNotInCombat - Means mob must be alive and not in combat.
        //              AliveNotInCombat.Dead - Means mob must be dead.
        //              AliveNotInCombat.DontCare - Means mob state does not matter.
        //          TargetMethod
        //              [optional; Default: MOB ]
        //              This value identifies the target of the spell cast.
        //              TargettingMethod.None - Means just cast the spell where the player is.
        //              TargettingMethod.GROUND - Means cast the spell on a ground position. i.e. on a fire on a building
        //              TargettingMethod.MOB - Means cast the spell on a mob by targetting it. In this case 'MobId' must have been set.
        //              TargettingMethod.MOB_POSITION - Means go to the mob position and cast the spell. In this case 'MobId' must have been set.
        //              TargettingMethod.POSITION - Maans go to the position and then cast the spell. In this case a hotspot must have been set. 
        //          WaitTime
        //              [optional; Default: 500ms ]
        //              Identifies the time to wait after spell has been casted.
        //              Example CastSpell(x, x, x, x, WaitTime: 4000);  bot will wait 4000ms = 4 seconds after spell has been casted.
        //          BlacklistTime
        //              [optional; Default: 180 seconds]
        //              Identifies the time for blacklist mobs after spell is casted.
        //              Example CastSpell(x, x, x, x, BlacklistTime: 60);  bot will cast the spell on a mob and blacklist it for 60 seconds.
        //           MobHavedAuraId
        //              [optional; Default: 0 ]
        //              If set to a value other than zero, then only mobs that have this aura are considered valid spell targets.
        //           MobMissdAuraId
        //              [optional; Default: 0 ]
        //              If set to a value other than zero, then only mobs that do not have this aura are considered valid spell targets.
        //          VisitOrder
        //              [optional; Default: HotspotVisitOrder.Order]
        //              Identifies the strategy that bot use to visit each waypoint.
        //              HotspotVisitOrder.Order - Player will go to hotspots in order they are defined.
        //              HotspotVisitOrder.Random - Player will go to hotspots randomly.
        //          DisableFlags
        //              [optional; Default: ""]
        //              Not Implemented
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit.
        //              Example InteractWithObject(x, x, x, "Text", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given Mob Id
        CastSpell(530, 8346, "Arcane Torrent", 15274, "Thirst Unending", MaxDistance: 6);
    }

    public void WaitBehavior()
    {
        //BEHAVIOR Description:
        //  Waits until the given amount of seconds passed.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. If quest is completed or player does not have this quest then behavior will finish immediately.
        //              Wait(8483, 30, "The Dwarven Spy");  8483 is the id of Quest, and wait only runs if player has this quest and it is not completed.
        //          Time
        //              REQUIRED Default: 30 seconds
        //              The wait time in seconds.
        //          SpellIDs
        //              REQUIRED
        //              Spell Ids to learn seperated by ','
        //              Example TrainSkill(530, 16272,  "13163,1978");  13163 and 1978 are spell IDs to learn
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running.
        //              Example CastSpell(x, x, x, QuestName: "Claiming the Light");  "Claiming the Light" is the text to show in logs
        Wait(8483, 30, "The Dwarven Spy");
    }

    public void AbandonQuestBehavior()
    {
        //BEHAVIOR Description:
        //  Abandons the given quest.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest to be abandoned. 
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running.
        AbandonQuest(9484, "Taming Rod");
    }

    public void DefendNPCBehavior()
    {
        //BEHAVIOR Description:
        //  Defends the given NPC by fighting any mob which attacks this NPC. Used for quests that you need to defend a NPC from incoming mobs
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //              Example DefendNpc(1, x, x, "Text");  1 is the id of Map = Kalindor
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. If quest is completed or player does not have this quest then behavior will finish immediately.
        //              DefendNpc(x, 8488, x, "Text");  8488 is the id of Quest, and wait only runs if player has this quest and it is not completed.
        //          MobId
        //              REQUIRED
        //              Id of the NPC to defend.
        //              Example DefendNpc(x, x, 15402, "Text");,  '15402' is the id of the mob to defend.
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running.
        //          LimitTime
        //              [optional; Default: 300000]
        //              The limiting time for this behavior. After this time is passed then behavior ends.
        //              The time is in milliseconds.
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit. used with Hotspots: parameter
        //              Example TrainSkill(x, x, x, "", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given trainer
        DefendNpc(530, 8488, 15402, "Unexpected Results");
    }

    public void LeadNPCBehavior()
    {
        //BEHAVIOR Description:
        //  Leads the given NPC to the given hotspots. Used for quests that NPC follows the player to until reached a specific spot.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //              Example LeadNpc(1, x, x, "Text");  1 is the id of Map = Kalindor
        //          QuestId
        //              REQUIRED 
        //              Identifies the quest where behavior will be used. If quest is completed or player does not have this quest then behavior will finish immediately.
        //              LeadNpc(x, 8488, x, "Text");  8488 is the id of Quest, and wait only runs if player has this quest and it is not completed.
        //          MobId
        //              REQUIRED
        //              Id of the NPC to lead.
        //              Example LeadNpc(x, x, 3568, "Text");,  '3568' is the id of the mob to defend.
        //          QuestName
        //              [optional; Default: ""]
        //              Identifies the text of quest to show in logs when running.
        //          LimitTime
        //              [optional; Default: 300000]
        //              The limiting time for this behavior. After this time is passed then behavior ends.
        //              The time is in milliseconds.
        //          MobFollowAuraId
        //              [optional; Default: 0]
        //              Identifies the aura Id the mob must have to be considered a valid follower
        //          MobUseItemAuraId
        //              [optional; Default: 0]
        //              When set, then will use item with 'ItemID' on the NPC when NPC has this aura ID
        //          ItemName
        //              [optional; Default: ""]
        //              Used with 'MobUseItemAuraId' and will use this item when mob has the given aura.
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit. used with Hotspots: parameter
        //              Example TrainSkill(x, x, x, "", Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given trainer
        LeadNpc(1, 938, 3568, "Mist", Hotspots: "(10663.67, 1861.15, 1324.25)");
    }

    public void TamePetBehavior()
    {
        //BEHAVIOR Description:
        //  A behavior for hunters to tame a pet.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //              Example DefendNpc(1, x, x, "Text");  1 is the id of Map = Kalindor
        //          MobId
        //              REQUIRED
        //              Id of the pet to tame.
        //              Example TamePet(x, 15652);,  '15652' is the id of the pet to tame.
        //          Hotspots
        //              [optional; Default: ""]
        //              Identifies the custom waypoint for bot visit. used with Hotspots: parameter
        //              Example TamePet(x, x, Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given trainer
        var ME = ObjectManager.Instance.Player;
        IF(() => ME.Class == UnitClass.Hunter && !ME.HasActivePet);
            TamePet(530, 15652);
        EndIF();
    }

    public void Fishing()
    {
        //BEHAVIOR Description:
        //  A behavior for fishing normal water. For fishing pools it is not required to use this behavior.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //          FishingSpots
        //              REQUIRED
        //              Fishing spots, bot will got to each spot and faces the water and then fish for the given 'FishingDuration'
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          FishingDuration
        //              [optional; Default: 600]
        //              The time to fish at each spot in seconds.
        //          RepeatCount
        //              [optional; Default: 1]
        //              Set number of fishing iterations. A value greater than 0 means after bot fished at all spots it will repeat again
        //          LureIDs
        //              [optional; Default: null]
        //              Set id of lures to apply before starting to fish at each spot.
        //          WaitBeforeCastingPoleMin
        //              [optional; Default: 800]
        //              Set the minimum time in milliseconds to wait before casting fishing pole again after previous cast.
        //          WaitBeforeCastingPoleMax
        //              [optional; Default: 1200]
        //              Set the maximum time in milliseconds to wait before casting fishing pole again after previous cast.
        //          WaitBeforeClickingBobberMin
        //              [optional; Default: 800]
        //              Set the minimum time in milliseconds to wait before clicking the bobber after fish caught.
        //          WaitBeforeClickingBobberMax
        //              [optional; Default: 1200]
        //              Set the maximum time in milliseconds to wait before clicking the bobber after fish caught.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while going to spots if supported for this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving to spots.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to the fishing spots.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to the fishing spots.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        FishSpot(MapId: 571, FishingSpots: "(4197.745, -1599.453, 170.3316),(4083.568, -1630.193, 178.6334),(4143.76, -1817.022, 199.6549)", TaskName: "Fishing", FishingDuration: 300, RepeatCount: 5, LureIDs: "1234,5678");
    }

    public void Vendoring()
    {
        //BEHAVIOR Description:
        //  When ever player bags are full or items need repairing, bot will run this behavior automatically.
        //  You can also manually invoke it to run this routine in certain points in your profile.
        //  It will go to the closest vendor defined in the profile or given as a parameter to this method, to sell items,
        //  Restock foond and drink and ammo and send mail if user set the mail recipient in their settings.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          MinFreeBagSlots
        //              [optional; Default: 1]
        //              Set the minimum free bag slots player must have before this routine tries to free bag slots.
        //              For example if set to 1, if player only have 1 free general bag slot(not mining or other bag slots)
        //              then bot will try to empty the bags.
        //          SellGray
        //              [optional; Default: true]
        //              Set to true to sell grays while selling items.
        //          SellWhite
        //              [optional; Default: false]
        //              Set to true to sell white items while selling items.
        //          SellGreen
        //              [optional; Default: false]
        //              Set to true to sell green items while selling items.
        //          SellBlue
        //              [optional; Default: false]
        //              Set to true to sell blue items while selling items.
        //          SellPurple
        //              [optional; Default: false]
        //              Set to true to sell epic items while selling items.
        //          SellBindOnEquips
        //              [optional; Default: false]
        //              Set to true to sell BoE items too while selling items. i.e. looted green BoE items.
        //          SellTradeGoods
        //              [optional; Default: false]
        //              Set to true to sell trade good items too while selling items. i.e. crafting materials, cloths etc.
        //          SellRecipies
        //              [optional; Default: false]
        //              Set to true to sell recipies while selling items. i.e. if 'SellGreen' is set then all green recipies will be sold.
        //          MailGray
        //              [optional; Default: false]
        //              Set to true to mail grays while mailing items.
        //          MailWhite
        //              [optional; Default: true]
        //              Set to true to mail white items while mailing items.
        //          MailGreen
        //              [optional; Default: true]
        //              Set to true to mail green items while mailing items.
        //          MailBlue
        //              [optional; Default: true]
        //              Set to true to mail blue items while mailing items.
        //          MailPurple
        //              [optional; Default: false]
        //              Set to true to mail epic items while mailing items.
        //          MailTradeGoods
        //              [optional; Default: false]
        //              Set to true to mail trade good items too while mailing items. i.e. crafting materials, cloths etc.
        //          MailRecipies
        //              [optional; Default: false]
        //              Set to true to mail recipies while mailing items. i.e. if 'MailGreen' is set then all green recipies will be mailed.
        //          MinDurabilityPercentToRepair
        //              [optional; Default: 15]
        //              Set the minimum item durability before this behavior try to repair items.
        //          FindVendorsAutomatically
        //              [optional; Default: false]
        //              Set to true to let the bot to find vendors automatically from the database if not any defined in the profile.
        //          FindMailboxesAutomatically
        //              [optional; Default: true]
        //              Set to true to let the bot to find mailboxes automatically from the database if not any defined in the profile.
        //          Vendors
        //              [optional; Default: null]
        //              Set list of vendors to overwrite the profile vendors.
        //          Mailboxes
        //              [optional; Default: null]
        //              Set list of mailboxes to overwrite the profile mailboxes.
        //          KeepItems
        //              [optional; Default: null]
        //              Set list of item ids to exclude from sell and mailing items.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.

        SellMailAndRepair();

        //BEHAVIOR Description:
        //  This behavior can be used to restock foods, drinks, arrows and bullets on demand.
        //  It will go to the closest vendor defined in the profile or given as a parameter to this method,
        //  to restock foond and drink and ammo and bullets.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          FindVendorsAutomatically
        //              [optional; Default: false]
        //              Set to true to let the bot to find vendors automatically from the database if not any defined in the profile.
        //          Vendors
        //              [optional; Default: null]
        //              Set list of vendors to overwrite the profile vendors.
        //              If this value not set then the vendors defined in the profile will be used.
        //          FoodVendors
        //              [optional; Default: null]
        //              Set list of food vendors to overwrite the list defined in the profile.
        //              If this value not set then the restock food vendors defined in the profile will be used.
        //          DrinkVendors
        //              [optional; Default: null]
        //              Set list of drink vendors to overwrite the list defined in the profile.
        //              If this value not set then the restock drink vendors defined in the profile will be used.
        //          ArrowVendors
        //              [optional; Default: null]
        //              Set list of arrow vendors to overwrite the list defined in the profile.
        //              If this value not set then the restock arrow vendors defined in the profile will be used.
        //          BulletVendors
        //              [optional; Default: null]
        //              Set list of bullet vendors to overwrite the list defined in the profile.
        //              If this value not set then the restock bullet vendors defined in the profile will be used.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        Restock();

        //BEHAVIOR Description:
        //  This behavior can be used to sell items to a vendor. Grey items will be sold automatically on this behavior.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //          VendorID
        //              REQUIRED
        //              Set the Id of the vendor NPC.
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          ItemIDs
        //              [optional; Default: ""]
        //              Set id of the items you want to specifically sell. i.e. "1234,5678"
        //          Hotspots
        //              [optional; Default: ""]
        //              Set the position(s) of the vendor
        //              Example Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given vendor
        //          VendorName
        //              [optional; Default: ""]
        //              Set the name of the vendor. It is not required.
        //          SellWhiteItems
        //              [optional; Default: false]
        //              Set to true to sell white items while selling items.
        //          SellGreenItems
        //              [optional; Default: false]
        //              Set to true to sell green items while selling items.
        //          SellBlueItems
        //              [optional; Default: false]
        //              Set to true to sell blue items while selling items.
        //          SellBoEItems
        //              [optional; Default: false]
        //              Set to true to sell BoE items too while selling items. i.e. looted green BoE items.
        //          SellTradeGoodItems
        //              [optional; Default: false]
        //              Set to true to sell trade good items too while selling items. i.e. crafting materials, cloths etc.
        //          SellRecipies
        //              [optional; Default: false]
        //              Set to true to sell recipies while selling items. i.e. if 'SellGreen' is set then all green recipies will be sold.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        SellItems(530, 16920, "Sell Stuff", SellWhiteItems: false, SellGreenItems: true, SellTradeGoodItems: true, SellBoEItems: true, SellRecipies: false);

        //BEHAVIOR Description:
        //  This behavior can be used to buy items from a vendor.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //          VendorID
        //              REQUIRED
        //              Set the Id of the vendor NPC.
        //          ItemIDs
        //              REQUIRED
        //              Set id of the items you want to buy. i.e. "1234,5678"
        //          RestockQuantity
        //              REQUIRED
        //              Set the restock number for buying items. Bot will restock the missing amount in the bags from the vendor.
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          Hotspots
        //              [optional; Default: ""]
        //              Set the position(s) of the vendor
        //              Example Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given vendor
        //          VendorName
        //              [optional; Default: ""]
        //              Set the name of the vendor. It is not required.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        BuyItems(1, 1691, "4496", 1, TaskName: "Buy 1 Small Brown Pouch");
    }

    public void Banking()
    {
        //BEHAVIOR Description:
        //  This behavior can be used to put items into a bank.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //          BankerID
        //              REQUIRED
        //              Set the Id of the banker NPC.
        //          ItemIDs
        //              REQUIRED
        //              Set id of the items you want to store into bank. i.e. "1234,5678"
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          Hotspots
        //              [optional; Default: ""]
        //              Set the position(s) of the banker
        //              Example Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given vendor
        //          KeepAmount
        //              [optional; Default: 0]
        //              Set the number you want to keep from each item in the player bags. i.e if set to 5
        //              bot will make sure 5 of each item remains in the player bags
        //          BankerName
        //              [optional; Default: ""]
        //              Set the name of the banker. It is not required.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        PutToBank(1, BankerID: 1234, ItemIDs: "222,333", TaskName: "Put items into bank", KeepAmount: 5);

        //BEHAVIOR Description:
        //  This behavior can be used to grab items from a bank.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //          BankerID
        //              REQUIRED
        //              Set the Id of the banker NPC.
        //          ItemIDs
        //              REQUIRED
        //              Set id of the items you want to grab from the bank. i.e. "1234,5678"
        //          RestockQuantity
        //              REQUIRED
        //              Set the restock number for grabbing items. Bot will restock the missing amount in the bags from the bank.
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          Hotspots
        //              [optional; Default: ""]
        //              Set the position(s) of the banker
        //              Example Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given vendor
        //          BankerName
        //              [optional; Default: ""]
        //              Set the name of the banker. It is not required.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        GrabFromBank(1, BankerID: 1234, ItemIDs: "222,333", RestockQuantity: 5, TaskName: "Put items into bank");
    }

    public void MailBehaviors()
    {
        //BEHAVIOR Description:
        //  This behavior can be used to send mails.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          ItemIDs
        //              REQUIRED
        //              Set id of the items you want to mail. i.e. "1234,5678"
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          KeepAmount
        //              [optional; Default: 0]
        //              Set the number you want to keep from each item in the player bags. i.e if set to 5
        //              bot will make sure 5 of each item remains in the player bags
        //          MinSend
        //              [optional; Default: 0]
        //              Set the minimum number you want to mail.
        //              i.e. if set to 20, bot only mail items if there are atleast 20 items to mail.
        //          MaxSend
        //              [optional; Default: 0]
        //              Set the maiximum number you want to mail.
        //              i.e. if set to 60, bot will not mail more than 60 items.
        //          ShouldKeepItemIDs
        //              [optional; Default: false]
        //              If set to true but will not mail any item with ID from the 'ItemIDs' parameter.
        //              If set to false then any item in the bags with ID from the 'ItemIDs' parameter will be mailed.
        //          MailGray
        //              [optional; Default: false]
        //              Set to true to mail grays while mailing items.
        //          MailWhite
        //              [optional; Default: true]
        //              Set to true to mail white items while mailing items.
        //          MailGreen
        //              [optional; Default: true]
        //              Set to true to mail green items while mailing items.
        //          MailBlue
        //              [optional; Default: true]
        //              Set to true to mail blue items while mailing items.
        //          MailPurple
        //              [optional; Default: false]
        //              Set to true to mail epic items while mailing items.
        //          MailTradeGoods
        //              [optional; Default: false]
        //              Set to true to mail trade good items too while mailing items. i.e. crafting materials, cloths etc.
        //          MailRecipies
        //              [optional; Default: false]
        //              Set to true to mail recipies while mailing items. i.e. if 'MailGreen' is set then all green recipies will be mailed.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        MailItems(ItemIDs: "6948", TaskName: "Mail items", KeepAmount: 10, MinSend: 20, MaxSend: 60, ShouldKeepItemIDs: true);

        //BEHAVIOR Description:
        //  This behavior can to open mails in the mailbox.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          AutoRefreshMailbox
        //              [optional; Default: true]
        //              Set to true to refresh mailbox when there are too many items are in the mailbox.
        //              After each batch of mails opened, if there are more mails remained, then bot will refresh the mailbox.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        OpenMails(TaskName: "Open mailbox", AutoRefreshMailbox: true);
    }

    public void DestroyItemsBehavior()
    {
        //BEHAVIOR Description:
        //  This behavior can be used to destroy items in the player bags.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          ItemIDs
        //              REQUIRED
        //              Set id of the items you want to destroy. i.e. "1234,5678"
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          KeepAmount
        //              [optional; Default: 0]
        //              Set the number you want to keep from each item in the player bags. i.e if set to 5
        //              bot will make sure 5 of each item remains in the player bags
        DestroyItems("6948");
    }

    public void Hearthstone()
    {
        //BEHAVIOR Description:
        //  This behavior can be used set hearthstone to a given InnKeeper.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          MapId
        //              REQUIRED
        //              Identifies the map where behavior will be used.
        //          InnKeeperID
        //              [optional; Default: true]
        //              Set the Id of the innkeeper NPC.
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        //          Hotspots
        //              [optional; Default: ""]
        //              Set the position(s) of the innkeeper
        //              Example Hotspots: "(0.1,0.2,0.3)");  bot will use Hotspot x=0.1, y=0.2, y=0.3
        //              More examples "(1,2,3),(4,5,6),(12.12,-5.12,24.5),(7,8,9)" = bot will use this 4 hotspots
        //              when not given, bot will look at PixelMaster Database for hotspots for given NPC
        //          InnKeeperName
        //              [optional; Default: ""]
        //              Set the name of the innkeeper. It is not required.
        //          CanFly
        //              [optional; Default: true]
        //              Set true to let character using flying while moving and flying is supported in this map.
        //          CanUseMount
        //              [optional; Default: true]
        //              Set true to allow using a ground mount while moving.
        //          CanUseTaxi
        //              [optional; Default: true]
        //              Set true to allow taking taxis while moving to locations.
        //              Bot assumes player does know the taxi paths.
        //          IgnoreCombat
        //              [optional; Default: false]
        //              Set true to ignore combat while moving to different locations.
        //          IgnoreCombatIfMounted
        //              [optional; Default: true]
        //              Set true to ignore combat if mounted while moving to different locations.
        //          AvoidEnemies
        //              [optional; Default: true]
        //              Set true to let the bot try to avoid enemies on the path.
        SetHearthstone(1, InnKeeperID: 6737, "Inn Auberdine");

        //BEHAVIOR Description:
        //  This behavior can be used to use the player hearthstone.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          TaskName
        //              [optional; Default: ""]
        //              The task name for logging and debugging purposes.
        UseHearthstone(TaskName: "Using HS!");
    }

    public void Logging()
    {
        //BEHAVIOR Description:
        //  Logs information message in the bot console.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          Message
        //              REQUIRED
        //              The message to log.
        LogInfo("info");

        //BEHAVIOR Description:
        //  Logs warning message in the bot console.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          Message
        //              REQUIRED
        //              The message to log.
        LogWarning("warning");

        //BEHAVIOR Description:
        //  Logs error message in the bot console.
        //BEHAVIOR ATTRIBUTES:
        //      Attributes:
        //          Message
        //              REQUIRED
        //              The message to log.
        LogError("error");
    }

    public void SpecialQuests()
    {
        //BEHAVIOR Description:
        //  Quest 12641 special behavior.
        Quest12641();
        //BEHAVIOR Description:
        //  Quest 12680 special behavior.
        Quest12680();
        //BEHAVIOR Description:
        //  Quest 12687 special behavior.
        Quest12687();
        //BEHAVIOR Description:
        //  Quest 12701 special behavior.
        Quest12701();
        //BEHAVIOR Description:
        //  Quest 12720 special behavior.
        Quest12720();
        //BEHAVIOR Description:
        //  Quest 12779 special behavior.
        Quest12779();
        //BEHAVIOR Description:
        //  Quest 12801 special behavior.
        Quest12801();
    }
#endregion



}
