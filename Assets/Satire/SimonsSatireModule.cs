using System;
using System.Collections;
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
	private ArrayList phrasesList, unformattedPhrases = new ArrayList(), simonModules, talkModules, colorsList, displayPhrases = new ArrayList();
	private bool active = true;
	public TextAsset simon, talk, phrases, colors;
	private int sequenceNum = 1, phase; //determines the current phase, 1-3
	private string inputSequence = "", currentPhrase, phase1Input, phase2Input, phase3Input;

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
		colorsList = readFile ("colors");
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

		/*num1 = 410;
		num2 = 1;
		num3 = 2;*/
			
		displayPhrases.Add(phrasesList[num1]);
		displayPhrases.Add(phrasesList[num2]);
		displayPhrases.Add(phrasesList[num3]);

		//DETERMINING COLOR INPUTS REQUIRED
		string color1 = getColor(num1, column, (string)displayPhrases[0]);
		string color2 = getColor(num2, column, (string)displayPhrases[1]);
		string color3 = getColor(num3, column, (string)displayPhrases[2]);
		phase1Input = color1 + " ";
		phase2Input = phase1Input + color2 + " ";
		phase3Input = phase2Input + color3 + " ";

		Debug.Log ("[Simon's Satire] Random phrases determined.");
		Debug.Log ("[Simon's Satire] Phrase 1: " + unformattedPhrases [num1]);
		Debug.Log ("[Simon's Satire] Phrase 2: " + unformattedPhrases [num2]);
		Debug.Log ("[Simon's Satire] Phrase 3: " + unformattedPhrases [num3]);
		Debug.Log ("[Simon's Satire] Color Sequence (column " + (column+1) + "): " + phase3Input);

		//initial setup
		phase = 1;
		currentPhrase = (string)displayPhrases [phase - 1]; //phase-1 because phase is 1-3, indexes start at 0
		checkSplit(currentPhrase);

		Debug.Log ("[Simon's Satire] Phase 1.");
	}

	private string getColor(int num, int column, string phrase){
		if (phrase.IndexOf ("Exodia", StringComparison.CurrentCultureIgnoreCase) == -1) {
			return (string)((string[])colorsList [num]) [column];
		} else {
			if (phrase.IndexOf ("just drew!") != -1) { //special case 1, always green but has to be when there's a 2 in the timer (checked in validateInput)
				return "Green";
			} else if (phrase.IndexOf ("obliteration") != -1) { //special case 2, blue = valid, red = invalid
				string indicators = BombInfo.GetIndicators().Join(""), letters = "";
				int count = 0;

				foreach(char letter in indicators){
					if ("CHAIN DESTRUCTION".Contains(letter) && !letters.Contains (letter)) {
						count++;
						letters += letter;
						if (count >= 3) {
							return "Green";
						}
					}
				}

				return "Red";
			} else if (phrase.IndexOf ("left leg") != -1) {
				//special case 3, port plate with 2 or more = green, exactly 1 = blue, empty plate = yellow, no plates = red
				return "Blue";
			} else if (phrase.IndexOf ("Para-Dox") != -1) {
				//special case 4, if vanilla maze, 3d maze, morse-a-maze, or red arrows, press red, otherwise blue
				return "Blue";
			} else if (phrase.IndexOf ("Go fish") != -1) {
				//special case 5, press yellow green or red IF serial number shares a letter with "EXODIA", otherwise press blue either when the seconds digits read 00 or any time if less than 1 minute
				return "Blue";
			} else if (phrase.IndexOf ("combined..") != -1) {
				//special case 6, if exactly 5 modules solved, press anything, otherwise can still press anything but only when there's less than 2 minutes on the timer
				return "Blue";
			} else if (phrase.IndexOf ("WATERGATE") != -1) { //special case 7, red if serial has a letter in CROOK, otherwise blue
				string serialLetters = BombInfo.GetSerialNumberLetters().Join("");

				foreach (char letter in serialLetters) {
					if("CROOK".Contains(letter)){
						return "Red";
					}
				}

				return "Blue";
			} else if (phrase.IndexOf ("INCARCERATE") != -1) { //special case 8, always yellow
				return "Yellow";
			} else if (phrase.IndexOf ("COMMUNICATE") != -1) {
				//special case 9, if combined letters from all indicators share a letter with "SPANISH", "FRENCH", and "JAPANESE", press green, otherwise press red
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

	private ArrayList readFile(string filename){
		StreamReader reader;
		MemoryStream stream;
		ArrayList list = new ArrayList ();
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
				list.Add (lineSplit);
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
		bool sound = validateInput ("Red");
		if(sound){
			//red press sound
		}
        return false;
    }

    protected bool BluePress()
    {
		bool sound = validateInput ("Blue");
		if(sound){
			//blue press sound
		}
        return false;
    }

    protected bool GreenPress()
    {
		bool sound = validateInput ("Green");
		if(sound){
			//green press sound
		}
        return false;
    }

    protected bool YellowPress()
    {
		bool sound = validateInput ("Yellow");
		if(sound){
			//yellow press sound
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
		if (color.Equals ("Green") && ((string)displayPhrases [phase - 1]).Contains("just drew!")) { //special case 1, has to be green with a 2 in the timer
			string time = BombInfo.GetFormattedTime();
			if(time.Contains("2")){
				nextPhase ();
				return true;
			} else {
				BombModule.HandleStrike ();
				KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
				return false;
			}
		}

		//if here, not a special case
		switch(phase){
		case 1:
			if (color.Equals (phase1Input.Trim ())) {
				nextPhase ();
				return true;
			} else {
				BombModule.HandleStrike ();
				KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
				return false;
			}
		case 2:
			if (sequenceNum == 1) {
				if (color.Equals (phase1Input.Trim ())) {
					inputSequence += color + " ";
					sequenceNum++;
					return true;
				} else {
					BombModule.HandleStrike ();
					KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
					inputSequence = "";
					return false;
				}
			} else if (sequenceNum == 2) {
				if ((inputSequence + color).Equals (phase2Input.Trim ())) {
					nextPhase ();
					return true;
				} else {
					BombModule.HandleStrike ();
					KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
					inputSequence = "";
					sequenceNum = 1;
					return false;
				}
			}
			break;
		case 3:
			if (sequenceNum == 1) {
				if (color.Equals (phase1Input.Trim ())) {
					inputSequence += color + " ";
					sequenceNum++;
					return true;
				} else {
					BombModule.HandleStrike ();
					KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
					return false;
				}
			} else if (sequenceNum == 2) {
				if ((inputSequence + color).Equals (phase2Input.Trim ())) {
					inputSequence += color + " ";
					sequenceNum++;
					return true;
				} else {
					BombModule.HandleStrike ();
					KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
					inputSequence = "";
					sequenceNum = 1;
					return false;
				}
			} else if (sequenceNum == 3) {
				if ((inputSequence + color).Equals (phase3Input.Trim ())) {
					BombModule.HandlePass ();
					active = false;
				} else {
					BombModule.HandleStrike ();
					KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.Strike, this.transform);
					inputSequence = "";
					sequenceNum = 1;
					return false;
				}
			}
			break;
		}

		return false;
	}

	private void nextPhase(){
		++phase;
		currentPhrase = (string)displayPhrases [phase - 1]; //phase-1 because phase is 1-3, indexes start at 0
		checkSplit (currentPhrase);
		inputSequence = "";
		sequenceNum = 1;
	}
}