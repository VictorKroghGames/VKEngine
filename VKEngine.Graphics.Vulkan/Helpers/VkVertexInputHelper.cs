using Vulkan;

namespace VKEngine.Graphics.Vulkan.Helpers;

internal static class VkVertexInputHelper
{
    internal static IEnumerable<(VkVertexInputBindingDescription VertexInputBindingDescription, VkVertexInputAttributeDescription[] VertexInputAttributeDescriptions)> GetVertexInputBindingAndAttributeDescriptions(VertexLayout[] vertexLayouts)
    {
        for (uint i = 0; i < vertexLayouts.Length; i++)
        {
            var vertexLayout = vertexLayouts[i];
            if (vertexLayout.Attributes.Length == 0)
            {
                throw new InvalidOperationException("Vertex layout must have at least one attribute!");
            }

            var vertexInputAttributeDescriptions = new VkVertexInputAttributeDescription[vertexLayout.Attributes.Length];

            var offset = 0u;
            for (uint j = 0; j < vertexLayout.Attributes.Length; j++)
            {
                vertexInputAttributeDescriptions[j] = new VkVertexInputAttributeDescription
                {
                    binding = i,
                    location = (uint)j,
                    format = (VkFormat)vertexLayout.Attributes[j].Format,
                    offset = offset
                };

                // TODO: refactor this
                var componentSize = vertexLayout.Attributes[j].Format switch
                {
                    Format.R32Sfloat => 1u,
                    Format.R32g32Sfloat => 2u,
                    Format.R32g32b32Sfloat => 3u,
                    Format.R32g32b32a32Sfloat => 4u,
                    _ => throw new InvalidOperationException("Invalid format!")
                };

                offset += componentSize * sizeof(float);
            }

            var vertexInputBindingDescription = new VkVertexInputBindingDescription();
            vertexInputBindingDescription.binding = i;
            vertexInputBindingDescription.stride = offset;
            vertexInputBindingDescription.inputRate = (VkVertexInputRate)vertexLayout.InputRate;

            yield return (vertexInputBindingDescription, vertexInputAttributeDescriptions);
        }
    }
}
