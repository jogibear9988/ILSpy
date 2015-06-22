using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using Microsoft.Win32;
using Mono.Cecil;

namespace DotNetDecompiler
{
    class Program
    {
        private static ReaderParameters readerParameters;

        private static List<string> additionalPaths = new List<string>();

        static void Main(string[] args)
        {
            additionalPaths.Add("D:\\git\\mcc4\\usedExternalProjects\\wpf\\wpfextendedtoolkit");
            
            var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            var pfKey = key.OpenSubKey("SOFTWARE\\Perforce\\Environment").GetValue("P4INSTROOT") as string;

            var perforge = Path.Combine(pfKey, "p4merge.exe");

            List<string> files = new List<string>();

            //Console.ReadLine();

            Console.WriteLine("Compare Disassembler");
            Console.WriteLine("Perforce Path:" + perforge);

            for (int index = 0; index < 2; index++)
            {
                Console.WriteLine("Disassamble File:" + args[index]);
                var f = args[index];
                var dll = f;
                var path = Path.GetDirectoryName(dll);

                var tmp = Path.GetTempFileName();
                files.Add(tmp);

                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(path);
                resolver.AddSearchDirectory(Path.GetDirectoryName(args[2]));
                resolver.AddSearchDirectory(Path.GetDirectoryName(args[3]));
                resolver.ResolveFailure += resolver_ResolveFailure;
                readerParameters = new ReaderParameters {AssemblyResolver = resolver};
                var assembly = AssemblyDefinition.ReadAssembly(args[index], readerParameters);

                var context = new DecompilerContext(assembly.MainModule);
                AstBuilder decompiler = new AstBuilder(context);
                decompiler.AddAssembly(assembly);
                //new Helpers.RemoveCompilerAttribute().Run(decompiler.SyntaxTree);

                using (var s = new FileStream(tmp, FileMode.Create))
                {
                    using (var wr = new StreamWriter(s))
                    {
                        decompiler.GenerateCode(new PlainTextOutput(wr));
                    }
                }
            }

            Process.Start(perforge, string.Join(" ", files.Select(x => "\"" + x + "\"")));
        }

        static AssemblyDefinition resolver_ResolveFailure(object sender, AssemblyNameReference reference)
        {
            foreach (var additionalPath in additionalPaths)
            {
                var file = Path.Combine(additionalPath, reference.Name + ".dll");
                if (File.Exists(file))
                    return AssemblyDefinition.ReadAssembly(file, readerParameters);
            }
            return null;
        }
    }
}
