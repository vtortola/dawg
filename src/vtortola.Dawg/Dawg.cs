using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vtortola
{
    public sealed partial class Dawg : IEnumerable<string>
    {
        readonly IDawgState _state;
        readonly char[] _charset;
        readonly Dictionary<char, int> _charsetLookup;

        internal Dawg(IDawgState state, char[] charset)
        {
            _state = state;
            _charset = charset;
            _charsetLookup = charset.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);
        }

        public int NodeCount 
            => _state.Length;
        
        bool FindLastCharLocation(int[] symbols, IDawgReader reader)
        {
            for (var i = 0; i < symbols.Length; i++)
            {
                var symbolId = symbols[i];
                while (reader.SymbolId != symbolId && !reader.IsLastSibling)
                {
                    if(!reader.MoveNextSibling())
                        return false;
                }

                if (reader.SymbolId != symbolId)
                    return false;

                if (i == symbols.Length - 1)
                    return true;

                if (!reader.MoveToFirstChild())
                    return false;
            }

            return false;
        }

        IEnumerable<string> SuffixesFrom(IDawgReader reader, StringBuilder builder)
        {
            if(reader.Current == 0)
                yield break;
            
            builder.Append(_charset[reader.SymbolId]);

            if (reader.IsEndOfWord)
                yield return builder.ToString();

            if (reader.FirstChild != 0)
            {
                var current = reader.Current;
                if(!reader.MoveToFirstChild())
                    yield break;
                
                foreach (var word in SuffixesFrom(reader, builder))
                    yield return word;
                
                reader.MoveToNode(current);
            }

            builder.Length--;

            if (reader.IsLastSibling)
                yield break;
            
            reader.MoveNextSibling();
            foreach (var word in SuffixesFrom(reader, builder))
                yield return word;
        }

        bool TryGetSymbolsArray(string word, out int[] result)
        {
            result = new int[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                if(!_charsetLookup.TryGetValue(word[i], out var symbolId))
                    return false;

                result[i] = symbolId;
            }

            return true;
        }
        
        public bool Contains(string word)
        {
            var reader = _state.GetReader();
            if (!TryGetSymbolsArray(word, out var symbols))
                return false;
            
            return FindLastCharLocation(symbols, reader) && reader.IsEndOfWord;
        }
        
        public IEnumerable<string> WithPrefix(string prefix)
        {
            var reader = _state.GetReader();
            
            if (!TryGetSymbolsArray(prefix, out var symbols))
                yield break;
            
            if(!FindLastCharLocation(symbols, reader))
                yield break;
            
            if (reader.IsEndOfWord)
                yield return prefix;

            var builder = new StringBuilder(prefix);
            if(!reader.MoveToFirstChild())
                yield break;
            
            foreach (var word in SuffixesFrom(reader, builder))
                yield return word;
        }
        
        public IEnumerator<string> GetEnumerator()
        {
            var builder = new StringBuilder();
            var reader = _state.GetReader();
            foreach (var word in SuffixesFrom(reader, builder))
                yield return word;
        }
        
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}