using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal struct QueueFamilyIndices
{
    public uint Graphics;
    public uint Compute;
    public uint Transfer;
}

internal struct Vertex
{
    public Vector2 Position;
    public Vector3 Color;
    public Vector2 TexCoord;

    public static VkVertexInputBindingDescription GetBindingDescription()
    {
        return new VkVertexInputBindingDescription()
        {
            inputRate = VkVertexInputRate.Vertex,
            stride = (uint)Unsafe.SizeOf<Vertex>(),
            binding = 0
        };
    }

    public static FixedArray3<VkVertexInputAttributeDescription> GetAttributeDescriptions()
    {
        FixedArray3<VkVertexInputAttributeDescription> ad;
        ad.First.binding = 0;
        ad.First.location = 0;
        ad.First.format = VkFormat.R32g32Sfloat;
        ad.First.offset = 0;

        ad.Second.binding = 0;
        ad.Second.location = 1;
        ad.Second.format = VkFormat.R32g32b32Sfloat;
        ad.Second.offset = (uint)Unsafe.SizeOf<Vector2>();

        ad.Third.binding = 0;
        ad.Third.location = 2;
        ad.Third.format = VkFormat.R32g32Sfloat;
        ad.Third.offset = (uint)Unsafe.SizeOf<Vector2>() + (uint)Unsafe.SizeOf<Vector3>();

        return ad;
    }
}

public struct UboMatrices
{
    public Matrix4x4 Model;
    public Matrix4x4 View;
    public Matrix4x4 Projection;

    public UboMatrices(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection)
    {
        Model = model;
        View = view;
        Projection = projection;
    }
}

public unsafe static class VkPhysicalDeviceMemoryPropertiesEx
{
    public static VkMemoryType GetMemoryType(this VkPhysicalDeviceMemoryProperties memoryProperties, uint index)
    {
        return (&memoryProperties.memoryTypes_0)[index];
    }
}

internal sealed unsafe class VulkanRenderer(IWindow window, IVulkanPhysicalDevice vulkanPhysicalDevice, IVulkanLogicalDevice vulkanLogicalDevice, IVulkanSwapChain vulkanSwapChain, IVulkanPipeline vulkanPipeline, IVulkanCommandPoolFactory vulkanCommandPoolFactory, IVulkanRenderPassFactory vulkanRenderPassFactory, IShaderLibrary shaderLibrary) : ITestRenderer
{
    private VkPipelineLayout _pipelineLayout;
    private VkBuffer _vertexBuffer;
    private VkDeviceMemory _vertexBufferMemory;
    private VkDeviceMemory _indexBufferMemory;
    private VkBuffer _indexBuffer;
    private VkDescriptorSetLayout _descriptoSetLayout;
    private VkBuffer _uboStagingBuffer;
    private VkDeviceMemory _uboStagingMemory;
    private VkBuffer _uboBuffer;
    private VkDeviceMemory _uboMemory;
    private VkDescriptorPool _descriptorPool;
    private VkDescriptorSet _descriptorSet;
    private VkImage _textureImage;
    private VkDeviceMemory _textureImageMemory;
    private VkImageView _textureImageView;
    private VkSampler _textureSampler;

    private IVulkanRenderPass renderPass;
    private IVulkanCommandPool commandPool;
    private IVulkanCommandBuffer[] commandBuffers = [];

    private RawList<VkFramebuffer> _scFramebuffers = new RawList<VkFramebuffer>();

    private DateTime _startTime = DateTime.UtcNow;

    private RawList<Vertex> _vertices = new RawList<Vertex>
        {
            new Vertex { Position = new Vector2(-0.5f, -0.5f), Color = new Vector3(1f, 0f, 0f), TexCoord = new Vector2(0f, 0f) },
            new Vertex { Position = new Vector2(0.5f, -0.5f), Color = new Vector3(0f, 1f, 0f), TexCoord = new Vector2(1f, 0f) },
            new Vertex { Position = new Vector2(0.5f, 0.5f), Color = new Vector3(0f, 0f, 1f), TexCoord = new Vector2(1f, 1f) },
            new Vertex { Position = new Vector2(-0.5f, 0.5f), Color = new Vector3(1f, 1f, 1f), TexCoord = new Vector2(0f, 1f) },
        };

