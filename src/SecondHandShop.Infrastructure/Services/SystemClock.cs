using SecondHandShop.Application.Abstractions.Common;

namespace SecondHandShop.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
