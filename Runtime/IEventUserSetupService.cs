using System;
using Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model;

namespace Xprees.EventLogging
{
    public interface IEventUserSetupService
    {
        OGPlayer CurrentPlayer { get; set; }

        /// Group slug of the player in the current scenario (optional, empty = no group). Stamped on every event.
        string Group { get; }

        /// Raised after a new player was assigned (the sender updates the player profile on the server).
        event Action<OGPlayer> PlayerChanged;

        void SetPlayerId(string playerId);
        void SetName(string playerName);
    }
}