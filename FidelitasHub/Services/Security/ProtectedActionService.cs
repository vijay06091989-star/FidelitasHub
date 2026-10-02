using Microsoft.AspNetCore.Http;

namespace FidelitasHub.Services.Security
{
    public interface IProtectedActionService
    {
        bool VerifyPin(string? pin);

        bool IsAuthorized(
            HttpContext httpContext,
            string action,
            int resourceId);

        void Grant(
            HttpContext httpContext,
            string action,
            int resourceId);

        void Revoke(
            HttpContext httpContext,
            string action,
            int resourceId);

        void RevokeAll(
            HttpContext httpContext,
            string action);
    }

    public sealed class ProtectedActionService : IProtectedActionService
    {
        public const string EmployeeEditAction = "EMPLOYEE_EDIT";

        private const string DefaultEmployeeEditPin = "2016";

        private const int AuthorizationMinutes = 5;

        public bool VerifyPin(string? pin)
        {
            if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4)
                return false;

            var configuredPin =
                Environment.GetEnvironmentVariable(
                    "FIDELITAS_EMPLOYEE_EDIT_PIN");

            configuredPin = string.IsNullOrWhiteSpace(configuredPin)
                ? DefaultEmployeeEditPin
                : configuredPin.Trim();

            return string.Equals(
                pin.Trim(),
                configuredPin,
                StringComparison.Ordinal);
        }

        public bool IsAuthorized(
            HttpContext httpContext,
            string action,
            int resourceId)
        {
            var key = BuildKey(action, resourceId);
            var value = httpContext.Session.GetString(key);

            if (!long.TryParse(value, out var ticks))
                return false;

            var expiresAt = new DateTime(
                ticks,
                DateTimeKind.Utc);

            if (expiresAt <= DateTime.UtcNow)
            {
                httpContext.Session.Remove(key);
                return false;
            }

            return true;
        }

        public void Grant(
            HttpContext httpContext,
            string action,
            int resourceId)
        {
            var expiresAt = DateTime.UtcNow.AddMinutes(
                AuthorizationMinutes);

            httpContext.Session.SetString(
                BuildKey(action, resourceId),
                expiresAt.Ticks.ToString());
        }

        public void Revoke(
            HttpContext httpContext,
            string action,
            int resourceId)
        {
            httpContext.Session.Remove(
                BuildKey(action, resourceId));
        }

        public void RevokeAll(
            HttpContext httpContext,
            string action)
        {
            var prefix = $"ProtectedAction:{action}:";

            foreach (var key in httpContext.Session.Keys
                         .Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
            {
                httpContext.Session.Remove(key);
            }
        }

        private static string BuildKey(
            string action,
            int resourceId)
        {
            return $"ProtectedAction:{action}:{resourceId}";
        }
    }
}
