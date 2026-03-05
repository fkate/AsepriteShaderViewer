---- [Config] ----------------------------------------------------------------
local portConfig = "9000";
local fileDirectoryConfig = "shaders"
local logFileConfig = "Log.txt"
local debugConfig = false
local exportLimitConfig = 128


---- [References] ----------------------------------------------------------------
local dialog = nil
local activeSprite = nil
local shaderScript = nil
local noRefresh = false
local exportBuffer = {}


---- [File Manager logic] ----------------------------------------------------------------
local FileManager = { pluginPath = "", filePath = "" }

-- Set the file manager paths (Internal use)
function FileManager:setPluginPath(plugin, fileDirectory)
	self.pluginPath = app.fs.normalizePath(plugin.path .. "/")
	self.filePath = app.fs.normalizePath(self.pluginPath .. fileDirectory .. "/")	
end

-- Read text data from a file located inside the file folder
function FileManager:readTextfile(path)
	path = self.filePath .. path

	if app.fs.isFile(path) then
		local f = io.open(path, "rb")
		local c = f:read("*all")
		f:close()
		return c;
	else
		app.alert(path .. "not found")		
		return nil
	end	
end

-- Read Image data from a file located inside the file folder
function FileManager:readImagefile(path)
	path = self.filePath .. path

	if app.fs.isFile(path) then
		return Image{fromFile =  path}
	else
		app.alert(path .. "not found")		
		return nil
	end	
end

-- List all files of an extension type inside the file folder
function FileManager:listFiles(ext)
	local files = app.fs.listFiles(self.filePath)
	
	for i = #files, 1, -1 do
		if app.fs.fileExtension(files[i]) ~= ext then
			table.remove(files, i)
		end
	end
	
	return files
end

-- Find the shaderScript script to a shader (Internal use)
function FileManager:findShaderScript(path)
	path = string.gsub(path, ".glsl", ".lua")
	path = self.filePath .. path
	
	if app.fs.isFile(path) then
		return dofile(path)
	else
		return nil
	end
end

-- Open external editor
function FileManager:openFileFolder()
	if app.os.windows then	
		io.popen("start " .. self.filePath)
	elseif app.os.linux then
		io.popen("xdg-open " .. self.filePath)
	elseif app.os.macos then
		io.popen("open " .. self.filePath)
	end
end

-- Convert a path into a path relative to the plugin
function FileManager:getLocalPath(path)
	return self.pluginPath .. app.fs.normalizePath(path)
end

-- Start an external process (Internal use)
function FileManager:startProcess(command, path, arguments, wait)
	if app.fs.isFile(path) then
		os.execute(command .. " " .. path .. " " .. arguments)
	
		if wait > 0 then
			-- enforce wait before we start the socket to give the server enough time to start
			local clock = os.clock
			local t = clock()
			while clock() - t <= wait do end
		end
		
		return true
	else
		app.alert("Process at" .. path .. "could not be not found")
		return false
	end
end


---- [Websocket client logic] ----------------------------------------------------------------
local Client = { websocket = nil, isConnected = false, imageData = Image(1, 1), onPrepareRender = nil }

-- Connect the client to the local port. The dataHandler will listen to the socket (Internal use)
function Client:connect(dataHandler, port)
	self.websocket = WebSocket {
		url = "http://127.0.0.1:" .. port,
		onreceive=dataHandler,
		deflate=false,
		100
	}
	
	self.websocket:connect()	
	self.isConnected = true
end

-- End the clients connection (Internal use)
function Client:close()
	if self.isConnected then
		self.websocket:close()
		self.websocket = nil
		self.isConnected = false
	end
end

-- Force the server to shut down (Internal use)
function Client:shutdown()
	if self.isConnected then
		-- Make sure that websocket errors don't crash the script
		pcall(function ()
			self.websocket:sendText("EXIT")		
		end)

	end
end

-- Send raw text. Will be included in the log but will not do anything else
function Client:sendText(message)
	if self.isConnected then
		self.websocket:sendText(message)
	end
end

-- Send string data as shader information
function Client:sendShader(raw)
	if self.isConnected then
		self.websocket:sendText("FRAG" .. raw)
	end
end

-- Send string data as mesh information
function Client:sendMesh(raw)
	if self.isConnected then
		self.websocket:sendText("MESH" .. raw)
	end
end

-- Send a specific frame as the main image (channel0)
function Client:sendFrame(frame)
	if self.isConnected and frame ~= nil then
		local sprite = frame.sprite
	
		if self.imageData.width ~= sprite.width or self.imageData.height ~= sprite.height then		
			self.imageData = Image(sprite.width, sprite.height)
		end
	
		self.imageData:drawSprite(sprite, frame)
		self.websocket:sendBinary(string.pack("I2I2I2I2", 0, frame.frameNumber, self.imageData.width, self.imageData.height), self.imageData.bytes)
	end
end

