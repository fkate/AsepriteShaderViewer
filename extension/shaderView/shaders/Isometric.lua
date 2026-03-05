-- Example script showing of the 3D capabilities of the view
-- To get started there are two requirements: a "#define USE3D" as first line in the shader and a custom mesh
-- Using the define will cause the view to switch control mode to a rotation based approach
-- View input will be raycasted onto the mesh front faces and will return the clicked pixel (check the input method)

-- Meshes can be uploaded from an OBJ file with basic UV / Normal support; vertex colors and animation are unsupported
-- If the OBJ file consists of multiple meshes the meshID can be extracted via the "iMeshID" float inside the shader
-- For best practice use meshes with a scaling of around 1.0

-- This script also shows how to pack custom data (light data) into a seperate image channel to use within a mesh
-- Lastly it shows an example on how to set up a custom rendering export loop (for isometric tiles)

ShaderScript = { dialog = nil, buffer = nil, lastBtn = 0 }

-- Called when shader is loaded. Used to send additional data or set up controls
function ShaderScript:setup(fileManager, client)
	-- Setup a simple info dialog
	self.dialog = Dialog { title = "3D Example" }	
	
	self.dialog:separator {
		text = "Light"
	}
	
	-- Called whenever a light value changes
	function setLight()
		-- Convert direction from 0-360 to 0-255 and send changes
		local yaw = ((self.dialog.data.lightyaw + 180.0) / 360.0)
		local pitch = ((self.dialog.data.lightpitch + 180.0) / 360.0)
		local color = self.dialog.data.lighttint
		local fixed = self.dialog.data.lightfixed and 255 or 0
	
		-- Set first two buffer pixels to encoded light values
		self.buffer:drawPixel(0, 0, color)
		self.buffer:drawPixel(1, 0, Color { r = yaw * 255, g = pitch * 255, b = fixed, a = 255 })
		client:sendImage(self.buffer, 1, "point", "clamp")
	end
	
	self.dialog:color {
		id = "lighttint",
        label = "Tint",
        color = Color { r=255, g=255, b=255, a=255 },
        onchange = setLight
	}
	
	self.dialog:slider {
		id = "lightyaw",
		label = "Yaw",
		min = -180.0,
		max = 180.0,
		value = 25.0,
		onchange = setLight
	}
	
	self.dialog:slider {
		id = "lightpitch",
		label = "Pitch",
		min = -90.0,
		max = 90.0,
		value = 25.0,
		onchange = setLight
	}	
		
	self.dialog:check {
		id = "lightfixed",
		label = "Light orientation",
		text = "Fixed to view",
		selected = true,
		onclick = setLight
	}	
	
	self.dialog:separator {
		text = "Export"
	}
		
	self.dialog:button {
		text = "Isometric (8 direction)",
		onclick = function()
			self:onExport(client, 64, 64, 8, 30.0, 1.415)
		end
	}
	
	self.dialog:button {
		text = "Isometric (16 direction)",
		onclick = function()
			self:onExport(client, 64, 64, 16, 30.0, 1.415)
		end
	}
	
	self.dialog:show { wait = false }
		
	-- Send the obj mesh to the view
	client:sendMesh(fileManager:readTextfile("Halftile.obj"))

	-- Create an image buffer and assign default values
	-- Values are controled via the dialog
	self.buffer = Image(2, 1)
	setLight()
end

-- Custom export render loop for multi side images
function ShaderScript:onExport(client, width, height, sides, tilt, zoom)
	-- Use "client:renderImage(width, height)" to export a single image and "client:renderImages(width, height, onPrepare)" to process multiple into one sprite
	-- RenderImages takes a custom function with a number containing how many itterations have passed and needs to return a boolean that decides when to stop the loop
	-- It often makes sense to pass local values from outside the loop (Frame.Next() or in this case sides) to track conditions over multiple itterations
	-- When preparing for a new itteration don't forget to update the values or the frame via "client:setFrame(frame)" (if necessary) since app events are skipped during rendering
	client:renderImages(width, height, function(i)
		if i >= sides then
			return false
		else		
			client:sendRotation(360.0 * (i / sides), tilt, zoom)
			return true
		end
	end)
end

-- Called when the user clicks inside the view. Coordinates are in the sprites pixel space
-- The btn value holds the input: 1 is left 2 is right; when the number is negative the coresponding button was released
function ShaderScript:processInput(btn, x, y)
	-- Grab the current cel info
	local cel = app.cel;
	if cel == nil then
		return false
	end

	-- Check for button: 1 is left 2 is right; when the number is negative the coresponding button was released
	if btn == 1 then
		-- when button is first down clone image to create an undo step
		if self.lastBtn == 0 then
			cel.image = cel.image:clone()
			self.lastBtn = 1
		end
		
		-- Check repeated; Return true to tell the view to update
		if self.lastBtn == 1 then
			cel.image:drawPixel(x - cel.position.x, y - cel.position.y, app.fgColor)
			return true
		end
	elseif btn == -self.lastBtn then	
		-- reset input when released button was hold button
		self.lastBtn = 0
	end
	
	return false
end

-- Called when the shader is unloaded. Used to clean up resources
function ShaderScript:close()
	-- clean up info dialog
	self.dialog:close()
	self.dialog = nil
end

return ShaderScript