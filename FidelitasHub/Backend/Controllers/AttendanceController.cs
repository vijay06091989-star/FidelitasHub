    using ClosedXML.Excel;
    using FidelitasHub.Data;
    using FidelitasHub.Helpers;
    using FidelitasHub.Models;
    using FidelitasHub.Services.Attendance;
using FidelitasHub.Services;
using Microsoft.AspNetCore.Mvc;
    using System.IO;

    namespace FidelitasHub.Controllers
    {
        public class AttendanceController : Controller
        {
        private readonly ApplicationDbContext _context;
        private readonly IAttendanceRegisterService _attendanceRegisterService;
        private readonly IAttendanceProcessingService _attendanceProcessingService;
        private readonly ReportingService _reportingService;

        public AttendanceController(
    ApplicationDbContext context,
    IAttendanceRegisterService attendanceRegisterService,
    IAttendanceProcessingService attendanceProcessingService,
    ReportingService reportingService)
        {
            _context = context;
            _attendanceRegisterService = attendanceRegisterService;
            _attendanceProcessingService = attendanceProcessingService;
            _reportingService = reportingService;
        }

        //====================================================
        // ATTENDANCE DASHBOARD
        //====================================================

        [HttpGet]
            public IActionResult Dashboard()
            {
                //--------------------------------------------------
                // Get Logged-in Employee
                //--------------------------------------------------

                var employeeCode = HttpContext.Session.GetString("EmployeeCode");

                if (string.IsNullOrEmpty(employeeCode))
                {
                    return RedirectToAction("Login", "Account");
                }

                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                //--------------------------------------------------
                // SuperAdmin is not attendance-tracked
                //--------------------------------------------------

                if (IsAttendanceExempt(employee))
                {
                    ViewBag.EmployeeName = employee.EmployeeName;
                    ViewBag.ShiftName = "Not Required";
                    ViewBag.ShiftTiming = "-";
                    ViewBag.AttendanceStatus = "Attendance Not Applicable";
                    ViewBag.CanPunchIn = false;
                    ViewBag.PunchInTime = "--";
                    ViewBag.PunchOutTime = "--";
                    ViewBag.TotalBreakTime = "--";
                    ViewBag.WorkedTime = "--";
                    ViewBag.BreakStartTime = "--";
                    ViewBag.BreakEndTime = "--";
                    ViewBag.IsOnApprovedLeave = false;
                    ViewBag.LeaveStatus = "";

                    return View();
                }

                //--------------------------------------------------
                // Get Shift
                //--------------------------------------------------

                var shift = _context.Shifts
                    .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

                ViewBag.EmployeeName = employee.EmployeeName;

                if (shift != null)
                {
                    ViewBag.ShiftName = shift.ShiftName;

                    var attendanceDate =
                        GetAttendanceDateForCurrentShift(
                            shift,
                            DateTimeHelper.GetIST());

                    var applicableStartTime =
                        ShiftTimeHelper.GetStartTime(
                            shift,
                            attendanceDate);

                    var applicableEndTime =
                        ShiftTimeHelper.GetEndTime(
                            shift,
                            attendanceDate);

                    ViewBag.ShiftTiming =
                        $"{attendanceDate.Add(applicableStartTime):hh:mm tt} - " +
                        $"{attendanceDate.Add(applicableEndTime):hh:mm tt}";
                }
                else
                {
                    ViewBag.ShiftName = "Not Assigned";
                    ViewBag.ShiftTiming = "-";
                }

                //--------------------------------------------------
                // Current IST Time
                //--------------------------------------------------

                var istNow = DateTimeHelper.GetIST();

                //--------------------------------------------------
                // Get Active Attendance
                //--------------------------------------------------

                var todayAttendance = shift != null
                    ? GetActiveAttendance(
                        employee,
                        shift,
                        istNow)
                    : null;

                //--------------------------------------------------
                // Determine Approved Leave
                //--------------------------------------------------

                LeaveApplication? approvedLeave = null;

                bool leaveIsActive = false;

                if (shift != null)
                {
                    var attendanceDate =
                        GetAttendanceDateForCurrentShift(
                            shift,
                            istNow);

                    approvedLeave =
                        _context.LeaveApplications
                            .FirstOrDefault(l =>
                                l.EmployeeId == employee.EmployeeId &&
                                l.Status == "Approved" &&
                                attendanceDate >= l.FromDate.Date &&
                                attendanceDate <= l.ToDate.Date);

                    if (approvedLeave != null)
                    {
                        leaveIsActive =
                            IsHalfDayLeaveActive(
                                approvedLeave,
                                shift,
                                istNow);
                    }
                }

            //--------------------------------------------------
            // Today's Attendance Status
            //--------------------------------------------------

            //====================================================
            // ATTENDANCE / BUTTON STATE
            //====================================================

            bool isFullDayApprovedLeave =
                approvedLeave != null &&
                !approvedLeave.IsMorningHalf &&
                !approvedLeave.IsAfternoonHalf;

            bool hasPunchedIn =
                todayAttendance?.PunchIn.HasValue == true;

            //====================================================
            // PUNCH IN PERMISSION
            //====================================================
            //
            // ONLY full-day approved leave blocks Punch In.
            //
            // Half-day leave NEVER blocks Punch In.
            //
            // Existing Punch In also blocks another Punch In.
            //====================================================

            ViewBag.CanPunchIn =
                !isFullDayApprovedLeave &&
                !hasPunchedIn;

            //====================================================
            // DISPLAY STATUS
            //====================================================

            if (!hasPunchedIn)
            {
                ViewBag.AttendanceStatus =
                    isFullDayApprovedLeave
                        ? "On Leave"
                        : "Not Punched In";

                ViewBag.PunchInTime = "--";
                ViewBag.PunchOutTime = "--";
                ViewBag.TotalBreakTime = "00:00";
                ViewBag.WorkedTime = "00:00";
                ViewBag.BreakStartTime = "--";
                ViewBag.BreakEndTime = "--";
            }
            else
            {
                ViewBag.AttendanceStatus = todayAttendance!.Status;

                ViewBag.PunchInTime =
                    todayAttendance.PunchIn?.ToString("hh:mm:ss tt");

                ViewBag.PunchOutTime =
                    todayAttendance.PunchOut?.ToString("hh:mm:ss tt") ?? "--";

                ViewBag.TotalBreakTime =
                    TimeSpan.FromMinutes(
                        todayAttendance.TotalBreakMinutes)
                        .ToString(@"hh\:mm");

                ViewBag.WorkedTime =
                    TimeSpan.FromMinutes(
                        todayAttendance.WorkedMinutes)
                        .ToString(@"hh\:mm");

                var lastBreak = _context.AttendanceBreaks
                    .Where(b =>
                        b.AttendanceId == todayAttendance.AttendanceId)
                    .OrderByDescending(b => b.BreakStart)
                    .FirstOrDefault();

                if (lastBreak != null)
                {
                    ViewBag.BreakStartTime =
                        lastBreak.BreakStart.ToString("hh:mm:ss tt");

                    ViewBag.BreakEndTime =
                        lastBreak.BreakEnd?.ToString("hh:mm:ss tt") ?? "--";
                }
                else
                {
                    ViewBag.BreakStartTime = "--";
                    ViewBag.BreakEndTime = "--";
                }
            }

            

                //--------------------------------------------------
                // Leave Information
                //--------------------------------------------------

                ViewBag.IsOnApprovedLeave =
                    approvedLeave != null && leaveIsActive;

                ViewBag.LeaveStatus =
                    approvedLeave != null && leaveIsActive
                        ? (approvedLeave.IsMorningHalf
                            ? "Approved Leave - First Half"
                            : approvedLeave.IsAfternoonHalf
                                ? "Approved Leave - Second Half"
                                : "Approved Leave")
                        : "";

                return View();
            }

            //====================================================
            // PUNCH IN
            //====================================================

            [HttpPost]
    public IActionResult PunchIn()
    {
        var employeeCode = HttpContext.Session.GetString("EmployeeCode");

        if (string.IsNullOrEmpty(employeeCode))
        {
            return Json(new
            {
                success = false,
                title = "Session Expired",
                message = "Please login again."
            });
        }

        var employee = _context.Employees
            .FirstOrDefault(e => e.EmployeeCode == employeeCode);

        if (employee == null)
        {
            return Json(new
            {
                success = false,
                title = "Employee",
                message = "Employee not found."
            });
        }

        if (IsAttendanceExempt(employee))
        {
            return AttendanceNotApplicableResponse();
        }

        var shift = _context.Shifts
            .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

        if (shift == null)
        {
            return Json(new
            {
                success = false,
                title = "Shift",
                message = "No shift assigned."
            });
        }

        var istNow = DateTimeHelper.GetIST();

                //====================================================
                // DETERMINE CORRECT ATTENDANCE DATE
                //====================================================

                var attendanceDate =
                GetAttendanceDateForCurrentShift(
                    shift,
                    istNow);

                //====================================================
                // CHECK FULL-DAY APPROVED LEAVE
                //====================================================

                // Full-day approved leave blocks Punch In.
                // Half-day approved leave does NOT block Punch In.

                var fullDayApprovedLeave =
                    _context.LeaveApplications
                        .FirstOrDefault(l =>
                            l.EmployeeId == employee.EmployeeId &&
                            l.Status == "Approved" &&
                            attendanceDate >= l.FromDate.Date &&
                            attendanceDate <= l.ToDate.Date &&
                            !l.IsMorningHalf &&
                            !l.IsAfternoonHalf);

                if (fullDayApprovedLeave != null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Approved Leave",
                        message = "You are on approved full-day leave for today. Punch In is not required."
                    });
                }


                //====================================================
                // CHECK ACTIVE ATTENDANCE
                //====================================================

                var activeAttendance =
            GetActiveAttendance(
                employee,
                shift,
                istNow);

                if (activeAttendance != null && activeAttendance.PunchIn.HasValue)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Already Punched In",
                        message =
                            $"You already have an active attendance record " +
                            $"from {activeAttendance.PunchIn:dd-MMM-yyyy hh:mm tt}."
                    });
                }

                //====================================================
                // SAFETY CHECK
                //
                // Because Employee + AttendanceDate is now UNIQUE,
                // never create another record if one already exists.
                //====================================================

                var existingAttendance =
        _context.Attendances
            .FirstOrDefault(a =>
                a.EmployeeId == employee.EmployeeId &&
                a.AttendanceDate.Date == attendanceDate);

                Attendance attendance;

                if (existingAttendance != null)
                {
                    // Attendance record was already created for leave purposes,
                    // but employee has not punched in yet.
                    if (existingAttendance.PunchIn.HasValue)
                    {
                        return Json(new
                        {
                            success = false,
                            title = "Already Punched In",
                            message =
                                $"You already have an attendance record " +
                                $"from {existingAttendance.PunchIn:dd-MMM-yyyy hh:mm tt}."
                        });
                    }

                    // Reuse the existing attendance record.
                    attendance = existingAttendance;

                    attendance.PunchIn = istNow;
                    attendance.Status = "Working";
                    attendance.TotalBreakMinutes = 0;
                    attendance.PunchOutMode = "Manual";
                }
                else
                {
                    // No attendance record exists — create a new one.
                    attendance = new Attendance
                    {
                        EmployeeId = employee.EmployeeId,
                        AttendanceDate = attendanceDate,
                        PunchIn = istNow,
                        Status = "Working",
                        TotalBreakMinutes = 0,
                        PunchOutMode = "Manual"
                    };

                    _context.Attendances.Add(attendance);
                }

        //====================================================
        // AUDIT LOG
        //====================================================

        _context.AuditLogs.Add(new AuditLog
        {
            EmployeeId = employee.EmployeeId,
            Action = "Punch In",
            ActionTime = istNow,
            Remarks = "Manual Punch In",
            IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            ComputerName = Environment.MachineName
        });

        try
        {
            _context.SaveChanges();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Json(new
            {
                success = false,
                title = "Already Punched In",
                message =
                    "An attendance record already exists for this shift."
            });
        }

                //====================================================
                // GREETING
                //====================================================

                string greeting;

                if (istNow.Hour < 12)
                    greeting = "🌞 Good Morning";
                else if (istNow.Hour < 17)
                    greeting = "☀️ Good Afternoon";
                else
                    greeting = "🌙 Good Evening";

                return Json(new
        {
            success = true,
            title = $"{greeting}, {employee.EmployeeName}",
            message =
    $@"Welcome to Fidelitas Hub

    Shift : {shift.ShiftName}

    Punch In : {attendance.PunchIn:hh:mm:ss tt} IST

    Have a wonderful day!"
        });
    }

            //====================================================
            // BREAK
            //====================================================

            [HttpPost]
            public IActionResult Break()
            {
                var employeeCode = HttpContext.Session.GetString("EmployeeCode");

                if (string.IsNullOrEmpty(employeeCode))
                {
                    return Json(new
                    {
                        success = false,
                        title = "Session Expired",
                        message = "Please login again."
                    });
                }

                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Employee",
                        message = "Employee not found."
                    });
                }

                if (IsAttendanceExempt(employee))
                {
                    return AttendanceNotApplicableResponse();
                }

                var istNow = DateTimeHelper.GetIST();

                var shift = _context.Shifts
        .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

                if (shift == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Shift",
                        message = "No shift assigned."
                    });
                }

                var attendance =
                    GetActiveAttendance(employee, shift, istNow);

            if (attendance == null || !attendance.PunchIn.HasValue)
            {
                return Json(new
                {
                    success = false,
                    title = "Break",
                    message = "Please Punch In before taking a Break."
                });
            }

            if (attendance.Status == "On Break")
                {
                    return Json(new
                    {
                        success = false,
                        title = "Break",
                        message = "You are already on Break."
                    });
                }

                if (attendance.Status == "Punched Out")
                {
                    return Json(new
                    {
                        success = false,
                        title = "Break",
                        message = "You have already punched out."
                    });
                }

            if (attendance.Status != "Working")
            {
                return Json(new
                {
                    success = false,
                    title = "Break",
                    message = "You are not currently working."
                });
            }

            AttendanceBreak attendanceBreak = new AttendanceBreak
                {
                    AttendanceId = attendance.AttendanceId,
                    BreakStart = istNow,
                    BreakEnd = null,
                    DurationMinutes = 0
                };

                _context.AttendanceBreaks.Add(attendanceBreak);

                attendance.Status = "On Break";

                _context.AuditLogs.Add(new AuditLog
                {
                    EmployeeId = employee.EmployeeId,
                    Action = "Break Started",
                    ActionTime = istNow,
                    Remarks = "Employee started a break",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ComputerName = Environment.MachineName
                });

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    title = "☕ Break Started",
                    message =
    $@"Started At

    {istNow:hh:mm:ss tt} IST

    Enjoy your break!"
                });
            }
            //====================================================
            // RESUME
            //====================================================

            [HttpPost]
            public IActionResult Resume()
            {
                var employeeCode = HttpContext.Session.GetString("EmployeeCode");

                if (string.IsNullOrEmpty(employeeCode))
                {
                    return Json(new
                    {
                        success = false,
                        title = "Session Expired",
                        message = "Please login again."
                    });
                }

                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Employee",
                        message = "Employee not found."
                    });
                }

                if (IsAttendanceExempt(employee))
                {
                    return AttendanceNotApplicableResponse();
                }

                var istNow = DateTimeHelper.GetIST();

                var shift = _context.Shifts
        .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

                if (shift == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Shift",
                        message = "No shift assigned."
                    });
                }

                var attendance = GetActiveAttendance(employee, shift, istNow);

                if (attendance == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Resume",
                        message = "Attendance record not found."
                    });
                }

                if (attendance.Status != "On Break")
                {
                    return Json(new
                    {
                        success = false,
                        title = "Resume",
                        message = "You are not currently on Break."
                    });
                }

                var attendanceBreak = _context.AttendanceBreaks
                    .Where(b =>
                        b.AttendanceId == attendance.AttendanceId &&
                        b.BreakEnd == null)
                    .OrderByDescending(b => b.BreakStart)
                    .FirstOrDefault();

                if (attendanceBreak == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Resume",
                        message = "No active break found."
                    });
                }

                attendanceBreak.BreakEnd = istNow;

                attendanceBreak.DurationMinutes =
                    (int)Math.Ceiling(
                        (attendanceBreak.BreakEnd.Value -
                         attendanceBreak.BreakStart).TotalMinutes);

                attendance.TotalBreakMinutes += attendanceBreak.DurationMinutes;

                attendance.Status = "Working";

                _context.AuditLogs.Add(new AuditLog
                {
                    EmployeeId = employee.EmployeeId,
                    Action = "Break Ended",
                    ActionTime = istNow,
                    Remarks = $"Break Duration : {attendanceBreak.DurationMinutes} minutes",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ComputerName = Environment.MachineName
                });

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    title = "▶ Break Ended",
                    message =
            $@"Welcome Back!

    Break Duration : {attendanceBreak.DurationMinutes} minute(s)

    You may continue your work."
                });
            }
            //====================================================
            // PUNCH OUT
            //====================================================

            [HttpPost]
            public IActionResult PunchOut()
            {
                var employeeCode = HttpContext.Session.GetString("EmployeeCode");

                if (string.IsNullOrEmpty(employeeCode))
                {
                    return Json(new
                    {
                        success = false,
                        title = "Session Expired",
                        message = "Please login again."
                    });
                }

                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Employee",
                        message = "Employee not found."
                    });
                }

                if (IsAttendanceExempt(employee))
                {
                    return AttendanceNotApplicableResponse();
                }

                var istNow = DateTimeHelper.GetIST();

                var shift = _context.Shifts
        .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

                if (shift == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Shift",
                        message = "No shift assigned."
                    });
                }

                var attendance =
                    GetActiveAttendance(employee, shift, istNow);

                if (attendance == null)
                {
                    return Json(new
                    {
                        success = false,
                        title = "Punch Out",
                        message = "Please Punch In first."
                    });
                }

            if (!attendance.PunchIn.HasValue)
            {
                return Json(new
                {
                    success = false,
                    title = "Punch Out",
                    message = "Please Punch In first."
                });
            }

            if (attendance.Status == "Punched Out")
                {
                    return Json(new
                    {
                        success = false,
                        title = "Punch Out",
                        message = "You have already punched out."
                    });
                }

                if (attendance.Status == "On Break")
                {
                    return Json(new
                    {
                        success = false,
                        title = "Punch Out",
                        message = "Please Resume before Punching Out."
                    });
                }

            if (attendance.Status != "Working")
            {
                return Json(new
                {
                    success = false,
                    title = "Punch Out",
                    message = "You are not currently working."
                });
            }

            attendance.PunchOut = istNow;

                attendance.Status = "Punched Out";

                //--------------------------------------------------
                // Worked Minutes
                //--------------------------------------------------

                DateTime punchIn = attendance.PunchIn.Value;
                DateTime punchOut = attendance.PunchOut.Value;

                // Overnight Shift
                if (punchOut < punchIn)
                {
                    punchOut = punchOut.AddDays(1);
                }

                attendance.WorkedMinutes =
                    (int)Math.Max(
                        0,
                        (punchOut - punchIn).TotalMinutes
                        - attendance.TotalBreakMinutes);

                _context.AuditLogs.Add(new AuditLog
                {
                    EmployeeId = employee.EmployeeId,
                    Action = "Punch Out",
                    ActionTime = istNow,
                    Remarks = "Manual Punch Out",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ComputerName = Environment.MachineName
                });

                _context.SaveChanges();

                TimeSpan worked =
                    TimeSpan.FromMinutes(attendance.WorkedMinutes);

                TimeSpan breakTime =
                    TimeSpan.FromMinutes(attendance.TotalBreakMinutes);

                return Json(new
                {
                    success = true,
                    title = $"👋 Goodbye {employee.EmployeeName}",
                    message =

    $@"Punch Out Successful

    Worked : {worked:hh\:mm}

    Break : {breakTime:hh\:mm}

    See you tomorrow!"
                });
            }
            //====================================================
            // TODAY'S ATTENDANCE (ADMIN)
            //====================================================

            [HttpGet]
            public IActionResult TodayAttendance(string shift = "General Shift")
            {
                var istNow = DateTimeHelper.GetIST();
                var today = istNow.Date;

                //====================================================
                // DETERMINE WHICH ATTENDANCE DATE(S) ARE RELEVANT
                //====================================================

                DateTime attendanceDate = today;

                //====================================================
                // US SHIFT OVERNIGHT ATTENDANCE
                //====================================================

                if (shift == "US Shift")
                {
                    var usShift = _context.Shifts
                        .FirstOrDefault(s => s.ShiftName == "US Shift");

                    if (usShift != null)
                    {
                        var yesterday = today.AddDays(-1);

                        var startTime =
                            ShiftTimeHelper.GetStartTime(
                                usShift,
                                yesterday);

                        var endTime =
                            ShiftTimeHelper.GetEndTime(
                                usShift,
                                yesterday);

                        // US Shift crosses midnight
                        bool isOvernight = endTime <= startTime;

                        if (isOvernight)
                        {
                            // Maximum allowed punch-out time after
                            // the scheduled shift end.
                            var punchOutCutoff =
                                endTime +
                                TimeSpan.FromMinutes(
                                    Math.Max(
                                        0,
                                        usShift.MaximumPunchOutMinutes));

                            /*
                             * Example:
                             *
                             * US Shift:
                             * 06:30 PM -> 03:30 AM
                             *
                             * 12:00 AM - 03:30 AM
                             *       -> yesterday's attendance
                             *
                             * 03:30 AM - punch-out cutoff
                             *       -> yesterday's attendance
                             *
                             * After punch-out cutoff
                             *       -> today's attendance
                             */

                            if (istNow.TimeOfDay <= punchOutCutoff)
                            {
                                attendanceDate = yesterday;
                            }
                        }
                    }
                }

            //====================================================
            // TODAY'S / ACTIVE SHIFT ATTENDANCE
            //====================================================

            var attendanceList =
(from e in _reportingService.GetVisibleEmployees()

 join d in _context.Departments
                     on e.Department equals d.DepartmentName
                     into dept

                     from d in dept.DefaultIfEmpty()

                     join s in _context.Shifts
                     on e.ShiftId equals s.ShiftId
                     into shiftGroup

                     from s in shiftGroup.DefaultIfEmpty()

                     join a in _context.Attendances
                         .Where(x => x.AttendanceDate == attendanceDate)
                     on e.EmployeeId equals a.EmployeeId
                     into attendance

                     from a in attendance.DefaultIfEmpty()

                     let approvedLeave =
                         _context.LeaveApplications.FirstOrDefault(l =>
                             l.EmployeeId == e.EmployeeId &&
                             l.Status == "Approved" &&
                             attendanceDate >= l.FromDate.Date &&
                             attendanceDate <= l.ToDate.Date)

                     where e.IsActive
                           && e.Role != "SuperAdmin"
                           && s != null
                           && s.ShiftName == shift

                     orderby e.EmployeeCode

                     select new TodayAttendanceViewModel
                     {
                         EmployeeCode = e.EmployeeCode,

                         EmployeeName = e.EmployeeName,

                         Department = e.Department,

                         Shift = s != null
                             ? s.ShiftName
                             : "--",

                         PunchIn =
                             a != null && a.PunchIn != null
                                 ? a.PunchIn.Value.ToString("hh:mm tt")
                                 : "--",

                         PunchOut =
                             a != null && a.PunchOut != null
                                 ? a.PunchOut.Value.ToString("hh:mm tt")
                                 : "--",

                         BreakStart =
                             a != null
                                 ? _context.AttendanceBreaks
                                     .Where(b =>
                                         b.AttendanceId == a.AttendanceId)
                                     .OrderByDescending(b => b.BreakStart)
                                     .Select(b =>
                                         b.BreakStart.ToString("hh:mm tt"))
                                     .FirstOrDefault() ?? "--"
                                 : "--",

                         BreakEnd =
                             a != null
                                 ? _context.AttendanceBreaks
                                     .Where(b =>
                                         b.AttendanceId == a.AttendanceId)
                                     .OrderByDescending(b => b.BreakStart)
                                     .Select(b =>
                                         b.BreakEnd != null
                                             ? b.BreakEnd.Value.ToString("hh:mm tt")
                                             : "--")
                                     .FirstOrDefault() ?? "--"
                                 : "--",

                         TotalBreak =
                             a != null
                                 ? TimeSpan.FromMinutes(
                                     a.TotalBreakMinutes)
                                     .ToString(@"hh\:mm")
                                 : "--",

                         WorkedTime =
                             a != null
                                 ? TimeSpan.FromMinutes(
                                     a.WorkedMinutes)
                                     .ToString(@"hh\:mm")
                                 : "--",

                         Status =
        approvedLeave != null &&
        !approvedLeave.IsMorningHalf &&
        !approvedLeave.IsAfternoonHalf
            ? "On Leave"
            : (approvedLeave != null &&
               approvedLeave.IsMorningHalf &&
(a == null || a.PunchIn == null)
                ? "First Half Leave"
                : (approvedLeave != null &&
                   approvedLeave.IsAfternoonHalf &&
(a == null || a.PunchIn == null)
                    ? "Second Half Leave"
                    : (a != null
                        ? a.Status
                        : "Absent"))),

                         LeaveStatus =
        approvedLeave != null
            ? (approvedLeave.IsMorningHalf
                ? "Approved Leave - First Half"
                : approvedLeave.IsAfternoonHalf
                    ? "Approved Leave - Second Half"
                    : "Approved Leave")
            : "",

                         IsOnApprovedLeave = approvedLeave != null,

                         ApprovedLeave = approvedLeave,

                         ShiftModel = s

                     }).ToList();

                ViewBag.SelectedShift = shift;

                return View(attendanceList);
            }

            //====================================================
            // ATTENDANCE REGISTER
            //====================================================

            [HttpGet]
            public IActionResult Register(
                DateTime? fromDate,
                DateTime? toDate,
                string shift = "All",
                string employee = "",
                string status = "All")
            {
                var today = DateTimeHelper.GetIST().Date;

                fromDate ??= today;
                toDate ??= today;

                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.Shift = shift;
                ViewBag.Employee = employee;
                ViewBag.Status = status;

                //==============================================
                // 90 DAY VALIDATION
                //==============================================

                if (fromDate.HasValue && toDate.HasValue)
                {
                    if (fromDate > toDate)
                    {
                        TempData["Error"] = "From Date cannot be greater than To Date.";

                        return View(new List<AttendanceRegisterViewModel>());
                    }

                    if ((toDate.Value - fromDate.Value).TotalDays > 90)
                    {
                        TempData["Error"] = "Attendance Register can be generated only for a maximum period of 90 days.";

                        return View(new List<AttendanceRegisterViewModel>());
                    }

                    if (fromDate.Value < DateTime.Today.AddDays(-90))
                    {
                        TempData["Error"] = "Attendance records older than 90 days cannot be viewed.";

                        return View(new List<AttendanceRegisterViewModel>());
                    }
                }

                var request = new AttendanceRegisterRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Shift = shift,
                    Employee = employee,
                    Status = status
                };

                var attendanceList =
                    _attendanceRegisterService.GetAttendanceRegister(request);

                return View(attendanceList);
            }

            //====================================================
            // EXPORT ATTENDANCE REGISTER - EXCEL
            //====================================================

            [HttpGet]
            //====================================================
            // EXPORT ATTENDANCE REGISTER - EXCEL
            //====================================================

            [HttpGet]
            public IActionResult ExportAttendanceRegisterExcel(
        DateTime? fromDate,
        DateTime? toDate,
        string shift = "All",
        string employee = "",
        string status = "All")
            {
                //==========================================
                // Build Request
                //==========================================

                var request = new AttendanceRegisterRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Shift = shift,
                    Employee = employee,
                    Status = status
                };

                //==========================================
                // Get Attendance Data
                //==========================================

                var attendanceList =
                    _attendanceRegisterService.GetAttendanceRegister(request);

                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Attendance Register");

                    //==========================================
                    // Report Heading
                    //==========================================

                    ws.Range("A1:L1").Merge();
                    ws.Cell("A1").Value = "FIDELITAS HUB";
                    ws.Cell("A1").Style.Font.Bold = true;
                    ws.Cell("A1").Style.Font.FontSize = 20;
                    ws.Cell("A1").Style.Font.FontColor = XLColor.White;
                    ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.RoyalBlue;
                    ws.Cell("A1").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    ws.Range("A2:L2").Merge();
                    ws.Cell("A2").Value = "ATTENDANCE REGISTER";
                    ws.Cell("A2").Style.Font.Bold = true;
                    ws.Cell("A2").Style.Font.FontSize = 16;
                    ws.Cell("A2").Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                    //==========================================
                    // Report Information
                    //==========================================

                    ws.Cell("A4").Value = "From Date";
                    ws.Cell("B4").Value =
                        fromDate?.ToString("dd-MMM-yyyy") ?? "-";

                    ws.Cell("D4").Value = "To Date";
                    ws.Cell("E4").Value =
                        toDate?.ToString("dd-MMM-yyyy") ?? "-";

                    ws.Cell("G4").Value = "Shift";
                    ws.Cell("H4").Value = shift;

                    ws.Cell("J4").Value = "Status";
                    ws.Cell("K4").Value = status;

                    ws.Cell("A5").Value = "Generated On";
                    ws.Cell("B5").Value =
                        DateTimeHelper.GetIST().ToString("dd-MMM-yyyy hh:mm tt");

                    ws.Cell("J5").Value = "Records";
                    ws.Cell("K5").Value = attendanceList.Count;

                    //==========================================
                    // Column Headers
                    //==========================================

                    int row = 7;

                    ws.Cell(row, 1).Value = "Employee Code";
                    ws.Cell(row, 2).Value = "Employee Name";
                    ws.Cell(row, 3).Value = "Department";
                    ws.Cell(row, 4).Value = "Date";
                    ws.Cell(row, 5).Value = "Shift";
                    ws.Cell(row, 6).Value = "Punch In";
                    ws.Cell(row, 7).Value = "Punch Out";
                    ws.Cell(row, 8).Value = "Worked";
                    ws.Cell(row, 9).Value = "Break";
                    ws.Cell(row, 11).Value = "Status";
                    ws.Cell(row, 12).Value = "Remarks";

                    ws.Range(row, 1, row, 12).Style.Font.Bold = true;
                    ws.Range(row, 1, row, 12).Style.Fill.BackgroundColor =
                        XLColor.LightBlue;

                    //==========================================
                    // Data
                    //==========================================

                    row++;

                    foreach (var item in attendanceList)
                    {
                        ws.Cell(row, 1).Value = item.EmployeeCode;
                        ws.Cell(row, 2).Value = item.EmployeeName;
                        ws.Cell(row, 3).Value = item.Department;
                        ws.Cell(row, 4).Value = item.AttendanceDate;
                        ws.Cell(row, 4).Style.DateFormat.Format = "dd-MMM-yyyy";
                        ws.Cell(row, 5).Value = item.Shift;
                        ws.Cell(row, 6).Value = item.PunchIn;
                        ws.Cell(row, 7).Value = item.PunchOut;
                        ws.Cell(row, 8).Value = item.WorkedTime;
                        ws.Cell(row, 9).Value = item.BreakTime;
                        ws.Cell(row, 11).Value = item.AttendanceStatus;
                        ws.Cell(row, 12).Value = item.Remarks;

                        row++;
                    }

                    //==========================================
                    // Formatting
                    //==========================================

                    ws.Columns().AdjustToContents();

                    ws.RangeUsed().Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;

                    ws.RangeUsed().Style.Border.InsideBorder =
                        XLBorderStyleValues.Thin;

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"Attendance Register {DateTimeHelper.GetIST():yyyyMMddHHmmss}.xlsx");
                    }
                }
            }

            //====================================================
            // EXPORT TODAY ATTENDANCE - EXCEL
            //====================================================

            public IActionResult ExportTodayAttendanceExcel(string shift = "General Shift")
            {
                using (var workbook = new XLWorkbook())
                {
                    var ws = workbook.Worksheets.Add("Today's Attendance");

                    //==========================================
                    // Report Title
                    //==========================================

                    ws.Range("A1:L1").Merge();
                    ws.Cell("A1").Value = "FIDELITAS HUB";
                    ws.Cell("A1").Style.Font.Bold = true;
                    ws.Cell("A1").Style.Font.FontSize = 20;
                    ws.Cell("A1").Style.Font.FontColor = XLColor.White;
                    ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.RoyalBlue;
                    ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Range("A2:L2").Merge();
                    ws.Cell("A2").Value = "TODAY'S ATTENDANCE REPORT";
                    ws.Cell("A2").Style.Font.Bold = true;
                    ws.Cell("A2").Style.Font.FontSize = 16;
                    ws.Cell("A2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell("A3").Value = "Shift";
                    ws.Cell("B3").Value = shift;

                    ws.Cell("A4").Value = "Generated On";
                    ws.Cell("B4").Value =
        DateTimeHelper.GetIST().ToString("dd-MMM-yyyy hh:mm tt");
                    //==========================================
                    // Column Headers
                    //==========================================

                    ws.Cell("A6").Value = "Employee Code";
                    ws.Cell("B6").Value = "Employee Name";
                    ws.Cell("C6").Value = "Department";
                    ws.Cell("D6").Value = "Shift";
                    ws.Cell("E6").Value = "Punch In";
                    ws.Cell("F6").Value = "Punch Out";
                    ws.Cell("G6").Value = "Break In";
                    ws.Cell("H6").Value = "Break Out";
                    ws.Cell("I6").Value = "Total Break";
                    ws.Cell("J6").Value = "Worked";
                    ws.Cell("K6").Value = "Status";

                    //==========================================
                    // Get Today's Attendance
                    //==========================================

                    var today = DateTimeHelper.GetIST().Date;

                    var attendanceList = (from e in _reportingService.GetVisibleEmployees()

                                          join s in _context.Shifts
                                          on e.ShiftId equals s.ShiftId

                                          join a in _context.Attendances
                                          .Where(x => x.AttendanceDate == today)
                                          on e.EmployeeId equals a.EmployeeId
                                          into attendance

                                          from a in attendance.DefaultIfEmpty()

                                          where e.IsActive
                                                && e.Role != "SuperAdmin"
                                                && s.ShiftName == shift

                                          orderby e.EmployeeCode

                                          select new
                                          {
                                              e.EmployeeCode,

                                              e.EmployeeName,

                                              e.Department,

                                              Shift = s.ShiftName,

                                              PunchIn = a != null && a.PunchIn != null
                ? a.PunchIn.Value.ToString("hh:mm tt")
                : "--",

                                              PunchOut = a != null && a.PunchOut != null
                ? a.PunchOut.Value.ToString("hh:mm tt")
                : "--",

                                              BreakStart =
        a != null
            ? _context.AttendanceBreaks
                .Where(b => b.AttendanceId == a.AttendanceId)
                .OrderByDescending(b => b.BreakStart)
                .Select(b => b.BreakStart.ToString("hh:mm tt"))
                .FirstOrDefault() ?? "--"
            : "--",

                                              BreakEnd =
        a != null
            ? _context.AttendanceBreaks
                .Where(b => b.AttendanceId == a.AttendanceId)
                .OrderByDescending(b => b.BreakStart)
                .Select(b => b.BreakEnd != null
                    ? b.BreakEnd.Value.ToString("hh:mm tt")
                    : "--")
                .FirstOrDefault() ?? "--"
            : "--",

                                              TotalBreak =
        a != null
            ? TimeSpan.FromMinutes(a.TotalBreakMinutes)
                .ToString(@"hh\:mm")
            : "--",

                                              Worked =
        a != null
            ? TimeSpan.FromMinutes(a.WorkedMinutes)
                .ToString(@"hh\:mm")
            : "--",

                                              Status = a != null
                ? a.Status
                : "Absent"

                                          }).ToList();

                    //==========================================
                    // Write Data to Excel
                    //==========================================

                    int row = 7;

                    foreach (var item in attendanceList)
                    {
                        ws.Cell(row, 1).Value = item.EmployeeCode;
                        ws.Cell(row, 2).Value = item.EmployeeName;
                        ws.Cell(row, 3).Value = item.Department;
                        ws.Cell(row, 4).Value = item.Shift;
                        ws.Cell(row, 5).Value = item.PunchIn;
                        ws.Cell(row, 6).Value = item.PunchOut;
                        ws.Cell(row, 7).Value = item.BreakStart;
                        ws.Cell(row, 8).Value = item.BreakEnd;
                        ws.Cell(row, 9).Value = item.TotalBreak;
                        ws.Cell(row, 10).Value = item.Worked;
                        ws.Cell(row, 11).Value = item.Status;

                        row++;
                    }

                    ws.Cell("A1").Style.Font.Bold = true;
                    ws.Cell("A1").Style.Font.FontSize = 18;

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);

                        return File(
                            stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            $"Today's Attendance - {shift}.xlsx");
                    }
                }
            }

            //====================================================
            // GET ACTIVE ATTENDANCE FOR CURRENT SHIFT
            //====================================================

            //====================================================
            // GET ATTENDANCE DATE FOR CURRENT SHIFT
            //
            // Determines which attendance date the current
            // activity belongs to.
            //
            // Normal shift:
            //     AttendanceDate = today's IST date
            //
            // Overnight US shift:
            //     Before today's shift start and within the
            //     previous shift's punch-out window:
            //     AttendanceDate = yesterday
            //
            // Example:
            //     US Shift: 06:30 PM -> 03:30 AM
            //
            //     13-Aug 01:30 AM
            //     belongs to the 12-Aug attendance.
            //====================================================
            private DateTime GetAttendanceDateForCurrentShift(
                Shift shift,
                DateTime istNow)
            {
                var today = istNow.Date;

                var todayStartTime =
                    ShiftTimeHelper.GetStartTime(
                        shift,
                        today);

                var todayEndTime =
                    ShiftTimeHelper.GetEndTime(
                        shift,
                        today);

                bool isOvernight =
                    todayEndTime <= todayStartTime;

                //================================================
                // NORMAL / DAY SHIFT
                //================================================
                if (!isOvernight)
                {
                    return today;
                }

                //================================================
                // OVERNIGHT SHIFT
                //
                // We are before today's shift start.
                // Check whether we are still inside yesterday's
                // overnight attendance window.
                //================================================
                if (istNow.TimeOfDay < todayStartTime)
                {
                    var yesterday = today.AddDays(-1);

                    var yesterdayEndTime =
                        ShiftTimeHelper.GetEndTime(
                            shift,
                            yesterday);

                    var punchOutCutoff =
                        yesterdayEndTime +
                        TimeSpan.FromMinutes(
                            Math.Max(
                                0,
                                shift.MaximumPunchOutMinutes));

                    if (istNow.TimeOfDay <= punchOutCutoff)
                    {
                        return yesterday;
                    }
                }

                return today;
            }

            //====================================================
            // GET ACTIVE ATTENDANCE FOR CURRENT SHIFT
            //====================================================
            private Attendance? GetActiveAttendance(
                Employee employee,
                Shift shift,
                DateTime istNow)
            {
                var attendanceDate =
                    GetAttendanceDateForCurrentShift(
                        shift,
                        istNow);

                return _context.Attendances
                    .Where(a =>
                        a.EmployeeId == employee.EmployeeId &&
                        a.AttendanceDate.Date == attendanceDate)
                    .OrderByDescending(a => a.AttendanceId)
                    .FirstOrDefault();
            }

            //====================================================
            // ATTENDANCE CORRECTION
            //====================================================

            //====================================================
            // LOAD EMPLOYEE DROPDOWN
            //====================================================

            private void LoadAttendanceCorrectionDropdown(
                AttendanceCorrectionViewModel model)
            {
                model.Employees = _context.Employees
                    .Where(e => e.IsActive && e.Role != "SuperAdmin")
                    .OrderBy(e => e.EmployeeCode)
                    .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = e.EmployeeId.ToString(),
                        Text = e.EmployeeCode + " - " + e.EmployeeName
                    })
                    .ToList();
            }

            //====================================================
            // LOAD ATTENDANCE DETAILS
            //====================================================

            private void LoadAttendanceDetails(
                AttendanceCorrectionViewModel model)
            {
                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeId == model.EmployeeId);

                if (employee == null ||
                    string.Equals(employee.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                    return;

                model.EmployeeCode = employee.EmployeeCode;
                model.EmployeeName = employee.EmployeeName;
                model.Department = employee.Department;
                var shift = _context.Shifts
        .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

                model.Shift = shift != null
                    ? shift.ShiftName
                    : "";

                var attendance = _context.Attendances
                    .FirstOrDefault(a =>
                        a.EmployeeId == model.EmployeeId &&
                        a.AttendanceDate.Date == model.AttendanceDate.Date);

                if (attendance == null)
                    return;

                model.PunchIn = attendance.PunchIn;
                model.PunchOut = attendance.PunchOut;
                model.BreakMinutes = attendance.TotalBreakMinutes;
                model.Status = attendance.Status;

                if (attendance.PunchIn.HasValue)
                    model.NewPunchInTime = attendance.PunchIn.Value.TimeOfDay;

                if (attendance.PunchOut.HasValue)
                    model.NewPunchOutTime = attendance.PunchOut.Value.TimeOfDay;

                model.NewBreakMinutes = attendance.TotalBreakMinutes;
            }

            [HttpGet]
            public IActionResult AttendanceCorrection()
            {
                var model = new AttendanceCorrectionViewModel
                {
                    AttendanceDate = DateTimeHelper.GetIST().Date
                };

                LoadAttendanceCorrectionDropdown(model);

                return View(model);
            }

            //====================================================
            // LOAD ATTENDANCE FOR CORRECTION
            //====================================================

            [HttpPost]
            public IActionResult AttendanceCorrection(
                AttendanceCorrectionViewModel model)
            {
                LoadAttendanceCorrectionDropdown(model);

                LoadAttendanceDetails(model);

                //==========================================
                // Decide which correction fields to show
                //==========================================

                switch (model.CorrectionType)
                {
                    case "Forgot Punch In":
                        model.ShowPunchIn = true;
                        break;

                    case "Forgot Punch Out":
                        model.ShowPunchOut = true;
                        break;

                    case "Wrong Punch Time":
                        model.ShowPunchIn = true;
                        model.ShowPunchOut = true;
                        break;

                    case "Wrong Break Time":
                        model.ShowBreakMinutes = true;
                        break;

                    case "Remove Punch Out":
                        break;

                    case "Remove Break":
                        break;

                    case "Half Day Leave Correction":
                        model.ShowPunchIn = true;
                        model.ShowPunchOut = true;
                        model.ShowBreakMinutes = true;
                        break;

                    case "Manual Attendance":
                        model.ShowPunchIn = true;
                        model.ShowPunchOut = true;
                        model.ShowBreakMinutes = true;
                        break;
                }

                return View(model);
            }

            //====================================================
            // SAVE ATTENDANCE CORRECTION
            //====================================================

            [HttpPost]
            public IActionResult SaveAttendanceCorrection(
        AttendanceCorrectionViewModel model)
            {

                Console.WriteLine("=================================");
                Console.WriteLine("SAVE ATTENDANCE CORRECTION");
                Console.WriteLine($"Employee ID : {model.EmployeeId}");
                Console.WriteLine($"Attendance Date : {model.AttendanceDate}");
                Console.WriteLine($"New Punch In : {model.NewPunchInTime}");
                Console.WriteLine($"New Punch Out : {model.NewPunchOutTime}");
                Console.WriteLine("=================================");

                //==========================================
                // Validation
                //==========================================

                if (model.EmployeeId == 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Please select an employee.");

                    LoadAttendanceCorrectionDropdown(model);
                    LoadAttendanceDetails(model);

                    return View("AttendanceCorrection", model);
                }

                if (string.IsNullOrWhiteSpace(model.CorrectionType))
                {
                    ModelState.AddModelError("", "Please select a correction type.");

                    LoadAttendanceCorrectionDropdown(model);
                    LoadAttendanceDetails(model);

                    return View("AttendanceCorrection", model);
                }

                if (string.IsNullOrWhiteSpace(model.Reason))
                {
                    ModelState.AddModelError("", "Please enter the reason for correction.");

                    LoadAttendanceCorrectionDropdown(model);
                    LoadAttendanceDetails(model);

                    return View("AttendanceCorrection", model);
                }

            


            //==========================================
            // SuperAdmin is not attendance-tracked
            //==========================================

            var selectedEmployee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == model.EmployeeId);

            if (selectedEmployee == null || IsAttendanceExempt(selectedEmployee))
            {
                ModelState.AddModelError(
                    "",
                    "Attendance is not applicable for SuperAdmin.");

                LoadAttendanceCorrectionDropdown(model);
                return View("AttendanceCorrection", model);
            }

            //==========================================
            // Find Attendance Record
            //==========================================

            var attendance = _context.Attendances
                    .FirstOrDefault(a =>
                        a.EmployeeId == model.EmployeeId &&
                        a.AttendanceDate.Date == model.AttendanceDate.Date);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        EmployeeId = model.EmployeeId,
                        AttendanceDate = model.AttendanceDate.Date,
                        TotalBreakMinutes = 0,
                        WorkedMinutes = 0,
                        Status = "Working",
                        PunchOutMode = "Manual Correction"
                    };

                    _context.Attendances.Add(attendance);
                }

                //==========================================
                // UPDATE PUNCH IN / PUNCH OUT
                //==========================================

                if (model.CorrectionType == "Remove Punch Out")
                {
                    // Remove the existing Punch Out
                    attendance.PunchOut = null;
                    attendance.PunchOutMode = "Manual Correction";
                }
                else if (model.CorrectionType == "Forgot Punch In")
                {
                    // Add the missing Punch In
                    if (model.NewPunchInTime.HasValue)
                    {
                        attendance.PunchIn = model.AttendanceDate.Date
                            + model.NewPunchInTime.Value;

                        attendance.PunchOutMode = "Manual Correction";
                    }
                }
                else if (model.CorrectionType == "Forgot Punch Out")
                {
                    // Add the missing Punch Out

                    if (model.NewPunchOutTime.HasValue)
                    {
                        var punchOutDateTime =
                            model.AttendanceDate.Date
                            + model.NewPunchOutTime.Value;

                        var employee = _context.Employees
                            .FirstOrDefault(e =>
                                e.EmployeeId == model.EmployeeId);

                        var shift = employee != null
                            ? _context.Shifts
                                .FirstOrDefault(s =>
                                    s.ShiftId == employee.ShiftId)
                            : null;

                        // Overnight shift:
                        // Punch Out belongs to the following calendar day
                        if (shift != null)
                        {
                            var applicableStartTime =
                                ShiftTimeHelper.GetStartTime(
                                    shift,
                                    model.AttendanceDate);

                            var applicableEndTime =
                                ShiftTimeHelper.GetEndTime(
                                    shift,
                                    model.AttendanceDate);

                            bool isOvernight =
                                applicableEndTime <= applicableStartTime;

                            if (isOvernight &&
                                model.NewPunchOutTime.Value <= applicableEndTime)
                            {
                                punchOutDateTime =
                                    punchOutDateTime.AddDays(1);
                            }
                        }

                        attendance.PunchOut = punchOutDateTime;

                        attendance.PunchOutMode = "Manual Correction";
                    }
                }
                else
                {
                    // Update Punch In if supplied
                    if (model.NewPunchInTime.HasValue)
                    {
                        attendance.PunchIn = model.AttendanceDate.Date
                            + model.NewPunchInTime.Value;
                    }

                    // Update Punch Out if supplied
                    if (model.NewPunchOutTime.HasValue)
                    {
                        var punchOutDateTime =
                            model.AttendanceDate.Date
                            + model.NewPunchOutTime.Value;

                        var employee = _context.Employees
                            .FirstOrDefault(e =>
                                e.EmployeeId == model.EmployeeId);

                        var shift = employee != null
                            ? _context.Shifts
                                .FirstOrDefault(s =>
                                    s.ShiftId == employee.ShiftId)
                            : null;

                        // Overnight shift:
                        // Punch Out belongs to the following calendar day
                        if (shift != null)
                        {
                            var applicableStartTime =
                                ShiftTimeHelper.GetStartTime(
                                    shift,
                                    model.AttendanceDate);

                            var applicableEndTime =
                                ShiftTimeHelper.GetEndTime(
                                    shift,
                                    model.AttendanceDate);

                            bool isOvernight =
                                applicableEndTime <= applicableStartTime;

                            if (isOvernight &&
                                model.NewPunchOutTime.Value <= applicableEndTime)
                            {
                                punchOutDateTime =
                                    punchOutDateTime.AddDays(1);
                            }
                        }

                        attendance.PunchOut = punchOutDateTime;
                    }

                    // Any normal time correction is a manual correction
                    attendance.PunchOutMode = "Manual Correction";
                }

                //==========================================
                // UPDATE BREAK MINUTES
                //==========================================

                if (model.CorrectionType == "Remove Break")
                {
                    attendance.TotalBreakMinutes = 0;
                }
                else
                {
                    attendance.TotalBreakMinutes = model.NewBreakMinutes;
                }

                //==========================================
                // RECALCULATE WORKED MINUTES
                //==========================================

                if (attendance.PunchIn.HasValue && attendance.PunchOut.HasValue)
                {
                    DateTime punchIn = attendance.PunchIn.Value;
                    DateTime punchOut = attendance.PunchOut.Value;

                    //======================================
                    // Overnight Shift
                    //======================================

                    if (punchOut < punchIn)
                    {
                        punchOut = punchOut.AddDays(1);
                    }

                    attendance.WorkedMinutes =
                        (int)Math.Max(
                            0,
                            (punchOut - punchIn).TotalMinutes
                            - attendance.TotalBreakMinutes);
                }
                else
                {
                    attendance.WorkedMinutes = 0;
                }

                //==========================================
                // ATTENDANCE STATUS
                //==========================================

                if (model.CorrectionType == "Remove Punch Out")
                {
                    attendance.Status = "Working";
                }
                else if (attendance.PunchIn.HasValue && attendance.PunchOut.HasValue)
                {
                    attendance.Status = "Present";
                }
                else if (attendance.PunchIn.HasValue)
                {
                    attendance.Status = "Working";
                }
                else
                {
                    attendance.Status = "Absent";
                }

                //==========================================
                // SAVE CHANGES
                //==========================================

                //==========================================
                // AUDIT LOG
                //==========================================

                _context.AuditLogs.Add(new AuditLog
                {
                    EmployeeId = model.EmployeeId,
                    Action = "Attendance Correction",
                    ActionTime = DateTimeHelper.GetIST(),
                    Remarks =
                        $"Type : {model.CorrectionType} | Reason : {model.Reason}",
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ComputerName = Environment.MachineName
                });

                _context.SaveChanges();

                //==========================================
                // SUCCESS
                //==========================================

                LoadAttendanceCorrectionDropdown(model);
                LoadAttendanceDetails(model);

                TempData["Success"] = "Attendance corrected successfully.";

                return View("AttendanceCorrection", model);
            }

            //====================================================
            // DETERMINE WHETHER APPROVED HALF-DAY LEAVE APPLIES
            // TO THE CURRENT SHIFT TIME
            //====================================================
            private bool IsHalfDayLeaveActive(
                LeaveApplication leave,
                Shift shift,
                DateTime istNow)
            {
                // Full-day leave
                if (!leave.IsMorningHalf && !leave.IsAfternoonHalf)
                    return true;

                var attendanceDate =
                    GetAttendanceDateForCurrentShift(
                        shift,
                        istNow);

                var shiftStartTime =
                    ShiftTimeHelper.GetStartTime(
                        shift,
                        attendanceDate);

                var shiftEndTime =
                    ShiftTimeHelper.GetEndTime(
                        shift,
                        attendanceDate);

                var shiftStart =
                    attendanceDate.Date + shiftStartTime;

                var shiftEnd =
                    attendanceDate.Date + shiftEndTime;

                // Overnight shift
                if (shiftEnd <= shiftStart)
                    shiftEnd = shiftEnd.AddDays(1);

                var halfPoint =
                    shiftStart.AddTicks(
                        (shiftEnd - shiftStart).Ticks / 2);

                // First Half
                if (leave.IsMorningHalf)
                {
                    return istNow >= shiftStart &&
                           istNow < halfPoint;
                }

                // Second Half
                if (leave.IsAfternoonHalf)
                {
                    return istNow >= halfPoint &&
                           istNow <= shiftEnd;
                }

                return false;
            }


            //====================================================
            // ATTENDANCE EXEMPTION
            //====================================================

            private static bool IsAttendanceExempt(Employee employee)
            {
                return string.Equals(
                    employee.Role,
                    "SuperAdmin",
                    StringComparison.OrdinalIgnoreCase);
            }

            private JsonResult AttendanceNotApplicableResponse()
            {
                return Json(new
                {
                    success = false,
                    title = "Attendance Not Applicable",
                    message = "SuperAdmin accounts are not included in attendance tracking."
                });
            }

        }

    }

