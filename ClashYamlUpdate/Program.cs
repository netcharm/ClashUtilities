using Mono.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashYamlUpdate
{
    class Program
    {
        static string AppName = Path.GetFileName(AppDomain.CurrentDomain.FriendlyName);
        static string AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        static string WorkPath = Directory.GetCurrentDirectory();

        static bool simple_stdin = true;

        static void Main(string[] args)
        {
            var show_help = false;

            var config_yaml = Path.Combine(AppPath, $"{Path.GetFileNameWithoutExtension(AppName)}.config.yaml");
            var appconfig = AppConfigYaml.FromFile(config_yaml);

            var template_yaml = string.IsNullOrEmpty(appconfig.Template_Yaml) ? Path.Combine(WorkPath, "default.yaml") : FullPath(appconfig.Template_Yaml);
            var source_yaml = string.IsNullOrEmpty(appconfig.Source_Yaml) ? Path.Combine(WorkPath, "source.yaml") : FullPath(appconfig.Source_Yaml);
            var target_yaml = string.IsNullOrEmpty(appconfig.Target_Yaml) ? Path.Combine(WorkPath, "target.yaml") : FullPath(appconfig.Target_Yaml);
            var copyto_path = string.IsNullOrEmpty(appconfig.CopyTo_Path) ? string.Empty : FullPath(appconfig.CopyTo_Path);

            var opts = new OptionSet()
            {
                { "i|input=", "Source Clash Config YAML {FILE}", v => { if (!Console.IsInputRedirected && v != null) source_yaml = FullPath(v); } },
                { "o|output=", "Target Clash Config YAML {FILE}", v => { if (v != null) target_yaml = FullPath(v); } },
                { "t|template=", "Template Clash Config YAML {FILE}", v => { if (v != null) template_yaml = FullPath(v); } },
                { "c|copyto=", "Copy Target Clash Config YAML to {PATH}", v => { if (v != null) copyto_path = FullPath(v); } },

                { "h|?|help", "Help", v => { show_help = v != null; } },
            };
            var extras = args is string[] ? opts.Parse(args) : new List<string>();

            if (!Console.IsInputRedirected && (show_help || (args is string[] && args.Length == 0)))
            {
                ShowHelp(opts);
                return;
            }

            if (Console.IsInputRedirected)
            {
                var flist = GetStdIn().GetAwaiter().GetResult();
                if (flist.Count() > 0) source_yaml = flist.Last();
                //foreach (var f in flist)
                //{
                //    Console.WriteLine($"FileName = {f}");
                //}
            }

            if (!string.IsNullOrEmpty(template_yaml)) template_yaml = FullPath(template_yaml);
            if (!string.IsNullOrEmpty(source_yaml)) source_yaml = FullPath(source_yaml);
            if (!string.IsNullOrEmpty(target_yaml)) target_yaml = FullPath(target_yaml);
            if (!string.IsNullOrEmpty(copyto_path)) copyto_path = FullPath(copyto_path);

            if (File.Exists(template_yaml) && File.Exists(source_yaml))
            {
                Console.WriteLine("=".PadRight(72, '='));
                Console.WriteLine($"{"Template File".PadRight(13)} : {template_yaml}");
                Console.WriteLine($"{"Source File".PadRight(13)} : {source_yaml}");
                Console.WriteLine($"{"Target File".PadRight(13)} : {target_yaml}");
                if (!string.IsNullOrEmpty(copyto_path) && Directory.Exists(copyto_path))
                    Console.WriteLine($"{"CopyTo Path".PadRight(13)} : {copyto_path}");

                var clash_template = Clash.ClashConfigYaml.FromFile(template_yaml);
                var clash_source = Clash.ClashConfigYaml.FromFile(source_yaml);

                if (clash_template is Clash.ClashConfigYaml && clash_source is Clash.ClashConfigYaml)
                {
                    Console.WriteLine("-".PadRight(72, '-'));
                    Console.WriteLine($"Merge \"{Path.GetFileName(template_yaml)}\" And \"{Path.GetFileName(source_yaml)}\" To \"{Path.GetFileName(target_yaml)}\" ...");

                    var clash_target = clash_template.MergeTo(clash_source);
                    if (appconfig is AppConfigYaml)
                    {
                        clash_target = clash_target.CleanUp(appconfig.RemoveList, appconfig.ReplaceList);
                    }
                    clash_target.ToFile(target_yaml);

                    if (!string.IsNullOrEmpty(copyto_path) && Directory.Exists(copyto_path))
                    {
                        Console.WriteLine($"Copy \"{target_yaml}\" To \"{copyto_path}\" ...");
                        File.Copy(target_yaml, Path.Combine(copyto_path, Path.GetFileName(target_yaml)), true);
                    }
                }
                Console.WriteLine("=".PadRight(72, '='));
            }
        }

        static void ShowHelp(OptionSet opts)
        {
            Console.WriteLine($"Usage: {AppName} [OPTIONS]+");
            Console.WriteLine("Options:");
            opts.WriteOptionDescriptions(Console.Out);
        }

        static string FullPath(string path)
        {
            var result = path;
            if (!string.IsNullOrEmpty(result)) result = Path.GetFullPath(Environment.ExpandEnvironmentVariables(result));
            return (result);
        }

        static async Task<IEnumerable<string>> GetStdIn()
        {
            var result = new List<string>();
            var line_break = new string[] { Environment.NewLine, "\r", "\n", "\r\n", "\n\r" };

            if (simple_stdin)
            {
                var contents = await Console.In.ReadToEndAsync();
                if (contents.Length > 0)
                    result = contents.Trim().Split(line_break, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                using (var std_in = Console.OpenStandardInput())
                {
                    if (std_in.CanRead)
                    {
                        //std_in.ReadTimeout = 250;
                        var buf_len = 512*1024;
                        var bytes = new byte[buf_len];
                        var count = await std_in.ReadAsync(bytes, 0, buf_len);
                        if (count > 0)
                        {
                            var contents = Encoding.Default.GetString(bytes.Take(count).ToArray());
                            result = contents.Split(line_break, StringSplitOptions.RemoveEmptyEntries).ToList();
                        }
                    }
                }
            }
            return (result);
        }

    }
}
