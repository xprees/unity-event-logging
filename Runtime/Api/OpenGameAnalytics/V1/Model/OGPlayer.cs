using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Xprees.EventLogging.Api.OpenGameAnalytics.V1.Model
{
    /// Player profile used for event logging. The <see cref="id"/> is the process-mining case id across all scenarios of the game app.
    [Serializable]
    public class OGPlayer
    {
        [Tooltip("Unique per game app. 1-128 chars of A-Z a-z 0-9 . _ : - (empty = filled by the event logger)")]
        [FormerlySerializedAs("formId")]
        public string id;

        [Tooltip("Optional display name of the player (sent to the player profile endpoint).")]
        public string name;
    }
}