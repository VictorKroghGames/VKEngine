namespace VKEngine.Graphics.Enumerations;

[Flags]
public enum BufferMemoryPropertyFlags
{
    None = 0,
    DeviceLocal = 1,
    HostVisible = 2,
    HostCoherent = 4,
    HostCached = 8,
    LazilyAllocated = 0x10
}
