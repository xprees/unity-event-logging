using UnityEngine;
using Xprees.Core;

namespace Xprees.EventLogging.ScriptableObjects
{
    [CreateAssetMenu(menuName = "EventLogging/New Event", fileName = "Event")]
    [StatefulLifetime(StateLifetime.Persistent)]
    public class EventSO : DescriptionBaseSO
    {
        [Tooltip("Scenario name")]
        public string scenario;

        [Tooltip("Name of the event.")]
        public string eventName;

        [Tooltip("Event data/arguments (optional)")]
        public string eventData;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning($"{nameof(EventSO)} - {name} has null/empty {nameof(eventName)}", this);
            }
        }
#endif
    }
}