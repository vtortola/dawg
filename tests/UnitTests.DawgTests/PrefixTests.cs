using System.Linq;
using vtortola;
using Xunit;

namespace UnitTests.DawgTests
{
    public class PrefixTests
    {
        [Fact]
        public void FindPrefixTapsTops()
        {
            var words = new[]
            {
                "tap",
                "taps",
                "top",
                "tops"
            };

            var dawg = Dawg.CreateBuilder(words).Build();

            var array = dawg.WithPrefix("to").ToArray();
            
            Assert.Contains("top", array);
            Assert.Contains("tops", array);
            
            array = dawg.WithPrefix("tap").ToArray();
            
            Assert.Contains("tap", array);
            Assert.Contains("taps", array);
            
            Assert.Empty(dawg.WithPrefix("tu"));
            
            array = dawg.WithPrefix("t").ToArray();
            
            Assert.Contains("tap", array);
            Assert.Contains("taps", array);
            Assert.Contains("top", array);
            Assert.Contains("tops", array);
        }
        
        [Fact]
        public void FindPrefix()
        {
            var words = new[]
            {
                "ba",
                "ab",
                "aa",
                "bb"
            };

            var dawg = Dawg.CreateBuilder(words).Build();

            var array = dawg.WithPrefix("b").ToArray();
            
            Assert.Contains("bb", array);
            Assert.Contains("ba", array);
            
            array = dawg.WithPrefix("a").ToArray();
            
            Assert.Contains("aa", array);
            Assert.Contains("ab", array);
            
            Assert.Empty(dawg.WithPrefix("t"));
        }
        
        [Fact]
        public void Case14()
        {
            var words = new[]
            {
                "RIG",
                "RIN",
                "RIE"
            };
            
            var dawg = Dawg.CreateBuilder(words).Build();

            var array = dawg.WithPrefix("R").ToArray();
            
            Assert.Equal(3, array.Length);
        }
    }
}