-- Send a channel Image (channel 1-3); Set filterMode to "linear" or wrapMode to "repeat" to set the texture channel flags 
function Client:sendImage(data, channel, filterMode, wrapMode)
	if self.isConnected then
		local filter = 0
		local wrap = 0
		
		if filterMode == "linear" then filter = 1 end
		if wrapMode == "repeat" then wrap = 1 end
	
		self.websocket:sendBinary(string.pack("I2BBI2I2", channel, filter, wrap, data.width, data.height), data.bytes)
	end
end

-- Send position transform to the camera
function Client:sendPosition(posX, posX, size)
	if self.isConnected then
		self.websocket:sendText("TRSP" .. posX .. " " .. posX .. " " .. size)
	end
end

-- Send rotation transform to the camera
function Client:sendRotation(rotX, rotY, size)
	if self.isConnected then
		self.websocket:sendText("TRSR" .. rotX .. " " .. rotY .. " " .. size)
	end
end

-- Translate incoming data to image (Internal use)
function Client:readImage(data)
	local width, height = string.unpack("I2I2", data, 0)
	local image = Image(width, height)
	image.bytes = string.sub(data, 5)
	
	return image
end

-- Wait for x seconds
function Client:waitForSeconds(seconds)	
	if seconds > 0 then
		local clock = os.clock
		local t = clock()
		while clock() - t <= seconds do end
	end
end

-- Request to render the current frame at the given size
function Client:renderImage(width, height)
	if self.isConnected then
		self.websocket:sendText("REND" .. width .. " " .. height)
	end
end

-- Request to render multiple images with a function for preparing the next frame
function Client:renderImages(width, height, onPrepare)
	if self.isConnected then
		self.onPrepareRender = onPrepare
		
		if self.onPrepareRender(0) then
			self.websocket:sendText("REND" .. width .. " " .. height)
		end
	end
end


---- [Setup logic] ----------------------------------------------------------------
-- Initialize plugin from file menu
function initialize()
	local processLaunched = false
	local processPath = nil
	local arguments = portConfig .. " " .. FileManager.pluginPath .. logFileConfig

	-- start the view depending on the os (todo: linux and mac implementation)
	if debugConfig then
		-- Special config for running process as a linked executable on windows for debugging
		processPath = FileManager:getLocalPath("debug/AsepriteShaderViewer.lnk")
		processLaunched = FileManager:startProcess("start", processPath,  arguments, 1)
		
	elseif app.os.windows and app.os.x64 then
		processPath = FileManager:getLocalPath("win-x64/AsepriteShaderViewer.exe")
		processLaunched = FileManager:startProcess("start", processPath,  arguments, 1)
		
	elseif app.os.windows and app.os.x86 then
		processPath = FileManager:getLocalPath("win-x86/AsepriteShaderViewer.exe")
		processLaunched = FileManager:startProcess("start", processPath,  arguments, 1)
		
	elseif app.os.macos then
		processPath = FileManager:getLocalPath("osx-64/AsepriteShaderViewer.exe")
		FileManager:startProcess("chmod +x", processPath, "", 0)	
		processLaunched = FileManager:startProcess("", processPath,  arguments .. " &", 1)
		
	elseif app.os.linux then
		processPath = FileManager:getLocalPath("linux-x64/AsepriteShaderViewer.exe")
		FileManager:startProcess("chmod +x", processPath, "", 0)		 
		processLaunched = FileManager:startProcess("", processPath,  arguments .. " &", 1)
				
	end
	
	if not processLaunched then
		return;
	end	

	Client:connect(onRecieveData, portConfig)
	
	-- Setup plugin dialog
	dialog = Dialog {
		title = "Shader View",
		onclose = function()
			dialog = nil
			cleanup()
		end
	}
	
	dialog:separator {
		text = "Options"
	}
	
	dialog:combobox {
		id = "shaderbox",
		label = "Shader",
		option = "Default.glsl",
		options = FileManager:listFiles("glsl"),
		onchange = setFile
	}
	
	dialog:newrow()
	
	dialog:button {
		text = "Open folder",
		onclick = function()
			FileManager:openFileFolder()
		end
	}	
	
	dialog:button {
		text = "Refresh",
		onclick = function()
			dialog:modify {
				id = "shaderbox",
				option = dialog.data.shaderbox,
				options = FileManager:listFiles("glsl")			
			}			
			setFile()
		end
	}	

	dialog:separator {
		text = "Export"
	}
	
	dialog:check {
		id = "exportall",
		label = "Range",
		text = "All Frames",
		selected = false
	}
	
	dialog:slider {
		id = "exportscale",
		label = "Scale",
		min = 1,
		max = 16,
		value = 4
	}
	
	dialog:button {
		text = "Export",
		onclick = function()	
			if Client.isConnected and activeSprite ~= nil then
				if dialog.data.exportall then
					-- Render frames starting from frame 1 until last frame is reached
					local frame = activeSprite.frames[1]
					
					Client:renderImages(activeSprite.width * dialog.data.exportscale, activeSprite.height * dialog.data.exportscale, function(i)
						if frame == nil then
							return false
						else
							Client:sendFrame(frame)
							frame = frame.next
							return true
						end
					end)
				else
					-- Render single frame
					Client:renderImage(activeSprite.width * dialog.data.exportscale, activeSprite.height * dialog.data.exportscale)
				end
			end		
		end
	}
			
	dialog:show { wait = false }
