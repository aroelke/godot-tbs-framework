# Turn-Based Strategy Framework Demo
In the demo for this framework, the player must accomplish one of a variety of objectives before the enemy
manages to accomplish any of theirs.
## Running the Demo
The main scene for the demo is [DemoMap.tscn](demo/DemoMap.tscn). To run the demo, run that scene from the editor.
## Factions and Alliances
There are four factions in the demo: blue (player), red (CPU), yellow (CPU), and green (CPU). Factions take turns
in that order, starting with blue. They are divided into two teams: blue and green, and red and yellow.
## Objectives
The player must complete one of the following objectives in order to win:
- Defeat all units in the red faction
- Defeat the unit in the yellow faction
- Occupy all four of the green-highlighted spaces at the bottom of the screen anytime during their turn
- Perform a "Capture" action in the white-highlighted space near the upper-right of the screen

The player loses if any of the following conditions are true:
- None of the above objectives has been completed by the end of the fifth turn
- All of the units in the blue faction have been defeated
- The green faction unit has been defeated
## Combat
All units in the demo have 10 health and are defeated upon reaching 0 health. Blue and red units attack each other
for 5 damage each, but blue units will get two attacks per engagement, can attack from two spaces away while red
units can only attack or retaliate against adjacent units, and red units have a 25% chance to miss when attacking
a blue unit. When attacking the yellow unit, blue units deal only 2 damage but still attack twice. The yellow
unit will not miss when attacking or retaliating against a blue unit, can do so from two spaces away, and will
deal 5 damage each hit. When attacking the green unit, red units will deal 3 damage and the green unit will deal 5
damage back.

The yellow and green units will not move, but will attack any enemy they can reach from their location.  Red units
may move to attack enemies or may wait to do so depending on battle conditions.

Blue units can also forego an attack to heal an ally unit for 5 health from an adjacent space. They cannot heal
themselves.
