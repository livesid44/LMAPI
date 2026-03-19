using LMAPI.Models;

namespace LMAPI.Services;

public interface ILogicMonitorService
{
    /// <summary>Returns details for a single device by its LogicMonitor device ID.</summary>
    Task<Device?> GetDeviceAsync(int deviceId, CancellationToken cancellationToken = default);

    /// <summary>Returns a paged list of events/logs for the specified device.</summary>
    Task<LogicMonitorListData<DeviceEvent>> GetDeviceEventsAsync(
        int deviceId,
        int size = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);
}
