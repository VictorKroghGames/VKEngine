using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using VKEngine.Graphics.Vulkan.Native;
using Vulkan;
using static Vulkan.VulkanNative;

namespace VKEngine.Graphics.Vulkan;

internal unsafe class VulkanTutorial(IWindow window, IVulkanPhysicalDevice physicalDevice, IVulkanLogicalDevice logicalDevice, ISwapChain swapChain) : ITestRenderer
{
    private VkPipelineLayout _pipelineLayout;
    private VkRenderPass _renderPass;
    private VkPipeline _graphicsPipeline;
    private VkCommandPool _commandPool;
    private RawList<VkCommandBuffer> _commandBuffers = new RawList<VkCommandBuffer>();
    private VkSemaphore _imageAvailableSemaphore;
    private VkSemaphore _renderCompleteSemaphore;
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

    private VkShaderModule vertexShaderModule = VkShaderModule.Null;
    private VkShaderModule fragmentShaderModule = VkShaderModule.Null;

    // Swapchain stuff
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

    private VulkanSwapChain vulkanSwapChain = default!;

    public void Initialize()
    {
        if (swapChain is not VulkanSwapChain vulkanSwapChain)
        {
            throw new InvalidOperationException("Swap chain is not a Vulkan swap chain.");
        }

        this.vulkanSwapChain = vulkanSwapChain;

        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateTextureImage();
        CreateTextureImageView();
        CreateTextureSampler();
        CreateVertexBuffer();
        CreateIndexBuffer();
        CreateUniformBuffer();
        CreateDescriptorPool();
        CreateDescriptorSet();
        CreateCommandBuffers();
        CreateSemaphores();
    }

    public void Cleanup()
    {
        vkDeviceWaitIdle(logicalDevice.Device);

        vkDestroySampler(logicalDevice.Device, _textureSampler, null);
        vkDestroyImageView(logicalDevice.Device, _textureImageView, null);
        vkDestroyImage(logicalDevice.Device, _textureImage, null);
        vkFreeMemory(logicalDevice.Device, _textureImageMemory, null);

        vkDestroyDescriptorPool(logicalDevice.Device, _descriptorPool, null);

        vkDestroyBuffer(logicalDevice.Device, _uboBuffer, null);
        vkFreeMemory(logicalDevice.Device, _uboMemory, null);
        vkDestroyBuffer(logicalDevice.Device, _uboStagingBuffer, null);
        vkFreeMemory(logicalDevice.Device, _uboStagingMemory, null);

        vkDestroyBuffer(logicalDevice.Device, _indexBuffer, null);
        vkFreeMemory(logicalDevice.Device, _indexBufferMemory, null);

        vkDestroyBuffer(logicalDevice.Device, _vertexBuffer, null);
        vkFreeMemory(logicalDevice.Device, _vertexBufferMemory, null);

        for (int i = 0; i < _scFramebuffers.Count; i++)
        {
            vkDestroyFramebuffer(logicalDevice.Device, _scFramebuffers[i], null);
        }

        vkDestroyShaderModule(logicalDevice.Device, vertexShaderModule, null);
        vkDestroyShaderModule(logicalDevice.Device, fragmentShaderModule, null);

        vkDestroyPipeline(logicalDevice.Device, _graphicsPipeline, null);
        vkDestroyRenderPass(logicalDevice.Device, _renderPass, null);
        vkDestroyPipelineLayout(logicalDevice.Device, _pipelineLayout, null);
        vkDestroyDescriptorSetLayout(logicalDevice.Device, _descriptoSetLayout, null);

        // clear command buffers
        vkFreeCommandBuffers(logicalDevice.Device, _commandPool, _commandBuffers.Count, ref _commandBuffers[0]);

        vkDestroyCommandPool(logicalDevice.Device, _commandPool, null);

        vkDestroySemaphore(logicalDevice.Device, _renderCompleteSemaphore, null);
        vkDestroySemaphore(logicalDevice.Device, _imageAvailableSemaphore, null);
    }
    public void RenderTriangle()
    {
        UpdateUniformBuffer();
        DrawFrame();
    }

