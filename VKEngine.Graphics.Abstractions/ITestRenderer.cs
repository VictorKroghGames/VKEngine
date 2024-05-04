namespace VKEngine.Graphics;

public interface ITestRenderer
{
    void Initialize();
    void Cleanup();
    void RenderTriangle();
    void RecordCommandBuffer(uint i);
}