# FemBoy
### A misguided attempt to write a t-cycle-aligned GameBoy emulator

FemBoy was originally written to exercise/exorcise my game engine, Raven. It uses Raven's input handling, 2D rendering, UI, window managing, configuration, and timing systems. As such, it requires RavenEngine to build.

Also originally written with no regard for timing accuracy, but there's something clearly wrong with my brain, so.

Currently only supports dot-matrix GameBoy emulation. Fairly capable of running a lot of titles which are not heavily reliant on PPU mode 3 timing, especially with sprites involved. No sound support yet. 