namespace KL.Integrations.Twitch {
    public class TwitchMessage {
        private const string PRIVMSG = "PRIVMSG";
        private const string WHISPER = "WHISPER";
        private const string displayNameStr = "display-name=";
        private const string badgestStr = "badges=";
        /*
        // broadcaster example:
        @badge-info=subscriber/9;badges=broadcaster/1,subscriber/3009;client-nonce=HEX_NUMBER;color=;display-name=NICKNAME;emotes=;flags=;id=GUID;mod=0;room-id=NUMBER;subscriber=1;tmi-sent-ts=1599632863146;turbo=0;user-id=NUMBER;user-type= :NICKNAME!NICKNAME@NICKNAME.tmi.twitch.tv PRIVMSG #CHANNEL :!rep nickname
        // vip example:
        @badge-info=founder/1;badges=vip/1,founder/0;client-nonce=HEX_NUMBER;color=#DC52FF;display-name=NICKNAME;emotes=;flags=;id=GUID;mod=0;room-id=NUMBER;subscriber=0;tmi-sent-ts=1599660315036;turbo=0;user-id=NUMBER;user-type= :NICKNAME!NICKNAME@NICKNAME.tmi.twitch.tv PRIVMSG #CHANNEL :!join
        // mod example:
        @badge-info=;badges=moderator/1;client-nonce=HEX_NUMBER;color=;display-name=bot_spajus;emotes=;flags=;id=GUID;mod=1;room-id=NUMBER;subscriber=0;tmi-sent-ts=1599660237377;turbo=0;user-id=NUMBER;user-type=mod :NICKNAME!NICKNAME@NICKNAME.tmi.twitch.tv PRIVMSG #CHANNEL :test
        // regular user example:
        @badge-info=;badges=;client-nonce=HEX_NUMBER;color=;display-name=NICKNAME;emotes=;flags=;id=GUID;mod=0;room-id=NUMBER;subscriber=0;tmi-sent-ts=1599633882371;turbo=0;user-id=NUMBER;user-type= :NICKNAME!NICKNAME@NICKNAME.tmi.twitch.tv PRIVMSG #CHANNEL :How are things going?
        // whisper example:
        @badges=;color=;display-name=NICKNAME;emotes=;message-id=2;thread-id=NUMBER_NUMBER;turbo=0;user-id=NUMBER;user-type= :NICKNAME!NICKNAME@NICKNAME.tmi.twitch.tv WHISPER BOTNAME :test
        */
        private string messageRaw;
        public TwitchMessage(string raw) {
            messageRaw = raw;
            message = ParseMessage();
        }

        private string username;
        public string Username {
            get {
                if (username == null) {
                    username = GetUserName();
                }
                return username;
            }
        }

        private string command;
        public string Command {
            get {
                if (command == null) {
                    command = Message.Split(' ')[0];
                    args = command == message ? null : 
                        message.Replace($"{command} ", "").Trim();
                }
                return command;
            }
        }
        private string args;
        public string Args => args;

        private string message;
        public string Message => message;
        public bool IsCommand => (IsChannelMessage || IsWhisper) 
            && Message[0] == '!';

        public bool IsChannelMessage;
        public bool IsWhisper;

        public bool ParsePrivileges(out bool isBroadcaster, 
                out bool isVip, out bool isMod) {
            isBroadcaster = false;
            isVip = false;
            isMod = false;
            if (IsWhisper) { return false; }
            var badgesIdx = messageRaw.IndexOf(badgestStr);
            var badgesMsgPart = messageRaw.Substring(badgesIdx + badgestStr.Length);
            badgesMsgPart = badgesMsgPart.Substring(0, badgesMsgPart.IndexOf(';') + 1);
            var badges = badgesMsgPart.Split(',');
            foreach (var badge in badges) {
                var b = badge.Split('/')[0];
                switch (b) {
                    case "broadcaster": isBroadcaster = true; break;
                    case "vip":
                    case "subscriber":
                    case "founder": isVip = true; break;
                    case "moderator": isVip = isMod = true; break;
                }
            }
            return true;
        }

        private string ParseMessage() {
            var msgStartIdx = messageRaw.IndexOf(PRIVMSG);
            if (msgStartIdx == -1) {
                // It's not a privmsg!
                msgStartIdx = messageRaw.IndexOf(WHISPER);
                if (msgStartIdx == -1) {
                    // We don't care about system messages
                    return null;
                } else {
                    IsWhisper = true;
                }
            }  else {
                IsChannelMessage = true;
            }

            var indexOfMsg = msgStartIdx + 
                (IsChannelMessage ? PRIVMSG.Length : WHISPER.Length);

            var msg = messageRaw.Substring(indexOfMsg);
            
            int indexOfColon = msg.IndexOf(':') + 1;
            msg = msg.Substring(indexOfColon).Trim(); 
            return msg;
        }

        private string GetUserName() {
            int indexOfWhisper = messageRaw.IndexOf(displayNameStr) 
                + displayNameStr.Length;
            var usernamePart = messageRaw.Substring(indexOfWhisper);
            int indexOfDelimiter = usernamePart.IndexOf(';');
            //-1 to remove the semi colon.
            string userName = usernamePart.Substring(0, indexOfDelimiter); 
            return userName;
        }
    }
}