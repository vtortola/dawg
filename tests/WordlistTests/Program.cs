using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using vtortola;

namespace WordlistTests
{
    class Program
    {
        static void Main(string[] args)
        {
            // https://boardgames.stackexchange.com/questions/38366/latest-collins-scrabble-words-list-in-text-file
            // https://github.com/dwyl/english-words
            // http://wordlist.aspell.net/
            // http://www.umich.edu/~archive/linguistics/texts/lexica/
            // http://www.gwicks.net/dictionaries.htm
            // https://www.keithv.com/software/wlist/
            // http://www.mieliestronk.com/wordlist.html
            // https://www.webfeud.com/Info/Dictionaries
            // https://www.wordgamedictionary.com/sowpods/download/sowpods.txt
            // https://github.com/luckytoilet/scrabble-ai
            // https://ipranges.opera.com/res/dictionary/

            var files = Directory
                .GetFiles("WordLists")
                .OrderBy(x => x);

            var reports = new List<FileReport>();
            foreach (var file in files)
            {
                try
                {
                    reports.Add(TestWordsFile(file));
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR {e.GetType().Name}: { e.Message}");
                    Console.WriteLine(e.StackTrace);
                    Console.ResetColor();
                    return;
                }
            }

            PrintReports(reports);

            Console.WriteLine("END");
        }

        // https://stackoverflow.com/questions/605621/how-to-get-object-size-in-memory
        static long EstimateObjectSize(object o)
        {
            using Stream s = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(s, o);
            return s.Length;
        }

        static long CompressedFileSize(string file, string tempFile)
        {
            using var writer = File.OpenWrite(tempFile);
            using var gzip = new GZipStream(writer, CompressionMode.Compress);
            using var reader = File.OpenRead(file);
            reader.CopyTo(gzip);
            reader.Flush();
            gzip.Flush();
            writer.Flush();
            
            return new FileInfo(tempFile).Length;
        }
        
        static long DecompressFileSize(string tempFile)
        {
            using var reader = File.OpenRead(tempFile);
            using var writer = new MemoryStream();
            using var gzip = new GZipStream(reader, CompressionMode.Decompress);
            gzip.CopyTo(writer);

            return new FileInfo(tempFile).Length;
        }

