# FemBoy
### An attempt at a cycle-accurate GameBoy emulator

Originally written to exercise/exorcise my game engine, Raven. It uses Raven's input handling, 2D rendering, UI, window managing, configuration, and timing systems. As such, it requires RavenEngine to build.

Distant goal is to add a modding framework with persistent tile/sprite editing and script-based code and memory insertion/replacement

Arrow keys for D-pad, X and Z for A and B, return and backslash/pipe for start/select. Currently no controller or remapping support.

No easy way to select a ROM yet, either start with a ROM as a command line argument, drag/drop a ROM onto the window, or press tilde to open the console and use "gb.LoadROM("rom_path_here")". The console is a full-on C# REPL.

### Features

#### Model/component/feature support
- [x] DMG
- [ ] CGB
- [x] Serial
- [ ] Split-screen Link Cable Multiplayer
- [x] Timer
- [ ] Audio
- [ ] RTC (implemented but only returns 0xFF)
- [ ] Rumble

#### Mappers
- [x] MBC1
- [ ] MBC2
- [x] MBC3
- [x] MBC5
- [ ] MBC7 + Accelerometer
- [ ] M161 Multi-cart
- [ ] MMM01 Multi-cart
- [ ] HuC1
- [ ] HuC3

#### Emulator features
- [ ] ROM Folder selection and ROM list
- [ ] Memory Viewer + Editor (current memory viewer is very simplistic)
- [ ] Tile Viewer + Persistent Tile Editor
- [ ] Custom DMG Palettes
- [ ] Input Remapping
- [ ] Controller Support (Not currently implemented in Raven)
- [ ] Save States
- [ ] Fast Forward
- [ ] Rewind (maybe?)

### Tests

#### Blargg
- [ ] cgb_sound (no CGB support yet)
- [x] cpu_instrs
- [ ] dmg_sound (no audio support yet)
- [x] instr_timing
- [x] interrupt_time
- [x] mem_timing
- [x] mem_timing-2
- [ ] oam_bug (passing 3 & 6, fail all others)
- [x] halt_bug

#### Numism
| Stage 1 | Stage 2 | Stage 3 | Stage 4 | Stage 5 |
|:-------:|:-------:|:-------:|:-------:|:-------:|
|  7/10   |  7/10   |  7/10   |  7/10   |   1/1   |

- Stage 1: Needs APU for 3/5/6
- Stage 2: Needs APU for 16/17, needs OAM corruption bug for 20
- Stage 3: Needs APU for 21, not certain why 23 fails- STAT japes, 26 fails due to PPU sprite fetching + stalls not being properly implemented
- Stage 4: 33 is more STAT japes, 37 needs better DMA blocking, 38 needs APU