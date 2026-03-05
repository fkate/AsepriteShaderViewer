// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using System;
using System.IO;

namespace AsepriteShaderViewer {
    public static class Log {

        public enum MessageType {
            None,
            Info,
            Warning,
            Error
        }

        public static Action<string, MessageType> ExternalOutput;

        private static StreamWriter _writer;

        /// <summary> Start writing to an external log </summary>
        public static void Start(string path) {
            if(!string.IsNullOrEmpty(path)) {
                FileStream fs = new FileStream(path, FileMode.Create);
                _writer = new StreamWriter(fs);
                _writer.AutoFlush = true;
                Console.SetOut(_writer);
            }
        }

        /// <summary> Stop writing to an external log </summary>
        public static void Stop() {
            if(_writer != null) {
                _writer.Close();
                Console.SetOut(null);
                _writer = null;
            }
        }

        /// <summary> Print message to whatever output is set </summary>
        public static void Print(string message, MessageType type = MessageType.Info) {
            if(type == MessageType.None) {
                Console.WriteLine(message);
            } else {
                Console.WriteLine(string.Format("{0}: {1}", type.ToString(), message));
            }

            ExternalOutput?.Invoke(message, type);
        }

        /// <summary> Print message to whatever output is set </summary>
        public static void Print(object message, MessageType type = MessageType.Info) {
            Print(message.ToString(), type);
        }

    }
}
