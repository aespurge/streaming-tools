using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using notification_app.ViewModels;
using notification_app.Views;

namespace notification_app {
    public class App : Application {
        private TwitchChatTts twitchChatTts;

        public override void Initialize() {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted() {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow {
                    DataContext = new MainWindowViewModel()
                };

            base.OnFrameworkInitializationCompleted();
        }
    }
}