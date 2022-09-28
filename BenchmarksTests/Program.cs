using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Xml.Linq;
using BenchmarkDotNet.Engines;

namespace com.clusterrr.Famicom.NesTiler.Benchmarks
{
    public class Benchmarks
    {
        static int Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmarks>();
            Console.WriteLine(summary);
            return 0;
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
            var prefix = Path.GetFileName(imagePath);
            var args = new string[] {
                "--enable-palettes", "0,1,2,3",
                "-i0", $"{imagePath}:0:64",
                "-i1", $"{imagePath}:64:64",
                "-i2", $"{imagePath}:128:64",
                "-i3", $"{imagePath}:192:48",
                "--out-pattern-table0", PatternTablePath(prefix, 0),
                "--out-pattern-table1", PatternTablePath(prefix, 1),
                "--out-pattern-table2", PatternTablePath(prefix, 2),
                "--out-pattern-table3", PatternTablePath(prefix, 3),
                "--out-name-table0", NameTablePath(prefix, 0),
                "--out-name-table1", NameTablePath(prefix, 1),
                "--out-name-table2", NameTablePath(prefix, 2),
                "--out-name-table3", NameTablePath(prefix, 3),
                "--out-attribute-table0", AttrTablePath(prefix, 0),
                "--out-attribute-table1", AttrTablePath(prefix, 1),
                "--out-attribute-table2", AttrTablePath(prefix, 2),
                "--out-attribute-table3", AttrTablePath(prefix, 3),
                "--out-palette0", PalettePath(prefix, 0),
                "--out-palette1", PalettePath(prefix, 1),
                "--out-palette2", PalettePath(prefix, 2),
                "--out-palette3", PalettePath(prefix, 3),
            };
            var r = Program.Main(args);
            if (r != 0) throw new InvalidOperationException($"Return code: {r}");
        }
    }
}