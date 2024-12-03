using System;
using System.Xml;
using SharpDX;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Assimp;

namespace Spark
{
    [AssetReader(".dae-anim")]
    public class AnimationReader : AssetReader<AnimationClip>
    {
        public float Scale = 1;

        private void ReadSkeleton(XmlElement root)
        {
            XmlNodeList scene = root.SelectNodes("/COLLADA/library_visual_scenes/visual_scene");
          
        }

        public class JointNode
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Sid { get; set; }
            public Matrix Matrix { get; set; }
            public List<JointNode> Children { get; set; } = new List<JointNode>();
        }

        public static JointNode ParseJointNode(XmlNode xmlNode)
        {
            JointNode joint = new JointNode();
            joint.Id = xmlNode.Attributes["id"]?.Value;
            joint.Name = xmlNode.Attributes["name"]?.Value;
            joint.Sid = xmlNode.Attributes["sid"]?.Value;

            foreach (XmlNode childNode in xmlNode.ChildNodes)
            {
                if (childNode.Name == "matrix")
                {
                    string matrixString = childNode.InnerText.Trim();
                    joint.Matrix = ParseMatrix(matrixString);
                }

                if (childNode.Name == "node" && childNode.Attributes["type"]?.Value == "JOINT")
                {
                    JointNode childJoint = ParseJointNode(childNode);
                    joint.Children.Add(childJoint);
                }
            }

            return joint;
        }

        private static Matrix ParseMatrix(string matrixString)
        {
            if (string.IsNullOrEmpty(matrixString)) return Matrix.Identity;

            var values = matrixString.Split(' ').Select(float.Parse).ToArray();
            return new Matrix(
                values[0], values[1], values[2], values[3],
                values[4], values[5], values[6], values[7],
                values[8], values[9], values[10], values[11],
                values[12], values[13], values[14], values[15]
            );
        }
        public static List<JointNode> ExtractJointNodes(XmlNode rootNode)
        {
            List<JointNode> joints = new List<JointNode>();
            XmlNode sceneNode = rootNode.SelectSingleNode("/COLLADA/library_visual_scenes/visual_scene");

            foreach (XmlNode node in sceneNode.ChildNodes)
            {
                if (node.Name == "node" && node.Attributes["type"]?.Value == "JOINT")
                {
                    JointNode joint = ParseJointNode(node);
                    joints.Add(joint);
                }
            }

            return joints;
        }

        public override AnimationClip Import(string filename)
        {
            float scaleFactor = Scale;
            if (scaleFactor <= 0) scaleFactor = 1;

            // Namespaces are handled wrongly by XPath 1.0 and also we don't need
            // them anyway, so all namespaces are simply removed
            string xmlWithoutNamespaces = Regex.Replace(File.ReadAllText(filename), @"xmlns="".+?""", "");

            var doc = new XmlDocument();
            doc.LoadXml(xmlWithoutNamespaces);

            var root = doc.DocumentElement;

            // Next import animations from library_animations
            XmlNodeList xmlAnimations = root.SelectNodes("/COLLADA/library_animations/animation");
            AnimationClip result = new AnimationClip(); ;
            float duration = 0;

            foreach (XmlNode xmlAnim in xmlAnimations)
            {
                XmlNodeList xmlChannels = xmlAnim.SelectNodes(".//channel");

                foreach (XmlNode xmlChannel in xmlChannels)
                {
                    // Target joint
                    string target = xmlChannel.Attributes["target"].Value;
                    string jointId = ExtractNodeIdFromTarget(target);
                    string targetType = target.Substring(target.IndexOf('/') + 1);

                    // Sampler
                    string samplerId = xmlChannel.Attributes["source"].Value.Substring(1);
                    XmlNode xmlSampler = xmlAnim.SelectSingleNode("//sampler[@id='" + samplerId + "']");

                    // Input and Output sources
                    ColladaSource input = ColladaSource.FromInput(xmlSampler.SelectSingleNode("input[@semantic='INPUT']"), xmlAnim);
                    ColladaSource output = ColladaSource.FromInput(xmlSampler.SelectSingleNode("input[@semantic='OUTPUT']"), xmlAnim);

                    // It is assumed that TIME is used for input
                    var times = input.GetData<float>();
                    var channel = new AnimationChannel { Target = jointId };
                    duration = Math.Max(duration, times[times.Count - 1]);

                    if (targetType == "matrix")
                    {
                        // Baked matrices were used so that there is one animation per joint animating
                        // the whole transform matrix. Now we just need to save these transforms in keyframes
                        var transforms = output.GetData<Matrix>();
                        Debug.Assert(transforms.Count == times.Count);

                        var positions = new List<VectorKey>();
                        var scales = new List<VectorKey>();
                        var rotations = new List<QuaternionKey>();

                        for (int i = 0; i < times.Count; i++)
                        {
                            var transform = transforms[i];
                            // transform = transform * Matrix.Scaling(1, 1, -1);


                            transform.Decompose(out var scale, out var rotate, out var translate);
                            translate.Z *= -1;
                            rotate.X *= -1;
                            rotate.Y *= -1;

                            scales.Add(new VectorKey { Time = times[i], Value = scale });
                            positions.Add(new VectorKey { Time = times[i], Value = translate * scaleFactor });
                            rotations.Add(new QuaternionKey { Time = times[i], Value = rotate });
                            //keyframes[i] = new JointAnimationKeyFrame(times[i], transforms[i]);
                        }

                        channel.Position = new VectorKeys(positions);
                        channel.Scale = new VectorKeys(scales);
                        channel.Rotation = new QuaternionKeys(rotations);

                        result.AddChannel(channel);
                    }
                    else if (output.DataType == typeof(float))
                    {
                        // No baked matrices, i.e. there is an animation for each component of the transform
                        var data = output.GetData<float>();
                        Debug.Assert(times.Count == data.Count);

                        //for (int i = 0; i < times.Count; i++)
                        //{
                        //    keyframes[i] = CreateKeyFrameFromSingle(target, data[i], times[i]);
                        //}
                    }
                    else if (output.DataType == typeof(Vector3))
                    {
                        var data = output.GetData<Vector3>();
                        Debug.Assert(times.Count == data.Count);

                        //for (int i = 0; i < times.Count; i++)
                        //{
                        //    keyframes[i] = CreateKeyFrameFromVector(target, data[i], times[i]);
                        //}
                    }
                }
            }

            //// Import the animation clips library
            result.Duration = duration;// (float)(duration - 1) / 30;// 1.033f;// duration;
            result.TicksPerSecond = 1;// 30;

            var rootJoint = ExtractJointNodes(root);
            return result;
        }

        string ExtractNodeIdFromTarget(string target)
        {
            return target.Substring(0, target.IndexOf('/'));
        }
    }
}
