using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Serilog.Events;
using Serilog.Formatting.Json;

namespace Collector.Serilog.Sinks.AzureEventHub
{
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

        public ScalarValueTypeSuffixJsonFormatter(bool omitEnclosingObject = false, string closingDelimiter = null, bool renderMessage = true, IFormatProvider formatProvider = null)
            : base(omitEnclosingObject, closingDelimiter, renderMessage, formatProvider)
        {
        }

        public void AddSuffix(Type type, string suffix)
        {
            _suffixes[type] = suffix;
        }

        protected override void WriteJsonProperty(string name, object value, ref string precedingDelimiter, TextWriter output)
        {
            base.WriteJsonProperty(DotEscapeFieldName(name + GetSuffix(value)), value, ref precedingDelimiter, output);
        }

        protected override void WriteDictionary(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> elements, TextWriter output)
        {
            var dictionary = elements.ToDictionary(
                pair => new ScalarValue(DotEscapeFieldName(pair.Key.Value + GetSuffix(pair.Value))),
                pair => pair.Value);

            var readOnlyDictionary = new ReadOnlyDictionary<ScalarValue, LogEventPropertyValue>(dictionary);

            base.WriteDictionary(readOnlyDictionary, output);
        }

        protected virtual string DotEscapeFieldName(string value)
        {
            return value?.Replace('.', '/');
        }

        private string GetSuffix(object value)
        {
            if (value is ScalarValue scalarValue)
            {
                if (scalarValue.Value != null && _suffixes.ContainsKey(scalarValue.Value.GetType()))
                    return _suffixes[scalarValue.Value.GetType()];
                return _suffixes[typeof(string)];
            }

            return string.Empty;
        }
    }
}