    private void UpdateUniformBuffer()
    {
        DateTime currentTime = DateTime.UtcNow;
        float time = (float)((currentTime - _startTime).TotalSeconds);

        UboMatrices uboMatrices = new UboMatrices(
            Matrix4x4.CreateRotationZ(time * DegreesToRadians(90f)),
            Matrix4x4.CreateLookAt(new Vector3(2, 2, 2), new Vector3(), new Vector3(0, 0, 1)),
            Matrix4x4.CreatePerspectiveFieldOfView(DegreesToRadians(45f), (float)window.Width / window.Height, 0.1f, 10f));
        //Matrix4x4.CreatePerspectiveFieldOfView(DegreesToRadians(45f), (float)_scExtent.width / _scExtent.height, 0.1f, 10f));

        uboMatrices.Projection.M22 *= -1; // ?

        UploadBufferData(_uboStagingMemory, ref uboMatrices, 1);
        CopyBuffer(_uboStagingBuffer, _uboBuffer, (ulong)Unsafe.SizeOf<UboMatrices>());
    }

    private float DegreesToRadians(float degrees)
    {
        return (float)((degrees / 360f) * 2 * Math.PI);
    }

    private void DrawFrame()
    {
        uint imageIndex = 0;
        VkResult result = vkAcquireNextImageKHR(logicalDevice.Device, vulkanSwapChain.swapchain, ulong.MaxValue, _imageAvailableSemaphore, VkFence.Null, ref imageIndex);
        if (result == VkResult.ErrorOutOfDateKHR || result == VkResult.SuboptimalKHR)
        {
            RecreateSwapChain();
        }
        else if (result != VkResult.Success)
        {
            throw new InvalidOperationException("Acquiring next image failed: " + result);
        }

        VkSubmitInfo submitInfo = VkSubmitInfo.New();
        VkSemaphore waitSemaphore = _imageAvailableSemaphore;
        VkPipelineStageFlags waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
        submitInfo.waitSemaphoreCount = 1;
        submitInfo.pWaitSemaphores = &waitSemaphore;
        submitInfo.pWaitDstStageMask = &waitStages;
        VkCommandBuffer cb = _commandBuffers[imageIndex];
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &cb;
        VkSemaphore signalSemaphore = _renderCompleteSemaphore;
        submitInfo.signalSemaphoreCount = 1;
        submitInfo.pSignalSemaphores = &signalSemaphore;
        vkQueueSubmit(logicalDevice.GraphicsQueue, 1, &submitInfo, VkFence.Null);

        VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
        presentInfo.waitSemaphoreCount = 1;
        presentInfo.pWaitSemaphores = &signalSemaphore;

        VkSwapchainKHR swapchain = vulkanSwapChain.swapchain;
        presentInfo.swapchainCount = 1;
        presentInfo.pSwapchains = &swapchain;
        presentInfo.pImageIndices = &imageIndex;

        vkQueuePresentKHR(logicalDevice.PresentQueue, ref presentInfo);
    }

