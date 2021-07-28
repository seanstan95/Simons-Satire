using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using KModkit;

public class SimonsSatireModule : MonoBehaviour
{
	public static int totalNum = 0;
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio Audio;
    public KMSelectable RedButton, BlueButton, GreenButton, YellowButton, LeftArrow, RightArrow;
	public TextAsset simon, talk, phrases, colors;
    public TextMesh phraseText, pageText;

	private bool active = true, specialCase = false, special10 = false, valid = false, special = false;
	private int sequenceNum = 0, phase, count, column, thisNum = 0;
	private readonly List<int> randomNums = new List<int>();
	private string currentPhrase, winSound = "";
	private readonly string[] colorInputs = new string[3];
	private List<string> phrasesList = new List<string>(), talkModules = new List<string>(), simonModules = new List<string>();
	private readonly List<string> unformattedPhrases = new List<string>();
	private readonly List<string[]> colorsList = new List<string[]>();

    protected void Start()
    {
		totalNum++;
		thisNum = totalNum;
		Debug.Log ("[Simon's Satire #" + thisNum + "] Initializing Simon's Satire.");
		RedButton.OnInteract += () => ButtonPress("Red");
		BlueButton.OnInteract += () => ButtonPress("Blue");
		GreenButton.OnInteract += () => ButtonPress("Green");
		YellowButton.OnInteract += () => ButtonPress("Yellow");
		LeftArrow.OnInteract += () => ArrowPress("Left", "1/2");
		RightArrow.OnInteract += () => ArrowPress("Right", "2/2");

		//PULLING BOMB AND PHRASE INFO
		simonModules = ReadFile ("simon", simon);
		talkModules = ReadFile ("talk", talk);
		phrasesList = ReadFile ("phrases", phrases);
		ReadFile ("colors", colors); //don't need to save result since colors are directly added
		List<string> modules = BombInfo.GetModuleNames ();

		Debug.Log ("[Simon's Satire #" + totalNum + "] Phrases loaded.");

		//DETERMINING COLUMN
		int numSimonModules = modules.Count(x => simonModules.Contains(x.ToLower()));
		int numTalkModules = modules.Count (x => talkModules.Contains(x.ToLower ()));
			
		if (numSimonModules == 0 && numTalkModules == 0)
			column = 0;
		else if (numSimonModules >= 1 && numTalkModules == 0)
			column = 1;
		else if (numSimonModules == 0 && numTalkModules >= 1)
			column = 2;
		else if (numSimonModules >= 1 && numTalkModules >= 1)
			column = 3;

		Debug.Log ("[Simon's Satire #" + totalNum + "] Bomb information gathered.");

		//DETERMINING RANDOM PHRASES + WIN SOUND
		GenerateRandomStuff();

		//DETERMINING COLOR INPUTS
		for (int i = 0; i < 3; ++i) {
			colorInputs [i] = GetColor (randomNums [i], column);
			Debug.Log ("[Simon's Satire #" + totalNum + "] Phrase " + (i + 1) + ": " + unformattedPhrases [randomNums [i]]);
		}
		Debug.Log ("[Simon's Satire #" + totalNum + "] Color Inputs (column " + (column+1) + "): " + colorInputs[0] + " " + colorInputs[1] + " " + colorInputs[2]);

		//INITIAL SETUP
		phase = 0;
		currentPhrase = phrasesList [randomNums [phase]];
		CheckSplit(currentPhrase);

		Debug.Log ("[Simon's Satire #" + totalNum + "] Phase 1.\nPhrase: " + unformattedPhrases[randomNums[phase]]);
	}

	private List<string> ReadFile(string filename, TextAsset asset){
		StreamReader reader;
		List<string> list = new List<string>();
		string line;

		reader = new StreamReader(new MemoryStream (asset.bytes));

		while ((line = reader.ReadLine ()) != null) {
			if (filename.Equals ("colors")) {  //colors is an array list of lists as opposed to array list of strings, handle differently
				colorsList.Add (line.Split(','));
			} else {
				if (filename.Equals ("phrases")) { //if phrase, add to unformatted before formatting
					unformattedPhrases.Add (line.Replace ("\\n", " ").Replace ("SPLIT", " "));
					line = FormatPhrase (line);
				} else {
					line = line.ToLower (); //lowercase for module names to be safe on case sensitivity
				}

				list.Add (line);
			}
		}

		return list;
	}

