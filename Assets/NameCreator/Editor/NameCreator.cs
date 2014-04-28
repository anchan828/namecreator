using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEditorInternal;
using System.Collections.Generic;
using System;
using System.IO;

[InitializeOnLoad]
public class NameCreator
{
	[MenuItem("Assets/Name Creator/Force Rebuilds", true)]
	static bool Validate ()
	{
		return (EditorApplication.isPlaying || Application.isPlaying) == false;
	}

	[MenuItem("Assets/Name Creator/Force Rebuilds")]
	static void Build ()
	{
		List<string> layerNames = new List<string> ();
		
		var objs = Resources.FindObjectsOfTypeAll<UnityEngine.Object> ();
		List<string> sortingLayers = new List<string> ();
		List<string> inputNames = new List<string> ();
		List<string> navMeshLayers = new List<string> ();

		for (int i = 0; i < objs.Length; i++) {
			if (objs [i].name == "InputManager") {
				var axesProperty = new SerializedObject (objs [i]).FindProperty ("m_Axes");
				
				for (int j = 0; j < axesProperty.arraySize; j++) {
					inputNames.Add (axesProperty.GetArrayElementAtIndex (j).FindPropertyRelative ("m_Name").stringValue);
				}
			}
			
			if (objs [i].name == "TagManager") {
				var sortinglayersProperty = new SerializedObject (objs [i]).FindProperty ("m_SortingLayers");
				
				for (int j = 0; j < sortinglayersProperty.arraySize; j++) {
					sortingLayers.Add (sortinglayersProperty.GetArrayElementAtIndex (j).FindPropertyRelative ("name").stringValue);
				}
			}

			if (objs [i].name == "NavMeshLayers") {

				var navMeshlayersObject = new SerializedObject (objs [i]);

				for (int j = 0; j < 3; j++) {
					navMeshLayers.Add (navMeshlayersObject.FindProperty ("Built-in Layer " + j).FindPropertyRelative ("name").stringValue);
				}

				for (int j = 0; j < 28; j++) {
					navMeshLayers.Add (navMeshlayersObject.FindProperty ("User Layer " + j).FindPropertyRelative ("name").stringValue);
				}
			}
		}

		for (int i = 0; i < 32; i++) {
			layerNames.Add (LayerMask.LayerToName (i));
		}

		AssetDatabase.StartAssetEditing ();
		{
			Build ("Tag", InternalEditorUtility.tags);
			Build ("Layer", layerNames.ToArray ());
			Build ("SortingLayer", sortingLayers.ToArray ());
			Build ("NavMeshLayer", navMeshLayers.ToArray ());
			Build ("Input", inputNames.ToArray ());
			Build ("Scene", EditorBuildSettings.scenes.Where (scene => scene.enabled).Select<EditorBuildSettingsScene,string> (scene => Path.GetFileNameWithoutExtension (scene.path)).ToArray ());
		}
		AssetDatabase.StopAssetEditing ();
		EditorUtility.UnloadUnusedAssets ();
		AssetDatabase.Refresh (ImportAssetOptions.ImportRecursive);
	}

	static NameCreator()
	{
		if(EditorApplication.timeSinceStartup < 10){
			Build();
		}
	}

	static void Build (string className, string[] names)
	{
		System.Text.StringBuilder builder = new System.Text.StringBuilder ();
		
		builder = AppendClassText (builder, className, names);
		
		string text = builder.ToString ().Replace (",}", "}");
		string assetPath = string.Format ("{0}../{1}Name.cs", currentFolderPath, className);
		
		var monoImporter = MonoImporter.GetAtPath (assetPath.Replace ("/Editor/..", "")) as MonoImporter;
		
		bool needRebuild = false;
		
		if (monoImporter) {
			var props = monoImporter.GetScript ().GetClass ().GetProperties ();
			
			if (props.Length != names.Length)
				needRebuild = true;
			
			for (int i = 0; i < props.Length; i++) {
				if (props [i].Name != Replace (names [i])) {
					needRebuild = true;
				}
			}
		} else {
			needRebuild = true;
		}
		
		if (needRebuild) {
			System.IO.File.WriteAllText (assetPath, text);
		}
	}

	static System.Text.StringBuilder AppendClassText (System.Text.StringBuilder builder, string className, string[] names)
	{
		
		builder.AppendLine ("public class " + className + "Name");
		builder.AppendLine ("{");
		{
			AppendPropertyText (builder, names);
			AppendArrayText (builder, names);
		}
		builder.AppendLine ("}");
		return builder;
	}
	
	static void AppendPropertyText (System.Text.StringBuilder builder, string[] names)
	{
		string[] _names = names.Distinct ().ToArray ();
		for (int i = 0; i < _names.Length; i++) {
			var name = _names [i];

			if (string.IsNullOrEmpty (name))
				return;

			builder.AppendFormat (@"
	/// <summary>
	/// return ""{0}""
 	/// </summary>
	public static string @{1} {{ get{{ return ""{0}""; }} }}", name, Replace (name)).AppendLine ();
		}
	}
	
	static void AppendArrayText (System.Text.StringBuilder builder, string[] names)
	{
		builder.Append ("\n\t").AppendLine ("/// <summary>");

		for (int i = 0; i < names.Length; i++) {
			builder.Append ("\t").AppendFormat ("/// <para>{0}. \"{1}\"</para>", i, names [i]).AppendLine ();
		}

		builder.Append ("\t").AppendLine ("/// </summary>");
		builder.Append ("\t").Append ("public static readonly string[] names = new string[]{");

		for (int i = 0; i < names.Length; i++) {
			builder.AppendFormat (@"""{0}"",", names [i]);
		}

		builder.AppendLine ("};");
	}
	
	static string Replace (string name)
	{
		string[] invalidChars = new string[] {
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

		for (int i = 0; i < invalidChars.Length; i++) {
			name = name.Replace (invalidChars [i], string.Empty);
		}

		if (char.IsNumber (name [0])) {
			name = "_" + name;
		}

		return name;
	}
	
	static string currentFolderPath {
		get {
			string currentFilePath = new System.Diagnostics.StackTrace (true).GetFrame (0).GetFileName ();
			return "Assets" + currentFilePath.Substring (0, currentFilePath.LastIndexOf (Path.DirectorySeparatorChar) + 1).Replace (Application.dataPath.Replace ("/", Path.DirectorySeparatorChar.ToString ()), string.Empty).Replace ("\\", "/");
		}
	}
}