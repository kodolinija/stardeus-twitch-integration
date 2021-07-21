namespace KL.Integrations.Twitch {
    public struct TwitchConnection {
        public bool IsActive;
        // The Twitch Chat OAuth key (see https://twitchapps.com/tmi/)
        public string OAuth;
        // The username of above's account
        public string BotName;
        // The channel to listen from
        public string ChannelName;
    }
}