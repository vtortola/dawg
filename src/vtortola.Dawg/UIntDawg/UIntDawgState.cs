using System.IO;

namespace vtortola
{
    internal sealed class UIntDawgState : IDawgState
    {
        static readonly uint[] _empty = new uint[0];
        uint[] _graph;

        public UIntDawgState(uint[] graph)
            => _graph = graph;
        
        public UIntDawgState()
            => _graph = _empty;

        public int Length 
            => _graph.Length;

        public IDawgReader GetReader()
            => new UIntDawgReader(_graph);
        
        public void Write(BinaryWriter writer)
        {
            writer.Write((uint)_graph.Length);
            for (var i = 0; i < _graph.Length; i++)
                writer.Write(_graph[i]);
        }

        public void Read(BinaryReader reader)
        {
            var nodeCount = reader.ReadUInt32();
            var nodes = new uint[nodeCount];
            for (var i = 0; i < nodeCount; i++)
                nodes[i] = reader.ReadUInt32();
            _graph = nodes;
        }
    }
}