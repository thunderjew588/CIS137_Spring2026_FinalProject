using Microsoft.Extensions.DependencyInjection;
using System;

namespace MustangGongShow.IOC2
{
    public static class Registrar
    {
        public static IServiceProvider ConfigureServices()
        {
            // TODO Change to load reflectively from some file list

            var services = new ServiceCollection();

            // Services
            //services.AddSingleton<IFOF8FileService, FOF8FileService>();                                 // Loads game logs and provides loaded FOF8Game object
            //services.AddSingleton<IFOF8ConfigService, FOF8ConfigService>();                             // Settings service that reads the config file - we can almost copy paste the game log service, but the parsing and access is differently shaped
            //services.AddSingleton<IFOF8DataService, FOF8DataService>();                                 // FOF8 Data query service - first item will be to get all players names for a given team - will use this to drive player filters for GAT

            // ViewModels
            //services.AddSingleton<ISwitchboardViewModel, SwitchboardViewModel>();                       // VM for main switchboard window
            //services.AddTransient<IGeneralAnalysisViewModel, GeneralAnalysisViewModel>();               // VM for generic analysis
            //services.AddTransient<IMultiAnalysisViewModel, MultiAnalysisViewModel>();               // VM for generic analysis
            //services.AddTransient<IRunAnalysisViewModel, RunAnalysisViewModel>();                       // VM for analysis of run plays
            //services.AddTransient<IPassAnalysisViewModel, PassAnalysisViewModel>();                     // VM for analysis of pass plays
            //services.AddTransient<IParticipationAnalysisViewModel, ParticipationAnalysisViewModel>();   // VM for player participation analysis
            //services.AddTransient<ISetPersonnelViewModel, SetPersonnelViewModel>();   // VM for player participation analysis
            //services.AddTransient<IPlayerBrowserViewModel, PlayerBrowserViewModel>();
            //services.AddTransient<IPlaybookBrowserViewModel, PlaybookBrowserViewModel>();
            //services.AddTransient<IDraftBrowserViewModel, DraftBrowserViewModel>();
            //services.AddTransient<IPlayBrowserViewModel, PlayBrowserViewModel>();
            //services.AddTransient<IPassAnalysisViewModel, PassAnalysisViewModel>();

            return services.BuildServiceProvider();
        }
    }

    [Serializable]
    internal class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException()
        {
        }

        public ServiceNotFoundException(string message) : base(message)
        {
        }

        public ServiceNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        //protected ServiceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        //{
        //}
    }
}
