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
        static bool simple_stdin = true;

        static void Main(string[] args)
        {
            var show_help = false;
            var template_yaml = Path.Combine(AppPath, "default.yaml");
            var source_yaml = Path.Combine(AppPath, "source.yaml");
            var target_yaml = Path.Combine(AppPath, "target.yaml");

            var opts = new OptionSet()
            {
                { "i|input=", "Source Clash Config YAML", v => { if (!Console.IsInputRedirected) source_yaml = Path.GetFullPath(v); } },
                { "o|output=", "Target Clash Config YAML", v => { target_yaml = Path.GetFullPath(v); } },
                { "t|template=", "Template Clash Config YAML", v => { template_yaml = Path.GetFullPath(v); } },

                { "h|?|help", "Help", v => { show_help = v != null; } },
            };
            var extras = opts.Parse(args);

            if (!Console.IsInputRedirected && (show_help || args.Length == 0))
            {
                ShowHelp(opts);
                return;
            }

            if (Console.IsInputRedirected)
            {
                var flist = GetStdIn().GetAwaiter().GetResult();
                foreach (var f in flist)
                {
                    Console.WriteLine($"FileName = {f}");
                }
                if (flist.Count() > 0) source_yaml = flist.Last();
            }

            Console.WriteLine(source_yaml);
            Console.WriteLine(target_yaml);
            Console.WriteLine(template_yaml);

            if (File.Exists(template_yaml) && File.Exists(source_yaml))
            {
                var clash_template = Clash.ConfigYaml.FromFile(template_yaml);
                var clash_source = Clash.ConfigYaml.FromFile(source_yaml);

                if (clash_template is Clash.ConfigYaml && clash_source is Clash.ConfigYaml)
                {
                    var clash_target = clash_template.MergeTo(clash_source);
                    clash_target.ToFile(target_yaml);
                }
            }
        }

        static void ShowHelp(OptionSet opts)
        {
            Console.WriteLine($"Usage: {AppName} [OPTIONS]+");
            Console.WriteLine("Options:");
            opts.WriteOptionDescriptions(Console.Out);
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
