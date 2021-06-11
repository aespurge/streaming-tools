namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;

    using Newtonsoft.Json;

    using ReactiveUI;

    using streaming_tools.Models;
    using streaming_tools.Views;

    using TwitchLib.Api;

    /// <summary>
    ///     Handles updating the list and credentials for twitch accounts.
    /// </summary>
    public class AccountsViewModel : ViewModelBase {
        /// <summary>
        ///     The singleton collection for configuring the application.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     The OAuth token for api of the currently added/edited twitch account.
        /// </summary>
        private string? apiOAuth;

        /// <summary>
        ///     The date time at which the OAuth token expires.
        /// </summary>
        private DateTime? apiTokenExpires;

        /// <summary>
        ///     The refresh token used to refresh the <see cref="ApiOAuth" />.
        /// </summary>
        private string? apiTokenRefresh;

        /// <summary>
        ///     The OAuth token for chat of the currently added/edited twitch account.
        /// </summary>
        private string? chatOAuth;

        /// <summary>
        ///     The client id associated with the app on the twitch account.
        /// </summary>
        private string? clientId;

        /// <summary>
        ///     The client secret of the currently added/edited twitch account.
        /// </summary>
        private string? clientSecret;

        /// <summary>
        ///     The "code" for getting a api OAuth token of the currently added/edited twitch account.
        /// </summary>
        private string? code;

        /// <summary>
        ///     A value indicating whether the account is the account the user uses to stream.
        /// </summary>
        private bool isUsersStreamingAccount;

        /// <summary>
        ///     The redirect uri for the registered client id/secret.
        /// </summary>
        private string? redirectUrl;

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
        ///     Gets or sets the api OAuth token of the currently added/edited twitch account.
        /// </summary>
        public string? ApiOAuth {
            get => this.apiOAuth;
            set => this.RaiseAndSetIfChanged(ref this.apiOAuth, value);
        }

        /// <summary>
        ///     Gets or sets the OAuth token for chat of the currently added/edited twitch account.
        /// </summary>
        public string? ChatOAuth {
            get => this.chatOAuth;
            set => this.RaiseAndSetIfChanged(ref this.chatOAuth, value);
        }

        /// <summary>
        ///     Gets or sets the client id associated with the app on the twitch account.
        /// </summary>
        public string? ClientId {
            get => this.clientId;
            set => this.RaiseAndSetIfChanged(ref this.clientId, value);
        }

        /// <summary>
        ///     Gets or sets the client secret of the currently added/edited twitch account.
        /// </summary>
        public string? ClientSecret {
            get => this.clientSecret;
            set => this.RaiseAndSetIfChanged(ref this.clientSecret, value);
        }

        /// <summary>
        ///     Gets or sets the "code" for getting a api OAuth token of the currently added/edited twitch account.
        /// </summary>
        public string? Code {
            get => this.code;
            set => this.RaiseAndSetIfChanged(ref this.code, value);
        }

        /// <summary>
        ///     Gets or sets a value indicating whether the account is the account the user uses to stream.
        /// </summary>
        public bool IsUsersStreamingAccount {
            get => this.isUsersStreamingAccount;
            set => this.RaiseAndSetIfChanged(ref this.isUsersStreamingAccount, value);
        }

        /// <summary>
        ///     Gets or sets the redirect uri for the registered client id/secret.
        /// </summary>
        public string? RedirectUrl {
            get => this.redirectUrl;
            set => this.RaiseAndSetIfChanged(ref this.redirectUrl, value);
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
            this.ClearForm();
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
        ///     Launches the twitch OAuth webpage.
        /// </summary>
        public async void LaunchApiOAuthWebpage() {
            HttpClient client = new HttpClient();
            var response = await client.PostAsync($"https://id.twitch.tv/oauth2/token?client_id={this.ClientId}&client_secret={this.ClientSecret}&code={this.Code}&grant_type=authorization_code&redirect_uri={this.RedirectUrl}", new StringContent(""));
            if (!response.IsSuccessStatusCode) {
                this.ApiOAuth = "";
                return;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<TwitchTokenResponseJson>(responseString);
            if (null == json)
                return;

            this.ApiOAuth = json.access_token;
            this.apiTokenRefresh = json.refresh_token;
            this.apiTokenExpires = DateTime.UtcNow + new TimeSpan(0, 0, json.expires_in - 300);
        }

        /// <summary>
        ///     Launches the twitch OAuth webpage.
        /// </summary>
        public void LaunchChatOAuthWebpage() {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Constants.TWITCH_CHAT_OAUTH_SITE}") { CreateNoWindow = true });
        }

        /// <summary>
        ///     Launches the webpage to get the "code" from.
        /// </summary>
        public void LaunchCodeWebpage() {
            var twitchClient = new TwitchAPI();
            twitchClient.Settings.ClientId = this.ClientId;
            twitchClient.Settings.Secret = this.ClientSecret;
            var url = twitchClient.V5.Auth.GetAuthorizationCodeUrl(this.RedirectUrl, Constants.TWITCH_AUTH_SCOPES).Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }

        /// <summary>
        ///     Launches the twitch developer webpage.
        /// </summary>
        public void LaunchDeveloperWebpage() {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {Constants.TWITCH_DEVELOPER_SITE}") { CreateNoWindow = true });
        }

        /// <summary>
        ///     Saves the current twitch account details.
        /// </summary>
        public void SaveAccount() {
            if (string.IsNullOrWhiteSpace(this.Username) || string.IsNullOrWhiteSpace(this.ClientId) || string.IsNullOrWhiteSpace(this.ClientSecret) || string.IsNullOrWhiteSpace(this.Code) || string.IsNullOrWhiteSpace(this.ApiOAuth) || string.IsNullOrWhiteSpace(this.ChatOAuth) || string.IsNullOrWhiteSpace(this.RedirectUrl) || null == this.config.TwitchAccounts || null == this.apiTokenRefresh)
                return;

            var existingAccount = this.config.GetTwitchAccount(this.Username);
            var isNew = null == existingAccount;
            if (isNew) {
                existingAccount = new TwitchAccount();
                this.config.TwitchAccounts.Add(existingAccount);
            }

#pragma warning disable 8602
            existingAccount.Username = this.Username;
#pragma warning restore 8602
            existingAccount.ClientId = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ClientId));
            existingAccount.ClientSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ClientSecret));
            existingAccount.RedirectUrl = this.RedirectUrl;
            existingAccount.Code = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.Code));
            existingAccount.ApiOAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ApiOAuth));
            existingAccount.ChatOAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ChatOAuth));
            existingAccount.IsUsersStreamingAccount = this.IsUsersStreamingAccount;
            existingAccount.ApiOAuthRefresh = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.apiTokenRefresh));
            existingAccount.ApiOAuthExpires = this.apiTokenExpires;
            this.config.WriteConfiguration();

            if (isNew)
                this.Accounts.Add(new AccountView { DataContext = this.CreateAccountViewModel(this.Username) });

            this.ClearForm();
        }

        /// <summary>
        ///     Clears the form.
        /// </summary>
        private void ClearForm() {
            this.Username = "";
            this.ApiOAuth = "";
            this.ChatOAuth = "";
            this.ClientId = "";
            this.ClientSecret = "";
            this.RedirectUrl = "";
            this.Code = "";
            this.apiTokenExpires = DateTime.MinValue;
            this.apiTokenRefresh = "";
            this.IsUsersStreamingAccount = false;
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
                this.ClearForm();
                return;
            }

            this.Username = existingAccount.Username;
            this.ClientId = null != existingAccount.ClientId ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ClientId)) : "";
            this.ClientSecret = null != existingAccount.ClientSecret ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ClientSecret)) : "";
            this.RedirectUrl = existingAccount.RedirectUrl;
            this.Code = null != existingAccount.Code ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.Code)) : "";
            this.ApiOAuth = null != existingAccount.ApiOAuth ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ApiOAuth)) : "";
            this.ChatOAuth = null != existingAccount.ChatOAuth ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ChatOAuth)) : "";
            this.IsUsersStreamingAccount = existingAccount.IsUsersStreamingAccount;
            this.apiTokenExpires = existingAccount.ApiOAuthExpires;
            this.apiTokenRefresh = null != existingAccount.ApiOAuthRefresh ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ApiOAuthRefresh)) : "";
        }
    }
}