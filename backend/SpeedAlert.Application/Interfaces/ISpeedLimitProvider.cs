using System.Threading.Tasks;
using SpeedAlert.Application.Models;

namespace SpeedAlert.Application.Interfaces;

public interface ISpeedLimitProvider
{
    Task<SpeedLimitResult> GetSpeedLimitAsync(double latitude, double longitude);
}
