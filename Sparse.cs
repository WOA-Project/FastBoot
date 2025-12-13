using System;
using System.IO;

namespace FastBoot
{
    internal class Sparse
    {
        public static readonly uint sparse_magic = 0xED26FF3A;

        public enum chunkTypes
        {
            RAW = 0xCAC1,
            FILL,
            DONT_CARE,
            CRC32
        }

        public struct sparseFileHeader
        {
            public uint magic;
            public ushort majorVersion;
            public ushort minorVersion;
            public ushort fileHeaderSize;
            public ushort chunkHeaderSize;
            public uint blockSize;
            public uint totalBlocks;
            public uint totalChunks;
            public uint imageChecksum;
        }

        public struct sparseChunkHeader
        {
            public ushort chunkType;
            public ushort reserved;
            public uint chunkSize;
            public uint totalSize;
        }

        private static void VerifySparseFileHeader(sparseFileHeader fileHeader)
        {
            if (fileHeader.magic != sparse_magic) throw new Exception("Invalid sparse magic");
            if (fileHeader.majorVersion > 1) throw new Exception("Sparse format too new");
            if (fileHeader.fileHeaderSize != 28 && fileHeader.majorVersion == 1) throw new Exception("Invalid file header size");
            if (fileHeader.chunkHeaderSize != 12 && fileHeader.majorVersion == 1) throw new Exception("Invalid chunk header size");
            if (fileHeader.blockSize % 4 != 0) throw new Exception("Invalid block size");
        }

        public static sparseFileHeader ReadSparseFileHeader(byte[] buffer)
        {
            sparseFileHeader fileHeader = new();

            using (var ms = new MemoryStream(buffer))
            using (var br = new BinaryReader(ms))
            {
                fileHeader.magic = br.ReadUInt32();
                fileHeader.majorVersion = br.ReadUInt16();
                fileHeader.minorVersion = br.ReadUInt16();
                fileHeader.fileHeaderSize = br.ReadUInt16();
                fileHeader.chunkHeaderSize = br.ReadUInt16();
                fileHeader.blockSize = br.ReadUInt32();
                fileHeader.totalBlocks = br.ReadUInt32();
                fileHeader.totalChunks = br.ReadUInt32();
                fileHeader.imageChecksum = br.ReadUInt32();
            }

            VerifySparseFileHeader(fileHeader);

            return fileHeader;
        }

        public static sparseChunkHeader ReadSparseChunkHeader(byte[] buffer)
        {
            sparseChunkHeader chunk_header = new();

            using (var ms = new MemoryStream(buffer))
            using (var br = new BinaryReader(ms))
            {
                chunk_header.chunkType = br.ReadUInt16();
                chunk_header.reserved = br.ReadUInt16();
                chunk_header.chunkSize = br.ReadUInt32();
                chunk_header.totalSize = br.ReadUInt32();
            }
            return chunk_header;
        }

        public static byte[] HeaderToBytes(sparseFileHeader fileHeader)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(fileHeader.magic);
                bw.Write(fileHeader.majorVersion);
                bw.Write(fileHeader.minorVersion);
                bw.Write(fileHeader.fileHeaderSize);
                bw.Write(fileHeader.chunkHeaderSize);
                bw.Write(fileHeader.blockSize);
                bw.Write(fileHeader.totalBlocks);
                bw.Write(fileHeader.totalChunks);
                bw.Write(fileHeader.imageChecksum);

                return ms.ToArray();
            }
        }

        public static byte[] ChunkHeaderToBytes(sparseChunkHeader chunk)
        {
            if (chunk.chunkType != (uint)chunkTypes.DONT_CARE) throw new Exception("Only DONT_CARE chunks can be generated in this lib.");

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(chunk.chunkType);
                bw.Write(chunk.reserved);
                bw.Write(chunk.chunkSize);
                bw.Write(chunk.totalSize);

                return ms.ToArray();
            }
        }
    }
}
