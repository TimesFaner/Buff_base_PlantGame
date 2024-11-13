/*
Copyright (c) 2010-2021 Matt Schoen

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

//#define JSONOBJECT_DISABLE_PRETTY_PRINT // Use when you no longer need to read JSON to disable pretty Print system-wide
//#define JSONOBJECT_USE_FLOAT //Use floats for numbers instead of doubles (enable if you don't need support for doubles and want to cut down on significant digits in output)
//#define JSONOBJECT_POOLING //Create JSONObjects from a pool and prevent finalization by returning objects to the pool

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable MergeConditionalExpression
// ReSharper disable UseStringInterpolation

#if UNITY_2 || UNITY_3 || UNITY_4 || UNITY_5 || UNITY_5_3_OR_NEWER
#define USING_UNITY
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
#if USING_UNITY
using UnityEngine;
using Debug = UnityEngine.Debug;
#endif

namespace QFramework
{
    public class JSONObject : IEnumerable
    {
#if JSONOBJECT_POOLING
		const int MaxPoolSize = 100000;
		static readonly Queue<JSONObject> JSONObjectPool = new Queue<JSONObject>();
		static readonly Queue<List<JSONObject>> JSONObjectListPool = new Queue<List<JSONObject>>();
		static readonly Queue<List<string>> StringListPool = new Queue<List<string>>();
		static bool poolingEnabled = true;
#endif

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
        private const string Newline = "\r\n";
        private const string Tab = "\t";
#endif

        private const string Infinity = "Infinity";
        private const string NegativeInfinity = "-Infinity";
        private const string NaN = "NaN";
        private const string True = "true";
        private const string False = "false";
        private const string Null = "null";

        private const float MaxFrameTime = 0.008f;
        private static readonly Stopwatch PrintWatch = new();
        public static readonly char[] Whitespace = { ' ', '\r', '\n', '\t', '\uFEFF', '\u0009' };

        public enum Type
        {
            Null,
            String,
            Number,
            Object,
            Array,
            Bool,
            Baked
        }

        public struct ParseResult
        {
            public readonly JSONObject Result;
            public readonly int Offset;
            public readonly bool Pause;

            public ParseResult(JSONObject result, int offset, bool pause)
            {
                Result = result;
                Offset = offset;
                Pause = pause;
            }
        }

        public delegate void FieldNotFound(string name);

        public delegate void GetFieldResponse(JSONObject jsonObject);

        public Type ObjectType { get; private set; } = Type.Null;

        public List<JSONObject> List;
        public List<string> Keys;
        public string StringValue;
        public bool IsInteger;
        public long LongValue;
        public bool BoolValue;
#if JSONOBJECT_USE_FLOAT
		public float floatValue;
#else
        public double DoubleValue;
#endif

        private bool mIsPooled;

        public int Count
        {
            get { return List == null ? 0 : List.Count; }
        }


        public bool IsContainer
        {
            get { return ObjectType == Type.Array || ObjectType == Type.Object; }
        }

        public int IntValue
        {
            get { return (int)LongValue; }
            set { LongValue = value; }
        }

#if JSONOBJECT_USE_FLOAT
		public double doubleValue {
			get {
				return floatValue;
			}
			set {
				floatValue = (float) value;
			}
		}
#else
        public float FloatValue
        {
            get { return (float)DoubleValue; }
            set { DoubleValue = value; }
        }
#endif

        public delegate void AddJSONContents(JSONObject self);

        public static JSONObject NullObject
        {
            get { return Create(Type.Null); }
        }

        public static JSONObject EmptyObject
        {
            get { return Create(Type.Object); }
        }

        public static JSONObject EmptyArray
        {
            get { return Create(Type.Array); }
        }

        public JSONObject(Type type)
        {
            ObjectType = type;
        }

        public JSONObject(bool value)
        {
            ObjectType = Type.Bool;
            BoolValue = value;
        }

        public JSONObject(float value)
        {
            ObjectType = Type.Number;
#if JSONOBJECT_USE_FLOAT
			floatValue = value;
#else
            DoubleValue = value;
#endif
        }

        public JSONObject(double value)
        {
            ObjectType = Type.Number;
#if JSONOBJECT_USE_FLOAT
			floatValue = (float)value;
#else
            DoubleValue = value;
#endif
        }

        public JSONObject(int value)
        {
            ObjectType = Type.Number;
            LongValue = value;
            IsInteger = true;
#if JSONOBJECT_USE_FLOAT
			floatValue = value;
#else
            DoubleValue = value;
#endif
        }

        public JSONObject(long value)
        {
            ObjectType = Type.Number;
            LongValue = value;
            IsInteger = true;
#if JSONOBJECT_USE_FLOAT
			floatValue = value;
#else
            DoubleValue = value;
#endif
        }

        public JSONObject(Dictionary<string, string> dictionary)
        {
            ObjectType = Type.Object;
            Keys = CreateStringList();
            List = CreateJSONObjectList();
            foreach (var kvp in dictionary)
            {
                Keys.Add(kvp.Key);
                List.Add(CreateStringObject(kvp.Value));
            }
        }

        public JSONObject(Dictionary<string, JSONObject> dictionary)
        {
            ObjectType = Type.Object;
            Keys = CreateStringList();
            List = CreateJSONObjectList();
            foreach (var kvp in dictionary)
            {
                Keys.Add(kvp.Key);
                List.Add(kvp.Value);
            }
        }

        public JSONObject(AddJSONContents content)
        {
            content.Invoke(this);
        }

        public JSONObject(JSONObject[] objects)
        {
            ObjectType = Type.Array;
            List = CreateJSONObjectList();
            List.AddRange(objects);
        }

        public JSONObject(List<JSONObject> objects)
        {
            ObjectType = Type.Array;
            List = objects;
        }

        /// <summary>
        ///     Convenience function for creating a JSONObject containing a string.
        ///     This is not part of the constructor so that malformed JSON data doesn't just turn into a string object
        /// </summary>
        /// <param name="value">The string value for the new JSONObject</param>
        /// <returns>Thew new JSONObject</returns>
        public static JSONObject StringObject(string value)
        {
            return CreateStringObject(value);
        }

        public void Absorb(JSONObject other)
        {
            var otherList = other.List;
            if (otherList != null)
            {
                if (List == null)
                    List = CreateJSONObjectList();

                List.AddRange(otherList);
            }

            var otherKeys = other.Keys;
            if (otherKeys != null)
            {
                if (Keys == null)
                    Keys = CreateStringList();

                Keys.AddRange(otherKeys);
            }

            StringValue = other.StringValue;
#if JSONOBJECT_USE_FLOAT
			floatValue = other.floatValue;
#else
            DoubleValue = other.DoubleValue;
#endif

            IsInteger = other.IsInteger;
            LongValue = other.LongValue;
            BoolValue = other.BoolValue;
            ObjectType = other.ObjectType;
        }

        public static JSONObject Create()
        {
#if JSONOBJECT_POOLING
			lock (JSONObjectPool) {
				if (JSONObjectPool.Count > 0) {
					var result = JSONObjectPool.Dequeue();

					result.isPooled = false;
					return result;
				}
			}
#endif

            return new JSONObject();
        }

        public static JSONObject Create(Type type)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = type;
            return jsonObject;
        }

        public static JSONObject Create(bool value)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Bool;
            jsonObject.BoolValue = value;
            return jsonObject;
        }

        public static JSONObject Create(float value)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Number;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = value;
#else
            jsonObject.DoubleValue = value;
#endif

            return jsonObject;
        }

        public static JSONObject Create(double value)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Number;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = (float)value;
#else
            jsonObject.DoubleValue = value;
#endif

            return jsonObject;
        }

        public static JSONObject Create(int value)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Number;
            jsonObject.IsInteger = true;
            jsonObject.LongValue = value;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = value;
#else
            jsonObject.DoubleValue = value;
#endif

            return jsonObject;
        }

        public static JSONObject Create(long value)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Number;
            jsonObject.IsInteger = true;
            jsonObject.LongValue = value;
#if JSONOBJECT_USE_FLOAT
			jsonObject.floatValue = value;
#else
            jsonObject.DoubleValue = value;
#endif

            return jsonObject;
        }

        public static JSONObject CreateStringObject(string value)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.String;
            jsonObject.StringValue = value;
            return jsonObject;
        }

        public static JSONObject CreateBakedObject(string value)
        {
            var bakedObject = Create();
            bakedObject.ObjectType = Type.Baked;
            bakedObject.StringValue = value;
            return bakedObject;
        }

        /// <summary>
        ///     Create a JSONObject (using pooling if enabled) using a string containing valid JSON
        /// </summary>
        /// <param name="jsonString">A string containing valid JSON to be parsed into objects</param>
        /// <param name="offset">An offset into the string at which to start parsing</param>
        /// <param name="endOffset">
        ///     The length of the string after the offset to parse
        ///     Specify a length of -1 (default) to use the full string length
        /// </param>
        /// <param name="maxDepth">The maximum depth for the parser to search.</param>
        /// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
        /// <returns>A JSONObject containing the parsed data</returns>
        public static JSONObject Create(string jsonString, int offset = 0, int endOffset = -1, int maxDepth = -1,
            bool storeExcessLevels = false)
        {
            var jsonObject = Create();
            Parse(jsonString, ref offset, endOffset, jsonObject, maxDepth, storeExcessLevels);
            return jsonObject;
        }

        public static JSONObject Create(AddJSONContents content)
        {
            var jsonObject = Create();
            content.Invoke(jsonObject);
            return jsonObject;
        }

        public static JSONObject Create(JSONObject[] objects)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Array;
            var list = CreateJSONObjectList();
            list.AddRange(objects);
            jsonObject.List = list;

            return jsonObject;
        }

        public static JSONObject Create(List<JSONObject> objects)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Array;
            jsonObject.List = objects;

            return jsonObject;
        }

        public static JSONObject Create(Dictionary<string, string> dictionary)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Object;
            var keys = CreateStringList();
            jsonObject.Keys = keys;
            var list = CreateJSONObjectList();
            jsonObject.List = list;
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                list.Add(CreateStringObject(kvp.Value));
            }

            return jsonObject;
        }

        public static JSONObject Create(Dictionary<string, JSONObject> dictionary)
        {
            var jsonObject = Create();
            jsonObject.ObjectType = Type.Object;
            var keys = CreateStringList();
            jsonObject.Keys = keys;
            var list = CreateJSONObjectList();
            jsonObject.List = list;
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                list.Add(kvp.Value);
            }

            return jsonObject;
        }

        /// <summary>
        ///     Create a JSONObject (using pooling if enabled) using a string containing valid JSON
        /// </summary>
        /// <param name="jsonString">A string containing valid JSON to be parsed into objects</param>
        /// <param name="offset">An offset into the string at which to start parsing</param>
        /// <param name="endOffset">
        ///     The length of the string after the offset to parse
        ///     Specify a length of -1 (default) to use the full string length
        /// </param>
        /// <param name="maxDepth">The maximum depth for the parser to search.</param>
        /// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
        /// <returns>A JSONObject containing the parsed data</returns>
        public static IEnumerable<ParseResult> CreateAsync(string jsonString, int offset = 0, int endOffset = -1,
            int maxDepth = -1, bool storeExcessLevels = false)
        {
            var jsonObject = Create();
            PrintWatch.Reset();
            PrintWatch.Start();
            foreach (var e in ParseAsync(jsonString, offset, endOffset, jsonObject, maxDepth, storeExcessLevels))
            {
                if (e.Pause)
                    yield return e;

                offset = e.Offset;
            }

            yield return new ParseResult(jsonObject, offset, false);
        }

        public JSONObject()
        {
        }

        /// <summary>
        ///     Construct a new JSONObject using a string containing valid JSON
        /// </summary>
        /// <param name="jsonString">A string containing valid JSON to be parsed into objects</param>
        /// <param name="offset">An offset into the string at which to start parsing</param>
        /// <param name="endOffset">
        ///     The length of the string after the offset to parse
        ///     Specify a length of -1 (default) to use the full string length
        /// </param>
        /// <param name="maxDepth">The maximum depth for the parser to search.</param>
        /// <param name="storeExcessLevels">Whether to store levels beyond maxDepth in baked JSONObjects</param>
        public JSONObject(string jsonString, int offset = 0, int endOffset = -1, int maxDepth = -1,
            bool storeExcessLevels = false)
        {
            Parse(jsonString, ref offset, endOffset, this, maxDepth, storeExcessLevels);
        }

        // ReSharper disable UseNameofExpression
        private static bool BeginParse(string inputString, int offset, ref int endOffset, JSONObject container,
            int maxDepth, bool storeExcessLevels)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            if (maxDepth == 0)
            {
                if (storeExcessLevels)
                {
                    container.StringValue = inputString;
                    container.ObjectType = Type.Baked;
                }
                else
                {
                    container.ObjectType = Type.Null;
                }

                return false;
            }

            var stringLength = inputString.Length;
            if (endOffset == -1)
                endOffset = stringLength - 1;

            if (string.IsNullOrEmpty(inputString)) return false;

            if (endOffset >= stringLength)
                throw new ArgumentException("Cannot parse if end offset is greater than or equal to string length",
                    "endOffset");

            if (offset > endOffset)
                throw new ArgumentException("Cannot parse if offset is greater than end offset", "offset");

            return true;
        }
        // ReSharper restore UseNameofExpression

        private static void Parse(string inputString, ref int offset, int endOffset, JSONObject container, int maxDepth,
            bool storeExcessLevels, int depth = 0, bool isRoot = true)
        {
            if (!BeginParse(inputString, offset, ref endOffset, container, maxDepth, storeExcessLevels))
                return;

            var startOffset = offset;
            var quoteStart = 0;
            var quoteEnd = 0;
            var lastValidOffset = offset;
            var openQuote = false;
            var bakeDepth = 0;

            while (offset <= endOffset)
            {
                var currentCharacter = inputString[offset++];
                if (Array.IndexOf(Whitespace, currentCharacter) > -1)
                    continue;

                JSONObject newContainer;
                switch (currentCharacter)
                {
                    case '\\':
                        offset++;
                        break;
                    case '{':
                        if (openQuote)
                            break;

                        if (maxDepth >= 0 && depth >= maxDepth)
                        {
                            bakeDepth++;
                            break;
                        }

                        newContainer = container;
                        if (!isRoot)
                        {
                            newContainer = Create();
                            SafeAddChild(container, newContainer);
                        }

                        newContainer.ObjectType = Type.Object;
                        Parse(inputString, ref offset, endOffset, newContainer, maxDepth, storeExcessLevels, depth + 1,
                            false);

                        break;
                    case '[':
                        if (openQuote)
                            break;

                        if (maxDepth >= 0 && depth >= maxDepth)
                        {
                            bakeDepth++;
                            break;
                        }

                        newContainer = container;
                        if (!isRoot)
                        {
                            newContainer = Create();
                            SafeAddChild(container, newContainer);
                        }

                        newContainer.ObjectType = Type.Array;
                        Parse(inputString, ref offset, endOffset, newContainer, maxDepth, storeExcessLevels, depth + 1,
                            false);

                        break;
                    case '}':
                        if (!ParseObjectEnd(inputString, offset, openQuote, container, startOffset, lastValidOffset,
                                maxDepth, storeExcessLevels, depth, ref bakeDepth))
                            return;

                        break;
                    case ']':
                        if (!ParseArrayEnd(inputString, offset, openQuote, container, startOffset, lastValidOffset,
                                maxDepth, storeExcessLevels, depth, ref bakeDepth))
                            return;

                        break;
                    case '"':
                        ParseQuote(ref openQuote, offset, ref quoteStart, ref quoteEnd);
                        break;
                    case ':':
                        if (!ParseColon(inputString, openQuote, container, ref startOffset, offset, quoteStart,
                                quoteEnd, bakeDepth))
                            return;

                        break;
                    case ',':
                        if (!ParseComma(inputString, openQuote, container, ref startOffset, offset, lastValidOffset,
                                bakeDepth))
                            return;

                        break;
                }

                lastValidOffset = offset - 1;
            }
        }

        private static IEnumerable<ParseResult> ParseAsync(string inputString, int offset, int endOffset,
            JSONObject container,
            int maxDepth, bool storeExcessLevels, int depth = 0, bool isRoot = true)
        {
            if (!BeginParse(inputString, offset, ref endOffset, container, maxDepth, storeExcessLevels))
                yield break;

            var startOffset = offset;
            var quoteStart = 0;
            var quoteEnd = 0;
            var lastValidOffset = offset;
            var openQuote = false;
            var bakeDepth = 0;
            while (offset <= endOffset)
            {
                if (PrintWatch.Elapsed.TotalSeconds > MaxFrameTime)
                {
                    PrintWatch.Reset();
                    yield return new ParseResult(container, offset, true);
                    PrintWatch.Start();
                }

                var currentCharacter = inputString[offset++];
                if (Array.IndexOf(Whitespace, currentCharacter) > -1)
                    continue;

                JSONObject newContainer;
                switch (currentCharacter)
                {
                    case '\\':
                        offset++;
                        break;
                    case '{':
                        if (openQuote)
                            break;

                        if (maxDepth >= 0 && depth >= maxDepth)
                        {
                            bakeDepth++;
                            break;
                        }

                        newContainer = container;
                        if (!isRoot)
                        {
                            newContainer = Create();
                            SafeAddChild(container, newContainer);
                        }

                        newContainer.ObjectType = Type.Object;
                        foreach (var e in ParseAsync(inputString, offset, endOffset, newContainer, maxDepth,
                                     storeExcessLevels, depth + 1, false))
                        {
                            if (e.Pause)
                                yield return e;

                            offset = e.Offset;
                        }

                        break;
                    case '[':
                        if (openQuote)
                            break;

                        if (maxDepth >= 0 && depth >= maxDepth)
                        {
                            bakeDepth++;
                            break;
                        }

                        newContainer = container;
                        if (!isRoot)
                        {
                            newContainer = Create();
                            SafeAddChild(container, newContainer);
                        }

                        newContainer.ObjectType = Type.Array;
                        foreach (var e in ParseAsync(inputString, offset, endOffset, newContainer, maxDepth,
                                     storeExcessLevels, depth + 1, false))
                        {
                            if (e.Pause)
                                yield return e;

                            offset = e.Offset;
                        }

                        break;
                    case '}':
                        if (!ParseObjectEnd(inputString, offset, openQuote, container, startOffset, lastValidOffset,
                                maxDepth, storeExcessLevels, depth, ref bakeDepth))
                        {
                            yield return new ParseResult(container, offset, false);
                            yield break;
                        }

                        break;
                    case ']':
                        if (!ParseArrayEnd(inputString, offset, openQuote, container, startOffset, lastValidOffset,
                                maxDepth, storeExcessLevels, depth, ref bakeDepth))
                        {
                            yield return new ParseResult(container, offset, false);
                            yield break;
                        }

                        break;
                    case '"':
                        ParseQuote(ref openQuote, offset, ref quoteStart, ref quoteEnd);
                        break;
                    case ':':
                        if (!ParseColon(inputString, openQuote, container, ref startOffset, offset, quoteStart,
                                quoteEnd, bakeDepth))
                        {
                            yield return new ParseResult(container, offset, false);
                            yield break;
                        }

                        break;
                    case ',':
                        if (!ParseComma(inputString, openQuote, container, ref startOffset, offset, lastValidOffset,
                                bakeDepth))
                        {
                            yield return new ParseResult(container, offset, false);
                            yield break;
                        }

                        break;
                }

                lastValidOffset = offset - 1;
            }

            yield return new ParseResult(container, offset, false);
        }

        private static void SafeAddChild(JSONObject container, JSONObject child)
        {
            var list = container.List;
            if (list == null)
            {
                list = CreateJSONObjectList();
                container.List = list;
            }

            list.Add(child);
        }

        private void ParseValue(string inputString, int startOffset, int lastValidOffset)
        {
            var firstCharacter = inputString[startOffset];
            do
            {
                if (Array.IndexOf(Whitespace, firstCharacter) > -1)
                {
                    firstCharacter = inputString[++startOffset];
                    continue;
                }

                break;
            } while (true);

            // Use character comparison instead of string compare as performance optimization
            switch (firstCharacter)
            {
                case '"':
                    ObjectType = Type.String;

                    // Trim quotes from string values
                    StringValue =
                        UnEscapeString(inputString.Substring(startOffset + 1, lastValidOffset - startOffset - 1));
                    return;
                case 't':
                    ObjectType = Type.Bool;
                    BoolValue = true;
                    return;
                case 'f':
                    ObjectType = Type.Bool;
                    BoolValue = false;
                    return;
                case 'n':
                    ObjectType = Type.Null;
                    return;
                case 'I':
                    ObjectType = Type.Number;

#if JSONOBJECT_USE_FLOAT
					floatValue = float.PositiveInfinity;
#else
                    DoubleValue = double.PositiveInfinity;
#endif

                    return;
                case 'N':
                    ObjectType = Type.Number;

#if JSONOBJECT_USE_FLOAT
					floatValue = float.NaN;
#else
                    DoubleValue = double.NaN;
#endif
                    return;
                case '-':
                    if (inputString[startOffset + 1] == 'I')
                    {
                        ObjectType = Type.Number;
#if JSONOBJECT_USE_FLOAT
						floatValue = float.NegativeInfinity;
#else
                        DoubleValue = double.NegativeInfinity;
#endif
                        return;
                    }

                    break;
            }

            var numericString = inputString.Substring(startOffset, lastValidOffset - startOffset + 1);
            try
            {
                if (numericString.Contains("."))
                {
#if JSONOBJECT_USE_FLOAT
					floatValue = Convert.ToSingle(numericString, CultureInfo.InvariantCulture);
#else
                    DoubleValue = Convert.ToDouble(numericString, CultureInfo.InvariantCulture);
#endif
                }
                else
                {
                    LongValue = Convert.ToInt64(numericString, CultureInfo.InvariantCulture);
                    IsInteger = true;
#if JSONOBJECT_USE_FLOAT
					floatValue = longValue;
#else
                    DoubleValue = LongValue;
#endif
                }

                ObjectType = Type.Number;
            }
            catch (OverflowException)
            {
                ObjectType = Type.Number;
#if JSONOBJECT_USE_FLOAT
				floatValue = numericString.StartsWith("-") ? float.NegativeInfinity : float.PositiveInfinity;
#else
                DoubleValue = numericString.StartsWith("-") ? double.NegativeInfinity : double.PositiveInfinity;
#endif
            }
            catch (FormatException)
            {
                ObjectType = Type.Null;
#if USING_UNITY
                Debug.LogWarning
#else
				Debug.WriteLine
#endif
                    (string.Format("Improper JSON formatting:{0}", numericString));
            }
        }

        private static bool ParseObjectEnd(string inputString, int offset, bool openQuote, JSONObject container,
            int startOffset,
            int lastValidOffset, int maxDepth, bool storeExcessLevels, int depth, ref int bakeDepth)
        {
            if (openQuote)
                return true;

            if (container == null)
            {
                Debug.LogError("Parsing error: encountered `}` with no container object");
                return false;
            }

            if (maxDepth >= 0 && depth >= maxDepth)
            {
                bakeDepth--;
                if (bakeDepth == 0)
                    SafeAddChild(container,
                        storeExcessLevels
                            ? CreateBakedObject(inputString.Substring(startOffset, offset - startOffset))
                            : NullObject);

                if (bakeDepth >= 0)
                    return true;
            }

            ParseFinalObjectIfNeeded(inputString, container, startOffset, lastValidOffset);
            return false;
        }

        private static bool ParseArrayEnd(string inputString, int offset, bool openQuote, JSONObject container,
            int startOffset, int lastValidOffset, int maxDepth, bool storeExcessLevels, int depth, ref int bakeDepth)
        {
            if (openQuote)
                return true;

            if (container == null)
            {
                Debug.LogError("Parsing error: encountered `]` with no container object");
                return false;
            }

            if (maxDepth >= 0 && depth >= maxDepth)
            {
                bakeDepth--;
                if (bakeDepth == 0)
                    SafeAddChild(container,
                        storeExcessLevels
                            ? CreateBakedObject(inputString.Substring(startOffset, offset - startOffset))
                            : NullObject);

                if (bakeDepth >= 0)
                    return true;
            }

            ParseFinalObjectIfNeeded(inputString, container, startOffset, lastValidOffset);
            return false;
        }

        private static void ParseQuote(ref bool openQuote, int offset, ref int quoteStart, ref int quoteEnd)
        {
            if (openQuote)
            {
                quoteEnd = offset - 1;
                openQuote = false;
            }
            else
            {
                quoteStart = offset;
                openQuote = true;
            }
        }

        private static bool ParseColon(string inputString, bool openQuote, JSONObject container,
            ref int startOffset, int offset, int quoteStart, int quoteEnd, int bakeDepth)
        {
            if (openQuote || bakeDepth > 0)
                return true;

            if (container == null)
            {
                Debug.LogError("Parsing error: encountered `:` with no container object");
                return false;
            }

            var keys = container.Keys;
            if (keys == null)
            {
                keys = CreateStringList();
                container.Keys = keys;
            }

            container.Keys.Add(inputString.Substring(quoteStart, quoteEnd - quoteStart));
            startOffset = offset;

            return true;
        }

        private static bool ParseComma(string inputString, bool openQuote, JSONObject container,
            ref int startOffset, int offset, int lastValidOffset, int bakeDepth)
        {
            if (openQuote || bakeDepth > 0)
                return true;

            if (container == null)
            {
                Debug.LogError("Parsing error: encountered `,` with no container object");
                return false;
            }

            ParseFinalObjectIfNeeded(inputString, container, startOffset, lastValidOffset);

            startOffset = offset;
            return true;
        }

        private static void ParseFinalObjectIfNeeded(string inputString, JSONObject container, int startOffset,
            int lastValidOffset)
        {
            if (IsClosingCharacter(inputString[lastValidOffset]))
                return;

            var child = Create();
            child.ParseValue(inputString, startOffset, lastValidOffset);
            SafeAddChild(container, child);
        }

        private static bool IsClosingCharacter(char character)
        {
            switch (character)
            {
                case '}':
                case ']':
                    return true;
            }

            return false;
        }

        public bool IsNumber
        {
            get { return ObjectType == Type.Number; }
        }

        public bool IsNull
        {
            get { return ObjectType == Type.Null; }
        }

        public bool IsString
        {
            get { return ObjectType == Type.String; }
        }

        public bool IsBool
        {
            get { return ObjectType == Type.Bool; }
        }

        public bool IsArray
        {
            get { return ObjectType == Type.Array; }
        }

        public bool IsObject
        {
            get { return ObjectType == Type.Object; }
        }

        public bool IsBaked
        {
            get { return ObjectType == Type.Baked; }
        }

        public void Add(bool value)
        {
            Add(Create(value));
        }

        public void Add(float value)
        {
            Add(Create(value));
        }

        public void Add(double value)
        {
            Add(Create(value));
        }

        public void Add(long value)
        {
            Add(Create(value));
        }

        public void Add(int value)
        {
            Add(Create(value));
        }

        public void Add(string value)
        {
            Add(CreateStringObject(value));
        }

        public void Add(AddJSONContents content)
        {
            Add(Create(content));
        }

        public void Add(JSONObject jsonObject)
        {
            if (jsonObject == null)
                return;

            // Convert to array to support list
            ObjectType = Type.Array;
            if (List == null)
                List = CreateJSONObjectList();

            List.Add(jsonObject);
        }

        public void AddField(string name, bool value)
        {
            AddField(name, Create(value));
        }

        public void AddField(string name, float value)
        {
            AddField(name, Create(value));
        }

        public void AddField(string name, double value)
        {
            AddField(name, Create(value));
        }

        public void AddField(string name, int value)
        {
            AddField(name, Create(value));
        }

        public void AddField(string name, long value)
        {
            AddField(name, Create(value));
        }

        public void AddField(string name, AddJSONContents content)
        {
            AddField(name, Create(content));
        }

        public void AddField(string name, string value)
        {
            AddField(name, CreateStringObject(value));
        }

        public void AddField(string name, JSONObject jsonObject)
        {
            if (jsonObject == null)
                return;

            // Convert to object if needed to support fields
            ObjectType = Type.Object;
            if (List == null)
                List = CreateJSONObjectList();

            if (Keys == null)
                Keys = CreateStringList();

            while (Keys.Count < List.Count) Keys.Add(Keys.Count.ToString(CultureInfo.InvariantCulture));

            Keys.Add(name);
            List.Add(jsonObject);
        }

        public void SetField(string name, string value)
        {
            SetField(name, CreateStringObject(value));
        }

        public void SetField(string name, bool value)
        {
            SetField(name, Create(value));
        }

        public void SetField(string name, float value)
        {
            SetField(name, Create(value));
        }

        public void SetField(string name, double value)
        {
            SetField(name, Create(value));
        }

        public void SetField(string name, long value)
        {
            SetField(name, Create(value));
        }

        public void SetField(string name, int value)
        {
            SetField(name, Create(value));
        }

        public void SetField(string name, JSONObject jsonObject)
        {
            if (HasField(name))
            {
                List.Remove(this[name]);
                Keys.Remove(name);
            }

            AddField(name, jsonObject);
        }

        public void RemoveField(string name)
        {
            if (Keys == null || List == null)
                return;

            if (Keys.IndexOf(name) > -1)
            {
                List.RemoveAt(Keys.IndexOf(name));
                Keys.Remove(name);
            }
        }

        public bool GetField(out bool field, string name, bool fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref bool field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
                    field = List[index].BoolValue;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(out double field, string name, double fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref double field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
#if JSONOBJECT_USE_FLOAT
					field = list[index].floatValue;
#else
                    field = List[index].DoubleValue;
#endif
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(out float field, string name, float fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref float field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
#if JSONOBJECT_USE_FLOAT
					field = list[index].floatValue;
#else
                    field = (float)List[index].DoubleValue;
#endif
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(out int field, string name, int fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref int field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (int)List[index].LongValue;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(out long field, string name, long fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref long field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
                    field = List[index].LongValue;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(out uint field, string name, uint fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref uint field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
                    field = (uint)List[index].LongValue;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public bool GetField(out string field, string name, string fallback)
        {
            field = fallback;
            return GetField(ref field, name);
        }

        public bool GetField(ref string field, string name, FieldNotFound fail = null)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
                    field = List[index].StringValue;
                    return true;
                }
            }

            if (fail != null) fail.Invoke(name);
            return false;
        }

        public void GetField(string name, GetFieldResponse response, FieldNotFound fail = null)
        {
            if (response != null && ObjectType == Type.Object && Keys != null && List != null)
            {
                var index = Keys.IndexOf(name);
                if (index >= 0)
                {
                    response.Invoke(List[index]);
                    return;
                }
            }

            if (fail != null)
                fail.Invoke(name);
        }

        public JSONObject GetField(string name)
        {
            if (ObjectType == Type.Object && Keys != null && List != null)
                for (var index = 0; index < Keys.Count; index++)
                    if (Keys[index] == name)
                        return List[index];

            return null;
        }

        public bool HasFields(string[] names)
        {
            if (ObjectType != Type.Object || Keys == null || List == null)
                return false;

            foreach (var name in names)
                if (!Keys.Contains(name))
                    return false;

            return true;
        }

        public bool HasField(string name)
        {
            if (ObjectType != Type.Object || Keys == null || List == null)
                return false;

            if (Keys == null || List == null)
                return false;

            foreach (var fieldName in Keys)
                if (fieldName == name)
                    return true;

            return false;
        }

        public void Clear()
        {
#if JSONOBJECT_POOLING
			if (list != null) {
				lock (JSONObjectListPool) {
					if (poolingEnabled && JSONObjectListPool.Count < MaxPoolSize) {
						list.Clear();
						JSONObjectListPool.Enqueue(list);
					}
				}
			}

			if (keys != null) {
				lock (StringListPool) {
					if (poolingEnabled && StringListPool.Count < MaxPoolSize) {
						keys.Clear();
						StringListPool.Enqueue(keys);
					}
				}
			}
#endif

            ObjectType = Type.Null;
            List = null;
            Keys = null;
            StringValue = null;
            LongValue = 0;
            BoolValue = false;
            IsInteger = false;
#if JSONOBJECT_USE_FLOAT
			floatValue = 0;
#else
            DoubleValue = 0;
#endif
        }

        /// <summary>
        ///     Copy a JSONObject. This could be more efficient
        /// </summary>
        /// <returns></returns>
        public JSONObject Copy()
        {
            return Create(Print());
        }

        /*
         * The Merge function is experimental. Use at your own risk.
         */
        public void Merge(JSONObject jsonObject)
        {
            MergeRecur(this, jsonObject);
        }

        /// <summary>
        ///     Merge object right into left recursively
        /// </summary>
        /// <param name="left">The left (base) object</param>
        /// <param name="right">The right (new) object</param>
        private static void MergeRecur(JSONObject left, JSONObject right)
        {
            if (left.ObjectType == Type.Null)
            {
                left.Absorb(right);
            }
            else if (left.ObjectType == Type.Object && right.ObjectType == Type.Object && right.List != null &&
                     right.Keys != null)
            {
                for (var i = 0; i < right.List.Count; i++)
                {
                    var key = right.Keys[i];
                    if (right[i].IsContainer)
                    {
                        if (left.HasField(key))
                            MergeRecur(left[key], right[i]);
                        else
                            left.AddField(key, right[i]);
                    }
                    else
                    {
                        if (left.HasField(key))
                            left.SetField(key, right[i]);
                        else
                            left.AddField(key, right[i]);
                    }
                }
            }
            else if (left.ObjectType == Type.Array && right.ObjectType == Type.Array && right.List != null)
            {
                if (right.Count > left.Count)
                {
#if USING_UNITY
                    Debug.LogError
#else
					Debug.WriteLine
#endif
                        ("Cannot merge arrays when right object has more elements");
                    return;
                }

                for (var i = 0; i < right.List.Count; i++)
                    if (left[i].ObjectType == right[i].ObjectType)
                    {
                        //Only overwrite with the same type
                        if (left[i].IsContainer)
                            MergeRecur(left[i], right[i]);
                        else
                            left[i] = right[i];
                    }
            }
        }

        public void Bake()
        {
            if (ObjectType == Type.Baked)
                return;

            StringValue = Print();
            ObjectType = Type.Baked;
        }

        public IEnumerable BakeAsync()
        {
            if (ObjectType == Type.Baked)
                yield break;


            var builder = new StringBuilder();
            using (var enumerator = PrintAsync(builder).GetEnumerator())
            {
                while (enumerator.MoveNext())
                    if (enumerator.Current)
                        yield return null;

                StringValue = builder.ToString();
                ObjectType = Type.Baked;
            }
        }

        public string Print(bool pretty = false)
        {
            var builder = new StringBuilder();
            Print(builder, pretty);
            return builder.ToString();
        }

        public void Print(StringBuilder builder, bool pretty = false)
        {
            Stringify(0, builder, pretty);
        }

        private static string EscapeString(string input)
        {
            var escaped = input.Replace("\b", "\\b");
            escaped = escaped.Replace("\f", "\\f");
            escaped = escaped.Replace("\n", "\\n");
            escaped = escaped.Replace("\r", "\\r");
            escaped = escaped.Replace("\t", "\\t");
            escaped = escaped.Replace("\"", "\\\"");
            return escaped;
        }

        private static string UnEscapeString(string input)
        {
            var unescaped = input.Replace("\\\"", "\"");
            unescaped = unescaped.Replace("\\b", "\b");
            unescaped = unescaped.Replace("\\f", "\f");
            unescaped = unescaped.Replace("\\n", "\n");
            unescaped = unescaped.Replace("\\r", "\r");
            unescaped = unescaped.Replace("\\t", "\t");
            return unescaped;
        }

        public IEnumerable<string> PrintAsync(bool pretty = false)
        {
            var builder = new StringBuilder();
            foreach (var pause in PrintAsync(builder, pretty))
                if (pause)
                    yield return null;

            yield return builder.ToString();
        }

        public IEnumerable<bool> PrintAsync(StringBuilder builder, bool pretty = false)
        {
            PrintWatch.Reset();
            PrintWatch.Start();
            using (var enumerator = StringifyAsync(0, builder, pretty).GetEnumerator())
            {
                while (enumerator.MoveNext())
                    if (enumerator.Current)
                        yield return true;
            }
        }

        /// <summary>
        ///     Convert the JSONObject into a string
        /// </summary>
        /// <param name="depth">How many containers deep this run has reached</param>
        /// <param name="builder">The StringBuilder used to build the string</param>
        /// <param name="pretty">Whether this string should be "pretty" and include whitespace for readability</param>
        /// <returns>An enumerator for this function</returns>
        private IEnumerable<bool> StringifyAsync(int depth, StringBuilder builder, bool pretty = false)
        {
            if (PrintWatch.Elapsed.TotalSeconds > MaxFrameTime)
            {
                PrintWatch.Reset();
                yield return true;
                PrintWatch.Start();
            }

            switch (ObjectType)
            {
                case Type.Baked:
                    builder.Append(StringValue);
                    break;
                case Type.String:
                    StringifyString(builder);
                    break;
                case Type.Number:
                    StringifyNumber(builder);
                    break;
                case Type.Object:
                    var fieldCount = Count;
                    if (fieldCount <= 0)
                    {
                        StringifyEmptyObject(builder);
                        break;
                    }

                    depth++;

                    BeginStringifyObjectContainer(builder, pretty);
                    for (var index = 0; index < fieldCount; index++)
                    {
                        var jsonObject = List[index];
                        if (jsonObject == null)
                            continue;

                        var key = Keys[index];
                        BeginStringifyObjectField(builder, pretty, depth, key);
                        foreach (var pause in jsonObject.StringifyAsync(depth, builder, pretty))
                            if (pause)
                                yield return true;

                        EndStringifyObjectField(builder, pretty);
                    }

                    EndStringifyObjectContainer(builder, pretty, depth);
                    break;
                case Type.Array:
                    var arraySize = Count;
                    if (arraySize <= 0)
                    {
                        StringifyEmptyArray(builder);
                        break;
                    }

                    BeginStringifyArrayContainer(builder, pretty);
                    for (var index = 0; index < arraySize; index++)
                    {
                        var jsonObject = List[index];
                        if (jsonObject == null)
                            continue;

                        BeginStringifyArrayElement(builder, pretty, depth);
                        foreach (var pause in List[index].StringifyAsync(depth, builder, pretty))
                            if (pause)
                                yield return true;

                        EndStringifyArrayElement(builder, pretty);
                    }

                    EndStringifyArrayContainer(builder, pretty, depth);
                    break;
                case Type.Bool:
                    StringifyBool(builder);
                    break;
                case Type.Null:
                    StringifyNull(builder);
                    break;
            }
        }

        /// <summary>
        ///     Convert the JSONObject into a string
        /// </summary>
        /// <param name="depth">How many containers deep this run has reached</param>
        /// <param name="builder">The StringBuilder used to build the string</param>
        /// <param name="pretty">Whether this string should be "pretty" and include whitespace for readability</param>
        private void Stringify(int depth, StringBuilder builder, bool pretty = false)
        {
            depth++;
            switch (ObjectType)
            {
                case Type.Baked:
                    builder.Append(StringValue);
                    break;
                case Type.String:
                    StringifyString(builder);
                    break;
                case Type.Number:
                    StringifyNumber(builder);
                    break;
                case Type.Object:
                    var fieldCount = Count;
                    if (fieldCount <= 0)
                    {
                        StringifyEmptyObject(builder);
                        break;
                    }

                    BeginStringifyObjectContainer(builder, pretty);
                    for (var index = 0; index < fieldCount; index++)
                    {
                        var jsonObject = List[index];
                        if (jsonObject == null)
                            continue;

                        if (Keys == null || index >= Keys.Count)
                            break;

                        var key = Keys[index];
                        BeginStringifyObjectField(builder, pretty, depth, key);
                        jsonObject.Stringify(depth, builder, pretty);
                        EndStringifyObjectField(builder, pretty);
                    }

                    EndStringifyObjectContainer(builder, pretty, depth);
                    break;
                case Type.Array:
                    if (Count <= 0)
                    {
                        StringifyEmptyArray(builder);
                        break;
                    }

                    BeginStringifyArrayContainer(builder, pretty);
                    foreach (var jsonObject in List)
                    {
                        if (jsonObject == null)
                            continue;

                        BeginStringifyArrayElement(builder, pretty, depth);
                        jsonObject.Stringify(depth, builder, pretty);
                        EndStringifyArrayElement(builder, pretty);
                    }

                    EndStringifyArrayContainer(builder, pretty, depth);
                    break;
                case Type.Bool:
                    StringifyBool(builder);
                    break;
                case Type.Null:
                    StringifyNull(builder);
                    break;
            }
        }

        private void StringifyString(StringBuilder builder)
        {
            builder.AppendFormat("\"{0}\"", EscapeString(StringValue));
        }

        private void BeginStringifyObjectContainer(StringBuilder builder, bool pretty)
        {
            builder.Append("{");

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                builder.Append(Newline);
#endif
        }

        private static void StringifyEmptyObject(StringBuilder builder)
        {
            builder.Append("{}");
        }

        private void BeginStringifyObjectField(StringBuilder builder, bool pretty, int depth, string key)
        {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                for (var j = 0; j < depth; j++)
                    builder.Append(Tab); //for a bit more readability
#endif

            builder.AppendFormat("\"{0}\":", key);
        }

        private void EndStringifyObjectField(StringBuilder builder, bool pretty)
        {
            builder.Append(",");
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                builder.Append(Newline);
#endif
        }

        private void EndStringifyObjectContainer(StringBuilder builder, bool pretty, int depth)
        {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                builder.Length -= 3;
            else
#endif
                builder.Length--;

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty && Count > 0)
            {
                builder.Append(Newline);
                for (var j = 0; j < depth - 1; j++)
                    builder.Append(Tab);
            }
