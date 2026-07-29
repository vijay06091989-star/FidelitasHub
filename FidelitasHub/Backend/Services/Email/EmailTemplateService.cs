using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Services.Email
{
    public class EmailTemplateService
    {
        private readonly ApplicationDbContext _context;

        public EmailTemplateService(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Get All Templates
        //==================================================

        public async Task<List<EmailTemplate>> GetAllAsync()
        {
            return await _context.EmailTemplates
                .OrderBy(x => x.TemplateName)
                .ToListAsync();
        }

        //==================================================
        // Get Template By Id
        //==================================================

        public async Task<EmailTemplate?> GetByIdAsync(int id)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        //==================================================
        // Get Template By Code
        //==================================================

        public async Task<EmailTemplate?> GetByCodeAsync(string templateCode)
        {
            return await _context.EmailTemplates
                .FirstOrDefaultAsync(x =>
                    x.TemplateCode == templateCode &&
                    x.IsActive);
        }

        //==================================================
        // Render Template By Code
        //==================================================

        public async Task<(string Subject, string Body)> RenderTemplateAsync(
            string templateCode,
            Dictionary<string, string> values)
        {
            var template = await GetByCodeAsync(templateCode);

            if (template == null)
                throw new Exception($"Email Template '{templateCode}' not found.");

            string subject = template.Subject;
            string body = template.Body;

            foreach (var item in values)
            {
                string token = "{" + item.Key + "}";

                subject = subject.Replace(token, item.Value);

                body = body.Replace(token, item.Value);
            }

            return (subject, body);
        }

        //==================================================
        // Render Template By Id
        //==================================================

        public async Task<(string Subject, string Body)> RenderTemplateAsync(
            int templateId,
            Dictionary<string, string> values)
        {
            var template = await GetByIdAsync(templateId);

            if (template == null)
                throw new Exception($"Email Template '{templateId}' not found.");

            return await RenderTemplateAsync(
                template.TemplateCode,
                values);
        }

        //==================================================
        // Save Template
        //==================================================

        public async Task SaveAsync(EmailTemplate template)
        {
            if (template.Id == 0)
            {
                _context.EmailTemplates.Add(template);
            }
            else
            {
                template.ModifiedOn = DateTime.Now;

                _context.EmailTemplates.Update(template);
            }

            await _context.SaveChangesAsync();
        }

        //==================================================
        // Delete Template
        //==================================================

        public async Task DeleteAsync(int id)
        {
            var template = await GetByIdAsync(id);

            if (template == null)
                return;

            _context.EmailTemplates.Remove(template);

            await _context.SaveChangesAsync();
        }
    }
}