⚠️ READ BEFORE USING ⚠️

* Action, Control Bindings
* Navigate 3D Camera, Hold Right Mouse Button + Move Mouse
* Camera Movement, W / A / S / D (While holding Right Click)
* Vertical Ascent/Descent, E / Q (While holding Right Click)
* Camera Sprint Multiplier, Hold Left Shift
* Select Entity, Left Click on object bounding volume within viewport / hierarchy
* Translate Selected Entity, Hold Left Shift + Hold Left Click on object and drag across plane



📜 Scripting Interface Example
Attach a ScriptComponent to any active entity in the inspector panel to implement custom game behavior:

```
-- Lua Script
function start()
    -- Executed exactly once upon entering play mode
    print("FrostEngine Entity Initialized Successfully.")
end

function update()
    -- Invoked on every single logic tick frame
end
```
