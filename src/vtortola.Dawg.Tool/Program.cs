using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace vtortola
{
    // https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create
    public class Program
    {
        public static void Main(string[] args)
        {
            void ThrowInstructions()
                => throw new InvalidOperationException("You must indicate if 'build' or 'unbuild'");

            if(!Console.IsInputRedirected)
                throw new InvalidOperationException("You must redirect the input to this application.");
            
            if(args?.Length != 1)
                ThrowInstructions();

            switch (args[0].ToLowerInvariant())
            {
                case "build":
                {
                    var words = GetLines().ToHashSet();
                    var wdawg = Dawg.Create(words);
                    Dawg.Verify(words, wdawg);
                    Dawg.Write(wdawg, Console.OpenStandardOutput());
                    break;
                }
                case "unbuild":
                {
                    var rdawg = Dawg.Read(Console.OpenStandardInput());
                    var isFirst = true;
                    using var writer = new StreamWriter(Console.OpenStandardOutput());
                    foreach (var word in rdawg)
                    {
                        if (!isFirst)
                            writer.WriteLine();
                        isFirst = false;
                        writer.Write(word);
                    }
                    break;
                }
                default:
                {
                    ThrowInstructions();
                    break;
                }
            }
        }

        static IEnumerable<string> GetLines()
        {
            using var reader = new StreamReader(Console.OpenStandardInput());
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if(!string.IsNullOrWhiteSpace(line))
                    yield return line;
            }
        }
    }
}