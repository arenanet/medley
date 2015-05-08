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

namespace ArenaNet.Medley.Collections.Concurrent
{
    /// <summary>
    /// A concurrent queue implementation based on Maged M. Michael's and Michael L. Scott's concurrent queue algorithm.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentLinkedQueue<T> : IQueue<T>
    {
        /// <summary>
        /// Internal node.
        /// </summary>
        internal class Node
        {
            public T value;
            public Node next = null;
        }

        private Node head = null;
        private Node tail = null;

        /// <summary>
        /// Creates an empty queue.
        /// </summary>
        public ConcurrentLinkedQueue()
        {
            head = new Node();
            tail = head;
        }

        /// <summary>
        /// Enqueue an item into the queue.
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            Node node = new Node()
            {
                value = item
            };

            Node currentTail = null;
            Node currentNext = null;

            while (true)
            {
                currentTail = tail;
                currentNext = tail.next;

                if (currentTail == tail)
                {
                    if (currentNext == null)
                    {
                        if (Interlocked.CompareExchange(ref currentTail.next, node, currentNext) == currentNext)
                        {
                            break;
                        }
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref tail, currentNext, currentTail);
                    }
                }
            }

            Interlocked.CompareExchange(ref tail, node, currentTail);
        }

        /// <summary>
        /// Dequeues an item from the queue.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Dequeue(out T item)
        {
            while (true)
            {
                Node currentHead = head;
                Node currentTail = tail;
                Node currentNext = head.next;

                if (currentHead == head)
                {
                    if (currentHead == currentTail)
                    {
                        if (currentNext == null)
                        {
                            item = default(T);
                            return false;
                        }

                        Interlocked.CompareExchange(ref tail, currentNext, currentTail);
                    }
                    else
                    {
                        item = currentNext.value;

                        if (Interlocked.CompareExchange(ref head, currentNext, currentHead) == currentHead)
                        {
                            break;
                        }
                    }
                }
            }

            return true;
        }
    }
}