    private ushort[] _indices =
    {
            0, 1, 2, 0, 2, 3,
        };

    public void Initialize()
    {
        renderPass = vulkanRenderPassFactory.CreateRenderPass(vulkanSwapChain);
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        commandPool = vulkanCommandPoolFactory.CreateCommandPool();
        CreateTextureImage();
        CreateTextureImageView();
        CreateTextureSampler();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffer();
        CreateDescriptorPool();
        CreateDescriptorSet();
    }

    public void Cleanup()
    {
    }

    public void RenderTriangle()
    {
        //uint imageIndex = 0;
        //vulkanSwapChain.AquireNextImage(ref imageIndex);

        // Do realtime rendering here?
        UpdateUniformBuffer();

        //vulkanSwapChain.Present(commandBuffers[imageIndex]);
    }

    public void RecordCommandBuffer(uint i)
    {
        var commandBuffer = vulkanSwapChain.CurrentCommandBuffer;
        if(commandBuffer is not IVulkanCommandBuffer vulkanCommandBuffer)
        {
            throw new InvalidCastException("Command buffer must be of type IVulkanCommandBuffer");
        }

        var cb = vulkanCommandBuffer.Raw;

        VkRenderPassBeginInfo rpbi = VkRenderPassBeginInfo.New();
        rpbi.renderPass = renderPass.Raw;
        rpbi.framebuffer = _scFramebuffers[vulkanSwapChain.CurrentImageIndex];
        rpbi.renderArea.extent = vulkanSwapChain.Extent;

        VkClearValue clearValue = new VkClearValue() { color = new VkClearColorValue(0.2f, 0.3f, 0.3f, 1.0f) };
        rpbi.clearValueCount = 1;
        rpbi.pClearValues = &clearValue;

        vkCmdBeginRenderPass(cb, ref rpbi, VkSubpassContents.Inline);

        vkCmdBindPipeline(cb, VkPipelineBindPoint.Graphics, vulkanPipeline.Raw); //  _graphicsPipeline

        vkCmdBindDescriptorSets(
            cb,
            VkPipelineBindPoint.Graphics,
            _pipelineLayout,
            0,
            1,
            ref _descriptorSet,
            0,
            null);

        ulong offset = 0;
        vkCmdBindVertexBuffers(cb, 0, 1, ref _vertexBuffer, ref offset);
        vkCmdBindIndexBuffer(cb, _indexBuffer, 0, VkIndexType.Uint16);

        vkCmdDrawIndexed(cb, (uint)_indices.Length, 1, 0, 0, 0);

        vkCmdEndRenderPass(cb);
    }

    private void UpdateUniformBuffer()
    {
        DateTime currentTime = DateTime.UtcNow;
        float time = (float)((currentTime - _startTime).TotalSeconds);

        UboMatrices uboMatrices = new UboMatrices(
            Matrix4x4.CreateRotationZ(time * DegreesToRadians(90f)),
            Matrix4x4.CreateLookAt(new Vector3(2, 2, 2), new Vector3(), new Vector3(0, 0, 1)),
            Matrix4x4.CreatePerspectiveFieldOfView(DegreesToRadians(45f), (float)window.Width / window.Height, 0.1f, 10f));

        uboMatrices.Projection.M22 *= -1; // ?

        UploadBufferData(_uboStagingMemory, ref uboMatrices, 1);
        CopyBuffer(_uboStagingBuffer, _uboBuffer, (ulong)Unsafe.SizeOf<UboMatrices>());
    }

    private float DegreesToRadians(float degrees)
    {
        return (float)((degrees / 360f) * 2 * Math.PI);
    }

