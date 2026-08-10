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

            var today = istNow.Date;
            var yesterday = today.AddDays(-1);

            //==========================================
            // Get today's active attendance records
            // and yesterday's records for possible
            // overnight US shift processing.
            //==========================================

            var attendanceList = await _context.Attendances
                .Where(a =>
                    (a.AttendanceDate.Date == today ||
                     a.AttendanceDate.Date == yesterday) &&
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
                // Determine whether this is an overnight shift
                //==========================================

                bool isOvernight =
                    shift.StandardEndTime <= shift.StandardStartTime;

                //==========================================
                // Yesterday's attendance is relevant ONLY
                // for an overnight shift.
                //==========================================

                if (attendance.AttendanceDate.Date == yesterday &&
                    !isOvernight)
                {
                    continue;
                }

                //==========================================
                // Calculate Auto Punch-Out Time
                //==========================================

                DateTime autoPunchOutTime =
                    attendance.AttendanceDate.Date
                    + shift.StandardEndTime;

                //==========================================
                // Overnight Shift
                //==========================================

                if (isOvernight)
                {
                    autoPunchOutTime =
                        autoPunchOutTime.AddDays(1);
                }

                //==========================================
                // Add allowed Punch-Out window
                //==========================================

                autoPunchOutTime =
                    autoPunchOutTime.AddMinutes(
                        shift.MaximumPunchOutMinutes);

                //==========================================
                // Not yet time for Auto Punch-Out
                //==========================================

                if (istNow < autoPunchOutTime)
                {
                    continue;
                }

                //==========================================
                // Complete Auto Punch-Out
                //==========================================

                await CompletePunchOutAsync(
                    attendance,
                    employee,
                    autoPunchOutTime,
                    true);
            }
        }

        public async Task CompletePunchOutAsync(
    AttendanceModel attendance,
    EmployeeModel employee,
    DateTime punchOutTime,
    bool isAutoPunchOut)
        {
            attendance.PunchOut = punchOutTime;

            attendance.PunchOutMode = isAutoPunchOut
    ? "Auto"
    : "Manual";

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