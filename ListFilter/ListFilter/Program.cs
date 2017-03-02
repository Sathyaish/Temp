using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ListFilter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var file = @"C:\Sathyaish\temp\WindowsAzureChannelUploads.txt";
                var contains = @"(?i)document\s*?db.*";
                var outputFile = @"C:\Sathyaish\temp\WindowsAzure-Videos-DocumentDb.txt";

                var query = from string line in File.ReadAllLines(file)
                            where !string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line)
                            && Regex.IsMatch(line, contains)
                            select line;

                if (query == null) return;

                var count = query.Count();

                File.WriteAllLines(outputFile, query.ToArray());

                foreach (var line in query)
                    Console.WriteLine(line);

                Console.WriteLine($"\n\n{count} matching lines found. New file: '{outputFile}'");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
