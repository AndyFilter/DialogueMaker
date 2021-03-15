# DialogueMaker

[![GitHub top language](https://img.shields.io/github/languages/top/AndyFilter/DialogueMaker?style=flat-square)](https://en.wikipedia.org/wiki/C_Sharp_(programming_language))  [![Windows](https://img.shields.io/badge/platform-Windows-0078d7.svg?style=flat-square)](https://en.wikipedia.org/wiki/Microsoft_Windows) ![GitHub all releases](https://img.shields.io/github/downloads/AndyFilter/DialogueMaker/total?style=flat-square) [ ![PayPal](https://img.shields.io/badge/donate-PayPal-orange.svg?style=flat-square&logo=PayPal)](https://www.paypal.me/MaciejGrzeda)
### Makes creating Dialogue System for Your game just a bit easier.

# Installation
To install go to the [**releases page**](https://github.com/AndyFilter/DialogueMaker/releases/latest); You'll find 2 files there, one named **DialogueMaker.exe** and the other one named **DialogueMakerNet.exe**. Try downloading the **first one**, if it **wont work** for some reason then try with the **second one**. If both wont work, then contact me, or just compile the code Yourself. The program runs on **.NET 5**

# How to use
I think I'm gonna do some more explaining in the program itself for now even though it's pretty self explanatory. All You do is **create a project**, go to the *Create Tab* **create some dialogues** and **test them** in the *Test Tab*. When You think You are done save the dialogues, go back to the *Project Tab*, select the desired export path and press ***Export***.

Now that You have a *.Json* file all You need to do is import it into Your game and code Your *Dialogue System* for it. Examples of the code are in the ***Implementation Part***.

# Implementation
<details><summary>Godot Code</summary>
<p>
	
**All You need to do to get Your dialogues to work is to create all the required UI elements in Your dialogue scene like this:**
 
## ![image](https://user-images.githubusercontent.com/69699046/110820732-a8de3080-828f-11eb-8ec2-608034a339ef.png)


***DialogueBox Scene Code***. This is the code that is attached to the *DialogueUI* element on the photo above.
```python
extends Panel

var DialogueDir = "res://Assets/Dialogues/"
var File_name = "Name.json" #Name of the file + ".json"! / This file has to be in the directory declared one line above!
var Nodes
var NPCName = ""
var NPCS;
var NPC;


var curent_node_id = -1
var curent_node_name
var curent_node_text
var curent_node_choices = []

onready var dialogueButtons = [$Control/DialogueButton,$Control/DialogueButton2,$Control/DialogueButton3,$Control/DialogueButton4] #References to all the buttons
onready var dialogueText = $DialogueText #Reference to the label that will be showing the text of the dialogue
onready var dialogueName = $DialogueName #Reference to the label thet will be showing the name of the NPC
onready var dialoguePanel = self


func _ready():
	rand_seed(OS.get_unix_time())
	LoadFile(File_name)
	StartDialogue()

func LoadFile(fileName):
	File_name = fileName
	var file = File.new()
	if file.file_exists(DialogueDir + File_name):
		file.open(DialogueDir + File_name, file.READ)
		var json_result = parse_json(file.get_as_text())
		curent_node_id = 1
		NPCS = json_result["NPCS"]
		for npc in NPCS:
			if(npc["Name"] == NPCName):
				NPC = npc
				Nodes = npc["Nodes"]

func StartDialogue():
	if Nodes:
		curent_node_name = NPCName
		curent_node_id = 1 #Sets the id of the first Node, which is 1
		HandleNode()
	else:
		print("Dialogue: Could not Find Nodes")

func EndDialogue():
	curent_node_id = -1
	get_tree().get_current_scene().get_node("Player").isInDialogue = false

func NextNode(id):
	curent_node_id = id
	HandleNode()

func HandleNode():
	if curent_node_id < 1 :
		EndDialogue()
	else:
		if !GetNode(curent_node_id):
			EndDialogue()
	UpdateUI()

func GetNode(id):
	for node in Nodes:
		if int(node["Id"]) == id:
			curent_node_text = node["Text"]
			curent_node_choices = node["Choices"]
			return true
	return false

func UpdateUI():
	if curent_node_id >= 0:
		dialoguePanel.show()
		for x in dialogueButtons:
			x.hide()
			if x.is_connected("pressed",self,"_on_Button_Pressed"):
				x.disconnect("pressed",self,"_on_Button_Pressed")

		dialogueText.percent_visible = 0	
		dialogueName.text = curent_node_name
		dialogueText.text = curent_node_text
		if curent_node_choices.size() > 0:
			for x in clamp(curent_node_choices.size(),0,3):
				dialogueButtons[x].text = curent_node_choices[x]["Text"]
				dialogueButtons[x].connect("pressed",self,"_on_Button_Pressed", [curent_node_choices[x]["Next_id"]])
				dialogueButtons[x].show()

	else:
		dialoguePanel.hide()

func _on_Button_Pressed(id):
	NextNode(id)
```
##

**Now the last, but not least You will need to open the dialogue box, so here's the code for it**. You can run this code for example when the player gets near the NPC, or interacts with one.
```python
var DialBox = DialogueBox.instance() #Creates a instance on the DialogueBox Scene
DialBox.get_node("DialogueUI").NPCName = Name; #Sets the name of the NPC
get_tree().get_current_scene().call_deferred("add_child", DialBox) #Adds the DialogueBox to the view
```
*Note that you will have to position the window on the screen so its visible.*

</p>
</details>



# To Do

 - ~~Better user interface (Darkmode etc.)~~
 - ~~User response dialogue~~
 - ~~Option to rename the project~~
 - ~~Auto-Save function~~
 - ~~Search function (For NPC and Dialogue)~~
 - ~~Json object instructions to [README.md](https://github.com/AndyFilter/DialogueMaker/blob/main/README.md)~~
 - ~~Customization and user settings~~

# Pull requests
Feel free to do pull requests, and edit the code hoverer You like. The software is distributed under a **MIT license**.

# Contact
Preferable method of contact would be my discord: ***IsPossible#8212***. You can **send me anything** you want, from **issues** regarding the software through **new features** to add and even **pictures of Your cat**. If I'll be getting too many messages I'll think about making a Discord Server, but I don't expect this program to get more than 100 downloads.

# Other Information
There is not much to say about that except that if You want **more frequent updates** and fixes, please **consider [Donating](https://www.paypal.me/MaciejGrzeda)**. Also, sorry for the code being a bit messy, I don't think I'll fix that anytime soon 😅.