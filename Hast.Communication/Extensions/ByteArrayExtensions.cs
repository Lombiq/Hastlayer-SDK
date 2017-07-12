namespace System
{
    internal static class ByteArrayExtensions
    {
        /// <summary>
        /// Appends a byte array to another array.
        /// </summary>
        /// <param name="additionalBytes">Byte array to append the original array with.</param>
        /// <returns>A new byte array containing the original and the additional bytes.</returns>
        public static byte[] Append(this byte[] originalBytes, byte[] additionalBytes)
        {
            var newByteArray = new byte[originalBytes.Length + additionalBytes.Length];

            Array.Copy(originalBytes, 0, newByteArray, 0, originalBytes.Length);
            Array.Copy(additionalBytes, 0, newByteArray, originalBytes.Length, additionalBytes.Length);

            return newByteArray;
        }
    }
}
