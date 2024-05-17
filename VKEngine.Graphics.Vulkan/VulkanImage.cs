using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal sealed class VulkanImageFactory(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ICommandPoolFactory commandPoolFactory, ICommandBufferAllocator commandBufferAllocator) : IImageFactory
{
    public unsafe IImage CreateImageFromFile(string filepath)
    {
        Image<Rgba32> fileImage;
        using (var fs = File.OpenRead(filepath))
        {
            fileImage = Image.Load<Rgba32>(fs);
        }
        ulong imageSize = (ulong)(fileImage.Width * fileImage.Height * Unsafe.SizeOf<Rgba32>());

        var buffer = new byte[imageSize];
        fileImage.CopyPixelDataTo(buffer);
        var image = CreateImageFromMemory(fileImage.Width, fileImage.Height, (nint)Unsafe.AsPointer(ref buffer[0]), (uint)imageSize);

        return image;
    }

    public IImage CreateImageFromMemory(int width, int height, IntPtr data, uint size)
    {
        var image = new VulkanImage(physicalDevice, logicalDevice, commandPoolFactory, commandBufferAllocator);
        image.Initialize(width, height, data, size);
        return image;
    }
}

internal sealed class VulkanImage(IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ICommandPoolFactory commandPoolFactory, ICommandBufferAllocator commandBufferAllocator) : IImage
{
    private VkImage image = VkImage.Null;
    internal VkImageView imageView = VkImageView.Null;
    private VkDeviceMemory imageMemory = VkDeviceMemory.Null;

    internal unsafe void Initialize(int width, int height, IntPtr data, uint size)
    {
        CreateBuffer(size, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out var stagingBuffer, out var stagingBufferMemory);

        void* mappedMemory;
        vkMapMemory(logicalDevice.Device, stagingBufferMemory, 0, size, 0, &mappedMemory);
        Unsafe.CopyBlock(mappedMemory, data.ToPointer(), size);
        vkUnmapMemory(logicalDevice.Device, stagingBufferMemory);

        CreateImage(width, height, VkFormat.R8g8b8a8Unorm, VkImageTiling.Optimal, VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled, VkMemoryPropertyFlags.DeviceLocal);

        TransitionImageLayout(image, VkFormat.R8g8b8a8Unorm, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, image, (uint)width, (uint)height);
        TransitionImageLayout(image, VkFormat.R8g8b8a8Unorm, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

        vkDestroyBuffer(logicalDevice.Device, stagingBuffer, null);
        vkFreeMemory(logicalDevice.Device, stagingBufferMemory, null);

        CreateImageView(image);
    }

    public void Cleanup()
    {
        vkDestroyImageView(logicalDevice.Device, imageView, IntPtr.Zero);
        vkDestroyImage(logicalDevice.Device, image, IntPtr.Zero);
        vkFreeMemory(logicalDevice.Device, imageMemory, IntPtr.Zero);
    }

    private unsafe void CreateImage(int width, int height, VkFormat format, VkImageTiling imageTiling, VkImageUsageFlags imageUsageFlags, VkMemoryPropertyFlags memoryPropertyFlags)
    {
        var imageCreateInfo = VkImageCreateInfo.New();
        imageCreateInfo.imageType = VkImageType.Image2D;
        imageCreateInfo.extent.width = (uint)width;
        imageCreateInfo.extent.height = (uint)height;
        imageCreateInfo.extent.depth = 1;
        imageCreateInfo.mipLevels = 1;
        imageCreateInfo.arrayLayers = 1;
        imageCreateInfo.format = format;
        imageCreateInfo.tiling = imageTiling;
        imageCreateInfo.initialLayout = VkImageLayout.Preinitialized;
        imageCreateInfo.usage = imageUsageFlags;
        imageCreateInfo.samples = VkSampleCountFlags.Count1;
        imageCreateInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateImage(logicalDevice.Device, &imageCreateInfo, null, out image) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create texture image!");
        }

        vkGetImageMemoryRequirements(logicalDevice.Device, image, out var memoryRequirements);

        var memoryAllocateInfo = VkMemoryAllocateInfo.New();
        memoryAllocateInfo.allocationSize = memoryRequirements.size;
        memoryAllocateInfo.memoryTypeIndex = physicalDevice.FindMemoryType(memoryRequirements.memoryTypeBits, memoryPropertyFlags);

        if (vkAllocateMemory(logicalDevice.Device, &memoryAllocateInfo, null, out imageMemory) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate image memory!");
        }

        vkBindImageMemory(logicalDevice.Device, image, imageMemory, 0);
    }

