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


namespace ProfileTemplate;

public class Tauran01_16 : IPMProfile
{
    List<Mob> avoidMobs = new List<Mob>()
    {
        new Mob{Id=3068, MapId=1, Name="Mazzranache"},
        new Mob{Id=5786, MapId=1, Name="Snagglespear"},
    };
    List<Blackspot> blackspots = new List<Blackspot>()
    {
        new Blackspot{Position= new Vector3(-2774.54f, -703.32f, 5.86f), MapID= 1, Radius=20f, Faction = PlayerFactions.Horde},//too many mobs bot die alot
    };
    List<MailBox> mailboxes = new List<MailBox>()
    {
        new MailBox{Name="Bloodhoof Village", MapId=1, Position= new Vector3(-2338.213f, -367.142f, -8.528f)},//143984
        new MailBox{Name="Thunder Bluff", MapId=1, Position= new Vector3(-1263.310f, 44.545f, 127.544f)},//143985
        new MailBox{Name="Camp Taurajo", MapId=1, Position= new Vector3(-2351.58f, -1944.75f ,  95.79f)},//153578
        new MailBox{Name="X Roads", MapId=1, Position= new Vector3(-443.69f, -2649.08f ,  95.77f)},//143982
        new MailBox{Name="Razor Hill", MapId=1, Position= new Vector3(322.41f, -4706.90f, 14.68f)},//143981
        new MailBox{Name="Moonglade", MapId=1, Position= new Vector3(7867.27f, -2575.55f, 486.91f)},//195219
    };
    List<Vendor> vendors = new List<Vendor>()
    {
        new Vendor{Id=3883, Name="Moodan Sungrain", MapId=1, Position=new Vector3(-2940.840f, -245.957f, 53.809f), Type=VendorType.Food},//start
        new Vendor{Id=3073, Name="Marjak Keenblade", MapId=1, Position=new Vector3(-2926.321f, -215.715f, 54.176f), Type=VendorType.Repair},//start
        new Vendor{Id=6747, Name="Innkeeper Kauth", MapId=1, Position=new Vector3(-2365.366f, -347.309f, -8.956f), Type=VendorType.Food},//Bloodhoof Vilage
        new Vendor{Id=3080, Name="Harant Ironbrace", MapId=1, Position=new Vector3(-2284.467f, -310.260f, -9.424f), Type=VendorType.Repair},//Bloodhoof Vilage
        new Vendor{Id=8362, Name="Kuruk", MapId=1, Position=new Vector3(-1300.17f, 110.54f, 131.37f), Type=VendorType.Food},//Thunder Bluff
        new Vendor{Id=2997, Name="Jyn Stonehoof", MapId=1, Position=new Vector3(-1283.917f, 84.360f, 128.727f), Type=VendorType.Repair},//Thunder Bluff
        new Vendor{Id=7714, Name="Innkeeper Byula", MapId=1, Position=new Vector3(-2376.27f, -1995.74f ,  96.71f), Type=VendorType.Food},//CTaurajo
        new Vendor{Id=10380, Name="Sanuye Runetotem", MapId=1, Position=new Vector3(-2374.26f, -1948.79f ,  96.09f), Type=VendorType.Repair},//Ctaurajo
        new Vendor{Id=3934, Name="Innkeeper Boorand Plainswind", MapId=1, Position=new Vector3(-407.12f, -2645.22f ,  96.22f), Type=VendorType.Food},//X Roads
        new Vendor{Id=3479, Name="Nargal Deatheye", MapId=1, Position=new Vector3(-357.00f, -2568.86f ,  95.79f), Type=VendorType.Repair},//X Roads
        new Vendor{Id=6928, Name="Innkeeper Grosk", MapId=1, Position=new Vector3(340.36f, -4686.29f ,  16.46f), Type=VendorType.Food},//Razor Hill
        new Vendor{Id=3165, Name="Ghrawt", MapId=1, Position=new Vector3(362.10f, -4763.84f ,  12.47f), Type=VendorType.Repair},//Razor Hill
        new Vendor{Id=12026, Name="My'lanna", MapId=1, Position=new Vector3(7970.00f, -2408.71f, 488.93f), Type=VendorType.Food},//Moonglade
        new Vendor{Id=12024, Name="Meliri", MapId=1, Position=new Vector3(7840.32f, -2562.68f, 489.29f), Type=VendorType.Repair},//Moonglade
    };

    PMProfileSettings CreateSettings()
    {
        return new PMProfileSettings()
        {
            ProfileName = "[H-Quest](01-16)Tauren",
            Author = "Cava",
            Description = "Quest Leveling Tauren Level 1 to 16!",
            //Objects
            AvoidMobs = avoidMobs,
            Blackspots = blackspots,
            Mailboxes = mailboxes,
            Vendors = vendors,
            //Player Settings
            MinPlayerLevel = 1,
            MaxPlayerLevel = 100,
            MinDurabilityPercent = 15,
            MinFreeBagSlots = 1,
            //Death Settings
            MaxDeathsByOtherPlayersBeforeStop = 0,
            MaxDeathsBeforeStop = 0,
            //Sell Settings
            SellGrey = true,
            SellWhite = true,
            SellGreen = true,
            SellBlue = true,
            SellPurple = false,
            SellIncludesBOEs = true,
            SellIncludesRecipies = false,
            SellIncludesTradeGoodItems = false,
            //Mail Settings
            MailGrey = false,
            MailWhite = true,
            MailGreen = true,
            MailBlue = true,
            MailPurple = true,
            MailTradeGoodItems = true,
            MailRecipies = true,
            //Failure behavior
            OnTaskFailure = TaskFailureBehavior.ReturnFailure,
        };
    }

