/*
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

namespace ArenaNet.Medley.Pool
{
    [TestClass]
    public class ObjectPoolTest
    {
        [TestMethod]
        public void TestMultiThreaded()
        {
            int poolCount = 0;

            int count = 0;

            ObjectPool<int> pool = new ObjectPool<int>(() => { return Interlocked.Increment(ref poolCount) - 1; });

            ConcurrentQueue<PooledObject<int>> pooledObjects = new ConcurrentQueue<PooledObject<int>>();

            for (int i = 0; i < 100000; i++)
            {
                Interlocked.Increment(ref count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        pooledObjects.Enqueue(pool.Borrow());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }
                });

                Interlocked.Increment(ref count);

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        pooledObjects.Enqueue(pool.Borrow());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }
                });

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        PooledObject<int> pooledObject;

                        while (!pooledObjects.TryDequeue(out pooledObject))
                        {

                        }

                        pooledObject.Return();
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
                        pooledObjects.Enqueue(pool.Borrow());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e);
                    }
                });

                ThreadPool.QueueUserWorkItem((object state) =>
                {
                    try
                    {
                        PooledObject<int> pooledObject;

                        while (!pooledObjects.TryDequeue(out pooledObject))
                        {

                        }

                        pooledObject.Return();
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
                        PooledObject<int> pooledObject;

                        while (!pooledObjects.TryDequeue(out pooledObject))
                        {

                        }

                        pooledObject.Return();
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

                Console.WriteLine("Total: " + pool.TotalNumberOfObjects + ", In Pool: " + pool.ObjectsInPool);

                Thread.Sleep(1);
            }

            Assert.AreEqual(0, count);
            Assert.AreEqual(pool.TotalNumberOfObjects, pool.ObjectsInPool);
        }

        [TestMethod]
        public void TestGarbageCollectedPooledObject()
        {
            ObjectPool<string> pool = new ObjectPool<string>(() => { return "hello"; });

            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    PooledObject<string> pooledObject = pool.Borrow();

                    Assert.AreEqual("hello", pooledObject.Value);
                    Assert.AreEqual(0, pool.ObjectsInPool);
                    Assert.AreEqual(j + 1, pool.TotalNumberOfObjects);

                    pooledObject = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Assert.AreEqual(0, pool.ObjectsInPool);
                Assert.AreEqual(0, pool.TotalNumberOfObjects);
            }
        }

        [TestMethod]
        public void TestManuallyDisposedPooledObject()
        {
            ObjectPool<string> pool = new ObjectPool<string>(() => { return "hello"; });

            for (int i = 0; i < 100; i++)
            {
                List<PooledObject<string>> objects = new List<PooledObject<string>>();

                for (int j = 0; j < 10; j++)
                {
                    PooledObject<string> pooledObject = pool.Borrow();

                    Assert.AreEqual("hello", pooledObject.Value);
                    Assert.AreEqual(0, pool.ObjectsInPool);
                    Assert.AreEqual(j + 1, pool.TotalNumberOfObjects);

                    objects.Add(pooledObject);
                }

                for (int j = 0; j < objects.Count; j++)
                {
                    objects[j].Dispose();
                }

                Assert.AreEqual(0, pool.ObjectsInPool);
                Assert.AreEqual(0, pool.TotalNumberOfObjects);
            }
        }

        [TestMethod]
        public void TestBorrow()
        {
            ObjectPool<string> pool = new ObjectPool<string>(() => { return "hello"; });
            Assert.AreEqual(0, pool.ObjectsInPool);
            Assert.AreEqual(0, pool.TotalNumberOfObjects);

            PooledObject<string> pooledObj1 = pool.Borrow();
            PooledObject<string> pooledObj2 = pool.Borrow();

            Assert.AreEqual(pool, pooledObj1.Pool);
            Assert.AreEqual("hello", pooledObj1.Value);
            Assert.AreEqual(pool, pooledObj2.Pool);
            Assert.AreEqual("hello", pooledObj2.Value);

            Assert.AreEqual(0, pool.ObjectsInPool);
            Assert.AreEqual(2, pool.TotalNumberOfObjects);
        }

        [TestMethod]
        public void TestBorrowAndReturn()
        {
            ObjectPool<string> pool = new ObjectPool<string>(() => { return "hello"; });
            Assert.AreEqual(0, pool.ObjectsInPool);
            Assert.AreEqual(0, pool.TotalNumberOfObjects);

            PooledObject<string> pooledObj1 = pool.Borrow();
            PooledObject<string> pooledObj2 = pool.Borrow();

            Assert.AreEqual(pool, pooledObj1.Pool);
            Assert.AreEqual("hello", pooledObj1.Value);
            Assert.AreEqual(pool, pooledObj2.Pool);
            Assert.AreEqual("hello", pooledObj2.Value);

            pool.Return(pooledObj2);

            Assert.AreEqual(1, pool.ObjectsInPool);
            Assert.AreEqual(2, pool.TotalNumberOfObjects);
        }

        [TestMethod]
        public void TestBorrowAndPooledObjectReturn()
        {
            int counter = 0;

            ObjectPool<string> pool = new ObjectPool<string>(() => { return "hello" + (++counter); });
            Assert.AreEqual(0, pool.ObjectsInPool);
            Assert.AreEqual(0, pool.TotalNumberOfObjects);

            PooledObject<string> pooledObj1 = pool.Borrow();
            PooledObject<string> pooledObj2 = pool.Borrow();

            Assert.AreEqual(pool, pooledObj1.Pool);
            Assert.AreEqual("hello1", pooledObj1.Value);
            Assert.AreEqual(pool, pooledObj2.Pool);
            Assert.AreEqual("hello2", pooledObj2.Value);

            pooledObj1.Return();

            Assert.AreEqual(1, pool.ObjectsInPool);
            Assert.AreEqual(2, pool.TotalNumberOfObjects);

            pooledObj2.Return();

            Assert.AreEqual(2, pool.ObjectsInPool);
            Assert.AreEqual(2, pool.TotalNumberOfObjects);
        }

        [TestMethod]
        public void TestBorrowAndReturnAndBorrow()
        {
            ObjectPool<string> pool = new ObjectPool<string>(() => { return "hello"; });
            Assert.AreEqual(0, pool.ObjectsInPool);
            Assert.AreEqual(0, pool.TotalNumberOfObjects);

            PooledObject<string> pooledObj1 = pool.Borrow();
            PooledObject<string> pooledObj2 = pool.Borrow();

            Assert.AreEqual(pool, pooledObj1.Pool);
            Assert.AreEqual("hello", pooledObj1.Value);
            Assert.AreEqual(pool, pooledObj2.Pool);
            Assert.AreEqual("hello", pooledObj2.Value);

            pooledObj1.Return();

            Assert.AreEqual(1, pool.ObjectsInPool);
            Assert.AreEqual(2, pool.TotalNumberOfObjects);

            PooledObject<string> pooledObj1Again = pool.Borrow();

            Assert.AreEqual(pooledObj1, pooledObj1Again);

            Assert.AreEqual(0, pool.ObjectsInPool);
            Assert.AreEqual(2, pool.TotalNumberOfObjects);
        }
    }
}