    private void CreateRenderPass()
    {
        VkAttachmentDescription colorAttachment = new VkAttachmentDescription();
        colorAttachment.format = vulkanSwapChain.surfaceFormat.format;
        colorAttachment.samples = VkSampleCountFlags.Count1;
        colorAttachment.loadOp = VkAttachmentLoadOp.Clear;
        colorAttachment.storeOp = VkAttachmentStoreOp.Store;
        colorAttachment.stencilLoadOp = VkAttachmentLoadOp.DontCare;
        colorAttachment.stencilStoreOp = VkAttachmentStoreOp.DontCare;
        colorAttachment.initialLayout = VkImageLayout.Undefined;
        colorAttachment.finalLayout = VkImageLayout.PresentSrcKHR;

        VkAttachmentReference colorAttachmentRef = new VkAttachmentReference();
        colorAttachmentRef.attachment = 0;
        colorAttachmentRef.layout = VkImageLayout.ColorAttachmentOptimal;

        VkSubpassDescription subpass = new VkSubpassDescription();
        subpass.pipelineBindPoint = VkPipelineBindPoint.Graphics;
        subpass.colorAttachmentCount = 1;
        subpass.pColorAttachments = &colorAttachmentRef;

        VkRenderPassCreateInfo renderPassCI = VkRenderPassCreateInfo.New();
        renderPassCI.attachmentCount = 1;
        renderPassCI.pAttachments = &colorAttachment;
        renderPassCI.subpassCount = 1;
        renderPassCI.pSubpasses = &subpass;

        VkSubpassDependency dependency = new VkSubpassDependency();
        dependency.srcSubpass = SubpassExternal;
        dependency.dstSubpass = 0;
        dependency.srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        dependency.srcAccessMask = 0;
        dependency.dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
        dependency.dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite;

        renderPassCI.dependencyCount = 1;
        renderPassCI.pDependencies = &dependency;

        vkCreateRenderPass(logicalDevice.Device, ref renderPassCI, null, out _renderPass);
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

        vkCreateDescriptorSetLayout(logicalDevice.Device, ref dslCI, null, out _descriptoSetLayout);
    }

    private void CreateGraphicsPipeline()
    {
        byte[] vertBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.vert.spv"));
        byte[] fragBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.frag.spv"));

        vertexShaderModule = CreateShader(vertBytes);
        fragmentShaderModule = CreateShader(fragBytes);

        VkPipelineShaderStageCreateInfo vertCreateInfo = VkPipelineShaderStageCreateInfo.New();
        vertCreateInfo.stage = VkShaderStageFlags.Vertex;
        vertCreateInfo.module = vertexShaderModule;
        vertCreateInfo.pName = Strings.main;

        VkPipelineShaderStageCreateInfo fragCreateInfo = VkPipelineShaderStageCreateInfo.New();
        fragCreateInfo.stage = VkShaderStageFlags.Fragment;
        fragCreateInfo.module = fragmentShaderModule;
        fragCreateInfo.pName = Strings.main;

        FixedArray2<VkPipelineShaderStageCreateInfo> shaderStageCreateInfos
            = new FixedArray2<VkPipelineShaderStageCreateInfo>(vertCreateInfo, fragCreateInfo);

        VkPipelineVertexInputStateCreateInfo vertexInputStateCI = VkPipelineVertexInputStateCreateInfo.New();
        var vertexBindingDesc = Vertex.GetBindingDescription();
        var attributeDescr = Vertex.GetAttributeDescriptions();
        vertexInputStateCI.vertexBindingDescriptionCount = 1;
        vertexInputStateCI.pVertexBindingDescriptions = &vertexBindingDesc;
        vertexInputStateCI.vertexAttributeDescriptionCount = attributeDescr.Count;
        vertexInputStateCI.pVertexAttributeDescriptions = &attributeDescr.First;

        VkPipelineInputAssemblyStateCreateInfo inputAssemblyCI = VkPipelineInputAssemblyStateCreateInfo.New();
        inputAssemblyCI.primitiveRestartEnable = false;
        inputAssemblyCI.topology = VkPrimitiveTopology.TriangleList;

        VkViewport viewport = new VkViewport();
        viewport.x = 0;
        viewport.y = 0;
        viewport.width = vulkanSwapChain.extent.width;
        viewport.height = vulkanSwapChain.extent.height;
        viewport.minDepth = 0f;
        viewport.maxDepth = 1f;

        VkRect2D scissorRect = new VkRect2D(vulkanSwapChain.extent);

        VkPipelineViewportStateCreateInfo viewportStateCI = VkPipelineViewportStateCreateInfo.New();
        viewportStateCI.viewportCount = 1;
        viewportStateCI.pViewports = &viewport;
        viewportStateCI.scissorCount = 1;
        viewportStateCI.pScissors = &scissorRect;

        VkPipelineRasterizationStateCreateInfo rasterizerStateCI = VkPipelineRasterizationStateCreateInfo.New();
        rasterizerStateCI.cullMode = VkCullModeFlags.Back;
        rasterizerStateCI.polygonMode = VkPolygonMode.Fill;
        rasterizerStateCI.lineWidth = 1f;
        rasterizerStateCI.frontFace = VkFrontFace.CounterClockwise;

        VkPipelineMultisampleStateCreateInfo multisampleStateCI = VkPipelineMultisampleStateCreateInfo.New();
        multisampleStateCI.rasterizationSamples = VkSampleCountFlags.Count1;
        multisampleStateCI.minSampleShading = 1f;

        VkPipelineColorBlendAttachmentState colorBlendAttachementState = new VkPipelineColorBlendAttachmentState();
        colorBlendAttachementState.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
        colorBlendAttachementState.blendEnable = false;

        VkPipelineColorBlendStateCreateInfo colorBlendStateCI = VkPipelineColorBlendStateCreateInfo.New();
        colorBlendStateCI.attachmentCount = 1;
        colorBlendStateCI.pAttachments = &colorBlendAttachementState;

        VkDescriptorSetLayout dsl = _descriptoSetLayout;
        VkPipelineLayoutCreateInfo pipelineLayoutCI = VkPipelineLayoutCreateInfo.New();
        pipelineLayoutCI.setLayoutCount = 1;
        pipelineLayoutCI.pSetLayouts = &dsl;
        vkCreatePipelineLayout(logicalDevice.Device, ref pipelineLayoutCI, null, out _pipelineLayout);

        VkGraphicsPipelineCreateInfo graphicsPipelineCI = VkGraphicsPipelineCreateInfo.New();
        graphicsPipelineCI.stageCount = shaderStageCreateInfos.Count;
        graphicsPipelineCI.pStages = &shaderStageCreateInfos.First;

        graphicsPipelineCI.pVertexInputState = &vertexInputStateCI;
        graphicsPipelineCI.pInputAssemblyState = &inputAssemblyCI;
        graphicsPipelineCI.pViewportState = &viewportStateCI;
        graphicsPipelineCI.pRasterizationState = &rasterizerStateCI;
        graphicsPipelineCI.pMultisampleState = &multisampleStateCI;
        graphicsPipelineCI.pColorBlendState = &colorBlendStateCI;
        graphicsPipelineCI.layout = _pipelineLayout;

        graphicsPipelineCI.renderPass = _renderPass;
        graphicsPipelineCI.subpass = 0;

        vkCreateGraphicsPipelines(logicalDevice.Device, VkPipelineCache.Null, 1, ref graphicsPipelineCI, null, out _graphicsPipeline);
    }

