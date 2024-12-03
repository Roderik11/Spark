using System;
using System.Collections.Generic;
using System.IO;
using Assimp;
using SharpDX;

namespace Spark
{
    [AssetReader(".smesh")]
    public class MeshPackReader : AssetReader<Mesh>
    {
        public override Mesh Import(string filename)
        {
            FileStream filestream = File.OpenRead(filename);
            BinaryReader reader = new BinaryReader(filestream);
            var packer = new MeshPacker();
            var mesh = packer.Unpack(reader);
            filestream.Close();
            return mesh;
        }
    }

    [AssetReader(".obj")]
    public class OBJReader : AssetReader<Mesh>
    {
        public float Scale = 1;
        public bool CalculateTangents = true;

        public override Mesh Import(string filename)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            string data = File.ReadAllText(filename);

            string[] lines = data.Split(new string[2] { Environment.NewLine, "\n" }, StringSplitOptions.RemoveEmptyEntries);

            List<Vector3> vertex_positions = new List<Vector3>();
            List<Vector2> vertex_uvs = new List<Vector2>();
            List<Vector3> vertex_normals = new List<Vector3>();

            List<Vector3> vertexList = new List<Vector3>();
            List<Vector3> normalList = new List<Vector3>();
            List<Vector2> uvList = new List<Vector2>();

            List<int> indexList = new List<int>();

            List<MeshPart> parts = new List<MeshPart>();
            MeshPart part = new MeshPart();
            parts.Add(part);

            int newvertices = 0;
            int faces = 0;
            int baseIndex = 0;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            Dictionary<string, int> hashmap = new Dictionary<string, int>();


            foreach (string line in lines)
            {
                string[] cl = line.Split(new string[1] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (line.StartsWith("v ")) // vertex position
                {
                    Vector3 pos = new Vector3(float.Parse(cl[1]), float.Parse(cl[2]), float.Parse(cl[3])) * Scale;
                    vertex_positions.Add(pos);

                    min = Vector3.Min(min, pos);
                    max = Vector3.Max(max, pos);
                }

                if (line.StartsWith("vn ")) // vertex normal
                    vertex_normals.Add(new Vector3(float.Parse(cl[1]), float.Parse(cl[2]), float.Parse(cl[3])));

                if (line.StartsWith("vt ")) // vertex texture
                {
                    Vector2 vtx = Vector2.Zero;
                    vtx.X = float.Parse(cl[1]);
                    vtx.Y = float.Parse(cl[2]);
                    // if (cl.Length > 3) vtx.Z = float.Parse(cl[3]);
                    vertex_uvs.Add(vtx);
                }

                // new group
                if (line.StartsWith("g "))
                {
                    //int startindex = 0;
                    //if (part != null)
                        baseIndex += part.NumIndices;

                    part = new MeshPart();
                    part.BaseIndex = baseIndex;
                    part.Name = line.Remove(0, 2);
                    parts.Add(part);
                }

                if (line.StartsWith("f ")) // face
                {
                    int num = cl.Length - 1;
                    List<string> verthash = new List<string>();

                    foreach (string k in cl)
                    {
                        if (k == "f") continue;

                        string[] tri = k.Split(new string[1] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                        int pv = int.Parse(tri[0]) - 1;
                        int pvt = int.Parse(tri[1]) - 1;
                        int pvn = int.Parse(tri[2]) - 1;

                        string hash = string.Format("{0}_{1}_{2}", pv, pvt, pvn);
                        verthash.Add(hash);

                        if (!hashmap.ContainsKey(hash))
                        {
                            hashmap.Add(hash, newvertices);

                            vertexList.Add(vertex_positions[pv]);
                            normalList.Add(vertex_normals[pv]);
                            uvList.Add(vertex_uvs[pvt]);
                            newvertices++;
                        }
                    }

                    // 3 vertices
                    if (num == 3)
                    {
                        indexList.Add(hashmap[verthash[2]]);
                        indexList.Add(hashmap[verthash[0]]);
                        indexList.Add(hashmap[verthash[1]]);
                        
                        part.NumIndices += 3;
                        faces++;
                    }

                    // 4 vertices
                    if (num == 4)
                    {
                        indexList.Add(hashmap[verthash[2]]);
                        indexList.Add(hashmap[verthash[3]]);
                        indexList.Add(hashmap[verthash[0]]);

                        indexList.Add(hashmap[verthash[2]]);
                        indexList.Add(hashmap[verthash[0]]);
                        indexList.Add(hashmap[verthash[1]]);

                        part.NumIndices += 6;
                        faces += 2;
                    }

                    // more than 4 vertices
                    if (num > 4)
                    {
                        for (int r = 0; r < num - 2; r++)
                        {
                            indexList.Add(hashmap[verthash[0]]);
                            indexList.Add(hashmap[verthash[r + 1]]);
                            indexList.Add(hashmap[verthash[r + 2]]);

                            part.NumIndices += 3;
                            faces++;
                        }
                    }
                }
            }

            
            var indices = indexList.ToArray();
            var verts = vertexList.ToArray();
            var normals = normalList.ToArray();
            var uvs = uvList.ToArray();
            var tangents = new Vector3[vertexList.Count];
            var binormals = new Vector3[vertexList.Count];

            if(CalculateTangents)
                Geometry.TangentBinormal(indices, verts, tangents, binormals, uvs);

            for (int i = parts.Count - 1; i >= 0; i--)
            {
                if (parts[i].NumIndices <= 0)
                    parts.RemoveAt(i);
            }
           
            var mesh = new Mesh
            {
                Name = Path.GetFileNameWithoutExtension(filename),
                MeshParts = parts,
                Indices = indices,
                Vertices = verts,
                Normals = normals,
                Tangents = tangents,
                BiNormals = binormals,
                UV = uvs,
                BoundingBox = new SharpDX.BoundingBox(min, max),
            };

            return mesh;
        }
    }

