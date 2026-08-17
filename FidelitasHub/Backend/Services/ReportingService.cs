using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Services
{
    public class ReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportingService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        //==================================================
        // Get Currently Logged-In Employee
        //==================================================

        public Employee? GetCurrentEmployee()
        {
            var employeeCode =
                _httpContextAccessor.HttpContext?
                    .Session
                    .GetString("EmployeeCode");

            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                return null;
            }

            return _context.Employees
                .FirstOrDefault(e =>
                    e.EmployeeCode == employeeCode &&
                    e.IsActive);
        }

        //==================================================
        // Get Current Employee ID
        //==================================================

        public int? GetCurrentEmployeeId()
        {
            return GetCurrentEmployee()?.EmployeeId;
        }

        //==================================================
        // Get Current Employee Role
        //==================================================

        public string? GetCurrentRole()
        {
            return GetCurrentEmployee()?.Role;
        }

        //==================================================
        // Get Employees Visible To Current User
        //==================================================

        public IQueryable<Employee> GetVisibleEmployees()
        {
            var currentEmployee = GetCurrentEmployee();

            //--------------------------------------------------
            // Not logged in
            //--------------------------------------------------

            if (currentEmployee == null)
            {
                return _context.Employees
                    .Where(e => false);
            }

            //--------------------------------------------------
            // ADMIN
            //
            // Admin can see everyone
            //--------------------------------------------------

            if (currentEmployee.Role == "Admin")
            {
                return _context.Employees
                    .Where(e => e.IsActive);
            }

            //--------------------------------------------------
            // MANAGER
            //
            // Manager can see:
            // 1. Employees directly reporting to the Manager
            // 2. Team Leaders reporting to the Manager
            // 3. Employees reporting to those Team Leaders
            //--------------------------------------------------

            if (currentEmployee.Role == "Manager")
            {
                var teamLeaderIds = _context.Employees
                    .Where(e =>
                        e.IsActive &&
                        e.Role == "Team Leader" &&
                        e.ReportingManagerId ==
                            currentEmployee.EmployeeId)
                    .Select(e => e.EmployeeId);

                return _context.Employees
                    .Where(e =>
                        e.IsActive &&
                        (
                            e.EmployeeId ==
                                currentEmployee.EmployeeId

                            || e.ReportingManagerId ==
                                currentEmployee.EmployeeId

                            || e.ReportingTeamLeaderId
                                .HasValue
                                && teamLeaderIds.Contains(
                                    e.ReportingTeamLeaderId.Value)
                        ));
            }

            //--------------------------------------------------
            // TEAM LEADER
            //
            // Team Leader can see:
            // 1. Themselves
            // 2. Employees reporting to them
            //--------------------------------------------------

            if (currentEmployee.Role == "Team Leader")
            {
                return _context.Employees
                    .Where(e =>
                        e.IsActive &&
                        (
                            e.EmployeeId ==
                                currentEmployee.EmployeeId

                            || e.ReportingTeamLeaderId ==
                                currentEmployee.EmployeeId
                        ));
            }

            //--------------------------------------------------
            // NORMAL EMPLOYEE
            //
            // Employee can see only themselves
            //--------------------------------------------------

            return _context.Employees
                .Where(e =>
                    e.IsActive &&
                    e.EmployeeId ==
                        currentEmployee.EmployeeId);
        }

        //==================================================
        // Get Visible Employee IDs
        //==================================================

        public List<int> GetVisibleEmployeeIds()
        {
            return GetVisibleEmployees()
                .Select(e => e.EmployeeId)
                .ToList();
        }

        //==================================================
        // Can Current User View Employee?
        //==================================================

        public bool CanViewEmployee(int employeeId)
        {
            return GetVisibleEmployees()
                .Any(e => e.EmployeeId == employeeId);
        }
    }
}