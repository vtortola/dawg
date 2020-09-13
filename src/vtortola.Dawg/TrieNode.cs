using System;
using System.Collections.Generic;

namespace vtortola
{
    internal sealed class TrieNode : IEquatable<TrieNode>
    {
        public int Id;
        public char Value;
        public int FurthestLeaf;
        public uint SymbolId;
        public uint NodeIndex;
        public List<(long,long)> SiblingHashes;
        public List<TrieNode> Children;
        public bool IsTerminal;
        public bool IsLastSibling;

        public TrieNode()
            => Children = new List<TrieNode>();

        public override string ToString()
            => $"({Value}){Id}";

        public bool Equals(TrieNode other)
            => other is { } && Id == other.Id;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is TrieNode node && Equals(node);
        }

        public override int GetHashCode()
            => Id;

        public static bool operator ==(TrieNode left, TrieNode right)
            => Equals(left, right);

        public static bool operator !=(TrieNode left, TrieNode right)
            => !Equals(left, right);
    }
}