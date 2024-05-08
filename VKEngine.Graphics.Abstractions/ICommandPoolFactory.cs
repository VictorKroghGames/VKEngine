namespace VKEngine.Graphics;

public interface ICommandPoolFactory
{
    ICommandPool CreateCommandPool();
    ICommandPool CreateCommandPool(uint queueFamilyIndex);
}
