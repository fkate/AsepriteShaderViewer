-- Example script for sending additional textures to the View
-- Shaders support up to three additional channels (iChannel0 for the main texture and 1-3 for custom data)

ShaderScript = { dialog = nil }

-- Called when shader is loaded. Used to send additional data or set up controls
function ShaderScript:setup(fileManager, client)
	-- Load texture "Noise.png" from the same subpath as this file and send it as rgba image to the view
	-- The first parameter is the channel ID (1-3)
	-- The second parameter is the filter mode ("point" or "linear")
	-- The third parameter is the wrap mode ("clamp" or "repeat")
	client:sendImage(fileManager:readImagefile("Noise.png"), 1, "linear", "repeat")
end

-- Called when the user clicks inside the view. Coordinates are in the sprites pixel space
-- The btn value holds the input: 1 is left 2 is right; when the number is negative the coresponding button was released
function ShaderScript:processInput(btn, x, y)
	-- Return true to force Aseprite to refresh
	return false
end

-- Called when the shader is unloaded. Used to clean up resources
function ShaderScript:close()

end

return ShaderScript