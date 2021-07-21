#define DEBUG_TWITCH
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using KL.Signals;
using KL.Utils;
using UnityEngine;
using WebSocketSharp;
using WebSocket = WebSocketSharp.WebSocket;
using WebSocketState = WebSocketSharp.WebSocketState;

// heavily modified version of
// https://github.com/ppartida/unitwitch
namespace KL.Integrations.Twitch {
    public class Twitch {
        private const string PING = "PING";
        private const string PONG = "PONG";
        private const string JOIN = "JOIN";
        private const string PART = "PART";
        private const float MessageTimeout = 1;
        private const string URL = "wss://irc-ws.chat.twitch.tv:443/";
        private float lastMsgTime;
        public readonly Signal1<TwitchMessage> OnMessage 
            = new Signal1<TwitchMessage>("OnMessage");
        public readonly Signal1<TwitchMessage> OnCommand 
            = new Signal1<TwitchMessage>("OnCommand");
        public readonly Signal1<string> OnError = new Signal1<string>("OnError");
        public readonly Signal1<string> OnOpen = new Signal1<string>("OnOpen");
        public readonly Signal1<string> OnClose = new Signal1<string>("OnClose");
        public readonly Signal1<string> OnJoin = new Signal1<string>("OnJoin");
        public readonly Signal1<string> OnPart = new Signal1<string>("OnPart");
        public Signal0 OnReconnect = new Signal0("OnReconnect");
        private TwitchConnection config;
        private WebSocket ws;
        private bool hasErrors = false;
        public bool IsConfigured => !hasErrors;
        public WebSocketState State => ws == null ? WebSocketState.Open : ws.ReadyState;
        public bool IsConnected => ws != null && ws.ReadyState == WebSocketState.Open;

        /*
        Example join message:
        :nickname!nickname@nickname.tmi.twitch.tv JOIN #your_channel
         */

        public Twitch(TwitchConnection config) {
            this.config = config;
            if (String.IsNullOrWhiteSpace(config.OAuth)) {
                D.Err("Twitch OAuth is blank");
                hasErrors = true;
            }
            if (String.IsNullOrWhiteSpace(config.BotName)) {
                D.Err("Twitch BotName is blank");
                hasErrors = true;
            }
            if (String.IsNullOrWhiteSpace(config.ChannelName)) {
                D.Err("Twitch ChannelName is blank");
                hasErrors = true;
            }
            if (hasErrors) { return; }

            #if DEBUG_TWITCH
            D.Log("Starting Twitch integration");
            #endif

            using (ws = new WebSocket(URL, "irc")) {
                ws.EmitOnPing = true;
                ws.OnMessage += (sender, e) => ReceiveMessage(e.Data);
                ws.OnClose += (sender, e) => { 
                    D.Err("Connection closed");
                    OnClose.Send(e.Reason); 
                };
                ws.OnError += (sender, e) => {
                    D.Err("Connection error: {0}", e.Message);
                    OnError.Send(e.Message);
                };
                ws.OnOpen += (sender, e) => {
                    D.Err("Connection opened");
                    OnOpen.Send(e.ToString());
                };
            }
            D.Err("Created websocket: {0}", ws);
        }

        public void ExecuteCommand(string command) {
            ws.Send(command);
        }

        public void SendChannelMessage(string message) {
            if (hasErrors) { return; }
            var time = Time.realtimeSinceStartup;
            if (time - lastMsgTime < MessageTimeout) {
                D.Warn("Skipping message, too frequent! {0}", message);
                return;
            }
            lastMsgTime = time;
            var msg = $"PRIVMSG #{config.ChannelName} :{message}";
            D.Log("Sending to chat: {0}", msg);
            ws.Send(msg);
        }

        public void SendWhisper(string username, string message) {
            if (hasErrors) { return; }
            var time = Time.realtimeSinceStartup;
            if (time - lastMsgTime < MessageTimeout) {
                D.Warn("Skipping whisper to {1}, too frequent! {0}", 
                    message, username);
                return;
            }
            lastMsgTime = time;
            var msg = $"PRIVMSG #{config.ChannelName} :/w {username} {message}";
            D.Log("Sending whisper to {0}: {1}", username, message);
            ws.Send(msg);
        }

        public void ReplyTo(TwitchMessage msg, string reply) {
            if (msg.IsChannelMessage) {
                SendChannelMessage(reply);
            } else {
                SendWhisper(msg.Username, reply);
            }
        }
         
        private void Reconnect() {
            if (hasErrors) { 
                D.Err("Trying to reconnect with errors");
                return; 
            }

            #if DEBUG_TWITCH
            D.Log("Reconnecting to Twitch! Socket state: {0}", ws.ReadyState);
            #endif

            ws.WaitTime = new TimeSpan(0, 0, 1);
            try {
                ws.Connect();
            } catch (System.Exception ex) {
                D.LogEx(ex, "Failed connecting to Twitch");
                hasErrors = true;
                return;
            }
            OnReconnect.Send();

            if (ws.ReadyState == WebSocketState.Open) {
                ws.Send("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
                ws.Send("PASS " + config.OAuth);
                ws.Send("NICK " + config.BotName);
                ws.Send("JOIN #" + config.ChannelName);
            } else {
                D.Err("Twitch connection not ready: {0}", ws.ReadyState);
                D.Log("Twitch Connection NOT Ready.");
            }
        }
        private void ReceiveMessage(string msg) {
            #if DEBUG_TWITCH
            D.Log(msg);
            #endif
            if (hasErrors) { return; }
            
            if (msg.StartsWith(PING)) {
                ws.Send(PONG);
                return;
            } 
            if (msg[0] == ':' && (msg.Contains(JOIN) || msg.Contains(PART))) {
                ParseJoinsParts(msg);
                return;
            }
            var twMsg = new TwitchMessage(msg);
            if (twMsg.IsCommand) {
                OnCommand.Send(twMsg);
            } else {
                OnMessage.Send(twMsg);
            }
        }

        private void ParseJoinsParts(string msg) {
            foreach (var joinPart in msg.Split('\n')) {
                var nick = joinPart.Substring(1, joinPart.IndexOf('!') - 1);
                if (msg.Contains(PART)) {
                    OnPart.Send(nick);
                } else if (msg.Contains(JOIN)) {
                    OnJoin.Send(nick);
                }
            }
        }

        public void ReconnectIfNeeded() {
            if (hasErrors) { 
                D.Err("Trying to reconnect twitch while having errors");
                return; 
            }
            if (!IsConnected) {
                Reconnect();
            }
        }

        public void Destroy() {
            D.Err("Destroying twitch connection");
            if (hasErrors) { return; }
            ws.Close();
        }
    }
}