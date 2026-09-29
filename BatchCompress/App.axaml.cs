using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BatchCompress.ViewModels;
using BatchCompress.Views;

namespace BatchCompress
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var vm = new MainWindowViewModel();
                desktop.MainWindow = new MainWindow
                {
                    DataContext = vm
                };
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}
