using System.Text;

namespace vtortola
{
    public sealed partial class Dawg
    {
#if(DEBUG)
        public override string ToString()
        {
            var sb = new StringBuilder();
            var reader = _state.GetReader();
            for (uint i = 0; i < _state.Length; i++)
            {
                reader.MoveToNode(i);
                sb.AppendLine($"[{i:00}] {_charset[reader.SymbolId]} #{reader.FirstChild} {(reader.IsLastSibling ? "L": " ")} {(reader.IsEndOfWord?"E":"")}");
            }

            return sb.ToString();
        }
#endif
    }
}