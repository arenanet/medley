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
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArenaNet.Medley.Collections
{
    [TestClass]
    public class IterableLinkedListTest
    {
        
        [TestMethod]
        public void TestAddFirstRemoveFirst()
        {
            IterableLinkedList<string> list = new IterableLinkedList<string>();
            list.AddFirst("3");
            list.AddFirst("2");
            list.AddFirst("1");
            string value;

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("1", value);

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("2", value);

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("3", value);
        }

        [TestMethod]
        public void TestAddLastRemoveFirst()
        {
            IterableLinkedList<string> list = new IterableLinkedList<string>();
            list.AddLast("3");
            list.AddLast("2");
            list.AddLast("1");
            string value;

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("3", value);

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("2", value);

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("1", value);
        }

        [TestMethod]
        public void TestAddBeforeAndAfterRemoveFirst()
        {
            IterableLinkedList<string> list = new IterableLinkedList<string>();
            list.AddLast("2");
            list.AddAfter("2", "3");
            list.AddBefore("2", "1");
            string value;

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("1", value);

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("2", value);

            Assert.IsTrue(list.RemoveFirst(out value));
            Assert.AreEqual("3", value);
        }

        [TestMethod]
        public void TestRemove()
        {
            IterableLinkedList<string> list = new IterableLinkedList<string>();
            for (int i = 0; i < 50; i++)
            {
                list.AddLast("" + i);
            }

            Assert.IsTrue(list.Remove("35"));
            
            string value;

            for (int i = 0; i < 49; i++)
            {
                Assert.IsTrue(list.RemoveFirst(out value));
            }

            Assert.IsFalse(list.RemoveFirst(out value));
        }
    }
}
