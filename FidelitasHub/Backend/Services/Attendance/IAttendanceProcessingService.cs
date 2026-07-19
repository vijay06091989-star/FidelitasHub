using AttendanceModel = FidelitasHub.Models.Attendance;
using EmployeeModel = FidelitasHub.Models.Employee;

namespace FidelitasHub.Services.Attendance
{
    public interface IAttendanceProcessingService
    {
        Task ProcessAutoPunchOutAsync();

        Task CompletePunchOutAsync(
    AttendanceModel attendance,
    EmployeeModel employee,
    DateTime punchOutTime,
    bool isAutoPunchOut);
    }
}