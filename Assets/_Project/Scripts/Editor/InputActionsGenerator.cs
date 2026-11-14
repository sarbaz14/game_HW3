using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChickenShooter.EditorTools
{
    /// <summary>
    /// Writes Assets/_Project/Input/PlayerInputActions.inputactions with a Gameplay map
    /// (WASD + arrows, Space / LMB fire, B / Shift / RMB super missile, Esc / P pause) and a UI map.
    /// </summary>
    public static class InputActionsGenerator
    {
        public const string AssetPath = EditorAssets.InputDir + "/PlayerInputActions.inputactions";

        [MenuItem("ChickenShooter/3. Generate Input Actions", false, 30)]
        public static InputActionAsset Generate()
        {
            ProceduralArt.EnsureFolder(EditorAssets.InputDir);

            var json = BuildJson();
            File.WriteAllText(AssetPath, json, Encoding.UTF8);
            AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);

            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            if (asset == null)
            {
                Debug.LogError("[InputActions] Failed to import " + AssetPath + ". Check the JSON structure.");
                return null;
            }

            Debug.Log("[InputActions] PlayerInputActions created: maps = " + asset.actionMaps.Count +
                      ", actions = " + (asset.FindActionMap("Gameplay", false) != null ? "Gameplay ok" : "Gameplay missing"));
            return asset;
        }

        private static string BuildJson()
        {
            var builder = new StringBuilder();
            builder.Append("{\n");
            builder.Append("    \"name\": \"PlayerInputActions\",\n");
            builder.Append("    \"maps\": [\n");

            // ---------------------------------------------------------- Gameplay map
            builder.Append("        {\n");
            builder.Append("            \"name\": \"Gameplay\",\n");
            builder.Append("            \"id\": \"").Append(NewGuid()).Append("\",\n");
            builder.Append("            \"actions\": [\n");
            builder.Append(Action("Move", "Value", "Vector2", true)).Append(",\n");
            builder.Append(Action("Fire", "Button", "Button", false)).Append(",\n");
            builder.Append(Action("SuperMissile", "Button", "Button", false)).Append(",\n");
            builder.Append(Action("Pause", "Button", "Button", false)).Append('\n');
            builder.Append("            ],\n");
            builder.Append("            \"bindings\": [\n");

            builder.Append(Composite("WASD", "2DVector", "Move")).Append(",\n");
            builder.Append(Part("up", "<Keyboard>/w", "Move")).Append(",\n");
            builder.Append(Part("down", "<Keyboard>/s", "Move")).Append(",\n");
            builder.Append(Part("left", "<Keyboard>/a", "Move")).Append(",\n");
            builder.Append(Part("right", "<Keyboard>/d", "Move")).Append(",\n");

            builder.Append(Composite("Arrows", "2DVector", "Move")).Append(",\n");
            builder.Append(Part("up", "<Keyboard>/upArrow", "Move")).Append(",\n");
            builder.Append(Part("down", "<Keyboard>/downArrow", "Move")).Append(",\n");
            builder.Append(Part("left", "<Keyboard>/leftArrow", "Move")).Append(",\n");
            builder.Append(Part("right", "<Keyboard>/rightArrow", "Move")).Append(",\n");

            builder.Append(Binding("Space", "<Keyboard>/space", "Fire")).Append(",\n");
            builder.Append(Binding("Left Mouse", "<Mouse>/leftButton", "Fire")).Append(",\n");

            builder.Append(Binding("B Key", "<Keyboard>/b", "SuperMissile")).Append(",\n");
            builder.Append(Binding("Left Shift", "<Keyboard>/leftShift", "SuperMissile")).Append(",\n");
            builder.Append(Binding("Right Mouse", "<Mouse>/rightButton", "SuperMissile")).Append(",\n");

            builder.Append(Binding("Escape", "<Keyboard>/escape", "Pause")).Append(",\n");
            builder.Append(Binding("P Key", "<Keyboard>/p", "Pause")).Append('\n');

            builder.Append("            ]\n");
            builder.Append("        },\n");

            // ---------------------------------------------------------- UI map
            builder.Append("        {\n");
            builder.Append("            \"name\": \"UI\",\n");
            builder.Append("            \"id\": \"").Append(NewGuid()).Append("\",\n");
            builder.Append("            \"actions\": [\n");
            builder.Append(Action("Navigate", "PassThrough", "Vector2", true)).Append(",\n");
            builder.Append(Action("Submit", "Button", "Button", false)).Append(",\n");
            builder.Append(Action("Cancel", "Button", "Button", false)).Append(",\n");
            builder.Append(Action("Point", "PassThrough", "Vector2", true)).Append(",\n");
            builder.Append(Action("Click", "PassThrough", "Button", false)).Append(",\n");
            builder.Append(Action("ScrollWheel", "PassThrough", "Vector2", false)).Append(",\n");
            builder.Append(Action("TrackedDevicePosition", "PassThrough", "Vector3", false)).Append(",\n");
            builder.Append(Action("TrackedDeviceOrientation", "PassThrough", "Quaternion", false)).Append('\n');
            builder.Append("            ],\n");
            builder.Append("            \"bindings\": [\n");

            builder.Append(Composite("WASD", "2DVector", "Navigate")).Append(",\n");
            builder.Append(Part("up", "<Keyboard>/w", "Navigate")).Append(",\n");
            builder.Append(Part("down", "<Keyboard>/s", "Navigate")).Append(",\n");
            builder.Append(Part("left", "<Keyboard>/a", "Navigate")).Append(",\n");
            builder.Append(Part("right", "<Keyboard>/d", "Navigate")).Append(",\n");

            builder.Append(Composite("Arrows", "2DVector", "Navigate")).Append(",\n");
            builder.Append(Part("up", "<Keyboard>/upArrow", "Navigate")).Append(",\n");
            builder.Append(Part("down", "<Keyboard>/downArrow", "Navigate")).Append(",\n");
            builder.Append(Part("left", "<Keyboard>/leftArrow", "Navigate")).Append(",\n");
            builder.Append(Part("right", "<Keyboard>/rightArrow", "Navigate")).Append(",\n");

            builder.Append(Binding("Enter", "<Keyboard>/enter", "Submit")).Append(",\n");
            builder.Append(Binding("Space", "<Keyboard>/space", "Submit")).Append(",\n");
            builder.Append(Binding("Gamepad Submit", "<Gamepad>/buttonSouth", "Submit")).Append(",\n");
            builder.Append(Binding("Escape", "<Keyboard>/escape", "Cancel")).Append(",\n");
            builder.Append(Binding("Gamepad Cancel", "<Gamepad>/buttonEast", "Cancel")).Append(",\n");
            builder.Append(Binding("Pointer", "<Mouse>/position", "Point")).Append(",\n");
            builder.Append(Binding("Left Click", "<Mouse>/leftButton", "Click")).Append(",\n");
            builder.Append(Binding("Scroll", "<Mouse>/scroll", "ScrollWheel")).Append(",\n");
            builder.Append(Binding("Tracked Position", "<XRHMD>/centerEyePosition", "TrackedDevicePosition")).Append(",\n");
            builder.Append(Binding("Tracked Orientation", "<XRHMD>/centerEyeRotation", "TrackedDeviceOrientation")).Append('\n');

            builder.Append("            ]\n");
            builder.Append("        }\n");

            builder.Append("    ],\n");
            builder.Append("    \"controlSchemes\": []\n");
            builder.Append("}\n");

            return builder.ToString();
        }

        private static string NewGuid() => Guid.NewGuid().ToString();

        private static string Action(string name, string type, string expectedControlType, bool initialStateCheck)
        {
            return "                {\n" +
                   "                    \"name\": \"" + name + "\",\n" +
                   "                    \"type\": \"" + type + "\",\n" +
                   "                    \"id\": \"" + NewGuid() + "\",\n" +
                   "                    \"expectedControlType\": \"" + expectedControlType + "\",\n" +
                   "                    \"processors\": \"\",\n" +
                   "                    \"interactions\": \"\",\n" +
                   "                    \"initialStateCheck\": " + (initialStateCheck ? "true" : "false") + "\n" +
                   "                }";
        }

        private static string Binding(string name, string path, string action)
        {
            return "                {\n" +
                   "                    \"name\": \"" + name + "\",\n" +
                   "                    \"id\": \"" + NewGuid() + "\",\n" +
                   "                    \"path\": \"" + path + "\",\n" +
                   "                    \"interactions\": \"\",\n" +
                   "                    \"processors\": \"\",\n" +
                   "                    \"groups\": \"\",\n" +
                   "                    \"action\": \"" + action + "\",\n" +
                   "                    \"isComposite\": false,\n" +
                   "                    \"isPartOfComposite\": false\n" +
                   "                }";
        }

        private static string Composite(string name, string compositePath, string action)
        {
            return "                {\n" +
                   "                    \"name\": \"" + name + "\",\n" +
                   "                    \"id\": \"" + NewGuid() + "\",\n" +
                   "                    \"path\": \"" + compositePath + "\",\n" +
                   "                    \"interactions\": \"\",\n" +
                   "                    \"processors\": \"\",\n" +
                   "                    \"groups\": \"\",\n" +
                   "                    \"action\": \"" + action + "\",\n" +
                   "                    \"isComposite\": true,\n" +
                   "                    \"isPartOfComposite\": false\n" +
                   "                }";
        }

        private static string Part(string name, string path, string action)
        {
            return "                {\n" +
                   "                    \"name\": \"" + name + "\",\n" +
                   "                    \"id\": \"" + NewGuid() + "\",\n" +
                   "                    \"path\": \"" + path + "\",\n" +
                   "                    \"interactions\": \"\",\n" +
                   "                    \"processors\": \"\",\n" +
                   "                    \"groups\": \"\",\n" +
                   "                    \"action\": \"" + action + "\",\n" +
                   "                    \"isComposite\": false,\n" +
                   "                    \"isPartOfComposite\": true\n" +
                   "                }";
        }
    }
}
