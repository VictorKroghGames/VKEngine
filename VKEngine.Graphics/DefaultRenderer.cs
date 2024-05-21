using VKEngine.Configuration;

namespace VKEngine.Graphics;

internal class DefaultRenderer(IGraphicsConfiguration graphicsConfiguration, IGraphicsContext graphicsContext, ISwapChain swapChain, ICommandBufferAllocator commandBufferAllocator) : IRenderer
{
    private ICommandBuffer[] commandBuffers = [];

    private ICommandBuffer currentFrameCommandBuffer = default!;

    public void Initialize()
    {
        graphicsContext.Initialize();
        commandBufferAllocator.Initialize();

        //commandBuffers = new ICommandBuffer[graphicsConfiguration.FramesInFlight];
        //for (int i = 0; i < graphicsConfiguration.FramesInFlight; i++)
        //{
        //    commandBuffers[i] = commandBufferAllocator.AllocateCommandBuffer();
        //}

        //shaderLibrary.Load("shader",
        //    new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.vert.spv"), ShaderModuleType.Vertex),
        //    new ShaderModuleSpecification(Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.frag.spv"), ShaderModuleType.Fragment)
        //);
    }

    public void Cleanup()
    {
        commandBufferAllocator.Cleanup();
        graphicsContext.Cleanup();
    }

    public void BeginFrame()
    {
        swapChain.AquireNextImage();

        currentFrameCommandBuffer = commandBuffers[swapChain.CurrentFrameIndex];
        currentFrameCommandBuffer.Begin();
    }

    public void EndFrame()
    {
        currentFrameCommandBuffer.End();
    }

    public void Render()
    {
        currentFrameCommandBuffer.Submit();
    }

    public void Wait()
    {
        graphicsContext.Wait();
    }

    public void Draw(IRenderPass renderPass, IPipeline pipeline, IBuffer vertexBuffer, IBuffer indexBuffer, IDescriptorSet descriptorSet)
    {
        currentFrameCommandBuffer.BeginRenderPass(renderPass);

        currentFrameCommandBuffer.BindPipeline(pipeline);

        currentFrameCommandBuffer.BindVertexBuffer(vertexBuffer);

        currentFrameCommandBuffer.BindIndexBuffer(indexBuffer);

        currentFrameCommandBuffer.BindDescriptorSet(pipeline, descriptorSet);

        currentFrameCommandBuffer.DrawIndex(6);

        currentFrameCommandBuffer.EndRenderPass();
    }
}