    [AssetReader(".x", ".dae", ".fbx")]
    public class AssimpReader : AssetReader<Mesh>
    {
        public bool ConvertUnits = true;
        public bool Optimize = false;
        public bool LeftHanded = true;
        public bool FlipUVs = false;
        public bool FlipWinding = true;
        public bool CalculateTangents = true;
        [ValueRange(0, 180)]
        public float SmoothingAngle = 66f;
        public float Scale = 1;

        private List<Bone> skeleton = new List<Bone>();
        private List<string> boneNames = new List<string>();
        private Dictionary<string, bool> validNodes = new Dictionary<string, bool>();
        private Dictionary<string, Assimp.Node> nodes = new Dictionary<string, Assimp.Node>();
        private Dictionary<string, Assimp.Bone> bones = new Dictionary<string, Assimp.Bone>();

        private void FindBones(Assimp.Node node)
        {
            if (!string.IsNullOrEmpty(node.Name))
            {
                nodes.Add(node.Name, node);
                validNodes.Add(node.Name, false);
            }

            if (!node.HasChildren) return;

            foreach (Assimp.Node child in node.Children)
                FindBones(child);
        }

        private void FlattenHierarchy(Assimp.Node node)
        {
            if (!validNodes.ContainsKey(node.Name)) return;
            if (validNodes[node.Name] == false) return;

            boneNames.Add(node.Name);

            Bone newBone = new Bone { Name = node.Name };

            Matrix bind = node.Transform.AsMatrix();
            //bind.TranslationVector *= TotalScale;
            bind.Decompose(out var scale, out var rotate, out var translate);

            newBone.BindPoseMatrix = bind;
            var q = SharpDX.Quaternion.Identity;

            newBone.BindPose = new BonePose { Position = translate, Rotation = rotate, Scale = scale };
            newBone.Parent = node.Parent != null ? boneNames.IndexOf(node.Parent.Name) : -1;
            skeleton.Add(newBone);

            if (bones.ContainsKey(node.Name))
            {
                newBone.Parent = node.Parent != null ? boneNames.IndexOf(node.Parent.Name) : -1;
                newBone.OffsetMatrix = bones[node.Name].OffsetMatrix.AsMatrix();
                //newBone.OffsetMatrix.TranslationVector *= TotalScale;
            }
            else
            {
                newBone.OffsetMatrix = Matrix.Identity;
            }

            if (node.HasChildren)
            {
                foreach (Assimp.Node child in node.Children)
                    FlattenHierarchy(child);
            }
        }

        private void ValidateBones(Assimp.Scene scene)
        {
            int meshIndex = 0;

            foreach (Assimp.Mesh mesh in scene.Meshes)
            {
                if (!mesh.HasBones) continue;

                foreach (Assimp.Bone bone in mesh.Bones)
                {
                    if (!nodes.ContainsKey(bone.Name))
                        continue;

                    if (bones.ContainsKey(bone.Name)) continue;

                    validNodes[bone.Name] = true;

                    bones.Add(bone.Name, bone);

                    Assimp.Node node = nodes[bone.Name];

                    while (node.Parent != null)
                    {
                        node = node.Parent;

                        if (node.HasMeshes && node.MeshIndices.Contains(meshIndex))
                            break;

                        if (validNodes.ContainsKey(node.Name))
                            validNodes[node.Name] = true;
                    }
                }

                meshIndex++;
            }
        }

