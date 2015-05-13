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
using System.Collections;
using System.Collections.Generic;

namespace ArenaNet.Medley.Collections.Concurrent
{
    /// <summary>
    /// A concurrent hashset implementation built on top of the concurrent hashmap.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentHashSet<T> : ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        private readonly ConcurrentHashMap<T, bool> dictionary;

        /// <summary>
        /// Returns the number of items in this hashset.
        /// </summary>
        public int Count
        {
            get { return this.dictionary.Count; }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return this.dictionary.IsReadOnly; }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Always returns a new object.
        /// </summary>
        public object SyncRoot
        {
            get { return new object(); }
        }

        /// <summary>
        /// Creates a new concurrent hashset.
        /// </summary>
        /// <param name="comparer"></param>
        /// <param name="numberOfBuckets"></param>
        /// <param name="numberOfStripes"></param>
        public ConcurrentHashSet(IEqualityComparer<T> comparer = null, uint numberOfBuckets = 128, uint numberOfStripes = 16)
        {
            this.dictionary = new ConcurrentHashMap<T, bool>(comparer, numberOfBuckets, numberOfStripes);
        }

        /// <summary>
        /// Adds the given item to this set and returns true if this value wasn't set before.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryAdd(T item)
        {
            return this.dictionary.Put(item, true, false);
        }

        /// <summary>
        /// Add a new item into this hashset.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            this.dictionary[item] = true;
        }

        /// <summary>
        /// Removes the given item from the hashset.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            return this.dictionary.Remove(item);
        }

        /// <summary>
        /// Clears all items from the hashset.
        /// </summary>
        public void Clear()
        {
            this.dictionary.Clear();
        }

        /// <summary>
        /// Returns true if thie hashset contains the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return this.dictionary.ContainsKey(item);
        }

        /// <summary>
        /// Copies the values of this hashset into the given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.dictionary.KeyList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the contents of this hash set to the given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(Array array, int index)
        {
            this.dictionary.KeyList.CopyTo((T[])array, index);
        }

        /// <summary>
        /// Gets an enumerator for this hashset.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return this.dictionary.Keys.GetEnumerator();
        }

        /// <summary>
        /// Gets an enumerator for this hashset.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.dictionary.Keys.GetEnumerator();
        }
    }
}
