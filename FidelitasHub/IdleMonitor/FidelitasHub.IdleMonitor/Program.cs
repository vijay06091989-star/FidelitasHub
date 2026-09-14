using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace FidelitasHub.IdleMonitor;

internal static class Program
{
    private const int DefaultIdleThresholdSeconds = 120;

    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FidelitasHubIdleMonitor");

    private static readonly string SettingsPath = Path.Combine(
        SettingsFolder,
        "settings.json");

    [STAThread]
    private static async Task Main()
    {
        Directory.CreateDirectory(SettingsFolder);

        var settings = await LoadSettingsAsync();

        if (settings == null ||
            string.IsNullOrWhiteSpace(settings.EmployeeCode) ||
            string.IsNullOrWhiteSpace(settings.HubUrl))
        {
            MessageBox.Show(
                $"Idle monitor is not configured.\n\nEdit:\n{SettingsPath}",
                "FidelitasHub Idle Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        using var http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var currentlyIdle = false;
        var pendingStatus = string.Empty;
        var computerLockedForCurrentIdle = false;
        var monitoringWasEnabled = false;

        while (true)
        {
            try
            {
                // Ask the Hub whether idle monitoring is currently
                // enabled for this employee.
                var monitoringEnabled =
                    await GetMonitoringEnabledAsync(
                        http,
                        settings);

                // If monitoring is disabled in Employee Master,
                // reset the local idle state completely.
                if (!monitoringEnabled)
                {
                    // If an idle session was open, tell the Hub
                    // the employee is now Active so the session closes.
                    if (currentlyIdle)
                    {
                        await SendStatusAsync(
                            http,
                            settings,
                            "Active");
                    }

                    currentlyIdle = false;
                    pendingStatus = string.Empty;
                    computerLockedForCurrentIdle = false;
                    monitoringWasEnabled = false;

                    await Task.Delay(
                        TimeSpan.FromSeconds(5));

                    continue;
                }

                // Monitoring has just been enabled again.
                // Reset the local state so a new idle cycle starts cleanly.
                if (!monitoringWasEnabled)
                {
                    currentlyIdle = false;
                    pendingStatus = string.Empty;
                    computerLockedForCurrentIdle = false;
                    monitoringWasEnabled = true;
                }

                var idleSeconds = GetIdleSeconds();

                var idleThreshold =
                    settings.IdleThresholdSeconds > 0
                        ? settings.IdleThresholdSeconds
                        : DefaultIdleThresholdSeconds;

                var shouldBeIdle =
                    idleSeconds >= idleThreshold;

                var desiredStatus =
                    shouldBeIdle
                        ? "Idle"
                        : "Active";

                if (shouldBeIdle != currentlyIdle ||
                    !string.IsNullOrEmpty(pendingStatus))
                {
                    var statusToSend = desiredStatus;

                    pendingStatus = statusToSend;

                    var sent = await SendStatusAsync(
                        http,
                        settings,
                        statusToSend);

                    if (sent)
                    {
                        currentlyIdle =
                            statusToSend == "Idle";

                        pendingStatus = string.Empty;
                    }
                    else
                    {
                        pendingStatus = statusToSend;
                    }
                }

                // Lock Windows only once during each idle period,
                // and only while monitoring is enabled in the Hub.
                if (shouldBeIdle &&
                    settings.LockComputerWhenIdle &&
                    !computerLockedForCurrentIdle)
                {
                    LockWorkStation();

                    computerLockedForCurrentIdle = true;
                }

                // Once the employee becomes active again,
                // allow locking during the next idle period.
                if (!shouldBeIdle)
                {
                    computerLockedForCurrentIdle = false;
                }
            }
            catch
            {
                // Keep the monitor alive.
                // The next loop will retry.
            }

            await Task.Delay(
                TimeSpan.FromSeconds(5));
        }
    }

    private static async Task<IdleMonitorSettings?> LoadSettingsAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            var sample = new IdleMonitorSettings();

            await File.WriteAllTextAsync(
                SettingsPath,
                JsonSerializer.Serialize(
                    sample,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));

            return sample;
        }

        var json =
            await File.ReadAllTextAsync(SettingsPath);

        return JsonSerializer.Deserialize<IdleMonitorSettings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
    }

    private static async Task<bool> GetMonitoringEnabledAsync(
        HttpClient http,
        IdleMonitorSettings settings)
    {
        var baseUrl =
            settings.HubUrl.TrimEnd('/');

        var employeeCode =
            Uri.EscapeDataString(
                settings.EmployeeCode);

        var response =
            await http.GetAsync(
                $"{baseUrl}/api/idle-monitor/enabled?employeeCode={employeeCode}");

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var result =
            await response.Content
                .ReadFromJsonAsync<
                    IdleMonitoringEnabledResponse>();

        return result?.Enabled == true;
    }

    private static async Task<bool> SendStatusAsync(
        HttpClient http,
        IdleMonitorSettings settings,
        string status)
    {
        var baseUrl =
            settings.HubUrl.TrimEnd('/');

        var response =
            await http.PostAsJsonAsync(
                $"{baseUrl}/api/idle-monitor/status",
                new
                {
                    EmployeeCode =
                        settings.EmployeeCode,

                    Status =
                        status,

                    ComputerName =
                        Environment.MachineName,

                    WindowsUserName =
                        Environment.UserName
                });

        return response.IsSuccessStatusCode;
    }

    private static long GetIdleSeconds()
    {
        var info =
            new LASTINPUTINFO();

        info.cbSize =
            (uint)Marshal.SizeOf(info);

        if (!GetLastInputInfo(ref info))
            return 0;

        var tickCount =
            unchecked(
                (uint)Environment.TickCount);

        var idleMilliseconds =
            unchecked(
                tickCount - info.dwTime);

        return idleMilliseconds / 1000;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(
        ref LASTINPUTINFO plii);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    private sealed class IdleMonitoringEnabledResponse
    {
        public bool Success { get; set; }

        public bool Enabled { get; set; }
    }

    private sealed class IdleMonitorSettings
    {
        public string HubUrl { get; set; } =
            "https://localhost:7273";

        public string EmployeeCode { get; set; } =
            "";

        public int IdleThresholdSeconds { get; set; } =
            120;

        public bool LockComputerWhenIdle { get; set; } =
            true;
    }
}