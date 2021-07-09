namespace streaming_tools.ViewModels {
    using System;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Timers;
    using Models;
    using Newtonsoft.Json;
    using ReactiveUI;
    using Views;

    /// <summary>
    ///     Handles updating the list and credentials for twitch accounts.
    /// </summary>
    public class AccountsViewModel : ViewModelBase {
        /// <summary>
        ///     The singleton collection for configuring the application.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        ///     The timer that looks for the copied OAuth token on the clipboard.
        /// </summary>
        private readonly Timer oauthCodeCheckTimer;

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
        ///     The "code" for getting a api OAuth token of the currently added/edited twitch account.
        /// </summary>
        private string? code;

        /// <summary>
        ///     A value indicating whether the account is the account the user uses to stream.
        /// </summary>
        private bool isUsersStreamingAccount;

        /// <summary>
        ///     The username of the currently added/edited twitch account.
        /// </summary>
        private string? username;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AccountsViewModel" /> class.
        /// </summary>
        public AccountsViewModel() {
            this.Accounts = new ObservableCollection<AccountView>();
            this.oauthCodeCheckTimer = new Timer(100);
            this.oauthCodeCheckTimer.AutoReset = false;
            this.oauthCodeCheckTimer.Elapsed += this.OauthCodeCheckTimer_Elapsed;

            // Loop through the list of existing accounts and add them to the UI.
            this.config = Configuration.Instance;

            if (null == this.config.TwitchAccounts) {
                return;
            }

            foreach (var user in this.config.TwitchAccounts) {
                if (null == user.Username) {
                    continue;
                }

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
            if (string.IsNullOrWhiteSpace(twitchUsername) || null == this.config.TwitchAccounts) {
                return;
            }

            var existingControl = this.Accounts.FirstOrDefault(a => twitchUsername.Equals((a.DataContext as AccountViewModel)?.Username, StringComparison.InvariantCultureIgnoreCase));
            if (null != existingControl) {
                this.Accounts.Remove(existingControl);
            }

            var existingAccount = this.config.GetTwitchAccount(twitchUsername);
            if (null == existingAccount) {
                return;
            }

            this.config.TwitchAccounts.Remove(existingAccount);
            this.config.WriteConfiguration();
        }

        /// <summary>
        ///     Gets the OAuth token.
        /// </summary>
        public async void GetApiOAuthToken() {
            this.ApiOAuth = "";
            if (string.IsNullOrWhiteSpace(this.Code)) {
                return;
            }

            HttpClient client = new();
            var response = await client.PostAsync($"{Constants.NULLINSIDE_TWITCH_OAUTH}?code={this.Code}", new StringContent(""));
            if (!response.IsSuccessStatusCode) {
                this.ApiOAuth = "";
                return;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<TwitchTokenResponseJson>(responseString);
            if (null == json) {
                return;
            }

            this.ApiOAuth = json.access_token;
            this.apiTokenRefresh = json.refresh_token;
            this.apiTokenExpires = DateTime.UtcNow + new TimeSpan(0, 0, json.expires_in - 300);
        }

        /// <summary>
        ///     Launches the webpage to get the "code" from.
        /// </summary>
        public async void LaunchCodeWebpage() {
            var url = await this.GetCodeUrl();
            if (null == url) {
                return;
            }

            Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true });
            this.oauthCodeCheckTimer.Start();
        }

        /// <summary>
        ///     Saves the current twitch account details.
        /// </summary>
        public void SaveAccount() {
            if (string.IsNullOrWhiteSpace(this.Username) || string.IsNullOrWhiteSpace(this.ApiOAuth) || null == this.config.TwitchAccounts || null == this.apiTokenRefresh) {
                return;
            }

            var existingAccount = this.config.GetTwitchAccount(this.Username);
            var isNew = null == existingAccount;
            if (isNew) {
                existingAccount = new TwitchAccount();
                this.config.TwitchAccounts.Add(existingAccount);
            }

#pragma warning disable 8602
            existingAccount.Username = this.Username;
#pragma warning restore 8602
            existingAccount.ApiOAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.ApiOAuth));
            existingAccount.IsUsersStreamingAccount = this.IsUsersStreamingAccount;
            existingAccount.ApiOAuthRefresh = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.apiTokenRefresh));
            existingAccount.ApiOAuthExpires = this.apiTokenExpires;
            this.config.WriteConfiguration();

            if (isNew) {
                this.Accounts.Add(new AccountView { DataContext = this.CreateAccountViewModel(this.Username) });
            }

            this.ClearForm();
        }

        /// <summary>
        ///     Clears the form.
        /// </summary>
        private void ClearForm() {
            this.Username = "";
            this.ApiOAuth = "";
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
            if (string.IsNullOrWhiteSpace(twitchUsername)) {
                return;
            }

            var existingAccount = this.config.GetTwitchAccount(twitchUsername);
            if (null == existingAccount) {
                this.ClearForm();
                return;
            }

            this.Username = existingAccount.Username;
            this.Code = "";
            this.ApiOAuth = null != existingAccount.ApiOAuth ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ApiOAuth)) : "";
            this.IsUsersStreamingAccount = existingAccount.IsUsersStreamingAccount;
            this.apiTokenExpires = existingAccount.ApiOAuthExpires;
            this.apiTokenRefresh = null != existingAccount.ApiOAuthRefresh ? Encoding.UTF8.GetString(Convert.FromBase64String(existingAccount.ApiOAuthRefresh)) : "";
        }

        /// <summary>
        ///     Gets the URL to launch in the browser for the user to get the "code" to generate an OAuth token.
        /// </summary>
        /// <returns>The url if successful, null otherwise.</returns>
        private async Task<string?> GetCodeUrl() {
            if (null == Constants.CLIPBOARD) {
                return null;
            }

            await Constants.CLIPBOARD.SetTextAsync("");
            var client = new HttpClient();
            var nullinsideResponse = await client.GetAsync(Constants.NULLINSIDE_TWITCH_CODE);
            if (!nullinsideResponse.IsSuccessStatusCode) {
                return null;
            }

            var body = await nullinsideResponse.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) {
                return null;
            }

            var json = JsonConvert.DeserializeObject<NullInsideTwitchCodeResponseJson>(body);
            if (null == json) {
                return null;
            }

            return json.url;
        }

        /// <summary>
        ///     Searches the clipboard for the "code" to generate an OAuth token.
        /// </summary>
        /// <param name="sender">The timer.</param>
        /// <param name="e">The event arguments.</param>
        private async void OauthCodeCheckTimer_Elapsed(object sender, ElapsedEventArgs e) {
            if (null == Constants.CLIPBOARD) {
                return;
            }

            var text = await Constants.CLIPBOARD.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text)) {
                this.oauthCodeCheckTimer.Start();
                return;
            }

            this.Code = text;
        }
    }
}