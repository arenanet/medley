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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArenaNet.Medley.Concurrent
{
    [TestClass]
    public class PromiseTest
    {
        [TestMethod]
        public void TestMultithreadedFulfill()
        {
            Promise<string> promise = new Promise<string>();

            Thread thread = new Thread((object obj) =>
            {
                Thread.Sleep(1000);

                promise.CreateFulfiller().Fulfill("awesome test!");
            });
            thread.Start();

            Assert.AreEqual("awesome test!", promise.WaitForValue(TimeSpan.FromSeconds(2)));
        }

        [TestMethod]
        public void TestInstantFulfill()
        {
            Promise<string> promise = new Promise<string>("awesome test!");
            Assert.AreEqual("awesome test!", promise.WaitForValue(TimeSpan.FromSeconds(2)));
        }
    }
}
