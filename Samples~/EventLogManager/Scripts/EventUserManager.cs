using System;
using UnityEngine;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Util;

namespace Xprees.EventLogging.Samples.Scripts
{
    /// Provides the player (id, name, group) for event logging.
    [DisallowMultipleComponent]
    public class EventUserManager : MonoBehaviour, IEventUserSetupService
    {
        private const string playerIdPrefsKey = "EventLoggingSamplePlayerId";

        [Header("Settings")]
        [Tooltip("Prefix of the generated player id, e.g. player -> player-<uuidv7>")]
        [SerializeField] private string playerIdPrefix = "player";

        [Tooltip("Name of the player sent to the player profile.")]
        [SerializeField] private string playerName = "player";

        [Tooltip("Optional group slug (a-z 0-9 . _ -) of the player in the scenario, e.g. control-a.")]
        [SerializeField] private string group = "";

        public OGPlayer CurrentPlayer { get; set; }

        public string Group => group;

        public event Action<OGPlayer> PlayerChanged;

        private void Awake() => SetupPlayerIfNeeded();

        public void SetPlayerId(string playerId)
        {
            SetupPlayerIfNeeded();
            CurrentPlayer.id = playerId;
            PlayerChanged?.Invoke(CurrentPlayer);
        }

        public void SetName(string newName)
        {
            SetupPlayerIfNeeded();
            CurrentPlayer.name = newName;
            PlayerChanged?.Invoke(CurrentPlayer);
        }

        private void SetupPlayerIfNeeded()
        {
            if (CurrentPlayer != null) return;

            // The id is the process-mining case id, so it has to stay the same across sessions of the install.
            var id = PlayerPrefs.GetString(playerIdPrefsKey, null);
            if (string.IsNullOrEmpty(id))
            {
                id = $"{OGValidation.SanitizePlayerId(playerIdPrefix)}-{Uuid7.New()}";
                PlayerPrefs.SetString(playerIdPrefsKey, id);
            }

            CurrentPlayer = new OGPlayer { id = id, name = playerName };
        }
    }
}
