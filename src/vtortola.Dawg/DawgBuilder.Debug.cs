using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vtortola
{
    internal partial class DawgBuilder
    {
#if(DEBUG)
        public override string ToString()
        {
            var builder = new StringBuilder();
            var queue = new Queue<TrieNode>();
            var visited = new HashSet<(TrieNode, TrieNode)>();
            queue.Enqueue(_start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                for (var i = 0; i < current.Children.Count; i++)
                {
                    var next = current.Children[i];
                    if (visited.Add((current, next)))
                        builder.AppendLine($"[{current.Value}({current.Id}) -> {next.Value}({next.Id})] -- {next.IsTerminal}");

                    queue.Enqueue(next);
                }
            }
            
            return builder.ToString();
        }

        [Obsolete("This method should not be used beyond testing purposes")]
        public bool Contains(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return false;
            
            var current = _start;
            for (var i = 0; i < word.Length; i++)
            {
                var c = word[i];
                var node = current.Children.SingleOrDefault(n => n.Value == c);

                if (node == null)
                    return false;

                current = node;
            }

            return current.IsTerminal;
        }
#endif
    }
}