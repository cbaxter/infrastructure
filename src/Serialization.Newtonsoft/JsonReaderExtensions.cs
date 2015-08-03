using System;
using Newtonsoft.Json;

/* Copyright (c) 2015 Spark Software Ltd.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

namespace Spark.Serialization
{
    internal static class JsonReaderExtensions
    {
        /// <summary>
        /// Returns true if the specified <paramref name="reader"/> is ready to read a JSON object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> instance.</param>
        public static Boolean CanReadObject(this JsonReader reader)
        {
            return (reader.TokenType != JsonToken.None || reader.Read()) && reader.TokenType == JsonToken.StartObject;
        }

        /// <summary>
        /// Returns true if the specified <paramref name="reader"/> is ready to read a JSON array.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> instance.</param>
        public static Boolean CanReadArray(this JsonReader reader)
        {
            return (reader.TokenType != JsonToken.None || reader.Read()) && reader.TokenType == JsonToken.StartArray;
        }
        
        /// <summary>
        /// Returns true if the <paramref name="reader"/> was advanced to the next JSON object property; otherwise false.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> instance.</param>
        /// <param name="propertyName">The property name associated with the current <paramref name="reader"/> value.</param>
        public static Boolean TryGetProperty(this JsonReader reader, out String propertyName)
        {
            propertyName = null;

            while (reader.TokenType != JsonToken.PropertyName && reader.Read())
                continue;

            if (reader.TokenType != JsonToken.PropertyName)
                return false;

            propertyName = (String)reader.Value;
            return reader.Read() && reader.TokenType != JsonToken.Null;
        }
    }
}
