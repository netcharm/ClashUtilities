using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ClashYamlUpdate
{
    class ConfigYaml<T>
    {
        private static string[] LINEBREAK = new string[] { Environment.NewLine, "\n\r", "\r\n", "\n", "\r" };

        public virtual T FromFile(string file)
        {
            T result = default(T);
            if (File.Exists(file))
            {
                try
                {
                    result = FromLines(File.ReadAllLines(file, Encoding.UTF8).ToList());
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public virtual async Task<T> FromFileAsync(string file)
        {
            T result = default(T);
            if (File.Exists(file))
            {
                try
                {
                    var yaml = new Deserializer();
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        result = await FromStreamAsync(fs);
                    }
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public virtual T FromStream(Stream stream)
        {
            T result = default(T);
            if (stream is Stream && stream.CanRead && stream.Length > 0)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var bytes = new byte[stream.Length];
                    var count = stream.Read(bytes, 0, (int)stream.Length);
                    var contents = Encoding.UTF8.GetString(bytes).Split(LINEBREAK, StringSplitOptions.RemoveEmptyEntries).ToList();
                    result = FromLines(contents);
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public virtual async Task<T> FromStreamAsync(Stream stream)
        {
            T result = default(T);
            if (stream is Stream && stream.CanRead && stream.Length > 0)
            {
                try
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var bytes = new byte[stream.Length];
                    var count = await stream.ReadAsync(bytes, 0, (int)stream.Length);
                    var contents = Encoding.UTF8.GetString(bytes).Split(LINEBREAK, StringSplitOptions.RemoveEmptyEntries).ToList();
                    result = FromLines(contents);
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public virtual T FromLines(IList<string> lines)
        {
            T result = default(T);
            try
            {
                if (lines is IList<string> && lines.Count > 0)
                {
                    var yaml = new Deserializer();
                    if (!lines[0].StartsWith("#")) lines.Insert(0, $"# YAML Starting...{Environment.NewLine}");
                    result = yaml.Deserialize<T>(string.Join(Environment.NewLine, lines));
                }
            }
            catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            return (result);
        }

        public ConfigYaml(string file)
        {
            FromFile(file);
        }

        public ConfigYaml(Stream stream)
        {
            FromStream(stream);
        }

        public ConfigYaml(IList<string> lines)
        {
            FromLines(lines);
        }
    }
}
