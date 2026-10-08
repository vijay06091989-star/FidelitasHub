using Microsoft.AspNetCore.Http;

namespace FidelitasHub.Middleware
{
    /// <summary>
    /// Enforces the Productivity role split at the HTTP layer.
    /// Employee / Team Leader -> Dashboard, Register and Reports.
    /// Manager / Admin / SuperAdmin -> Productivity Setup and configuration.
    /// Legacy Viewer / Editor restrictions are retained for the rest of the Hub.
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
            var role = context.Session.GetString("Role")?.Trim() ?? string.Empty;

            if (context.Request.Path.StartsWithSegments("/Account"))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path;
            if (path.StartsWithSegments("/Productivity"))
            {
                var action = context.Request.RouteValues["action"]?.ToString() ?? string.Empty;

                var canView = role.Equals("Employee", StringComparison.OrdinalIgnoreCase) ||
                              role.Equals("Team Leader", StringComparison.OrdinalIgnoreCase);

                var canManage = role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase);

                // Legacy Productivity-only accounts are allowed through to the
                // controller so they receive the normal authorization result
                // rather than getting caught in a redirect loop.
                if (role.Equals("Viewer", StringComparison.OrdinalIgnoreCase) ||
                    role.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

                var viewerActions = new[] { "Index", "Register", "Reports", "ExportReport", "UploadSpreadsheet" };
                var managerActions = new[]
                {
                    "Setup", "CreateProcess", "CreateActivity", "CreateAssignment",
                    "CreateGroup", "SaveGroupMembers", "CreateGroupAssignment",
                    "DeactivateGroupAssignment", "DeactivateGroupMember", "DeactivateAssignment",
                    "Client", "WebLogins"
                };

                if ((canView || canManage) && viewerActions.Any(x => x.Equals(action, StringComparison.OrdinalIgnoreCase)))
                {
                    await _next(context);
                    return;
                }

                if (canManage && managerActions.Any(x => x.Equals(action, StringComparison.OrdinalIgnoreCase)))
                {
                    await _next(context);
                    return;
                }

                context.Response.Redirect(canManage ? "/Productivity/Setup" : "/Productivity/Index");
                return;
            }

            // Retain the legacy special-role restriction for Viewer / Editor accounts.
            var isViewer = role.Equals("Viewer", StringComparison.OrdinalIgnoreCase);
            var isEditor = role.Equals("Editor", StringComparison.OrdinalIgnoreCase);

            if (!isViewer && !isEditor)
            {
                await _next(context);
                return;
            }

            if (isEditor && path.StartsWithSegments("/Client"))
            {
                await _next(context);
                return;
            }

            context.Response.Redirect("/Productivity/Index");
        }
    }
}

