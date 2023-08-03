using UnityEngine;
using Xprees.EventLogging;
using Xprees.EventLogging.Api.Model;
using Xprees.Events.ScriptableObjects.EventLogging;
using Xprees.Variables.Primitive;

namespace Xprees.EventLogging
{
    public class EventUserSetter : MonoBehaviour, IEventUserSetupService
    {
        [Header("User")]
        [SerializeField] private User currentUser;

        [Tooltip("Overrides the formId from the user.")]
        [SerializeField] private StringVariable formIdVariable;

        public User CurrentUser
        {
            get
            {
                var formIdVal = formIdVariable.CurrentValue;
                if (!string.IsNullOrEmpty(formIdVal)) currentUser.formId = formIdVal;

                return currentUser;
            }
            set
            {
                if (string.IsNullOrEmpty(value.formId)) value.formId = formIdVariable.CurrentValue;
                currentUser = value;
            }
        }

        [Header("Listens to")]
        [SerializeField] private EventLogUserEventChannelSO setUserEvent;

        private void OnEnable()
        {
            if (setUserEvent != null) setUserEvent.onEventRaised += SetUser;
        }

        private void OnDisable()
        {
            if (setUserEvent != null) setUserEvent.onEventRaised -= SetUser;
        }

        private void SetUser(User user) => CurrentUser = user;

        public void SetFormId(string formId) => CurrentUser.formId = formId;

        public void SetName(string username) => CurrentUser.name = username;

        public void SetEmail(string email) => CurrentUser.email = email;
    }
}