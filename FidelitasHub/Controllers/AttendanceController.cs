using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using System.IO;
using FidelitasHub.Services.Attendance;

namespace FidelitasHub.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAttendanceRegisterService _attendanceRegisterService;
        private readonly IAttendanceProcessingService _attendanceProcessingService;

        public AttendanceController(
    ApplicationDbContext context,
    IAttendanceRegisterService attendanceRegisterService,
    IAttendanceProcessingService attendanceProcessingService)
        {
            _context = context;
            _attendanceRegisterService = attendanceRegisterService;
            _attendanceProcessingService = attendanceProcessingService;
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
            // Get Shift
            //--------------------------------------------------

            var shift = _context.Shifts
                .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

            ViewBag.EmployeeName = employee.EmployeeName;

            if (shift != null)
            {
                ViewBag.ShiftName = shift.ShiftName;

                ViewBag.ShiftTiming =
                    $"{DateTime.Today.Add(shift.StandardStartTime):hh:mm tt} - " +
                    $"{DateTime.Today.Add(shift.StandardEndTime):hh:mm tt}";
            }
            else
            {
                ViewBag.ShiftName = "Not Assigned";
                ViewBag.ShiftTiming = "-";
            }

            //--------------------------------------------------
            // Today's Attendance Status
            //--------------------------------------------------

            var todayAttendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate.Date == DateTimeHelper.GetIST().Date);

            if (todayAttendance == null)
            {
                ViewBag.AttendanceStatus = "Not Punched In";

                ViewBag.PunchInTime = "--";

                ViewBag.PunchOutTime = "--";

                ViewBag.TotalBreakTime = "00:00";

                ViewBag.WorkedTime = "00:00";

                ViewBag.BreakStartTime = "--";

                ViewBag.BreakEndTime = "--";
            }
            else
            {
                ViewBag.AttendanceStatus = todayAttendance.Status;

                ViewBag.PunchInTime =
                    todayAttendance.PunchIn?.ToString("hh:mm:ss tt");

                ViewBag.PunchOutTime =
                    todayAttendance.PunchOut?.ToString("hh:mm:ss tt") ?? "--";

                ViewBag.TotalBreakTime =
                    TimeSpan.FromMinutes(todayAttendance.TotalBreakMinutes)
                            .ToString(@"hh\:mm");

                ViewBag.WorkedTime =
                    TimeSpan.FromMinutes(todayAttendance.WorkedMinutes)
                            .ToString(@"hh\:mm");

                var lastBreak = _context.AttendanceBreaks
                    .Where(b => b.AttendanceId == todayAttendance.AttendanceId)
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

            var attendance = _context.Attendances.FirstOrDefault(a =>
                a.EmployeeId == employee.EmployeeId &&
                a.AttendanceDate.Date == istNow.Date);

            if (attendance != null)
            {
                return Json(new
                {
                    success = false,
                    title = "Already Punched In",
                    message = $"You already punched in today at {attendance.PunchIn:hh:mm tt}."
                });
            }

            attendance = new Attendance
            {
                EmployeeId = employee.EmployeeId,
                AttendanceDate = istNow.Date,
                PunchIn = istNow,
                Status = "Working",
                TotalBreakMinutes = 0,
                PunchOutMode = "Manual"
            };

            _context.Attendances.Add(attendance);

            _context.AuditLogs.Add(new AuditLog
            {
                EmployeeId = employee.EmployeeId,
                Action = "Punch In",
                ActionTime = istNow,
                Remarks = "Manual Punch In",
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                ComputerName = Environment.MachineName
            });

            _context.SaveChanges();

            string greeting;

            if (istNow.Hour >= 5 && istNow.Hour < 12)
                greeting = "🌞 Good Morning";
            else if (istNow.Hour >= 12 && istNow.Hour < 17)
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

            var istNow = DateTimeHelper.GetIST();

            var attendance = _context.Attendances.FirstOrDefault(a =>
                a.EmployeeId == employee.EmployeeId &&
                a.AttendanceDate.Date == istNow.Date);

            if (attendance == null)
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

            var istNow = DateTimeHelper.GetIST();

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate.Date == istNow.Date);

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

            var istNow = DateTimeHelper.GetIST();

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate.Date == istNow.Date);

            if (attendance == null)
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

            attendance.PunchOut = istNow;

            attendance.Status = "Punched Out";

            //--------------------------------------------------
            // Worked Minutes
            //--------------------------------------------------

            int workedMinutes =
                (int)Math.Max(0,
                (attendance.PunchOut.Value - attendance.PunchIn.Value)
                .TotalMinutes
                - attendance.TotalBreakMinutes);

            attendance.WorkedMinutes = workedMinutes;

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
            var today = DateTimeHelper.GetIST().Date;

            var attendanceList = (from e in _context.Employees

                                  join d in _context.Departments
                                  on e.Department equals d.DepartmentName
                                  into dept

                                  from d in dept.DefaultIfEmpty()

                                  join s in _context.Shifts
on e.ShiftId equals s.ShiftId
into shiftGroup

                                  from s in shiftGroup.DefaultIfEmpty()

                                  join a in _context.Attendances
                                  .Where(x => x.AttendanceDate == today)
                                  on e.EmployeeId equals a.EmployeeId
                                  into attendance

                                  from a in attendance.DefaultIfEmpty()

                                  where e.IsActive
      && s != null
      && s.ShiftName == shift

                                  orderby e.EmployeeCode

                                  select new TodayAttendanceViewModel
                                  {
                                      EmployeeCode = e.EmployeeCode,

                                      EmployeeName = e.EmployeeName,

                                      Department = e.Department,

                                      Shift = s != null ? s.ShiftName : "--",

                                      PunchIn = a != null && a.PunchIn != null
                                                ? a.PunchIn.Value.ToString("hh:mm tt")
                                                : "--",

                                      PunchOut = a != null && a.PunchOut != null
                                                ? a.PunchOut.Value.ToString("hh:mm tt")
                                                : "--",

                                      WorkedTime = a != null
                                                ? TimeSpan.FromMinutes(a.WorkedMinutes)
                                                    .ToString(@"hh\:mm")
                                                : "--",

                                      Status = a != null
                                                ? a.Status
                                                : "Absent"
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

                ws.Range("A1:I1").Merge();
                ws.Cell("A1").Value = "FIDELITAS HUB";
                ws.Cell("A1").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 20;
                ws.Cell("A1").Style.Font.FontColor = XLColor.White;
                ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.RoyalBlue;
                ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Range("A2:I2").Merge();
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
                ws.Cell("G6").Value = "Worked";
                ws.Cell("I6").Value = "Status";

                //==========================================
                // Get Today's Attendance
                //==========================================

                var today = DateTimeHelper.GetIST().Date;

                var attendanceList = (from e in _context.Employees

                                      join s in _context.Shifts
                                      on e.ShiftId equals s.ShiftId

                                      join a in _context.Attendances
                                      .Where(x => x.AttendanceDate == today)
                                      on e.EmployeeId equals a.EmployeeId
                                      into attendance

                                      from a in attendance.DefaultIfEmpty()

                                      where e.IsActive
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
                    ws.Cell(row, 7).Value = item.Worked;
                    ws.Cell(row, 8).Value = item.Status;

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
        // ATTENDANCE CORRECTION
        //====================================================

        //====================================================
        // LOAD EMPLOYEE DROPDOWN
        //====================================================

        private void LoadAttendanceCorrectionDropdown(
            AttendanceCorrectionViewModel model)
        {
            model.Employees = _context.Employees
                .Where(e => e.IsActive)
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

            if (employee == null)
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
            var model = new AttendanceCorrectionViewModel();

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
            // Find Attendance Record
            //==========================================

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == model.EmployeeId &&
                    a.AttendanceDate.Date == model.AttendanceDate.Date);

            if (attendance == null)
            {
                ModelState.AddModelError("", "Attendance record not found.");

                LoadAttendanceCorrectionDropdown(model);
                LoadAttendanceDetails(model);

                return View("AttendanceCorrection", model);
            }

            //==========================================
            // UPDATE PUNCH IN
            //==========================================

            if (model.NewPunchInTime.HasValue)
            {
                attendance.PunchIn = model.AttendanceDate.Date
                    + model.NewPunchInTime.Value;
            }

            //==========================================
            // UPDATE PUNCH OUT
            //==========================================

            if (model.NewPunchOutTime.HasValue)
            {
                attendance.PunchOut = model.AttendanceDate.Date
                    + model.NewPunchOutTime.Value;
            }

            //==========================================
            // UPDATE BREAK MINUTES
            //==========================================

            attendance.TotalBreakMinutes = model.NewBreakMinutes;

            //==========================================
            // RECALCULATE WORKED MINUTES
            //==========================================

            if (attendance.PunchIn.HasValue && attendance.PunchOut.HasValue)
            {
                attendance.WorkedMinutes =
                    (int)(attendance.PunchOut.Value - attendance.PunchIn.Value)
                    .TotalMinutes
                    - attendance.TotalBreakMinutes;
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

            TempData["Success"] = "Attendance corrected successfully.";

            return RedirectToAction(nameof(AttendanceCorrection));
        }

    }

}

