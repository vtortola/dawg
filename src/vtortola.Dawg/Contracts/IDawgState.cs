using System.IO;

namespace vtortola
{
    internal interface IDawgState
    {
        int Length { get; }
        IDawgReader GetReader();
        void Write(BinaryWriter writer);
        void Read(BinaryReader reader);
    }
}