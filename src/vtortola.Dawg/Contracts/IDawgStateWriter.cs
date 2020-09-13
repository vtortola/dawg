namespace vtortola
{
    internal interface IDawgStateWriter
    {
        Dawg Create(char[] symbols);
        void MoveToNode(in uint index);
        uint SymbolId { set; }
        uint FirstChild { set; }
        bool IsEndOfWord { set; }
        bool IsLastSibling { set; }
    }
}