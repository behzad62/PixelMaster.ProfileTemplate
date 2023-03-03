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

            //'restockAmount' is cumulative, it means if there are more than one item ID in the list, bot will try to restock
            //amount equal to 'restockAmount' - sum of existing item counts in the bags.
            //i.e. restockAmount = 20, and there are 5x item:4540 and 10x item: 4560 already in the bags,
            //then, bot will buy only 5x item:4540 or item:4560 depending on which item vendor has.

            //'maxPlayerLevel' indicates what is the max player level before items in that group will be ignored.
            //i.e. in the Drinks list bellow if player level is 15, then the first two rows will be ignored
            //and player will restock items from the row 3

            //'vendorIDs' are list of vendors player will try to restock those item IDs.
            //restocking only happens if vendor is close enough to the player when bot is doing sell/mail/repair sequence
            //so if none of these vendors are close enough to the player (100 yards range) then restokcing will be skipped
            Foods = new List<(int[] itemIDs, int restockAmount, int maxPlayerLevel, int[] vendorIDs)>{
                (new int[] { 4540, 4560 }, 20, 13, new int[] { 829,1247,1237,6734 }),//Tough Hunk of Bread
            },
            Drinks = new List<(int[] itemIDs, int restockAmount, int maxPlayerLevel, int[] vendorIDs)>()
            {
                (new [] {159  }, 20, 5,  new [] { 6791, 6928, 6746, 7714, 3934 }),
                (new [] {1179 }, 20, 15, new [] { 6791, 6928, 6746, 7714, 3934 }),
                (new [] {1205 }, 20, 25, new [] { 6791, 6928, 6746, 7714, 3934 }),
                (new [] {1708 }, 20, 35, new [] { 6791, 6928, 6746, 7714, 3934 }),
            },
            Arrows = new List<(int[] itemIDs, int restockAmount, int maxPlayerLevel, int[] vendorIDs)>{
                (new int[] { 2512 }, 1000, 13, new int[] { 829,1691,1682,7976 }),//Rough Arrow
            },
            Bullets = new List<(int[] itemIDs, int restockAmount, int maxPlayerLevel, int[] vendorIDs)>{
                (new int[] { 2516 }, 1000, 13, new int[] { 829,1691,1682,7976 }),//Light Shot 
            },

            //Keep items are items you want to skil selling are mailing
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

}
