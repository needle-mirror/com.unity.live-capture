namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// The interface for captured data that has a sampling time.
    /// </summary>
    interface ISample
    {
        /// <summary>
        /// The time in seconds since the client connected.
        /// </summary>
        /// <remarks>
        /// This can be used to determine the order and timing of this sample relative to other samples received.
        /// </remarks>
        float Timestamp { get; }
    }
}