end

-- Clean up after plugin was closed or interrupted
function cleanup()
	Client:shutdown()
	Client:close()
	
	if dialog ~= nil then
		dialog:close()
		dialog = nil		
	end
	
	if activeSprite ~= nil then
		activeSprite.events:off(refreshView)
		activeSprite = nil
	end
	
	if shaderScript ~=nil then
		shaderScript:close()		
		shaderScript = nil
	end
	
	app.events:off(refreshView)
end


---- [Client/Server logic] ----------------------------------------------------------------
-- Evaluate incoming data
function onRecieveData(mt, data)
	if Client.isConnected then
		if mt == WebSocketMessageType.OPEN then
			app.events:on('sitechange', refreshView)		
			refreshView(nil)
			
		elseif mt == WebSocketMessageType.TEXT then		
			onMessage(data)
		
		elseif mt == WebSocketMessageType.BINARY then		
			onRecieveImage(Client:readImage(data))
			
		elseif mt == WebSocketMessageType.CLOSE then
			print("disconnected")
			cleanup();
			
		end
	end
end

-- Handle incoming text messages. Try using the first four letters as a hint
function onMessage(message)
	local messageType = string.sub(message, 1, 4)

	if messageType == "EXIT" then
		-- Close early to prevent second EXIT message		
		cleanup()
		
	elseif messageType == "TOOL" then
		if shaderScript ~= nil then
			message = string.gsub(message, "TOOL", "")
		
			local values = {}
			for item in string.gmatch(message, "[^,]+") do
				values[#values + 1] = tonumber(item)  
			end
			
			if shaderScript:processInput(values[1], values[2], values[3]) then
				app.refresh()
				refreshView(nil)
			end			
		end		
	else
		-- Unhandled standard message
		print(message)
		
	end
end

-- Handle incoming images. Start a render loop if necessary
function onRecieveImage(image)
	noRefresh = true
	exportBuffer[#exportBuffer + 1] = image
	
	-- Repeat as long as there is a request (there is a limit to avoid infinite loops)
	if Client.onPrepareRender ~= nil and Client.onPrepareRender(#exportBuffer) and #exportBuffer <= exportLimitConfig then
		Client:waitForSeconds(0.01)
		Client:renderImage(image.width, image.height)	
		return
	end
	
	Client.onPrepareRender = nil	

	-- Convert export Buffer to sprite
	local current = activeSprite
	local export = Sprite(image.width, image.height, ColorMode.RGB)
	export.filename = "Export"
	
	local layer = export.layers[1]
	layer.name = "Layer"
	
	export:setPalette(current.palettes[1])
	
	export.cels[1].image = exportBuffer[1]
	
	for i = 2, #exportBuffer, 1 do
		local frame = export:newEmptyFrame()
		local cel = export:newCel(layer, frame.frameNumber)
		cel.image = exportBuffer[i];
	end
	
	app.refresh()
	
	exportBuffer = {}
	noRefresh = false
end

-- On sprite change or site change events update events and resend frame to the view
function refreshView(command)
	if noRefresh then
		return
	end

	-- Check changes to active sprite
	if activeSprite == nil and app.sprite ~= nil then
		activeSprite = app.sprite
		activeSprite.events:on('change', refreshView)
		
	elseif app.sprite == nil and activeSprite ~= nil then
		activeSprite.events:off(refreshView)
		activeSprite = nil		
		
	elseif activeSprite ~= app.sprite then
		activeSprite.events:off(refreshView)
		activeSprite = app.sprite
		activeSprite.events:on('change', refreshView)

	end		
	
	Client:sendFrame(app.frame)
end

-- Listen to dropdown changes or manual refreshes and setup shader and shaderScript logic
function setFile()
	if Client.isConnected then
		-- Skip file refreshes during operations
		noRefresh = true
	
		-- Clean up previous file
		if shaderScript ~=nil then
			shaderScript:close()		
			shaderScript = nil
		end
		
		-- Reset export overwrite
		Client.onImageRecieved = nil
		
		local text = FileManager:readTextfile(dialog.data.shaderbox)
		Client:sendShader(text)
		
		-- Try execute shaderScript lua file with same name as shader
		shaderScript = FileManager:findShaderScript(dialog.data.shaderbox)
		
		if shaderScript ~= nil then
			shaderScript:setup(FileManager, Client)
		end
		
		noRefresh = false
	end	
end


---- [Plugin logic] ----------------------------------------------------------------
-- Plugin is first initialized
function init(plugin)
	FileManager:setPluginPath(plugin, fileDirectoryConfig)
	FileManager.onExport = createSprite;
	
	plugin:newCommand {
		id="shaderView",
		title="ShaderView",
		group="file_scripts",
		onclick=function()
			if not Client.isConnected then
				initialize()
			else			
				cleanup()
			end
		end,
		onchecked=function()
			return Client.isConnected
		end
	}
end

-- Plugin lifetime has ended
function exit(plugin)
	cleanup()
end