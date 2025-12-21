# C# Turn-Based Strategy Framework for Godot
## Introduction
This is a framework written in C# for building turn-based strategy games in Godot 4.4 or later. It has the following features:
 - Support for a variable number of factions with customizable alliances that can be controlled either by player or CPU
 - Fully customizable units with classes and stats specified with resources
 - Multiple types of CPU behavior that can be specified per unit, including:
   - "Passive" or "guarding" behavior, which prevents movement and optionally also action
   - Aggressive behavior, where CPU units will chase and attack enemies
   - Switching between different behaviors based on time or locations of other units on the map
 - A variety of map objectives that can be combined in multiple ways, including:
   - Defeat specific enemy units or all units in a faction
   - Survive for some number of turns (or complete another objective within some number of turns)
   - Occupy a region of the map with an option to require an action be performed there and/or require multiple units to be there
   - Using any objective as failure rather than success (e.g. all player units being defeated leads to failure)
- Display of combat using a cut scene where participating units attack each other _(currently this is mandatory)_
- Free switching between keyboard, mouse, and gamepad controls with free switching between joystick and dpad controls on gamepad
## How to Use the Framework
### Setting Up a Map Scene
A "map scene" is a scene containing the grid and units that move around it. This is where the player will interact with units and
command them, moving them around and controlling their interactions with each other.
#### The Scene Tree
Most nodes in this scene must be arranged in a specific hierarchical manner, which is used to determine units' allegiances to factions
and the turn order, among other things.

The hierarchy looks like this:
- `LevelManager`
  - `Grid`
    - "Ground" `TileMapLayer`
    - "Terrain" `TileMapLayer`
    - Any number of additional `TileMapLayer`s representing special regions of the map
  - First `Army`
    - `ArmyController`
    - Any number of `Unit`s
      - Unit `Behavior` (if CPU controlled)

#### Event Controller
The scene tree should also contain an `EventController`, which is intended to be used for scripting custom map events. Whenever an `Army`
begins its turn, a `Unit` ends its action, or an `Army` ends its turn, the `LevelManager` will signal the event and wait for an
acknowledgment signal that returns control to it. The `EventController` receives these signals and returns control once its scripted event is
complete. By default, `EventController` will do nothing with these signals except check that any objectives have been completed and then
returns control to the `LevelManager` if none have been. The `EventController` does not have to be in any specific place in the scene tree
and could even be the root if desired.

#### Map Objectives
Map objectives are specified using a tree of `Objective` nodes. There are several types of `Objective`s available:
- `ActionObjective`: One or more units must enter a specific region of the map specified using a `SpecialActionRegion`, which is a special
  type of `TileMapLayer` that defines a special action to be performed, and perform that region's action
- `OccupyObjective`: One or more units must be in a specific region of the map when the objective is evaluated
- `DefeatUnitObjective`: A specific unit must be defeated
- `RouteObjective`: All units in a specific faction must be defeated
- `TimeObjective`: Automatically completed at the end of a specific turn

`Objective`s can be combined by adding them to the scene tree as children of an `AggregateObjective`. There are two types of
`AggregateObjective`s: `AllObjective`, which is only complete as long as all of its child objectives are complete; and `AnyObjective`,
which is complete as long as any of its child objectives are complete.

`Objective`s do not have to be anywhere specific in the scene tree. They primarily interact with the `EventController` to be evaluated and
determine the result of completion, which allows them to be interpreted in any way desired. The default `EventController` contains a property
specifying an `Objective` to interpret as successful completion of the map and one that is interpreted as failure of the map.
#### Creating an Army
Each `Army` has three main components:
- A `Faction` property which is specified using a `Faction` resource and determines the alliance it has with other factions
- One `ArmyController` child node that specifies if the `Army` is player-controlled or CPU-controlled
- Any number of `Unit` child nodes

Any number of `Army`s can be present on the map, but each one should have a unique `Faction`. If a `Unit` is dragged directly into the scene
tree as a child of an `Army`, the `Army` will automatically set that `Unit`'s `Faction` to its own. By default, the first `Army` in the tree will
be the one to take its first turn, but `LevelManager` has a `StartingArmy` property that allows specification of who takes the first turn.
Subsequent turns follow the order each `Army` in the tree is listed.

Each `Unit` has a `Class` property that specifies its appearance on the map and in combat and a `Stats` property that specifies its performance
in battle. A `Class` is simply a mapping of `Faction` resources onto scenes to use to represent them on the map and in combat and the `Stats`
resource is simply a collection of numbers that are combined with other `Unit`s' `Stats` to determine combat outcome and where on the map it can
move and interact with other `Unit`s. A `Unit`'s `Faction` cannot be manually specified; instead, it is determined by the `Army` parent it has.

If a `Unit` is part of a CPU-controlled `Army`, it must have a `Behavior` child node providing the `AIController` information about what the unit
is allowed to do on its turn. There are three main types of `Behavior`s:
- `StandBehavior`: prevents its parent `Unit` from moving and optionally allows it to act
- `MoveBehavior`: allows its parent `Unit` to move to a new location to perform its action
- `SwitchBehavior`: provides a mechanism for the `Unit` to change the way it acts during the level.

