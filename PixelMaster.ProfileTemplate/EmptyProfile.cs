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

public class EmptyProfile : IPMProfile //it is important to implement 'IPMProfile' interface, but u can change 'MyProfile' to any name
{
    List<Mob> avoidMobs = new List<Mob>()
    {

    };
    List<Blackspot> blackspots = new List<Blackspot>()
    {
        
    };
    List<Blackspot> ignoredAreas = new List<Blackspot>()
    {

    };
    List<int> wantedObjects = new List<int>()
    {

    };
    List<MailBox> mailboxes = new List<MailBox>()
    {

    };
    List<Vendor> vendors = new List<Vendor>()
    {

    };
    PMProfileSettings CreateSettings()
    {
        return new PMProfileSettings()
        {
            ProfileName = "",
            Author = "",
            Description = "",
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
            Foods = (20, new int[] {  }),
            Drinks = (20, new int[] {  }),
            Arrows = (1000, new int[] {  }),
            Bullets = (1000, new int[] {  }),

            //Keep items are items you want to skip from selling or mailing
            KeepItems = new List<int> {

            },

            OnTaskFailure = TaskFailureBehavior.ReturnFailure,
        };
    }
    public IPMProfileContext Create()
    {
        var ME = ObjectManager.Instance.Player;//just a shortcut to use inside profile
        var settings = CreateSettings(); //Creates profile settings from the above method
        StartProfile(settings); //Starting the profile using the settings
        //-------------------------------START PROFILE-------------------------------

        //-------------------------------END PROFILE-------------------------------
        return EndProfile();
    }
}
