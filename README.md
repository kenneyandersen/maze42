# Maze 42
Maze 42 is a 3 dimensional maze generator.
It was build to be able to generate "blueprints" for building a maze in Valheim.

It will generate images like this:

<img width="265" alt="image" src="https://user-images.githubusercontent.com/20858590/218342164-81061af6-b389-4a09-96ed-deda573bd399.png"><img width="263" alt="image" src="https://user-images.githubusercontent.com/20858590/218342170-fcda5cdb-d504-41de-9c73-81cce2200ba4.png">


For now it is a c# linqpad script. If i get around to it, I might convert it into a proper tool later on.
Download Linqpad to run it: https://www.linqpad.net/ 
If you want to run it in Visual Studio, it should work if you remove ".Dump()" which is present a few places (it is linqpad extension to create a visual dump within Linqpad)

Parameters that can be set:
- Dimensions (length, width, height)
- Start coordinate (x,y,z), zero indexed, and marked with an S in the images
- End coordinate (x,y,z), zero indexed, and marked with an E in the images
- Save location (folder to store images, default c:\map, it will generate one image per level in the maze)
- whether to allow multiple level changes at one place. If you are building it without stairs, but have to jump you might not want to have to jump 2+ levels up. 


Disclaimer: Code is written to work, not to be pretty. A lot of stuff I am not happy with, and moved through different attemps that has not been fully cleaned up.
