using System.Text;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;

[InitializeOnLoad]
public class NameCreator
{
    [MenuItem("Assets/Name Creator/Force Rebuilds", true)]
    static bool Validate()
    {
        return (EditorApplication.isPlaying || Application.isPlaying) == false;
    }

    [MenuItem("Assets/Name Creator/Force Rebuilds")]
    static void Build()
    {
        var layerNames = new List<string>();

        var objs = Resources.FindObjectsOfTypeAll<Object>();
        var sortingLayers = new List<string>();
        var inputNames = new List<string>();
        var navMeshLayers = new List<string>();

        foreach (var obj in objs)
        {
            switch (obj.name)
            {
                case "InputManager":
                    {
                        var axesProperty = new SerializedObject(obj).FindProperty("m_Axes");

                        for (var j = 0; j < axesProperty.arraySize; j++)
                        {
                            inputNames.Add(axesProperty.GetArrayElementAtIndex(j).FindPropertyRelative("m_Name").stringValue);
                        }
                    }
                    break;
                case "TagManager":
                    {
                        var sortinglayersProperty = new SerializedObject(obj).FindProperty("m_SortingLayers");

                        for (var j = 0; j < sortinglayersProperty.arraySize; j++)
                        {
                            sortingLayers.Add(sortinglayersProperty.GetArrayElementAtIndex(j).FindPropertyRelative("name").stringValue);
                        }
                    }
                    break;

                case "NavMeshLayers":
                    {

                        var navMeshlayersObject = new SerializedObject(obj);

                        for (var j = 0; j < 3; j++)
                        {
                            navMeshLayers.Add(navMeshlayersObject.FindProperty("Built-in Layer " + j).FindPropertyRelative("name").stringValue);
                        }

                        for (var j = 0; j < 28; j++)
                        {
                            navMeshLayers.Add(navMeshlayersObject.FindProperty("User Layer " + j).FindPropertyRelative("name").stringValue);
                        }
                    }
                    break;
            }
        }

        for (var i = 0; i < 32; i++)
        {
            layerNames.Add(LayerMask.LayerToName(i));
        }

        AssetDatabase.StartAssetEditing();
        {
            Build("Tag", InternalEditorUtility.tags);
            Build("Layer", layerNames.ToArray());
            Build("SortingLayer", sortingLayers.ToArray());
            Build("NavMeshLayer", navMeshLayers.ToArray());
            Build("Input", inputNames.ToArray());
            Build("Scene", EditorBuildSettings.scenes.Where(scene => scene.enabled).Select<EditorBuildSettingsScene, string>(scene => Path.GetFileNameWithoutExtension(scene.path)).ToArray());
        }
        AssetDatabase.StopAssetEditing();
        EditorUtility.UnloadUnusedAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
    }

    static NameCreator()
    {
        if (EditorApplication.timeSinceStartup < 10)
        {
            Build();
        }
    }

    static void Build(string className, string[] names)
    {
        var builder = new StringBuilder();

        builder = AppendClassText(builder, className, names);

        var text = builder.ToString().Replace(",}", "}");
        var assetPath = string.Format("{0}../{1}Name.cs", currentFolderPath, className);

        var monoImporter = AssetImporter.GetAtPath(assetPath.Replace("/Editor/..", "")) as MonoImporter;

        var needRebuild = false;

        if (monoImporter)
        {
            var props = monoImporter.GetScript().GetClass().GetProperties();

            if (props.Length != names.Length)
                needRebuild = true;

            for (var i = 0; i < props.Length; i++)
            {
                if (props[i].Name != Replace(names[i]))
                {
                    needRebuild = true;
                }
            }
        }
        else
        {
            needRebuild = true;
        }

        if (needRebuild)
        {
            File.WriteAllText(assetPath, text);
        }
    }

    static StringBuilder AppendClassText(StringBuilder builder, string className, string[] names)
    {

        builder.AppendLine("public class " + className + "Name");
        builder.AppendLine("{");
        {
            AppendPropertyText(builder, names);
            AppendArrayText(builder, names);
        }
        builder.AppendLine("}");
        return builder;
    }

    static void AppendPropertyText(StringBuilder builder, IEnumerable<string> names)
    {
        var _names = names.Distinct().ToArray();
        foreach (var name in _names)
        {
            if (string.IsNullOrEmpty(name))
                return;

            builder.AppendFormat(@"
	/// <summary>
	/// return ""{0}""
 	/// </summary>
	public const string @{1} = ""{0}"";", name, Replace(name)).AppendLine();
        }
    }

    static void AppendArrayText(StringBuilder builder, IList<string> names)
    {
        builder.Append("\n\t").AppendLine("/// <summary>");

        for (var i = 0; i < names.Count; i++)
        {
            builder.Append("\t").AppendFormat("/// <para>{0}. \"{1}\"</para>", i, names[i]).AppendLine();
        }

        builder.Append("\t").AppendLine("/// </summary>");
        builder.Append("\t").Append("public static readonly string[] names = new string[]{");

        foreach (var name in names)
        {
            builder.AppendFormat(@"""{0}"",", name);
        }

        builder.AppendLine("};");
    }

    static string Replace(string name)
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
        {
            name = "_" + name;
        }

        return name;
    }

    static string currentFolderPath
    {
        get
        {
            var currentFilePath = new System.Diagnostics.StackTrace(true).GetFrame(0).GetFileName();
            return "Assets" + currentFilePath.Substring(0, currentFilePath.LastIndexOf(Path.DirectorySeparatorChar) + 1).Replace(Application.dataPath.Replace("/", Path.DirectorySeparatorChar.ToString()), string.Empty).Replace("\\", "/");
        }
    }
}
