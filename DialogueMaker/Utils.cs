using System;
using System.IO;

namespace DialogueMaker
{
    class Utils
    {
        public static DirectoryInfo UserDataPath = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DialogueMaker"));

        public static string GetProjectPath(string ProjectName)
        {
            return Path.Combine(Path.Combine(UserDataPath.FullName, ProjectName), ProjectName + ".Json");
        }
    }
}