        private void GenerateSkeleton(Assimp.Scene scene)
        {
            FindBones(scene.RootNode);
            ValidateBones(scene);
            FlattenHierarchy(scene.RootNode);
        }

        private void ReadAnimations(Assimp.Scene scene, Mesh mesh)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            if (scene.Animations == null) return;

            //var totalScale = TotalScale;
            foreach (Assimp.Animation anim in scene.Animations)
            {
                AnimationClip clip = new AnimationClip { Name = anim.Name };
                clip.Duration = (float)anim.DurationInTicks;
                clip.TicksPerSecond = (float)anim.TicksPerSecond;
                if (clip.TicksPerSecond <= 0) clip.TicksPerSecond = 25;

                clips.Add(clip);

                foreach (Assimp.NodeAnimationChannel chan in anim.NodeAnimationChannels)
                {
                    AnimationChannel channel = new AnimationChannel { Target = chan.NodeName };
                    var positions = new List<VectorKey>();
                    var scales = new List<VectorKey>();
                    var rotations = new List<QuaternionKey>();

                    for (int i = 0; i < chan.PositionKeys.Count; i++)
                    {
                        if (i == 0 || chan.PositionKeys[i].Time > channel.Position.Last().Time)
                            positions.Add(new VectorKey { Time = (float)chan.PositionKeys[i].Time, Value = new Vector3(chan.PositionKeys[i].Value.X, chan.PositionKeys[i].Value.Y, chan.PositionKeys[i].Value.Z) });
                    }

                    for (int i = 0; i < chan.ScalingKeys.Count; i++)
                    {
                        if (i == 0 || chan.ScalingKeys[i].Time > channel.Scale.Last().Time)
                            scales.Add(new VectorKey { Time = (float)chan.ScalingKeys[i].Time, Value = new Vector3(chan.ScalingKeys[i].Value.X, chan.ScalingKeys[i].Value.Y, chan.ScalingKeys[i].Value.Z) });
                    }

                    for (int i = 0; i < chan.RotationKeys.Count; i++)
                    {
                        if (i == 0 || chan.RotationKeys[i].Time > channel.Rotation.Last().Time)
                            rotations.Add(new QuaternionKey { Time = (float)chan.RotationKeys[i].Time, Value = new SharpDX.Quaternion(chan.RotationKeys[i].Value.X, chan.RotationKeys[i].Value.Y, chan.RotationKeys[i].Value.Z, chan.RotationKeys[i].Value.W) });
                    }


                    channel.Position = new VectorKeys(positions);
                    channel.Scale = new VectorKeys(scales);
                    channel.Rotation = new QuaternionKeys(rotations);

                    clip.AddChannel(channel);
                }
            }

            // mesh.Animations = clips.ToArray();
        }

