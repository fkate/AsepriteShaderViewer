// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.Input;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WatsonWebsocket;

namespace AsepriteShaderViewer {
    public class WebSocketServer {

        // Recieved data
        public Action<AppEvent, object> RecievedEvent;

        public bool Running { get; private set; }

        private WatsonWsServer _server;
        private Guid _clientID;

        public WebSocketServer(string ip, int port) {
            _server = new WatsonWsServer(ip, port, false);
            _server.ClientConnected += OnClientConnect;
            _server.ClientDisconnected += OnClientDisconnect;
            _server.MessageReceived += OnMessageRecieved;

            Log.Print(string.Format("Server created at: {0}:{1}", ip, port));
        }

        /// <summary> Start the websocket server </summary>
        public void Start() {
            _server.Start();
            Running = true;

            Log.Print("Server started");
        }

        /// <summary> Stop the websocket server and notify the client </summary>
        public async void Stop() {
            SendMessage("EXIT");
            _server.Stop();
            Running = false;

            Log.Print("Server stopped");
        }

        /// <summary> Send a text message to the connected client </summary>
        public async void SendMessage(string message) {
            await _server.SendAsync(_clientID, message);
        }

        /// <summary> Send an image to the connected client </summary>
        public async void SendImage(RawImage image) {
            byte[] buffer = new byte[image.Data.Length + 4];
            byte[] width = BitConverter.GetBytes((ushort) image.Width);
            byte[] height = BitConverter.GetBytes((ushort) image.Height);

            Array.Copy(width, 0, buffer, 0, 2);
            Array.Copy(height, 0, buffer, 2, 2);
            Array.Copy(image.Data, 0, buffer, 4, image.Data.Length);

            await _server.SendAsync(_clientID, buffer);
        }

        /// <summary> A client has connected to the server </summary>
        private void OnClientConnect(object sender, ConnectionEventArgs e) {
            Log.Print("Client connected");
            _clientID = e.Client.Guid;
        }

        /// <summary> A client has disconnected from the server </summary>
        private void OnClientDisconnect(object sender, DisconnectionEventArgs e) {
            Log.Print("Client disconnected");
            RecievedEvent.Invoke(AppEvent.Exit, null);
        }

        /// <summary> Evaluate incoming messages from the connected client </summary>
        private void OnMessageRecieved(object sender, MessageReceivedEventArgs e) {
            switch(e.MessageType) {
                case System.Net.WebSockets.WebSocketMessageType.Text:
                    EvaluateTextMessage(Encoding.UTF8.GetString(e.Data.ToArray()));                    
                    return;

                case System.Net.WebSockets.WebSocketMessageType.Binary:
                    EvaluateBinaryMessage(e.Data.ToArray());
                    return;
            }            
        }

        /// <summary> Evaluate text based messages </summary>
        private void EvaluateTextMessage(string message) {
            // We expect the first 4 indices to be the type
            if (WebMessage.ParseMessage(message, out string messageType, out string content)) {

                switch (messageType) {
                    case "FRAG":
                        RecievedEvent.Invoke(AppEvent.LoadShader, content);
                        Log.Print("Upload fragment shader", Log.MessageType.Info);
                        break;

                    case "MESH":
                        RecievedEvent.Invoke(AppEvent.LoadMesh, content);
                        Log.Print("Upload mesh", Log.MessageType.Info);
                        break;

                    case "REND":
                        vec2 rend = WebMessage.ParseVec2(content.Split(" "));

                        RecievedEvent.Invoke(AppEvent.ExportImage, rend);
                        Log.Print("Create snapshot", Log.MessageType.Info);
                        break;

                    case "TRSP":                        
                        vec3 trsp = WebMessage.ParseVec3(content.Split(" "));
                        RecievedEvent.Invoke(AppEvent.SetCamera, new Transform2DEvent { positionX = trsp.X, positionY = trsp.Y, size = trsp.Z });
                        break;

                    case "TRSR":
                        vec3 trsr = WebMessage.ParseVec3(content.Split(" "));
 
                        RecievedEvent.Invoke(AppEvent.SetCamera, new Transform2DEvent { rotationYaw = trsr.X * math.DegToRad, rotationPitch = trsr.Y * math.DegToRad, size = trsr.Z });
                        break;

                    case "TEXT":
                        Log.Print(content, Log.MessageType.Info);
                        break;

                    case "EXIT":
                        RecievedEvent.Invoke(AppEvent.Exit, null);
                        Log.Print("Recieve exit request", Log.MessageType.Info);
                        break;
                }
            }
        }

        /// <summary> Evaluate binary messages. Binary messages in this case are always images </summary>
        private void EvaluateBinaryMessage(byte[] stream) {
            ushort channel = BitConverter.ToUInt16(stream, 0);
            ushort frame = BitConverter.ToUInt16(stream, 2);
            ushort width = BitConverter.ToUInt16(stream, 4);
            ushort height = BitConverter.ToUInt16(stream, 6);
            byte[] data = new byte[stream.Length - 8];
            Array.Copy(stream, 8, data, 0, data.Length);

            BinaryReader reader = new BinaryReader(new MemoryStream(stream));

            RawImage img = new RawImage
            {
                Width = width,
                Height = height,
                Data = data,
                Channel = channel,
                Frame = frame
            };

            RecievedEvent.Invoke(AppEvent.LoadImage, img);
            if (img.Channel != 0) Log.Print(string.Format("Upload image to channel {0}", img.Channel), Log.MessageType.Info);
        }

    }
}
