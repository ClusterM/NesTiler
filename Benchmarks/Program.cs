using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace com.clusterrr.Famicom.NesTiler.Benchmarks
{
    public class Benchmarks
    {
        const string ImagesPath = "Images";

        static void Main()
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
            Console.WriteLine(summary);
        }

        [Benchmark]
        public void BenchmarkBelayaAkula()
        {
            var imagePath = Path.Combine(ImagesPath, "belaya_akula.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkBuhanka()
        {
            var imagePath = Path.Combine(ImagesPath, "buhanka.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkChernobyl()
        {
            var imagePath = Path.Combine(ImagesPath, "chernobyl.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkDira()
        {
            var imagePath = Path.Combine(ImagesPath, "dira.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkGlaza()
        {
            var imagePath = Path.Combine(ImagesPath, "glaza.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkGorgona()
        {
            var imagePath = Path.Combine(ImagesPath, "gorgona.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkMyatejl()
        {
            var imagePath = Path.Combine(ImagesPath, "myatej.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkPagoda()
        {
            var imagePath = Path.Combine(ImagesPath, "pagoda.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkRayon4()
        {
            var imagePath = Path.Combine(ImagesPath, "rayon4.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkShkola()
        {
            var imagePath = Path.Combine(ImagesPath, "shkola.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkSindikat()
        {
            var imagePath = Path.Combine(ImagesPath, "sindikat.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkSputnik()
        {
            var imagePath = Path.Combine(ImagesPath, "sputnik.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkSworm()
        {
            var imagePath = Path.Combine(ImagesPath, "sworm.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkTrailerPark()
        {
            var imagePath = Path.Combine(ImagesPath, "trailer-park.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkWarfaceLogo()
        {
            var imagePath = Path.Combine(ImagesPath, "warface_logo.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkZapravka()
        {
            var imagePath = Path.Combine(ImagesPath, "zapravka.gif");
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkJurassic()
        {
            var imagePath = Path.Combine(ImagesPath, "jurassic.png");
            DoBenchmarkSplit2(imagePath);
        }

        [Benchmark]
        public void BenchmarkJurassic2()
        {
            var imagePath = Path.Combine(ImagesPath, "jurassic2.png");
            DoBenchmarkSplit2(imagePath);
        }

        [Benchmark]
        public void BenchmarkBlasterMasterLeft()
        {
            var imagePath = Path.Combine(ImagesPath, "blaster_master_left.png");
            DoBenchmarkNoSplit(imagePath, "#000000");
        }

        [Benchmark]
        public void BenchmarkBlasterMasterRight()
        {
            var imagePath = Path.Combine(ImagesPath, "blaster_master_right.png");
            DoBenchmarkNoSplit(imagePath, "#000000");
        }

        [Benchmark]
        public void BenchmarkBlasterMasterRightFull()
        {
            var imagePath = Path.Combine(ImagesPath, "blaster_master_right_full.png");
            DoBenchmarkNoSplit(imagePath, "#000000");
        }

        private string PatternTablePath(string prefix, int number) => $"{prefix}_pattern_{number}.bin";
        private string NameTablePath(string prefix, int number) => $"{prefix}_name_table_{number}.bin";
        private string AttrTablePath(string prefix, int number) => $"{prefix}_attr_table_{number}.bin";
        private string PalettePath(string prefix, int number) => $"{prefix}_palette_{number}.bin";

        public void DoBenchmarkNoSplit(string imagePath, string bgColor = "auto")
        {
            var prefix = Path.GetFileNameWithoutExtension(imagePath);
            var args = new string[] {
                "--enable-palettes", "0,1,2,3",
                "-input-0", $"{imagePath}",
                "--out-pattern-table-0", PatternTablePath(prefix, 0),
                "--out-name-table-0", NameTablePath(prefix, 0),
                "--out-attribute-table-0", AttrTablePath(prefix, 0),
                "--out-palette-0", PalettePath(prefix, 0),
                "--out-palette-1", PalettePath(prefix, 1),
                "--out-palette-2", PalettePath(prefix, 2),
                "--out-palette-3", PalettePath(prefix, 3),
                "--bg-color", bgColor,
            };
            var r = Program.Main(args);
            if (r != 0) throw new InvalidOperationException($"Return code: {r}");

            //foreach (var file in Directory.GetFiles(".", "*.bin")) File.Copy(file, Path.Join(@"E:\bins", Path.GetFileName(file)), true);
        }

        public void DoBenchmarkSplit2(string imagePath)
        {
            var prefix = Path.GetFileNameWithoutExtension(imagePath);
            var args = new string[] {
                "--enable-palettes", "0,1,2,3",
                "-input-0", $"{imagePath}:0:128",
                "-input-1", $"{imagePath}:128:112",
                "--out-pattern-table-0", PatternTablePath(prefix, 0),
                "--out-pattern-table-1", PatternTablePath(prefix, 1),
                "--out-name-table-0", NameTablePath(prefix, 0),
                "--out-name-table-1", NameTablePath(prefix, 1),
                "--out-attribute-table-0", AttrTablePath(prefix, 0),
                "--out-attribute-table-1", AttrTablePath(prefix, 1),
                "--out-palette-0", PalettePath(prefix, 0),
                "--out-palette-1", PalettePath(prefix, 1),
                "--out-palette-2", PalettePath(prefix, 2),
                "--out-palette-3", PalettePath(prefix, 3),
            };
            var r = Program.Main(args);
            if (r != 0) throw new InvalidOperationException($"Return code: {r}");

            //foreach (var file in Directory.GetFiles(".", "*.bin")) File.Copy(file, Path.Join(@"E:\bins", Path.GetFileName(file)), true);
        }
        
        public void DoBenchmarkSplit4(string imagePath)
        {
            var prefix = Path.GetFileNameWithoutExtension(imagePath);
            var args = new string[] {
                "--enable-palettes", "0,1,2,3",
                "-input-0", $"{imagePath}:0:64",
                "-input-1", $"{imagePath}:64:64",
                "-input-2", $"{imagePath}:128:64",
                "-input-3", $"{imagePath}:192:48",
                "--out-pattern-table-0", PatternTablePath(prefix, 0),
                "--out-pattern-table-1", PatternTablePath(prefix, 1),
                "--out-pattern-table-2", PatternTablePath(prefix, 2),
                "--out-pattern-table-3", PatternTablePath(prefix, 3),
                "--out-name-table-0", NameTablePath(prefix, 0),
                "--out-name-table-1", NameTablePath(prefix, 1),
                "--out-name-table-2", NameTablePath(prefix, 2),
                "--out-name-table-3", NameTablePath(prefix, 3),
                "--out-attribute-table-0", AttrTablePath(prefix, 0),
                "--out-attribute-table-1", AttrTablePath(prefix, 1),
                "--out-attribute-table-2", AttrTablePath(prefix, 2),
                "--out-attribute-table-3", AttrTablePath(prefix, 3),
                "--out-palette-0", PalettePath(prefix, 0),
                "--out-palette-1", PalettePath(prefix, 1),
                "--out-palette-2", PalettePath(prefix, 2),
                "--out-palette-3", PalettePath(prefix, 3),
            };
            var r = Program.Main(args);
            if (r != 0) throw new InvalidOperationException($"Return code: {r}");

            //foreach (var file in Directory.GetFiles(".", "*.bin")) File.Copy(file, Path.Join(@"E:\bins", Path.GetFileName(file)), true);
        }

    }
}