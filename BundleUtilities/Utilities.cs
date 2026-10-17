using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Reflection;

namespace BundleUtilities
{
    public class LogWriter
    {
        private string m_exePath = string.Empty;

        public LogWriter(string logMessage)
        {
            LogWrite(logMessage);
        }

        public void LogWrite(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "log.txt"))
                {
                    Log(logMessage, w);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public void Log(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.WriteLine("{0} {1} : {2}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString(), logMessage);
            }
            catch (Exception ex)
            {
            }
        }
    }

    public static class Utilities
    {
        public static ulong CalcLookup8(string text)
        {
            byte[] message = Encoding.ASCII.GetBytes(text);
            var hashValue = Lookup8.Hash(message, (ulong)message.Length, 0xABCDEF0011223344);

            return hashValue;
        }

        public static bool IsValidPath(string path)
        {
            return !string.IsNullOrEmpty(path);
        }

        public static bool FileExists(string path)
        {
            return IsValidPath(path) && File.Exists(path);
        }

        public static byte[] Pad(this byte[] self, int bytes)
        {
            if (self.Length == bytes)
                return self;

            if (self.Length > bytes)
                throw new ArgumentException("Cannot pad to " + bytes);

            byte[] result = new byte[bytes];

            for (int i = 0; i < self.Length; i++)
            {
                result[i] = self[i];
            }

            return result;
        }

        /// <summary>
        /// Converts the specified text to an object.
        /// </summary>
        /// <typeparam name="T">The type of the output object</typeparam>
        /// <param name="s">The text representation of the object to convert.</param>
        /// <param name="forceHex">Parse the input string as a hexadecimal number even if the input string is not prefixed with "0x"</param>
        /// <param name="value">An output System.Object that represents the converted text.</param>
        /// <exception cref="NotSupportedException">The string cannot be converted into the appropriate object.</exception>
        public static void Parse<T>(string s, bool forceHex, out T value)
        {
            if (forceHex && !s.StartsWith("0x"))
                s = "0x" + s;
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            value = (T)converter.ConvertFromString(s);
        }
    }
}