	private string FormatPhrase(string phrase){
		int lineLength = 26, nextLine = lineLength, stringLength = 0;
		string formatted = "";
		string[] phraseSplit = phrase.Split (' ');

		//if phrase has \n in the source, format is already hardcoded
		if(phrase.IndexOf("\\n") != -1){
			return phrase.Replace ("\\n", "\n");
		}

		foreach (string word in phraseSplit) {
			if (stringLength + word.Length + 1 > nextLine) { //new line needs to be added
				formatted += "\n" + word + " ";
				nextLine = stringLength + lineLength;
				stringLength += word.Length + 1;
			} else {
				formatted += word + " ";
				stringLength += word.Length + 1;
			}
		}

		return formatted;
	}

	private void GenerateRandomStuff(){
		List<int> ranges = new List<int>() { phrasesList.Count - 1, phrasesList.Count, phrasesList.Count };
		while (randomNums.Count < 3) {
			int a = UnityEngine.Random.Range (0, ranges [randomNums.Count]);
			if (!randomNums.Contains (a))
				randomNums.Add(a);
		}

		if (UnityEngine.Random.Range(1, 3) == 1)
			winSound = "Win1";
		else
			winSound = "Win2";
	}

	private string GetColor(int num, int column){
		if (num < 490) { //not a special case
			return colorsList [num][column];
		} else {
			count = 0; //reset here to save some space, used in special cases 2 3 and 9
			if (num == 490 || num == 494 || num == 495)
				specialCase = true;

			switch (num) {
			case 490:
				return "Green";
			case 491:
				return Case2();
			case 492:
				return Case3();
			case 493:
				string[] names = { "maze", "3d maze", "morse-a-maze", "red arrows", "a-maze-ing buttons", "maze³", "polyhedral maze", "module maze", "boolean maze", 
					"rgb maze", "blind mazes", "the colored maze", "faulty rgb maze", "cruel boolean maze", "mazematics", "the labyrinth", "maze scrambler"};

				if (BombInfo.GetModuleNames ().Any (x => names.Contains (x.ToLower ())))
					return "Red";
				else
					return "Blue";
			case 494:
				if (BombInfo.GetSerialNumberLetters ().Any (x => "EXODIA".Contains (x)))
					return "YGR";
				else
					return "Blue";
			case 495:
				return "Any";
			case 496:
				if (BombInfo.GetSerialNumberLetters ().Any (x => "CROOK".Contains (x)))
					return "Red";
				else
					return "Blue";
			case 497:
				return "Yellow";
			case 498:
				return Case9();
			case 499:
				special10 = true;
				return "Nothing";
			default:
				return "Error";
			}
		}
	}

	private string Case2(){
		ArrayList letters = new ArrayList ();

		foreach (string indicator in BombInfo.GetIndicators()) {
			foreach (char letter in indicator) {
				if ("CHAINDESTRUO".Contains (letter) && !letters.Contains (letter)) {
					letters.Add (letter);
					if (letters.Count == 3) {
						return "Green";
					}
				}
			}
		}

		return "Red";
	}

	private string Case3(){
		count = BombInfo.GetPortPlates ().Max (x => x.Length);

		if (count >= 2)
			return "Green";
		else if (count == 1)
			return "Blue";
		else if (count == 0)
			return "Yellow";
		else
			return "Red";
	}

	private string Case9(){
		bool spanish = false, french = false, japanese = false;

		foreach (char letter in BombInfo.GetIndicators().Join("")) {
			if ("SPANISH".Contains (letter))
				spanish = true;
			if ("FRENCH".Contains (letter))
				french = true;
			if ("JAPANESE".Contains (letter))
				japanese = true;
			if (spanish && french && japanese)
				return "Green";
		}

		return "Red";
	}

