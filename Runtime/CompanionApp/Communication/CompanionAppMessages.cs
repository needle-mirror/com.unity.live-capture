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

            public const string k_Initialize = k_Base + "Initialize";
            public const string k_ServerState = k_Base + "ServerState";
            public const string k_PlayerState = k_Base + "PlayerState";
            public const string k_SlateDescriptor = k_Base + "SlateDescriptor";
        }

        public static class ToServer
        {
            const string k_Base = "CompanionApp_ToServer_";

            public const string k_SetMode = k_Base + "SetMode";
            public const string k_StartRecording = k_Base + "StartRecording";
            public const string k_StopRecording = k_Base + "StopRecording";
            public const string k_PlayerStart = k_Base + "PlayerStart";
            public const string k_PlayerStop = k_Base + "PlayerStop";
            public const string k_PlayerPause = k_Base + "PlayerPause";
            public const string k_PlayerSetTime = k_Base + "PlayerSetTime";
            public const string k_SetSelectedTake = k_Base + "SetSelectedTake";
        }
    }
}
