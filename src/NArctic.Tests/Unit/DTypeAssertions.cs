using MongoDB.Driver;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NArctic.Tests.Unit
{
    [TestFixture]
    public class DTypeAssertions
    {
        [Test]
        public static void ParseLEFloat()
        {
            var cur = new DType();
            new DTypeParser().Parse("'<f8'", 0, cur);
            TestFloat(cur, EndianType.Little);
        }

        [Test]
        public static void ParseBool()
        {
            var cur = new DType();
            new DTypeParser().Parse("'?'", 0, cur);
            TestBool(cur);
        }

        [Test]
        public static void ParseLEInt()
        {
            var cur = new DType();
            new DTypeParser().Parse("'<i4'", 0, cur);
            TestInt(cur, EndianType.Little);
        }

        [Test]
        public static void ParseLELong()
        {
            var cur = new DType();
            new DTypeParser().Parse("'<i8'", 0, cur);
            TestLong(cur, EndianType.Little);
        }

        [Test]
        public static void ParseLEDateTime()
        {
            var cur = new DType();
            new DTypeParser().Parse("'<M8[ns]'", 0, cur);
            TestDateTime(cur, EndianType.Little);
        }

        [Test]
        public static void ParseNEString()
        {
            var cur = new DType();
            new DTypeParser().Parse("'S32'", 0, cur);
            TestString(cur, EndianType.Native, Encoding.UTF8, 32);
        }

        [Test]
        public static void ParseLEUnicode()
        {
            var cur = new DType();
            new DTypeParser().Parse("'<U32'", 0, cur);
            TestString(cur, EndianType.Little, Encoding.UTF32, 32 * 4);
        }

        [Test]
        public static void ParseBEUnicode()
        {
            var cur = new DType();
            new DTypeParser().Parse("'<U42'", 0, cur);
            TestString(cur, EndianType.Little, Encoding.UTF32, 42 * 4);
        }

        private static void TestFloat(DType cur, EndianType endian)
        {
            Assert.IsNotNull(cur);
            Assert.AreEqual(typeof(double), cur.Type);
            Assert.AreEqual(endian, cur.Endian);
            Assert.AreEqual(8, cur.Size);
        }

        private static void TestBool(DType cur)
        {
            Assert.IsNotNull(cur);
            Assert.AreEqual(typeof(bool), cur.Type);
            Assert.AreEqual(1, cur.Size);
        }

        private static void TestInt(DType cur, EndianType endian)
        {
            Assert.IsNotNull(cur);
            Assert.AreEqual(typeof(int), cur.Type);
            Assert.AreEqual(endian, cur.Endian);
            Assert.AreEqual(4, cur.Size);
        }

        private static void TestLong(DType cur, EndianType endian)
        {
            Assert.IsNotNull(cur);
            Assert.AreEqual(typeof(Int64), cur.Type);
            Assert.AreEqual(endian, cur.Endian);
            Assert.AreEqual(8, cur.Size);
        }

        private static void TestDateTime(DType cur, EndianType endian)
        {
            Assert.IsNotNull(cur);
            Assert.AreEqual(typeof(DateTime), cur.Type);
            Assert.AreEqual(endian, cur.Endian);
            Assert.AreEqual(8, cur.Size);
        }

        private static void TestString(DType cur, EndianType endian, Encoding encoding, int size)
        {
            Assert.IsNotNull(cur);
            Assert.AreEqual(typeof(string), cur.Type);
            Assert.AreEqual(endian, cur.Endian);
            Assert.AreEqual(encoding, cur.EncodingStyle);
            Assert.AreEqual(size, cur.Size);
        }
    }
}
