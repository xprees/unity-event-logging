using System;
using UnityEngine;

namespace Xprees.EventLogging.Api.Model
{
    [Serializable]
    public class User
    {
        public long? id = null;

        [Tooltip("Can be left blank and be filled individually later by the UserSetupService.")]
        public string formId;

        [HideInInspector]
        public bool formSubmitted = false;

        public string name;
        public string email = null;
    }
}