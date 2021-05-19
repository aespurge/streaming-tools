using System;

namespace streaming_tools.ViewModels {
    /// <summary>
    ///     The UI representation of a twitch account.
    /// </summary>
    public class AccountViewModel : ViewModelBase {
        /// <summary>
        ///     The username of the twitch account.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        ///     The delegate provided from the parent control when editing an account.
        /// </summary>
        public Action EditAccount { get; set; }

        /// <summary>
        ///     The delegate provided from the parent control when deleting an account.
        /// </summary>
        public Action DeleteAccount { get; set; }

        /// <summary>
        ///     Handles executing the edit account action.
        /// </summary>
        public void EditAccountCommand() {
            EditAccount?.Invoke();
        }

        /// <summary>
        ///     Handles executing the delete account action.
        /// </summary>
        public void DeleteAccountCommand() {
            DeleteAccount?.Invoke();
        }
    }
}