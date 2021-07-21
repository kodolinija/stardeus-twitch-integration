using System;

namespace Game.Data.Twitch {
    [Serializable]
    public class TwitchRole {
        public string Role;
        public int AddAtReputation;
        public int RemoveAtReputation;
        public int VIPAddAtReputation;
        public int VIPRemoveAtReputation;
        public bool AddToBroadcaster;
        public bool AddToMods;
        public bool AddToVIP;
    }
}