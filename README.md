# SpaceWars.Console

1 - Pressing UP_ARROW causes the ship to go in the 0 degree direction
2 - Pressing LEFT_ARROW causes the ship to go in the 270 degree direction
3 - Pressing RIGHT_ARROW causes the ship to go in the 90 degree direction
4 - Pressing DOWN_ARROW causes the ship to go in the 180 degree direction
5 - Pressing SPACE causes the ship to rotate towards a target and shoot
6 - If the ship is already facing at the target, the code does not give the "changeHeading" command to the server.
7 - If the ship is not facing at a target, the code changes the direction of the ship.
8 - Pressing R causes the ship to repair itself
9 - All purchasable items are displayed on the screen corresponding to a number key
10 - Pressing the corresponding number key will attempt to purchase the weapon if not bought already
11 - Pressing the corresponding number key will switch to the weapon if it is purchased
12 - Pressing I and a number will display more information about a weapon
13 - Pressing C causes the queue to clear
14 - Targeting needs to account for effective range of selected weapon
15 - The console properly displays shop/slot information
16 - Includes a way to track your position in the program
17 - Includes a way to calculate the angle to your enemies
18 - Includes a way to press multiple movement keys and give the server a single move command instead of two
19 - Includes a way to find the nearest enemy by distance
20 - Displays relevant errors when it can't do something
