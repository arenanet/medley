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
using System.Xml;
using System.Xml.Serialization;

namespace ArenaNet.Medley.Serialization
{
    /// <summary>
    /// Xml based SerDe.
    /// </summary>
    public class XmlSerDe : ITextSerDe
    {
        private static readonly XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
        {
            OmitXmlDeclaration = true,
            Indent = false,
            Encoding = new UTF8Encoding(false)
        };

        private static readonly XmlReaderSettings xmlReaderSettings = new XmlReaderSettings()
        {
            IgnoreWhitespace = true
        };

        /// <summary>
        /// Serializes the given object into the given TextWriter.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="writer"></param>
        public void SerializeToTextWriter<T>(T obj, TextWriter writer)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            serializer.Serialize(XmlWriter.Create(writer, xmlWriterSettings), obj,
                new XmlSerializerNamespaces(new XmlQualifiedName[] { new XmlQualifiedName("", "") }));
        }

        /// <summary>
        /// Serializees the given object into the given Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="stream"></param>
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            serializer.Serialize(XmlWriter.Create(stream, xmlWriterSettings), obj,
                new XmlSerializerNamespaces(new XmlQualifiedName[] { new XmlQualifiedName("", "") }));
        }

        /// <summary>
        /// Deserializes the content of the given stream into the provided object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public T DeserializeFromStream<T>(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            return (T)serializer.Deserialize(XmlReader.Create(stream, xmlReaderSettings));
        }

        /// <summary>
        /// Deserializes the content of the given TextReader into the provided object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public T DeserializeFromTextReader<T>(TextReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            return (T)serializer.Deserialize(XmlReader.Create(reader, xmlReaderSettings));
        }
    }
}
