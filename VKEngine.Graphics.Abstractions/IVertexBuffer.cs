namespace VKEngine.Graphics;

public interface IVertexBufferFactory
{
    IVertexBuffer CreateVertexBuffer();
}

public interface IVertexBuffer
{
    void Initialize();
    void Cleanup();

    void SetData();
}
