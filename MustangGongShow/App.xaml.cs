//using CommunityToolkit.Mvvm.Messaging;
//using GAT.View;
//using GAT.Interface;
//using GAT.Shell;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
//using DialogType = GAT.Interface.DialogType;
//using System.Threading.Tasks;
//using GAT.IOC2;
//using System.Diagnostics;

namespace MustangGongShow
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; private set; }
        public IDictionary<Type, Type> Views { get; private set; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        //private static IServiceProvider ConfigureServices()
        //{
        //    return ServiceRegistrar.ConfigureServices();
        //}

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Services = ConfigureServices();
            //Views = ConfigureViews();

            //var config = Current.Services.GetService<IFOF8ConfigService>();
            //config.FileName = e.Args[0];

            //// Register messages
            //WeakReferenceMessenger.Default.Register<SpawnShellMessage>(this, async (r, m) =>
            //{
            //    var vc = await ViewContainer.SpawnShell(m.Interface, Services, Views, m.DataContextAction, m.ClosingAction, m.IsSingleton);

            //    // If this is the switchboard, mark it as the main window
            //    if (m.Interface == typeof(ISwitchboardViewModel))
            //    {
            //        ViewContainer.SetMainWindow(vc);
            //    }

            //    vc.Show();
            //});
            //WeakReferenceMessenger.Default.Register<SpawnConsoleMessage>(this, (r, m) =>
            //{
            //    var process = Process.Start(m.ProcessInfo);
            //});
            //WeakReferenceMessenger.Default.Register<DialogRequestMessage>(this, async (r, m) =>
            //{
            //    await ProcessDialogRequest(m);
            //});

            //WeakReferenceMessenger.Default.Send(new SpawnShellMessage(typeof(ISwitchboardViewModel), null, ClosingAction));
        }

        //private async static Task ProcessDialogRequest(DialogRequestMessage m)
        //{
        //    FileDialog fileDialog = null;

        //    await Task.Run(() =>
        //    {
        //        switch (m.DialogType)
        //        {
        //            case DialogType.FileOpen:
        //                fileDialog = new OpenFileDialog() { Multiselect = true, };
        //                break;
        //            case DialogType.FileSave:
        //                fileDialog = new SaveFileDialog();
        //                break;
        //        }

        //        if (fileDialog == null)
        //        {
        //            m.ProcessFiles([]);
        //            return;
        //        }

        //        var fd = fileDialog.ShowDialog();

        //        if (!fd ?? false)
        //        {
        //            m.ProcessFiles([]);
        //            return;
        //        }

        //        m.ProcessFiles(fileDialog.FileNames);
        //    });
        //}

        public static IDictionary<Type, Type> ConfigureViews()
        {
            // TODO DAY 1.5 This should be driven reflectively

            var views = new Dictionary<Type, Type>
            {
                //{ typeof(ISwitchboardViewModel), typeof(SwitchboardView2) },   // VM for main switchboard window
                //{ typeof(IParticipationAnalysisViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
                //{ typeof(ISetPersonnelViewModel), typeof(SetPersonnelView) },
                //{ typeof(IGeneralAnalysisViewModel), typeof(GeneralAnalysisView) },   // VM for main switchboard window
                //{ typeof(IMultiAnalysisViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
                //{ typeof(IPlayerBrowserViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
                //{ typeof(IDraftBrowserViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
                //{ typeof(IPlayBrowserViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
                //{ typeof(IPassAnalysisViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
                //{ typeof(IPlaybookBrowserViewModel), typeof(UniversalGridView) },   // VM for main switchboard window
            };

            return views;
        }

        private void ClosingAction(object sender, CancelEventArgs eventArgs)
        {
            // TODO DAY 1.5 Do closing action
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Enable detailed WPF tracing for .NET 8 debugging
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
        }
    }
}
