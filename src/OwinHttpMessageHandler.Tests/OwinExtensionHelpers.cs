namespace OwinHttpMessageHandler.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Owin.Types;

    public static class OwinExtensionHelpers
    {
        // Mostly copy-pasta from Gate as there currently is no equivalent functions in Owin.Types

        static readonly char[] CommaSemicolon = new[] { ',', ';' };

        internal static IDictionary<string, string> ReadForm(this OwinRequest request)
        {
            if (!request.HasFormData() && !request.HasParseableData())
            {
                return ParamDictionary.Parse("");
            }

            var form = request.Dictionary.Get<IDictionary<string, string>>("Gate.Request.Form");
            var thisInput = request.Body;
            var lastInput = request.Dictionary.Get<object>("Gate.Request.Form#input");
            if (form != null && ReferenceEquals(thisInput, lastInput))
            {
                return form;
            }

            var text = request.ReadText();
            form = ParamDictionary.Parse(text);
            request.Set("Gate.Request.Form#input", thisInput);
            request.Set("Gate.Request.Form", form);
            return form;
        }

        internal static bool HasFormData(this OwinRequest request)
        {
            var contentType = request.ContentType();
            return (request.Method == "POST" && string.IsNullOrEmpty(contentType))
                   || contentType == "application/x-www-form-urlencoded"
                   || contentType == "multipart/form-data";
        }

        internal static bool HasParseableData(this OwinRequest request)
        {
            var mediaType = request.MediaType();
            return mediaType == "application/x-www-form-urlencoded"
                   || mediaType == "multipart/form-data";
        }

        internal static string MediaType(this OwinRequest request)
        {
            var contentType = request.ContentType();
            if (contentType == null)
                return null;
            var delimiterPos = contentType.IndexOfAny(CommaSemicolon);
            return delimiterPos < 0 ? contentType : contentType.Substring(0, delimiterPos);
        }

        internal static string ContentType(this OwinRequest owinRequest)
        {
            return owinRequest.GetHeader("Content-Type");
        }

        public static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            object value;
            return dictionary.TryGetValue(key, out value) ? (T)value : default(T);
        }

        public static Stream Write(this Stream stream, string s)
        {
            new StreamWriter(stream).Write(s);
            return stream;
        }

        public static void Set<T>(this IDictionary<string, object> dictionary, string key, T value)
        {
            if (object.Equals(value, default(T)))
            {
                dictionary.Remove(key);
            }
            else
            {
                dictionary[key] = value;
            }
        }

        internal static string ReadText(this OwinRequest request)
        {
            var text = request.Get<string>("Gate.Request.Text");

            var thisInput = request.Body;
            var lastInput = request.Get<object>("Gate.Request.Text#input");

            if (text != null && ReferenceEquals(thisInput, lastInput))
            {
                return text;
            }

            if (thisInput != null)
            {
                if (thisInput.CanSeek)
                {
                    thisInput.Seek(0, SeekOrigin.Begin);
                }
                text = new StreamReader(thisInput).ReadToEnd();
            }

            request.Set("Gate.Request.Text#input", thisInput);
            request.Set("Gate.Request.Text", text);
            return text;
        }

        internal static void Write(this OwinResponse response, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            response.Body.Write(bytes, 0, bytes.Length);
        }

        private class ParamDictionary : IDictionary<string, string>
        {
            static readonly char[] DefaultParamSeparators = new[] { '&', ';' };
            static readonly char[] ParamKeyValueSeparator = new[] { '=' };
            static readonly char[] LeadingWhitespaceChars = new[] { ' ' };

            public static IEnumerable<KeyValuePair<string, string>> ParseToEnumerable(string queryString, char[] delimiters)
            {
                var items = (queryString ?? "").Split(delimiters ?? DefaultParamSeparators, StringSplitOptions.RemoveEmptyEntries);
                var rawPairs = items.Select(item => item.Split(ParamKeyValueSeparator, 2, StringSplitOptions.None));
                var pairs = rawPairs.Select(pair => new KeyValuePair<string, string>(
                                                        Uri.UnescapeDataString(pair[0]).Replace('+', ' ').TrimStart(LeadingWhitespaceChars),
                                                        pair.Length < 2 ? "" : Uri.UnescapeDataString(pair[1]).Replace('+', ' ')));
                return pairs;
            }

            public static IDictionary<string, string> Parse(string queryString, char[] delimiters = null)
            {
                var d = ParseToEnumerable(queryString, delimiters)
                    .GroupBy(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => string.Join(",", g.ToArray()), StringComparer.OrdinalIgnoreCase);

                return new ParamDictionary(d);
            }

            readonly IDictionary<string, string> _impl;



            ParamDictionary(IDictionary<string, string> impl)
            {
                _impl = impl;
            }

            IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
            {
                return _impl.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _impl.GetEnumerator();
            }

            void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
            {
                _impl.Add(item);
            }

            void ICollection<KeyValuePair<string, string>>.Clear()
            {
                _impl.Clear();
            }

            bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
            {
                return _impl.Contains(item);
            }

            void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                _impl.CopyTo(array, arrayIndex);
            }

            bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
            {
                return _impl.Remove(item);
            }

            int ICollection<KeyValuePair<string, string>>.Count
            {
                get { return _impl.Count; }
            }

            bool ICollection<KeyValuePair<string, string>>.IsReadOnly
            {
                get { return _impl.IsReadOnly; }
            }

            bool IDictionary<string, string>.ContainsKey(string key)
            {
                return _impl.ContainsKey(key);
            }

            void IDictionary<string, string>.Add(string key, string value)
            {
                _impl.Add(key, value);
            }

            bool IDictionary<string, string>.Remove(string key)
            {
                return _impl.Remove(key);
            }

            bool IDictionary<string, string>.TryGetValue(string key, out string value)
            {
                return _impl.TryGetValue(key, out value);
            }

            string IDictionary<string, string>.this[string key]
            {
                get
                {
                    string value;
                    return _impl.TryGetValue(key, out value) ? value : default(string);
                }
                set { _impl[key] = value; }
            }

            ICollection<string> IDictionary<string, string>.Keys
            {
                get { return _impl.Keys; }
            }

            ICollection<string> IDictionary<string, string>.Values
            {
                get { return _impl.Values; }
            }
        }
    }
}