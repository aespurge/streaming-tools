using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace streaming_tools.Views {
    public class AccountsView : UserControl {
        public AccountsView() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
}