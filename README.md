# Short description
An in-game-script for Space Engineers showing power statistics on programmable blocks and LCDs.

With this script one is able to see, if the power-trend is increasing or decreasing, what power-producers are on the grid and how many batteries are available.

I needed this, because my first buildings had too less batteries and too less power-producers. This script is also very helpful on space ships to manage the power-drop from jump-drives.

This script will output some details on current mass and volume to the display of the programmable block.

It will give extra details (like defined blocks) in the debugging-section of the programmable block.

# Custom Data (Properties)

## Another text-surface
You can define another text-surface to use for output of the stats.

### text-surface (string)

This `string`-property will define the name of the block with a text-surface (display) where to print the power-stats to. If it is not defined, the displays of the programmable block will be used.
Currently only the first available text-surface on the given block will be used. (I have an idea to split the visual battery and the stats to be shown on defineable text-surfaces someday).