        static FileReport TestWordsFile(string fileName)
        {
            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.WaitForPendingFinalizers();

            var report = new FileReport();
            report.FileName = Path.GetFileName(fileName);
            var sw = new Stopwatch();
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(fileName);
            var words = File.ReadAllLines(fileName)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(w => w.ToLowerInvariant())
                .Distinct()
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            report.WordCount = words.Length;
            Console.WriteLine($"Words: {words.Length:n0}");
            Console.ResetColor();
            
            var hash = new HashSet<string>(words);
            
            PrintPhase("Creating DAWG...");
            sw.Start();
            var dawg = Dawg.Create(words);
            sw.Stop();
            PrintOK(sw);
            report.BuildDawgTime = sw.Elapsed;
            report.NodeCount = dawg.NodeCount;
            Console.WriteLine($"Nodes {dawg.NodeCount:n0}");
            
            report.ArraySize = EstimateObjectSize(words);
            report.HashSize = EstimateObjectSize(hash);
            
            Console.WriteLine($"HashSize:{report.HashSize/1024.0:n2} kB  ArraySize:{report.ArraySize/1024.0:n2} kB.");

            sw.Restart();
            PrintPhase("Verifying... ");
            Dawg.Verify(words, dawg);
            sw.Stop();
            PrintOK(sw);
            
            PrintPhase("Serializing/Deserializing the DAWG... \n");
            var dawgFile = Path.GetTempFileName();
            var gzipFile = Path.GetTempFileName();

            Console.Write(" * Write DAWG to disk... ");
            sw.Restart();
            Dawg.Write(dawg, dawgFile);
            sw.Stop();
            PrintOK(sw);
            report.DawgWriteTime = sw.Elapsed;
            
            Console.Write(" * Read DAWG from disk... ");
            sw.Restart();
            dawg = Dawg.Read(dawgFile);
            sw.Stop();
            PrintOK(sw);
            
            Console.Write(" * Write Gzip to disk... ");
            sw.Restart();
            report.GzipOriginalFileLength = CompressedFileSize(fileName, gzipFile);
            sw.Stop();
            report.GzipCompressTime = sw.Elapsed;
            PrintOK(sw);
            
            Console.Write(" * Read Gzip from disk... ");
            sw.Restart();
            DecompressFileSize(gzipFile);
            sw.Stop();
            report.GzipDecompressTime = sw.Elapsed;
            PrintOK(sw);

            report.OriginalFileLength = new FileInfo(fileName).Length;
            Console.Write($"Original File is {report.OriginalFileLength / 1024:n0} kB. ");
            report.DawgFileLength = new FileInfo(dawgFile).Length;
            Console.Write($"DAWG File is {report.DawgFileLength / 1024:n0} kB. ");
            Console.Write($"Gzip File is {report.GzipOriginalFileLength / 1024:n0} kB. \n");
            
            File.Delete(dawgFile);
            File.Delete(gzipFile);

            PrintPhase("DAWGSharp package comparison ...");
            sw.Restart();
            var dawgSharpBuilder = new DawgSharp.DawgBuilder<bool> (); // <bool> is the value type.
            foreach (string key in words)
            {
                dawgSharpBuilder.Insert (key, true);
            }

            var dawgSharp = dawgSharpBuilder.BuildDawg();
            sw.Stop();
            using var ms = new MemoryStream();
            dawgSharp.SaveTo(ms);
            PrintOK(sw);
            Console.WriteLine($" * DAWGSharp NodeCount is {dawgSharp.GetNodeCount():n0}.");
            Console.WriteLine($" * DAWGSharp File is {ms.Length/1024:n2} kB.");
            
            sw.Restart();
            PrintPhase("Checking own words... ");
            TestOwnWordsExits(words, dawg);
            sw.Stop();
            PrintOK(sw);
            
            PrintPhase("Find 20 random word 100 times... \n");
            var random = new Random();
            var toFind = new HashSet<string>();
            while (toFind.Count != 20)
            {
                toFind.Add(words[random.Next(0, words.Length)]);
            }
            Console.Write(" * Find in word set...");
            sw.Restart();
            FindWords(toFind, hash, 100);
            sw.Stop();
            report.HashContains = sw.Elapsed;
            PrintOK(sw);
            Console.Write(" * Find in word list...");
            sw.Restart();
            FindWords(toFind, words, 100);
            sw.Stop();
            report.ArrayContains = sw.Elapsed;
            PrintOK(sw);
            Console.Write(" * Find in word list binary search...");
            sw.Restart();
            FindWordsBinarySearch(toFind, words, 100);
            sw.Stop();
            report.BinarySearchContains = sw.Elapsed;
            PrintOK(sw);
            Console.Write(" * Find in DAWG...");
            sw.Restart();
            FindWords(toFind, dawg, 100);
            report.DawgSearch = sw.Elapsed;
            sw.Stop();
            PrintOK(sw);

            var prefixes = words
                .Where(w => w.Length > 4)
                .Select(w => w.Substring(0, 4))
                .GroupBy(s => s).Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2)
                .Select(x => x.Key)
                .Take(20)
                .ToArray();
            
            PrintPhase($"Finding 50 words that start with 20 prefixes 100 times...\n");
            Console.Write($" * DAWG...                ");
            sw.Restart();
            var found = DawgPrefixSearch(dawg, prefixes, 100, 50);
            sw.Stop();
            report.DawgPrefixSearch = sw.Elapsed;
            Console.Write($"found {string.Join(", ", found.Select(x => x.Count).OrderByDescending(x => x))}");
            PrintOK(sw);
            
            Console.Write($" * Array...               ");
            sw.Restart();
            // https://stackoverflow.com/questions/52395504/inconsistent-string-startswith-on-different-platforms
            var hfound = LinearPrefixSearch(words, prefixes, 100, 50);
            sw.Stop();
            report.ArrayPrefixSearch = sw.Elapsed;
            Console.Write($"found {string.Join(", ", hfound.Select(x => x.Count).OrderByDescending(x => x))}");
            PrintOK(sw);
            if(!SameSets(found, hfound))
            {
                throw new Exception("Different prefixed words count.");
            }

            PrintPhase($"Finding first 500 words that start with 20 prefixes 100 times...\n");
            Console.Write($" * DAWG...                ");
            sw.Restart();
            found = DawgPrefixSearch(dawg, prefixes, 100, 500);
            sw.Stop();
            report.DawgLimitedPrefixSearch = sw.Elapsed;
            Console.Write($"found {string.Join(", ", found.Select(x => x.Count).OrderByDescending(x => x))}");
            PrintOK(sw);
            
