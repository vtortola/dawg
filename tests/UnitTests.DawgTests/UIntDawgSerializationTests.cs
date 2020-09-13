using System;
using System.IO;
using vtortola;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests.DawgTests
{
    public class UIntDawgSerializationTests
    {
        readonly ITestOutputHelper _testOutputHelper;

        public UIntDawgSerializationTests(ITestOutputHelper testOutputHelper)
            => _testOutputHelper = testOutputHelper;

        [Fact]
        public void Max()
        {
            UIntDawgStateWriter builder = new UIntDawgStateWriter(1, 1);

            var maxChild = (uint)Math.Pow(2, 22) - 1;
            var maxSymbolId = (uint) Math.Pow(2, 8) - 1;
            
            builder.IsLastSibling = true;
            builder.IsEndOfWord = true;
            builder.FirstChild = maxChild;
            builder.SymbolId = maxSymbolId;

            Assert.True(builder.IsLastSibling);
            Assert.True(builder.IsEndOfWord);
            Assert.Equal(maxChild, builder.FirstChild);
            Assert.Equal(maxSymbolId, builder.SymbolId);

            builder.IsLastSibling = false;
            builder.IsEndOfWord = false;
            
            Assert.False(builder.IsLastSibling);
            Assert.False(builder.IsEndOfWord);
            Assert.Equal(maxChild, builder.FirstChild);
            Assert.Equal(maxSymbolId, builder.SymbolId);

            for (uint i = 0; i < maxChild; i++)
            {
                builder.FirstChild = i;
                Assert.False(builder.IsEndOfWord);
            }
        }

        [Fact]
        public void All()
        {
            UIntDawgStateWriter builder = new UIntDawgStateWriter(1,1);
            
            builder.IsLastSibling = true;
            builder.FirstChild = 35;
            builder.SymbolId = 193;
            
            Assert.True(builder.IsLastSibling);
            Assert.Equal(35u, builder.FirstChild);
            Assert.Equal(193u, builder.SymbolId);
            Assert.False(builder.IsEndOfWord);

            builder.IsEndOfWord = true;
            
            Assert.True(builder.IsLastSibling);
            Assert.Equal(35u, builder.FirstChild);
            Assert.Equal(193u, builder.SymbolId);
            Assert.True(builder.IsEndOfWord);
            
            builder.FirstChild = 728;

            Assert.True(builder.IsLastSibling);
            Assert.Equal(728u, builder.FirstChild);
            Assert.Equal(193u, builder.SymbolId);
            Assert.True(builder.IsEndOfWord);
            
            builder.SymbolId = 3;

            Assert.True(builder.IsLastSibling);
            Assert.Equal(728u, builder.FirstChild);
            Assert.Equal(3u, builder.SymbolId);
            Assert.True(builder.IsEndOfWord);
            
            builder.IsLastSibling = false;

            Assert.False(builder.IsLastSibling);
            Assert.Equal(728u, builder.FirstChild);
            Assert.Equal(3u, builder.SymbolId);
            Assert.True(builder.IsEndOfWord);
        }
        
        [Fact]
        public void IsLastSibling()
        {
            UIntDawgStateWriter dawgState = new UIntDawgStateWriter(1,1);
            Assert.False(dawgState.IsLastSibling);
            dawgState.IsLastSibling = true;
            Assert.True(dawgState.IsLastSibling);
            dawgState.IsLastSibling = false;
            Assert.False(dawgState.IsLastSibling);
        }
        
        [Fact]
        public void IsEndOfWord()
        {
            UIntDawgStateWriter dawgState = new UIntDawgStateWriter(1,1);
            Assert.False(dawgState.IsEndOfWord);
            dawgState.IsEndOfWord = true;
            Assert.True(dawgState.IsEndOfWord);
            dawgState.IsEndOfWord = false;
            Assert.False(dawgState.IsEndOfWord);
        }
        
        [Fact]
        public void SymbolId()
        {
            UIntDawgStateWriter dawgState = new UIntDawgStateWriter(1,1);;
            Assert.Equal(0u, dawgState.SymbolId);
            dawgState.SymbolId = 22;
            Assert.Equal(22u, dawgState.SymbolId);
            dawgState.SymbolId = 0;
            Assert.Equal(0u, dawgState.SymbolId);
        }
        
        [Fact]
        public void FirstChild()
        {
            UIntDawgStateWriter dawgState = new UIntDawgStateWriter(1,1);
            Assert.Equal(0u, dawgState.FirstChild);
            dawgState.FirstChild = 22;
            Assert.Equal(22u, dawgState.FirstChild);
            dawgState.FirstChild = 0;
            Assert.Equal(0u, dawgState.FirstChild);
        }

        [Fact]
        public void File_TapTopTapsTops()
        {
            var dawg = Dawg.CreateBuilder(new[] {"tap", "top", "taps", "tops"})
                .Build();

            var file = Path.GetTempFileName();
            Dawg.Write(dawg, file);

            var redawg = Dawg.Read(file);
            
            Assert.True(redawg.Contains("tap"));
            Assert.True(redawg.Contains("top"));
            Assert.True(redawg.Contains("taps"));
            Assert.True(redawg.Contains("tops"));
            Assert.False(redawg.Contains("to"));
        }
        
    }
}