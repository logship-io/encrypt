using encrypt.Encryptors.E2E;
using System.Text.Json.Serialization;

namespace encrypt.Encryptors.Metadata
{
    [JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(EncryptedFileHiddenMetadata))]
    [JsonSerializable(typeof(EncryptedFilePublicMetadata))]
    [JsonSerializable(typeof(EncryptorTypes))]
    internal partial class EncryptSourceGenerationContext : JsonSerializerContext
    {
    }
}
