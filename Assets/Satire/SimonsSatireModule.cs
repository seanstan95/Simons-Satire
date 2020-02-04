using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using KModkit;

public class SimonsSatireModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public KMAudio KMAudio;
    public KMSelectable RedButton, BlueButton, GreenButton, YellowButton, LeftArrow, RightArrow;
    public TextMesh phraseText, pageText;
	private List<string> phrasesList = new List<string>(), talkModules = new List<string>(), simonModules = new List<string>(), unformattedPhrases = new List<string>(), displayPhrases = new List<string>();
	private List<string[]> colorsList = new List<string[]>();
	private bool active = true, specialPhrase = false, special6 = false;
	public TextAsset simon, talk, phrases, colors;
	private int sequenceNum = 0, phase; //determines the current phase, 1-3
	private string currentPhrase, special = "";
	private string[] inputs = new string[3];

    protected void Start()
    {
		Debug.Log ("[Simon's Satire] Initializing Simon's Satire.");
        RedButton.OnInteract += RedPress;
        BlueButton.OnInteract += BluePress;
        GreenButton.OnInteract += GreenPress;
        YellowButton.OnInteract += YellowPress;
        LeftArrow.OnInteract += LeftPress;
        RightArrow.OnInteract += RightPress;

		//GETTING BOMB AND PHRASE INFO
		simonModules = readFile ("simon");
		talkModules = readFile ("talk");
		phrasesList = readFile ("phrases");
		readFile ("colors");
		int numPhrases = phrasesList.Count;
		string[] modules = BombInfo.GetModuleNames ().ToArray ();

		Debug.Log ("[Simon's Satire] Phrases loaded.");

		int numSimonModules = 0, numTalkModules = 0, column = 0;

		//DETERMINING COLUMN BASED ON SIMON AND TALK MODULES
		foreach (string name in modules) {
			if (name.EqualsIgnoreCase ("simon's satire"))
				continue;
			if (simonModules.Contains (name))
				numSimonModules++;
			if (talkModules.Contains (name))
				numTalkModules++;
		}
			
		if (numSimonModules == 0 && numTalkModules == 0)
			column = 0;
		else if (numSimonModules >= 1 && numTalkModules == 0)
			column = 1;
		else if (numSimonModules == 0 && numTalkModules >= 1)
			column = 2;
		else if (numSimonModules >= 1 && numTalkModules >= 1)
			column = 3;

		Debug.Log ("[Simon's Satire] Bomb information gathered.");

		//DETERMINING RANDOM PHRASES (and ensuring they are different)
		int num1 = UnityEngine.Random.Range(0, numPhrases);

		int num2 = UnityEngine.Random.Range(0, numPhrases);
		while (num2 == num1) {
			num2 = UnityEngine.Random.Range(0, numPhrases);
		}

		int num3 = UnityEngine.Random.Range(0, numPhrases);
		while (num3 == num2 || num3 == num1) {
			num3 = UnityEngine.Random.Range(0, numPhrases);
		}

		/*num1 = 1;
		num2 = 2;
		num3 = 3;*/
			
		displayPhrases.Add(phrasesList[num1-1]);
		displayPhrases.Add(phrasesList[num2-1]);
		displayPhrases.Add(phrasesList[num3-1]);

		//DETERMINING COLOR INPUTS REQUIRED
		inputs [0] = getColor (num1 - 1, column, (string)displayPhrases [0]);
		inputs [1] = getColor (num2 - 1, column, (string)displayPhrases [1]);
		inputs [2] = getColor (num3 - 1, column, (string)displayPhrases [2]);

		Debug.Log ("[Simon's Satire] Random phrases determined.");
		Debug.Log ("[Simon's Satire] Phrase 1: " + unformattedPhrases [num1-1]);
		Debug.Log ("[Simon's Satire] Phrase 2: " + unformattedPhrases [num2-1]);
		Debug.Log ("[Simon's Satire] Phrase 3: " + unformattedPhrases [num3-1]);
		Debug.Log ("[Simon's Satire] Color Sequence (column " + (column+1) + "): " + inputs[0] + " " + inputs[1] + " " + inputs[2]);

		//initial setup
		phase = 1;
		currentPhrase = (string)displayPhrases [phase - 1]; //phase-1 because phase is 1-3, indexes start at 0
		checkSplit(currentPhrase);

		Debug.Log ("[Simon's Satire] Phase 1.");
	}

	private string getColor(int num, int column, string phrase){
		
		if (num < 490) { //if not a special case
			return colorsList [num][column];
		} else {
			string special = colorsList [num][0];
			specialPhrase = true;
			if (special.Equals ("Special1")) { //special case 1, always green but only when there's a 2 in the timer
				return "Green";
			} else if (special.Equals ("Special2")) { //special case 2, green if indicator letters in CHAIN DESTRUCTION, red otherwise
				ArrayList letters = new ArrayList ();
				int count = 0;

				foreach (string indicator in BombInfo.GetIndicators()) {
					foreach (char letter in indicator) {
						if ("CHAINDESTRUO".Contains (letter) && !letters.Contains (letter)) {
							count++;
							letters.Add (letter);
							if (count >= 3) {
								return "Green";
							}
						}
					}
				}

				return "Red";
			} else if (special.Equals ("Special3")) { //special case 3, ports on each plate
				if (BombInfo.GetPortPlateCount () == 0) {
					return "Red";
				} else {
					int maxPortCount = BombInfo.GetPortPlates ().Max (x => x.Length);

					if (maxPortCount >= 2) {
						return "Green";
					} else if (maxPortCount == 1) {
						return "Blue";
					} else if (maxPortCount == 0) {
						return "Yellow";
					} else {
						return "Error in special case 3";
					}
				}

			} else if (special.Equals ("Special4")) { //special case 4, red if vanilla maze, 3d maze, morse-a-maze, or red arrows, otherwise blue
				string[] names = {"Maze", "3D Maze", "Mors-a-maze", "Red Arrows"};
				if (BombInfo.GetModuleNames ().Any (x => names.Contains (x))) {
					return "Red";
				} else {
					return "Blue";
				}
			} else if (special.Equals ("Special5")) { //special case 5, Y/G/R if EXODIA has serial letter char, otherwise blue when seconds read 00 or any time if less than 1 minute
				if (BombInfo.GetSerialNumberLetters ().Any (x => "EXODIA".Contains (x))) {
					return "YellowGreenRed";
				} else {
					return "Blue";
				}
			} else if (special.Equals ("Special6")) { //special case 6, any whenever if exactly 5 modules solved, otherwise any  when less than 2 minutes on the timer
				if (BombInfo.GetSolvedModuleNames().Count == 5) {
					special6 = true;
				}
				return "Any";
			} else if (special.Equals ("Special7")) { //special case 7, red if serial has a letter in CROOK, otherwise blue
				if (BombInfo.GetSerialNumberLetters ().Any (x => "CROOK".Contains (x))) {
					return "Red";
				} else {
					return "Blue";
				}
			} else if (special.Equals ("Special8")) { //special case 8, always yellow
				return "Yellow";
			} else if (special.Equals ("Special9")) {
				//special case 9, if combined letters from all indicators share a letter with "SPANISH", "FRENCH", and "JAPANESE", press green, otherwise press red
				int count = 0;
				string usedLetters = "", wordLetters = "SPANIHFRECJ"; //spanish french and japanese without duplicate letters
				foreach (char letter in BombInfo.GetIndicators().Join("")) {
					if (wordLetters.Contains (letter) && !usedLetters.Contains (letter)) {
						count++;
						if(count == 3)
							return "Green";
						usedLetters += letter;
					}
				}

				return "Red";
			} else if (special.Equals ("Special10")) {
				//special case 10
				return "Blue";
			} else {
				return "Unknown Error";
			}
		}
	}

	protected void Update(){
		if (!active) {
			return;
		}

		if (!phraseText.text.Equals (currentPhrase)) {
			phraseText.text = (string)currentPhrase;
		}
	}

	private void checkSplit(string phrase){
		if (((string)displayPhrases[phase-1]).IndexOf("SPLIT") != -1) {
			currentPhrase = currentPhrase.Substring (0, currentPhrase.IndexOf ("SPLIT"));
			pageText.text = "1/2";
		} else {
			pageText.text = "";
		}
	}

	private List<string> readFile(string filename){
		StreamReader reader;
		MemoryStream stream;
		List<string> list = new List<string>();
		int phraseNum = 1;
		string line;

		if (filename.Equals ("phrases"))
			stream = new MemoryStream (phrases.bytes);
		else if (filename.Equals ("talk"))
			stream = new MemoryStream (talk.bytes);
		else if (filename.Equals ("colors"))
			stream = new MemoryStream (colors.bytes);
		else if (filename.Equals ("simon"))
			stream = new MemoryStream (simon.bytes);
		else
			return list;

		reader = new StreamReader (stream);

		while ((line = reader.ReadLine ()) != null) {
			if (filename.Equals ("colors")) {  //colors is an array list of lists as opposed to array list of strings
				string[] lineSplit = line.Split (',');
				colorsList.Add (lineSplit);
			} else {
				if(filename.Equals("phrases")){
					unformattedPhrases.Add (line.Replace ("\\n", " ").Replace ("SPLIT", " "));
					line = formatPhrase (line, phraseNum);
					phraseNum++;
				}
				list.Add (line);
			}
		}

		return list;
	}

	private string formatPhrase(string phrase, int phraseNum){
		int lineLength = 26, nextLine = lineLength, stringLength = 0;
		string formatted = "";
		string[] phraseSplit = phrase.Split (' ');

		//if phrase has \n, it's hardcoded already
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

    protected bool RedPress()
    {
		if (active) {
			bool sound = validateInput ("Red");
			if (sound) {
				//red press sound
			}
		}
        return false;
    }

    protected bool BluePress()
    {
		if (active) {
			bool sound = validateInput ("Blue");
			if (sound) {
				//blue press sound
			}
		}
        return false;
    }

    protected bool GreenPress()
    {
		if (active) {
			bool sound = validateInput ("Green");
			if (sound) {
				//green press sound
			}
		}
        return false;
    }

    protected bool YellowPress()
    {
		if (active) {
			bool sound = validateInput ("Yellow");
			if (sound) {
				//yellow press sound
			}
		}
        return false;
    }

    protected bool LeftPress()
    {
		if (((string)displayPhrases[phase-1]).IndexOf("SPLIT") != -1) { //only continue if phrase is one that requires 2 pages
			string edit = (string)displayPhrases [phase - 1];
			currentPhrase = edit.Substring (0, edit.IndexOf ("SPLIT"));
			pageText.text = "1/2";
			return false;
		} else {
			return false;
		}
    }

    protected bool RightPress()
    {
		if (((string)displayPhrases[phase-1]).IndexOf("SPLIT") != -1) { //only continue if phrase is one that requires 2 pages
			string edit = (string)displayPhrases [phase - 1];
			currentPhrase = edit.Substring (edit.IndexOf ("SPLIT")+5);
			pageText.text = "2/2";
			return false;
		} else {
			return false;
		}
    }

	private bool validateInput(string color){
		bool valid = color.Equals (inputs [sequenceNum]);
		if(specialPhrase){
			special = checkSpecialCases (color); //true is successful phase finish, false is strike, Not a Special Case is self-explanatory
		}

		if (valid || special.Equals("True")) {
			sequenceNum++;
			if (sequenceNum == phase) {
				nextPhase ();
			}
			return true;
		}else if (!valid || special.Equals("False")){
			sequenceNum = 0;
			BombModule.HandleStrike ();
			KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
			return false;
		}

		return false;
	}

	private string checkSpecialCases(string color){
		if (color.Equals ("Green") && displayPhrases[phase-1].Contains("just drew!")) { //special case 1, has to be green with a 2 in the timer
			string time = BombInfo.GetFormattedTime();
			if(time.Contains("2")){
				return "True";
			} else {
				return "False";
			}
		}

		if (displayPhrases[phase-1].Contains("Go fish")) { //special case 5, any color except blue, or blue when seconds are 00 or seconds is anything when below a minute
			if(inputs[phase-1].Equals("YellowGreenRed") && !color.Equals("Blue")){
				return "True";
			}else if(color.Equals("Blue")){
				if (BombInfo.GetTime () >= 60) { //1 or more minutes left
					if (BombInfo.GetFormattedTime ().Contains ("00")) {
						return "True";
					} else {
						return "False";
					}
				} else { //less than 1 minute left
					return "True";
				}
			}
		}

		if (displayPhrases [phase - 1].Contains ("EARTH!")) { //special case 6, any color whenever if special6 is true, otherwise any color when less than 2 minutes remain
			if (special6 || (!special6 && BombInfo.GetTime () < 120)) {
				return "True";
			} else {
				return "False";
			}
		}

		return "Not a Special Case";
	}

	private void nextPhase(){
		if (phase == 3) {
			BombModule.HandlePass ();
			KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.CorrectChime, this.transform);
			active = false;
		} else {
			++phase;
			currentPhrase = (string)displayPhrases [phase - 1];
			checkSplit (currentPhrase);
			sequenceNum = 0;
		}
	}
}