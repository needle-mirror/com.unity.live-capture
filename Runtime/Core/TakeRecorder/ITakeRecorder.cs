namespace Unity.LiveCapture
{
    /// <summary>
    /// Represents a take recorder.
    /// </summary>
    public interface ITakeRecorder
    {
        /// <summary>
        /// The frame rate to use for recording.
        /// </summary>
        FrameRate FrameRate { get; set; }

        /// <summary>
        /// The selected slate to use for recording.
        /// </summary>
        /// <returns>
        /// The selected slate.
        /// </returns>
        ISlate GetActiveSlate();

        /// <summary>
        /// Indicates whether the take recorder is ready for recording.
        /// </summary>
        /// <returns>
        /// true if ready for recording; otherwise, false.
        /// </returns>
        bool IsLive();

        /// <summary>
        /// Use this method to set the take recorder ready for recording.
        /// </summary>
        /// <param name="value">true to set ready; otherwise, false.</param>
        void SetLive(bool value);

        /// <summary>
        /// Checks whether the take recorder is recording or not.
        /// </summary>
        /// <returns>
        /// true if playing; otherwise, false.
        /// </returns>
        bool IsRecording();

        /// <summary>
        /// Starts the recording of a new take for the selected slate.
        /// </summary>
        void StartRecording();

        /// <summary>
        /// Stops the recording.
        /// </summary>
        void StopRecording();

        /// <summary>
        /// Checks whether the take recorder is playing the selected take or not.
        /// </summary>
        /// <returns>
        /// true if playing; otherwise, false.
        /// </returns>
        bool IsPreviewPlaying();

        /// <summary>
        /// Starts playing the selected take.
        /// </summary>
        void PlayPreview();

        /// <summary>
        /// Pauses the playback of the selected take.
        /// </summary>
        void PausePreview();

        /// <summary>
        /// Returns the current playback time of the selected take.
        /// </summary>
        /// <returns>The current time in seconds.</returns>
        double GetPreviewTime();

        /// <summary>
        /// Changes the current playback time of the selected take.
        /// </summary>
        /// <param name="time">The current time in seconds.</param>
        void SetPreviewTime(double time);

        /// <summary>
        /// Returns the current playback duration of the selected take.
        /// </summary>
        /// <returns>The current duration in seconds.</returns>
        double GetPreviewDuration();
    }

    interface ITakeRecorderInternal : ITakeRecorder
    {
        /// <summary>
        /// The enabled state of this take recorder.
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Changes the current playback time of the slate.
        /// </summary>
        /// <param name="slate">The slate.</param>
        /// <param name="time">The current time in seconds.</param>
        void SetPreviewTime(ISlate slate, double time);

        /// <summary>
        /// Prepares the current context for playback.
        /// </summary>
        void Prepare();
    }
}