    private void CreateFramebuffers()
    {
        _scFramebuffers.Resize((uint)vulkanSwapChain.imageViews.Length);
        for (uint i = 0; i < vulkanSwapChain.imageViews.Length; i++)
        {
            VkImageView attachment = vulkanSwapChain.imageViews[i];
            VkFramebufferCreateInfo framebufferCI = VkFramebufferCreateInfo.New();
            framebufferCI.renderPass = _renderPass;
            framebufferCI.attachmentCount = 1;
            framebufferCI.pAttachments = &attachment;
            framebufferCI.width = vulkanSwapChain.extent.width;
            framebufferCI.height = vulkanSwapChain.extent.height;
            framebufferCI.layers = 1;

            vkCreateFramebuffer(logicalDevice.Device, ref framebufferCI, null, out _scFramebuffers[i]);
        }
    }

    private void CreateCommandPool()
    {
        VkCommandPoolCreateInfo commandPoolCI = VkCommandPoolCreateInfo.New();
        commandPoolCI.queueFamilyIndex = physicalDevice.QueueFamilyIndices.Graphics;
        vkCreateCommandPool(logicalDevice.Device, ref commandPoolCI, null, out _commandPool);
    }

    private void CreateTextureImage()
    {
        Image<Rgba32> image;
        using (var fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Textures", "texture.jpg")))
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

        vkGetImageSubresourceLayout(logicalDevice.Device, stagingImage, ref subresource, out VkSubresourceLayout stagingImageLayout);
        ulong rowPitch = stagingImageLayout.rowPitch;

        void* mappedPtr;
        vkMapMemory(logicalDevice.Device, stagingImageMemory, 0, imageSize, 0, &mappedPtr);
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
        vkUnmapMemory(logicalDevice.Device, stagingImageMemory);

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

        vkDestroyImage(logicalDevice.Device, stagingImage, null);
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

        vkCreateSampler(logicalDevice.Device, ref samplerCI, null, out _textureSampler);
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

        vkCreateImage(logicalDevice.Device, ref imageCI, null, out image);

        vkGetImageMemoryRequirements(logicalDevice.Device, image, out VkMemoryRequirements memRequirements);
        VkMemoryAllocateInfo allocInfo = VkMemoryAllocateInfo.New();
        allocInfo.allocationSize = memRequirements.size;
        allocInfo.memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties);
        vkAllocateMemory(logicalDevice.Device, ref allocInfo, null, out memory);

        vkBindImageMemory(logicalDevice.Device, image, memory, 0);
    }

