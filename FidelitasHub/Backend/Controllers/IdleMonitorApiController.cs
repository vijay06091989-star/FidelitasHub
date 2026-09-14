using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    [ApiController]
    [Route("api/idle-monitor")]
    public class IdleMonitorApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IdleMonitorApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("enabled")]
        public IActionResult GetMonitoringEnabled(
            [FromQuery] string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "EmployeeCode is required."
                });
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeCode == employeeCode &&
                    e.IsActive);

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Employee not found or inactive."
                });
            }

            return Ok(new
            {
                success = true,
                enabled = employee.EnableIdleMonitoring
            });
        }

        [HttpPost("status")]
        public IActionResult UpdateStatus(
            [FromBody] IdleMonitorStatusRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.EmployeeCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "EmployeeCode is required."
                });
            }

            var status = request.Status?.Trim();

            if (!string.Equals(
                    status,
                    "Idle",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Status must be Idle or Active."
                });
            }

            var employee = _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeCode == request.EmployeeCode &&
                    e.IsActive);

            if (employee == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Employee not found or inactive."
                });
            }

            if (!employee.EnableIdleMonitoring)
            {
                return Ok(new
                {
                    success = true,
                    ignored = true,
                    message =
                        "Idle monitoring is disabled for this employee."
                });
            }

            var now = DateTimeHelper.GetIST();

            var attendance = _context.Attendances
                .Where(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.PunchIn != null &&
                    a.PunchOut == null)
                .OrderByDescending(a => a.PunchIn)
                .FirstOrDefault();

            if (attendance == null)
            {
                return Ok(new
                {
                    success = true,
                    ignored = true,
                    message =
                        "Employee has no active attendance."
                });
            }

            if (string.Equals(
                status,
                "Idle",
                StringComparison.OrdinalIgnoreCase))
            {
                if (attendance.Status != "Working")
                {
                    return Ok(new
                    {
                        success = true,
                        ignored = true,
                        message =
                            "Employee is not currently working."
                    });
                }

                var existingOpenSession =
                    _context.EmployeeIdleSessions
                    .FirstOrDefault(i =>
                        i.AttendanceId ==
                            attendance.AttendanceId &&
                        i.IdleEnd == null);

                if (existingOpenSession == null)
                {
                    _context.EmployeeIdleSessions.Add(
                        new EmployeeIdleSession
                        {
                            EmployeeId =
                                employee.EmployeeId,

                            AttendanceId =
                                attendance.AttendanceId,

                            IdleStart =
                                now,

                            ComputerName =
                                request.ComputerName?.Trim(),

                            WindowsUserName =
                                request.WindowsUserName?.Trim()
                        });

                    _context.SaveChanges();
                }

                return Ok(new
                {
                    success = true,
                    status = "Idle",
                    serverTime = now
                });
            }

            var openSession =
                _context.EmployeeIdleSessions
                .Where(i =>
                    i.AttendanceId ==
                        attendance.AttendanceId &&
                    i.IdleEnd == null)
                .OrderByDescending(
                    i => i.IdleStart)
                .FirstOrDefault();

            if (openSession != null)
            {
                openSession.IdleEnd = now;

                openSession.DurationSeconds =
                    Math.Max(
                        0,
                        (int)Math.Round(
                            (now - openSession.IdleStart)
                            .TotalSeconds));

                _context.SaveChanges();
            }

            return Ok(new
            {
                success = true,
                status = "Active",
                serverTime = now
            });
        }

        public class IdleMonitorStatusRequest
        {
            public string EmployeeCode { get; set; } =
                string.Empty;

            public string Status { get; set; } =
                string.Empty;

            public string? ComputerName { get; set; }

            public string? WindowsUserName { get; set; }
        }
    }
}