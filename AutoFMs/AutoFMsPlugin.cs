using PixelMaster.Services.Behaviors;
using PixelMaster.Services.Products;
using Plugins.Plugins.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using PixelMaster.Core.Managers;

namespace Plugins.AutoFMs
{
    /// <summary>
    /// Main starting class of every plugin must implement 'IPlugin' interface.
    /// Also must be decorated with a unique key as below.
    /// </summary>
    [ProductKey(ProductKey)]
    internal class AutoFMsPlugin : IPlugin
    {
        internal const string ProductKey = "Key_AutoFMs";
        internal const string PluginName = "AutoFMs";

        private AutoFMsBehavior behavior;
        private Version version;
        public AutoFMsPlugin()
        {
            behavior = new AutoFMsBehavior();
            version = new Version(1, 0);
        }
        public string Id => ProductKey;
        public string Name => PluginName;
        public string Author => "PixelMaster";
        public string Description => "When there is a Flight Master nearby which is not already learned, bot will move and interact with it.";
        public Version Version => version;

        public IBehaviorContext Behavior => behavior;

        public bool HasGUI => false;

        public bool IsEnabled { get; set; }

        public bool NeedsToRun => !ObjectManager.Instance.Player.IsInCombat && behavior.AnyNearbyTaxiToLearn();

        public void Dispose()
        {
            behavior.Dispose();
        }

        public void ShowGUI(Action<string> callback)
        {

        }
    }
}
