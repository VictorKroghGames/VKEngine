namespace VKEngine.Graphics.Vulkan.Native;

public struct FixedArray2<T> where T : struct
{
    public T First;
    public T Second;

    public FixedArray2(T first, T second) { First = first; Second = second; }

    public uint Count => 2;
}

public struct FixedArray3<T> where T : struct
{
    public T First;
    public T Second;
    public T Third;

    public FixedArray3(T first, T second, T third) { First = first; Second = second; Third = third; }

    public uint Count => 3;
}