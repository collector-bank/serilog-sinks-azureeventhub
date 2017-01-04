// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Serilog.Formatting
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Serilog.Events;
    using Serilog.Formatting.Json;

    public class ScalarValueTypeSuffixJsonFormatter : JsonFormatter
    {
        private readonly Dictionary<Type, string> _suffixes = new Dictionary<Type, string>
        {
            [typeof(bool)] = "_b",

            [typeof(byte)] = "_i",
            [typeof(sbyte)] = "_i",
            [typeof(short)] = "_i",
            [typeof(ushort)] = "_i",
            [typeof(int)] = "_i",
            [typeof(uint)] = "_i",
            [typeof(long)] = "_i",
            [typeof(ulong)] = "_i",

            [typeof(float)] = "_d",
            [typeof(double)] = "_d",
            [typeof(decimal)] = "_d",

            [typeof(DateTime)] = "_t",
            [typeof(DateTimeOffset)] = "_t",
            [typeof(TimeSpan)] = "_ts",

            [typeof(string)] = "_s",
        };

        public ScalarValueTypeSuffixJsonFormatter(bool omitEnclosingObject = false, string closingDelimiter = null, bool renderMessage = false, IFormatProvider formatProvider = null)
            : base(omitEnclosingObject, closingDelimiter, renderMessage, formatProvider)
        {
        }

        public void AddSuffix(Type type, string suffix)
        {
            _suffixes[type] = suffix;
        }

        protected override void WriteJsonProperty(string name, object value, ref string precedingDelimiter, TextWriter output)
        {
            var scalarValue = value as ScalarValue;

            if (scalarValue != null)
            {
                if (_suffixes.ContainsKey(scalarValue.Value.GetType()))
                    name += _suffixes[scalarValue.Value.GetType()];
                else
                    name += _suffixes[typeof(string)];
            }

            base.WriteJsonProperty(name, value, ref precedingDelimiter, output);
        }
    }
}