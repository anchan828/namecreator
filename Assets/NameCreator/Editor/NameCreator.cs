using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Collections.Generic;

[InitializeOnLoad]
public class NameCreator
{
    #region variable
    // 生成するクラスのファイルパス
    private const string FilePath = "Assets/Scripts/SettingConstant";
    // メニューバーのパス
    private const string MenuPath = "EditorExtensions/NameCreator/ForceRebuilds";
    #endregion

    #region method
    [MenuItem(MenuPath, true)]
    private static bool Validate()
    {
        return (EditorApplication.isPlaying || Application.isPlaying) == false;
    }

    [MenuItem(MenuPath)]
    private static void Build()
    {
        SafeCreateDirectory(FilePath);

        List<string> layerNames = new List<string>();

        UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
        List<string> sortingLayers = new List<string>();
        List<string> inputNames = new List<string>();
        List<string> navMeshLayers = new List<string>();

        foreach (UnityEngine.Object obj in objs)
        {
            switch (obj.name)
            {
                case "InputManager":
                    {
                        SerializedProperty axesProperty = new SerializedObject(obj).FindProperty("m_Axes");

                        for (int j = 0; j < axesProperty.arraySize; ++j)
                            inputNames.Add(axesProperty.GetArrayElementAtIndex(j).FindPropertyRelative("m_Name").stringValue);
                    }
                    break;

                case "TagManager":
                    {
                        SerializedProperty sortinglayersProperty = new SerializedObject(obj).FindProperty("m_SortingLayers");

                        for (int j = 0; j < sortinglayersProperty.arraySize; ++j)
                            sortingLayers.Add(sortinglayersProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name").stringValue);
                    }
                    break;

                case "NavMeshLayers":
                    {
                        SerializedObject navMeshlayersObject = new SerializedObject(obj);

                        for (int j = 0; j < 3; ++j)
                            navMeshLayers.Add(navMeshlayersObject.FindProperty("Built-in Layer " + j).FindPropertyRelative("name").stringValue);

                        for (int j = 0; j < 28; ++j)
                            navMeshLayers.Add(navMeshlayersObject.FindProperty("User Layer " + j).FindPropertyRelative("name").stringValue);
                    }
                    break;
            }
        }

        for (int i = 0; i < 32; ++i)
            layerNames.Add(LayerMask.LayerToName(i));

        AssetDatabase.StartAssetEditing();
        {
            Build("Tag", InternalEditorUtility.tags);
            Build("Layers", layerNames.ToArray());
            Build("SortingLayer", sortingLayers.ToArray());
            Build("NavMeshLayer", navMeshLayers.ToArray());
            Build("Input", inputNames.ToArray());
            Build("Scene",
                EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select<EditorBuildSettingsScene, string>(scene => Path.GetFileNameWithoutExtension(scene.path))
                .ToArray());
        }
        AssetDatabase.StopAssetEditing();
        EditorUtility.UnloadUnusedAssetsImmediate();
        AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
    }

    static NameCreator()
    {
        if (EditorApplication.timeSinceStartup < 10)
            Build();
    }

    public static DirectoryInfo SafeCreateDirectory(string path)
    {
        if (Directory.Exists(path))
            return null;

        return Directory.CreateDirectory(path);
    }

    private static void Build(string className, string[] names)
    {
        StringBuilder builder = new StringBuilder();

        builder = AppendClassText(builder, className, names);

        string text = builder.ToString().Replace(",}", "}");
        string assetPath = string.Format("{0}/{1}Name.cs", FilePath, className);

        MonoImporter monoImporter = AssetImporter.GetAtPath(assetPath) as MonoImporter;

        bool needRebuild = false;

        if (monoImporter)
        {
            PropertyInfo[] props = monoImporter.GetScript().GetClass().GetProperties();

            needRebuild = props.Length != names.Length;

            for (int i = 0; i < props.Length; ++i)
            {
                if (props[i].Name != Replace(names[i]))
                    needRebuild = true;
            }
        }
        else
            needRebuild = true;

        if (needRebuild)
            File.WriteAllText(assetPath, text);
    }

    private static StringBuilder AppendClassText(StringBuilder builder, string className, string[] names)
    {
        builder.AppendLine(String.Concat("public class ", className, "Name"));
        builder.Append("{");
        {
            AppendPropertyText(builder, names);
            AppendArrayText(builder, names);
        }
        builder.Append("}");
        return builder;
    }

    private static void AppendPropertyText(StringBuilder builder, IEnumerable<string> names)
    {
        string[] _names = names.Distinct().ToArray();
        foreach (string name in _names)
        {
            if (string.IsNullOrEmpty(name))
                continue;

            builder.AppendFormat(@"
	/// <summary>
	/// return ""{0}""
 	/// </summary>
	public const string @{1} = ""{0}"";",
                                    name, Replace(name))
                                    .AppendLine();
        }
    }

    private static void AppendArrayText(StringBuilder builder, IList<string> names)
    {
        builder.AppendLine().AppendLine(String.Concat("\t", "/// <summary>"));

        for (int i = 0; i < names.Count; ++i)
            builder.Append("\t").AppendFormat("/// <para>{0}. \"{1}\"</para>", i, names[i]).AppendLine();

        builder.Append("\t").AppendLine("/// </summary>");
        builder.Append("\t").Append("public static readonly string[] names = new string[] {");

        foreach (string name in names)
            builder.AppendFormat(@" ""{0}"",", name);

        builder.AppendLine(" };");
    }

    private static string Replace(string name)
    {
        string[] invalidChars =
		{
		    " ",
		    "!",
		    "\"",
		    "#",
		    "$",
		    "%",
		    "&",
		    "\'",
		    "(",
		    ")",
		    "-",
		    "=",
		    "^",
		    "~",
		    "¥",
		    "|",
		    "[",
		    "{",
		    "@",
		    "`",
		    "]",
		    "}",
		    ":",
		    "*",
		    ";",
		    "+",
		    "/",
		    "?",
		    ".",
		    ">",
		    ",",
		    "<"
		};

        name = invalidChars.Aggregate(name, (current, t) => current.Replace(t, string.Empty));

        if (char.IsNumber(name[0]))
            name = "_" + name;

        return name;
    }
    #endregion
}