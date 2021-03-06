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
using ArenaNet.Medley.Collections.Concurrent;

namespace ArenaNet.Medley.Pool
{
    /// <summary>
    /// A pool of objects.
    /// </summary>
    public class ObjectPool<T>
    {
        public const int DefaultTrimPercentile = 65;
        public const int DefaultMinimumPoolSize = 10;

        public delegate T OnNewObjectDelegate();
        public delegate T OnUpdateObjectDelegate(T obj);
        public delegate T OnDestroyedObjectDelegate(T obj);

        /// <summary>
        /// Number of objects in pool
        /// </summary>
        public int ObjectsInPool { get { return availableObjects; } }

        /// <summary>
        /// Number of total objects
        /// </summary>
        public int TotalNumberOfObjects { get { return totalPoolSize; } }

        private readonly int trimPercentile;
        private readonly int minimumPoolSize;

        private ConcurrentLinkedQueue<PooledObject<T>> pool = new ConcurrentLinkedQueue<PooledObject<T>>();
        internal int availableObjects = 0;
        internal int totalPoolSize = 0;

        private OnNewObjectDelegate onNewObject;
        private OnUpdateObjectDelegate onUpdateObject;
        private OnDestroyedObjectDelegate onDestroyObject;

        /// <summary>
        /// Creates a pool with the given constructors, updators, and destructors.
        /// </summary>
        /// <param name="onNewObject"></param>
        /// <param name="onUpdateObject"></param>
        /// <param name="onDestroyObject"></param>
        public ObjectPool(OnNewObjectDelegate onNewObject, OnUpdateObjectDelegate onUpdateObject = null, OnDestroyedObjectDelegate onDestroyObject = null,
            int trimPercentile = DefaultTrimPercentile, int minimumPoolSize = DefaultMinimumPoolSize)
        {
            this.onNewObject = onNewObject;
            this.onUpdateObject = onUpdateObject;
            this.onDestroyObject = onDestroyObject;
            this.trimPercentile = trimPercentile;
            this.minimumPoolSize = minimumPoolSize;
        }

        /// <summary>
        /// Borrows an object from the pool.
        /// </summary>
        /// <returns></returns>
        public PooledObject<T> Borrow()
        {
            PooledObject<T> pooledObject = null;

            if (pool.Dequeue(out pooledObject))
            {
                Interlocked.Decrement(ref availableObjects);

                if (onUpdateObject != null)
                {
                    pooledObject.Value = onUpdateObject(pooledObject.Value);
                }
            }
            else
            {
                pooledObject = new PooledObject<T>(this, onNewObject());

                Interlocked.Increment(ref totalPoolSize);
            }

            pooledObject._state = (int)PooledObjectState.USED;

            return pooledObject;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="pooledObject"></param>
        public void Return(PooledObject<T> pooledObject)
        {
            if (pooledObject == null)
            {
                throw new ArgumentNullException("PooledObject cannot be null.");
            }

            if (pooledObject.Pool != this)
            {
                throw new ArgumentException("PooledObject does not belong to this pool.");
            }

            if (Interlocked.CompareExchange(ref pooledObject._state, (byte)PooledObjectState.NONE, (byte)PooledObjectState.USED) == (byte)PooledObjectState.USED)
            {
                int currentPercentile = (int)(((float)(availableObjects + 1) / (float)totalPoolSize) * 100f);

                if (onDestroyObject != null)
                {
                    pooledObject.Value = onDestroyObject(pooledObject.Value);
                }

                if (currentPercentile > trimPercentile || totalPoolSize <= minimumPoolSize)
                {
                    pooledObject._state = (int)PooledObjectState.POOLED;
                    pool.Enqueue(pooledObject);

                    Interlocked.Increment(ref availableObjects);
                }
                else
                {
                    pooledObject.Dispose();
                }
            }
            else
            {
                throw new ArgumentException("PooledObject is already pooled. State: " + pooledObject.State);
            }
        }

        /// <summary>
        /// Handles pooled object disposal.
        /// </summary>
        /// <param name="pooledObject"></param>
        internal void OnDisposed()
        {
            Interlocked.Decrement(ref totalPoolSize);
        }
    }
}
