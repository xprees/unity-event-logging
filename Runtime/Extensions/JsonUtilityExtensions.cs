using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Xprees.EventLogging.Extensions
{
    public static class JsonUtilityExtensions
    {
        public static T[] FromJsonArray<T>(string rawString) =>
            // JsonUtility can't deserialize a root-level array, so wrap it in an object first.
            JsonUtility.FromJson<ArrayWrapper<T>>($"{{\"items\":{rawString}}}").items;

        [Serializable]
        private class ArrayWrapper<T>
        {
            public T[] items;
        }

        public static string ToJsonArray(IEnumerable array) =>
            // JsonUtility can't serialize a root-level collection, so build the array manually for those.
            $"[{string.Join(",", array.Cast<object>().Select(JsonUtility.ToJson))}]";
    }
}