    private void CreateDescriptorSetLayout()
    {
        VkDescriptorSetLayoutBinding ubBinding = new VkDescriptorSetLayoutBinding();
        ubBinding.binding = 0;
        ubBinding.descriptorCount = 1;
        ubBinding.descriptorType = VkDescriptorType.UniformBuffer;
        ubBinding.stageFlags = VkShaderStageFlags.Vertex;

        VkDescriptorSetLayoutBinding samplerBinding = new VkDescriptorSetLayoutBinding();
        samplerBinding.binding = 1;
        samplerBinding.descriptorCount = 1;
        samplerBinding.descriptorType = VkDescriptorType.CombinedImageSampler;
        samplerBinding.stageFlags = VkShaderStageFlags.Fragment;

        FixedArray2<VkDescriptorSetLayoutBinding> bindings
            = new FixedArray2<VkDescriptorSetLayoutBinding>(ubBinding, samplerBinding);
        VkDescriptorSetLayoutCreateInfo dslCI = VkDescriptorSetLayoutCreateInfo.New();
        dslCI.bindingCount = bindings.Count;
        dslCI.pBindings = &bindings.First;

        vkCreateDescriptorSetLayout(vulkanLogicalDevice.Device, ref dslCI, null, out _descriptoSetLayout);
    }

    private void CreateGraphicsPipeline()
    {
        var shader = shaderLibrary.Get<IVulkanShader>("shader");
        if (shader is null)
        {
            throw new Exception("Shader not found");
        }

        VkDescriptorSetLayout dsl = _descriptoSetLayout;
        VkPipelineLayoutCreateInfo pipelineLayoutCI = VkPipelineLayoutCreateInfo.New();
        pipelineLayoutCI.setLayoutCount = 1;
        pipelineLayoutCI.pSetLayouts = &dsl;
        vkCreatePipelineLayout(vulkanLogicalDevice.Device, ref pipelineLayoutCI, null, out _pipelineLayout);

        vulkanPipeline.Initialize(shader, renderPass.Raw, _pipelineLayout);
    }

    private void CreateFramebuffers()
    {
        _scFramebuffers.Resize(vulkanSwapChain.ImageViews.Count);
        for (uint i = 0; i < vulkanSwapChain.ImageViews.Count; i++)
        {
            VkImageView attachment = vulkanSwapChain.ImageViews[i];
            VkFramebufferCreateInfo framebufferCI = VkFramebufferCreateInfo.New();
            framebufferCI.renderPass = renderPass.Raw;
            framebufferCI.attachmentCount = 1;
            framebufferCI.pAttachments = &attachment;
            framebufferCI.width = vulkanSwapChain.Extent.width;
            framebufferCI.height = vulkanSwapChain.Extent.height;
            framebufferCI.layers = 1;

            vkCreateFramebuffer(vulkanLogicalDevice.Device, ref framebufferCI, null, out _scFramebuffers[i]);
        }
    }

