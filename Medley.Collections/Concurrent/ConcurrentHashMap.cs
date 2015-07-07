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
using System.Threading;

namespace ArenaNet.Medley.Collections.Concurrent
{
    /// <summary>
    /// A concurrent hashmap. It uses striping to lock buckets.
    /// </summary>
    /// <typeparam name="K">the key type</typeparam>
    /// <typeparam name="V">the value type</typeparam>
    public class ConcurrentHashMap<K, V> : IDictionary<K, V>, IEnumerable<KeyValuePair<K, V>>, ICollection<KeyValuePair<K, V>>, IDictionary, IEnumerable, ICollection
    {
        #region Defaults
        public static readonly IEqualityComparer<K> DefaultComparer = EqualityComparer<K>.Default;
        public static readonly uint DefaultLookUpTableSize = 128;
        public static readonly uint DefaultLockCount = 16;
        #endregion Defaults

        private readonly IEqualityComparer<K> comparer;

        private Node[] buckets;
        private object[] locks;
        private int count;

        /// <summary>
        /// Returns the count (number of keyvaluepairs) in this hash map.
        /// </summary>
        public int Count
        {
            get { return count; }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Always returns false.
        /// </summary>
        public bool IsFixedSize
        {
            get { return false; }
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
        /// Create a concurrent hashmap that stripes access to the undelying buckets.
        /// </summary>
        /// <param name="numberOfBuckets"></param>
        /// <param name="numberOfStripes"></param>
        public ConcurrentHashMap(IEqualityComparer<K> comparer = null, uint numberOfBuckets = 128, uint numberOfStripes = 16)
        {
            if (!IsPowerOfTwo(numberOfBuckets))
            {
                throw new ArgumentException("'lookupTableSize' needs to be a power of two.");
            }

            if (!IsPowerOfTwo(numberOfStripes))
            {
                throw new ArgumentException("''lockCount' needs to be a power of two.");
            }

            if (comparer == null)
            {
                this.comparer = EqualityComparer<K>.Default;
            }
            else
            {
                this.comparer = comparer;
            }

            this.buckets = new Node[numberOfBuckets];
            this.locks = new object[numberOfStripes];
            for (int i = 0; i < locks.Length; i++)
            {
                locks[i] = new object();
            }

            this.count = 0;
        }

        /// <summary>
        /// Returns all the keys in this hash map.
        /// </summary>
        ICollection IDictionary.Keys
        {
            get
            {
                return KeyList;
            }
        }

        /// <summary>
        /// Returns all the keys in this hash map.
        /// </summary>
        public ICollection<K> Keys
        {
            get
            {
                return KeyList;
            }
        }

        /// <summary>
        /// A list of keys.
        /// </summary>
        internal List<K> KeyList
        {
            get
            {
                List<K> keys = new List<K>();

                for (int i = 0; i < buckets.Length; i++)
                {
                    Node currentNode = buckets[i];

                    while (currentNode != null)
                    {
                        keys.Add(currentNode.kvp.Key);
                        currentNode = currentNode.next;
                    }
                }

                return keys;
            }
        }

        /// <summary>
        /// Returns all the values in this hash map.
        /// </summary>
        ICollection IDictionary.Values
        {
            get
            {
                return ValueList;
            }
        }

        /// <summary>
        /// Returns are the values in this hash map.
        /// </summary>
        public ICollection<V> Values
        {
            get
            {
                return ValueList;
            }
        }

        /// <summary>
        /// A list of values.
        /// </summary>
        internal List<V> ValueList
        {
            get
            {
                List<V> values = new List<V>();

                for (int i = 0; i < buckets.Length; i++)
                {
                    Node currentNode = buckets[i];

                    while (currentNode != null)
                    {
                        values.Add(currentNode.kvp.Value);
                        currentNode = currentNode.next;
                    }
                }

                return values;
            }
        }

        /// <summary>
        /// Array accessors for this hash map.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[object key]
        {
            get
            {
                return this[(K)key];
            }
            set
            {
                this[(K)key] = (V)value;
            }
        }

        /// <summary>
        /// Arrays accessors for this hash map.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V this[K key]
        {
            get
            {
                V response;

                if (TryGetValue(key, out response))
                {
                    return response;
                }
                else
                {
                    throw new KeyNotFoundException("No value found for key: " + key);
                }
            }
            set
            {
                if (value == null)
                {
                    Remove(key);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// Puts the given key in the hashmap or updates it if the upsert flag is set to true.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="upsert"></param>
        /// <returns></returns>
        public bool Put(K key, V value, bool upsert = true)
        {
            return Put(new KeyValuePair<K, V>(key, value), upsert);
        }

        /// <summary>
        /// Puts the given key in the hashmap or updates it if the upsert flag is set to true.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="updateIfSet"></param>
        /// <returns></returns>
        public bool Put(KeyValuePair<K, V> item, bool upsert = true)
        {
            V throwAway;

            return TryAdd(item, out throwAway, upsert);
        }

        /// <summary>
        /// Adds the given key and value to this hash map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(object key, object value)
        {
            Add((K)key, (V)value);
        }

        /// <summary>
        /// Adds a key and a value to this hash map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(K key, V value)
        {
            Add(new KeyValuePair<K, V>(key, value));
        }

        /// <summary>
        /// Adds a keyvaluepair to this hasm map.
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<K, V> item)
        {
            Put(item, true);
        }

        /// <summary>
        /// Attempts to add a value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="existingValue"></param>
        /// <param name="upsert"></param>
        /// <returns></returns>
        public bool TryAdd(K key, V value, out V existingValue, bool upsert = true)
        {
            return TryAdd(new KeyValuePair<K, V>(key, value), out existingValue, upsert);
        }

        /// <summary>
        /// Attempts to add a value.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="existingValue"></param>
        /// <param name="upsert"></param>
        /// <returns></returns>
        public bool TryAdd(KeyValuePair<K, V> item, out V existingValue, bool upsert = true)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException("'key' cannot be null.");
            }

            int hash = Smear(comparer.GetHashCode(item.Key));
            int index = IndexFor(hash, buckets.Length);

            lock (GetMutexFor(index))
            {
                Node foundNode = buckets[index];

                if (foundNode == null)
                {
                    buckets[index] = new Node(item);
                    Interlocked.Increment(ref count);

                    existingValue = default(V);
                    return true;
                }
                else
                {
                    Node lastNode = null;

                    do
                    {
                        if (comparer.Equals(foundNode.kvp.Key, item.Key))
                        {
                            break;
                        }

                        lastNode = foundNode;
                        foundNode = foundNode.next;
                    } while (foundNode != null);

                    if (foundNode != null)
                    {
                        bool success = false;
                        V previousValue = foundNode.kvp.Value;

                        if (upsert)
                        {
                            foundNode.kvp = item;
                            success = true;
                        }

                        existingValue = previousValue;
                        return success;
                    }
                    else
                    {
                        lastNode.next = new Node(
                            item,
                            lastNode.next
                        );
                        Interlocked.Increment(ref count);

                        existingValue = default(V);
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if thie hash map contains the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(object key)
        {
            return Contains((K)key);
        }

        /// <summary>
        /// Returns true if this hash map contains the given keyvaluepair.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<K, V> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentNullException("'key' cannot be null.");
            }

            V response;

            if (TryGetValue(item.Key, out response))
            {
                return response.Equals(item.Value);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if the given key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("'key' cannot be null.");
            }

            int hash = Smear(comparer.GetHashCode(key));
            int index = IndexFor(hash, buckets.Length);

            Node foundNode = buckets[index];

            if (foundNode == null)
            {
                return false;
            }
            else
            {
                do
                {
                    if (comparer.Equals(foundNode.kvp.Key, key))
                    {
                        break;
                    }

                    foundNode = foundNode.next;
                } while (foundNode != null);

                return foundNode != null;
            }
        }

        /// <summary>
        /// Removes the key value pair with the given key.
        /// </summary>
        /// <param name="key"></param>
        public void Remove(object key)
        {
            Remove((K)key);
        }

        /// <summary>
        /// Removes the given key with the given value from this hash map.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<K, V> item)
        {
            return Remove(item.Key, item.Value);
        }

        /// <summary>
        /// Removes the given key from this hash map.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(K key)
        {
            V throwAway;

            return TryRemove(key, out throwAway);
        }

        /// <summary>
        /// Tries to remove the given key from the Map and returns its value through an out parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryRemove(K key, out V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("'key' cannot be null.");
            }

            int hash = Smear(comparer.GetHashCode(key));
            int index = IndexFor(hash, buckets.Length);

            lock (GetMutexFor(index))
            {
                Node foundNode = buckets[index];

                if (foundNode == null)
                {
                    value = default(V);
                    return false;
                }
                else
                {
                    Node lastNode = null;

                    do
                    {
                        if (comparer.Equals(foundNode.kvp.Key, key))
                        {
                            break;
                        }

                        lastNode = foundNode;
                        foundNode = foundNode.next;
                    } while (foundNode != null);

                    if (foundNode != null)
                    {
                        if (lastNode != null)
                        {
                            lastNode.next = foundNode.next;
                        }
                        else
                        {
                            buckets[index] = foundNode.next;
                        }

                        Interlocked.Decrement(ref count);

                        value = foundNode.kvp.Value;
                        return true;
                    }
                    else
                    {
                        value = default(V);
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the given key with the given value from the hash map.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Remove(K key, V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("'key' cannot be null.");
            }

            int hash = Smear(comparer.GetHashCode(key));
            int index = IndexFor(hash, buckets.Length);

            lock (GetMutexFor(index))
            {
                Node foundNode = buckets[index];

                if (foundNode == null)
                {
                    return false;
                }
                else
                {
                    Node lastNode = null;

                    do
                    {
                        if (comparer.Equals(foundNode.kvp.Key, key))
                        {
                            break;
                        }

                        lastNode = foundNode;
                        foundNode = foundNode.next;
                    } while (foundNode != null);

                    if (foundNode != null && foundNode.kvp.Value.Equals(value))
                    {
                        if (lastNode != null)
                        {
                            lastNode.next = foundNode.next;
                        }
                        else
                        {
                            buckets[index] = foundNode.next;
                        }

                        Interlocked.Decrement(ref count);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the value for the given key and returns true if found.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(K key, out V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("'key' cannot be null.");
            }

            int hash = Smear(comparer.GetHashCode(key));
            int index = IndexFor(hash, buckets.Length);

            Node foundNode = buckets[index];

            if (foundNode == null)
            {
                value = default(V);
                return false;
            }
            else
            {
                do
                {
                    if (comparer.Equals(foundNode.kvp.Key, key))
                    {
                        break;
                    }

                    foundNode = foundNode.next;
                } while (foundNode != null);

                if (foundNode != null)
                {
                    value = foundNode.kvp.Value;
                    return true;
                }
                else
                {
                    value = default(V);
                    return false;
                }
            }
        }

        /// <summary>
        /// Clears all keybaluepairs from this hash map.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                lock (GetMutexFor(i))
                {
                    while (buckets[i] != null)
                    {
                        buckets[i] = buckets[i].next;
                        Interlocked.Decrement(ref count);
                    }
                }
            }
        }

        /// <summary>
        /// Copies the contents of this hash map into the given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            CopyTo((Array)array, arrayIndex);
        }

        /// <summary>
        /// Copies the contenst of this hash map into the given array.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("'array' cannot be null.");
            }

            if (index > array.Length)
            {
                return;
            }

            for (int i = index; i < array.Length; i++)
            {
                int bucketIndex = i - index;

                if (bucketIndex >= buckets.Length)
                {
                    break;
                }

                Node currentNode = buckets[bucketIndex];

                while (currentNode != null)
                {
                    array.SetValue(currentNode.kvp, i);

                    currentNode = currentNode.next;
                }
            }
        }

        /// <summary>
        /// Copies the contents of this hash map into the given dictionary.
        /// </summary>
        /// <param name="dictionary"></param>
        public void CopyTo(IDictionary<K, V> dictionary)
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                Node currentNode = buckets[i];

                while (currentNode != null)
                {
                    dictionary.Add(currentNode.kvp);

                    currentNode = currentNode.next;
                }
            }
        }

        /// <summary>
        /// Gets the enumerator for this hash map.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator for this hash map.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            List<KeyValuePair<K, V>> kvps = new List<KeyValuePair<K, V>>();

            for (int i = 0; i < buckets.Length; i++)
            {
                Node currentNode = buckets[i];

                while (currentNode != null)
                {
                    kvps.Add(currentNode.kvp);
                    currentNode = currentNode.next;
                }
            }

            return kvps.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator for this hash map.
        /// </summary>
        /// <returns></returns>
        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new ConcurrentHashMapDictionaryEnumerator(this);
        }

