using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AtlusFileSystemLibrary.FileSystems.PAK;
using AtlusFileSystemLibrary.Common.IO;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Emit;
using AtlusFileSystemLibrary;

namespace MassPACReplacer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PAC Extractor/Replacer Thingy by ShrineFox (using AtlusFileSystemLibrary by TGEnigma)\n-----------------------------------------\n");
            Console.WriteLine("When inputFile is specified, contents of PACs in folderPath will be replaced.\nOtherwise, files matching searchPattern will be extracted to outputPath.\n\n");

            string folderPath = "";
            if (args.Length > 0)
                folderPath = args[0];
            string searchPattern = "";
            if (args.Length > 1)
                searchPattern = args[1];
            string inputFile = "";
            if (args.Length > 2)
                inputFile = args[2];

            Console.WriteLine($"args[0] folderPath: {folderPath}\nargs[1] searchPattern {searchPattern}\nargs[2] inputFile: {inputFile}\nPress any key to continue.\n");
            Console.ReadKey();
            
            if (Directory.Exists(folderPath))
            {
                foreach (var file in Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly).Where(x => x.ToLower().EndsWith(".pac") || x.ToLower().EndsWith(".pak") || x.ToLower().EndsWith(".arc") || x.ToLower().EndsWith(".bin")))
                {
                    if (inputFile == "")
                    {
                        string outputPath = Path.Combine(folderPath, $"Extracted {searchPattern} files");
                        ExtractPAC(file, outputPath, searchPattern);
                    }
                    else if (File.Exists(inputFile))
                    {
                        string outputPath = Path.Combine(folderPath, $"PACs with replaced {searchPattern} files");
                        InjectPAC(file, outputPath, searchPattern, inputFile);
                    }
                }
            }

            Console.WriteLine("Done");
        }

        private static void InjectPAC(string file, string outputPath, string searchPattern, string inputFile)
        {
            PAKFileSystem pak = new PAKFileSystem();
            List<string> pakFiles = new List<string>();
            if (PAKFileSystem.TryOpen(file, out pak))
            {
                pakFiles = pak.EnumerateFiles().ToList();
                PAKFileSystem newPak = pak;

                Console.WriteLine($"Replacing files ending with  \"{searchPattern}\" in {Path.GetFileName(file)} with \"{Path.GetFileName(inputFile)}\"...");
                foreach (var pakFile in pakFiles)
                {
                    if (pakFile.EndsWith(searchPattern))
                    {
                        string normalizedFilePath = pakFile.Replace("../", ""); //Remove backwards relative path
                        
                        Console.WriteLine($"Replacing {normalizedFilePath}");
                        newPak.AddFile(normalizedFilePath.Replace("\\", "/"), inputFile, ConflictPolicy.Replace);
                    }
                }
                newPak.Save(Path.Combine(outputPath, Path.GetFileName(file)));
            }
            else
            {
                Console.WriteLine($"Failed to open {Path.GetFileName(file)}. Skipping...");
            }
        }

        public static void ExtractPAC(string file, string outputPath, string searchPattern)
        {
            PAKFileSystem pak = new PAKFileSystem();
            if (PAKFileSystem.TryOpen(file, out pak))
            {
                Console.WriteLine($"Extracting all files ending with \"{searchPattern}\" in {Path.GetFileName(file)}...");
                List<string> pakFiles = new List<string>();
                foreach (var pakFile in pak.EnumerateFiles())
                {
                    if (pakFile.EndsWith(searchPattern))
                    {
                        string normalizedFilePath = pakFile.Replace("../", ""); //Remove backwards relative path
                        string output = outputPath + Path.DirectorySeparatorChar + Path.GetFileName(file) + "_extracted_" + Path.GetFileName(normalizedFilePath);

                        using (var stream = FileUtils.Create(output))
                        using (var inputStream = pak.OpenFile(pakFile))
                        {
                            inputStream.CopyTo(stream);
                            pakFiles.Add(output);
                            Console.WriteLine($"Extracted {Path.GetFileName(output)}");
                        }
                    }
                }

                //Extract stuff in PACs contained in this PAC
                foreach (var pakFile in pakFiles.Where(x => x.ToLower().EndsWith(".pac") || x.ToLower().EndsWith(".pak") || x.ToLower().EndsWith(".arc") || x.ToLower().EndsWith(".bin")))
                {
                    ExtractPAC(pakFile, outputPath, searchPattern);
                }
            }
            else
            {
                Console.WriteLine($"Failed to open {Path.GetFileName(file)}. Skipping...");
            }
        }
    }
}
