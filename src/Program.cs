// Aseprite Shader Viewer source
// Copyright (c) 2026 Felix Kate
// Licensed under the MIT license. Check LICENSE.txt for defails

using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;

namespace AsepriteShaderViewer {
    public class Program {

        public static WebSocketServer Server;
        public static IWindow AppWindow;

        public static void Main(string[] args) {
            // Ensure a messaging stop when termination is not done via the window lifetime
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);

            // Read optional arguments. Argument 1 will set the port while argument 2 is a path to an external log file
            int port = args.Length > 0 ? int.Parse(args[0]) : 9000;
            Log.Start(args.Length > 1 ? args[1] : null);

            // Register modules
            Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform();
            Silk.NET.Input.Glfw.GlfwInput.RegisterPlatform();

            // Ensure depth buffer and set window to topmost to stay in front when Aseprite is focused
            WindowOptions options = WindowOptions.Default;
            options.Size = new Vector2D<int>(800, 600);
            options.Title = "Asperite Shader Viewer";
            options.TopMost = true;
            options.PreferredDepthBufferBits = 32;

            AppWindow = Window.Create(options);

            Server = new WebSocketServer("127.0.0.1", port);
            Server.Start();

            // Send error messages back to aseprite (mainly to better debug shader errors)
            Log.ExternalOutput += (msg, t) => { if(t == Log.MessageType.Error || t == Log.MessageType.Warning) Server.SendMessage(msg); };

            App app = new App(AppWindow, Server);

            AppWindow.Load += app.Load;
            AppWindow.FramebufferResize += app.OnResize;
            AppWindow.Update += app.Update;
            AppWindow.Render += app.Render;
            AppWindow.Closing += app.Close;

            AppWindow.Run();

            // Stop all services when the window closes
            Server.Stop();
            
            AppWindow.Dispose();

            Log.Stop();
        }
        public static void ProcessExit(object sender, EventArgs e) {
            if(Server != null && Server.Running) Server.Stop();
            Log.Stop();
        }

    }

}