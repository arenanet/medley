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
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ArenaNet.Medley.Pool
{
    /// <summary>
    /// Defines the current state of a pooled object.
    /// </summary>
    public enum PooledObjectState : int
    {
        NONE,
        POOLED,
        USED,
        DISPOSED
    }

    /// <summary>
    /// A pooled object.
    /// </summary>
    /// <typeparam name="T">the type of the object</typeparam>
    public class PooledObject<T> : IDisposable
    {
        /// <summary>
        /// The pool where this object lives.
        /// </summary>
        public ObjectPool<T> Pool { get; internal set; }

        /// <summary>
        /// The value of this pooled object.
        /// </summary>
        internal T _value;
        public T Value
        {
            set
            {
                _value = value;
            }
            get
            {
                if (State != PooledObjectState.USED)
                {
                    throw new InvalidOperationException("This pooled object is not currently used. Its state is: " + State);
                }

                return _value;
            }
        }

        /// <summary>
        /// This value is owned by the Pool - not this object
        /// </summary>
        internal int _state = (int)PooledObjectState.NONE;
        public PooledObjectState State 
        {
            set
            {
                _state = (int)value;
            }
            get
            {
                return (PooledObjectState)_state;
            }
        }

        /// <summary>
        /// Creates a new PooledObject with the given pool and value.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="value"></param>
        internal PooledObject(ObjectPool<T> pool, T value)
        {
            this.Pool = pool;
            this.Value = value;
        }

        /// <summary>
        /// Returns this object to the pool.
        /// </summary>
        public void Return()
        {
            if (Pool == null)
            {
                throw new InvalidOperationException("This pooled object is not attached to a pool - it may have been evicted.");
            }

            if (State != PooledObjectState.USED)
            {
                throw new InvalidOperationException("This pooled object is not currently used. Its state is: " + State);
            }

            Pool.Return(this);
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~PooledObject()
        {
            DisposeInternal();
        }

        /// <summary>
        /// Closes this buffer.
        /// </summary>
        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void DisposeInternal()
        {
            if (_state == (int)PooledObjectState.DISPOSED)
                return;

            _state = (int)PooledObjectState.DISPOSED;

            Pool.OnDisposed();
            Pool = null;
        }
    }
}