        public override Mesh Import(string filename)
        {
            var importer = new AssimpContext();

            Assimp.Configs.NormalSmoothingAngleConfig config = new Assimp.Configs.NormalSmoothingAngleConfig(SmoothingAngle);
            Assimp.Configs.GlobalScaleConfig globalScale = new Assimp.Configs.GlobalScaleConfig(Scale);
            Assimp.Configs.KeepSceneHierarchyConfig keepSceneHierarchy = new Assimp.Configs.KeepSceneHierarchyConfig(true);
            Assimp.Configs.FBXConvertToMetersConfig convertUnits = new Assimp.Configs.FBXConvertToMetersConfig(true);
           
            importer.SetConfig(config);
            importer.SetConfig(keepSceneHierarchy);
            importer.SetConfig(globalScale);
           
            //if (ConvertUnits) importer.SetConfig(convertUnits);

            var flags = PostProcessPreset.TargetRealTimeMaximumQuality;
            if (FlipWinding) flags |= PostProcessSteps.FlipWindingOrder;
            if (FlipUVs) flags |= PostProcessSteps.FlipUVs;
            if (LeftHanded) flags |= PostProcessSteps.MakeLeftHanded;
            if (CalculateTangents) flags |= PostProcessSteps.CalculateTangentSpace;

            //flags |= PostProcessSteps.PreTransformVertices;
            flags |= PostProcessSteps.GlobalScale;
            flags &= ~PostProcessSteps.FindInstances;
            flags &= ~PostProcessSteps.FindDegenerates;
            flags &= ~PostProcessSteps.SplitLargeMeshes;
            flags &= ~PostProcessSteps.RemoveRedundantMaterials;
            flags &= ~PostProcessSteps.OptimizeMeshes;
            //if (Optimize) flags |= PostProcessSteps.OptimizeGraph | PostProcessSteps.OptimizeMeshes;

            Assimp.Scene scene = importer.ImportFile(filename, flags);

            int baseVertex = 0;
            int baseIndex = 0;

            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            List<int> indexbuff = new List<int>();
            List<Vector3> vertexbuff = new List<Vector3>();
            List<Vector3> normalbuff = new List<Vector3>();
            List<Vector2> uvbuff = new List<Vector2>();
            List<Vector3> tangentbuff = new List<Vector3>();
            List<Vector3> birnomalbuff = new List<Vector3>();

            List<Dictionary<int, float>> weightMap = new List<Dictionary<int, float>>();

            Mesh result = new Mesh();

            // generate bones for animation
            GenerateSkeleton(scene);

            if (skeleton.Count > 0)
                result.Bones = skeleton.ToArray();

            var meshes = new List<Assimp.Mesh>(scene.Meshes);
            meshes.Reverse();

            foreach (Assimp.Mesh mesh in meshes)
            {
                int[] indices = mesh.GetIndices();
                
                MeshPart part = new MeshPart();
                part.Name = mesh.Name;
                part.NumIndices = indices.Length;
                part.BaseVertex = baseVertex;
                part.BaseIndex = baseIndex;
                result.MeshParts.Add(part);

                bool hasTangents = mesh.Tangents != null;
                bool hasBinormals = mesh.BiTangents != null;
                var uvcoords = mesh.TextureCoordinateChannels[0];
                int uvcount = uvcoords.Count;

                // add vertices
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var position = mesh.Vertices[i].AsVector3();
                    var uv = i < uvcount ? uvcoords[i].AsVector2() : Vector2.Zero;
                    var normal =  mesh.Normals[i].AsVector3();

                    if (hasTangents && mesh.Tangents.Count > i)
                        tangentbuff.Add(mesh.Tangents[i].AsVector3());

                    if (hasBinormals && mesh.BiTangents.Count > i)
                        birnomalbuff.Add(mesh.BiTangents[i].AsVector3());

                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);

                    vertexbuff.Add(position);
                    normalbuff.Add(normal);
                    uvbuff.Add(uv);

                    // insert an entry for bone weights
                    weightMap.Add(new Dictionary<int, float>());
                }

                // indices WITHOUT baseVertex offset (start at zero)
                indexbuff.AddRange(indices);

                // indices WITH baseVertex offset (start at baseVertex)
                //foreach (Assimp.Face face in mesh.Faces)
                //{
                //    foreach (uint index in face.Indices)
                //        indexbuff.Add(baseVertex + (int)index);
                //}

                // store bone weights
                if (result.Bones != null && mesh.HasBones)
                {
                    foreach (Assimp.Bone bone in mesh.Bones)
                    {
                        if (bone.HasVertexWeights)
                        {
                            int boneIndex = boneNames.IndexOf(bone.Name);
                            foreach (Assimp.VertexWeight vw in bone.VertexWeights)
                            {
                                int vertexid = baseVertex + (int)vw.VertexID;
                                if (weightMap[vertexid].Count < 4 && !weightMap[vertexid].ContainsKey(boneIndex))
                                    weightMap[vertexid].Add(boneIndex, vw.Weight);
                            }
                        }
                    }
                }

                baseVertex += mesh.Vertices.Count;
                baseIndex += indices.Length;
            }

            var vertices = vertexbuff.ToArray();
            var normals = normalbuff.ToArray();
            var uvs = uvbuff.ToArray();
            var tangents = tangentbuff.ToArray();
            var binormals = birnomalbuff.ToArray();

            result.MeshParts.Sort((a, b) => a.Name.CompareTo(b.Name));
            result.Vertices = vertices;
            result.Normals = normals;
            result.BiNormals = binormals;
            result.UV = uvs;
            result.Indices = indexbuff.ToArray();
            result.BoundingBox = new SharpDX.BoundingBox(min, max);

            if (result.Bones != null)
            {
                // add bone weights
                int id = 0;
                BoneWeight[] weights = new BoneWeight[weightMap.Count];
                foreach (var map in weightMap)
                {
                    BoneWeight weight = new BoneWeight();

                    int j = 0;

                    foreach (var pair in map)
                    {
                        weight.BoneIDs[j] = pair.Key;
                        weight.Weights[j] = pair.Value;

                        j++;
                    }

                    weights[id] = weight;

                    id++;
                }

                if (weights.Length > 0)
                    result.Boneweights = weights;
                
                // ReadAnimations(scene, result);
            
            }

            return result;
        }
    }
}