Each `SwitchBehavior` must have one or more `SwitchCondition` child nodes and exactly two `Behavior` child nodes. The `SwitchCondition` nodes
specify when the parent `Unit` switches from the first `Behavior` in the list to the second one. There are several types of `SwitchCondition`s
included:
- `RegionSwitchCondition`: switch to the second behavior when one or more `Unit`s have entered a region of the map specified using a `TileMapLayer`
- `InRangeSwitchCondition`: switch to the second behavior when one or more `Unit`s have entered the total attack range of one or more other `Unit`s
- `ManualSwitchCondition`: switch to the second behavior when the `Trigger` method is called (for example, when a signal is raised)
- `TurnSwitchCondition`: switch to the second behavior after a certain number of turns have passed

`SwitchCondition` also allows for its `Behavior` selection to revert if its `SwitchCondition` stops being satisfied.
### Connecting a Combat Scene
_Note: Currently a separate combat scene is required. Future updates will add support for combat on the map._

The combat scene is flexible and designed to support any kind of combat situation. Combat scenes should extend the `CombatScene` abstract class, which
only requires an implementation of a `Start()` method to initiate the combat sequence and an `End` method to indicate the end. Implementations of the
`End()` method should call `SceneManager.ReturnToPreviousScene()` to return back to the map. This method can also be called anywhere else in the
combat script to return to the map at any time.

Once the combat scene is created, it can be connected to the `LevelManager` by assigning the path to the .tscn file to the `LevelManager`'s
`CombatScenePath` property. When a `Unit` initiates an interaction with another unit, the `LevelManager` will instantiate the `CombatScene` and switch
to it to play the combat sequence.

#### Notes About Implementing Combat
- Combat results are computed by the `LevelManager` before transitioning to the combat scene and automatically applied after returning. The combat
  scene should not make changes to the state of the map.
- Updates to combat mechanics and unit stats can be made by manually modifying the code in the following places:
  - The [`Stats`](src/TbsFramework/Data/Stats.cs) resource to change what stats units have (make sure there's always an `AttackRange` property and
    `SupportRange` property)
  - The [`CombatCalculations`](src/TbsFramework/Scenes/Combat/Data/CombatCalculations.cs) static class to change the way combat results are calculated
  - The [`AIController`](src/TbsFramework/Scenes/Level/Control/AIController.cs) class to update its combat simulation to reflect the changes made to
    `CombatCalculation`
### Controls
The framework uses the following input actions:
- `digital_move_up` (keyboard Up, dpad up), `digital_move_left` (keyboard Left, dpad left), `digital_move_down` (keyboard Down, dpad down),
  `digital_move_right` (keyboard key, dpad right): move the cursor in the indicated direction
- `analog_move_up` (left stick up), `analog_move_left` (left stick left), `analog_move_down` (left stick down), `analog_move_right` (left stick right):
  move the pointer in the indicated direction
- `accelerate` (keyboard Ctrl, gamepad right trigger): use with `digital_move_*` to skip cursor as far as it can go in the indicated direction or with
  `analog_move_*` to make the pointer move faster
- `select` (keyboard Space, gamepad south button, mouse left click): confirm a selection
- `cancel` (keyboard Esc, gamepad east button, mouse right click): cancel a selection
- `skip` (keyboard Esc, gamepad east button, mouse right click): skip through a unit's movement or CPU-controlled `Army`'s turn
- `previous` (keyboard Q, gamepad left shoulder, mouse thumb 1): cycle the cursor to the previous target or available unit
- `next` (keyboard E, gamepad right shoulder, mouse thumb 2): cycle the cursor to the next target or available unit
- `toggle_danger_zone` (keyboard C, gamepad north button, mouse middle click): toggle highlighted enemy's attack range, highlighted ally's movement
  range, or all enemies' attack range
- `digital_zoom_in` (keyboard Page Up, mouse wheel up), `digital_zoom_out` (keyboard Page Down, mouse wheel down), `analog_zoom_in` (right stick up),
  `analog_zoom_out` (right stick down): zoom map camera in or out

When controlling the pointer using the mouse or gamepad left stick, the cursor will automatically move to the cell the mouse is hovering over unless a
target for an action is being chosen, in which case it will only make the move if the pointer is hovering over a valid target.  Attempting to make a
selection when it isn't will cancel the target selection rather than confirm it.

In addition to the above input actions, the built-in UI input actions (`ui_left`, etc.) are used to control context menus. 
## Running the Demo
This project includes a demo that indicates how to set up a simple scene with the framework located in the "demo" directory. To run it,
open the scene in the editor and click the "run current scene" button. There is no default scene set for this project.

For more information on the demo, see [its README](demo/README.md).
## License
The license for this project only covers the source code, scenes, and node icons.  Assets used in the demo have their own licenses
which are included in their respective directories (all are CC0). Special thanks to Kenney (https://kenney.nl/) and Ne Mene
(https://ne-mene.itch.io/) for providing them.
