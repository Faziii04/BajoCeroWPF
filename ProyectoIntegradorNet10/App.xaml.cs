using System.Windows;
using ProyectoIntegradorNet10.Services;
namespace ProyectoIntegradorNet10
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GlobalVars.ApplyInitialTheme();
        }
    }
}

