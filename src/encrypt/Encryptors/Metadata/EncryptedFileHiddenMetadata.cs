using System.Text.Json.Serialization;

namespace encrypt.Encryptors.Metadata
{
    internal sealed class EncryptedFileHiddenMetadata
    {
        public required string InputFileName { get; set; }
    }
}
