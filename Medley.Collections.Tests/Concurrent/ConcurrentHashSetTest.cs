using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArenaNet.Medley.Collections.Concurrent
{
    [TestClass]
    public class ConcurrentHashSetTest
    {
        [TestMethod]
        public void TestAdd()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.Ordinal);
            set.Add("one");
            set.Add("two");
            set.Add("four");

            Assert.AreEqual(3, set.Count);

            Assert.IsTrue(set.Contains("one"));
            Assert.IsTrue(set.Contains("two"));
            Assert.IsFalse(set.Contains("three"));
            Assert.IsTrue(set.Contains("four"));
        }

        [TestMethod]
        public void TestAddIgnoreCaseComparer()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add("OnE");
            set.Add("ThrEE");
            set.Add("FouR");

            Assert.AreEqual(3, set.Count);

            Assert.IsTrue(set.Contains("one"));
            Assert.IsFalse(set.Contains("two"));
            Assert.IsTrue(set.Contains("tHRee"));
            Assert.IsTrue(set.Contains("four"));
        }

        [TestMethod]
        public void TestRemove()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.Ordinal);
            set.Add("one");
            set.Add("two");
            set.Add("three");
            set.Add("four");

            Assert.AreEqual(4, set.Count);

            Assert.IsTrue(set.Contains("one"));
            Assert.IsTrue(set.Contains("two"));
            Assert.IsTrue(set.Contains("three"));
            Assert.IsTrue(set.Contains("four"));
            
            set.Remove("two");
            set.Remove("three");

            Assert.AreEqual(2, set.Count);

            Assert.IsTrue(set.Contains("one"));
            Assert.IsFalse(set.Contains("two"));
            Assert.IsFalse(set.Contains("three"));
            Assert.IsTrue(set.Contains("four"));
        }


        [TestMethod]
        public void TestRemoveIgnoreCaseComparer()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add("OnE");
            set.Add("tWo");
            set.Add("thREE");
            set.Add("FOur");

            Assert.AreEqual(4, set.Count);

            Assert.IsTrue(set.Contains("one"));
            Assert.IsTrue(set.Contains("two"));
            Assert.IsTrue(set.Contains("three"));
            Assert.IsTrue(set.Contains("four"));

            set.Remove("two");
            set.Remove("thRee");

            Assert.AreEqual(2, set.Count);

            Assert.IsTrue(set.Contains("one"));
            Assert.IsFalse(set.Contains("two"));
            Assert.IsFalse(set.Contains("three"));
            Assert.IsTrue(set.Contains("four"));
        }
    }
}
