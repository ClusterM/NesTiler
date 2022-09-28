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
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkBuhanka()
        {
            var imagePath = @"Images\buhanka.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkChernobyl()
        {
            var imagePath = @"Images\chernobyl.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkDira()
        {
            var imagePath = @"Images\dira.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkGlaza()
        {
            var imagePath = @"Images\glaza.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkGorgona()
        {
            var imagePath = @"Images\gorgona.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkMyatejl()
        {
            var imagePath = @"Images\myatej.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkPagoda()
        {
            var imagePath = @"Images\pagoda.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkRayon4()
        {
            var imagePath = @"Images\rayon4.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkShkola()
        {
            var imagePath = @"Images\shkola.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkSindikat()
        {
            var imagePath = @"Images\sindikat.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkSputnik()
        {
            var imagePath = @"Images\sputnik.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkSworm()
        {
            var imagePath = @"Images\sworm.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkTrailerPark()
        {
            var imagePath = @"Images\trailer-park.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkWarfaceLogo()
        {
            var imagePath = @"Images\warface_logo.gif";
            DoBenchmark(imagePath);
        }

        [Benchmark]
        public void BenchmarkZapravka()
        {
            var imagePath = @"Images\zapravka.gif";
            DoBenchmark(imagePath);
        }

        public void DoBenchmark(string imagePath)
        {
            var suffix = Path.GetFileNameWithoutExtension(imagePath);
            var args = new string[] {
                "--enable-palettes", "0,1,2,3",
                "-i0", $"{imagePath}:0:64",
                "-i1", $"{imagePath}:64:64",
                "-i2", $"{imagePath}:128:64",
                "-i3", $"{imagePath}:192:48",
                "--out-pattern-table0", $"{suffix}-pattern-table-0.bin",
                "--out-pattern-table1", $"{suffix}-pattern-table-1.bin",
                "--out-pattern-table2", $"{suffix}-pattern-table-2.bin",
                "--out-pattern-table3", $"{suffix}-pattern-table-3.bin",
                "--out-name-table0", $"{suffix}-name-table-0.bin",
                "--out-name-table1", $"{suffix}-name-table-1.bin",
                "--out-name-table2", $"{suffix}-name-table-2.bin",
                "--out-name-table3", $"{suffix}-name-table-3.bin",
                "--out-attribute-table0", $"{suffix}-attribute-table-0.bin",
                "--out-attribute-table1", $"{suffix}-attribute-table-1.bin",
                "--out-attribute-table2", $"{suffix}-attribute-table-2.bin",
                "--out-attribute-table3", $"{suffix}-attribute-table-3.bin",
                "--out-palette0", $"{suffix}-palette-0.bin",
                "--out-palette1", $"{suffix}-palette-1.bin",
                "--out-palette2", $"{suffix}-palette-2.bin",
                "--out-palette3", $"{suffix}-palette-3.bin"
            };
            var r = Program.Main(args);
            if (r != 0) throw new InvalidOperationException($"Return code: {r}");
        }
    }
}