    private void CreateCommandBuffers()
    {
        if (_commandBuffers.Count > 0)
        {
            vkFreeCommandBuffers(logicalDevice.Device, _commandPool, _scFramebuffers.Count, ref _commandBuffers[0]);
        }

        _commandBuffers.Resize(_scFramebuffers.Count);
        VkCommandBufferAllocateInfo commandBufferAI = VkCommandBufferAllocateInfo.New();
        commandBufferAI.commandPool = _commandPool;
        commandBufferAI.level = VkCommandBufferLevel.Primary;
        commandBufferAI.commandBufferCount = _commandBuffers.Count;
        vkAllocateCommandBuffers(logicalDevice.Device, ref commandBufferAI, out _commandBuffers[0]);

        for (uint i = 0; i < _commandBuffers.Count; i++)
        {
            VkCommandBuffer cb = _commandBuffers[i];

            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.SimultaneousUse;
            vkBeginCommandBuffer(cb, ref beginInfo);

            VkRenderPassBeginInfo rpbi = VkRenderPassBeginInfo.New();
            rpbi.renderPass = _renderPass;
            rpbi.framebuffer = _scFramebuffers[i];
            rpbi.renderArea.extent = vulkanSwapChain.extent;

            VkClearValue clearValue = new VkClearValue() { color = new VkClearColorValue() };
            rpbi.clearValueCount = 1;
            rpbi.pClearValues = &clearValue;

            vkCmdBeginRenderPass(cb, ref rpbi, VkSubpassContents.Inline);

            vkCmdBindPipeline(cb, VkPipelineBindPoint.Graphics, _graphicsPipeline);

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

            vkEndCommandBuffer(cb);
        }
    }

    private void CreateSemaphores()
    {
        VkSemaphoreCreateInfo semaphoreCI = VkSemaphoreCreateInfo.New();
        vkCreateSemaphore(logicalDevice.Device, ref semaphoreCI, null, out _imageAvailableSemaphore);
        vkCreateSemaphore(logicalDevice.Device, ref semaphoreCI, null, out _renderCompleteSemaphore);
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
        vkCreateDescriptorPool(logicalDevice.Device, ref descriptorPoolCI, null, out _descriptorPool);
    }

    private void CreateDescriptorSet()
    {
        VkDescriptorSetLayout dsl = _descriptoSetLayout;
        VkDescriptorSetAllocateInfo dsAI = VkDescriptorSetAllocateInfo.New();
        dsAI.descriptorPool = _descriptorPool;
        dsAI.pSetLayouts = &dsl;
        dsAI.descriptorSetCount = 1;
        vkAllocateDescriptorSets(logicalDevice.Device, ref dsAI, out _descriptorSet);

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

        vkUpdateDescriptorSets(logicalDevice.Device, descriptorWrites.Count, &descriptorWrites.First, 0, null);
    }

