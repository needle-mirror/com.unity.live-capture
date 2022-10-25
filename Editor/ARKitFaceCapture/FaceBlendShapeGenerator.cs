using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Unity.LiveCapture.ARKitFaceCapture.Editor
{
    /// <summary>
    /// Generates the fields for <see cref="FaceBlendShapePose"/> from each enum value in <see cref="FaceBlendShape"/>.
    /// </summary>
    class FaceBlendShapeGenerator : AssetPostprocessor
    {
        /// <summary>
        /// The file that when modified/imported will trigger file generation.
        /// </summary>
        static readonly string k_TriggerFile = $"{nameof(FaceBlendShapePose)}.cs";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
            {
                if (asset.EndsWith(k_TriggerFile))
                {
                    var sourceFile = new FileInfo(asset);
                    var outputDir = sourceFile.Directory.FullName;
                    Generate(outputDir);
                }
            }
        }

        static void Generate(string outputDirectory)
        {
            var generator = new StringBuilder();

            generator.AppendLine(
                $@"// <auto-generated>
// This file is generated by {nameof(FaceBlendShapeGenerator)}, do not modify manually
using System;
using UnityEngine;

namespace {typeof(FaceBlendShapePose).Namespace}
{{
    partial struct {nameof(FaceBlendShapePose)}
    {{
        /// <summary>
        /// The number of supported blend shapes.
        /// </summary>
        public const int ShapeCount = {FaceBlendShapePose.Shapes.Length};
");

            foreach (var name in FaceBlendShapePose.Shapes)
            {
                generator.AppendLine($"        /// <inheritdoc cref=\"{nameof(FaceBlendShape)}.{name}\"/>");
                generator.AppendLine($"        [Range(0f, 1f)] public float {name};");
            }

            generator.AppendLine($@"
        float GetValue(int index)
        {{
            switch (index)
            {{");

            foreach (var name in FaceBlendShapePose.Shapes)
            {
                generator.AppendLine($"                case {(int)name}: return {name};");
            }

            generator.AppendLine(
                $@"            }}
            throw new IndexOutOfRangeException($""Blend shape index {{index}} out of valid range [0, {{ShapeCount}}]."");
        }}

        void SetValue(int index, float value)
        {{
            switch (index)
            {{");

            foreach (var name in FaceBlendShapePose.Shapes)
            {
                generator.AppendLine($"                case {(int)name}: {name} = value; return;");
            }

            generator.AppendLine(
                $@"            }}
            throw new IndexOutOfRangeException($""Blend shape index {{index}} out of valid range [0, {{ShapeCount}}]."");
        }}

        /// <summary>
        /// Horizontally mirrors the face pose.
        /// </summary>
        /// <remarks>
        /// ARKit's default blend shapes are set so that 'right' indicates the right side of the face when viewing from the front.
        /// </remarks>
        public void FlipHorizontally()
        {{");

            foreach (var name in FaceBlendShapePose.Shapes)
            {
                var str = name.ToString();

                if (str.Contains("Left"))
                {
                    generator.AppendLine($"            var temp{name} = {name};");
                }
                else if (str.Contains("Right"))
                {
                    generator.AppendLine($"            var temp{name} = {name};");
                }
            }
            foreach (var name in FaceBlendShapePose.Shapes)
            {
                var str = name.ToString();

                if (str.Contains("Left"))
                {
                    generator.AppendLine($"            {name} = temp{Enum.Parse(typeof(FaceBlendShape), str.Replace("Left", "Right"))};");
                }
                else if (str.Contains("Right"))
                {
                    generator.AppendLine($"            {name} = temp{Enum.Parse(typeof(FaceBlendShape), str.Replace("Right", "Left"))};");
                }
            }

            generator.AppendLine(
                $@"        }}

        /// <summary>
        /// Linearly interpolates between <paramref name=""a""/> and <paramref name=""b""/> by factor <paramref name=""t""/>.
        /// </summary>
        /// <remarks><br/>
        /// * When <paramref name=""t""/> is 0 <paramref name=""result""/> is set to <paramref name=""a""/>.
        /// * When <paramref name=""t""/> is 1 <paramref name=""result""/> is set to  <paramref name=""b""/>.
        /// * When <paramref name=""t""/> is 0.5 <paramref name=""result""/> is set to the midpoint of <paramref name=""a""/> and <paramref name=""b""/>.
        /// </remarks>
        /// <param name=""a"">The pose to interpolate from.</param>
        /// <param name=""b"">To pose to interpolate to.</param>
        /// <param name=""t"">The interpolation factor.</param>
        /// <param name=""result"">The interpolated pose.</param>
        public static void LerpUnclamped(in FaceBlendShapePose a, in FaceBlendShapePose b, float t, out FaceBlendShapePose result)
        {{");

            foreach (var name in FaceBlendShapePose.Shapes)
            {
                generator.AppendLine($"            result.{name} = Mathf.LerpUnclamped(a.{name}, b.{name}, t);");
            }

            generator.AppendLine(
                $@"        }}
    }}
}}");

            // change to Unix line endings to avoid warning in Unity
            var generatedContents = generator.ToString().Replace("\r\n", "\n");

            var fileName = $"{nameof(FaceBlendShapePose)}Fields.cs";
            var filePath = Path.Combine(outputDirectory, fileName);

            // only write the file if anything has changed to avoid triggering file watchers
            if (File.Exists(filePath) && File.ReadAllText(filePath) == generatedContents)
                return;

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            File.WriteAllText(filePath, generatedContents, Encoding.UTF8);
        }
    }
}
