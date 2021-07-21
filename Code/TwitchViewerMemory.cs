using System;
using System.Collections.Generic;
using System.IO;
using KL.Utils;
using UnityEngine;

namespace Game.Data.Twitch {
    [Serializable]
    public class TwitchViewerMemory {
        private const string TwitchViewerFile = "TwitchViewers.json";
        [SerializeField] private List<TwitchViewer> viewers;
        public List<TwitchViewer> Viewers => viewers;

        private TwitchViewerMemory() { }

        public static void Save(List<TwitchViewer> viewers) {
            var memory = new TwitchViewerMemory {
                viewers = viewers,
            };
            Persist.Write(Persist.Root, TwitchViewerFile, 
                Json.Dump(memory, true), false);
        }

        public static List<TwitchViewer> Load() {
            if (File.Exists(Persist.PersistentPath(TwitchViewerFile))) {
                var res = Persist.Read(Persist.Root, TwitchViewerFile, 
                    out string json, false);
                if (res.IsSuccess) {
                    var mem = Json.Deserialize<TwitchViewerMemory>(json, 
                        res.FilePath);
                    if (mem != null && mem.Viewers != null) {
                        return mem.Viewers;
                    } else {
                        res.Exception = "Nothing was inside the json file";
                    }
                }
                D.Err("Failed loading {0}. {1}", res.FilePath, res.Exception);
            }
            return new List<TwitchViewer>();
        }
    }
}