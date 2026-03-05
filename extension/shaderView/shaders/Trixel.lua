-- Trixel shader
-- Special Shader that turns 2x2 pixel blocks by 45 degrees	
-- Input inside the view is converted to draw commands
-- Use right click to draw and left click to pick a color	
-- Undo/Redo only works when the Aseprite window is focused

ShaderScript = { lastBtn = 0 }

-- Called when shader is loaded. Used to send additional data or set up controls
function ShaderScript:setup(fileManager, client)

end

-- Called when the user clicks inside the view. Coordinates are in the sprites pixel space
-- The btn value holds the input: 1 is left 2 is right; when the number is negative the coresponding button was released
function ShaderScript:processInput(btn, x, y)
	-- Grab the current cel info
	local cel = app.cel;
	if cel == nil then
		return false
	end

	-- Convert input to triangle grid
	local gridX = math.floor(x / 2) * 2
	local gridY = math.floor(y / 2) * 2
	
	local sideX = math.fmod(x, 2.0) - 1.0
	local sideY = math.fmod(y, 2.0) - 1.0
	
	local absX = math.abs(sideX)
	local absY = math.abs(sideY)	

	if absY > absX then 
		if sideY < 0 then 
			x = gridX
			y = gridY
		else 
			x = gridX + 1
			y = gridY + 1
		end
	else
		if sideX < 0 then 
			x = gridX
			y = gridY + 1
		else
			x = gridX + 1
			y = gridY
		end
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
	elseif btn == 2 then
		-- Pick color; Simple lastBtn assignment since we don't generate undo information
		app.fgColor = cel.image:getPixel(x - cel.position.x, y - cel.position.y)
		self.lastBtn = 2
	elseif btn == -self.lastBtn then	
		-- reset input when released button was hold button
		self.lastBtn = 0
	end
	
	return false
end

-- Called when the shader is unloaded. Used to clean up resources
function ShaderScript:close()

end

return ShaderScript