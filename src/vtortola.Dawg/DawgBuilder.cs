using System;
using System.Collections.Generic;
using System.Linq;

namespace vtortola
{
    using LongestListsPerHash = Dictionary<(long,long), (IList<TrieNode>, int)>;
    internal sealed partial class DawgBuilder
    {
        readonly IDawgStateProvider _provider;
        int _nodeIncremental;
        uint _nextNodeIndex = 1u;
        
        readonly TrieNode _start = new TrieNode 
        { 
            NodeIndex = 0,
            Value = '.', 
            SiblingHashes = new List<(long,long)>{(0,0)}
        };
        
        class NodeValueComparer : IComparer<TrieNode>
        {
            public int Compare(TrieNode x, TrieNode y)
                => x.Value.CompareTo(y.Value);

            public static NodeValueComparer Instance = new NodeValueComparer();
        }

        public DawgBuilder(IDawgStateProvider provider)
            => _provider = provider;

        readonly HashSet<char> _charset = new HashSet<char>();

        internal void AddToTrie(IEnumerable<string> words)
        {
            var lookup = new Dictionary<(TrieNode, char), TrieNode>();
            foreach (var word in words)
                Add(word, lookup);
        }

        void Add(string word, IDictionary<(TrieNode, char), TrieNode> lookup)
        {
            if (string.IsNullOrWhiteSpace(word))
                return;

            var parent = _start;

            for (var i = 0; i < word.Length; i++)
            {
                var c = word[i];

                if (!lookup.TryGetValue((parent, c), out var node))
                {
                    node = new TrieNode
                    {
                        Id = ++_nodeIncremental,
                        Value = c
                    };

                    lookup[(parent, c)] = node;
                    parent.Children.Add(node);
                    _charset.Add(c);
                }

                parent = node;
                if (i == word.Length - 1)
                    node.IsTerminal = true;
            }
        }

        public Dawg Build()
        {
            var sortedCharOrder = _charset
                .OrderBy(x => x)
                .ToArray();
            
            var charsetOrder = sortedCharOrder
                .Select((c, i) => (c, i))
                .ToDictionary(x => x.c, x => (uint)x.i);

            var longest = new LongestListsPerHash();
            
            ProcessTrieHashes(_start, charsetOrder, longest);

            var pending = new Queue<TrieNode>((int)_nextNodeIndex);
            pending.Enqueue(_start);

            NumberNodesInSizeOrderAndEnqueue(pending, longest);

            var writer = _provider.CreateWriter(nodeCount: (int)_nextNodeIndex, symbolCount: _charset.Count);

            WriteLists(pending, longest, writer);

            return writer.Create(sortedCharOrder);
        }

        void WriteLists(Queue<TrieNode> pending, LongestListsPerHash longest, IDawgStateWriter writer)
        {
            while (pending.Count > 0)
            {
                var current = pending.Dequeue();

                writer.MoveToNode(current.NodeIndex);
                writer.IsLastSibling = current.IsLastSibling;
                writer.SymbolId = current.SymbolId;
                writer.IsEndOfWord = current.IsTerminal;

                var first = current.Children.FirstOrDefault();
                if (first == null)
                    continue;

                if (longest.TryGetValue(first.SiblingHashes[0], out var outgoing))
                    writer.FirstChild = outgoing.Item1[outgoing.Item2].NodeIndex;
            }

            // sanity check
            writer.MoveToNode(0);
            writer.SymbolId = (uint) _charset.Count;
            writer.IsLastSibling = true;
        }

        void NumberNodesInSizeOrderAndEnqueue(Queue<TrieNode> pending, LongestListsPerHash biggerListPerHash)
        {
            // Write first letters in the trie, to ensure they are first list in the DAWG.
            foreach (var node in _start.Children)
                node.NodeIndex = _nextNodeIndex++;
            
            var visited = new HashSet<(long,long)>();
            foreach (var list in biggerListPerHash.Values.OrderByDescending(x => x.Item1.Count))
                NumberNodes(list, pending, visited);
        }