    public IPMProfileContext Create()
    {
        var ME = ObjectManager.Instance.Player;
        var settings = CreateSettings();
        StartProfile(settings);
        //-------------------------------START PROFILE-------------------------------
        IF(() => ME.Level > 13, "Level Check");

        EndIF();

        PickUp(1, 747, 2980, "The Hunt Begins");
        PickUp(1, 752, 2981, "A Humble Task");
        IF(() => ME.QuestLog.HasQuest(752));
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        DestroyItems("6948");
        EndGroup();
        EndIF();
        TurnIn(1, 752, 2991, "A Humble Task");
        PickUp(1, 753, 2991, "A Humble Task");
        InteractWithObject(1, 753, "2907", "A Humble Task");
        KillMobs(1, 747, "2955", "The Hunt Begins", LootMobs: true, PullRange: 100);
        IF(() => ME.Level < 2, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 2, "2955", "Grinding Until Level 2");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 2 - - - - - - - - - - - - - - - - - - -  
        TurnIn(1, 747, 2980, "The Hunt Begins");
        IF(() => ME.Class == UnitClass.Warrior, "Warrior Quest");
        PickUp(1, 3091, 2980, "Simple Note");
        TurnIn(1, 3091, 3059, "Simple Note");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman, "Shaman Quest");
        PickUp(1, 3093, 2980, "Rune-Inscribed Note");
        TurnIn(1, 3093, 3062, "Rune-Inscribed Note");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter, "Hunter Quest");
        PickUp(1, 3092, 2980, "Etched Note");
        TurnIn(1, 3092, 3061, "Etched Note");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid, "Druid Quest");
        PickUp(1, 3094, 2980, "Verdant Note");
        TurnIn(1, 3094, 3060, "Verdant Note");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(750) == false && ME.QuestLog.IsCompleted(750) == false);
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff1(3072, 3072, 3073, 0, 3072);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3059, "6673");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3062, "8017");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3061, "1126");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3060, "1126");
        EndIF();
        EndGroup();
        EndIF();
        PickUp(1, 750, 2980, "The Hunt Continues");
        IF(() => ME.Level < 4, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 4, "2955", "Grinding Until Level 4");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 4 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(753));
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff1(3072, 3072, 3073, 0, 3072);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3059, "6673,100,772,6343");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3062, "8017,8042");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3061, "1126,8921,774");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3060, "1126,8921,774");
        EndIF();
        EndGroup();
        EndIF();
        TurnIn(1, 753, 2981, "A Humble Task");
        PickUp(1, 755, 2981, "Rites of the Earthmother");
        KillMobs(1, 750, "2961", "The Hunt Continues", LootMobs: true, PullRange: 100);
        TurnIn(1, 755, 2982, "Rites of the Earthmother");
        PickUp(1, 757, 2982, "Rite of Strength");
        IF(() => ME.Level < 6, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 6, "2961", "Grinding Until Level 6");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 6 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(750));
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3059, "6673,100,772,6343,34428,3127");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3062, "8017,8042,2484,332");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3061, "1126,8921,774,467,5177");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3060, "1126,8921,774,467,5177");
        EndIF();
        SellBuyStuff1(3072, 3072, 3073, 0, 3072);
        //BuyBags(1, 3076);
        EndGroup();
        EndIF();
        TurnIn(1, 750, 2980, "The Hunt Continues");
        PickUp(1, 780, 2980, "The Battleboars");
        PickUp(1, 3376, 3209, "Break Sharptusk!");
        KillMobs(1, 780, "2966", "The Battleboars", LootMobs: true, PullRange: 100);
        IF(() => ObjectManager.Instance.Player.Level < 7, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 7, "2966", "Grinding Until Level 7");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 7 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.Class == UnitClass.Shaman);
        PickUp(1, 1519, 5888, "Call of Earth");
        EndIF();
        KillMobs(1, 3376, "8554", "Break Sharptusk!", LootMobs: true, PullRange: 100);
        KillMobs(1, 757, "2952,2953", "Rite of Strength", LootMobs: true, PullRange: 100);
        IF(() => ME.Class == UnitClass.Shaman);
        KillMobs(1, 1519, "2953", "Call of Earth", LootMobs: true, PullRange: 100);
        EndIF();
        TurnIn(1, 780, 2980, "The Battleboars");
        TurnIn(1, 3376, 3209, "Break Sharptusk!");

        IF(() => ME.Class == UnitClass.Shaman);
        TurnIn(1, 1519, 5888, "Call of Earth");
        PickUp(1, 1520, 5888, "Call of Earth");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(3073));
        SellItems(1, 3073, "Sell Stuff");
        EndIF();
        TurnIn(1, 757, 2981, "Rite of Strength");
        PickUp(1, 763, 2981, "Rites of the Earthmother");
        IF(() => ME.Class == UnitClass.Shaman, "Shaman Quest");
        UseItem(1, 1520, ItemName: "Earth Sapta", QuestName: "Call of Earth", NumTimes: 1, TargetMethod: TargettingMethod.POSITION, Hotspots: "(-3029.706, -716.913, 45.063)");
        TurnIn(1, 1520, 5891, "Call of Earth", Hotspots: "(-3029.706, -716.913, 45.063)");
        PickUp(1, 1521, 5891, "Call of Earth", Hotspots: "(-3029.706, -716.913, 45.063)");
        TurnIn(1, 1521, 5888, "Call of Earth");
        EndIF();
        IF(() => ME.Level < 8, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 8, "2952,2953", "Grinding Until Level 8");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 8 - - - - - - - - - - - - - - - - - - - 
        PickUp(1, 1656, 6775, "A Task Unfinished");
        PickUp(1, 743, 2985, "Dangers of the Windfury");
        //ammo 3076
        //food  6747
        //petfood
        //water 6747
        //repair 3080
        IF(() => ME.QuestLog.HasQuest(763), onChildFailure: TaskFailureBehavior.Continue);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3063, "6673,100,772,6343,34428,3127,1715,284");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3066, "8017,8042,2484,332,8044,529,324,8018,5730");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3065, "1126,8921,774,467,5177,339,5186");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3064, "1126,8921,774,467,5177,339,5186");
        EndIF();
        SellBuyStuff2(6747, 6747, 3080, 0, 3076);
        BuyBags(1, 3076);
        BuyBags(2, 3076);
        EndIF();
        TurnIn(1, 763, 2993, "Rites of the Earthmother");
        PickUp(1, 745, 2993, "Sharing the Land");
        PickUp(1, 767, 2993, "Rite of Vision");
        PickUp(1, 746, 2993, "Dwarven Digging");
        TurnIn(1, 1656, 6747, "A Task Unfinished");
        TurnIn(1, 767, 3054, "Rite of Vision");
        PickUp(1, 771, 3054, "Rite of Vision");
        PickUp(1, 766, 3055, "Mazzranache");
        PickUp(1, 761, 2947, "Swoop Hunting");
        PickUp(1, 748, 2948, "Poison Water");
        KillMobs(1, 766, "2958", "Mazzranache", ObjectiveIndex: 1, LootMobs: true, PullRange: 250);
        KillMobs(1, 766, "2956", "Mazzranache", ObjectiveIndex: 3, LootMobs: true, PullRange: 250);
        KillMobs(1, 748, "2958", "Poison Water", ObjectiveIndex: 1, LootMobs: true, PullRange: 250);
        KillMobs(1, 748, "2956", "Poison Water", ObjectiveIndex: 2, LootMobs: true, PullRange: 250);
        KillMobs(1, 766, "2969", "Mazzranache", ObjectiveIndex: 4, LootMobs: true, PullRange: 250);
        KillMobs(1, 761, "2969", "Swoop Hunting", LootMobs: true, PullRange: 50);
        InteractWithObject(1, 771, "2912", "Rite of Vision", ObjectiveIndex: 2);
        IF(() => ME.Level < 10, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 10, "2958,2956,2969", "Grinding Until Level 10");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 10 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(748), onChildFailure: TaskFailureBehavior.Continue);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3063, "6673,100,772,6343,34428,3127,1715,284,2687,6546");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3066, "8017,8042,2484,332,8044,529,324,8018,5730,8050,8024,8075");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3065, "1126,8921,774,467,5177,339,5186,99,5232,8924,16689,1058");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3064, "1126,8921,774,467,5177,339,5186,99,5232,8924,16689,1058");
        EndIF();
        SellBuyStuff2(6747, 6747, 3080, 0, 3076);
        BuyBags(1, 3076);
        BuyBags(2, 3076);
        BuyBags(3, 3076);
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter, "Hunter Quest");
        PickUp(1, 6061, 3065, "Taming the Beast");
        IF(() => ME.QuestLog.IsCompleted(6061) == false && ME.QuestLog.HasQuest(6061));
        While(() => ME.QuestLog.IsCompleted(6061) == false);
        UseItem(1, 6061, "Taming Rod", MobId: 2956, NumTimes: 3, MaxDistance: 30, WaitTime: 20000, DisableFlags: "Combat");
        Wait(6061, 3, "Taming Rod");
        IF(() => ME.QuestLog.IsCompleted(6061) == false);
        AbandonQuest(6061, "Taming Rod");
        Wait(0, 3, "Taming Rod");
        PickUp(1, 6061, 3065, "Taming the Beast");
        EndIF();
        EndWhile();
        EndIF();
        TurnIn(1, 6061, 3065, "Taming the Beast");
        PickUp(1, 6087, 3065, "Taming the Beast");
        IF(() => ME.QuestLog.IsCompleted(6087) == false && ME.QuestLog.HasQuest(6087));
        RunMacro(6087, "/Run PetDismiss();");
        While(() => ME.QuestLog.IsCompleted(6087) == false);
        UseItem(1, 6087, "Taming Rod", MobId: 2959, NumTimes: 3, MaxDistance: 30, WaitTime: 20000, DisableFlags: "Combat");
        Wait(6087, 3, "Taming Rod");
        IF(() => ME.QuestLog.IsCompleted(6087) == false);
        AbandonQuest(6087, "Taming Rod");
        Wait(0, 3, "Taming Rod");
        PickUp(1, 6087, 3065, "Taming the Beast");
        EndIF();
        EndWhile();
        EndIF();
        TurnIn(1, 6087, 3065, "Taming the Beast");
        PickUp(1, 6088, 3065, "Taming the Beast");
        IF(() => ME.QuestLog.IsCompleted(6088) == false && ME.QuestLog.HasQuest(6088));
        RunMacro(6088, "/Run PetDismiss();");
        While(() => ME.QuestLog.IsCompleted(6088) == false);
        UseItem(1, 6088, "Taming Rod", MobId: 2970, NumTimes: 3, MaxDistance: 30, WaitTime: 35000, DisableFlags: "Combat", IsChanneling: true);
        Wait(6088, 3, "Taming Rod");
        IF(() => ME.QuestLog.IsCompleted(6088) == false);
        AbandonQuest(6088, "Taming Rod");
        Wait(0, 3, "Taming Rod");
        PickUp(1, 6088, 3065, "Taming the Beast");
        EndIF();
        EndWhile();
        EndIF();
        //TB
        //ammo 8362
        //food  8362
        //petfood 3025
        //water 8362
        //repair 2997
        //bags 8362
        TurnIn(1, 6088, 3065, "Taming the Beast");
        PickUp(1, 6089, 3065, "Beast Training");
        IF(() => ME.QuestLog.HasQuest(6089));
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(8362, 8362, 2997, 0, 8362);
        BuyItems(1, 3025, "117", 20, "Tough Jerky");
        EndGroup();
        RunMacro(0, "/Run PetDismiss();");
        TurnIn(1, 6089, 3039, "Beast Training");
        EndIF();
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter && !ME.HasActivePet);
        TamePet(1, 3035);
        EndIF();


        IF(() => ME.QuestLog.HasQuest(748));
        TurnIn(1, 748, 2948, "Poison Water");
        Wait(0, 10, "Poison Water");
        EndIF();
        PickUp(1, 754, 2948, "Winterhoof Cleansing");
        TurnIn(1, 761, 2947, "Swoop Hunting");
        InteractWithObject(1, 771, "2910", "Rite of Vision", ObjectiveIndex: 1);

        IF(() => ME.QuestLog.IsCompleted(745) == false && ME.QuestLog.HasQuest(745));
        While(() => ME.QuestLog.IsObjectiveCompleted(745, 1) == false && ME.QuestLog.IsObjectiveCompleted(745, 2) == false && ME.QuestLog.IsObjectiveCompleted(745, 3) == false);
        KillMobs(1, 745, "2949,2950,2951", "Sharing the Land", LootMobs: false, PullRange: 100);
        EndWhile();
        While(() => ME.QuestLog.IsObjectiveCompleted(745, 1) == false && ME.QuestLog.IsObjectiveCompleted(745, 2) == false);
        KillMobs(1, 745, "2949,2950", "Sharing the Land", LootMobs: false, PullRange: 100);
        EndWhile();
        While(() => ME.QuestLog.IsObjectiveCompleted(745, 1) == false && ME.QuestLog.IsObjectiveCompleted(745, 3) == false);
        KillMobs(1, 745, "2949,2951", "Sharing the Land", LootMobs: false, PullRange: 100);
        EndWhile();
        While(() => ME.QuestLog.IsObjectiveCompleted(745, 2) == false && ME.QuestLog.IsObjectiveCompleted(745, 3) == false);
        KillMobs(1, 745, "2950,2951", "Sharing the Land", LootMobs: false, PullRange: 100);
        EndWhile();
        EndIF();
        KillMobs(1, 745, "2951", "Sharing the Land", ObjectiveIndex: 3, LootMobs: false, PullRange: 250);
        KillMobs(1, 745, "2950", "Sharing the Land", ObjectiveIndex: 2, LootMobs: false, PullRange: 250);
        KillMobs(1, 745, "2949", "Sharing the Land", ObjectiveIndex: 1, LootMobs: false, PullRange: 250);
        KillMobs(1, 743, "2962", "Dangers of the Windfury", LootMobs: true, PullRange: 50);
        IF(() => ME.Level < 11, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 11, "2962", "Grinding Until Level 11");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 11 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(745), onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(6747, 6747, 3080, 0, 3076);
        TurnIn(1, 745, 2993, "Sharing the Land");
        SellItems(1, 6747, "Sell Flash Pellet", "4960");
        EndIF();
        TurnIn(1, 743, 2985, "Dangers of the Windfury");
        TurnIn(1, 771, 3054, "Rite of Vision");
        PickUp(1, 772, 3054, "Rite of Vision");
        UseItem(1, 754, "Winterhoof Cleansing Totem", TargetMethod: TargettingMethod.POSITION, WaitTime: 11000, Hotspots: "(-2536.229, -705.017, -8.288)");
        TurnIn(1, 754, 2948, "Winterhoof Cleansing");
        PickUp(1, 756, 2948, "Thunderhorn Totem");
        PickUp(1, 749, 2988, "The Ravaged Caravan", Hotspots: "(-2445.01, -1118.822, -9.424)(-2290.005, -581.245, -9.276)");
        TurnIn(1, 749, 2908, "The Ravaged Caravan", Hotspots: "(-1924.657, -712.504, 3.660)");
        PickUp(1, 751, 2908, "The Ravaged Caravan", Hotspots: "(-1924.657, -712.504, 3.660)");
        KillMobs(1, 766, "3035", "Mazzranache", ObjectiveIndex: 2, LootMobs: true, PullRange: 50);
        IF(() => ME.QuestLog.IsCompleted(756) == false && ME.QuestLog.HasQuest(756));
        While(() => ME.QuestLog.IsObjectiveCompleted(756, 1) == false && ME.QuestLog.IsObjectiveCompleted(756, 2) == false);
        KillMobs(1, 756, "3035,2959", "Thunderhorn Totem", LootMobs: true, PullRange: 100);
        EndWhile();
        EndIF();
        KillMobs(1, 756, "3035", "Thunderhorn Totem", ObjectiveIndex: 2, LootMobs: true, PullRange: 250);
        KillMobs(1, 756, "2959", "Thunderhorn Totem", ObjectiveIndex: 1, LootMobs: true, PullRange: 250);
        IF(() => ME.Level < 12, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 12, "3035,2959", "Grinding Until Level 12");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 12 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(766), onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(6747, 6747, 3080, 0, 3076);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3063, "6673,100,772,6343,34428,3127,1715,284,2687,6546,5242,7384");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3066, "8017,8042,2484,332,8044,529,324,8018,5730,8050,8024,8075,2008,1535,547,370");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3065, "1494,13163,1978,3044,1130,5116,14260,3127,13165,13549,19883,14281,20736,136,2974");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3064, "1126,8921,774,467,5177,339,5186,99,5232,8924,16689,1058,5229,8936,50769");
        EndIF();
        BuyBags(1, 3076);
        BuyBags(2, 3076);
        BuyBags(3, 3076);
        BuyBags(4, 3076);
        EndIF();
        TurnIn(1, 766, 3055, "Mazzranache");
        IF(() => ME.Level < 13, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 13, "2989,2990", "Grinding Until Level 13");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 13 - - - - - - - - - - - - - - - - - - - 

        IF(() => ME.QuestLog.IsCompleted(746) == false && ME.QuestLog.HasQuest(746));
        While(() => ME.QuestLog.IsCompleted(746) == false, checkPeriod: LoopConditionCheckPeriod.AtEachTick);
        GrindMobsUntil(1, "4702:5", "2989,2990", "collecting Prospector Pick");
        UseItem(1, 0, "Prospector's Pick", QuestName: "Dwarven Digging", WaitTime: 3000, NumTimes: 5, TargetMethod: TargettingMethod.POSITION, Hotspots: "(-1240.00, 113.64, 129.85)", MaxDistance: 2f);
        MoveTo(1, 746, "Dwarven Digging", Hotspots: "(-1268.00, 128.05, 131.53)");
        EndWhile();
        EndIF();
        IF(() => ME.QuestLog.HasQuest(772));
        TurnIn(1, 772, 2984, "Rite of Vision");
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        DestroyItems("4823");
        EndGroup();
        EndIF();
        PickUp(1, 773, 2984, "Rite of Wisdom");
        IF(() => ME.QuestLog.HasQuest(756));
        TurnIn(1, 756, 2948, "Thunderhorn Totem");
        Wait(0, 10, "Thunderhorn Totem");
        EndIF();
        PickUp(1, 758, 2948, "Thunderhorn Cleansing");
        UseItem(1, 758, "Thunderhorn Cleansing Totem", TargetMethod: TargettingMethod.POSITION, WaitTime: 11000, Hotspots: "(-1830.485, -242.702, -9.424)");
        TurnIn(1, 758, 2948, "Thunderhorn Cleansing");
        PickUp(1, 759, 2948, "Wildmane Totem");
        TurnIn(1, 751, 2988, "The Ravaged Caravan", IgnoreCheck: true, Hotspots: "(-2445.01, -1118.822, -9.424)(-2290.005, -581.245, -9.276)");
        PickUp(1, 764, 2988, "The Venture Co.", Hotspots: "(-2445.01, -1118.822, -9.424)(-2290.005, -581.245, -9.276)");
        PickUp(1, 765, 2988, "Supervisor Fizsprocket", Hotspots: "(-2445.01, -1118.822, -9.424)(-2290.005, -581.245, -9.276)");
        KillMobs(1, 759, "2960", "Wildmane Totem", LootMobs: true, PullRange: 50);
        IF(() => ME.Level < 14, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 14, "2960", "Grinding Until Level 14");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 14 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(759), onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(6747, 6747, 3080, 0, 3076);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3063, "6673,100,772,6343,34428,3127,1715,284,2687,6546,5242,7384");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3066, "8017,8042,2484,332,8044,529,324,8018,5730,8050,8024,8075,2008,1535,547,370,8045,548,8154");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3065, "1494,13163,1978,3044,1130,5116,14260,3127,13165,13549,19883,14281,20736,136,2974,6197,1002,1513");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3064, "1126,8921,774,467,5177,339,5186,99,5232,8924,16689,1058,5229,8936,50769,5211,5187,782,5178");
        EndIF();
        BuyBags(1, 3076);
        BuyBags(2, 3076);
        BuyBags(3, 3076);
        BuyBags(4, 3076);
        EndIF();
        IF(() => ME.QuestLog.HasQuest(759));
        TurnIn(1, 759, 2948, "Wildmane Totem");
        Wait(0, 10, "Wildmane Totem");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid, "Druid Quest");
        PickUp(1, 5928, 3064, "Heeding the Call");
        TurnIn(1, 5928, 3033, "Heeding the Call");
        PickUp(1, 5922, 3033, "Moonglade");
        IF(() => ME.QuestLog.HasQuest(5922));
        While(() => ObjectManager.Instance.CurrentMap.UIMapId != 1450);
        RunMacro(0, "/cast Teleport: Moonglade");
        Wait(0, 15, "Casting Teleport: Moonglade");
        EndWhile();
        TurnIn(1, 5922, 11802, "Moonglade");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(5930) == false && ME.QuestLog.IsCompleted(5930) == false);
        While(() => ObjectManager.Instance.CurrentMap.UIMapId != 1450);
        RunMacro(0, "/cast Teleport: Moonglade");
        Wait(0, 15, "Casting Teleport: Moonglade");
        EndWhile();
        PickUp(1, 5930, 11802, "Great Bear Spirit");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(5930) && ME.QuestLog.IsCompleted(5930) == false);
        While(() => ObjectManager.Instance.CurrentMap.UIMapId != 1450);
        RunMacro(0, "/cast Teleport: Moonglade");
        Wait(0, 15, "Casting Teleport: Moonglade");
        EndWhile();
        While(() => ME.QuestLog.IsCompleted(5930) == false);
        InteractWithNpc(1, 5930, 11956, "Great Bear Spirit", NumTimes: 1, GossipOptions: "C_GossipInfo.SelectOption(89560),C_GossipInfo.SelectOption(89515),C_GossipInfo.SelectOption(89561),C_GossipInfo.SelectOption(88268)", BlacklistTime: 0, Hotspots: "(8068.98, -2284.81, 496.71)");
        Wait(5930, 3, "Great Bear Spirit");
        EndWhile();
        EndIF();
        IF(() => ME.QuestLog.HasQuest(5930));
        While(() => ObjectManager.Instance.CurrentMap.UIMapId != 1450);
        RunMacro(0, "/cast Teleport: Moonglade");
        Wait(0, 15, "Casting Teleport: Moonglade");
        EndWhile();
        TurnIn(1, 5930, 11802, "Great Bear Spirit");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(5932) == false && ME.QuestLog.IsCompleted(5932) == false);
        While(() => ObjectManager.Instance.CurrentMap.UIMapId != 1450);
        RunMacro(0, "/cast Teleport: Moonglade");
        Wait(0, 15, "Casting Teleport: Moonglade");
        EndWhile();
        PickUp(1, 5932, 11802, "Back to Thunder Bluff");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(5932));
        While(() => ObjectManager.Instance.CurrentMap.UIMapId == 1450);
        InteractWithNpc(1, 0, 11798, "Back to Thunder Bluff", NumTimes: 1, GossipOptions: "C_GossipInfo.SelectOption(89449)", BlacklistTime: 0, Hotspots: "(7785.46, -2403.46 ,  489.54)");
        Wait(0, 3, "Back to Thunder Bluff");
        IF(() => ME.IsOnTaxi);
        While(() => ME.IsOnTaxi);
        Wait(0, 5, "Back to Thunder Bluff");
        EndWhile();
        EndIF();
        EndWhile();
        Wait(0, 1, "Back to Thunder Bluff");
        TurnIn(1, 5932, 3033, "Back to Thunder Bluff");
        EndIF();
        PickUp(1, 6002, 3033, "Body and Heart");
        IF(() => ME.QuestLog.HasQuest(6002) && ME.QuestLog.IsCompleted(6002) == false);
        MoveTo(1, 0, "Camp Taurajo", Hotspots: "(-2311.306, -616.055, -9.422)(-2447.11, -1140.933, -9.424)(-2358.78, -1617.91, 75.02)");
        While(() => ME.QuestLog.IsCompleted(6002) == false);
        IF(() => ME.Inventory.HasItem(15710), onChildFailure: TaskFailureBehavior.Continue);
        UseItem(1, 0, "Cenarion Lunardust", NumTimes: 1, TargetMethod: TargettingMethod.POSITION, Hotspots: "(-2497.33, -1633.08, 91.76)");
        Wait(0, 10, "Body and Heart");
        EndIF();
        GrindMobsUntil(1, TimePassed: 5, "12138", "Body and Heart", Hotspots: "(-2497.33, -1633.08, 91.76)");
        InteractWithNpc(1, 0, 12144, "Body and Heart", NumTimes: 1, WaitForRespaw: false, GossipOptions: "C_GossipInfo.SelectOption(89512)", BlacklistTime: 0, Hotspots: "(-2497.33, -1633.08, 91.76)");
        Wait(0, 2, "Body and Heart");
        IF(() => ObjectManager.Instance.GetNPC(12138) == null && ObjectManager.Instance.GetNPC(12144) == null && !ME.Inventory.HasItem(15710), onChildFailure: TaskFailureBehavior.Continue);
        AbandonQuest(6002, "Body and Heart");
        Wait(0, 1, "Body and Heart");
        MoveTo(1, 0, "Body and Heart", Hotspots: "(-2358.78, -1617.91, 75.02)");
        PickUp(1, 6002, 3033, "Body and Heart");
        EndIF();
        EndWhile();
        MoveTo(1, 0, "Body and Heart", Hotspots: "(-2358.78, -1617.91, 75.02)");
        EndIF();
        TurnIn(1, 6002, 3033, "Body and Heart");
        EndIF();
        PickUp(1, 760, 2948, "Wildmane Cleansing");
        PickUp(1, 861, 3052, "The Hunter's Way");
        IF(() => ME.Class == UnitClass.Warrior, "Warrior Quest");
        PickUp(1, 1505, 3063, "Veteran Uzzek");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman, "Shaman Quests");
        PickUp(1, 2984, 3066, "Call of Fire");
        EndIF();
        KillMobs(1, 765, "3051", "Supervisor Fizsprocket", LootMobs: true, PullRange: 50);
        IF(() => ME.QuestLog.IsCompleted(764) == false && ME.QuestLog.HasQuest(764));
        While(() => ME.QuestLog.IsObjectiveCompleted(764, 1) == false && ME.QuestLog.IsObjectiveCompleted(764, 2) == false);
        KillMobs(1, 764, "2978,2979", "The Venture Co.", LootMobs: false, PullRange: 100);
        EndWhile();
        EndIF();
        KillMobs(1, 764, "2979", "The Venture Co.", ObjectiveIndex: 2, LootMobs: false, PullRange: 50);
        KillMobs(1, 764, "2978", "The Venture Co.", ObjectiveIndex: 1, LootMobs: false, PullRange: 50);

        IF(() => ME.Level < 15, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 15, "2964,2965", "Grinding Until Level 15");//2964,2965
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 15 - - - - - - - - - - - - - - - - - - - 
        PickUp(1, 833, 3233, "A Sacred Burial");
        TurnIn(1, 773, 2994, "Rite of Wisdom");
        PickUp(1, 775, 2994, "Journey into Thunder Bluff");
        KillMobs(1, 833, "3232", "A Sacred Burial", LootMobs: true, PullRange: 50);
        TurnIn(1, 833, 3233, "A Sacred Burial");
        TurnIn(1, 746, 2993, "Dwarven Digging");
        TurnIn(1, 764, 2988, "The Venture Co.", Hotspots: "(-2445.01, -1118.822, -9.424)(-2290.005, -581.245, -9.276)");
        TurnIn(1, 765, 2988, "Supervisor Fizsprocket", Hotspots: "(-2445.01, -1118.822, -9.424)(-2290.005, -581.245, -9.276)");
        PickUp(1, 744, 2987, "Preparation for Ceremony");
        IF(() => ME.QuestLog.HasQuest(775));
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(8362, 8362, 2997, 0, 8362);
        BuyItems(1, 3025, "117", 20, "Tough Jerky");
        EndGroup();
        EndIF();
        TurnIn(1, 775, 3057, "Journey into Thunder Bluff");
        PickUp(1, 776, 3057, "Rites of the Earthmother");
        UseItem(1, 760, "Wildmane Cleansing Totem", TargetMethod: TargettingMethod.POSITION, WaitTime: 11000, Hotspots: "(-760.788, -149.270, -28.613)");
        KillMobs(1, 776, "3058", "Rites of the Earthmother", LootMobs: true, HotSpotRange: 1000, BlacklistTime: 0, Hotspots: "(-695.579, -615.154, -16.701)(-849.692, -486.941, -30.830)(-951.553, -568.499, -55.948)(-1195.349, -636.247, -57.337)(-1360.947, -647.159, -56.077)(-1380.050, -757.614, -40.232)(-1270.197, -785.073, -36.281)(-1200.098, -704.966, -55.826)(-946.230, -756.904, -18.731)");
        IF(() => ME.QuestLog.IsCompleted(744) == false && ME.QuestLog.HasQuest(744));
        While(() => ME.QuestLog.IsObjectiveCompleted(744, 1) == false && ME.QuestLog.IsObjectiveCompleted(744, 2) == false);
        KillMobs(1, 744, "2964,2965", "Preparation for Ceremony", LootMobs: false, PullRange: 100);
        EndWhile();
        EndIF();
        KillMobs(1, 744, "2964", "Preparation for Ceremony", ObjectiveIndex: 1, LootMobs: true, PullRange: 50);
        KillMobs(1, 744, "2965", "Preparation for Ceremony", ObjectiveIndex: 2, LootMobs: true, PullRange: 50);
        KillMobs(1, 861, "3566", "The Hunter's Way", LootMobs: true, PullRange: 50);
        PickUp(1, 886, 5769, "The Barrens Oases");
        IF(() => ME.QuestLog.HasQuest(776));
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(8362, 8362, 2997, 0, 8362);
        BuyItems(1, 3025, "117", 20, "Tough Jerky");
        EndGroup();
        EndIF();
        TurnIn(1, 776, 3057, "Rites of the Earthmother");
        TurnIn(1, 744, 2987, "Preparation for Ceremony");
        TurnIn(1, 861, 3441, "The Hunter's Way");
        PickUp(1, 860, 3441, "Sergra Darkthorn");
        IF(() => ME.Level < 16, "Grind Check");
        GrindMobsUntil(1, PlayerLevelReached: 16, "2964,2965", "Grinding Until Level 16");
        EndIF();
        //- - - - - - - - - - - - - - - - LEVEL 16 - - - - - - - - - - - - - - - - - - - 
        IF(() => ME.QuestLog.HasQuest(760), onChildFailure: TaskFailureBehavior.Continue);
        SellBuyStuff2(6747, 6747, 3080, 0, 3076);
        IF(() => ME.Class == UnitClass.Warrior);
        TrainSkill(1, 3063, "6673,100,772,6343,34428,3127,1715,284,2687,6546,5242,7384,1160,6572,285,694,2565");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman);
        TrainSkill(1, 3066, "8017,8042,2484,332,8044,529,324,8018,5730,8050,8024,8075,2008,1535,547,370,8045,548,8154,526,2645,325,8019,57994");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        TrainSkill(1, 3065, "1494,13163,1978,3044,1130,5116,14260,3127,13165,13549,19883,14281,20736,136,2974,1513,5118,13795,1495,14261");
        EndIF();
        IF(() => ME.Class == UnitClass.Druid);
        TrainSkill(1, 3064, "1126,8921,774,467,5177,339,5186,99,5232,8924,16689,1058,5229,8936,50769,5211,5187,782,5178,1066,8925,1430,779,783");
        EndIF();
        BuyBags(1, 3076);
        BuyBags(2, 3076);
        BuyBags(3, 3076);
        BuyBags(4, 3076);
        EndIF();
        TurnIn(1, 760, 2948, "Wildmane Cleansing");
        IF(() => ME.QuestLog.IsCompleted(854) == false && ME.QuestLog.HasQuest(854) == false);
        MoveTo(1, 0, "Camp Taurajo", Hotspots: "(-2311.306, -616.055, -9.422)(-2447.11, -1140.933, -9.424)(-2379.035, -1880.416, 95.851)");
        InteractWithNpc(1, 0, 10378, NumTimes: 1);
        PickUp(1, 854, 3418, "Journey to the Crossroads");
        EndIF();
        IF(() => ME.QuestLog.HasQuest(886));
        FollowPath(1, QuestName: "X Roads", Waypoints: "(-2268.30, -2181.20, 96.13)(-2243.30, -2182.20, 95.31)(-2218.30, -2183.50, 95.17)(-2193.20, -2184.70, 95.98)(-2168.90, -2190.60, 96.28)(-2147.40, -2203.40, 96.13)(-2125.20, -2215.00, 96.40)(-2100.90, -2221.50, 96.12)(-2079.40, -2234.50, 96.23)(-2056.20, -2244.00, 95.96)(-2032.50, -2252.20, 95.96)(-2014.70, -2269.80, 95.96)(-1998.80, -2289.10, 94.68)(-1982.90, -2308.40, 94.47)(-1964.20, -2325.00, 95.93)(-1943.90, -2339.80, 96.13)(-1926.10, -2357.50, 96.23)(-1910.30, -2377.00, 96.19)(-1893.30, -2395.50, 96.16)(-1882.60, -2418.10, 96.19)(-1871.80, -2440.70, 95.62)(-1857.00, -2460.80, 93.78)(-1841.30, -2480.30, 92.12)(-1823.30, -2497.70, 91.69)(-1804.70, -2514.60, 91.69)(-1786.10, -2531.40, 91.90)(-1765.20, -2545.20, 91.72)(-1740.90, -2551.10, 91.99)(-1715.90, -2554.10, 92.05)(-1691.90, -2547.00, 91.85)(-1669.20, -2536.60, 93.39)(-1646.70, -2525.50, 92.18)(-1622.70, -2518.50, 91.78)(-1599.10, -2527.10, 92.20)(-1578.50, -2541.30, 92.03)(-1554.00, -2546.50, 92.05)(-1529.00, -2547.50, 92.00)(-1504.00, -2548.10, 92.03)(-1478.90, -2548.50, 92.11)(-1453.90, -2549.00, 92.64)(-1429.10, -2545.90, 94.95)(-1410.30, -2529.30, 95.63)(-1392.30, -2511.90, 96.09)(-1373.70, -2495.10, 95.98)(-1350.30, -2486.00, 96.19)(-1326.30, -2478.90, 96.06)(-1302.30, -2471.60, 96.07)(-1280.00, -2460.20, 95.59)(-1257.40, -2449.50, 93.53)(-1232.50, -2446.70, 92.12)(-1207.50, -2446.60, 93.16)(-1182.50, -2447.30, 94.26)(-1157.50, -2449.70, 95.17)(-1132.40, -2451.70, 95.28)(-1107.30, -2452.70, 94.71)(-1082.30, -2453.70, 92.56)(-1057.30, -2454.80, 91.72)(-1032.90, -2460.50, 91.73)(-1010.80, -2472.40, 91.84)(-987.10, -2480.70, 92.39)(-962.60, -2485.50, 93.98)(-938.20, -2490.90, 94.95)(-917.00, -2504.30, 96.13)(-897.90, -2520.60, 95.85)(-880.20, -2538.30, 94.17)(-862.80, -2556.30, 92.10)(-843.70, -2572.50, 91.94)(-819.70, -2579.50, 91.72)(-795.20, -2584.60, 91.87)(-770.60, -2589.80, 93.39)(-748.70, -2601.60, 95.89)(-727.40, -2614.70, 96.16)(-705.80, -2627.50, 96.18)(-684.50, -2640.60, 95.95)(-660.60, -2648.30, 96.02)(-635.60, -2650.40, 96.21)(-610.60, -2651.30, 96.26)(-585.60, -2652.80, 95.94)(-560.50, -2654.00, 96.03)");
        InteractWithNpc(1, 0, 3615, NumTimes: 1);
        TurnIn(1, 886, 3448, "The Barrens Oases");
        EndIF();
        TurnIn(1, 860, 3338, "Sergra Darkthorn");
        IF(() => ME.QuestLog.HasQuest(854));
        TurnIn(1, 854, 3429, "Journey to the Crossroads");
        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        DestroyItems("4854");
        EndGroup();
        EndIF();
        IF(() => ME.Class == UnitClass.Warrior, "Warrior Quest");
        IF(() => ME.QuestLog.HasQuest(1505));
        MoveTo(1, 0, "Veteran Uzzek", Hotspots: "(-354.361, -2681.333, 95.872)(39.460, -2722.573, 91.668)(197.900, -3416.622, 30.650)");
        EndIF();
        TurnIn(1, 1505, 5810, "Veteran Uzzek");
        PickUp(1, 1498, 5810, "Path of Defense");
        KillMobs(1, 1498, "3130", "Path of Defense", LootMobs: true, PullRange: 50);
        TurnIn(1, 1498, 5810, "Path of Defense");
        EndIF();
        IF(() => ME.Class == UnitClass.Shaman, "Shaman Quests");
        IF(() => ME.QuestLog.HasQuest(2984));
        MoveTo(1, 0, "Veteran Uzzek", Hotspots: "(-354.361, -2681.333, 95.872)(39.460, -2722.573, 91.668)");
        EndIF();
        TurnIn(1, 2984, 5907, "Call of Fire");
        PickUp(1, 1524, 5907, "Call of Fire");
        TurnIn(1, 1524, 5900, "Call of Fire");
        PickUp(1, 1525, 5900, "Call of Fire");
        KillMobs(1, 1525, "3267,3268", "Call of Fire", ObjectiveIndex: 1, LootMobs: true, PullRange: 50);
        KillMobs(1, 1525, "3199", "Call of Fire", ObjectiveIndex: 2, LootMobs: true, PullRange: 50);
        TurnIn(1, 1525, 5900, "Call of Fire");
        PickUp(1, 1526, 5900, "Call of Fire");
        IF(() => ME.QuestLog.IsCompleted(1526) == false && ME.QuestLog.HasQuest(1526));
        While(() => ME.QuestLog.IsCompleted(1526) == false, checkPeriod: LoopConditionCheckPeriod.AtEachTick);
        InteractWithNpc(1, 0, 5900, "Call of Fire", NumTimes: 1, BlacklistTime: 0, MeMissAuraId: 8898, Hotspots: "(-268.78, -3999.13 , 168.30)");
        UseItem(1, 0, "Fire Sapta", NumTimes: 1, TargetMethod: TargettingMethod.POSITION, Hotspots: "(-256.69, -3981.18, 168.40)");
        InteractWithNpc(1, 0, 5893, "Call of Fire", NumTimes: 1, BlacklistTime: 0, MeHaveAuraId: 8898, Hotspots: "(-246.94, -4018.30 , 187.30)");
        EndWhile();
        EndIF();
        TurnIn(1, 1526, 61934, "Call of Fire", Hotspots: "(-243.77, -4022.40, 187.30)");
        PickUp(1, 1527, 61934, "Call of Fire", Hotspots: "(-243.77, -4022.40, 187.30)");
        TurnIn(1, 1527, 5907, "Call of Fire");
        EndIF();
        MoveTo(1, 0, "Razor Hill", Hotspots: "(307.44, -4728.64, 9.68)");

        StartGroup(onChildFailure: TaskFailureBehavior.Continue);
        SellMailAndRepair();
        InteractWithNpc(1, 0, 3165, "Cleaning Bags", NumTimes: 1);
        //RunMacro(0, "/run for i=0,4 do for j=1,C_Container.GetContainerNumSlots(i)do l=C_Container.GetContainerItemLink(i,j)if l then _,_,q=GetItemInfo(l)if q<3 then C_Container.PickupContainerItem(i,j)PickupMerchantItem(0)end end end end");
        //Wait(0, 1, "Selling");
        //RunMacro(0, "/run for i=0,4 do for j=1,C_Container.GetContainerNumSlots(i)do l=C_Container.GetContainerItemLink(i,j)if l then _,_,q=GetItemInfo(l)if q<3 then C_Container.PickupContainerItem(i,j)DeleteCursorItem()end end end end");
        //Wait(0, 1, "Selling");
        RunMacro(0, "/run for i=1,GetNumQuestLogEntries() do SelectQuestLogEntry(i); SetAbandonQuest(); AbandonQuest(); end");
        EndGroup();
        //-------------------------------END PROFILE-------------------------------
        //LoadProfileFromServer(52, "SzTaFjFvux", "[H](16-20)Horde.cs");
        return EndProfile();
    }

    private void BuyBags(int _bag, int _vendor)
    {
        var INV = ObjectManager.Instance.Player.Inventory;
        var ME = ObjectManager.Instance.Player;
        IF(() => ((INV.GetBagByIndex(_bag) is null || INV.GetBagByIndex(_bag).TotalSlots < 4) && ME.Money.TotalSilvers >= 5), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(1, _vendor, "4496", 1, TaskName: "Buy 1 Small Brown Pouch");//4496
        EndIF();
        IF(() => (INV.GetBagByIndex(_bag) is null || INV.GetBagByIndex(_bag).TotalSlots < 4) && ME.Inventory.HasItem(4496));
        RunMacro(0, "/equip Small Brown Pouch", "Equipping Small Brown Pouch");
        EndIF();
    }
    private void SellBuyStuff1(int _drinkVendor, int _foodVendor, int _repair, int _petFood, int _ammoVendor)
    {
        var ME = ObjectManager.Instance.Player;
        SellItems(1, _repair, "Sell Stuff", SellWhiteItems: true, SellGreenItems: true, SellTradeGoodItems: true);
        IF(() => (ME.Class == UnitClass.Paladin || ME.Class == UnitClass.Druid || ME.Class == UnitClass.Shaman || ME.Class == UnitClass.Priest || ME.Class == UnitClass.Warlock), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(1, _drinkVendor, "159", 20, "Refreshing Spring Water");
        EndIF();
        IF(() => (ME.Class == UnitClass.Hunter || ME.Class == UnitClass.Rogue || ME.Class == UnitClass.Warlock), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(1, _foodVendor, "4540", 20, "Tough Hunk of Bread");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        BuyItems(1, _ammoVendor, "2512,2516", 1000, "Hunter Ammos");
        EndIF();
    }

    private void SellBuyStuff2(int _drinkVendor, int _foodVendor, int _repair, int _petFood, int _ammoVendor)
    {
        var ME = ObjectManager.Instance.Player;
        SellMailAndRepair();
        SellItems(1, _repair, "Sell Stuff", SellWhiteItems: true, SellGreenItems: true, SellTradeGoodItems: true, SellBoEItems: true);
        IF(() => (ME.Class == UnitClass.Paladin || ME.Class == UnitClass.Druid || ME.Class == UnitClass.Shaman || ME.Class == UnitClass.Priest || ME.Class == UnitClass.Warlock), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(1, _drinkVendor, "159", 20, "Refreshing Spring Water");
        EndIF();
        IF(() => (ME.Class == UnitClass.Hunter || ME.Class == UnitClass.Rogue || ME.Class == UnitClass.Warlock), onChildFailure: TaskFailureBehavior.Continue);
        BuyItems(1, _foodVendor, "4540", 20, "Tough Hunk of Bread");
        EndIF();
        IF(() => ME.Class == UnitClass.Hunter);
        BuyItems(1, _ammoVendor, "2512,2516", 1000, "Hunter Ammos");
        EndIF();
    }
}
