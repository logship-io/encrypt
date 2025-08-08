using System.Text.Json.Serialization;

namespace encrypt.Encryptors.E2E
{
    [JsonConverter(typeof(JsonStringEnumConverter<EncryptorTypes>))]

    internal enum EncryptorTypes : ushort
    {
        Unknown = 0,
        AES256HMAC256 = 1,
    }
}
