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
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArenaNet.Medley.Serialization
{
    [TestClass]
    public class XmlSerDeTest
    {
        public class Class
        {
            public string String { set; get; }
            public int Integer { set; get; }
        }

        [TestMethod]
        public void TestTextWriterSerialization()
        {
            XmlSerDe serDe = new XmlSerDe();

            StringWriter writer = new StringWriter();
            serDe.SerializeToTextWriter(
                new Class() 
                {
                    String = "51",
                    Integer = 15
                },
                writer);

            Assert.AreEqual("<Class><String>51</String><Integer>15</Integer></Class>", writer.ToString());
        }

        [TestMethod]
        public void TestStreamSerialization()
        {
            XmlSerDe serDe = new XmlSerDe();

            MemoryStream stream = new MemoryStream();
            serDe.SerializeToStream(
                new Class()
                {
                    String = "51",
                    Integer = 15
                },
                stream);

            Assert.AreEqual("<Class><String>51</String><Integer>15</Integer></Class>", Encoding.UTF8.GetString(stream.ToArray(), 0, (int)stream.Position));
        }

        [TestMethod]
        public void TestTextReaderDeserialization()
        {
            XmlSerDe serDe = new XmlSerDe();

            StringReader reader = new StringReader("<Class><String>51</String><Integer>15</Integer></Class>");
            Class clazz = serDe.DeserializeFromTextReader<Class>(reader);

            Assert.AreEqual("51", clazz.String);
            Assert.AreEqual(15, clazz.Integer);
        }

        [TestMethod]
        public void TestStreamDeserialization()
        {
            XmlSerDe serDe = new XmlSerDe();

            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("<Class><String>51</String><Integer>15</Integer></Class>"));
            Class clazz = serDe.DeserializeFromStream<Class>(stream);

            Assert.AreEqual("51", clazz.String);
            Assert.AreEqual(15, clazz.Integer);
        }
    }
}
