namespace Unity.LiveCapture.CompanionApp
{
    /// <summary>
    /// A class that contains the message IDs which define the companion app protocol.
    /// </summary>
    static class CompanionAppMessages
    {
        public static class ToClient
        {
            const string k_Base = "CompanionApp_ToClient_";

            public const string Initialize = k_Base + "Initialize";
            public const string ServerState = k_Base + "ServerState";
            public const string PlayerState = k_Base + "PlayerState";
            public const string SlateDescriptor = k_Base + "SlateDescriptor";
        }

        public static class ToServer
        {
            const string k_Base = "CompanionApp_ToServer_";

            public const string SetMode = k_Base + "SetMode";
            public const string StartRecording = k_Base + "StartRecording";
            public const string StopRecording = k_Base + "StopRecording";
            public const string PlayerStart = k_Base + "PlayerStart";
            public const string PlayerStop = k_Base + "PlayerStop";
            public const string PlayerPause = k_Base + "PlayerPause";
            public const string PlayerSetTime = k_Base + "PlayerSetTime";
            public const string SetSelectedTake = k_Base + "SetSelectedTake";
            public const string SetTakeData = k_Base + "SetTakeData";
            public const string DeleteTake = k_Base + "DeleteTake";
            public const string SetIterationBase = k_Base + "SetIterationBase";
            public const string ClearIterationBase = k_Base + "ClearIterationBase";
        }
    }
}