    private void CreateTextureImage()
    {
        Image<Rgba32> image;
        using (var fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "textures", "texture.jpg")))
        {
            image = Image.Load<Rgba32>(fs);
        }
        ulong imageSize = (ulong)(image.Width * image.Height * Unsafe.SizeOf<Rgba32>());

        CreateImage(
            (uint)image.Width,
            (uint)image.Height,
            VkFormat.R8g8b8a8Unorm,
            VkImageTiling.Linear,
            VkImageUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            out VkImage stagingImage,
            out VkDeviceMemory stagingImageMemory);

        VkImageSubresource subresource = new VkImageSubresource();
        subresource.aspectMask = VkImageAspectFlags.Color;
        subresource.mipLevel = 0;
        subresource.arrayLayer = 0;

        vkGetImageSubresourceLayout(vulkanLogicalDevice.Device, stagingImage, ref subresource, out VkSubresourceLayout stagingImageLayout);
        ulong rowPitch = stagingImageLayout.rowPitch;

        void* mappedPtr;
        vkMapMemory(vulkanLogicalDevice.Device, stagingImageMemory, 0, imageSize, 0, &mappedPtr);
        var memoryGroup = image.GetPixelMemoryGroup();
        var firstMemory = memoryGroup.ToArray()[0];
        var pixelData = MemoryMarshal.AsBytes(firstMemory.Span).ToArray();
        fixed (void* pixelsPtr = pixelData)
        {
            if (rowPitch == (ulong)image.Width)
            {
                Buffer.MemoryCopy(pixelsPtr, mappedPtr, imageSize, imageSize);
            }
            else
            {
                for (uint y = 0; y < image.Height; y++)
                {
                    byte* dstRowStart = ((byte*)mappedPtr) + (rowPitch * y);
                    byte* srcRowStart = ((byte*)pixelsPtr) + (image.Width * y * Unsafe.SizeOf<Rgba32>());
                    Unsafe.CopyBlock(dstRowStart, srcRowStart, (uint)(image.Width * Unsafe.SizeOf<Rgba32>()));
                }
            }
        }
        vkUnmapMemory(vulkanLogicalDevice.Device, stagingImageMemory);

        CreateImage(
            (uint)image.Width,
            (uint)image.Height,
            VkFormat.R8g8b8a8Unorm,
            VkImageTiling.Optimal,
            VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
            VkMemoryPropertyFlags.DeviceLocal,
            out _textureImage,
            out _textureImageMemory);

        TransitionImageLayout(stagingImage, VkFormat.R8g8b8a8Unorm, VkImageLayout.Preinitialized, VkImageLayout.TransferSrcOptimal);
        TransitionImageLayout(_textureImage, VkFormat.R8g8b8a8Unorm, VkImageLayout.Preinitialized, VkImageLayout.TransferDstOptimal);
        CopyImage(stagingImage, _textureImage, (uint)image.Width, (uint)image.Height);
        TransitionImageLayout(_textureImage, VkFormat.R8g8b8a8Unorm, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

        vkDestroyImage(vulkanLogicalDevice.Device, stagingImage, null);
    }

    private void CreateTextureImageView()
    {
        CreateImageView(_textureImage, VkFormat.R8g8b8a8Unorm, out _textureImageView);
    }

    private void CreateTextureSampler()
    {
        VkSamplerCreateInfo samplerCI = VkSamplerCreateInfo.New();
        samplerCI.magFilter = VkFilter.Linear;
        samplerCI.minFilter = VkFilter.Linear;
        samplerCI.anisotropyEnable = false;
        samplerCI.maxAnisotropy = 16;
        samplerCI.borderColor = VkBorderColor.IntOpaqueBlack;
        samplerCI.unnormalizedCoordinates = false;
        samplerCI.compareEnable = false;
        samplerCI.compareOp = VkCompareOp.Always;
        samplerCI.mipmapMode = VkSamplerMipmapMode.Linear;
        samplerCI.mipLodBias = 0f;
        samplerCI.minLod = 0f;
        samplerCI.maxLod = 0f;

        vkCreateSampler(vulkanLogicalDevice.Device, ref samplerCI, null, out _textureSampler);
    }

    private void CreateImage(
        uint width,
        uint height,
        VkFormat format,
        VkImageTiling tiling,
        VkImageUsageFlags usage,
        VkMemoryPropertyFlags properties,
        out VkImage image,
        out VkDeviceMemory memory)
    {
        VkImageCreateInfo imageCI = VkImageCreateInfo.New();
        imageCI.imageType = VkImageType.Image2D;
        imageCI.extent.width = width;
        imageCI.extent.height = height;
        imageCI.extent.depth = 1;
        imageCI.mipLevels = 1;
        imageCI.arrayLayers = 1;
        imageCI.format = format;
        imageCI.tiling = tiling;
        imageCI.initialLayout = VkImageLayout.Preinitialized;
        imageCI.usage = usage;
        imageCI.sharingMode = VkSharingMode.Exclusive;
        imageCI.samples = VkSampleCountFlags.Count1;

        vkCreateImage(vulkanLogicalDevice.Device, ref imageCI, null, out image);

        vkGetImageMemoryRequirements(vulkanLogicalDevice.Device, image, out VkMemoryRequirements memRequirements);
        VkMemoryAllocateInfo allocInfo = VkMemoryAllocateInfo.New();
        allocInfo.allocationSize = memRequirements.size;
        allocInfo.memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties);
        vkAllocateMemory(vulkanLogicalDevice.Device, ref allocInfo, null, out memory);

        vkBindImageMemory(vulkanLogicalDevice.Device, image, memory, 0);
    }

    private void CreateVertexBuffer()
    {
        ulong vertexBufferSize = (uint)Unsafe.SizeOf<Vertex>() * _vertices.Count;
        CreateBuffer(
            vertexBufferSize,
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            out VkBuffer stagingBuffer,
            out VkDeviceMemory stagingMemory);
        UploadBufferData(stagingMemory, _vertices.Items);
        CreateBuffer(
            vertexBufferSize,
            VkBufferUsageFlags.VertexBuffer | VkBufferUsageFlags.TransferDst,
            VkMemoryPropertyFlags.DeviceLocal,
            out _vertexBuffer,
            out _vertexBufferMemory);
        CopyBuffer(stagingBuffer, _vertexBuffer, vertexBufferSize);
    }

    private void CreateIndexBuffer()
    {
        ulong indexBufferSize = (ulong)(sizeof(ushort) * _indices.Length);
        CreateBuffer(
            indexBufferSize,
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            out VkBuffer stagingBuffer,
            out VkDeviceMemory stagingMemory);
        UploadBufferData(stagingMemory, _indices);
        CreateBuffer(
            indexBufferSize,
            VkBufferUsageFlags.IndexBuffer | VkBufferUsageFlags.TransferDst,
            VkMemoryPropertyFlags.DeviceLocal,
            out _indexBuffer,
            out _indexBufferMemory);
        CopyBuffer(stagingBuffer, _indexBuffer, indexBufferSize);
    }

    private void CreateUniformBuffer()
    {
        ulong bufferSize = (ulong)Unsafe.SizeOf<UboMatrices>();
        CreateBuffer(bufferSize, VkBufferUsageFlags.UniformBuffer | VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out _uboStagingBuffer, out _uboStagingMemory);
        CreateBuffer(bufferSize, VkBufferUsageFlags.UniformBuffer | VkBufferUsageFlags.TransferDst, VkMemoryPropertyFlags.DeviceLocal, out _uboBuffer, out _uboMemory);
    }

    private void CreateDescriptorPool()
    {
        FixedArray2<VkDescriptorPoolSize> poolSizes;
        poolSizes.First.type = VkDescriptorType.UniformBuffer;
        poolSizes.First.descriptorCount = 1;
        poolSizes.Second.type = VkDescriptorType.CombinedImageSampler;
        poolSizes.Second.descriptorCount = 1;

        VkDescriptorPoolCreateInfo descriptorPoolCI = VkDescriptorPoolCreateInfo.New();
        descriptorPoolCI.poolSizeCount = poolSizes.Count;
        descriptorPoolCI.pPoolSizes = &poolSizes.First;
        descriptorPoolCI.maxSets = 1;
        vkCreateDescriptorPool(vulkanLogicalDevice.Device, ref descriptorPoolCI, null, out _descriptorPool);
    }

    private void CreateDescriptorSet()
    {
        VkDescriptorSetLayout dsl = _descriptoSetLayout;
        VkDescriptorSetAllocateInfo dsAI = VkDescriptorSetAllocateInfo.New();
        dsAI.descriptorPool = _descriptorPool;
        dsAI.pSetLayouts = &dsl;
        dsAI.descriptorSetCount = 1;
        vkAllocateDescriptorSets(vulkanLogicalDevice.Device, ref dsAI, out _descriptorSet);

        VkDescriptorBufferInfo bufferInfo = new VkDescriptorBufferInfo();
        bufferInfo.buffer = _uboBuffer;
        bufferInfo.offset = 0;
        bufferInfo.range = (ulong)Unsafe.SizeOf<UboMatrices>();

        FixedArray2<VkWriteDescriptorSet> descriptorWrites;

        descriptorWrites.First = VkWriteDescriptorSet.New();
        descriptorWrites.First.dstSet = _descriptorSet;
        descriptorWrites.First.dstBinding = 0;
        descriptorWrites.First.dstArrayElement = 0;
        descriptorWrites.First.descriptorType = VkDescriptorType.UniformBuffer;
        descriptorWrites.First.descriptorCount = 1;
        descriptorWrites.First.pBufferInfo = &bufferInfo;

        VkDescriptorImageInfo imageInfo;
        imageInfo.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
        imageInfo.imageView = _textureImageView;
        imageInfo.sampler = _textureSampler;

        descriptorWrites.Second = VkWriteDescriptorSet.New();
        descriptorWrites.Second.dstSet = _descriptorSet;
        descriptorWrites.Second.dstBinding = 1;
        descriptorWrites.Second.dstArrayElement = 0;
        descriptorWrites.Second.descriptorType = VkDescriptorType.CombinedImageSampler;
        descriptorWrites.Second.descriptorCount = 1;
        descriptorWrites.Second.pImageInfo = &imageInfo;

        vkUpdateDescriptorSets(vulkanLogicalDevice.Device, descriptorWrites.Count, &descriptorWrites.First, 0, null);
    }

    private void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory memory)
    {
        VkBufferCreateInfo bufferCI = VkBufferCreateInfo.New();
        bufferCI.size = size;
        bufferCI.usage = usage;
        bufferCI.sharingMode = VkSharingMode.Exclusive;
        vkCreateBuffer(vulkanLogicalDevice.Device, ref bufferCI, null, out buffer);

        vkGetBufferMemoryRequirements(vulkanLogicalDevice.Device, buffer, out VkMemoryRequirements memReqs);
        VkMemoryAllocateInfo memAllocCI = VkMemoryAllocateInfo.New();
        memAllocCI.allocationSize = memReqs.size;
        memAllocCI.memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        vkAllocateMemory(vulkanLogicalDevice.Device, ref memAllocCI, null, out memory);
        vkBindBufferMemory(vulkanLogicalDevice.Device, buffer, memory, 0);
    }

    private void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size)
    {
        VkCommandBuffer copyCmd = BeginOneTimeCommands();

        VkBufferCopy bufferCopy = new VkBufferCopy();
        bufferCopy.size = size;
        vkCmdCopyBuffer(copyCmd, srcBuffer, dstBuffer, 1, ref bufferCopy);

        EndOneTimeCommands(copyCmd);
    }

    private void UploadBufferData<T>(VkDeviceMemory memory, T[] data)
    {
        ulong size = (ulong)(data.Length * Unsafe.SizeOf<T>());
        void* mappedMemory;
        vkMapMemory(vulkanLogicalDevice.Device, memory, 0, size, 0, &mappedMemory);
        GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
        Unsafe.CopyBlock(mappedMemory, gh.AddrOfPinnedObject().ToPointer(), (uint)size);
        gh.Free();
        vkUnmapMemory(vulkanLogicalDevice.Device, memory);
    }

    private void UploadBufferData<T>(VkDeviceMemory memory, ref T data, uint count)
    {
        ulong size = (ulong)(count * Unsafe.SizeOf<T>());
        void* mappedMemory;
        vkMapMemory(vulkanLogicalDevice.Device, memory, 0, size, 0, &mappedMemory);
        void* dataPtr = Unsafe.AsPointer(ref data);
        Unsafe.CopyBlock(mappedMemory, dataPtr, (uint)size);
        vkUnmapMemory(vulkanLogicalDevice.Device, memory);
    }


    private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        vkGetPhysicalDeviceMemoryProperties(vulkanPhysicalDevice.PhysicalDevice, out VkPhysicalDeviceMemoryProperties memProperties);
        for (int i = 0; i < memProperties.memoryTypeCount; i++)
        {
            if (((typeFilter & (1 << i)) != 0)
                && (memProperties.GetMemoryType((uint)i).propertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new InvalidOperationException("No suitable memory type.");
    }

    private void RecreateSwapChain()
    {
        vkDeviceWaitIdle(vulkanLogicalDevice.Device);

        //CreateSwapchain();
        //CreateImageViews();
        //CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        //CreateCommandBuffers();
    }

    private VkCommandBuffer BeginOneTimeCommands()
    {
        VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
        allocInfo.commandBufferCount = 1;
        allocInfo.commandPool = commandPool.Raw;
        allocInfo.level = VkCommandBufferLevel.Primary;

        vkAllocateCommandBuffers(vulkanLogicalDevice.Device, ref allocInfo, out VkCommandBuffer cb);

        VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
        beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

        vkBeginCommandBuffer(cb, ref beginInfo);

        return cb;
    }

    private void EndOneTimeCommands(VkCommandBuffer cb)
    {
        vkEndCommandBuffer(cb);

        VkSubmitInfo submitInfo = VkSubmitInfo.New();
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &cb;


        vkQueueSubmit(vulkanLogicalDevice.GraphicsQueue, 1, &submitInfo, VkFence.Null);
        vkQueueWaitIdle(vulkanLogicalDevice.GraphicsQueue);

        vkFreeCommandBuffers(vulkanLogicalDevice.Device, commandPool.Raw, 1, ref cb);
    }

    private void TransitionImageLayout(VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
        VkCommandBuffer cb = BeginOneTimeCommands();

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

        vkCmdPipelineBarrier(
            cb,
            VkPipelineStageFlags.TopOfPipe,
            VkPipelineStageFlags.TopOfPipe,
            VkDependencyFlags.None,
            0, null,
            0, null,
            1, &barrier);

        EndOneTimeCommands(cb);
    }

    private void CopyImage(VkImage srcImage, VkImage dstImage, uint width, uint height)
    {
        VkImageSubresourceLayers subresource = new VkImageSubresourceLayers();
        subresource.mipLevel = 0;
        subresource.layerCount = 1;
        subresource.aspectMask = VkImageAspectFlags.Color;
        subresource.baseArrayLayer = 0;

        VkImageCopy region = new VkImageCopy();
        region.dstSubresource = subresource;
        region.srcSubresource = subresource;
        region.extent.width = width;
        region.extent.height = height;
        region.extent.depth = 1;

        VkCommandBuffer copyCmd = BeginOneTimeCommands();
        vkCmdCopyImage(copyCmd, srcImage, VkImageLayout.TransferSrcOptimal, dstImage, VkImageLayout.TransferDstOptimal, 1, ref region);
        EndOneTimeCommands(copyCmd);
    }

    private void CreateImageView(VkImage image, VkFormat format, out VkImageView imageView)
    {
        VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
        imageViewCI.image = image;
        imageViewCI.viewType = VkImageViewType.Image2D;
        imageViewCI.format = format;
        imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
        imageViewCI.subresourceRange.baseMipLevel = 0;
        imageViewCI.subresourceRange.levelCount = 1;
        imageViewCI.subresourceRange.baseArrayLayer = 0;
        imageViewCI.subresourceRange.layerCount = 1;

        vkCreateImageView(vulkanLogicalDevice.Device, ref imageViewCI, null, out imageView);
    }

    private static void CheckResult(VkResult result)
    {
        if (result != VkResult.Success)
        {
            Console.WriteLine($"Vulkan call was not successful: {result}");
        }
    }
}
