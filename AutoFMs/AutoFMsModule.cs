using PixelMaster.Services.Logging;
using PixelMaster.Services.Products;
using Prism.Ioc;
using Prism.Modularity;

namespace Plugins.AutoFMs
{
    /// <summary>
    /// Each module(Plugins, Botbases,...) that bot loads must implement 'IModule' interface.
    /// Then it should register it's Main starting class that implements the corresponding interface in the 'RegisterTypes' method down bellow.
    /// In this case it is a plugin and we register 'AutoFMsPlugin' class that implement 'IPlugin' interface using it's unique key: 'AutoFMsPlugin.ProductKey'
    /// </summary>
    [ModuleDependency("CoreModule")]
    public class AutoFMsModule : IModule
    {
        internal static ILogService Log { get; private set; }
        //internal static IUserProducts UserProducts { get; private set; }
        public void OnInitialized(IContainerProvider containerProvider)
        {
            Log = containerProvider.Resolve<ILogService>();
            //CustomClass = containerProvider.Resolve<ICustomClass>();
            //UserProducts = containerProvider.Resolve<IUserProducts>();
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IPlugin, AutoFMsPlugin>(AutoFMsPlugin.ProductKey);
        }
    }
}