namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    using ReactiveUI;

    using streaming_tools.Views;

    /// <summary>
    ///     Handles updating the list and credentials for twitch accounts.
    /// </summary>
    public class AccountsViewModel : ViewModelBase {
        /// <summary>
        ///     The singleton collection for configuring the application.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     The client id associated with the app on the twitch account.
        /// </summary>
        private string? clientId;

        /// <summary>
        ///     The OAuth token of the currently added/edited twitch account.
        /// </summary>
        private string? oAuth;

        /// <summary>
        ///     The username of the currently added/edited twitch account.
        /// </summary>
        private string? username;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountsViewModel" /> class.
        /// </summary>
        public AccountsViewModel() {
            this.Accounts = new ObservableCollection<AccountView>();

            // Loop through the list of existing accounts and add them to the UI.
            this.config = Configuration.Instance;

            if (null == this.config.TwitchAccounts)
                return;

            foreach (var user in this.config.TwitchAccounts) {
                if (null == user.Username)
                    continue;

                var viewModel = this.CreateAccountViewModel(user.Username);
                var control = new AccountView { DataContext = viewModel };
                this.Accounts.Add(control);
            }
        }

        /// <summary>
        ///     Gets or sets the list of twitch accounts.
        /// </summary>
        public ObservableCollection<AccountView> Accounts { get; set; }

        /// <summary>
        ///     Gets or sets the client id associated with the app on the twitch account.
        /// </summary>
        public string? ClientId {
            get => this.clientId;
            set => this.RaiseAndSetIfChanged(ref this.clientId, value);
        }

        /// <summary>
        ///     Gets or sets the OAuth token of the currently added/edited twitch account.
        /// </summary>
        public string? OAuth {
            get => this.oAuth;
            set => this.RaiseAndSetIfChanged(ref this.oAuth, value);
        }

        /// <summary>
        ///     Gets or sets the username of the currently added/edited twitch account.
        /// </summary>
        public string? Username {
            get => this.username;
            set => this.RaiseAndSetIfChanged(ref this.username, value);
        }

        /// <summary>
        ///     Clears the form when we cancel adding/editing a twitch account.
        /// </summary>
        public void CancelEditing() {
            this.Username = "";
            this.OAuth = "";
            this.ClientId = "";
        }

        /// <summary>
        ///     Deletes the specified twitch account.
        /// </summary>
        /// <param name="twitchUsername">The twitch account to delete.</param>
        public void DeleteAccount(string? twitchUsername) {
            if (string.IsNullOrWhiteSpace(twitchUsername) || null == this.config.TwitchAccounts)
                return;

            var existingControl = this.Accounts.FirstOrDefault(a => twitchUsername.Equals((a.DataContext as AccountViewModel)?.Username, StringComparison.InvariantCultureIgnoreCase));
            if (null != existingControl)
                this.Accounts.Remove(existingControl);

            var existingAccount = this.config.GetTwitchAccount(twitchUsername);
            if (null == existingAccount)
                return;

            this.config.TwitchAccounts.Remove(existingAccount);
            this.config.WriteConfiguration();
        }

        /// <summary>
        ///     Launches the twitch developer webpage.
        /// </summary>
        public void LaunchDeveloperWebpage() {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Constants.TWITCH_DEVELOPER_SITE}") { CreateNoWindow = true });
        }

        /// <summary>
        ///     Launches the twitch OAuth webpage.
        /// </summary>
        public void LaunchOAuthWebpage() {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Constants.TWITCH_OAUTH_SITE}") { CreateNoWindow = true });
        }

        /// <summary>
        ///     Saves the current twitch account details.
        /// </summary>
        public void SaveAccount() {
            if (string.IsNullOrWhiteSpace(this.Username) || string.IsNullOrWhiteSpace(this.OAuth) || string.IsNullOrWhiteSpace(this.ClientId) || null == this.config.TwitchAccounts)
                return;

            var existingAccount = this.config.GetTwitchAccount(this.Username);
            if (null == existingAccount) {
                this.config.TwitchAccounts.Add(new TwitchAccount { Username = this.Username, OAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.OAuth)), ClientId = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ClientId)) });

                this.Accounts.Add(new AccountView { DataContext = this.CreateAccountViewModel(this.Username) });
                this.config.WriteConfiguration();
                return;
            }

            existingAccount.Username = this.Username;
            existingAccount.OAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.OAuth));
            existingAccount.ClientId = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ClientId));
            this.config.WriteConfiguration();
        }

        /// <summary>
        ///     Creates a new account view model.
        /// </summary>
        /// <param name="twitchUsername">The username of the currently added twitch account.</param>
        /// <returns>A new instance of the view model.</returns>
        private AccountViewModel CreateAccountViewModel(string twitchUsername) {
            return new() { Username = twitchUsername, DeleteAccount = () => this.DeleteAccount(twitchUsername), EditAccount = () => this.EditAccount(twitchUsername) };
        }

        /// <summary>
        ///     Edits an existing twitch account.
        /// </summary>
        /// <param name="twitchUsername">The username of the twitch account to edit..</param>
        private void EditAccount(string twitchUsername) {
            if (string.IsNullOrWhiteSpace(twitchUsername))
                return;

            var existingAccount = this.config.GetTwitchAccount(twitchUsername);
            if (null == existingAccount) {
                this.Username = "";
                this.OAuth = "";
                this.ClientId = "";
                return;
            }

            this.Username = existingAccount.Username;
            this.OAuth = existingAccount.OAuth;
            this.ClientId = existingAccount.ClientId;
        }
    }
}