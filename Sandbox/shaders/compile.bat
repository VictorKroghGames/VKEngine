echo Compiling shaders...

glslangvalidator -V %~dp0khronos_vulkan_tutorial.vert -o khronos_vulkan_tutorial.vert.spv
glslangvalidator -V %~dp0khronos_vulkan_tutorial.frag -o khronos_vulkan_tutorial.frag.spv

pause