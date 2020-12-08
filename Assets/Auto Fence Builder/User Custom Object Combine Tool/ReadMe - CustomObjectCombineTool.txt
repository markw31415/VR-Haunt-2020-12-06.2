If you have a Game Object you want to use as a custom rail/wall or post that is a complex group of nested objects instead of a simple single GameObject, you can use this tool to convert it to a single combined object.

This will not only make working with it in AFB much easier and intuitive (especially for nested objects with rotations), but is also more efficient for Unity and the GPU.

———

Add the CustomObjectCombineTool prefab to your hierarchy.

Drag the top-level object that you want to combine into the ‘Object to Combine’ slot and press ‘Combine’.
This will create a combined copy, leaving your original untouched. You can then drag this in to one of the custom post/rail/extra slots.

