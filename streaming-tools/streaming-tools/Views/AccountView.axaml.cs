using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace streaming_tools.Views {
    public class AccountView : UserControl {
        public AccountView() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}