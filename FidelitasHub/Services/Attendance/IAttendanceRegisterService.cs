using FidelitasHub.Models;

namespace FidelitasHub.Services.Attendance
{
    public interface IAttendanceRegisterService
    {
        List<AttendanceRegisterViewModel> GetAttendanceRegister(
    AttendanceRegisterRequest request);

        List<Employee> GetActiveEmployees();

        List<DateTime> GenerateDateList(
            DateTime fromDate,
            DateTime toDate);
    }
}