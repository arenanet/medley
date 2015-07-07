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

namespace ArenaNet.Medley.Serialization
{
    /// <summary>
    /// Main Serializer/Deserializer interface.
    /// </summary>
    public interface ISerDe
    {
        /// <summary>
        /// Serializees the given object into the given Stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="stream"></param>
        void SerializeToStream<T>(T obj, Stream stream);

        /// <summary>
        /// Deserializes the content of the given stream into the provided generic object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        T DeserializeFromStream<T>(Stream stream);
    }
}
