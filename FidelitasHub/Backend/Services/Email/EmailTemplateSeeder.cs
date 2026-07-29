using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Services.Email
{
    public class EmailTemplateSeeder
    {
        private readonly ApplicationDbContext _context;

        public EmailTemplateSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            if (await _context.EmailTemplates.AnyAsync())
                return;

            _context.EmailTemplates.Add(new EmailTemplate
            {
                TemplateCode = "WELCOME",
                TemplateName = "Welcome Email",
                Subject = "Welcome to Fidelitas Hub",

                Body = """
                <h2>Welcome {EmployeeName}</h2>

                <p>
                    Welcome to <strong>{CompanyName}</strong>.
                </p>

                <p>
                    Your account has been created successfully.
                </p>

                <p>
                    Regards,<br/>
                    {CompanyName}
                </p>
                """,

                IsActive = true
            });

            await _context.SaveChangesAsync();
        }
    }
}