namespace Owin.Tests
{
    using System.Collections.Generic;
    using System.IO;

    public static class ExtensionHelpers
    {
        public static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            return (T) dictionary[key];
        }

        public static Stream Write(this Stream stream, string s)
        {
            new StreamWriter(stream).Write(s);
            return stream;
        }
    }
}