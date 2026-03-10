# AsepriteShaderViewer <img width="64" height="64" alt="Icon_x64" src="https://github.com/user-attachments/assets/f61b57f3-b3da-45f5-8c24-d3e7518f471b" />

> :warning: This extension requires a 1.3+ version of Aseprite<br>
> Note: There is currently a bug with the newer Aseprite versions that can cause the active cel to jump to (0, 0) when drawing while the extension active.
> A new version of this extension will be released later this week with a workaround and new features.

## About
Extension for Aseprite to preview sprites with custom GLSL shaders in realtime. Uses a Silk.Net / OpenGL view and local websockets to allow for cross platform compability.

![ASV_01](https://github.com/user-attachments/assets/937f93b4-b8d9-48df-9e0a-167d3095428a)

## Features

- Write GLSL shaders in a ShaderToy similar fashion to preview your workspace in realtime
- Export shaded results back to Aseprite for further processing or saving
- Add lua companion scripts with your glsl shaders to send additional data to the view or send raycasted input from it back to Aseprite
- Limited 3D features to load OBJ files and map sprites onto their uv layout
- A variety of examples to show the views capabilities and explain how to implement your own

![ASV_02](https://github.com/user-attachments/assets/3759a37a-f42c-44f5-bba2-1da5c06867a1)

## Installation
Download the shaderView zip archive from releases and add it to Aseprite via **Edit>Preferences>Extensions**.
The installed extension can be found within the **File menu**. Starting the extension will cause lua to execute the View process depending on the OS and may take a few seconds depending on the system (this is an issue on luas side).
Navigating through the extension may cause Aseprite to ask for three permissions: external process execution (the View) / websocket activity / file loading.

![ASV_03](https://github.com/user-attachments/assets/7ec7b3ea-1540-4ebb-bb2c-ad00435f186c)

## This page is work in progress :construction:
An extensive documentation within the Github Wiki is in progress and an Itch.io page showing a more visual preview is planned within this week.
I also still need to figure out external contribution and a potential place where users can share their custom GLSL shaders.