#endif

            builder.Append("}");
        }

        private static void StringifyEmptyArray(StringBuilder builder)
        {
            builder.Append("[]");
        }

        private void BeginStringifyArrayContainer(StringBuilder builder, bool pretty)
        {
            builder.Append("[");
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                builder.Append(Newline);
#endif
        }

        private void BeginStringifyArrayElement(StringBuilder builder, bool pretty, int depth)
        {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                for (var j = 0; j < depth; j++)
                    builder.Append(Tab); //for a bit more readability
#endif
        }

        private void EndStringifyArrayElement(StringBuilder builder, bool pretty)
        {
            builder.Append(",");
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                builder.Append(Newline);
#endif
        }

        private void EndStringifyArrayContainer(StringBuilder builder, bool pretty, int depth)
        {
#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty)
                builder.Length -= 3;
            else
#endif
                builder.Length--;

#if !JSONOBJECT_DISABLE_PRETTY_PRINT
            if (pretty && Count > 0)
            {
                builder.Append(Newline);
                for (var j = 0; j < depth - 1; j++)
                    builder.Append(Tab);
            }
#endif

            builder.Append("]");
        }

        private void StringifyNumber(StringBuilder builder)
        {
            if (IsInteger)
            {
                builder.Append(LongValue.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
#if JSONOBJECT_USE_FLOAT
				if (float.IsNegativeInfinity(floatValue))
					builder.Append(NegativeInfinity);
				else if (float.IsInfinity(floatValue))
					builder.Append(Infinity);
				else if (float.IsNaN(floatValue))
					builder.Append(NaN);
				else
					builder.Append(floatValue.ToString("R", CultureInfo.InvariantCulture));
#else
                if (double.IsNegativeInfinity(DoubleValue))
                    builder.Append(NegativeInfinity);
                else if (double.IsInfinity(DoubleValue))
                    builder.Append(Infinity);
                else if (double.IsNaN(DoubleValue))
                    builder.Append(NaN);
                else
                    builder.Append(DoubleValue.ToString("R", CultureInfo.InvariantCulture));
#endif
            }
        }

        private void StringifyBool(StringBuilder builder)
        {
            builder.Append(BoolValue ? True : False);
        }

        private static void StringifyNull(StringBuilder builder)
        {
            builder.Append(Null);
        }

#if USING_UNITY
        public static implicit operator WWWForm(JSONObject jsonObject)
        {
            var form = new WWWForm();
            var count = jsonObject.Count;
            var list = jsonObject.List;
            var keys = jsonObject.Keys;
            var hasKeys = jsonObject.ObjectType == Type.Object && keys != null && keys.Count >= count;

            for (var i = 0; i < count; i++)
            {
                var key = hasKeys ? keys[i] : i.ToString(CultureInfo.InvariantCulture);
                var element = list[i];
                var val = element.ToString();
                if (element.ObjectType == Type.String)
                    val = val.Replace("\"", "");

                form.AddField(key, val);
            }

            return form;
        }
#endif
        public JSONObject this[int index]
        {
            get { return Count > index ? List[index] : null; }
            set
            {
                if (Count > index)
                    List[index] = value;
            }
        }

        public JSONObject this[string index]
        {
            get { return GetField(index); }
            set { SetField(index, value); }
        }

        public override string ToString()
        {
            return Print();
        }

        public string ToString(bool pretty)
        {
            return Print(pretty);
        }

        public Dictionary<string, string> ToDictionary()
        {
            if (ObjectType != Type.Object)
            {
#if USING_UNITY
                Debug.Log
#else
				Debug.WriteLine
#endif
                    ("Tried to turn non-Object JSONObject into a dictionary");

                return null;
            }

            var result = new Dictionary<string, string>();
            var listCount = Count;
            if (Keys == null || Keys.Count != listCount)
                return result;

            for (var index = 0; index < listCount; index++)
            {
                var element = List[index];
                switch (element.ObjectType)
                {
                    case Type.String:
                        result.Add(Keys[index], element.StringValue);
                        break;
                    case Type.Number:
#if JSONOBJECT_USE_FLOAT
						result.Add(keys[index], element.floatValue.ToString(CultureInfo.InvariantCulture));
#else
                        result.Add(Keys[index], element.DoubleValue.ToString(CultureInfo.InvariantCulture));
#endif

                        break;
                    case Type.Bool:
                        result.Add(Keys[index], element.BoolValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    default:
#if USING_UNITY
                        Debug.LogWarning
#else
						Debug.WriteLine
#endif
                            (string.Format("Omitting object: {0} in dictionary conversion", Keys[index]));
                        break;
                }
            }

            return result;
        }

        public static implicit operator bool(JSONObject jsonObject)
        {
            return jsonObject != null;
        }

#if JSONOBJECT_POOLING
		public static void ClearPool() {
			poolingEnabled = false;
			poolingEnabled = true;
			lock (JSONObjectPool) {
				JSONObjectPool.Clear();
			}
		}

		~JSONObject() {
			lock (JSONObjectPool) {
				if (!poolingEnabled || isPooled || JSONObjectPool.Count >= MaxPoolSize)
					return;

				Clear();
				isPooled = true;
				JSONObjectPool.Enqueue(this);
				GC.ReRegisterForFinalize(this);
			}
		}

		public static int GetJSONObjectPoolSize() {
			return JSONObjectPool.Count;
		}

		public static int GetJSONObjectListPoolSize() {
			return JSONObjectListPool.Count;
		}

		public static int GetStringListPoolSize() {
			return StringListPool.Count;
		}
#endif

        public static List<JSONObject> CreateJSONObjectList()
        {
#if JSONOBJECT_POOLING
			lock (JSONObjectListPool) {
				if (JSONObjectListPool.Count > 0)
					return JSONObjectListPool.Dequeue();
			}
#endif

            return new List<JSONObject>();
        }

        public static List<string> CreateStringList()
        {
#if JSONOBJECT_POOLING
			lock (StringListPool) {
				if (StringListPool.Count > 0)
					return StringListPool.Dequeue();
			}
#endif

            return new List<string>();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public JSONObjectEnumerator GetEnumerator()
        {
            return new JSONObjectEnumerator(this);
        }
    }

    public class JSONObjectEnumerator : IEnumerator
    {
        // Enumerators are positioned before the first element until the first MoveNext() call.
        private int position = -1;
        public JSONObject target;

        public JSONObjectEnumerator(JSONObject jsonObject)
        {
            if (!jsonObject.IsContainer)
                throw new InvalidOperationException("JSONObject must be an array or object to provide an iterator");

            target = jsonObject;
        }

        // ReSharper disable once InconsistentNaming
        public JSONObject Current
        {
            get { return target[position]; }
        }

        public bool MoveNext()
        {
            position++;
            return position < target.Count;
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}