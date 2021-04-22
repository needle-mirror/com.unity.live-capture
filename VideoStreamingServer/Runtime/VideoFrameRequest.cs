using System;
using Unity.Collections;
using UnityEngine.Rendering;

namespace Unity.LiveCapture.VideoStreaming.Server
{
    /// <summary>
    /// A request for texture data from the GPU that can be completed either synchronously or
    /// asynchronously. Only valid for the frame on which the request is completed, so be sure
    /// to poll <see cref="isDone"/> appropriately and copy the results as needed.
    /// </summary>
    struct VideoFrameRequest
    {
        AsyncGPUReadbackRequest m_Request;

        /// <summary>
        /// The time in seconds at which this image was requested.
        /// </summary>
        public readonly float elapsedTime;

        /// <summary>
        /// The width of the video frame.
        /// </summary>
        public readonly int width;

        /// <summary>
        /// The height of the video frame.
        /// </summary>
        public readonly int height;

        /// <summary>
        /// Is the frame data ready to access.
        /// </summary>
        public bool isDone => m_Request.done;

        /// <summary>
        /// Did an error occur while retrieving the texture data.
        /// </summary>
        public bool hasError => m_Request.hasError;

        /// <summary>
        /// Creates a new <see cref="VideoFrameRequest"/> instance.
        /// </summary>
        /// <param name="request">A read back request.</param>
        /// <param name="width">The width of the video frame.</param>
        /// <param name="height">The height of the video frame.</param>
        /// <param name="elapsedTime">The time in seconds at which this image was requested.</param>
        public VideoFrameRequest(AsyncGPUReadbackRequest request, int width, int height, float elapsedTime)
        {
            m_Request = request;
            this.width = width;
            this.height = height;
            this.elapsedTime = elapsedTime;
        }

        /// <summary>
        /// Returns the requested texture data if the request was successfully completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">The async operation was not completed or encountered an error.</exception>
        /// <returns>A native array with the texture data. Only valid for the remainder of the current frame. The collection
        /// does not need to be disposed by the caller.</returns>
        public NativeArray<byte> GetData()
        {
            if (!isDone)
                throw new InvalidOperationException("Texture data may not be accessed if the request is not done.");
            if (hasError)
                throw new InvalidOperationException("Texture data may not be accessed if the request completed with an error.");

            return m_Request.GetData<byte>();
        }
    }
}
