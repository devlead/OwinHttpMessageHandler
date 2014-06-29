namespace System.Net.Http
{
    using System.Collections.Generic;
    using System.IO;

    public static class OwinExtensionHelpers
    {
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
    }
}