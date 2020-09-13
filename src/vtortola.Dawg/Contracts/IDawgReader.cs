namespace vtortola
{
    internal interface IDawgReader
    {
        uint Current { get; }
        uint SymbolId { get; }
        uint FirstChild { get; }
        bool IsEndOfWord { get; }
        bool IsLastSibling { get; }
        bool MoveNextSibling();
        bool MoveToFirstChild();
        void MoveToNode(in uint index);
    }
}