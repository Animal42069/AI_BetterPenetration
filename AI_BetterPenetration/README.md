# AI_BetterPenetration

This mod is an attempt to make the penetration that occurs in AI look a little bit better.  I started with Roy12's PVCollider, but I ended up making so many changes that I felt it was better to create a separate repository for it rather than try to pull it in to his.  

# Features

Maintains the length of the penis at all times, including penetraion.  This is accomplised by finding an appropriate spot within the girl and placing the penis tip at that spot.<br />
This is done for vaginal, anal, and oral penetraion.<br />
Maintain proper rotation of the penis, no more spinning shafts during certain positions.<br />
Adjust the length of the penis, make it as short or long as you want.<br />
Adjust the girth of the penis <br />
Adjust the size of the sack <br />
Can add softness to the penis.  A soft penis will squish/shorten after penetration (similar to unmodded behavior, but not to the same extent) <br />
Options to adjust properties of the dynamic bones.<br />

# Notes

Male uncensor requires a shaft bone and a head bone, which most have.<br />
No special female uncensors are needed, but if you use Roy12's vagina uncensor it will utilize the dynamic bones if they are present.<br />
The mod tries to place the head inside the girl at a position that pierces the original target (vagina, anus, mouth).  Due to sizes, lengths, angles and different positions this isn't always possible.  It is recommended to use Mantas' BetterHScenes to adjust the characters in the scene to make the geometry involved more favorable. <br />
Currently the algorithms have difficulty locating targets when they are shifted too far apart horizontally (Vertical is OK and depth/distance is OK).  This is pretty rare, as no positions do this by default, you would need to move characters with BetterHScenes to make this happen.
