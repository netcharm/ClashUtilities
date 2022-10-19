using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace ClashYamlUpdate
{
    public partial class AppConfigYaml
    {
        private static string[] LINEBREAK = new string[] { Environment.NewLine, "\n\r", "\r\n", "\n", "\r" };

        public static T FromFile<T>(string file)
        {
            T result = default(T);
            if (File.Exists(file))
            {
                try
                {
                    result = FromLines<T>(File.ReadAllLines(file, Encoding.UTF8).ToList());
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public static async Task<T> FromFileAsync<T>(string file)
        {
            T result = default(T);
            if (File.Exists(file))
            {
                try
                {
                    var yaml = new Deserializer();
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        result = await FromStreamAsync<T>(fs);
                    }
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public static T FromStream<T>(Stream stream)
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
                    result = FromLines<T>(contents);
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public static async Task<T> FromStreamAsync<T>(Stream stream)
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
                    result = FromLines<T>(contents);
                }
                catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            }
            return (result);
        }

        public static T FromLines<T>(IList<string> lines)
        {
            T result = default(T);
            try
            {
                if (lines is IList<string> && lines.Count > 0)
                {
                    var yaml = new Deserializer();
                    if (!lines[0].StartsWith("#")) lines.Insert(0, $"# YAML Starting...{Environment.NewLine}");
                    result = yaml.Deserialize<T>(string.Join(Environment.NewLine, lines));
                    (result as AppConfigYaml).CleanUp();
                }
            }
            catch (Exception ex) { Console.Out.WriteLine(ex.Message); }
            return (result);
        }

        public static AppConfigYaml FromFile(string file)
        {
            return (FromFile<AppConfigYaml>(file));
        }

        public static async Task<AppConfigYaml> FromFileAsync(string file)
        {
            return (await FromFileAsync<AppConfigYaml>(file));
        }

        public static AppConfigYaml FromStream(Stream stream)
        {
            return (FromStream<AppConfigYaml>(stream));
        }

        public static async Task<AppConfigYaml> FromStreamAsync(Stream stream)
        {
            return (await FromStreamAsync<AppConfigYaml>(stream));
        }

        public static AppConfigYaml FromLines(IList<string> lines)
        {
            return (FromLines<AppConfigYaml>(lines).CleanUp());
        }

        private string ReplaceRegex(string text)
        {
            var result = text;
            if(!string.IsNullOrEmpty(text))
            if (Regex.IsMatch(text, @"^/(.*?)/i?$", RegexOptions.IgnoreCase))
            {
                result = text.Trim('/');
            }
            else
            {
                result = Regex.Replace(text, @"([\(\)\{\}\[\]\*\?\.\-\,])", m => $"\\{m.Value}", RegexOptions.IgnoreCase);
            }
            return (result);
        }

        private AppConfigYaml CleanUp()
        {
            if (ReplaceList is Dictionary<string, string>)
            {
                var rl = new Dictionary<string, string>();
                foreach (var replace in ReplaceList.ToList())
                {
                    var key = ReplaceRegex(replace.Key);
                    rl[key] = replace.Value;
                }
                ReplaceList = rl;
            }

            if (RemoveList is List<string>)
            {
                var rl = new List<string>();
                foreach (var remove in RemoveList.ToList())
                {
                    var value = ReplaceRegex(remove);
                    rl.Add(value);
                }
                RemoveList = rl;
            }
            return (this);
        }

        [YamlMember(Alias = "replace", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public Dictionary<string, string> ReplaceList { get; set; } = new Dictionary<string, string>();

        [YamlMember(Alias = "remove", DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public List<string> RemoveList { get; set; } = new List<string>();
    }

}
