using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

namespace FidelitasHub.Services.Security
{
    public interface IClientWebLoginProtectionService
    {
        string Protect(string value);

        bool TryUnprotect(string? protectedValue, out string value);
    }

    public sealed class ClientWebLoginProtectionService
        : IClientWebLoginProtectionService
    {
        private readonly IDataProtector _protector;

        public ClientWebLoginProtectionService(
            IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(
                "FidelitasHub.ClientWebLogin.v1");
        }

        public string Protect(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var bytes = Encoding.UTF8.GetBytes(value);

            return Convert.ToBase64String(
                _protector.Protect(bytes));
        }

        public bool TryUnprotect(
            string? protectedValue,
            out string value)
        {
            value = string.Empty;

            if (string.IsNullOrWhiteSpace(protectedValue))
            {
                return true;
            }

            try
            {
                var bytes = Convert.FromBase64String(
                    protectedValue);

                value = Encoding.UTF8.GetString(
                    _protector.Unprotect(bytes));

                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