	protected void Update(){
		if (!active) {
			return;
		}

		if (!phraseText.text.Equals (currentPhrase)) {
			phraseText.text = currentPhrase;
		}

		if (randomNums [phase] >= 490 && phraseText.color != Color.cyan) {
			phraseText.color = Color.cyan;
		} else if (randomNums [phase] < 490 && phraseText.color != Color.yellow) {
			phraseText.color = Color.yellow;
		}

		if (special10 && BombInfo.GetSolvedModuleNames().Count == BombInfo.GetSolvableModuleNames().Count-1) {
			BombModule.HandlePass ();
			Audio.PlaySoundAtTransform(winSound, transform);
			active = false;
			Debug.Log ("[Simon's Satire #" + totalNum + "] Module defused. Simon is impressed.");
		}
	}

	protected bool ButtonPress(string color)
	{
		ValidateInput (color);
		return false;
	}

	protected bool ArrowPress(string button, string text)
    {
		if (currentPhrase.IndexOf("SPLIT") != -1) { //only continue if phrase is one that requires 2 pages
			string edit = phrasesList [randomNums [phase]];

			if (button.Equals ("left"))
				currentPhrase = edit.Substring (0, edit.IndexOf ("SPLIT")); //display up to SPLIT
			else
				currentPhrase = edit.Substring (edit.IndexOf ("SPLIT")+5); //display after SPLIT
			
			pageText.text = text;
		}
		return false;
    }

	private void ValidateInput(string color){
		//reset from previous input
		valid = false;
		special = false;

		if (!active)
			return;

		if (randomNums [phase] == 499) { //all input is invalid if special case 10
			BombModule.HandleStrike();
			return;
		}
			
		if (specialCase) {
			special = CheckSpecialCases (color);
			if (special)
				specialCase = false;
		} else {
			valid = color.Equals (colorInputs [sequenceNum]);
		}

		if (valid || special) {
			sequenceNum++;
			if (sequenceNum == phase+1) //phase+1 = how many inputs are needed for a given phase
				NextPhase ();

			if(active)
				Audio.PlaySoundAtTransform ("ButtonPress", transform);
			
		}else if (!valid || !special){
			BombModule.HandleStrike ();
			sequenceNum = 0;
		}
	}

	private bool CheckSpecialCases(string color){
		if (randomNums[sequenceNum] == 490 && color.Equals("Green")) { //special case 1
			if (BombInfo.GetFormattedTime ().Contains ("2")) {
				return true;
			} else {
				return false;
			}
		}

		if (randomNums[sequenceNum] == 494) { //special case 5
			if(colorInputs[sequenceNum].Equals("YGR") && !color.Equals("Blue")){
				colorInputs [sequenceNum] = color;
				return true;
			}else if(color.Equals("Blue")){
				if (BombInfo.GetTime () >= 60 && BombInfo.GetFormattedTime ().Contains ("00")) { //checking for 00 in timer when over 1 minute
					colorInputs [sequenceNum] = "Blue";
					return true;
				} else if (BombInfo.GetTime () < 60) { //blue is always valid when under a minute
					colorInputs [sequenceNum] = "Blue";
					return true;
				} else {
					return false;
				}
			}
		}

		if (randomNums[sequenceNum] == 495) { //special case 6
			int solved = BombInfo.GetSolvedModuleNames().Count;
			if (solved == 5 || (solved != 5 && BombInfo.GetTime () < 120)) {
				colorInputs [sequenceNum] = color;
				return true;
			} else {
				return false;
			}
		}

		return false;
	}

	private void NextPhase(){
		if (phase == 2) {
			BombModule.HandlePass ();
			Audio.PlaySoundAtTransform (winSound, transform);
			active = false;
			Debug.Log ("[Simon's Satire #" + totalNum + "] Module defused. Simon is impressed.");
		} else {
			++phase;
			currentPhrase = phrasesList [randomNums [phase]];
			CheckSplit (currentPhrase);
			sequenceNum = 0;
			Debug.Log ("[Simon's Satire #" + totalNum + "] Phase " + (phase+1) + ".\nPhrase: " + unformattedPhrases[randomNums[phase]]);
		}
	}

	private void CheckSplit(string phrase){
		if (currentPhrase.IndexOf("SPLIT") != -1) {
			currentPhrase = currentPhrase.Substring (0, currentPhrase.IndexOf ("SPLIT"));
			pageText.text = "1/2";
		} else {
			pageText.text = "";
		}
	}
}