        /// <summary>
        /// Gets the mutex for the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private object GetMutexFor(int index)
        {
            return locks[IndexFor(index, locks.Length)];
        }

        /// <summary>
        /// Checks if the given number is a power of two.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static bool IsPowerOfTwo(uint x)
        {
            return (x & (x - 1)) == 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        private static int IndexFor(int hash, int length)
        {
            return Abs(hash) & (length - 1);
        }

        /// <summary>
        /// Applies a supplemental hash function to a given hashCode, which
        /// defends against poor quality hash functions.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        private static int Smear(int hashCode)
        {
            hashCode ^= (hashCode >> 20) ^ (hashCode >> 12);
            return hashCode ^ (hashCode >> 7) ^ (hashCode >> 4);
        }

        /// <summary>
        /// Gets the absolute value of the given integer.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private static int Abs(int x)
        {
            return (x + (x >> 31)) ^ (x >> 31);
        }

        /// <summary>
        /// The node implementation.
        /// </summary>
        private class Node
        {
            internal KeyValuePair<K, V> kvp;
            internal Node next;

            internal Node(KeyValuePair<K, V> kvp, Node next = null)
            {
                this.kvp = kvp;
                this.next = next;
            }
        }

        /// <summary>
        /// A hashMap iteratoe for thie hash map.
        /// </summary>
        private class ConcurrentHashMapDictionaryEnumerator : IDictionaryEnumerator
        {
            private List<DictionaryEntry> items;
            private int index = -1;

            /// <summary>
            /// Creates a new hashMap iterator for the given hash map.
            /// </summary>
            /// <param name="hashMap"></param>
            public ConcurrentHashMapDictionaryEnumerator(ConcurrentHashMap<K, V> hashMap)
            {
                items = new List<DictionaryEntry>();

                for (int i = 0; i < hashMap.buckets.Length; i++)
                {
                    Node currentNode = hashMap.buckets[i];

                    while (currentNode != null)
                    {
                        items.Add(new DictionaryEntry(currentNode.kvp.Key, currentNode.kvp.Value));
                        currentNode = currentNode.next;
                    }
                }
            }

            /// <summary>
            /// Returns the current item.
            /// </summary>
            public Object Current { get { ValidateIndex(); return items[index]; } }

            /// <summary>
            /// Returns the current hashMap entry.
            /// </summary>
            public DictionaryEntry Entry
            {
                get { return (DictionaryEntry)Current; }
            }

            /// <summary>
            /// Returns the key of the current item.
            /// </summary>
            public Object Key { get { ValidateIndex(); return items[index].Key; } }

            /// <summary>
            /// Returns the value of the current item.
            /// </summary>
            public Object Value { get { ValidateIndex(); return items[index].Value; } }

            /// <summary>
            /// Advance to the next item.
            /// </summary>
            /// <returns></returns>
 
            public Boolean MoveNext()
            {
                if (index < items.Count - 1) 
                { 
                    index++; 
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Validate the enumeration index and throw an exception if the index is out of range. 
            /// </summary>
            private void ValidateIndex()
            {
                if (index < 0 || index >= items.Count)
                {
                    throw new InvalidOperationException("Enumerator is before or after the collection.");
                }
            }

            /// <summary>
            /// Reset the index to restart the enumeration.
            /// </summary>
            public void Reset()
            {
                index = -1;
            }
        }
    }
}