namespace Unity.LiveCapture
{
    static class SynchronizerExtensions
    {
        /// <summary>
        /// Add a given number of frames to a timecode <see cref="Timecode"/>.
        /// </summary>
        /// <param name="timecode">The timecode we want to offset from.</param>
        /// <param name="frameRate">The number of frames per second.</param>
        /// <param name="frames">The number of frames.</param>
        /// <returns>The timecode with frames added.</returns>
        /// <remarks><paramref name="frames"/> can be negative.</remarks>
        public static Timecode AddFrames(this Timecode timecode, FrameRate frameRate, int frames)
        {
            return timecode.AddFrames(frameRate, new FrameTime(frames));
        }

        /// <summary>
        /// Add a given frame time to a timecode <see cref="Timecode"/>.
        /// </summary>
        /// <param name="timecode">The timecode we want to offset from.</param>
        /// <param name="frameRate">The number of frames per second.</param>
        /// <param name="frames">The frame time.</param>
        /// <returns>The timecode with frames added.</returns>
        /// <remarks><paramref name="frames"/> can be negative.</remarks>
        public static Timecode AddFrames(this Timecode timecode, FrameRate frameRate, FrameTime frames)
        {
            var thisFrameTime = timecode.ToFrameTime(frameRate);
            return Timecode.FromFrameTime(frameRate, thisFrameTime + frames);
        }
    }
}
