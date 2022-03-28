namespace Hast.Communication.Tester;

public enum PayloadType
{
    /// <summary>
    /// Each cell contains an int value of 1 (00000001h).
    /// </summary>
    ConstantIntOne,

    /// <summary>
    /// Each cell had 1 larger value than the previous one, overflow is permitted.
    /// </summary>
    Counter,

    /// <summary>
    /// Each cell gets a random value.
    /// </summary>
    Random,

    /// <summary>
    /// File gets read as binary.
    /// </summary>
    BinaryFile,

    /// <summary>
    /// File gets read as a bitmap.
    /// </summary>
    Bitmap,
}
