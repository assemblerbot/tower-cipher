# Tower cipher printing

## Prerequisites
You will need:
- 1 kg of filament (a bit less, but it depends on your slicer settings)
- 5mm metal or glass ball

I recommend at least 0.6mm nozzle to get reasonable print times.

## Slicing
I recommend following settings but of course you know your printer and what works for it the best.
No supports are needed.

### Axle
This part should be strong.
- 3 perimeters
- 15% gyroid infill
![Axle](Axle.png)

### Base, Top and Bolt
Nothing special, I've used:
- 2 perimeters
- 10% infill
![Base](Base.png)

### Break
**Print it 3x** and with following parameters:
- 2 perimeters
- 15% infill
![Break](Break.png)

### Rotors
The tricky part. Can be printed on Enter 3 with default speeds under 10 hours with following settings:
- 2 perimeters
- **10% rectilinear infill**
- add range modifier between heights 1mm - 57mm and set *Bottom solid layers* and *Top solid layers* to 0
  - this settings significantly reduce printing time while that infill supports pathways nicely
- Solid infill threshold area: 0
![Rotor](Rotor.png)

There is no need for solid infill inside the rotor.
![RotorInternalStructure](RotorInternal.png)

### Tweezers
Tweezers can be a bit problematic if you have warping troubles.
- 4 perimeters
- 15% infill (almost doesn't matter)
- I've turned off "Fill gaps" - that helped with warping because extruder is not returning back to fill in those tiny holes

## Printing
When you print everything, you should have these parts.
![All printed parts](AllPrintedParts.jpg)

## Assembly
Insert Axle into Base part (should be tight fit without any gabs, you can youse glue or any screws that fits from bottom if needed).
Insert Breaks into Rotors (see the picture).
![Rotor assembly](RotorAssembly.png)

Assembled parts:
![Assembled parts](AssembledParts.jpg)

## Coloring
Optional, I've used Citadel acrylic paints for miniatures with black undercoat.
