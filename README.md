# Spark
C# DirectX Game Engine

Totally raw and unfiltered.

A lot of code files aren't even used - need to clean up.

Took inspiration from both Unity and Unreal.


Some highlights:
- Deferred renderer (directional and point lights)
- Customizable render pipeline
- Aggressive use of instancing
- Skeletal animation (matrix palette skinning) with blendtrees
- Simple mesh LOD group system (viewspace size)
- Quadtree terrain with LOD and 16 splatmaps
- Cascaded shadow map for single directional light using TextureArray (4 cascades, working but unfinished)
- Icoseptree spatialization for culling, raycasts and shape queries
- GeometryShader for foilage (booh, MeshShaders next)
- Multithreaded, using all available cores where possible 
- CommandBuffer pattern to minimize state switching and efficient DrawCall handling
- Unity-esque components, script execution and Asset system
- Unity-esque extendable editor with custom inspectors etc.
- Editor UI created with Squid - the best example for complex Squid UIs


DirectX Bindings: https://github.com/sharpdx/SharpDX

Asset imports: https://github.com/assimp/assimp-net

Physics: https://github.com/notgiven688/jitterphysics

GUI: https://github.com/Roderik11/Squid


Future plans:
- Graphics API: either WebGPU or Vortice.Windows
- Physics: Jitter2
- "Bindless" approach
- GPU frustum and occlusion culling
- Forward+ renderer
- ECS with strict separation of components and systems
- Asset packaging and Executable export from Editor

![image](https://github.com/Roderik11/Spark/assets/5743257/013eb957-242b-48b6-920d-29903a9a5e17)
![image](https://github.com/Roderik11/Spark/assets/5743257/2b572531-5812-4003-8f90-2b4150faabbf)