        void NumberNodes((IList<TrieNode>, int) sublist, Queue<TrieNode> queue, ISet<(long, long)> visited)
        {
            if(!visited.Add(sublist.Item1[sublist.Item2].SiblingHashes[0]))
                return;
            
            TrieNode last = null;
            for (var i = 0; i < sublist.Item1.Count; i++)
            {
                var child = sublist.Item1[i];
                if (child.NodeIndex == 0)
                    child.NodeIndex = _nextNodeIndex++;

                last = child;
                queue.Enqueue(child);
                visited.Add(child.SiblingHashes[i]);
            }

            if (last != null)
                last.IsLastSibling = true;
        }

        (long,long) ProcessTrieHashes(TrieNode current, IDictionary<char,uint> charsetIndexes, LongestListsPerHash longest)
        {
            current.Children.Sort(NodeValueComparer.Instance);
            var accum = 0;
            var leafDepth = 0;
            var connectedToEnd = current.IsTerminal;

            var childrenHashes = new List<(long,long)>(current.Children.Count);

            for (var i = 0; i < current.Children.Count; i++)
            {
                var node = current.Children[i];
                node.SymbolId = charsetIndexes[node.Value];
                childrenHashes.Add(ProcessTrieHashes(node, charsetIndexes, longest));
                accum++;
                leafDepth = Math.Max(leafDepth, node.FurthestLeaf + 1);
            }

            current.FurthestLeaf = leafDepth;

            var siblingHashes = CalculateAndCacheSiblingHashes(current.Children, childrenHashes, longest);

            foreach (var node in current.Children)
                node.SiblingHashes = siblingHashes;
            
            return CalculateNodeHashes(current, childrenHashes, accum, leafDepth, connectedToEnd);
        }

        List<(long,long)> CalculateAndCacheSiblingHashes(IList<TrieNode> siblings, IList<(long,long)> childrenHashes, LongestListsPerHash longest)
        {
            var brotherHoodHashes = new List<(long,long)>(siblings.Count);
            for (var i = 0; i < siblings.Count; i++)
            {
                var hash = CalculateSiblingSublistHash(i, childrenHashes);
                brotherHoodHashes.Add(hash);
                if (longest.TryGetValue(hash, out var existing))
                {
                    if (existing.Item1.Count < siblings.Count)
                        longest[hash] = (siblings, i);
                }
                else
                {
                    longest[hash] = (siblings, i);
                }
            }

            return brotherHoodHashes;
        }

        (long, long) CalculateSiblingSublistHash(int from, IList<(long, long)> childrenHashes)
        {
            var hashSum1 = 1L;
            var hashSum2 = 1L;
            var relative = 1;
            for (var i = from; i < childrenHashes.Count; i++)
            {
                hashSum1 = hashSum1 * 43 * relative + childrenHashes[i].Item1;
                hashSum2 = hashSum2 * 43 * relative + childrenHashes[i].Item2;
                relative++;
            }

            return (hashSum1, hashSum2);
        }

        (long, long) CalculateNodeHashes(TrieNode node, IList<(long, long)> childrenHashes, int accum, int leafDepth, bool connectedToEnd)
        {
            unchecked
            {
                var value = node.Value;
                var hash1 = value * 97L;
                var hash2 = long.MaxValue ^ value;

                var hashSum1 = 1L;
                var hashSum2 = 1L;
                for (var i = 0; i < childrenHashes.Count; i++)
                {
                    hashSum1 = hashSum1 * i + childrenHashes[i].Item1 * 493;
                    hashSum2 = hashSum2 * 59 * i + childrenHashes[i].Item2;
                }

                hash1 = hash1 * 97 + accum;
                hash1 = hash1 * 199 + leafDepth;
                hash1 = hash1 * 293 + hashSum1;
                hash1 = hash1 ^ value * 397;
                
                hash2 = hash2 * 101 + accum;
                hash2 = hash2 * 103 + leafDepth;
                hash2 = hash2 * 107 + hashSum2;
                hash2 = (hash2 ^ value) * 293;

                if (connectedToEnd)
                {
                    hash1 = hash1 * 499 + hash1;
                    hash2 = hash2 * 383 + hash2;
                }

                return (hash1, hash2);
            }
        }
    }
}