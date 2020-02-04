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
	public TextAsset simon, talk, phrases, colors;
    public TextMesh phraseText, pageText;
	private bool active = true, specialCase = false, special6 = false, special10 = false, valid = false;
	private int sequenceNum = 0, phase, count;
	private int[] nums = new int[3];
	private string currentPhrase, special = "";
	private string[] inputs = new string[3];
	private List<string> phrasesList = new List<string>(), talkModules = new List<string>(), simonModules = new List<string>(), unformattedPhrases = new List<string>(), displayPhrases = new List<string>();
	private List<string[]> colorsList = new List<string[]>();

    protected void Start()
    {
		Debug.Log ("[Simon's Satire] Initializing Simon's Satire.");
        RedButton.OnInteract += RedPress;
        BlueButton.OnInteract += BluePress;
        GreenButton.OnInteract += GreenPress;
        YellowButton.OnInteract += YellowPress;
        LeftArrow.OnInteract += LeftPress;
        RightArrow.OnInteract += RightPress;

		//PULLING BOMB AND PHRASE INFO
		simonModules = readFile ("simon");
		talkModules = readFile ("talk");
		phrasesList = readFile ("phrases");
		readFile ("colors");
		string[] modules = BombInfo.GetModuleNames ().ToArray ();

		Debug.Log ("[Simon's Satire] Phrases loaded.");

		int numSimonModules = 0, numTalkModules = 0, column = 0;

		//DETERMINING COLUMN
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

		//DETERMINING RANDOM PHRASES
		nums[0] = UnityEngine.Random.Range(0, phrasesList.Count-1); //only 0-498 to prevent conch shell phrase on phase 1

		do{
			nums[1] = UnityEngine.Random.Range(0, phrasesList.Count);
		} while(nums[1] == nums[0]);
			
		do{
			nums[2] = UnityEngine.Random.Range(0, phrasesList.Count);
		} while(nums[2] == nums[1] || nums[2] == nums[0]);

		/*nums[0] = 499;
		nums[1] = 0;
		nums[2] = 1;*/
			
		displayPhrases.Add(phrasesList[nums[0]]);
		displayPhrases.Add(phrasesList[nums[1]]);
		displayPhrases.Add(phrasesList[nums[2]]);

		//DETERMINING COLOR INPUTS
		inputs [0] = getColor (nums[0], column, displayPhrases [0]);
		inputs [1] = getColor (nums[1], column, displayPhrases [1]);
		inputs [2] = getColor (nums[2], column, displayPhrases [2]);

		Debug.Log ("[Simon's Satire] Phrase 1: " + unformattedPhrases [nums[0]]);
		Debug.Log ("[Simon's Satire] Phrase 2: " + unformattedPhrases [nums[1]]);
		Debug.Log ("[Simon's Satire] Phrase 3: " + unformattedPhrases [nums[2]]);
		Debug.Log ("[Simon's Satire] Color Sequence (column " + (column+1) + "): " + inputs[0] + " " + inputs[1] + " " + inputs[2]);

		//initial setup
		phase = 0;
		currentPhrase = displayPhrases [phase];
		checkSplit(currentPhrase);

		Debug.Log ("[Simon's Satire] Phase 1.");
	}

	private string getColor(int num, int column, string phrase){
		if (num < 490) { //not a special case
			return colorsList [num][column];
		} else {
			count = 0; //reset here to save some space, used in special cases 2 3 and 9

			switch (num) {
			case 490: //special case 1, always green but only when 2 in timer
				specialCase = true;
				return "Green";
			case 491: //special case 2, green if 3 indicator letters in CHAIN DESTRUCTION, red otherwise
				return case2();
			case 492: //special case 3, ports on each plate
				return case3();
			case 493: //special case 4, red if vanilla maze, 3d maze, morse-a-maze, or red arrows are on bomb, otherwise blue
				string[] names = { "maze", "3d maze", "morse-a-maze", "red arrows" };

				if (BombInfo.GetModuleNames ().Any (x => names.Contains (x.ToLower ())))
					return "Red";
				else
					return "Blue";
			case 494: //special case 5, Y/G/R if EXODIA has a serial number char, otherwise blue when seconds read 00/any time if less than 1 minute
				specialCase = true;

				if (BombInfo.GetSerialNumberLetters ().Any (x => "EXODIA".Contains (x)))
					return "YellowGreenRed";
				else
					return "Blue";
			case 495: //special case 6, if exactly 5 modules solved, anything is valid, otherwise anything when less than 2 minutes left
				specialCase = true;

				if (BombInfo.GetSolvedModuleNames().Count == 5)
					special6 = true;
				
				return "Any";
			case 496: //special case 7, red if serial has a letter in CROOK, otherwise blue
				if (BombInfo.GetSerialNumberLetters ().Any (x => "CROOK".Contains (x)))
					return "Red";
				else
					return "Blue";
			case 497: //special case 8, always yellow
				return "Yellow";
			case 498: //special case 9, green if indicator letters match with "SPANISH" "FRENCH" and "JAPANESE", otherwise red
				return case9();
			case 499: //special case 10, always do nothing
				special10 = true;
				return "Nothing";
			default:
				return "Unknown Error";
			}
		}
	}

	private string case2(){
		ArrayList letters = new ArrayList ();

		foreach (string indicator in BombInfo.GetIndicators()) {
			foreach (char letter in indicator) {
				if ("CHAINDESTRUO".Contains (letter) && !letters.Contains (letter)) {
					count++;
					letters.Add (letter);
					if (count == 3) {
						return "Green";
					}
				}
			}
		}

		return "Red";
	}

	private string case3(){
		if (BombInfo.GetPortPlateCount () == 0) {
			return "Red";
		} else {
			int count = BombInfo.GetPortPlates ().Max (x => x.Length);

			if (count >= 2)
				return "Green";
			else if (count == 1)
				return "Blue";
			else if (count == 0)
				return "Yellow";
			else
				return "Error in special case 3";
		}
	}

	private string case9(){
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
	}

	protected void Update(){
		if (!active) {
			return;
		}

		if (!phraseText.text.Equals (currentPhrase)) {
			phraseText.text = currentPhrase;
		}

		if (nums [phase] >= 490 && phraseText.color != Color.blue) {
			phraseText.color = Color.cyan;
		} else if (nums [phase] < 490 && phraseText.color != Color.yellow) {
			phraseText.color = Color.yellow;
		}

		if (special10 && BombInfo.GetSolvedModuleNames().Count == BombInfo.GetSolvableModuleNames().Count-1) {
			BombModule.HandlePass ();
			KMAudio.PlayGameSoundAtTransformWithRef (KMSoundOverride.SoundEffect.CorrectChime, this.transform);
			active = false;
		}
	}

	private void checkSplit(string phrase){
		if (displayPhrases[phase].IndexOf("SPLIT") != -1) {
			currentPhrase = currentPhrase.Substring (0, currentPhrase.IndexOf ("SPLIT"));
			pageText.text = "1/2";
		} else {
			pageText.text = "";
		}
	}

	private List<string> readFile(string filename){
		MemoryStream stream;
		List<string> list = new List<string>();
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

		StreamReader reader = new StreamReader (stream);

		while ((line = reader.ReadLine ()) != null) {
			if (filename.Equals ("colors")) {  //colors is an array list of lists as opposed to array list of strings, handle differently
				colorsList.Add (line.Split(','));
			} else {
				if(filename.Equals("phrases")){ //if phrase, add to unformatted and then format before adding to list
					unformattedPhrases.Add (line.Replace ("\\n", " ").Replace ("SPLIT", " "));
					line = formatPhrase (line);
				}
				list.Add (line);
			}
		}

		return list;
	}

	private string formatPhrase(string phrase){
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

    protected bool RedPress()
    {
		validateInput ("Red");
        return false;
    }

    protected bool BluePress()
    {
		validateInput ("Blue");
        return false;
    }

    protected bool GreenPress()
    {
		validateInput ("Green");
        return false;
    }

    protected bool YellowPress()
    {
		validateInput ("Yellow");
        return false;
    }

    protected bool LeftPress()
    {
		if (displayPhrases[phase].IndexOf("SPLIT") != -1) { //only continue if phrase is one that requires 2 pages
			string edit = displayPhrases [phase];
			currentPhrase = edit.Substring (0, edit.IndexOf ("SPLIT")); //display up to SPLIT
			pageText.text = "1/2";
		}
		return false;
    }

    protected bool RightPress()
    {
		if (displayPhrases[phase].IndexOf("SPLIT") != -1) { //only continue if phrase is one that requires 2 pages
			string edit = displayPhrases [phase];
			currentPhrase = edit.Substring (edit.IndexOf ("SPLIT")+5); //display after SPLIT
			pageText.text = "2/2";
		}
		return false;
    }

	private void validateInput(string color){
		//reset from previous input
		valid = false;
		special = "";

		if (!active || nums[phase] == 499)
			return;

		if (nums [phase] == 499) { //all input is invalid if special case 10
			KMAudio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.Strike, this.transform);
			BombModule.HandleStrike();
			return;
		}
			
		if (specialCase)
			special = checkSpecialCases (color); //true is successful phase finish, false is strike, Not a Special Case is self-explanatory
		else
			valid = color.Equals (inputs [sequenceNum]);

		if (valid || special.Equals("True")) {
			sequenceNum++;
			if (sequenceNum == phase+1)
				nextPhase ();

			if(active)
				KMAudio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.ButtonPress, this.transform);
			
		}else if (!valid || special.Equals("False")){
			KMAudio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.Strike, this.transform);
			BombModule.HandleStrike ();
			sequenceNum = 0;
		}
	}

	private string checkSpecialCases(string color){
		if (color.Equals ("Green") && nums[sequenceNum] == 490) { //special case 1, has to be green with a 2 in the timer
			if (BombInfo.GetFormattedTime ().Contains ("2")) {
				specialCase = false;
				return "True";
			} else {
				return "False";
			}
		}

		if (nums[sequenceNum] == 494) { //special case 5, any color except blue, or blue when seconds are 00 or seconds is anything when below a minute
			if(inputs[sequenceNum].Equals("YellowGreenRed") && !color.Equals("Blue")){
				specialCase = false;
				inputs [sequenceNum] = color;
				return "True";
			}else if(color.Equals("Blue")){
				if (BombInfo.GetTime () >= 60) { //1 or more minutes left
					if (BombInfo.GetFormattedTime ().Contains ("00")) {
						specialCase = false;
						inputs [sequenceNum] = "Blue";
						return "True";
					} else {
						return "False";
					}
				} else { //less than 1 minute left
					specialCase = false;
					inputs [sequenceNum] = "Blue";
					return "True";
				}
			}
		}

		if (nums[sequenceNum] == 495) { //special case 6, any color whenever if special6 is true, otherwise any color when less than 2 minutes remain
			if (special6 || (!special6 && BombInfo.GetTime () < 120)) {
				specialCase = false;
				inputs [sequenceNum] = color;
				return "True";
			} else {
				return "False";
			}
		}

		return "Not a Special Case";
	}

	private void nextPhase(){
		if (phase == 2) {
			BombModule.HandlePass ();
			KMAudio.PlayGameSoundAtTransform (KMSoundOverride.SoundEffect.CorrectChime, this.transform);
			active = false;
		} else {
			++phase;
			currentPhrase = displayPhrases [phase];
			checkSplit (currentPhrase);
			sequenceNum = 0;
		}
	}
}