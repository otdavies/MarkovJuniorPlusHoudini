// Copyright (C) 2022 Maxim Gumin, The MIT License (MIT)
// Modified by Oliver Davies

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

static class Program
{
    // CLI usage example: 
    // dotnet run source/models.xml source/resources/palette.xml
    static void Main(string[] args)
    {
        // Initialize modules we leverage
        Stopwatch sw = Stopwatch.StartNew();

        CreateOutputDirectory("output");

        foreach (var arg in args)
        {
            Console.WriteLine($"Argument: {arg}");
        }

        // Load our pallete and document
        Dictionary<char, int> palette = CommandLine.Pallete(args);
        XDocument models = CommandLine.Document(args);

        ExecuteSingle("Test2", models, palette, 20);
        
        Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
    }

    private static void ExecuteSingle(string name, XDocument model, Dictionary<char, int> pallete, int linearSize, int customSeed=0, int steps=2000, int dimension=3, int amount=3, int pixelsize=4) {
        Random random = new();
        int MX = linearSize;
        int MY = linearSize;
        int MZ = dimension == 2 ? 1 : linearSize;

        Interpreter interpreter = Interpreter.Load(model.Root, MX, MY, MZ);
        if (interpreter == null)
        {
            Console.WriteLine("Interpreter errored!");
            return;
        }
        Console.Write($"{name} > ");
        for (int k = 0; k < amount; k++)
        {
            int seed = customSeed;
            if (seed == 0) seed = random.Next();
            foreach ((byte[] result, char[] legend, int FX, int FY, int FZ) in interpreter.Run(seed, steps, false))
            {
                int[] colors = legend.Select(ch => pallete[ch]).ToArray();
                string outputname = $"output/{name}_{seed}";
                var (bitmap, WIDTH, HEIGHT) = Graphics.Render(result, FX, FY, FZ, colors, pixelsize, 0);
                // GUI.Draw(name, interpreter.root, interpreter.current, bitmap, WIDTH, HEIGHT, pallete);
                Graphics.SaveBitmap(bitmap, WIDTH, HEIGHT, outputname + ".png");
                VoxHelper.SaveVox(result, (byte)FX, (byte)FY, (byte)FZ, colors, outputname + ".vox");
            }
            Console.WriteLine("DONE");
        }
    }

    private static void ExecuteMultiple(XDocument models, Dictionary<char, int> palette) {
        Random random = new();
        foreach (XElement xmodel in models.Root.Elements("model"))
        {
            string name = xmodel.Get<string>("name");
            int linearSize = xmodel.Get("size", -1);
            int dimension = xmodel.Get("d", 2);
            int MX = xmodel.Get("length", linearSize);
            int MY = xmodel.Get("width", linearSize);
            int MZ = xmodel.Get("height", dimension == 2 ? 1 : linearSize);

            Console.Write($"{name} > ");
            string filename = $"models/{name}.xml";
            XDocument modeldoc;
            try { modeldoc = XDocument.Load(filename, LoadOptions.SetLineInfo); }
            catch (Exception)
            {
                Console.WriteLine($"ERROR: couldn't open xml file {filename}");
                continue;
            }

            Interpreter interpreter = Interpreter.Load(modeldoc.Root, MX, MY, MZ);
            if (interpreter == null)
            {
                Console.WriteLine("ERROR");
                continue;
            }

            int amount = xmodel.Get("amount", 2);
            int pixelsize = xmodel.Get("pixelsize", 4);
            string seedString = xmodel.Get<string>("seeds", null);
            int[] seeds = seedString?.Split(' ').Select(s => int.Parse(s)).ToArray();
            bool gif = xmodel.Get("gif", false);
            bool iso = xmodel.Get("iso", false);
            int steps = xmodel.Get("steps", gif ? 1000 : 50000);
            int gui = xmodel.Get("gui", 0);
            if (gif) amount = 1;

            for (int k = 0; k < amount; k++)
            {
                int seed = seeds != null && k < seeds.Length ? seeds[k] : random.Next();
                foreach ((byte[] result, char[] legend, int FX, int FY, int FZ) in interpreter.Run(seed, steps, gif))
                {
                    int[] colors = legend.Select(ch => palette[ch]).ToArray();
                    string outputname = gif ? $"output/{interpreter.counter}" : $"output/{name}_{seed}";
                    if (FZ == 1 || iso)
                    {
                        var (bitmap, WIDTH, HEIGHT) = Graphics.Render(result, FX, FY, FZ, colors, pixelsize, gui);
                        if (gui > 0) GUI.Draw(name, interpreter.root, interpreter.current, bitmap, WIDTH, HEIGHT, palette);
                        Graphics.SaveBitmap(bitmap, WIDTH, HEIGHT, outputname + ".png");
                    }
                    else VoxHelper.SaveVox(result, (byte)FX, (byte)FY, (byte)FZ, colors, outputname + ".vox");
                }
                Console.WriteLine("DONE");
            }
        }
    }

    private static void CreateOutputDirectory(string name) 
    {
        var folder = System.IO.Directory.CreateDirectory(name);
        foreach (var file in folder.GetFiles()) file.Delete();
    }
}
