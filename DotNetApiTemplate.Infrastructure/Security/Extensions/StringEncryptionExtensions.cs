using System.Globalization;
using System.Net;
using System.Security.Cryptography;

namespace DotNetApiTemplate.Infrastructure.Security.Extensions;

public static class StringEncryptionExtensions
{
    private const string LegacyTokenDateFormat = "yyyyMMddHHmmss";
    private const string Version2TokenDateFormat = "yyyy-MM-ddTHH:mm:ssZ";

    public static string Encrypt(this string text, string key, string iv)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var (encryptionKey, encryptionIv) = GetEncryptionParameters(key, iv);

        return EncryptAes(text, encryptionKey, encryptionIv);
    }

    public static string Decrypt(this string text, string key, string iv)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var (encryptionKey, encryptionIv) = GetEncryptionParameters(key, iv);
        var decodedText = WebUtility.UrlDecode(text.Trim());
        var cipher = Convert.FromBase64String(decodedText);

        return DecryptAes(cipher, encryptionKey, encryptionIv);
    }

    public static string GetToken(
        this string userName,
        string siteSecretKey,
        string siteIv,
        string globalSecretKey,
        string globalIv,
        string sitename,
        string defaultUser)
    {
        _ = sitename;

        var correctDateTime = GetUkDateTime();

        if (string.IsNullOrEmpty(siteSecretKey) || string.IsNullOrEmpty(siteIv))
        {
            return string.Empty;
        }

        var tokenText = string.IsNullOrEmpty(defaultUser)
            ? $"{correctDateTime.ToString(LegacyTokenDateFormat, CultureInfo.InvariantCulture)}{userName}"
            : $"{correctDateTime.ToString(LegacyTokenDateFormat, CultureInfo.InvariantCulture)}{defaultUser}";

        var (unencryptedSecretKey, unencryptedIv) = DecryptSiteCredentials(
            siteSecretKey,
            siteIv,
            globalSecretKey,
            globalIv);

        return tokenText.Encrypt(unencryptedSecretKey, unencryptedIv);
    }

    public static string GetToken(
        this string userName,
        string siteSecretKey,
        string siteIv,
        string globalSecretKey,
        string globalIv,
        string sitename,
        string defaultUser,
        bool extraTokenInformation,
        string firstName,
        string lastName,
        string email,
        string companyName)
    {
        _ = sitename;

        var correctDateTimeUk = GetUkDateTime();

        if (string.IsNullOrEmpty(siteSecretKey) || string.IsNullOrEmpty(siteIv))
        {
            return string.Empty;
        }

        var (unencryptedSecretKey, unencryptedIv) = DecryptSiteCredentials(
            siteSecretKey,
            siteIv,
            globalSecretKey,
            globalIv);
        var usernameToUse = string.IsNullOrEmpty(defaultUser) ? userName ?? string.Empty : defaultUser;

        string tokenText;
        if (!extraTokenInformation)
        {
            tokenText = $"{correctDateTimeUk.ToString(LegacyTokenDateFormat, CultureInfo.InvariantCulture)}{usernameToUse}";
        }
        else
        {
            var dtUtc = DateTime.UtcNow.ToString(Version2TokenDateFormat, CultureInfo.InvariantCulture);

            static string Encode(string? value) => WebUtility.UrlEncode(value ?? string.Empty);

            tokenText =
                "v=2" +
                $"&dt={Encode(dtUtc)}" +
                $"&username={Encode(usernameToUse)}" +
                $"&firstName={Encode(firstName)}" +
                $"&lastName={Encode(lastName)}" +
                $"&email={Encode(email)}" +
                $"&companyName={Encode(companyName)}";
        }

        return tokenText.Encrypt(unencryptedSecretKey, unencryptedIv);
    }

    private static DateTime GetUkDateTime()
    {
        var utcNow = DateTime.UtcNow;

        try
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
        }
        catch (TimeZoneNotFoundException)
        {
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timezone);
        }
    }

    private static (byte[] Key, byte[] Iv) GetEncryptionParameters(string key, string iv)
    {
        return (
            Convert.FromBase64String(key.Trim()),
            Convert.FromBase64String(iv.Trim()));
    }

    private static (string SecretKey, string Iv) DecryptSiteCredentials(
        string siteSecretKey,
        string siteIv,
        string globalSecretKey,
        string globalIv)
    {
        return (
            siteSecretKey.Decrypt(globalSecretKey, globalIv),
            siteIv.Decrypt(globalSecretKey, globalIv));
    }

    private static Aes CreateConfiguredAes()
    {
        var aes = Aes.Create();
        aes.BlockSize = 128;
        aes.KeySize = 256;
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;

        return aes;
    }

    private static string EncryptAes(string plainText, byte[] key, byte[] iv)
    {
        try
        {
            using var aes = CreateConfiguredAes();

            using var encryptor = aes.CreateEncryptor(key, iv);
            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return WebUtility.UrlEncode(Convert.ToBase64String(memoryStream.ToArray()));
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Error whilst encrypting", ex);
        }
    }

    private static string DecryptAes(byte[] cipherText, byte[] key, byte[] iv)
    {
        try
        {
            using var aes = CreateConfiguredAes();

            using var decryptor = aes.CreateDecryptor(key, iv);
            using var memoryStream = new MemoryStream(cipherText);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);

            return streamReader.ReadToEnd();
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Error whilst decrypting", ex);
        }
    }
}
