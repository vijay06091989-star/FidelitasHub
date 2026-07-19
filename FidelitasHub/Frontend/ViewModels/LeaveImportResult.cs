namespace FidelitasHub.Models
{
    public class LeaveImportResult
    {
        public int Imported { get; set; }

        public int Updated { get; set; }

        public int Skipped { get; set; }

        public int Errors { get; set; }
    }
}