using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using OccaSoftware.Altos.Runtime;
using System.Linq;

namespace OccaSoftware.Altos.Editor
{
    public class CreateGameObjectContextMenus : EditorWindow
    {
        /// <summary>
        /// Sets up a Skybox Director object stack in the current scene's hierarchy.
        /// </summary>
        [MenuItem("GameObject/Altos/Sky Director", false, 15)]
        private static void CreateDirectorWithChildren()
        {
            if (FindObjectOfType<AltosSkyDirector>() != null)
            {
                Debug.Log("Altos Sky Director already exists in scene.");
                return;
            }
            GameObject director = CreateSkyboxDirector();
            CreateSkyObject();
            CreateSkyObject();
            Selection.activeObject = director;
        }

        public static GameObject CreateSkyboxDirector()
        {
            GameObject skyDirector = new GameObject("Sky Director");
            AltosSkyDirector altosSkyDirector = skyDirector.AddComponent<AltosSkyDirector>();

            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string path = scene.path;
            string directory = System.IO.Path.GetDirectoryName(path);

            string guid = AssetDatabase.CreateFolder(directory, scene.name + "_Altos");
            string folderPath = AssetDatabase.GUIDToAssetPath(guid);

            altosSkyDirector.skyDefinition = CreateAsset<SkyDefinition>(folderPath, "Sky");
            altosSkyDirector.atmosphereDefinition = CreateAsset<AtmosphereDefinition>(folderPath, "Atmosphere");
            altosSkyDirector.cloudDefinition = CreateAsset<CloudDefinition>(folderPath, "Clouds");
            altosSkyDirector.starDefinition = CreateAsset<StarDefinition>(folderPath, "Stars");

            return skyDirector;
        }

        private static T CreateAsset<T>(string path, string name)
            where T : ScriptableObject
        {
            string newAssetPath = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");
            T a = CreateInstance<T>();
            AssetDatabase.CreateAsset(a, newAssetPath);
            AssetDatabase.SaveAssets();
            return a;
        }

        [MenuItem("GameObject/Altos/Sky Object", false, 15)]
        public static void CreateSkyObject()
        {
            AltosSkyDirector skyDirector = FindObjectOfType<AltosSkyDirector>();
            if (skyDirector == null)
            {
                CreateDirectorWithChildren();
                return;
            }

            List<SkyObject> skyObjects = FindObjectsOfType<SkyObject>().ToList();

            List<System.Type> types = new List<System.Type>() { typeof(SkyObject) };

            bool sunAlreadyExists = CheckIfAnySuns(skyObjects);
            string name = sunAlreadyExists ? "Moon" : "Sun";
            float orbitOffset = sunAlreadyExists ? 180f : 0f;
            SkyObject.ObjectType objectType = sunAlreadyExists ? SkyObject.ObjectType.Other : SkyObject.ObjectType.Sun;

            GameObject newSkyObject = new GameObject(name, types.ToArray());
            newSkyObject.transform.SetParent(skyDirector.transform);
            newSkyObject.GetComponent<SkyObject>().type = objectType;
            newSkyObject.GetComponent<SkyObject>().orbitOffset = orbitOffset;
            newSkyObject.GetComponent<SkyObject>().SetIcon();
            Selection.activeObject = newSkyObject;
        }

        private static bool CheckIfAnySuns(List<SkyObject> skyObjects)
        {
            foreach (SkyObject skyObject in skyObjects)
            {
                if (skyObject.type == SkyObject.ObjectType.Sun)
                    return true;
            }
            return false;
        }
    }
}
