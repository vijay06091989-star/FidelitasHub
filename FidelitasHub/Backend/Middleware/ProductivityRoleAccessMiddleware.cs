using Microsoft.AspNetCore.Http;

namespace FidelitasHub.Middleware
{
    /// <summary>
    /// Restricts the special Productivity-only roles so they cannot enter
    /// the normal HR/attendance/leave/admin parts of Fidelitas Hub.
    ///
    /// Viewer  -> Productivity only.
    /// Editor  -> Productivity + Client Master.
    ///
    /// This is deliberately enforced server-side in addition to hiding
    /// navigation links in the sidebar.
    /// </summary>
    public class ProductivityRoleAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public ProductivityRoleAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var role = context.Session.GetString("Role")?.Trim();

            var isViewer = string.Equals(
                role,
                "Viewer",
                StringComparison.OrdinalIgnoreCase);

            var isEditor = string.Equals(
                role,
                "Editor",
                StringComparison.OrdinalIgnoreCase);

            if (!isViewer && !isEditor)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path;

            // Account pages must remain available so these users can
            // change password, use forgot-password and log out.
            if (path.StartsWithSegments("/Account"))
            {
                await _next(context);
                return;
            }

            // Both roles can view the Productivity dashboard, individual
            // client productivity pages and client web logins. Editor additionally gets the full
            // Productivity area (including future setup/register/report pages).
            if (path.StartsWithSegments("/Productivity"))
            {
                if (isEditor)
                {
                    await _next(context);
                    return;
                }

                var productivityAction =
                    context.Request.RouteValues["action"]?.ToString();

                if (string.Equals(
                        productivityAction,
                        "Index",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        productivityAction,
                        "Client",
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        productivityAction,
                        "WebLogins",
                        StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                context.Response.Redirect("/Productivity/Index");
                return;
            }

            // Editor additionally gets the Client Master, including
            // Create/Edit/SOP/Enable/Disable operations.
            if (isEditor &&
                path.StartsWithSegments("/Client"))
            {
                await _next(context);
                return;
            }

            // Keep the special accounts completely out of the normal
            // attendance, leave, reports, masters, system and email flow.
            context.Response.Redirect("/Productivity/Index");
        }
    }
}
