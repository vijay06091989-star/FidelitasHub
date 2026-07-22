using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using Microsoft.EntityFrameworkCore;
using AttendanceModel = FidelitasHub.Models.Attendance;
using EmployeeModel = FidelitasHub.Models.Employee;

namespace FidelitasHub.Services.Attendance
{
    public class AttendanceProcessingService
        : IAttendanceProcessingService
    {
        private readonly ApplicationDbContext _context;

        public AttendanceProcessingService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ProcessAutoPunchOutAsync()
        {
            Console.WriteLine(
    $"Auto Punch-Out Check : {DateTimeHelper.GetIST()}");

            var istNow = DateTimeHelper.GetIST();

            var attendanceList = await _context.Attendances
                .Where(a =>
                    a.AttendanceDate.Date == istNow.Date &&
                    (a.Status == "Working" ||
                     a.Status == "On Break"))
                .ToListAsync();

            foreach (var attendance in attendanceList)
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e =>
                        e.EmployeeId == attendance.EmployeeId);

                if (employee == null)
                    continue;

                var shift = await _context.Shifts
                    .FirstOrDefaultAsync(s =>
                        s.ShiftId == employee.ShiftId);

                if (shift == null)
                    continue;

                //==========================================
                // Auto Punch-Out Time
                //==========================================

                DateTime autoPunchOutTime =
                    attendance.AttendanceDate.Date
                    + shift.StandardEndTime;

                //==========================================
                // Overnight Shift Support
                //==========================================

                if (shift.StandardEndTime < shift.StandardStartTime)
                {
                    autoPunchOutTime = autoPunchOutTime.AddDays(1);
                }

                autoPunchOutTime = autoPunchOutTime.AddMinutes(
                    shift.MaximumPunchOutMinutes);

                if (istNow < autoPunchOutTime)
                {
                    continue;
                }

                await CompletePunchOutAsync(
    attendance,
    employee,
    autoPunchOutTime,
    true);

                // Auto Punch-Out logic will be moved here
                // after CompletePunchOutAsync() is implemented.
            }
        }

        public async Task CompletePunchOutAsync(
    AttendanceModel attendance,
    EmployeeModel employee,
    DateTime punchOutTime,
    bool isAutoPunchOut)
        {
            attendance.PunchOut = punchOutTime;

            attendance.Status = "Punched Out";

            //==========================================
            // Worked Minutes
            //==========================================

            int workedMinutes =
                (int)Math.Max(
                    0,
                    (attendance.PunchOut.Value - attendance.PunchIn.Value)
                    .TotalMinutes
                    - attendance.TotalBreakMinutes);

            attendance.WorkedMinutes = workedMinutes;

            //==========================================
            // Audit Log
            //==========================================

            _context.AuditLogs.Add(new AuditLog
            {
                EmployeeId = employee.EmployeeId,
                Action = "Punch Out",
                ActionTime = punchOutTime,
                Remarks = isAutoPunchOut
                    ? "Auto Punch Out"
                    : "Manual Punch Out",
                IPAddress = isAutoPunchOut
                    ? "SYSTEM"
                    : "",
                ComputerName = isAutoPunchOut
                    ? "BACKGROUND SERVICE"
                    : Environment.MachineName
            });

            await _context.SaveChangesAsync();
        }
    }
}