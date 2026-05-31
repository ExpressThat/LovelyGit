using System.Net;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using WindowsCredentialManager = AdysTech.CredentialManager.CredentialManager;

namespace ExpressThat.LovelyGit.Services.Keyring;

internal static class Keyring
{
    private const string PackageName = "expressthat.lovelygit";
    private const string Service = "Security";
    private const string Account = "MasterPassword";

    public static string GetPassword()
    {
        var password = TryGetPassword();
        if (password is not null)
        {
            return password;
        }

        password = CreatePassword();
        SetPassword(password);
        return password;
    }

    private static string? TryGetPassword()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return WindowsCredentialManager.GetCredentials(CreateWindowsTargetName())?.Password;
        }

        try
        {
            return KeySharp.Keyring.GetPassword(PackageName, Service, Account);
        }
        catch (KeySharp.KeyringException)
        {
            return null;
        }
    }

    private static void SetPassword(string password)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetWindowsPassword(password);
            return;
        }

        KeySharp.Keyring.SetPassword(PackageName, Service, Account, password);
    }

    private static void SetWindowsPassword(string password)
    {
        var credential = new NetworkCredential(Account, password);

        WindowsCredentialManager.SaveCredentials(CreateWindowsTargetName(), credential);
    }

    private static string CreatePassword()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string CreateWindowsTargetName()
    {
        return $"{PackageName}:{Service}:{Account}";
    }
}
