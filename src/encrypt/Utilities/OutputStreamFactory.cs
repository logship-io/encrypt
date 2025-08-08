namespace encrypt.Utilities
{
    internal class OutputStreamFactory
    {
        private readonly Stream? defaultOutputStream;

        public OutputStreamFactory(Stream? outputStream)
        {
            this.defaultOutputStream = outputStream;
        }

        public Stream CreateFileStream(string filePath)
        {
            if (null != this.defaultOutputStream)
            {
                return this.defaultOutputStream;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            if (filePath.Equals("-", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("File path cannot be '-' for file stream creation.", nameof(filePath));
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                //throw new Exception($"File already exists at path: {filePath}. Please choose a different path.");
            }

            return new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
    }
}
