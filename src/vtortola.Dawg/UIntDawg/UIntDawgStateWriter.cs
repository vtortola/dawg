using System;

namespace vtortola
{
    internal sealed class UIntDawgStateWriter : IDawgStateWriter
    {
        readonly uint[] _values;
        uint _current = 0;

        public UIntDawgStateWriter(int nodes, int symbols)
            => _values = new uint[nodes];

        public Dawg Create(char[] symbols)
            => new Dawg(new UIntDawgState(_values), symbols);

        public void MoveToNode(in uint index)
            => _current = index switch
            {
                _ when index >= _values.Length => throw new IndexOutOfRangeException(),
                _ => index
            };

        void Set(in uint value, in uint mask, in int offset)
        {
            ref var avalue = ref _values[_current];

            avalue &= ~mask;
            avalue |= ((value << offset) & mask);
        }

        uint Get(in uint mask, in int offset)
        {
            ref var avalue = ref _values[_current];
            return (avalue & mask) >> offset;
        }

        void SetFlag(in bool value, in uint mask, in uint invertedMask)
        {
            ref var avalue = ref _values[_current];
            avalue = value
                ? avalue | mask
                : avalue & invertedMask;
        }

        static readonly uint _symbolIdMask = (uint)Math.Pow(2, 8) - 1;
        static readonly uint _maxSymbolIdValue = (uint)Math.Pow(2, 9);
        public uint SymbolId
        {
            get => Get(in _symbolIdMask, 0);
            set
            {
                if (value >= _maxSymbolIdValue)
                    throw new InvalidOperationException($"Too many symbols.");

                Set(in value, in _symbolIdMask, 0);
            }
        }

        static readonly uint _firstChildIdMask = (uint)(Math.Pow(2, 22) - 1) << 8;
        static readonly uint _maxChildId = (uint)Math.Pow(2, 22);
        public uint FirstChild
        {
            get => Get(in _firstChildIdMask, 8);
            set
            {
                if (value >= _maxChildId)
                    throw new InvalidOperationException($"Too many nodes.");

                Set(in value, in _firstChildIdMask, 8);
            }
        }

        const uint _isEndOfWordMask = (uint)1 << 30;
        const uint _isEndOfWordInvertedMask = ~_isEndOfWordMask;
        public bool IsEndOfWord
        {
            get => (_values[_current] & _isEndOfWordMask) == _isEndOfWordMask;
            set => SetFlag(value, _isEndOfWordMask, _isEndOfWordInvertedMask );
        }

        const uint _lastSiblingMask = (uint)1 << 31;
        const uint _lastSiblingInvertedMask = ~_lastSiblingMask;
        public bool IsLastSibling
        {
            get => (_values[_current] & _lastSiblingMask) == _lastSiblingMask;
            set => SetFlag(value, _lastSiblingMask, _lastSiblingInvertedMask);
        }
    }
}