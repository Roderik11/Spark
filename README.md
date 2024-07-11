# Spark
C# Game Engine

Totally raw and unfiltered.

A lot of code files aren't even used - need to clean up.

Took inspiration from both Unity and Unreal.


Some highlights:
- customizable render pipeline
- agressive use of instancing
- skeletal animation (matrix palette skinning) with blendtrees
- Icoseptree spatialization for culling and shape queries
- GeometryShader for foilage (booh, MeshShaders next)
- multithreaded, using all available cores where possible 
- CommandBuffer pattern to minimize state switching and efficient DrawCall handling
- Unity-esque components and script execution
- Unity-esque customizable editor
- Editor UI created with Squid - probably the best example for complex UIs


DirectX Bindings: https://github.com/sharpdx/SharpDX

Asset imports: https://github.com/assimp/assimp-net

Physics: https://github.com/notgiven688/jitterphysics

GUI: https://github.com/Roderik11/Squid


Future plans:
- Graphics API: either WebGPU or Vortice.Windows
- Physics: Jitter2 

![image](https://github.com/Roderik11/Spark/assets/5743257/013eb957-242b-48b6-920d-29903a9a5e17)
![image](https://github.com/Roderik11/Spark/assets/5743257/2b572531-5812-4003-8f90-2b4150faabbf)