    private unsafe void CreateImageView(VkImage image)
    {
        var imageViewCreateInfo = VkImageViewCreateInfo.New();
        imageViewCreateInfo.image = image;
        imageViewCreateInfo.viewType = VkImageViewType.Image2D;
        imageViewCreateInfo.format = VkFormat.R8g8b8a8Unorm;
        imageViewCreateInfo.subresourceRange.aspectMask = VkImageAspectFlags.Color;
        imageViewCreateInfo.subresourceRange.baseMipLevel = 0;
        imageViewCreateInfo.subresourceRange.levelCount = 1;
        imageViewCreateInfo.subresourceRange.baseArrayLayer = 0;
        imageViewCreateInfo.subresourceRange.layerCount = 1;
        imageViewCreateInfo.components.r = VkComponentSwizzle.Identity;
        imageViewCreateInfo.components.g = VkComponentSwizzle.Identity;
        imageViewCreateInfo.components.b = VkComponentSwizzle.Identity;
        imageViewCreateInfo.components.a = VkComponentSwizzle.Identity;

        if (vkCreateImageView(logicalDevice.Device, &imageViewCreateInfo, null, out imageView) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create texture image view!");
        }
    }

    private unsafe void TransitionImageLayout(VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
        var commandPool = commandPoolFactory.CreateCommandPool(physicalDevice.QueueFamilyIndices.Transfer);

        var commandBuffer = commandBufferAllocator.AllocateCommandBuffer(commandPool: commandPool);
        if (commandBuffer is not VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new InvalidOperationException("Failed to allocate command buffer!");
        }

        commandBuffer.Begin(CommandBufferUsageFlags.OneTimeSubmit);

        VkImageMemoryBarrier barrier = VkImageMemoryBarrier.New();
        barrier.oldLayout = oldLayout;
        barrier.newLayout = newLayout;
        barrier.srcQueueFamilyIndex = QueueFamilyIgnored;
        barrier.dstQueueFamilyIndex = QueueFamilyIgnored;
        barrier.image = image;
        barrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
        barrier.subresourceRange.baseMipLevel = 0;
        barrier.subresourceRange.levelCount = 1;
        barrier.subresourceRange.baseArrayLayer = 0;
        barrier.subresourceRange.layerCount = 1;

        vkCmdPipelineBarrier(vulkanCommandBuffer.commandBuffer, VkPipelineStageFlags.TopOfPipe, VkPipelineStageFlags.Transfer, 0, 0, null, 0, null, 1, &barrier);

        commandBuffer.End();

        vulkanCommandBuffer.SubmitUnsafe(logicalDevice.TransferQueue);

        commandPool.Cleanup();
    }

    private unsafe void CopyBufferToImage(VkBuffer buffer, VkImage image, uint width, uint height)
    {
        var commandPool = commandPoolFactory.CreateCommandPool(physicalDevice.QueueFamilyIndices.Transfer);

        var commandBuffer = commandBufferAllocator.AllocateCommandBuffer(commandPool: commandPool);
        if (commandBuffer is not VulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new InvalidOperationException("Failed to allocate command buffer!");
        }

        commandBuffer.Begin();

        var region = new VkBufferImageCopy
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            imageOffset = new VkOffset3D { x = 0, y = 0, z = 0 },
            imageExtent = new VkExtent3D { width = width, height = height, depth = 1 }
        };

        vkCmdCopyBufferToImage(vulkanCommandBuffer.commandBuffer, buffer, image, VkImageLayout.TransferDstOptimal, 1, &region);

        commandBuffer.End();

        vulkanCommandBuffer.SubmitUnsafe(logicalDevice.TransferQueue);

        commandPool.Cleanup();
    }

    private unsafe void CreateBuffer(ulong size, VkBufferUsageFlags bufferUsageFlags, VkMemoryPropertyFlags memoryPropertyFlags, out VkBuffer buffer, out VkDeviceMemory deviceMemory)
    {
        var bufferCreateInfo = VkBufferCreateInfo.New();
        bufferCreateInfo.size = size;
        bufferCreateInfo.usage = bufferUsageFlags;
        bufferCreateInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(logicalDevice.Device, &bufferCreateInfo, null, out buffer) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to create buffer!");
        }

        vkGetBufferMemoryRequirements(logicalDevice.Device, buffer, out var memoryRequirements);

        var memoryAllocateInfo = VkMemoryAllocateInfo.New();
        memoryAllocateInfo.allocationSize = memoryRequirements.size;
        memoryAllocateInfo.memoryTypeIndex = physicalDevice.FindMemoryType(memoryRequirements.memoryTypeBits, memoryPropertyFlags);

        if (vkAllocateMemory(logicalDevice.Device, &memoryAllocateInfo, null, out deviceMemory) is not VkResult.Success)
        {
            throw new InvalidOperationException("Failed to allocate buffer memory!");
        }

        vkBindBufferMemory(logicalDevice.Device, buffer, deviceMemory, 0);
    }
}
