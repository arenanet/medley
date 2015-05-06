using System;
using System.Threading;
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
    }
}
