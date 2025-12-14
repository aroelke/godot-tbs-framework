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
## Running the Demo
This project includes a demo that indicates how to set up a simple scene with the framework located in the "demo" directory. To run it,
open the scene in the editor and click the "run current scene" button. There is no default scene set for this project.
## License
The license for this project only covers the source code, scenes, and node icons.  Assets used in the demo have their own licenses
which are included in their respective directories (all are CC0). Special thanks to Kenney (kenney.nl) and Ne Mene
(https://ne-mene.itch.io/) for providing them.