            Console.Write($" * Array...               ");
            sw.Restart();
            hfound = LinearPrefixSearch(words, prefixes, 100, 500);
            sw.Stop();
            report.ArrayLimitedPrefixSearch = sw.Elapsed;
            Console.Write($"found {string.Join(", ", hfound.Select(x => x.Count).OrderByDescending(x => x))}");
            PrintOK(sw);
            if(!SameSets(found, hfound))
            {
                throw new Exception("Different prefixed words count.");
            }

            sw.Restart();
            PrintPhase("Checking random generated words in parallel... ");
            TestRandomGeneratedWords(words, hash, dawg);
            sw.Stop();
            PrintOK(sw);
            Console.WriteLine();
            return report;
        }

        static bool SameSets<T>(ISet<T>[] a, ISet<T>[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                var sa = a[i];
                var sb = b[i];

                if (sb.Count != sa.Count)
                    return false;

                if (!sa.IsSubsetOf(sb))
                    return false;
            }

            return true;
        }
        
        class FileReport
        {
            public string FileName;
            public int WordCount;
            public TimeSpan BuildDawgTime;
            public long OriginalFileLength;
            public long DawgFileLength;
            public int NodeCount;
            public TimeSpan HashContains;
            public TimeSpan ArrayContains;
            public TimeSpan DawgSearch;
            public TimeSpan DawgPrefixSearch;
            public TimeSpan ArrayPrefixSearch;
            public long ArraySize;
            public long HashSize;
            public TimeSpan ArrayLimitedPrefixSearch;
            public TimeSpan DawgLimitedPrefixSearch;
            public long GzipOriginalFileLength;
            public TimeSpan DawgWriteTime;
            public TimeSpan GzipCompressTime;
            public TimeSpan GzipDecompressTime;
            public TimeSpan BinarySearchContains;
        }
        
        static HashSet<string>[] DawgPrefixSearch(Dawg dawg, string[] prefixes, int times, int? max = null)
        {
            var results = new HashSet<string>[prefixes.Length];

            for (var index = 0; index < prefixes.Length; index++)
            {
                var prefix = prefixes[index];
                for (int i = 0; i < times; i++)
                {
                    var query = dawg.WithPrefix(prefix);
                    if (max.HasValue)
                        query = query.Take(max.Value);
                    results[index] = query.ToHashSet();
                }
            }

            return results;
        }

        // https://stackoverflow.com/questions/52395504/inconsistent-string-startswith-on-different-platforms
        static HashSet<string>[] LinearPrefixSearch(string[] words, string[] prefixes, int times, int? max = null)
        {
            var results = new HashSet<string>[prefixes.Length];

            for (var index = 0; index < prefixes.Length; index++)
            {
                var prefix = prefixes[index];
                for (int i = 0; i < times; i++)
                {
                    var query = words.Where(x => x.StartsWith(prefix, StringComparison.Ordinal));
                    if (max.HasValue)
                        query = query.Take(max.Value);
                    results[index] = query.ToHashSet();
                }
            }

            return results;
        }

        static void FindWords(HashSet<string> toFind, string[] words, int times)
        {
            for (var i = 0; i < times; i++)
            {
                foreach (var word in toFind)
                {
                    words.Contains(word);
                }
            }
        }
        
        static void FindWordsBinarySearch(HashSet<string> toFind, string[] words, int times)
        {
            for (var i = 0; i < times; i++)
            {
                foreach (var word in toFind)
                {
                    Assert(true, Array.BinarySearch(words, word, StringComparer.Ordinal) > 0);
                }
            }
        }

        static void FindWords(HashSet<string> toFind, Dawg dawg, int times)
        {
            for (var i = 0; i < times; i++)
            {
                foreach (var word in toFind)
                {
                    Assert(true, dawg.Contains(word));
                }
            }
        }

        static void FindWords(HashSet<string> toFind, HashSet<string> hash, int times)
        {
            for (int i = 0; i < times; i++)
            {
                foreach (var word in toFind)
                {
                    Assert(true, hash.Contains(word));
                }
            }
        }

