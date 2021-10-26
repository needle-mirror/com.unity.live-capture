namespace Unity.LiveCapture
{
    /// <summary>
    /// The interface for sources that generate timecodes used for synchronization.
    /// </summary>
    public interface ITimecodeSource : IRegistrable
    {
        /// <summary>
        /// The latest timecode available at the source.
        /// </summary>
        Timecode Now { get; }

        /// <summary>
        /// The rate at which timecodes are generated.
        /// </summary>
        FrameRate FrameRate { get; }
    }
}
