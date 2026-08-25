using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RatGBLib;
using Raven.Engine;

namespace RatGB;

public class GameboyEmulator {
    public GameBoy gameboy;
    
    public Texture2D texture;
    
    public Color[] frame_buffer = new Color[160 * 144];

    public GBInput input;
    
    public int cycles = 0;

    private bool CRASHED = false;
    public bool Crashed => CRASHED;

    private string current_ROM = "";
    
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
        input = new GBInput(gameboy);

        State.window.Title = $"RatGB [{Path.GetFileName(filename)}]";
        
        cycles = 0;
        CRASHED = false;

        current_ROM = filename;
        gameboy.LoadROM(filename);
        Interlocked.Exchange(ref reloading, false);
    }
    
    public void Update() {
        if (CRASHED) return;
        if (gameboy == null || input == null) return;
        
        input.Update(gameboy.joypad);

        try {
            while (cycles < GameBoy.CYCLES_PER_FRAME) {
                if (reloading) break;
                cycles += gameboy.Execute();
            }
        } catch (Exception ex) {
            CRASHED = true;
        }

        cycles = 0;
    }

    public void Render() {
        if (CRASHED) return;
        if (gameboy == null || input == null) return;
        
        if (gameboy.PPU.frame_ready) {
            for (int c = 0; c < 160 * 144; c++) {
                byte b = gameboy.PPU.frame_buffer[c];
                switch (b) {
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
                }
            }
            
            texture.SetData(frame_buffer);
            gameboy.PPU.frame_ready = false;
        }
    }
}