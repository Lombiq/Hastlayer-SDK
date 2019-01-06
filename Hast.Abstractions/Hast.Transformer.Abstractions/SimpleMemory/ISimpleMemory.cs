namespace Hast.Transformer.Abstractions.SimpleMemory
{
    public interface ISimpleMemory
    {
        int CellCount { get; }

        byte[] Read4Bytes(int cellIndex);
        byte[][] Read4Bytes(int startCellIndex, int count);
        bool ReadBoolean(int cellIndex);
        bool[] ReadBoolean(int startCellIndex, int count);
        int ReadInt32(int cellIndex);
        int[] ReadInt32(int startCellIndex, int count);
        uint ReadUInt32(int cellIndex);
        uint[] ReadUInt32(int startCellIndex, int count);
        void Write4Bytes(int cellIndex, byte[] bytes);
        void Write4Bytes(int startCellIndex, params byte[][] bytesMatrix);
        void WriteBoolean(int cellIndex, bool boolean);
        void WriteBoolean(int startCellIndex, params bool[] booleans);
        void WriteInt32(int cellIndex, int number);
        void WriteInt32(int startCellIndex, params int[] numbers);
        void WriteUInt32(int startCellIndex, params uint[] numbers);
        void WriteUInt32(int cellIndex, uint number);
    }
}