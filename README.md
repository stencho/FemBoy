# FemBoy: A maximalist GameBoy emulator

Originally written to exercise/exorcise my game engine, Raven. It uses Raven's input handling, 2D rendering, UI, window managing, configuration, and timing systems. As such, it requires RavenEngine to build.

The immediate goal is to make a couch-friendly emulator, with built in split-screen link cable multiplayer. A very distant goal, and the reason I chose this project, is a game modding framework, to allow a user to write scripts to add co-op play or new levels to classic titles. 
 
No easy way to select a ROM yet, either start with a ROM as a command line argument, drag/drop a ROM onto the window, or press tilde to open the console and use "gb.LoadROM("rom_path_here")". The console is a full-on C# REPL.

### Features

#### Model/component/feature support
- [x] DMG
- [ ] CGB
- [x] Serial
- [x] Timer
- [x] Save RAM
- [ ] Audio
- [ ] MBC3 RTC (implemented but only returns 0xFF)
- [ ] MBC5 Rumble
- [ ] MBC7 Accelerometer

#### Mappers
- [x] MBC1
- [ ] MBC2
- [x] MBC3
- [x] MBC5
- [ ] MBC7
- [ ] M161 Multi-cart
- [ ] MMM01 Multi-cart
- [ ] HuC1
- [ ] HuC3

#### Emulator goals (consider this my TODO)
- Input Remapping
- ROM Folder selection and ROM list
- Save States
- Save RAM management/backups/selection
- Split-screen Link Cable Multiplayer
- Memory Viewer + Editor (current memory viewer is very simplistic, though still useful. Ctrl+M to display)
- Tile Viewer + Persistent Tile Editor
- Custom DMG Palettes
- Fast Forward
- Rewind (maybe?)

### Tests

#### Blargg
- [ ] cgb_sound (no CGB support yet)
- [x] cpu_instrs
- [ ] dmg_sound (no audio support yet)
- [x] instr_timing
- [x] interrupt_time (At least, failing correctly for DMG)
- [x] mem_timing
- [x] mem_timing-2
- [ ] oam_bug (passing 3 & 6, fail all others)
- [x] halt_bug

#### Mooneye
| Pass | DNF   | Fail | Total |
|:----:|:-----:|:----:|:-----:|
|  42  |   9   |  15  |  66   |

- Currently excluding 9 tests which always fail on DMG 
- All DNF tests appear to be due to DMA CPU bus blocking-related timing issues, leading to $FF opcodes trashing RAM
- All DNF tests are also _timing tests, jp/ret/reti/etc

#### GBMicroTest
| Pass |  DNF  | Fail  | Total |
|:----:|:-----:|:-----:|:----:|
| 212  | 29    | 272   | 513  |

#### Numism
| Stage 1 | Stage 2 | Stage 3 | Stage 4 | Stage 5 |
|:-------:|:-------:|:-------:|:-------:|:-------:|
|  7/10   |  7/10   |  7/10   |  8/10   |   1/1   |

- Stage 1: Needs APU for 3/5/6
- Stage 2: Needs APU for 16/17, needs OAM corruption bug for 20
- Stage 3: Needs APU for 21, not certain why 23 fails (STAT japes), 26 fails due to very slight PPU timing issues
- Stage 4: 33 is more STAT japes, 38 needs APU
- Some of the passes ARE false positives due to the stubbed out APU
