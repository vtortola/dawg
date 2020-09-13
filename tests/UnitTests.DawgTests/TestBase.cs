using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using vtortola;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.DawgTests
{
    public class TestBase
    {
        readonly ITestOutputHelper _testOutputHelper;

        protected TestBase(ITestOutputHelper testOutputHelper)
            => _testOutputHelper = testOutputHelper;

        void Swap<T>(T[] array, int a, int b)
        {
            var temp = array[a];
            array[a] = array[b];
            array[b] = temp;
        }

        void Shuffle<T>(T[] array, Random random)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var selected = random.Next(i, array.Length);
                Swap(array, i, selected);
            }
        }

        IEnumerable<(Dawg, string[])> MakeShuffledTries(int iterations, int expectedNodes, params string[] words)
        {
            var random = new Random(123);
            var hash = new HashSet<string>(words);
            for (int i = 0; i < iterations; i++)
            {
                var words2 = words.ToArray();
                
                if(i != 0)
                    Shuffle(words2, random);
                
                var dawgBuilder = Dawg.CreateBuilder(words2);
                var dawg = dawgBuilder.Build();
                
                Assert.Equal(expectedNodes, dawg.NodeCount);

                AssertAllContainedWords(dawg, hash);
                AssertContainsWordsInList(words2, i, dawgBuilder, dawg);
                AssertShuffledWords(words, iterations, random, hash, dawgBuilder, dawg);
                AssertRandomSubstrings(words, iterations, random, hash, dawgBuilder, dawg);
                AssertPrefixes(words, iterations, random, dawg);

                yield return (dawg, words2);
            }
        }

        static void AssertAllContainedWords(Dawg dawg, HashSet<string> hash)
        {
            var allDawgWords = dawg.ToHashSet();
            Assert.Equal(hash.Count, allDawgWords.Count);
            Assert.True(hash.IsSubsetOf(allDawgWords));
        }

        static void AssertPrefixes(string[] words, in int iterations, Random random, Dawg dawg)
        {
            for (int j = 0; j < iterations; j++)
            {
                var word = words[j % words.Length];
                if (word.Length == 1)
                    continue;
                var sword = word.Substring(0, random.Next(1, word.Length - 1));

                try
                { 
                    var sw = new Stopwatch();
                    sw.Start();
                    var dawgPrefixed = dawg.WithPrefix(sword).ToArray();
                    Assert.Equal(dawgPrefixed.Length, new HashSet<string>(dawgPrefixed).Count());
                    sw.Stop();
                    var dawgTimming = sw.ElapsedMilliseconds;
                    sw.Restart();
                    var wordsPrefixed = words.Where(w => w.StartsWith(sword)).ToHashSet();
                    sw.Stop();
                    var wordsTimming = sw.ElapsedMilliseconds;
                    Assert.Equal(wordsPrefixed.Count(), dawgPrefixed.Length);
                }
                catch (Exception)
                {
                    throw new Exception($"DAWG differs on prerix {sword}  on words: {string.Join(", ", words)}");
                }
            }
        }

        static void AssertRandomSubstrings(string[] words, int iterations, Random random, HashSet<string> hash, DawgBuilder dawgBuilder, Dawg dawg)
        {
            for (int j = 0; j < iterations * 100; j++)
            {
                var word = words[j % words.Length];
                if (word.Length == 1)
                    continue;
                var sword = word.Substring(0, random.Next(1, word.Length - 1));
                try
                {
                    Assert.Equal(hash.Contains(sword), dawgBuilder.Contains(sword));
                }
                catch (Exception)
                {
                    throw new Exception("Differs on " + sword);
                }

                try
                {
                    Assert.Equal(hash.Contains(sword), dawg.Contains(sword));
                }
                catch (Exception)
                {
                    throw new Exception($"DAWG substring differs on " + sword);
                }
            }
        }

        void AssertShuffledWords(string[] words, int iterations, Random random, HashSet<string> hash, DawgBuilder dawgBuilder, Dawg dawg)
        {
            for (int j = 0; j < iterations * 100; j++)
            {
                var word = words[j % words.Length].ToCharArray();
                Shuffle(word, random);
                var sword = new string(word);
                try
                {
                    Assert.Equal(hash.Contains(sword), dawgBuilder.Contains(sword));
                }
                catch (Exception)
                {
                    throw new Exception($"Failed on shuffled word {sword}");
                }

                try
                {
                    Assert.Equal(hash.Contains(sword), dawg.Contains(sword));
                }
                catch (Exception)
                {
                    throw new Exception($"DAWG Failed on shuffled word {sword}");
                }
            }
        }

        void AssertContainsWordsInList(string[] words, int iteration, DawgBuilder dawgBuilder, Dawg dawg)
        {
            foreach (var word in words)
            {
                Assert.True(dawgBuilder.Contains(word), $"Should contain '{word}' but it does not in iteration {iteration}: {string.Join(",", words)}");
                Assert.True(dawg.Contains(word), $"DAWG Should contain '{word}' but it does not in iteration {iteration}: {string.Join(",", words)}");
            }
        }

        protected void RunAutomaticTests(Action<Dawg> testAction, int expectedNodes, params string[] words)
        {
            foreach (var trie in MakeShuffledTries(20, expectedNodes, words))
            {
                try
                {
                    testAction(trie.Item1);
                }
                catch (Exception)
                {
                    _testOutputHelper.WriteLine(string.Join(", ", trie.Item2 ));
                    _testOutputHelper.WriteLine(trie.ToString());
                    throw;
                }
            }
        }
        
        protected void RunAutomaticTests(int expectedNodes, params string[] words)
        {
            MakeShuffledTries(20, expectedNodes, words).ToArray();
        }
    }
}