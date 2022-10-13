using NUnit.Framework;

namespace com.clusterrr.Famicom.NesTiler.Benchmarks
{
    public class Tests
    {
        const string ImagesPath = "Images";
        const string ReferencesDir = "References";

        [Test]
        public void TestBelayaAkula()
        {
            var imagePath = Path.Combine(ImagesPath, "belaya_akula.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestBuhanka()
        {
            var imagePath = Path.Combine(ImagesPath, "buhanka.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestChernobyl()
        {
            var imagePath = Path.Combine(ImagesPath, "chernobyl.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestDira()
        {
            var imagePath = Path.Combine(ImagesPath, "dira.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestGlaza()
        {
            var imagePath = Path.Combine(ImagesPath, "glaza.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestGorgona()
        {
            var imagePath = Path.Combine(ImagesPath, "gorgona.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestMyatejl()
        {
            var imagePath = Path.Combine(ImagesPath, "myatej.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestPagoda()
        {
            var imagePath = Path.Combine(ImagesPath, "pagoda.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestRayon4()
        {
            var imagePath = Path.Combine(ImagesPath, "rayon4.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestShkola()
        {
            var imagePath = Path.Combine(ImagesPath, "shkola.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestSindikat()
        {
            var imagePath = Path.Combine(ImagesPath, "sindikat.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestSputnik()
        {
            var imagePath = Path.Combine(ImagesPath, "sputnik.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestSworm()
        {
            var imagePath = Path.Combine(ImagesPath, "sworm.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestTrailerPark()
        {
            var imagePath = Path.Combine(ImagesPath, "trailer-park.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestWarfaceLogo()
        {
            var imagePath = Path.Combine(ImagesPath, "warface_logo.gif");
            DoTestSplit4(imagePath);
        }

        [Test]
        public void TestZapravka()
        {
            var imagePath = Path.Combine(ImagesPath, "zapravka.gif");
            DoTestSplit4(imagePath);
        }

        private string PatternTablePath(string prefix, int number) => $"{prefix}_pattern_{number}.bin";
        private string NameTablePath(string prefix, int number) => $"{prefix}_name_table_{number}.bin";
        private string AttrTablePath(string prefix, int number) => $"{prefix}_attr_table_{number}.bin";
        private string PalettePath(string prefix, int number) => $"{prefix}_palette_{number}.bin";

        public void DoTestSplit4(string imagePath)
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

            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 0)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 0)))));
            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 1)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 1)))));
            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 2)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 2)))));
            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 3)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 3)))));

            Assert.That(File.ReadAllBytes(NameTablePath(prefix, 0)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, NameTablePath(prefix, 0)))));
            Assert.That(File.ReadAllBytes(NameTablePath(prefix, 1)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, NameTablePath(prefix, 1)))));
            Assert.That(File.ReadAllBytes(NameTablePath(prefix, 2)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, NameTablePath(prefix, 2)))));
            Assert.That(File.ReadAllBytes(NameTablePath(prefix, 3)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, NameTablePath(prefix, 3)))));

            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 0)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 0)))));
            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 1)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 1)))));
            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 2)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 2)))));
            Assert.That(File.ReadAllBytes(PatternTablePath(prefix, 3)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PatternTablePath(prefix, 3)))));

            Assert.That(File.ReadAllBytes(PalettePath(prefix, 0)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PalettePath(prefix, 0)))));
            Assert.That(File.ReadAllBytes(PalettePath(prefix, 1)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PalettePath(prefix, 1)))));
            Assert.That(File.ReadAllBytes(PalettePath(prefix, 2)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PalettePath(prefix, 2)))));
            Assert.That(File.ReadAllBytes(PalettePath(prefix, 3)), Is.EqualTo(File.ReadAllBytes(Path.Combine(ReferencesDir, PalettePath(prefix, 3)))));
        }
    }
}