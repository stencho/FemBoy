# FemBoy
### A misguided attempt to write a t-cycle-aligned GameBoy emulator

FemBoy was originally written to exercise/exorcise my game engine, Raven. It uses Raven's input handling, 2D rendering, UI, window managing, configuration, and timing systems. As such, it requires RavenEngine to build.

Also originally written with no regard for timing accuracy, but there's something clearly wrong with my brain, so here we are.

Currently only supports dot-matrix GameBoy emulation. MBC1 fully implemented, MBC3 mostly implemented, but RTC will always return 0xFF for all values. This probably breaks games. MBC5 implemented, but no rumble yet. Saves supported. Fairly capable of running a lot of titles which are not heavily reliant on PPU mode 3 timing (due to a lack of sprite fetcher stalling/actual sprite FIFO in general). No sound support yet.

Arrow keys for D-pad, X and Z for A and B, return and backslash/pipe for start/select. Currently no controller or remapping support.

No easy way to select a ROM yet, either start with a ROM as a command line argument, drag/drop a ROM onto the window, or press tilde to open the console and use "gb.LoadROM("rom_path_here")". The console is a full-on C# REPL.