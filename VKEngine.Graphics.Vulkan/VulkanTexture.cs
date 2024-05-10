using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanTextureFactory(IVulkanLogicalDevice logicalDevice) : ITextureFactory
{
    public ITexture CreateFromImage(IImage image)
    {
        if (image is not VulkanImage vulkanImage)
        {
            throw new InvalidOperationException("Invalid image type!");
        }

        var texture = new VulkanTexture(logicalDevice, vulkanImage);
        texture.Initialize();
        return texture;
    }
}

internal sealed class VulkanTexture(IVulkanLogicalDevice logicalDevice, IImage image) : ITexture
{
    internal VkSampler sampler;

    internal unsafe void Initialize()
    {
        // create sampler
        var samplerCreateInfo = VkSamplerCreateInfo.New();
        samplerCreateInfo.magFilter = VkFilter.Linear;
        samplerCreateInfo.minFilter = VkFilter.Linear;
        samplerCreateInfo.addressModeU = VkSamplerAddressMode.Repeat;
        samplerCreateInfo.addressModeV = VkSamplerAddressMode.Repeat;
        samplerCreateInfo.addressModeW = VkSamplerAddressMode.Repeat;
        //vkGetPhysicalDeviceProperties(physicalDevice.PhysicalDevice, out var deviceProperties);
        //samplerCreateInfo.anisotropyEnable = true;
        //samplerCreateInfo.maxAnisotropy = deviceProperties.limits.maxSamplerAnisotropy;
        samplerCreateInfo.anisotropyEnable = false;
        samplerCreateInfo.maxAnisotropy = 1.0f;
        samplerCreateInfo.borderColor = VkBorderColor.IntOpaqueBlack;
        samplerCreateInfo.unnormalizedCoordinates = false;
        samplerCreateInfo.compareEnable = false;
        samplerCreateInfo.compareOp = VkCompareOp.Always;
        samplerCreateInfo.mipmapMode = VkSamplerMipmapMode.Linear;
        samplerCreateInfo.mipLodBias = 0.0f;
        samplerCreateInfo.minLod = 0.0f;
        samplerCreateInfo.maxLod = 0.0f;

        if (vkCreateSampler(logicalDevice.Device, &samplerCreateInfo, null, out sampler) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create texture sampler!");
        }
    }

    public void Cleanup()
    {
        vkDestroySampler(logicalDevice.Device, sampler, IntPtr.Zero);

        image.Cleanup();
    }

    internal VkDescriptorImageInfo GetDescriptorImageInfo()
    {
        if (image is not VulkanImage vulkanImage)
        {
            throw new InvalidOperationException("Invalid image type!");
        }

        return new VkDescriptorImageInfo
        {
            sampler = sampler,
            imageView = vulkanImage.imageView,
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal
        };
    }
}
