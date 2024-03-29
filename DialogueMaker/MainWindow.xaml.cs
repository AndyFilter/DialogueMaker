﻿using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DialogueMaker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Structs.Project CurrentProject;

        private Structs.NPC CurrentNPC;
        //private Structs.Node SelectedNode;

        //private Structs.NPC CurrentNPC;
        private Structs.Node CurrentNode;

        private DateTime LastTimeDialogueDeleted;
        private DateTime LastTimeNpcDeleted;

        private bool AutoSaveState = true;
        private bool AutoSaveNotif = true;
        private int AutoSaveMinutes = 1;
        private new Structs.Styles Style = Structs.Styles.Dark;

        private string TextChoice = "Continue";

        private Task AutoSavefunc;

        private Structs.UserSettings UserData;

        private class NotificationData
        {
            public string NotiText { get; set; }
        }

        private class TypesData
        {
            public string Type { get; set; }
        }

        private int LabelHeight = 25;

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif

            ((INotifyCollectionChanged)NpcList.Items).CollectionChanged += NpcList_CollectionChanged;
            ((INotifyCollectionChanged)DialogueList.Items).CollectionChanged += DialogueList_CollectionChanged;

            LoadProjectData();

            ReadUserData();

            if (AutoSaveState && AutoSavefunc == null)
                AutoSavefunc = SetAutoSaveEvent(AutoSaveMinutes);
        }

        private void CreateNotification(string Text, Structs.NotifType Type)
        {
            var noti = new Controls.UserNotification();

            (noti.FindName("NotificationLabel") as TextBlock).Text = Text;

            switch (Type)
            {
                case Structs.NotifType.Information:
                    break;

                case Structs.NotifType.Warning:
                    (noti.FindName("Border") as Border).Background = (Brush)FindResource("NotifWarning");
                    break;
                case Structs.NotifType.Alert:
                    break;
            }

            NotificationPanel.Children.Add(noti);
            DockPanel.SetDock(noti, Dock.Bottom);
        }

        private void CreateNotification(string Text)
        {
            CreateNotification(Text, Structs.NotifType.Information);
        }

        public void SetData()
        {
            NpcList.Items.Clear();
            DialogueList.Items.Clear();

            if (CurrentProject.NPCS == null)
                return;

            foreach (Structs.NPC npc in CurrentProject.NPCS)
            {
                var label = new Label();
                label.DataContext = npc;
                label.Content = npc.Name;
                NpcList.Items.Add(label);
            }
        }

        private void NewNpcSelected(object sender, SelectionChangedEventArgs e)
        {
            if (NpcList.Items.Count <= 0 || NpcList.Items.GetItemAt(NpcList.Items.Count - 1).GetType() != typeof(Label) || NpcList.SelectedItem == null)
                return;

            DialogueList.Items.Clear();
            DialogueText.Text = "";
            Button1.Visibility = Visibility.Hidden;
            Button2.Visibility = Visibility.Hidden;
            Button3.Visibility = Visibility.Hidden;
            Button4.Visibility = Visibility.Hidden;

            CurrentNPC = (Structs.NPC)(NpcList.SelectedItem as Label).DataContext;

            NpcNameBox.Text = CurrentNPC.Name;
            DialogueBox.Text = "";
            ButtonCombo1.ItemsSource = "";
            ButtonCombo2.ItemsSource = "";
            ButtonCombo3.ItemsSource = "";
            ButtonCombo4.ItemsSource = "";

            foreach (TypesData type in NpcTypeBox.Items)
            {
                if (type.Type == CurrentNPC.Type)
                    NpcTypeBox.SelectedItem = type;
            }

            if (NpcTypeBox.SelectedItem != null && ((TypesData)NpcTypeBox.SelectedItem).Type != CurrentNPC.Type)
            {
                NpcTypeBox.SelectedIndex = -1;
            }

            if (CurrentNPC == null || CurrentNPC.Nodes == null)
                return;

            foreach (Structs.Node dialogue in CurrentNPC.Nodes)
            {
                var label = new Label();
                label.Content = dialogue.Text;
                label.DataContext = dialogue;
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                label.MaxHeight = LabelHeight;
                DialogueList.Items.Add(label);
            }
        }

        private void NewDialogueSelected(object sender, SelectionChangedEventArgs e)
        {
            if (DialogueList.SelectedItem != null)
            {
                CurrentNode = (Structs.Node)(DialogueList.SelectedItem as Label).DataContext;
                SetDialogue(CurrentNode, CurrentNPC);
            }
        }

        private void SetDialogue(Structs.Node Node, Structs.NPC Npc)
        {
            SelectDialogue(Node);
            DialogueText.Text = Node.Text;
            NpcNameBox.Text = Npc.Name;
            DialogueBox.Text = Node.Text;
            DialogueText.Text = Node.Text;

            //UpdateNpcTypes();
            //NpcTypeBox.Items.Clear();
            //CurrentProject.NpcTypes.ForEach(a => NpcTypeBox.Items.Add(a));

            foreach (TypesData type in NpcTypeBox.Items)
            {
                if (type.Type == Npc.Type)
                    NpcTypeBox.SelectedItem = type;
                //NpcTypeBox.Items[NpcTypeBox.Items.IndexOf(type)] = true;
            }

            if (((TypesData)NpcTypeBox.SelectedItem).Type != Npc.Type)
            {
                NpcTypeBox.SelectedIndex = -1;
            }

            SetButtons(Node);
        }

        private void SetTestButtons(Structs.Node Node)
        {
            List<Structs.Choice> Choices = null;
            if (Node.Choices != null)
                Choices = new List<Structs.Choice>(Node.Choices);

            Button1.Visibility = Visibility.Hidden;
            Button2.Visibility = Visibility.Hidden;
            Button3.Visibility = Visibility.Hidden;
            Button4.Visibility = Visibility.Hidden;

            if (Choices == null || Choices.Count <= 0)
            {
                Button1.Content = TextChoice;
                Button1.Visibility = Visibility.Visible;
                Button1.DataContext = Choices;
                return;
            }

            for (int i = 1; i <= CurrentNode.Choices.Count; i++)
            {
                Button button = FindName("Button" + i) as Button;

                var choice = Choices[i - 1];

                button.Content = choice.Text;
                button.DataContext = choice;
                button.Visibility = Visibility.Visible;
                if (choice.Text.Length <= 0)
                {
                    button.Content = TextChoice;
                }
            }
        }

        private void SetButtons(Structs.Node Node)
        {
            List<Structs.Choice> Choices = null;
            if (Node.Choices != null)
                Choices = new List<Structs.Choice>(Node.Choices);

            SetButtonsCombos();

            Button1.Visibility = Visibility.Hidden;
            Button2.Visibility = Visibility.Hidden;
            Button3.Visibility = Visibility.Hidden;
            Button4.Visibility = Visibility.Hidden;

            ButtonBox1.Text = "";
            ButtonBox2.Text = "";
            ButtonBox3.Text = "";
            ButtonBox4.Text = "";

            if (Choices == null || Choices.Count <= 0)
            {
                Button1.Content = TextChoice;
                //ButtonBox1.Text = TextChoice;
                Button1.Visibility = Visibility.Visible;
                Button1.DataContext = Choices;
                return;
            }

            for (int i = 1; i <= CurrentNode.Choices.Count; i++)
            {
                Button button = FindName("Button" + i) as Button;
                TextBox buttonBox = FindName("ButtonBox" + i) as TextBox;

                var choice = Choices[i - 1];

                buttonBox.Text = choice.Text;
                button.Content = choice.Text;
                button.DataContext = choice;
                button.Visibility = Visibility.Visible;
                if (choice.Text.Length <= 0)
                {
                    button.Content = TextChoice;
                    buttonBox.Text = TextChoice;
                }
            }

            //foreach (Structs.Choice choice in Choices)
            //{
            //    switch (Choices.IndexOf(choice))
            //    {
            //        case 0:
            //            Button1.Content = choice.Text;
            //            ButtonBox1.Text = choice.Text;
            //            Button1.DataContext = choice;
            //            Button1.Visibility = Visibility.Visible;
            //            break;
            //        case 1:
            //            Button2.Content = choice.Text;
            //            ButtonBox2.Text = choice.Text;
            //            Button2.DataContext = choice;
            //            Button2.Visibility = Visibility.Visible;
            //            break;
            //        case 2:
            //            Button3.Content = choice.Text;
            //            ButtonBox3.Text = choice.Text;
            //            Button3.DataContext = choice;
            //            Button3.Visibility = Visibility.Visible;
            //            break;
            //        case 3:
            //            Button4.Content = choice.Text;
            //            ButtonBox4.Text = choice.Text;
            //            Button4.DataContext = choice;
            //            Button4.Visibility = Visibility.Visible;
            //            break;
            //    }
            //}
        }

        private void SetButtonsCombos()
        {

            ButtonCombo1.ItemsSource = GetAllDialoguesOfNpc(CurrentNPC);
            ButtonCombo2.ItemsSource = GetAllDialoguesOfNpc(CurrentNPC);
            ButtonCombo3.ItemsSource = GetAllDialoguesOfNpc(CurrentNPC);
            ButtonCombo4.ItemsSource = GetAllDialoguesOfNpc(CurrentNPC);

            if (CurrentNode.Choices == null)
            {
                return;
            }

            for (int i = 1; i <= CurrentNode.Choices.Count; i++)
            {
                ComboBox comboBox = FindName("ButtonCombo" + i) as ComboBox;
                //var LabelData = (comboBox.Items[i - 1] as Label).DataContext as Structs.Node;

                var SearchedNode = GetNodeById(CurrentNPC, CurrentNode.Choices[i - 1].Next_id);
                if (SearchedNode != null)
                {

                    foreach (Label node in comboBox.Items)
                    {
                        var Data = node.DataContext as Structs.Node;

                        if (SearchedNode.Id == Data.Id)
                        {
                            comboBox.SelectedItem = node;
                            break;
                        }
                    }
                    if (SearchedNode.Id == -2)
                    {
                        comboBox.SelectedItem = null;
                    }
                }
            }
        }

        private Structs.Node GetNodeById(Structs.NPC Npc, int Id)
        {
            if (Id == -1)
            {
                var text = "Exit the Dialogue";
                var node = new Structs.Node() { Id = -1, Text = string.Format("({0})", text) };
                return node;
            }
            else if (Id == -2)
            {
                var text = "None";
                var node = new Structs.Node() { Id = -2, Text = string.Format("({0})", text) };
                return node;
            }
            if (Npc.Nodes == null)
                return null;
            foreach (Structs.Node node in Npc.Nodes)
            {
                if (node.Id == Id)
                    return node;
            }
            return null;
        }

        private List<Label> GetAllDialoguesOfNpc(Structs.NPC Npc)
        {
            var Output = new List<Label>();

            var label = new Label();
            var text = "None";
            label.DataContext = new Structs.Node() { Id = -2, Text = string.Format("({0})", text) };
            label.Content = string.Format("({0})", text);
            Output.Add(label);

            label = new Label();
            text = "Exit the Dialogue";
            label.DataContext = new Structs.Node() { Id = -1, Text = string.Format("({0})", text) };
            label.Content = string.Format("({0})", text);
            Output.Add(label);

            if (Npc.Nodes == null)
                return Output;

            foreach (Structs.Node node in Npc.Nodes)
            {
                if (node == CurrentNode)
                    continue;
                label = new Label();
                text = node.Text;
                //text = node.Text.Substring(0, Math.Min(node.Text.Length, 16));
                //if (node.Text.Length >= 16)
                //{
                //    text += "...";
                //}
                label.DataContext = node;
                label.Content = string.Format("({0}) {1}", node.Id, text);
                Output.Add(label);
            }

            return Output;
        }

        private void DialogueButtonClicked(object sender, RoutedEventArgs e)
        {
            Button Button = (Button)sender;
            var Data = Button.DataContext as Structs.Choice;

            if (Data == null || Data.Next_id == -1)
            {
                DialogueText.Text = "Dialogue Exited";
                Button1.Visibility = Visibility.Hidden;
                Button2.Visibility = Visibility.Hidden;
                Button3.Visibility = Visibility.Hidden;
                Button4.Visibility = Visibility.Hidden;
                return;
            }

            var NextDialogueId = Data.Next_id;

            if (CurrentNPC.Nodes == null)
                return;

            foreach (Structs.Node node in CurrentNPC.Nodes)
            {
                if (node.Id == NextDialogueId)
                {
                    SetDialogue(node, CurrentNPC);
                    SelectDialogue(node);
                    return;
                }
            }
        }

        private void NpcList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (((e.Action == NotifyCollectionChangedAction.Add && e.NewItems == null) || (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems == null)) && e.NewItems[0].GetType() != typeof(Label))
                return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                NpcListTest.Items.Clear();
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Label item in e.OldItems)
                {
                    var ItemData = item.DataContext as Structs.NPC;
                    foreach (Label Npc in NpcListTest.Items.OfType<Label>())
                    {
                        var DialogueData = Npc.DataContext as Structs.NPC;

                        if (DialogueData.Name == ItemData.Name)
                        {
                            NpcListTest.Items.Remove(Npc);
                            return;
                        }
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Label item in e.NewItems)
                {
                    var label = new Label();
                    label.Content = item.Content;
                    label.DataContext = item.DataContext;
                    label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    label.MaxHeight = LabelHeight;
                    NpcListTest.Items.Add(label);
                }
            }
        }

        private void DialogueList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (((e.Action == NotifyCollectionChangedAction.Add && e.NewItems == null) || (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems == null)) && e.NewItems[0].GetType() != typeof(Label))
                return;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                DialogueListTest.Items.Clear();
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Label item in e.OldItems)
                {
                    var ItemData = item.DataContext as Structs.Node;
                    foreach (Label Dialogue in DialogueListTest.Items.OfType<Label>())
                    {
                        var DialogueData = Dialogue.DataContext as Structs.Node;

                        if (DialogueData.Id == ItemData.Id)
                        {
                            DialogueListTest.Items.Remove(Dialogue);
                            return;
                        }
                    }
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Label item in e.NewItems)
                {
                    var label = new Label();
                    label.Content = item.Content;
                    label.DataContext = item.DataContext;
                    label.MaxHeight = LabelHeight;
                    DialogueListTest.Items.Add(label);
                }
            }
        }

        private void NewNpcClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CreateNotification("Please create/select a project first");
                return;
            }

            var NewItems = new List<Label>();

            foreach (Label label in NpcList.Items.OfType<Label>())
            {
                if (label.Content.ToString().Contains("New Npc"))
                {
                    NewItems.Add(label);
                }
            }

            var Label = new Label();
            Label.Content = string.Format("New Npc({0})", NewItems.Count + 1);
            var newNpc = new Structs.NPC() { Name = Label.Content.ToString(), Type = "Foreground" };
            Label.DataContext = newNpc;

            if (CurrentProject.NPCS == null)
                CurrentProject.NPCS = new List<Structs.NPC>();

            CurrentProject.NPCS.Add(newNpc);
            NpcList.Items.Add(Label);
        }

        private void NewDialogueClicked(object sender, RoutedEventArgs e)
        {
            if (NpcList.SelectedItem == null || CurrentNPC == null)
            {
                CreateNotification("Please create/select an NPC first");
                return;
            }

            var NewItems = new List<Label>();

            foreach (Label label in DialogueList.Items.OfType<Label>())
            {
                if (label.Content.ToString().Contains("New Dialogue"))
                {
                    NewItems.Add(label);
                }
            }

            int NodeId = 1;
            if (CurrentNPC != null && CurrentNPC.Nodes != null)
                NodeId = CurrentNPC.Nodes.Count + 1;

            var Label = new Label();
            var NewNode = new Structs.Node() { Text = string.Format("New Dialogue({0})", NewItems.Count + 1), Id = NodeId };
            Label.Content = string.Format("New Dialogue({0})", NewItems.Count + 1);
            Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Label.MaxHeight = LabelHeight;
            Label.DataContext = NewNode;
            if (CurrentNPC.Nodes == null)
            {
                CurrentNPC.Nodes = new List<Structs.Node>() { NewNode };
                CurrentNode = NewNode;
            }
            else
            {
                CurrentNode = NewNode;
                CurrentNPC.Nodes.Add(CurrentNode);
            }
            DialogueList.Items.Add(Label);
        }

        private void NameChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentNPC == null || NpcList.SelectedItem == null)
                return;
            var label = sender as TextBox;
            CurrentNPC.Name = label.Text.ToString();
            ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Name = label.Text.ToString();
            (NpcList.SelectedItem as Label).Content = label.Text;
            ((NpcListTest.Items[NpcList.SelectedIndex] as Label).DataContext as Structs.NPC).Name = label.Text.ToString();
            (NpcListTest.Items[NpcList.SelectedIndex] as Label).Content = label.Text;
        }

        private void TypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentNPC == null || NpcList.SelectedItem == null || e.AddedItems.Count <= 0)
                return;
            var SelectionText = (e.AddedItems[0] as TypesData).Type;
            CurrentNPC.Type = SelectionText;
            ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Type = SelectionText;
        }

        private void DeleteDialogueClicked(object sender, RoutedEventArgs e)
        {
            if (DialogueList.SelectedIndex == -1 || LastTimeDialogueDeleted.AddSeconds(0.5) > DateTime.Now.ToUniversalTime())
                return;
            LastTimeDialogueDeleted = DateTime.Now.ToUniversalTime();
            DialogueBox.Text = "";
            if (CurrentNode == null)
                return;
            if (CurrentNPC.Nodes == null)
            {
                DialogueList.Items.Remove(DialogueList.SelectedItem);
                LastTimeDialogueDeleted = DateTime.Now.ToUniversalTime();
                return;
            }

            Structs.Node NextNode = new Structs.Node();
            foreach (Structs.Node node in CurrentNPC.Nodes)
            {
                if (CurrentNode.Id != node.Id)
                {
                    NextNode = node;
                    break;
                }
            }
            if (NextNode == null)
                return;

            DialogueList.Items.Remove(DialogueList.SelectedItem);
            foreach (Structs.Node node in CurrentNPC.Nodes)
            {
                if (CurrentNode.Id == node.Id)
                {
                    CurrentNPC.Nodes.Remove(node);
                    break;
                }
            }

            CurrentNode = NextNode;
            SelectDialogue(NextNode);
            foreach (Label label in DialogueList.Items)
            {
                if ((label.DataContext as Structs.Node).Id == NextNode.Id)
                {
                    ListViewItem item = DialogueList.ItemContainerGenerator.ContainerFromItem(label) as ListViewItem;
                    item.Focus();
                }
            }
            LastTimeDialogueDeleted = DateTime.Now.ToUniversalTime();
        }

        private void SelectDialogue(Structs.Node node)
        {
            foreach (Label label in DialogueList.Items)
            {
                if ((label.DataContext as Structs.Node).Id == node.Id)
                {
                    DialogueList.SelectedItem = label;
                }
            }
        }

        private void SelectNPC(Structs.NPC npc)
        {
            foreach (Label label in NpcList.Items)
            {
                if ((label.DataContext as Structs.NPC).Name == npc.Name)
                {
                    NpcList.SelectedItem = label;
                    ListViewItem item = NpcList.ItemContainerGenerator.ContainerFromItem(label) as ListViewItem;
                    item.Focus();
                }
            }
        }

        private void DialogueEdited(object sender, TextChangedEventArgs e)
        {
            if ((CurrentNode == null || CurrentNPC == null) || NpcList.SelectedItem == null || ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes == null || DialogueList.SelectedItem == null)
                return;
            var label = sender as TextBox;
            int NpcNodeToEdit = -1;
            foreach (Structs.Node node in ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes)
            {
                if (node.Id == CurrentNode.Id)
                    NpcNodeToEdit = ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes.IndexOf(node);
            }
            if (NpcNodeToEdit == -1)
                return;
            CurrentNode.Text = label.Text.ToString();
            ((DialogueList.SelectedItem as Label).DataContext as Structs.Node).Text = label.Text;
            ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes[NpcNodeToEdit].Text = label.Text;
            (DialogueList.SelectedItem as Label).Content = label.Text;
            //((DialogueList.SelectedItem as Label).DataContext as Structs.Node).Text = label.Text;

            DialogueText.Text = label.Text.ToString();
            ((DialogueListTest.Items[DialogueList.SelectedIndex] as Label).DataContext as Structs.Node).Text = label.Text;
            ((NpcListTest.Items[NpcList.SelectedIndex] as Label).DataContext as Structs.NPC).Nodes[NpcNodeToEdit].Text = label.Text;
            (DialogueListTest.Items[DialogueList.SelectedIndex] as Label).Content = label.Text;
            ((DialogueListTest.Items[DialogueList.SelectedIndex] as Label).DataContext as Structs.Node).Text = label.Text;
        }

        private void NextNodeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem == null || CurrentNPC.Nodes == null)
                return;
            ComboBox comboBox = sender as ComboBox;
            int comboBoxIndex = int.Parse(comboBox.Name.Substring(comboBox.Name.Length - 1, 1)) - 1;
            TextBox ConnectedButton = FindName("ButtonBox" + (comboBoxIndex + 1)) as TextBox;
            int ActuallIndex = 0;
            if (CurrentNode.Choices != null)
            {
                ActuallIndex = Math.Min(comboBoxIndex, CurrentNode.Choices.Count);
            }
            var SelectedData = (Structs.Node)(comboBox.SelectedItem as Label).DataContext;

            if (ConnectedButton == null || (CurrentNode.Choices == null && SelectedData.Id == -2) || (SelectedData.Id == -2 && CurrentNode.Choices.Count <= ActuallIndex))
                return;

            int OldNodeIndex = 0; // Math.Max(CurrentNPC.Nodes.IndexOf(CurrentNode), 0);
            foreach (Structs.Node node in CurrentNPC.Nodes)
            {
                if (node.Id == CurrentNode.Id)
                    OldNodeIndex = CurrentNPC.Nodes.IndexOf(node);
            }

            var ChoiceText = TextChoice;
            if (ConnectedButton.Text.Length > 0)
                ChoiceText = ConnectedButton.Text.ToString();
            else if (ConnectedButton.Text.Length <= 0)
                ConnectedButton.Text = ChoiceText;
            else if (CurrentNode.Choices != null && CurrentNode.Choices.Count < ActuallIndex)
                ChoiceText = CurrentNode.Choices[ActuallIndex].Text;

            var Choice = new Structs.Choice() { Next_id = SelectedData.Id, Text = ChoiceText };

            if (SelectedData.Id == -2 && CurrentNode.Choices.Count > ActuallIndex)
            {
                CurrentNode.Choices.RemoveAt(ActuallIndex);
                CurrentNPC.Nodes[OldNodeIndex] = CurrentNode;
                (DialogueList.SelectedItem as Label).DataContext = CurrentNode;
                ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes[OldNodeIndex] = CurrentNode;
                SetTestButtons(CurrentNode);
                return;
            }

            if (CurrentNode.Choices == null)
                CurrentNode.Choices = new List<Structs.Choice>() { Choice };
            else if (CurrentNode.Choices.Count <= ActuallIndex)
                CurrentNode.Choices.Add(Choice);
            else
                CurrentNode.Choices[ActuallIndex] = Choice;
            CurrentNPC.Nodes[OldNodeIndex] = CurrentNode;
            (DialogueList.SelectedItem as Label).DataContext = CurrentNode;
            ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes[OldNodeIndex] = CurrentNode;
            SetTestButtons(CurrentNode);
        }

        private void ChoiceTextChanged(object sender, TextChangedEventArgs e)
        {
            if ((sender as TextBox).Text == null || DialogueList.SelectedItem == null || (CurrentNode == null || CurrentNPC == null || CurrentNPC.Nodes == null))
                return;
            TextBox ChoiceTextBox = sender as TextBox;
            int comboBoxIndex = int.Parse(ChoiceTextBox.Name.Substring(ChoiceTextBox.Name.Length - 1, 1)) - 1;
            ComboBox ConnectedCombo = FindName("ButtonCombo" + (comboBoxIndex + 1)) as ComboBox;
            int ActuallIndex = 0;
            if (CurrentNode.Choices != null)
            {
                ActuallIndex = Math.Min(comboBoxIndex, CurrentNode.Choices.Count);
            }

            var SelectedData = new Structs.Node() { Id = -1, Text = string.Format("({0})", "Exit the Dialogue") };

            if (ConnectedCombo.SelectedItem != null)
                SelectedData = (Structs.Node)(ConnectedCombo.SelectedItem as Label).DataContext;

            int OldNodeIndex = 0; // Math.Max(CurrentNPC.Nodes.IndexOf(CurrentNode), 0);
            foreach (Structs.Node node in CurrentNPC.Nodes)
            {
                if (node.Id == CurrentNode.Id)
                    OldNodeIndex = CurrentNPC.Nodes.IndexOf(node);
            }

            if (SelectedData.Id == -2 && CurrentNode.Choices.Count <= ActuallIndex)
                return;

            if (SelectedData.Id == -2 && CurrentNode.Choices.Count > ActuallIndex)
            {
                CurrentNode.Choices.RemoveAt(ActuallIndex);
                CurrentNPC.Nodes[OldNodeIndex] = CurrentNode;
                (DialogueList.SelectedItem as Label).DataContext = CurrentNode;
                ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes[OldNodeIndex] = CurrentNode;
                SetTestButtons(CurrentNode);
                return;
            }

            var ChoiceText = TextChoice;
            if (ChoiceTextBox.Text.Length > 0)
                ChoiceText = ChoiceTextBox.Text.ToString();
            else if (CurrentNode.Choices != null && CurrentNode.Choices.Count > ActuallIndex)
                ChoiceText = CurrentNode.Choices[ActuallIndex].Text;
            else
                return;

            var Choice = new Structs.Choice() { Next_id = SelectedData.Id, Text = ChoiceText };

            if (CurrentNode.Choices == null)
                CurrentNode.Choices = new List<Structs.Choice>() { Choice };
            else if (CurrentNode.Choices.Count <= ActuallIndex)
                CurrentNode.Choices.Add(Choice);
            else
                CurrentNode.Choices[ActuallIndex] = Choice;
            CurrentNPC.Nodes[OldNodeIndex] = CurrentNode;
            (DialogueList.SelectedItem as Label).DataContext = CurrentNode;
            ((NpcList.SelectedItem as Label).DataContext as Structs.NPC).Nodes[OldNodeIndex] = CurrentNode;
            SetTestButtons(CurrentNode);
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CreateNotification("Please create/select a project first");
                return;
            }

            File.WriteAllText(Utils.GetProjectPath(CurrentProject.Name), JsonSerializer.Serialize(GetCurrentProject()));
            CreateNotification("All information has been saved");
        }

        private Structs.Project GetCurrentProject()
        {
            var Npcs = new Structs.Project() { NPCS = new List<Structs.NPC>() };
            foreach (Label label in NpcList.Items.OfType<Label>())
            {
                var Data = label.DataContext as Structs.NPC;

                if (Data.Nodes == null)
                    continue;

                foreach (Structs.Node node in Data.Nodes)
                {
                    if (node.Choices == null)
                    {
                        node.Choices = new List<Structs.Choice> { new Structs.Choice() { Next_id = -1, Text = TextChoice } };
                    }
                }

                Npcs.NPCS.Add(Data);
            }

            foreach (TypesData type in NpcTypesList.Items)
            {
                Npcs.NpcTypes.Add(type.Type);
            }

            Npcs.Name = CurrentProject.Name;
            Npcs.Exportpath = CurrentProject.Exportpath;
            Npcs.Importpath = CurrentProject.Importpath;
            return Npcs;
        }

        private void DeleteNpcClicked(object sender, RoutedEventArgs e)
        {
            if (NpcList.SelectedIndex == -1 || LastTimeNpcDeleted.AddSeconds(0.5) > DateTime.Now.ToUniversalTime())
                return;
            LastTimeNpcDeleted = DateTime.Now.ToUniversalTime();
            NpcNameBox.Text = "";
            if (CurrentNPC == null)
                return;

            if (CurrentNPC.Nodes != null)
                CurrentNPC.Nodes.Clear();
            DialogueList.Items.Clear();

            NpcList.Items.Remove(NpcList.SelectedItem);
            foreach (Structs.NPC Npc in CurrentProject.NPCS)
            {
                if (CurrentNPC.Name == Npc.Name)
                {
                    CurrentProject.NPCS.Remove(Npc);
                    break;
                }
            }

            Structs.NPC NextNpc = new Structs.NPC();
            foreach (Structs.NPC Npc in CurrentProject.NPCS)
            {
                if (CurrentNPC.Name != Npc.Name)
                {
                    NextNpc = Npc;
                    break;
                }
            }
            if (NextNpc == null)
                return;

            var CurrentNodeId = 0;
            if (CurrentNode != null)
                CurrentNodeId = CurrentNode.Id;
            Structs.Node NextNode = new Structs.Node();
            if (NextNpc == null || NextNpc.Nodes == null)
                goto ConfirmData;
            foreach (Structs.Node node in NextNpc.Nodes)
            {
                if (CurrentNodeId != node.Id)
                {
                    NextNode = node;
                    break;
                }
            }
            if (NextNode == null)
                return;

            ConfirmData:
            CurrentNPC = NextNpc;
            CurrentNode = NextNode;
            SelectDialogue(NextNode);
        }


        //Project Tab Code: -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        private async void LoadProjectData()
        {
            var label = new Label();
            label.Content = "(New)";
            label.DataContext = "New";
            label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            label.MaxHeight = LabelHeight;
            ProjectsBox.Items.Add(label);

            if (!Utils.UserDataPath.Exists)
                Utils.UserDataPath.Create();

            foreach (DirectoryInfo file in Utils.UserDataPath.GetDirectories())
            {
                if (!File.Exists(Utils.GetProjectPath(file.Name)))
                    continue;

                var FileText = await System.IO.File.ReadAllTextAsync(Utils.GetProjectPath(file.Name));

                var ProjectData = JsonSerializer.Deserialize<Structs.Project>(FileText);

                if(ProjectData.NpcTypes.Count <= 0)
                {
                    ProjectData.NpcTypes = new List<string>() { "Foreground", "Background" };
                }

                label = new Label();
                label.DataContext = ProjectData;
                label.Content = ProjectData.Name;
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                label.MaxHeight = LabelHeight;
                ProjectsBox.Items.Add(label);

                ProjectNameBox.Text = ProjectData.Name;

                if (Utils.UserDataPath.GetDirectories().Last().FullName == file.FullName || !File.Exists(Utils.GetProjectPath(file.Name)))
                {
                    CurrentProject = ProjectData;
                    SetData();

                    ProjectsBox.SelectedItem = label;
                }
            }

            if (ProjectsBox.Items.Count <= 1)
                ProjectsBox.SelectedIndex = 0;
        }

        private void ProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count <= 0 || (e.AddedItems[0] as Label).DataContext == null)
                return;

            var SelectedItem = (e.AddedItems[0] as Label);

            if (SelectedItem.DataContext.ToString() == "New")
            {
                CurrentProject = null;
                ProjectCreate.IsEnabled = true;
                ProjectCreate.Content = "Create";
                ProjectNameBox.Text = "";
                ClearAll();
                return;
            }
            else
            {
                ProjectCreate.IsEnabled = true;
                ProjectCreate.Content = "Save";
            }

            var SelectedData = SelectedItem.DataContext as Structs.Project;

            foreach (string type in SelectedData.NpcTypes)
            {
                NpcTypesList.Items.Add(new TypesData() { Type = type });
                NpcTypeBox.Items.Add(new TypesData() { Type = type });
            }

            if (SelectedData.Exportpath != null)
                ExportPathBox.Text = SelectedData.Exportpath;
            else
                ExportPathBox.Text = "Press here to set the Export path";

            CurrentProject = SelectedData;
            ProjectNameBox.Text = CurrentProject.Name;
            SetData();
        }

        private void CreateProjectClicked(object sender, RoutedEventArgs e)
        {
            var ProjectName = ProjectNameBox.Text;

            Structs.Project SelectedProj = new Structs.Project();

            if ((ProjectsBox.SelectedItem as Label).DataContext.ToString() != "New")
                SelectedProj = (Structs.Project)(ProjectsBox.SelectedItem as Label).DataContext;

            if (ProjectName.Length <= 0)
            {
                CreateNotification("Please enter a name for your Project first");
                return;
            }

            if (!Utils.UserDataPath.GetDirectories().Any(s => ProjectName.Equals(s.Name)) && ProjectCreate.Content.ToString() == "Create")
            {
                var NewProject = new Structs.Project();
                NewProject.Name = ProjectName;
                CurrentProject = NewProject;

                var label = new Label();
                label.DataContext = NewProject;
                label.Content = NewProject.Name;
                label.Padding = new Thickness(3);
                label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                label.MaxHeight = label.DesiredSize.Height;
                ProjectsBox.Items.Add(label);
                ProjectsBox.SelectedItem = label;

                NpcTypesList.Items.Add(new TypesData(){ Type = "Foreground"});
                NpcTypesList.Items.Add(new TypesData(){ Type = "Background"});
                UpdateNpcTypes();

                SetData();

                CreateNotification("A new Project has been created");

                Directory.CreateDirectory(Path.Combine(Utils.UserDataPath.FullName, NewProject.Name));
                File.WriteAllText(Utils.GetProjectPath(NewProject.Name), JsonSerializer.Serialize(NewProject));
            }
            else if (ProjectCreate.Content.ToString() == "Save" && !Utils.UserDataPath.GetDirectories().Any(s => ProjectName.Equals(s.Name)))
            {
                foreach (DirectoryInfo Dir in Utils.UserDataPath.GetDirectories())
                {
                    if (!File.Exists(Utils.GetProjectPath(Dir.Name)))
                        continue;

                    var FileText = System.IO.File.ReadAllText(Utils.GetProjectPath(Dir.Name));

                    var ProjectData = JsonSerializer.Deserialize<Structs.Project>(FileText);

                    if (ProjectData.Name == SelectedProj.Name)
                    {
                        CurrentProject = GetCurrentProject();

                        CurrentProject.Name = ProjectName;

                        if (ExportPathBox.DataContext != null)
                            CurrentProject.Exportpath = ExportPathBox.Text;

                        var NewDir = Directory.CreateDirectory(Path.Combine(Utils.UserDataPath.FullName, ProjectName));

                        File.WriteAllText(Utils.GetProjectPath(Dir.Name), JsonSerializer.Serialize(CurrentProject));
                        new FileInfo(Utils.GetProjectPath(Dir.Name)).MoveTo(Path.Combine(NewDir.FullName, ProjectName + ".Json"));
                        Dir.Delete(true);
                        (ProjectsBox.SelectedItem as Label).DataContext = CurrentProject;
                        (ProjectsBox.SelectedItem as Label).Content = ProjectName;
                        var selectedItem = ProjectsBox.SelectedItem;
                        ProjectsBox.SelectedIndex = -1;
                        ProjectsBox.SelectedItem = selectedItem;

                        CreateNotification("All information has been saved");
                    }
                }
            }
            else if (ProjectCreate.Content.ToString() == "Save" && Utils.UserDataPath.GetDirectories().Any(s => ProjectName.Equals(s.Name)) && CurrentProject.Name == ProjectName)
            {
                foreach (DirectoryInfo Dir in Utils.UserDataPath.GetDirectories())
                {
                    if (!File.Exists(Utils.GetProjectPath(Dir.Name)))
                        continue;

                    var FileText = System.IO.File.ReadAllText(Utils.GetProjectPath(Dir.Name));

                    var ProjectData = JsonSerializer.Deserialize<Structs.Project>(FileText);

                    if (ProjectData.Name == CurrentProject.Name)
                    {
                        CurrentProject = GetCurrentProject();

                        CurrentProject.Name = ProjectName;

                        if (ExportPathBox.DataContext != null)
                            CurrentProject.Exportpath = ExportPathBox.Text;

                        File.WriteAllText(Utils.GetProjectPath(Dir.Name), JsonSerializer.Serialize(CurrentProject));
                        (ProjectsBox.SelectedItem as Label).DataContext = CurrentProject;

                        CreateNotification("All information has been saved");
                    }
                }
            }
            else
            {
                CreateNotification("Project with this name already exists", Structs.NotifType.Warning);
            }
        }

        private void ExportPathGotFocus(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CreateNotification("Please select a Project first");
                UnfocusElement.Focus();
                return;
            }

            var ExportBox = sender as TextBox;

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();

            if (result.ToString() == "Ok")
            {
                ExportBox.Text = dialog.FileName;
                ExportBox.DataContext = new DirectoryInfo(dialog.FileName);

                if (CurrentProject != null)
                {
                    CurrentProject.Exportpath = dialog.FileName;


                    //string path = Path.Combine(Utils.GetProjectPath(CurrentProject.Name));
                    //File.WriteAllText(Utils.GetProjectPath(CurrentProject.Name), JsonSerializer.Serialize(CurrentProject));
                }
            }
            UnfocusElement.Focus();
        }

        private void ExportClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null)
            {
                CreateNotification("Please select a Project first");
                return;
            }
            if (CurrentProject.Exportpath == null)
            {
                ExportPathGotFocus(ExportPathBox, new RoutedEventArgs(GotFocusEvent));
            }
            if (ExportPathBox.DataContext == null && CurrentProject.Exportpath == null)
                return;
            File.WriteAllText(Path.Combine(CurrentProject.Exportpath, CurrentProject.Name + ".Json"), JsonSerializer.Serialize(GetCurrentProject()));
            CreateNotification("Project has been exported successfully");
        }

        private void DeleteProjectClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentProject == null || ProjectsBox.SelectedItem == null)
            {
                CreateNotification("Please select a Project first");
                return;
            }

            var Result = MessageBox.Show("You are about to DELETE this whole project, there is NO GOING BACK, are you sure you want to do this?", "Delete this Project", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (Result == MessageBoxResult.Yes)
            {
                ProjectsBox.Items.Remove(ProjectsBox.SelectedItem);
                Directory.Delete(new FileInfo(Utils.GetProjectPath(CurrentProject.Name)).Directory.FullName, true);
                CurrentProject = null;
                ProjectNameBox.Text = "";
                ExportPathBox.Text = "";
                NpcList.Items.Clear();
                DialogueList.Items.Clear();
                ClearAll();
                CreateNotification("Project has been deleted");
                if (ProjectsBox.Items.Count <= 1)
                    ProjectsBox.SelectedIndex = 0;
            }
            else
                return;
        }

        private void ClearAll()
        {
            for (int i = 1; i <= 4; i++)
            {
                var comboBox = FindName("ButtonCombo" + i) as ComboBox;
                var textBox = FindName("ButtonBox" + i) as TextBox;

                comboBox.SelectedIndex = -1;
                comboBox.ItemsSource = null;
                textBox.Text = "";
            }
            NpcList.Items.Clear();
            DialogueList.Items.Clear();
            NpcNameBox.Text = "";
            DialogueBox.Text = "";
            DialogueText.Text = "";
            NpcTypesList.Items.Clear();
        }

        private void MinimalizeClicked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            switch (WindowState)
            {
                case WindowState.Maximized:
                    WindowBorder.Margin = new Thickness(0);
                    this.WindowState = WindowState.Normal;
                    button.Content = "◻";
                    break;
                case WindowState.Normal:
                    WindowBorder.Margin = new Thickness(5);
                    this.WindowState = WindowState.Maximized;
                    button.Content = "❏";
                    break;
            }
        }

        private void ExitClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MouseTabDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void AutoSaveTimePreviewInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void AutoSaveStateChanged(object sender, RoutedEventArgs e)
        {
            if (UserData == null && File.Exists(Path.Combine(Utils.UserDataPath.FullName, "UserData.json")))
                return;

            CheckBox check = sender as CheckBox;

            AutoSaveState = check.IsChecked.Value;

            SaveUserData();

            if (AutoSavefunc == null)
                AutoSavefunc = SetAutoSaveEvent(AutoSaveMinutes);
        }

        private void AutoSaveNotifClicked(object sender, RoutedEventArgs e)
        {
            CheckBox check = sender as CheckBox;

            AutoSaveNotif = check.IsChecked.Value;

            SaveUserData();
        }

        private void AutoSaveTimeChanged(object sender, TextChangedEventArgs e)
        {
            if (UserData == null && File.Exists(Path.Combine(Utils.UserDataPath.FullName, "UserData.json")))
                return;

            var AutoSaveBox = sender as TextBox;

            if (AutoSaveBox.Text == "0")
            {
                //AutoSaveBox.Text = "1";
                AutoSaveCheckBox.IsChecked = false;
                AutoSaveCheckBox.IsEnabled = false;

                AutoSaveMinutes = int.Parse(AutoSaveBox.Text);

                SaveUserData();

                return;
            }
            else if(AutoSaveBox.Text.Contains(" ") || AutoSaveBox.Text.Length <= 0)
            {
                AutoSaveCheckBox.IsChecked = false;
                AutoSaveCheckBox.IsEnabled = false;

                AutoSaveMinutes = 0;

                SaveUserData();

                return;
            }

            AutoSaveCheckBox.IsEnabled = true;

            AutoSaveMinutes = int.Parse(AutoSaveBox.Text);

            SaveUserData();

            AutoSavefunc = SetAutoSaveEvent(AutoSaveMinutes);
        }

        private async Task SetAutoSaveEvent(int delay)
        {
            await Task.Delay(delay * 60000);
            if (delay == AutoSaveMinutes)
                AutoSave();
            else
                return;
            _ = SetAutoSaveEvent(AutoSaveMinutes);
        }

        private void AutoSave()
        {
            if (CurrentProject == null || AutoSaveState == false)
                return;

            File.WriteAllText(Utils.GetProjectPath(CurrentProject.Name), JsonSerializer.Serialize(GetCurrentProject()));
            if (AutoSaveNotif)
                CreateNotification("All information has been saved (Autosave)");
        }

        private void SaveUserData()
        {
            var userData = new Structs.UserSettings();

            userData.IsAutoSave = AutoSaveState;
            userData.AutoSaveMinutes = AutoSaveMinutes;
            userData.IsAutoSaveNotif = AutoSaveNotif;
            userData.Style = Style;

            userData.DefaultChoiceText = TextChoice;

            UserData = userData;

            if(Directory.Exists(Utils.UserDataPath.FullName))
                File.WriteAllText(Path.Combine(Utils.UserDataPath.FullName, "UserData.json"), JsonSerializer.Serialize(userData));
        }

        private void ReadUserData()
        {
            if (!File.Exists(Path.Combine(Utils.UserDataPath.FullName, "UserData.json")))
                return;

            var UserDataString = System.IO.File.ReadAllText(Path.Combine(Utils.UserDataPath.FullName, "UserData.json"));

            UserData = JsonSerializer.Deserialize<Structs.UserSettings>(UserDataString);

            if (UserData.DefaultChoiceText == null)
                UserData.DefaultChoiceText = "Continue";

            AutoSaveState = UserData.IsAutoSave;
            AutoSaveMinutes = UserData.AutoSaveMinutes;
            AutoSaveNotif = UserData.IsAutoSaveNotif;
            TextChoice = UserData.DefaultChoiceText;
            Style = UserData.Style;

            AutoSaveCheckBox.IsChecked = UserData.IsAutoSave;
            AutoSaveTextBox.Text = UserData.AutoSaveMinutes.ToString();
            AutoSaveNotifBox.IsChecked = UserData.IsAutoSaveNotif;
            DefChoiceTextBox.Text = UserData.DefaultChoiceText;
            DarkModeCheckBox.IsChecked = UserData.Style == Structs.Styles.Dark;

            SetStyle(Style);

            return;
        }

        private void NpcSearchBoxChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string SearchQuery = textBox.Text.ToLower();

            CurrentProject = GetCurrentProject();

            foreach (Label label in NpcList.Items)
            {
                if (label.Content.ToString().ToLower().Contains(SearchQuery))
                {
                    NpcList.SelectedItem = label;
                }
            }
        }

        private void DialogueSearchBoxChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string SearchQuery = textBox.Text.ToLower();

            CurrentProject = GetCurrentProject();

            foreach (Label label in DialogueList.Items)
            {
                if (label.Content.ToString().ToLower().Contains(SearchQuery))
                {
                    DialogueList.SelectedItem = label;
                }
            }
        }

        private void ProjectNameKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Enter)
            {
                CreateProjectClicked(null, null);

                UnfocusElement.Focus();
            }
        }

        private void UnFocusOnEnter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                UnfocusElement.Focus();
                if (sender == NpcNameBox)
                {
                    NameChanged(sender, new System.Windows.Controls.TextChangedEventArgs(e.RoutedEvent, UndoAction.Undo));
                }
            }
        }

        private void NpcNameBoxLostFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            NameChanged(sender, new System.Windows.Controls.TextChangedEventArgs(e.RoutedEvent, UndoAction.Undo));
        }

        private void DefaultTextChoiceChanged(object sender, TextChangedEventArgs e)
        {
            var ChoiceTextBox = sender as TextBox;
            var text = ChoiceTextBox.Text;

            if (text == "Continue")
                return;

            TextChoice = text;

            SaveUserData();
        }

        private void WindowKeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.S))
            {
                SaveClicked(sender, null);
            }
        }

        private void SetStyle(Structs.Styles style)
        {
            switch (style)
            {
                case Structs.Styles.Dark:
                    Resources.MergedDictionaries[0]["BackgroundColor"] = ColorConverter.ConvertFromString("#191D1E");
                    Resources.MergedDictionaries[0]["LightBackgroundColor"] = ColorConverter.ConvertFromString("#3B3B3B");
                    Resources.MergedDictionaries[0]["SecondaryLightColor"] = ColorConverter.ConvertFromString("#28CA6C");
                    Resources.MergedDictionaries[0]["SecondaryDarkColor"] = ColorConverter.ConvertFromString("#4F4F4F");
                    Resources.MergedDictionaries[0]["SecondaryColor"] = ColorConverter.ConvertFromString("#585858");
                    Resources.MergedDictionaries[0]["DefaultFontColor"] = ColorConverter.ConvertFromString("#F0F0F0");
                    //Colors below are the same in Dark and Light/Color mode
                    Resources.MergedDictionaries[0]["HoverGradientStartButtonColor"] = ColorConverter.ConvertFromString("#E040FB");
                    Resources.MergedDictionaries[0]["DefaultButtonBackgroundColor"] = ColorConverter.ConvertFromString("#47D48D");
                    Resources.MergedDictionaries[0]["DefaultButtonBackgroundHoverColor"] = ColorConverter.ConvertFromString("#589371");
                    Resources.MergedDictionaries[0]["ButtonColorColor"] = ColorConverter.ConvertFromString("#28CA6C");
                    Resources.MergedDictionaries[0]["HoverButtonColor"] = ColorConverter.ConvertFromString("#45DE7E");
                    Resources.MergedDictionaries[0]["DeleteButtonColorColor"] = ColorConverter.ConvertFromString("#FF424C");
                    Resources.MergedDictionaries[0]["DeleteButtonColorHoverColor"] = ColorConverter.ConvertFromString("#FC5C65");
                    Resources.MergedDictionaries[0]["GradientStartButtonColor"] = ColorConverter.ConvertFromString("#E040FB");
                    Resources.MergedDictionaries[0]["GradientStopButtonColor"] = ColorConverter.ConvertFromString("#AD1457");
                    Resources.MergedDictionaries[0]["NotifWarningColor"] = ColorConverter.ConvertFromString("#DA4167");
                    break;
                case Structs.Styles.Light:
                    Resources.MergedDictionaries[0]["BackgroundColor"] = ColorConverter.ConvertFromString("#F0F0F0");
                    Resources.MergedDictionaries[0]["LightBackgroundColor"] = ColorConverter.ConvertFromString("#08BDBD");
                    Resources.MergedDictionaries[0]["SecondaryLightColor"] = ColorConverter.ConvertFromString("#FFA400");
                    Resources.MergedDictionaries[0]["SecondaryDarkColor"] = ColorConverter.ConvertFromString("#6A1E54");
                    Resources.MergedDictionaries[0]["SecondaryColor"] = ColorConverter.ConvertFromString("#84396F");
                    Resources.MergedDictionaries[0]["DefaultFontColor"] = ColorConverter.ConvertFromString("#191D1E");
                    //Colors below are the same in Dark and Light/Color mode
                    Resources.MergedDictionaries[0]["HoverGradientStartButtonColor"] = ColorConverter.ConvertFromString("#E040FB");
                    Resources.MergedDictionaries[0]["DefaultButtonBackgroundColor"] = ColorConverter.ConvertFromString("#47D48D");
                    Resources.MergedDictionaries[0]["DefaultButtonBackgroundHoverColor"] = ColorConverter.ConvertFromString("#589371");
                    Resources.MergedDictionaries[0]["ButtonColorColor"] = ColorConverter.ConvertFromString("#8FB339");
                    Resources.MergedDictionaries[0]["HoverButtonColor"] = ColorConverter.ConvertFromString("#9FA349");
                    Resources.MergedDictionaries[0]["DeleteButtonColorColor"] = ColorConverter.ConvertFromString("#FF424C");
                    Resources.MergedDictionaries[0]["DeleteButtonColorHoverColor"] = ColorConverter.ConvertFromString("#FC5C65");
                    Resources.MergedDictionaries[0]["GradientStartButtonColor"] = ColorConverter.ConvertFromString("#E040FB");
                    Resources.MergedDictionaries[0]["GradientStopButtonColor"] = ColorConverter.ConvertFromString("#AD1457");
                    Resources.MergedDictionaries[0]["NotifWarningColor"] = ColorConverter.ConvertFromString("#DA4167");
                    break;
            }
        }

        private void DarkModeCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            bool isChecked = (bool)(sender as CheckBox).IsChecked;
            if (isChecked)
            {
                SetStyle(Structs.Styles.Dark);
                Style = Structs.Styles.Dark;
                SaveUserData();
            }
            else
            {
                SetStyle(Structs.Styles.Light);
                Style = Structs.Styles.Light;
                SaveUserData();
            }
        }

        private void NewTypeClicked(object sender, RoutedEventArgs e)
        {
            NpcTypesList.Items.Add(new TypesData() { Type = "Type" });
            CurrentProject.NpcTypes.Add("Type");
            NpcTypeBox.Items.Add(new TypesData() { Type = "Type" });
        }

        private void RemoveTypeClicked(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            foreach(TypesData type in NpcTypesList.Items)
            {
                if (type == (TypesData)btn.Tag)
                {
                    NpcTypesList.Items.Remove(type);
                    NpcTypeBox.Items.Remove((TypesData)btn.Tag);
                    break;
                }
            }

            CurrentProject.NpcTypes.Remove(((TypesData)btn.Tag).Type);

            UpdateNpcTypes();
        }

        private void UpdateNpcTypes()
        {
            CurrentProject.NpcTypes = new List<string>();
            NpcTypeBox.Items.Clear();
            foreach (TypesData type in NpcTypesList.Items)
            {
                CurrentProject.NpcTypes.Add(type.Type);
                NpcTypeBox.Items.Add(type);
            }
        }

        private void TypeTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateNpcTypes();
        }
    }
}