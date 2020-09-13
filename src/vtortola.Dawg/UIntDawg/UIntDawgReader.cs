using System;

namespace vtortola
{
    internal sealed class UIntDawgReader : IDawgReader
    {
        readonly uint[] _values;
        uint _current = 1;

        public UIntDawgReader(uint[] nodes)
            => _values = nodes;

        public uint Current
            => _current;
        
        public bool MoveNextSibling()
        {
            if (_current >= _values.Length - 1)
                return false;

            if (IsLastSibling)
                return false;
            
            MoveToNode(_current + 1);
            return true;
        }

        public bool MoveToFirstChild()
        {
            var fc = FirstChild;
            if (fc == 0)
                return false;
            MoveToNode(fc);
            return true;
        }

        public void MoveToNode(in uint index)
            => _current = index switch
            {
                _ when index >= _values.Length => throw new IndexOutOfRangeException(),
                _ => index
            };

        uint Get(in uint mask, in int offset)
        {
            ref var avalue = ref _values[_current];
            return (avalue & mask) >> offset;
        }

        static readonly uint _symbolIdMask = (uint)Math.Pow(2, 8) - 1;
        public uint SymbolId
            => Get(in _symbolIdMask, 0);

        static readonly uint _firstChildIdMask = (uint)(Math.Pow(2, 22) - 1) << 8;
        public uint FirstChild
            => Get(in _firstChildIdMask, 8);

        const uint _isEndOfWordMask = (uint)1 << 30;
        public bool IsEndOfWord
            => (_values[_current] & _isEndOfWordMask) == _isEndOfWordMask;

        const uint _lastSiblingMask = (uint)1 << 31;
        public bool IsLastSibling
            => (_values[_current] & _lastSiblingMask) == _lastSiblingMask;
    }
}