    private void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory memory)
    {
        VkBufferCreateInfo bufferCI = VkBufferCreateInfo.New();
        bufferCI.size = size;
        bufferCI.usage = usage;
        bufferCI.sharingMode = VkSharingMode.Exclusive;
        vkCreateBuffer(logicalDevice.Device, ref bufferCI, null, out buffer);

        vkGetBufferMemoryRequirements(logicalDevice.Device, buffer, out VkMemoryRequirements memReqs);
        VkMemoryAllocateInfo memAllocCI = VkMemoryAllocateInfo.New();
        memAllocCI.allocationSize = memReqs.size;
        memAllocCI.memoryTypeIndex = FindMemoryType(memReqs.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        vkAllocateMemory(logicalDevice.Device, ref memAllocCI, null, out memory);
        vkBindBufferMemory(logicalDevice.Device, buffer, memory, 0);
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
        vkMapMemory(logicalDevice.Device, memory, 0, size, 0, &mappedMemory);
        GCHandle gh = GCHandle.Alloc(data, GCHandleType.Pinned);
        Unsafe.CopyBlock(mappedMemory, gh.AddrOfPinnedObject().ToPointer(), (uint)size);
        gh.Free();
        vkUnmapMemory(logicalDevice.Device, memory);
    }

    private void UploadBufferData<T>(VkDeviceMemory memory, ref T data, uint count)
    {
        ulong size = (ulong)(count * Unsafe.SizeOf<T>());
        void* mappedMemory;
        vkMapMemory(logicalDevice.Device, memory, 0, size, 0, &mappedMemory);
        void* dataPtr = Unsafe.AsPointer(ref data);
        Unsafe.CopyBlock(mappedMemory, dataPtr, (uint)size);
        vkUnmapMemory(logicalDevice.Device, memory);
    }


    private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        vkGetPhysicalDeviceMemoryProperties(physicalDevice.PhysicalDevice, out VkPhysicalDeviceMemoryProperties memProperties);
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
        vkDeviceWaitIdle(logicalDevice.Device);

        //CreateSwapchain();
        //CreateImageViews();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandBuffers();
    }

    private VkShaderModule CreateShader(byte[] bytecode)
    {
        VkShaderModuleCreateInfo smci = VkShaderModuleCreateInfo.New();
        fixed (byte* byteCodePtr = bytecode)
        {
            smci.pCode = (uint*)byteCodePtr;
            smci.codeSize = new UIntPtr((uint)bytecode.Length);
            vkCreateShaderModule(logicalDevice.Device, ref smci, null, out VkShaderModule module);
            return module;
        }
    }

    private VkCommandBuffer BeginOneTimeCommands()
    {
        VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
        allocInfo.commandBufferCount = 1;
        allocInfo.commandPool = _commandPool;
        allocInfo.level = VkCommandBufferLevel.Primary;

        vkAllocateCommandBuffers(logicalDevice.Device, ref allocInfo, out VkCommandBuffer cb);

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

        vkQueueSubmit(logicalDevice.GraphicsQueue, 1, &submitInfo, VkFence.Null);
        vkQueueWaitIdle(logicalDevice.GraphicsQueue);

        vkFreeCommandBuffers(logicalDevice.Device, _commandPool, 1, ref cb);
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

        vkCreateImageView(logicalDevice.Device, ref imageViewCI, null, out imageView);
    }

    private static void CheckResult(VkResult result)
    {
        if (result != VkResult.Success)
        {
            Console.WriteLine($"Vulkan call was not successful: {result}");
        }
    }
}

public struct Vertex
{
    public Vector2 Position;
    public Vector3 Color;
    public Vector2 TexCoord;

    public static VkVertexInputBindingDescription GetBindingDescription()
    {
        VkVertexInputBindingDescription bindingDescription = new VkVertexInputBindingDescription();
        bindingDescription.inputRate = VkVertexInputRate.Vertex;
        bindingDescription.stride = (uint)Unsafe.SizeOf<Vertex>();
        bindingDescription.binding = 0;
        return bindingDescription;
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
