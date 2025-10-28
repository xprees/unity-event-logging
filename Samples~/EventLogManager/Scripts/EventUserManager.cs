using System;
using UnityEngine;
using Xprees.EventLogging.Api.Model;

namespace Xprees.EventLogging.Samples.Scripts
{
    /// Event user manager for the elevator experience.
    /// Provides user data for event logging.
    [DisallowMultipleComponent]
    public class EventUserManager : MonoBehaviour, IEventUserSetupService
    {
        // Changed default prefix of player GUIDs to easier separate event users from your project.
        private const string defaultPrefix = "event-logging-sample";

        [Header("Settings")]
        [Tooltip("Prefix for the generated user GUIDs. Can be used to identify users from. e.g., event-logging-sample")]
        [SerializeField] private string guidPrefix = "vr-mp-elevator";

        [Tooltip("Name of the player that will be used in event logs user.")]
        [SerializeField] public string playerName = "player";

        public User CurrentUser { get; set; }

        private void Awake() => SetupUserIfNeeded();

        public void SetFormId(string actorId)
        {
            SetupUserIfNeeded();
            CurrentUser.formId = actorId;
        }

        public void SetName(string username)
        {
            SetupUserIfNeeded();
            CurrentUser.name = username;
        }

        public void SetEmail(string email)
        {
            SetupUserIfNeeded();
            CurrentUser.email = email;
        }

        private void SetupUserIfNeeded()
        {
            CurrentUser ??= new User
            {
                // Random unique user id
                formId = $"{guidPrefix?.Trim() ?? defaultPrefix}-{Guid.NewGuid():D}",
                name = playerName,
                email = null,
            };
        }
    }
}