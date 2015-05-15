using System;
using System.Threading;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArenaNet.Medley.Collections.Concurrent
{
    [TestClass]
    public class ConcurrentLinkedQueueTest
    {
        [TestMethod]
        public void TestSingleThreaded()
        {
            ConcurrentLinkedQueue<string> queue = new ConcurrentLinkedQueue<string>();
            queue.Enqueue("one");
            queue.Enqueue("two");
            queue.Enqueue("three");

            string value;
            Assert.IsTrue(queue.Dequeue(out value));
            Assert.AreEqual("one", value);
            Assert.IsTrue(queue.Dequeue(out value));
            Assert.AreEqual("two", value);
            Assert.IsTrue(queue.Dequeue(out value));
            Assert.AreEqual("three", value);
            Assert.IsFalse(queue.Dequeue(out value));
        }

        [TestMethod]
        public void TestMultiThreaded()
        {
            int numberOfThreadsComplete = 0;
            bool[] found = new bool[100];
            for (int i = 0; i < found.Length; i++)
            {
                found[0] = false;
            }

            ConcurrentLinkedQueue<string> queue = new ConcurrentLinkedQueue<string>();

            for (int i = 0; i < found.Length; i++)
            {
                ThreadPool.QueueUserWorkItem((object obj) => 
                {
                    int num = (int)obj;
                    queue.Enqueue("" + num);
                    found[num] = true;
                    Interlocked.Increment(ref numberOfThreadsComplete);
                }, i);
            }

            DateTime startTime = DateTime.Now;

            while (DateTime.Now.Subtract(startTime).TotalSeconds < 5 && numberOfThreadsComplete < found.Length)
            {
                // do nothing
            }

            for (int i = 0; i < found.Length; i++)
            {
                Assert.IsTrue(found[i]);
            }

            string value;

            while (queue.Dequeue(out value))
            {
                found[int.Parse(value)] = false;
            }

            for (int i = 0; i < found.Length; i++)
            {
                Assert.IsFalse(found[i]);
            }
        }

        [TestMethod]
        public void TestMultiThreadedComplex()
        {
            ConcurrentLinkedQueue<int> queue = new ConcurrentLinkedQueue<int>();

            int count = 0;

            BlockingCollection<int> pooledObjects = new BlockingCollection<int>();

            for (int i = 0; i < 100; i++)
            {
                Interlocked.Increment(ref count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        queue.Enqueue((int)state);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }
                }, count);

                Interlocked.Increment(ref count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        queue.Enqueue((int)state);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }
                }, count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        int val;

                        while (!queue.Dequeue(out val))
                        {
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }

                    Interlocked.Decrement(ref count);
                });

                Interlocked.Increment(ref count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        queue.Enqueue((int)state);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }
                }, count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        int val;

                        while (!queue.Dequeue(out val))
                        {
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }

                    Interlocked.Decrement(ref count);
                });

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        int val;

                        while (!queue.Dequeue(out val))
                        {
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }

                    Interlocked.Decrement(ref count);
                });
            }

            DateTime startTime = DateTime.Now;

            while (count > 0 && DateTime.Now.Subtract(startTime).TotalSeconds < 10)
            {
                Console.WriteLine("Current count: " + count);

                Thread.Sleep(1);
            }

            Assert.AreEqual(0, count);
            Assert.AreEqual(0, queue.Count);
            //Assert.AreEqual(pool.TotalNumberOfObjects, pool.ObjectsInPool);
        }
    }
}