        static void TestRandomGeneratedWords(string[] words, HashSet<string> hash, Dawg dawg)
        {
            var factor = 1;
            var counter = 0L;
            Parallel.For(0, words.Length, new ParallelOptions() {MaxDegreeOfParallelism = Environment.ProcessorCount}, i =>
            {
                var random = new Random(123 + i);
                var selected = words[i].ToCharArray();
                for (int j = 0; j < selected.Length * factor; j++)
                {
                    Shuffle(selected, random);
                    var shuffled = new string(selected);
                    Assert(shuffled, hash, dawg, $"Differs on shuffled {shuffled}");
                    Interlocked.Increment(ref counter);
                }

                var forSubstring = words[i];
                if (forSubstring.Length < 3)
                    return;

                for (int j = 0; j < selected.Length * factor; j++)
                {
                    var cut = forSubstring.Substring(0, random.Next(1, forSubstring.Length - 1));
                    Assert(cut, hash, dawg,$"Differs on substring {cut}");
                    Interlocked.Increment(ref counter);
                }

                for (int j = 0; j < selected.Length * factor; j++)
                {
                    var insert = forSubstring.Insert(random.Next(0, forSubstring.Length - 1), "".PadLeft(random.Next(1, factor), 's'));
                    Assert(insert, hash, dawg, $"Differs on insert {insert}");
                    Interlocked.Increment(ref counter);
                }
            });
            Console.Write($"{counter:n0} of random words tried ");
        }
        
        static void TestOwnWordsExits(string[] words, Dawg dawg)
        {
            var original = words.ToHashSet();
            var dawgWords = dawg.ToHashSet();
            
            Assert(true, original.Count == dawgWords.Count, "The hash do not contains the same words");
            
            if(!original.IsSubsetOf(dawgWords))
            {
                var a = original.Except(dawgWords).ToArray();
                var b = dawgWords.Except(original).ToArray();
                Assert(true, false, "different sets");
            }
            
            foreach (var word in words)
            {
                Assert(true, dawg.Contains(word), $"DAWG Should contain {word}");
            }
        }

        static void PrintOK(Stopwatch sw)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" [OK] {sw.Elapsed.TotalSeconds:0.000} seconds.");
            Console.ResetColor();
        }
        
        static void PrintPhase(string line)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(line);
            Console.ResetColor();
        }

        static void Assert(string word, HashSet<string> set, Dawg dawg, string message)
        {
            Assert(set.Contains(word), dawg.Contains(word), "DAWG - " + message );
        }

        static void Assert(bool actual, bool expected, string message = "")
        {
            if(actual != expected)
                throw new Exception(message);
        }
        
        static void Swap<T>(T[] array, int a, int b)
        {
            var temp = array[a];
            array[a] = array[b];
            array[b] = temp;
        }

        static void Shuffle<T>(T[] array, Random random)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var selected = random.Next(i, array.Length);
                Swap(array, i, selected);
            }
        }
        
        static void PrintReports(List<FileReport> reports)
        {
            Console.WriteLine("FILE SIZES");
            foreach (var report in reports)
            {
                Console.WriteLine($"|{report.FileName}|{report.OriginalFileLength/1024:n0} kB|{report.WordCount:n0}|{report.BuildDawgTime.TotalMilliseconds:n0} ms|{report.DawgFileLength/1024:n0} kB|{report.GzipCompressTime.TotalMilliseconds:n0} ms|{report.GzipOriginalFileLength/1024:n0} kB|");
            }
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("MEMORY SIZES");
            foreach (var report in reports)
            {
                Console.WriteLine($"|{report.FileName}|{report.WordCount:n0}|{report.DawgFileLength/1024:n0} kB|{report.HashSize/1024:n0} kB|{report.ArraySize/1024:n0} kB|");
            }
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("SEARCH TIME");
            foreach (var report in reports)
            {
                Console.WriteLine($"|{report.FileName}|{report.WordCount:n0}|{report.DawgSearch.TotalMilliseconds:n0} ms|{report.HashContains.TotalMilliseconds:n0} ms|{report.ArrayContains.TotalMilliseconds:n0} ms|{report.BinarySearchContains.TotalMilliseconds:n0} ms|");
            }
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("PREFIX SEARCH TIME");
            foreach (var report in reports)
            {
                Console.WriteLine($"|{report.FileName}|{report.WordCount:n0}|{report.DawgPrefixSearch.TotalMilliseconds:n0} ms|{report.ArrayPrefixSearch.TotalMilliseconds:n0} ms|");
            }
            Console.WriteLine();
            Console.WriteLine();
            
            Console.WriteLine("LIMITED PREFIX SEARCH TIME");
            foreach (var report in reports)
            {
                Console.WriteLine($"|{report.FileName}|{report.WordCount:n0}|{report.DawgLimitedPrefixSearch.TotalMilliseconds:n0} ms|{report.ArrayLimitedPrefixSearch.TotalMilliseconds:n0} ms|");
            }
            Console.WriteLine();
            Console.WriteLine();
            
        }
    }
}