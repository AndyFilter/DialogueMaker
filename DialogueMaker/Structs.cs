using System.Collections.Generic;

namespace DialogueMaker
{
    class Structs
    {
        public class Project
        {
            public string Name { get; set; }
            public string Exportpath { get; set; }
            public string Importpath { get; set; }
            public List<NPC> NPCS { get; set; }
        }
        public class NPC
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public List<Node> Nodes { get; set; }
        }
        public class Node
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public List<Choice> Choices { get; set; }
        }
        public class Choice
        {
            public string Text { get; set; }
            public int Next_id { get; set; }
        }
        public class RandomNodes
        {
            public List<int> Timedelay { get; set; }
            public List<Node> Nodes { get; set; }
        }

        public enum NotifType
        {
            Alert,
            Warning,
            Information
        }

        public class UserSettings
        {
            public bool IsAutoSave { get; set; }
            public bool IsAutoSaveNotif { get; set; }
            public int AutoSaveMinutes { get; set; }
            public string DefaultChoiceText { get; set; }
            public Styles Style { get; set; }
        }

        public enum Styles
        {
            Dark,
            Light
        }
    }
}
