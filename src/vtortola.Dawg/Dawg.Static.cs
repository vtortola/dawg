using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace vtortola
{
    public sealed partial class Dawg
    {
        // This would allow to replace the way the DAWG is serialized
        static readonly IDawgStateProvider _provider = new UIntDawgStateProvider();
        
        public static Dawg Create(IEnumerable<string> words)
            => CreateBuilder(words).Build();
        
        internal static DawgBuilder CreateBuilder(IEnumerable<string> words)
        {
            var dawg = new DawgBuilder(_provider);
            dawg.AddToTrie(words);
            return dawg;
        }

        public static void Verify(IEnumerable<string> words, Dawg dawg)
            => Verify(words.ToHashSet(), dawg);

        public static void Verify(ISet<string> words, Dawg dawg)
        {
            var d = dawg.ToHashSet();
            if (words.Count != d.Count || !words.IsSubsetOf(d))
            {
                static void addRange(ISet<string> h, IEnumerable<string> e)
                {
                    foreach (var s in e)
                        h.Add(s);
                }

                var different = new HashSet<string>();
                addRange(different, words.Except(d));
                addRange(different, d.Except(words));
                
                throw new InvalidOperationException($"Dawg does not contain the same words than the provided list. Differs in {different.Count} words: {string.Join(", ", different)}.");
            }
        }

        public static Dawg Read(string file)
        {
            using var fileStream = File.OpenRead(file);
            return Read(fileStream);
        }

        public static Dawg Read(Stream stream)
        {
            static void ThrowInvalidFileFormat()
                => throw new FormatException("Invalid DAWG file.");

            using var br = new BinaryReader(stream);

            var c42 = br.ReadInt32();
            if (c42 != 42)
                ThrowInvalidFileFormat();

            var charsetLength = br.ReadUInt16();
            var charset = new char[charsetLength];
            for (var i = 0; i < charsetLength; i++)
                charset[i] = br.ReadChar();

            var state = _provider.CreateState();
            state.Read(br);
            var reader = state.GetReader();
            reader.MoveToNode(0);
            if(reader.SymbolId != charsetLength)
                ThrowInvalidFileFormat();
            
            if(br.ReadInt32() != 42)
                ThrowInvalidFileFormat();
            
            return new Dawg(state, charset);
        }
        
        public static void Write(Dawg dawg, Stream stream)
        {
            using var bw = new BinaryWriter(stream);

            bw.Write(42);
            bw.Write((ushort)dawg._charset.Length);
            for (var i = 0; i < dawg._charset.Length; i++)
                bw.Write(dawg._charset[i]);

            dawg._state.Write(bw);
            bw.Write(42);
        }

        public static void Write(Dawg dawg, string file)
        {
            using var fileStream = File.OpenWrite(file);
            Write(dawg, fileStream);
        }
    }
}