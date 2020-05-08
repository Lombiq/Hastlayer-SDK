namespace Hast.Communication.Tester
{
    public enum OutputFileType
    {
        /// <summary>
        /// No output file is to be generated.
        /// </summary>
        None,

        /// <summary>
        /// The output is saved as a text file containing a sequence of hexadecimal numbers in 8 digit groups.
        /// </summary>
        Hexdump,

        /// <summary>
        /// The output is saved as raw binary file.
        /// </summary>
        Binary,

        /// <summary>
        /// The output is saved as a JPEG file.
        /// </summary>
        BitmapJpeg
    }
}
