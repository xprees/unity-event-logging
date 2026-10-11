using System;
using UnityEngine;

namespace Xprees.EventLogging.Api.Legacy.Model
{
    [Serializable]
    [Obsolete(
        "Legacy CF-Bucket API - the Open Game Analytics backend has no /logs/batch or /logs/scenarios. Use the v1 client. Removed in the next major version.")]
    public class User
    {
        [Tooltip("Can be left blank and be filled individually later by the UserSetupService.")]
        public string formId;

        [HideInInspector]
        public bool formSubmitted = false;

        public string name;
        public string email = null;
    }
}