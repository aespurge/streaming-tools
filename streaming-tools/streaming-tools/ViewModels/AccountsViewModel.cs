using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ReactiveUI;
using streaming_tools.Views;

namespace streaming_tools.ViewModels {
    /// <summary>
    ///     Handles updating the list and credentials for twitch accounts.
    /// </summary>
    public class AccountsViewModel : ViewModelBase {
        /// <summary>
        ///     The singleton collection for configuring the application.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     The OAuth token of the currently added/edited twitch account.
        /// </summary>
        private string oAuth;

        /// <summary>
        ///     The username of the currently added/edited twitch account.
        /// </summary>
        private string username;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountsViewModel" /> class.
        /// </summary>
        public AccountsViewModel() {
            Accounts = new ObservableCollection<AccountView>();

            // Loop through the list of existing accounts and add them to the UI.
            config = Configuration.Instance();
            foreach (var user in config.TwitchAccounts) {
                var viewModel = CreateAccountViewModel(user.Username);

                var control = new AccountView {
                    DataContext = viewModel
                };

                Accounts.Add(control);
            }
        }

        /// <summary>
        ///     The username of the currently added/edited twitch account.
        /// </summary>
        public string Username {
            get => username;
            set => this.RaiseAndSetIfChanged(ref username, value);
        }

        /// <summary>
        ///     The OAuth token of the currently added/edited twitch account.
        /// </summary>
        public string OAuth {
            get => oAuth;
            set => this.RaiseAndSetIfChanged(ref oAuth, value);
        }

        /// <summary>
        ///     The list of twitch accounts.
        /// </summary>
        public ObservableCollection<AccountView> Accounts { get; set; }

        /// <summary>
        ///     Creates a new account view model.
        /// </summary>
        /// <param name="username">The username of the currently added twitch account.</param>
        /// <returns>A new instance of the view model.</returns>
        private AccountViewModel CreateAccountViewModel(string username) {
            return new() {
                Username = username,
                DeleteAccount = () => DeleteAccount(username),
                EditAccount = () => EditAccount(username)
            };
        }

        /// <summary>
        ///     Edits an existing twitch account.
        /// </summary>
        /// <param name="username">The username of the twitch account to edit..</param>
        private void EditAccount(string username) {
            if (string.IsNullOrWhiteSpace(username))
                return;

            var existingAccount = config.TwitchAccounts.FirstOrDefault(a => username.Equals(a.Username, StringComparison.InvariantCultureIgnoreCase));
            if (null == existingAccount) {
                Username = "";
                OAuth = "";
                return;
            }

            Username = existingAccount.Username;
            OAuth = existingAccount.OAuth;
        }

        /// <summary>
        ///     Saves the current twitch account details.
        /// </summary>
        public void SaveAccount() {
            if (string.IsNullOrWhiteSpace(Username))
                return;

            var existingAccount = config.TwitchAccounts.FirstOrDefault(a => Username.Equals(a.Username, StringComparison.InvariantCultureIgnoreCase));
            if (null == existingAccount) {
                config.TwitchAccounts.Add(new TwitchAccount {
                    Username = Username,
                    OAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(OAuth))
                });

                Accounts.Add(new AccountView {
                    DataContext = CreateAccountViewModel(Username)
                });

                config.WriteConfiguration();
                return;
            }

            existingAccount.Username = Username;
            existingAccount.OAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(OAuth));
        }

        /// <summary>
        ///     Deletes the specified twitch account.
        /// </summary>
        /// <param name="username">The twitch account to delete.</param>
        public void DeleteAccount(string username) {
            if (string.IsNullOrWhiteSpace(username))
                return;

            var existingControl = Accounts.FirstOrDefault(a => username.Equals(((AccountViewModel) a.DataContext).Username, StringComparison.InvariantCultureIgnoreCase));
            if (null != existingControl)
                Accounts.Remove(existingControl);

            var existingAccount = config.TwitchAccounts.FirstOrDefault(a => username.Equals(a.Username, StringComparison.InvariantCultureIgnoreCase));
            if (null != existingAccount) {
                config.TwitchAccounts.Remove(existingAccount);
                config.WriteConfiguration();
            }
        }

        /// <summary>
        ///     Clears the form when we cancel adding/editing a twitch account.
        /// </summary>
        public void CancelEditing() {
            Username = "";
            OAuth = "";
        }

        /// <summary>
        ///     Launches the twitch OAuth webpage.
        /// </summary>
        public void LaunchOAuthWebpage() {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Constants.TWITCH_OAUTH_SITE}") {CreateNoWindow = true});
        }
    }
}