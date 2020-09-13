using System.Linq;
using vtortola;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.DawgTests
{
    public class WordCases : TestBase
    {
        public WordCases(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper) { }

        [Fact]
        public void DivergingBranches()
        {
            var words = new[]
            {
                "ASIEAE",
                "AS",
                
                "OSIEAE",
                "OS"
            };
            
            RunAutomaticTests(8, words);
        }
        
        [Fact]
        public void Repeated()
        {
            var words = new[]
            {
                "AA",
                "AA"
            };
            
            RunAutomaticTests(3, words);
        }

        [Fact]
        public void TapTop()
        {
            RunAutomaticTests(5, "top", "tap");
        }
        
        [Fact]
        public void TapTopTapsTops()
        {
            RunAutomaticTests(trie =>
            {
                Assert.False(trie.Contains("to"));
                Assert.False(trie.Contains("ps"));
            }, 6, "top", "tap", "tops", "taps");
        }
        
        [Fact]
        public void TapsTopsTap()
        {
            RunAutomaticTests(trie =>
            {
                Assert.False(trie.Contains("to"));
                Assert.False(trie.Contains("top"));
            }, 7, "taps", "tops", "tap");
        }

        [Fact]
        public void CityPity()
        {
            RunAutomaticTests(trie =>
            {
                Assert.False(trie.Contains("citi"));
                Assert.False(trie.Contains("piti"));
                Assert.False(trie.Contains("pitie"));
            }, 9, "city", "pity", "cities", "pities");
        }

        [Fact]
        public void Case4()
        {
            var words = new[]
            {
                "RIG",
                "RIGLY",
                "RIN",
                "RIE",
                "LIG",
                "LIN",
                "LIE",
                "LIELY"
            };
            
            RunAutomaticTests(13, words);
        }
        
        [Fact]
        public void Case5()
        {
            var words = new[]
            {
                "RIAN",
                "RIES",
                "IES",
                "ISH"
            };
            
            RunAutomaticTests(11, words);
        }
        
        [Fact]
        public void Case6()
        {
            var words = new[]
            {
                "bon",
                "bcn",
                "acn",
                "asn",
                "boa"
            };
            
            RunAutomaticTests(9, words);
        }
        
        [Fact]
        public void Case7()
        {
            var words = new[]
            {
                "bcn",
                "acn",
                "bon",
                "boa",
                "asn"
            };

            var builder = Dawg.CreateBuilder(words).Build();
            Assert.True(builder.Contains("boa"));
        }
        
        [Fact]
        public void Case8()
        {
            var words = new[]
            {
                "AAHEED",
                "AAHED",
                "OOHED",
                "AALID",
                "AAED"
            };

            var builder = Dawg.CreateBuilder(words).Build();
            
            Assert.True(builder.Contains("AAHED"));
            Assert.False(builder.Contains("AAHEDS"));
            Assert.False(builder.Contains("AHAED"));
        }
        
        [Fact]
        public void Case9()
        {
            var words = Enumerable.Range(0, 300).Select(x => x.ToString()).ToArray();
            RunAutomaticTests(31, words);
        }
        
        [Fact]
        public void Case10()
        {
            var words = Enumerable.Range(100, 200).Select(x => (x * 10).ToString("000")).ToArray();
            RunAutomaticTests(24, words);
        }
        
        [Fact]
        public void Case11()
        {
            var words = Enumerable.Range(1000, 200).Select(x => (x * 100).ToString("000")).ToArray();
            RunAutomaticTests(26, words);
        }
        
        [Fact]
        public void Case12()
        {
            var words = new[]
            {
                "zz",
                "zx",
                "bb",
                "ba",
                "ab",
                "aa",
            };
            
            RunAutomaticTests(8, words);
        }
        
        [Fact]
        public void Case14()
        {
            var words = new[]
            {
                "zz",
                "zx",
                "bb",
                "ba",
                "ab",
                "ao",
            };
            
            RunAutomaticTests(10, words);
        }
        
        [Fact]
        public void Case13()
        {
            var words = new[]
            {
                "ba",
                "ab",
                "aa",
                "bb"
            };

            RunAutomaticTests(dawg =>
            {
                Assert.True(dawg.Contains("bb"));
            },5, words);
        }
        
        [Fact]
        public void Case15()
        {
            var words = new[]
            {
                "AAHS",
                "AALIIS",
                "AALS"
            };
            
            var builder = Dawg.CreateBuilder(words);
        }

        [Fact]
        public void Case16()
        {
            var words = new[]
            {
                "AAHS",
                "AALIIS",
                "AALS",
                "AARDVARKS"
            };

            var builder = Dawg.CreateBuilder(words);
        }
        
        [Fact]
        public void Case17()
        {
            var words = new[]
            {
                "ABX",
                "ABY",
                "ABZ",
                "ACY",
                "ACZ"
            };

           RunAutomaticTests(7, words);
        }
        
        [Fact]
        public void Case18()
        {
            var words = new[]
            {
                "AX",
                "AY",
                "AZ",
                "BY",
                "BZ",
                "CZ"
            };
            
            RunAutomaticTests(7, words);
        }
        
        [Fact]
        public void Case19()
        {
            var words = new[]
            {
                "AXS",
                "AYS",
                "AZS",
                "BYS",
                "BZS",
                "CZS"
            };
            
            RunAutomaticTests(8, words);
        }
        
        [Fact]
        public void Case20()
        {
            var words = new[]
            {
                "AXS",
                "AYS",
                "AZS",
                "BYS",
                "BZS",
                "CZ"
            };
            
            RunAutomaticTests(9, words);
        }
        
        [Fact]
        public void Case21()
        {
            var words = new[]
            {
                "HX",
                "HY",
                "HZ",
                "BY",
                "BZ",
                "CZ"
            };
            
            RunAutomaticTests(7, words);
        }
        
        [Fact] // Example optimized
        public void Case22()
        {
            var words = new[]
            {
                "TAP",
                "TAPS",
                "TOP",
                "TOPS",
                "TUP",
                "TUPS",
                "COP",
                "COPS",
                "CUP",
                "CUPS",
                "HOP",
                "HOPS",
                "HUP",
                "HUPS"
            };
            
            RunAutomaticTests(9, words);
        }
        
        [Fact] // Example unfixable
        public void Case23()
        {
            var words = new[]
            {
                "TAC",
                "TAN",
                "CAC",
                "CAP",
                "CAN"
            };
            
            RunAutomaticTests(10, words);
        }
        
        [Fact] // Example unmergeable prefix
        public void Case24()
        {
            var words = new[]
            {
                "CAP",
                "COP",
                "TAP"
            };
            
            RunAutomaticTests(7, words);
        }
        
        [Fact] // Example mergeable suffix
        public void Case25()
        {
            var words = new[]
            {
                "CAP",
                "COP",
                "TOP"
            };
            
            RunAutomaticTests(6, words);
        }
    }
}