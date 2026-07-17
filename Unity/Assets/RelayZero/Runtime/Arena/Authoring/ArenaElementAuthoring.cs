using System;
using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    public abstract class ArenaElementAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string stableId = string.Empty;

        public string StableId
        {
            get { return stableId; }
        }

        protected void ConfigureStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Arena element stable IDs cannot be empty.", nameof(value));
            }

            stableId = value.Trim();
        }

        protected virtual void OnValidate()
        {
            stableId = stableId == null ? string.Empty : stableId.Trim();
        }
    }
}
