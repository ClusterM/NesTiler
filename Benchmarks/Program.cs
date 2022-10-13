using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace com.clusterrr.Famicom.NesTiler.Benchmarks
{
    public class Benchmarks
    {
        static void Main()
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
            Console.WriteLine(summary);
        }

        [Benchmark]
        public void BenchmarkBelayaAkula()
        {
            var imagePath = @"Images\belaya_akula.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkBuhanka()
        {
            var imagePath = @"Images\buhanka.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkChernobyl()
        {
            var imagePath = @"Images\chernobyl.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkDira()
        {
            var imagePath = @"Images\dira.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkGlaza()
        {
            var imagePath = @"Images\glaza.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkGorgona()
        {
            var imagePath = @"Images\gorgona.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkMyatejl()
        {
            var imagePath = @"Images\myatej.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkPagoda()
        {
            var imagePath = @"Images\pagoda.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkRayon4()
        {
            var imagePath = @"Images\rayon4.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkShkola()
        {
            var imagePath = @"Images\shkola.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkSindikat()
        {
            var imagePath = @"Images\sindikat.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkSputnik()
        {
            var imagePath = @"Images\sputnik.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkSworm()
        {
            var imagePath = @"Images\sworm.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkTrailerPark()
        {
            var imagePath = @"Images\trailer-park.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkWarfaceLogo()
        {
            var imagePath = @"Images\warface_logo.gif";
            DoBenchmarkSplit4(imagePath);
        }

        [Benchmark]
        public void BenchmarkZapravka()
        {
            var imagePath = @"Images\zapravka.gif";
            DoBenchmarkSplit4(imagePath);
        }

        private string PatternTablePath(string prefix, int number) => $"{prefix}_pattern_{number}.bin";
        private string NameTablePath(string prefix, int number) => $"{prefix}_name_table_{number}.bin";
        private string AttrTablePath(string prefix, int number) => $"{prefix}_attr_table_{number}.bin";
        private string PalettePath(string prefix, int number) => $"{prefix}_palette_{number}.bin";

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