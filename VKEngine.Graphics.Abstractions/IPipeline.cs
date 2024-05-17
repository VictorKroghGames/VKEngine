namespace VKEngine.Graphics;

public interface IPipelineFactory
{
    IPipeline CreateGraphicsPipeline(PipelineDescription pipelineDescription);
}

public interface IPipeline
{
    void Cleanup();

    void Bind();
    void Unbind();
}
