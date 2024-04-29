namespace VKEngine.Graphics.Vulkan.Native;

internal struct VkVersion
{
    private readonly uint version;

    public VkVersion(uint major, uint minor, uint patch)
    {
        version = (major << 22) | (minor << 12) | patch;
    }

    public static VkVersion Make(uint major, uint minor, uint patch) => new(major, minor, patch);

    public static implicit operator uint(VkVersion version) => version.version;
}
