using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class MyResources
{
    public static ClassAndPathName[] GetPathToArray()
    {
        List<ClassAndPathName> path = new List<ClassAndPathName> ();
        path.Add (new ClassAndPathName ("Sprite", "Sprites"));
        path.Add (new ClassAndPathName ("BGM", "Sounds/BGM"));
        path.Add (new ClassAndPathName ("SE", "Sounds/SE"));
        return path.ToArray ();
    }

    public static string[] GetNameToArray(string path)
    {
        var list = new List<string> ();
        Object[] objs = Resources.LoadAll (path);
        foreach (Object obj in objs) {
            list.Add (obj.name);
        }
        return list.ToArray ();
    }

    public class ClassAndPathName
    {
        public string className {
            get;
            private set;
        }
        public string path {
            get;
            private set;
        }

        public ClassAndPathName(string className ,string path)
        {
            this.className = className;
            this.path = path;
        }
    }
}

