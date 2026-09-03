using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FemBoy;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FemBoy;
using Raven.Engine;

namespace FemBoy;

public class GameboyEmulator {
    public GameBoy gameboy;

    public CPU CPU => gameboy.CPU;
    public PPU PPU => gameboy.PPU;
    public IMemory RAM => gameboy.RAM;
    public Cartridge Cartridge => gameboy.Cartridge;
    
    public Texture2D texture;
    
    public Color[] frame_buffer = new Color[160 * 144];

    public GBInput input;
    
    public int cycles = 0;

    private bool CRASHED = false;
    public bool Crashed => CRASHED;

    private string current_ROM = "";
    public string ROMName = "";
    
    bool _execution_paused = false;
    public bool ExecutionPaused => _execution_paused;

    private bool run_single_execution_step = false;
    
    public GameboyEmulator() {
        texture = new Texture2D(State.graphics_device, 160, 144);
        
    }

    ~GameboyEmulator() {
        while (SaveGame.CurrentlySaving) ;
    }
    
    private bool reloading = false;
    public void ReloadROM() {
        while (SaveGame.CurrentlySaving) ;
        
        Interlocked.Exchange(ref reloading, true);
        
        LoadROM(current_ROM);
        
        Interlocked.Exchange(ref reloading, false);
    }
    
    public void LoadROM(string filename) {
        Interlocked.Exchange(ref reloading, true);
        gameboy = new GameBoy();
        
        //gameboy.CPU.StartTrace("trace.txt");
        
        UpdateFrameBufferTexture();
        GC.Collect();
        
        input = new GBInput(gameboy);
        ROMName = Path.GetFileName(filename);
        State.window.Title = $"FemBoy [{ROMName}]";
        
        cycles = 0;
        total_frames = 0;
        CRASHED = false;
        //_execution_paused = false;
        
        current_ROM = filename;
        
        gameboy.LoadROM(filename);
        
        Interlocked.Exchange(ref reloading, false);
        
    }

    public void WriteMemory(ushort address, byte value) => gameboy.WriteMemory(address, value);
    public byte ReadMemory(ushort address) => gameboy.ReadMemory(address);
    
    public void ToggleExecution() {
        _execution_paused = !_execution_paused;
    }
    
    public void PauseExecution() => _execution_paused = true;
    public void ResumeExecution() => _execution_paused = false;
    
    public void StepExecution() {
        _execution_paused = true;
        run_single_execution_step = true;
    }
    
    public int total_frames = 0;
    
    public void Update() {
        if (CRASHED) return;
        if (gameboy == null || input == null) return;
        if (ExecutionPaused && run_single_execution_step) {
            run_single_execution_step = false;    
        } else if (ExecutionPaused) return;
        
        input.Update(gameboy.joypad);

        try {
            while (cycles < 70224 && !gameboy.CPU.wants_pause) {
                if (reloading) break;
                gameboy.Tick();
                cycles++;
            }
            
        } catch (Exception ex) {
            CRASHED = true;
            Console.WriteLine(ex.Message);
        }
        
        cycles = 0;
        total_frames++;
        
    }

    public void Render() {
        if (CRASHED) return;
        if (gameboy == null || input == null) return;
        
        if (gameboy.PPU.frame_ready) {
            UpdateFrameBufferTexture();
            gameboy.PPU.frame_ready = false;
        }
    }

    void UpdateFrameBufferTexture() {
        
        Parallel.For(0, 160 * 144, (c) => {
            //for (int c = 0; c < 160 * 144; c++) {
            switch (gameboy.PPU.frame_buffer[c]) {
                case 0:
                    frame_buffer[c] = new Color(155, 188, 15);
                    break;
                case 1:
                    frame_buffer[c] = new Color(139, 172, 15);
                    break;
                case 2:
                    frame_buffer[c] = new Color(48, 98, 48);
                    break;
                case 3:
                    frame_buffer[c] = new Color(15, 56, 15);
                    break;
                default:
                    frame_buffer[c] = new Color(255, 0, 255);
                    break;
            }
        });
            
        texture.SetData(frame_buffer);
    }
}