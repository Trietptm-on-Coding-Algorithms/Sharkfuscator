﻿using Fclp;
using Sharkfuscator.Protections;
using Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sharkfuscator
{

    /// <summary>
    /// For command-line arguments
    /// </summary>
    public class ApplicationArguments
    {
        public string input_file { get; set; }
        public string output_file { get; set; }
    }

    /// <summary>
    /// Parameter management
    /// </summary>
    class Program
    {
        static string plugin_folder = "plugins\\";
        static PluginLoader plugin_loader = new PluginLoader(plugin_folder);
        static List<iProtection> protections = new List<iProtection>();
        static ApplicationArguments arguments = new ApplicationArguments();

        static void Main(string[] args)
        {
            PrintHello();
            LoadProtections();

            var parser = new FluentCommandLineParser();

            parser.Setup<string>('i', "input")
             .Callback(value => arguments.input_file = value)
             .Required();

            parser.Setup<string>('o', "output")
             .Callback(value => arguments.output_file = value);

            foreach (var protection in protections)
            {
                parser.Setup<bool>(protection.command_short, protection.command_long)
                 .Callback(value => protection.enabled = value)
                 .SetDefault(protection.enabled_default);
            }

            var result = parser.Parse(args);

            if (result.HasErrors == false)
            {
                App(arguments);
            }
            else
            {
                PrintHelp();
            }
        }

        /// <summary>
        /// Main program functionality
        /// </summary>
        /// <param name="o"></param>
        static void App(object o)
        {
            var arguments = (ApplicationArguments)o;

            if (arguments.output_file == string.Empty || arguments.output_file == null)
                arguments.output_file = arguments.input_file + ".shark.exe";

            using (MemoryStream base_stream = new MemoryStream())
            using (FileStream fs = new FileStream(arguments.input_file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                fs.CopyTo(base_stream);

                /*
                 * Perform pre-protections
                 */
                PerformProtections(ProtectorState.Pre, arguments.output_file, base_stream);

                /*
                 * Perform during-protections
                 */
                PerformProtections(ProtectorState.During, arguments.output_file, base_stream);

                /*
                 * Write output
                 */
                Console.WriteLine("Writing " + arguments.output_file + "..");
                if (File.Exists(arguments.output_file)) File.Delete(arguments.output_file);
                using (FileStream fs_out = new FileStream(arguments.output_file, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    base_stream.CopyTo(fs_out);
                    fs_out.Flush();
                }

                /*
                 * Perform post-protections
                 */
                PerformProtections(ProtectorState.Post, arguments.output_file, base_stream);


                Console.WriteLine("Done");
            }
        }

        static void PerformProtections(ProtectorState state, string output_filename, Stream stream)
        {
            foreach (var protection in protections)
            {
                if (protection.enabled)
                {
                    if (protection.Protect(state, output_filename, stream))
                        Console.WriteLine(protection.init_message);
                    stream.Position = 0;
                }
            }
        }

        static void LoadProtections()
        {
            protections.Add(new EOF_Anti_Tamper());
            protections.Add(new DOSModifier());
            plugin_loader.LoadPlugins(protections);
        }

        /// <summary>
        /// Print hello
        /// </summary>
        static void PrintHello()
        {
            Console.Clear();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(" ____  _                _     __                     _             ");
            sb.AppendLine("/ ___|| |__   __ _ _ __| | __/ _|_   _ ___  ___ __ _| |_ ___  _ __ ");
            sb.AppendLine("\\___ \\| '_ \\ / _` | '__| |/ / |_| | | / __|/ __/ _` | __/ _ \\| '__|");
            sb.AppendLine(" ___) | | | | (_| | |  |   <|  _| |_| \\__ \\ (_| (_| | || (_) | |   ");
            sb.AppendLine("|____/|_| |_|\\__,_|_|  |_|\\_\\_|  \\__,_|___/\\___\\__,_|\\__\\___/|_|   ");
            sb.AppendLine("                                                                   ");
            sb.AppendLine("Project repository can be found at https://github.com/Rottweiler/Sharkfuscator/");
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Print help
        /// </summary>
        static void PrintHelp()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Invalid usage!!1");
            sb.AppendLine("Here is how you do it, scrub: ./Sharkfuscator.exe -i INPUT_FILE.exe -o OUTPUT_FILE.exe");
            Console.WriteLine(sb.ToString());
        }
    }
}
