# LeveledSpawnController
An item that creates spawns that level up in difficulty as you kill the creatures.<br>
<br>
-This is a small script for the Ultima Online emulator ServUO (www.ServUO.com).<br>
<br>
## Info:
This creates an item that controls the default Spawners that come with the emulator.<br>
(This does not work with xmlSpawners. I may look into adding it later.)<br>
<br>
This item holds a List of Spawners, and when activated, will cause them to spawn in order.<br>
You, as the admin, set up each Spawner with the list creatures you want, and add them to the Controller.<br>
When all of the creatures that belong to one Spawner are killed, the controller will spawn the next set of creatures in the list.<br>
<br>
## Set up:
Simply add the LeveledSpawnController item, along with a Spawner for each level you want the Controller to have, then add the Spawner to the item's list, and Activate!

