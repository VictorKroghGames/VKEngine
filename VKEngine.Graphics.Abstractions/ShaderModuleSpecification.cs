namespace VKEngine.Graphics;

public struct ShaderModuleSpecification
{
    public string FilePath { get; }
    public ShaderModuleType Type { get; }
    public string MainFunctionIdendifier { get; }

    public ShaderModuleSpecification(string filePath, ShaderModuleType type, string mainFunctionIdentifier = "main")
    {
        FilePath = filePath;
        Type = type;
        MainFunctionIdendifier = mainFunctionIdentifier;
    }
}
