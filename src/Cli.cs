using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dungeons2SkinLoader
{
    static class Cli
    {
        // Dungeons2SkinLoader.exe --cli <outdir | game folder with --install> [--flat] [--nomesh] [--install]
        //     [--cape "Soul|cape.png"] "Hero|deluxe|png|mode|c.r c.r" ...
        public static int Run(string[] args)
        {
            var gd = App.LoadData();
            string outdir = args[1]; bool flat = args.Contains("--flat"), nomesh = args.Contains("--nomesh");
            var slots = new List<SkinSlot>();
            CapeSlot cape = null;
            for (int ai = 2; ai < args.Length; ai++)
            {
                var a = args[ai];
                if (a == "--cape")
                {
                    if (++ai >= args.Length) throw new ArgumentException("--cape needs CapeName|image.png (for example Soul|cape.png).");
                    var cp = args[ai].Split('|');
                    if (cp.Length != 2) throw new ArgumentException("--cape must be CapeName|image.png.");
                    cape = new CapeSlot { CapeKey = cp[0], ImagePath = cp[1] };
                    continue;
                }
                if (a.StartsWith("--")) continue;
                var p = a.Split('|');
                if (p.Length < 4) throw new ArgumentException("A skin entry must be Hero|deluxe|image.png|drawn|game|blink.");
                var s = new SkinSlot { HeroKey = p[0] + "|" + p[1], ImagePath = p[2] };
                s.Mode = p[3] == "game" ? FaceMode.Game : p[3] == "blink" ? FaceMode.Blink : FaceMode.Drawn;
                if (p.Length > 4 && p[4].Trim() != "")
                    s.Eyes = p[4].Split(' ').Select(e => int.Parse(e.Split('.')[0]) * 8 + int.Parse(e.Split('.')[1])).ToList();
                else if (s.Mode != FaceMode.Drawn)
                    s.Eyes = Converter.DetectEyes(Converter.To64(Img.FromFile(s.ImagePath)));
                slots.Add(s);
            }
            var files = ModBuilder.Build(gd, slots, !flat, Console.WriteLine, !nomesh, cape);
            if (args.Contains("--export"))      // <outdir> is a .zip for a friend (mod files + INSTALL/UNINSTALL scripts)
            {
                if (File.Exists(outdir)) File.Delete(outdir);
                using (var z = System.IO.Compression.ZipFile.Open(outdir, System.IO.Compression.ZipArchiveMode.Create))
                {
                    foreach (var kv in files) using (var st = z.CreateEntry(kv.Key).Open()) st.Write(kv.Value, 0, kv.Value.Length);
                    foreach (var kv in FriendScripts.Files(slots.Select(s => gd.FindHero(s.HeroKey).Display)))
                        using (var w = new StreamWriter(z.CreateEntry(kv.Key).Open())) w.Write(kv.Value);
                }
                Console.WriteLine("exported " + outdir);
                return 0;
            }
            if (args.Contains("--install"))     // <outdir> is a game folder: install there, replacing any earlier install
            {
                int removed = Game.Install(outdir, files, slots.Select(s => gd.FindHero(s.HeroKey).Display));
                Console.WriteLine("installed to " + Game.ModDir(outdir) + " (removed " + removed + " old files)");
                return 0;
            }
            Directory.CreateDirectory(outdir);
            foreach (var kv in files) File.WriteAllBytes(Path.Combine(outdir, kv.Key), kv.Value);
            Console.WriteLine("wrote " + outdir);
            return 0;
        }
    }
}
