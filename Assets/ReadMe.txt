*This is a prototype of an in-development tool so there will be bugs.

Make sure you have these packages installed:
1. Burst
2. Collections
3. Entities
4. Hybrid Renderer
5. Mathematics

Directions:
1. Open the Prop Spawner Window with Alt + P or by selecting Window > Prop Spawner.
2. Be sure a terrain is present in the scene.
3. Drag any prefabs that you want to be props into the left panel of the window.
4. Edit the rules for each prop to define how they should be spawned in relation to one another. 
5. Click "Spn" in the top left of the window.

-note: Each prop has two circles that define its boundaries, its base circle and its same model circle. These circles cannot overlap with the corresponding circles from other props. The Base radius field defines the extent of a prop's own space where no other props' space can overlap.The Same Model radius field defines the extent of a prop's circle where other prop's of the same kind cannot have their Same Model circle overlap.
	
Example: A tree has a Base radius of 1 and a rock has a base radius of 0.5. They will be at least 1.5 units apart. That same tree has a Same Model radius of 5 so all trees of that prop kind will be at least 10 units apart.
