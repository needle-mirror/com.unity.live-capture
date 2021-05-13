using System;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// The base class for servers that can be managed in the Live Capture window.
    /// </summary>
    /// <remarks>
    /// Servers are instantiated using the <see cref="ServerManager"/>, which will serializes servers to the Library folder.
    /// </remarks>
    public abstract class Server : ScriptableObject
    {
#if UNITY_EDITOR
#pragma warning disable 414
        [SerializeField, HideInInspector]
        bool m_Expanded = true;
#pragma warning restore 414
#endif

        /// <summary>
        /// Called when the server is created.
        /// </summary>
        protected virtual void OnEnable() {}

        /// <summary>
        /// Called when the server is being destroyed.
        /// </summary>
        protected virtual void OnDisable() {}

        /// <summary>
        /// Called when the server's serialized fields have been changed from the inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            OnServerChanged(true);
        }

        /// <summary>
        /// Gets the display name for this server.
        /// </summary>
        /// <returns>
        /// A string containing the name of the server.
        /// </returns>
        public abstract string GetName();

        /// <summary>
        /// The method which is called regularly to update the server.
        /// </summary>
        public virtual void OnUpdate() {}

        /// <summary>
        /// Call this when the server's state or serialized data has been modified.
        /// </summary>
        /// <remarks>
        /// This triggers a refresh of the server GUI.
        /// </remarks>
        /// <param name="save">Should the server's serialized data be re-written to disk.</param>
        protected void OnServerChanged(bool save)
        {
            ServerManager.Instance.OnServerChanged();

            if (save)
            {
                ServerManager.Instance.Save();
            }
        }
    }
}
