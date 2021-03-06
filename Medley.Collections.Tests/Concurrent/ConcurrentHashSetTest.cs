﻿/*
 * Copyright 2015 ArenaNet, LLC.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this 
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * 	 http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under 
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF 
 * ANY KIND, either express or implied. See the License for the specific language governing 
 * permissions and limitations under the License.
 */
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        public void TestTryAdd()
        {
            ConcurrentHashSet<string> set = new ConcurrentHashSet<string>(StringComparer.Ordinal);
            Assert.IsTrue(set.TryAdd("one"));
            Assert.IsTrue(set.TryAdd("two"));
            Assert.IsFalse(set.TryAdd("two"));
            Assert.IsTrue(set.TryAdd("three"));

            Assert.AreEqual(3, set.Count);
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

        [TestMethod]
        public void TestSingleThreaded()
        {
            TestCollectionWithSingleThread(new ConcurrentHashSet<string>(), 10000);
        }

        [TestMethod]
        public void TestMultiThreaded()
        {
            TestCollectionWithMultipleThreads(new ConcurrentHashSet<string>(), 10000);
        }

        private static void TestCollectionWithSingleThread(ICollection<string> collection, int size)
        {
            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();

            ///>- PUT -<///
            DateTime startTime = DateTime.Now;

            for (int i = 0; i < size; i++)
            {
                collection.Add("testValue" + i);
            }

            TimeSpan timeTook = DateTime.Now.Subtract(startTime);

            Console.WriteLine(collection.GetType().Name + ":\t" + size + " puts took: {0:g}", timeTook);

            Assert.AreEqual(size, collection.Count);

            string[] getValues = new string[size];

            ///>- CONTAINS -<///
            startTime = DateTime.Now;

            for (int i = 0; i < size; i++)
            {
                if (collection.Contains("testValue" + i))
                {
                    getValues[i] = "testValue" + i;
                }
            }

            timeTook = DateTime.Now.Subtract(startTime);

            Console.WriteLine(collection.GetType().Name + ":\t" + size + " gets took: {0:g}", timeTook);

            Assert.AreEqual(size, collection.Count);

            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual("testValue" + i, getValues[i]);
            }

            ///>- REMOVE -<///
            startTime = DateTime.Now;

            for (int i = 0; i < size; i++)
            {
                collection.Remove("testValue" + i);
            }

            timeTook = DateTime.Now.Subtract(startTime);

            Console.WriteLine(collection.GetType().Name + ":\t" + size + " removes took: {0:g}", timeTook);

            Assert.AreEqual(0, collection.Count);

            Console.WriteLine();
        }

        private static void TestCollectionWithMultipleThreads(ICollection<string> collection, int size)
        {
            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();

            ///>- PUT -<///
            DateTime startTime = DateTime.Now;

            int completedCount = 0;

            for (int i = 0; i < size; i++)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    collection.Add("testValue" + (int)state);
                    Interlocked.Increment(ref completedCount);
                }, i);
            }

            TimeSpan timeTook;

            while ((timeTook = DateTime.Now.Subtract(startTime)).Seconds < 5 && completedCount < size)
            {
                // do stuff
            }

            Assert.AreEqual(size, collection.Count);

            Console.WriteLine(collection.GetType().Name + ":\t" + size + " puts took: {0:g}", timeTook);

            string[] getValues = new string[size];

            ///>- CONTAINS -<///
            completedCount = 0;
            startTime = DateTime.Now;

            for (int i = 0; i < size; i++)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    string value = "testValue" + (int)state;
                    if (collection.Contains(value))
                    {
                        getValues[(int)state] = value;
                    }
                    Interlocked.Increment(ref completedCount);
                }, i);
            }

            while ((timeTook = DateTime.Now.Subtract(startTime)).Seconds < 5 && completedCount < size)
            {
                // do stuff
            }

            Console.WriteLine(collection.GetType().Name + ":\t" + size + " gets took: {0:g}", timeTook);

            Assert.AreEqual(size, collection.Count);

            for (int i = 0; i < size; i++)
            {
                Assert.AreEqual("testValue" + i, getValues[i]);
            }

            ///>- REMOVE -<///
            completedCount = 0;
            startTime = DateTime.Now;

            for (int i = 0; i < size; i++)
            {
                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    collection.Remove("testValue" + (int)state);
                    Interlocked.Increment(ref completedCount);
                }, i);
            }

            while ((timeTook = DateTime.Now.Subtract(startTime)).Seconds < 5 && completedCount < size)
            {
                // do stuff
            }

            Console.WriteLine(collection.GetType().Name + ":\t" + size + " removes took: {0:g}", timeTook);

            Assert.AreEqual(0, collection.Count);

            Console.WriteLine();
        }
    }
}
