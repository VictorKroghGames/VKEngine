namespace VKEngine.Platform;

public interface IInput
{
    bool IsKeyReleased(KeyCodes key);
    bool IsKeyPressed(KeyCodes key);
}