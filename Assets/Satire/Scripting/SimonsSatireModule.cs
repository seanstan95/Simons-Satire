using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using KModkit;
using System.Text.RegularExpressions;

public class SimonsSatireModule : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public KMSelectable RedButton, BlueButton, GreenButton, YellowButton, LeftArrow, RightArrow;
	public static int totalNum = 0;
	public TextAsset simon, talk, phrases, colors;
	public TextMesh phraseText, pageText;

	private bool active = true, case6Option2 = false, special = false, special6 = false, valid = false;
	private int sequenceNum = 0, phase, count, column, thisNum = 0, case6Red, case6Blue, case6Green, case6Yellow;
	private readonly List<int> randomNums = new List<int>();
	private readonly List<string> unformattedPhrases = new List<string>(), phrasesList = new List<string>(), talkModules = new List<string>(), simonModules = new List<string>();
	private readonly List<string[]> colorsList = new List<string[]>();
	private readonly string[] colorInputs = new string[3];
	private string currentPhrase, winSound = "";

	protected void Start()
	{
		totalNum++;
		thisNum = totalNum;
		Debug.Log("[Simon's Satire #" + thisNum + "] Initializing Simon's Satire.");
		RedButton.OnInteract += () => ButtonPress("Red");
		BlueButton.OnInteract += () => ButtonPress("Blue");
		GreenButton.OnInteract += () => ButtonPress("Green");
		YellowButton.OnInteract += () => ButtonPress("Yellow");
		LeftArrow.OnInteract += () => ArrowPress("Left", "1/2");
		RightArrow.OnInteract += () => ArrowPress("Right", "2/2");

		//PULLING BOMB AND PHRASE INFO
		simonModules.AddRange(ReadFile("simon", simon));
		talkModules.AddRange(ReadFile("talk", talk));
		phrasesList.AddRange(ReadFile("phrases", phrases));
		ReadFile("colors", colors); //don't need to save result since colors are directly added
		List<string> modules = BombInfo.GetModuleNames();

		Debug.Log("[Simon's Satire #" + thisNum + "] Phrases loaded.");

		//DETERMINING COLUMN
		int numSimonModules = modules.Count(x => simonModules.Contains(x.ToLower()));
		int numTalkModules = modules.Count(x => talkModules.Contains(x.ToLower()));

		if (numSimonModules == 0 && numTalkModules == 0)
			column = 0;
		else if (numSimonModules >= 1 && numTalkModules == 0)
			column = 1;
		else if (numSimonModules == 0 && numTalkModules >= 1)
			column = 2;
		else if (numSimonModules >= 1 && numTalkModules >= 1)
			column = 3;

		Debug.Log("[Simon's Satire #" + thisNum + "] Bomb information gathered.");

		//DETERMINING RANDOM PHRASES + WIN SOUND
		GenerateRandomStuff();

		//DETERMINING COLOR INPUTS
		for (int i = 0; i < 3; ++i)
		{
			colorInputs[i] = GetColor(randomNums[i], column);
			Debug.Log("[Simon's Satire #" + thisNum + "] Phrase " + (i + 1) + ": " + unformattedPhrases[randomNums[i]]);
		}
		Debug.Log("[Simon's Satire #" + thisNum + "] Color Inputs (column " + (column + 1) + "): " + colorInputs[0] + " " + colorInputs[1] + " " + colorInputs[2]);

		//INITIAL SETUP
		phase = 0;
		currentPhrase = phrasesList[randomNums[phase]];
		CheckSplit();

		Debug.Log("[Simon's Satire #" + thisNum + "] Phase 1.\nPhrase: " + unformattedPhrases[randomNums[phase]]);
	}

	private IEnumerable<string> ReadFile(string filename, TextAsset asset)
	{
		StreamReader reader;
		List<string> list = new List<string>();
		string line;

		reader = new StreamReader(new MemoryStream(asset.bytes));

		while ((line = reader.ReadLine()) != null)
		{
			if (filename.Equals("colors")) //colors is an array list of lists as opposed to array list of strings, handle differently
				colorsList.Add(line.Split(','));
			else
			{
				if (filename.Equals("phrases")) //if phrase, add to unformatted before formatting
				{
					unformattedPhrases.Add(line.Replace("\\n", " ").Replace("SPLIT", " "));
					line = FormatPhrase(line);
				}
				else
					line = line.ToLower(); //lowercase for module names to be safe on case sensitivity

				list.Add(line);
			}
		}

		return list.AsEnumerable();
	}

	private string FormatPhrase(string phrase)
	{
		int lineLength = 26, nextLine = lineLength, stringLength = 0;
		string formatted = "";
		string[] phraseSplit = phrase.Split(' ');

		//if phrase has \n in the source, format is already hardcoded
		if (phrase.IndexOf("\\n") != -1)
			return phrase.Replace("\\n", "\n");

		foreach (string word in phraseSplit)
		{
			if (stringLength + word.Length + 1 > nextLine) //new line needs to be added
			{
				formatted += "\n" + word + " ";
				nextLine = stringLength + lineLength;
				stringLength += word.Length + 1;
			}
			else
			{
				formatted += word + " ";
				stringLength += word.Length + 1;
			}
		}

		return formatted;
	}

	private void GenerateRandomStuff()
	{
		List<int> ranges = new List<int>() { phrasesList.Count, phrasesList.Count-9, phrasesList.Count-9 };
		while (randomNums.Count < 3)
		{
			int a = UnityEngine.Random.Range(0, ranges[randomNums.Count]);
			if (!randomNums.Contains(a))
				randomNums.Add(a);
		}

		if (UnityEngine.Random.Range(1, 3) == 1)
			winSound = "Win1";
		else
			winSound = "Win2";
	}

	private string GetColor(int num, int column)
	{
		if (num < 490) //not a special case
			return colorsList[num][column];
		else
		{
			count = 0; //reset here to save some space, used in special cases 2 3 and 9

			switch (num)
			{
				case 490:
					return "Green";
				case 491:
					return Case2(new ArrayList());
				case 492:
					return Case3();
				case 493:
					string[] names = { "maze", "3d maze", "morse-a-maze", "red arrows", "a-maze-ing buttons", "maze³", "polyhedral maze", "module maze", "boolean maze",
					"rgb maze", "blind mazes", "the colored maze", "faulty rgb maze", "cruel boolean maze", "mazematics", "the labyrinth", "maze scrambler"};

					if (BombInfo.GetModuleNames().Any(x => names.Contains(x.ToLower())))
						return "Red";
					else
						return "Blue";
				case 494:
					if (BombInfo.GetSerialNumberLetters().Any(x => "EXODIA".Contains(x)))
						return "YGR";
					else
						return "Blue";
				case 495:
					return "Any"; //effectively a placeholder, case 6 input is determined dynamically vs. during setup
				case 496:
					if (BombInfo.GetSerialNumberLetters().Any(x => "CROOK".Contains(x)))
						return "Red";
					else
						return "Blue";
				case 497:
					return "Yellow";
				case 498:
					return Case9();
				case 499:
					return "Blue";
				default:
					return "Error";
			}
		}
	}

	private string Case2(ArrayList letters)
	{
		foreach (string indicator in BombInfo.GetIndicators())
		{
			foreach (char letter in indicator)
			{
				if ("CHAINDESTRUO".Contains(letter) && !letters.Contains(letter))
				{
					letters.Add(letter);
					if (letters.Count == 3)
						return "Green";
				}
			}
		}

		return "Red";
	}

	private string Case3()
	{
		count = BombInfo.GetPortPlates().Max(x => x.Length);

		if (count >= 2)
			return "Green";
		else if (count == 1)
			return "Blue";
		else if (count == 0)
			return "Yellow";
		else
			return "Red";
	}

	private string Case9()
	{
		bool spanish = false, french = false, japanese = false;

		foreach (char letter in BombInfo.GetIndicators().Join(""))
		{
			if ("SPANISH".Contains(letter))
				spanish = true;
			if ("FRENCH".Contains(letter))
				french = true;
			if ("JAPANESE".Contains(letter))
				japanese = true;
			if (spanish && french && japanese)
				return "Green";
		}

		return "Red";
	}

	protected void Update()
	{
		if (!active)
			return;

		//Update the displayed phrase + color
		if (!phraseText.text.Equals(currentPhrase))
			phraseText.text = currentPhrase;

		if (randomNums[phase] >= 490 && phraseText.color != Color.cyan)
			phraseText.color = Color.cyan;
		else if (randomNums[phase] < 490 && phraseText.color != Color.yellow)
			phraseText.color = Color.yellow;

		//Continual checking of timer being in the xx:59-xx:50 range for special case 6
		if((int)BombInfo.GetTime() % 60 >= 50)
		{
			if (!special6)
				special6 = true;
		}
		else
		{
			if (special6)
			{
				case6Red = 0;
				case6Blue = 0;
				case6Green = 0;
				case6Yellow = 0;
				special6 = false;
			}
		}

		//Check if all other modules are solved while special case 10 is visible
		if (randomNums[phase] == 499 && BombInfo.GetSolvedModuleNames().Count == BombInfo.GetSolvableModuleNames().Count - 1)
		{
			BombModule.HandlePass();
			Audio.PlaySoundAtTransform(winSound, transform);
			active = false;
			Debug.Log("[Simon's Satire #" + thisNum + "] Module defused. Simon is impressed.");
		}
	}

	protected bool ButtonPress(string color)
	{
		ValidateInput(color);
		return false;
	}

	protected bool ArrowPress(string button, string text)
	{
		if (phrasesList[randomNums[phase]].IndexOf("SPLIT") != -1) //only continue if phrase is one that requires 2 pages
		{
			string edit = phrasesList[randomNums[phase]];

			if (button.Equals("Left"))
				currentPhrase = edit.Substring(0, edit.IndexOf("SPLIT")); //display up to SPLIT
			else
				currentPhrase = edit.Substring(edit.IndexOf("SPLIT") + 5); //display after SPLIT

			pageText.text = text;
		}
		return false;
	}

	private void ValidateInput(string color)
	{
		//reset from previous input
		valid = false;
		special = false;
		case6Option2 = false;

		if (!active)
			return;

		if (randomNums[phase] == 490 || randomNums[phase] == 494 || randomNums[phase] == 495 || randomNums[phase] == 499)
			special = CheckSpecialCases(color);
		else
			valid = color.Equals(colorInputs[sequenceNum]);

		if (valid || special)
		{
			if (active)
				Audio.PlaySoundAtTransform("ButtonPress", transform);
			if (case6Option2) //leave now if taking case 6 option to
				return;

			sequenceNum++;
			if (sequenceNum == phase + 1) //phase+1 = how many inputs are needed for a given phase
				NextPhase();

		}
		else if (!valid || !special)
		{
			Audio.PlaySoundAtTransform("Strike", transform);
			BombModule.HandleStrike();
			sequenceNum = 0;
		}
	}

	private bool CheckSpecialCases(string color)
	{
		if (randomNums[sequenceNum] == 490 && color.Equals("Green")) //special case 1
			return BombInfo.GetFormattedTime().Contains("2");

		if (randomNums[sequenceNum] == 494) //special case 5
		{
			if (colorInputs[sequenceNum].Equals("YGR") && !color.Equals("Blue"))
			{
				colorInputs[sequenceNum] = color;
				return true;
			}
			else if (color.Equals("Blue"))
			{
				if (BombInfo.GetTime() >= 60 && BombInfo.GetFormattedTime().Contains("00")) //checking for 00 in timer when over 1 minute
				{
					colorInputs[sequenceNum] = "Blue";
					return true;
				}
				else if (BombInfo.GetTime() < 60) //blue is always valid when under a minute
				{
					colorInputs[sequenceNum] = "Blue";
					return true;
				}
				else
					return false;
			}
		}

		if (randomNums[sequenceNum] == 495) //special case 6
		{
			int solved = BombInfo.GetSolvedModuleNames().Count;
			if (solved == 5)
			{
				colorInputs[sequenceNum] = color;
				return true;
			}
			else
				return Case6(color);
		}

		if (randomNums[sequenceNum] == 499) //special case 10
		{
			//Special case 10 finishes automatically in Update() OR it can finish here by pressing blue with a 0 in the seconds timer
			if (color.Equals("Blue") && (int)BombInfo.GetTime() % 10 == 0)
				return true;
		}

		return false;
	}

	private bool Case6(string color)
	{
		case6Option2 = true; //ensures phase doesn't advance when this option is taken

		//Check if in the correct time range (only when seconds is between 50-59, aka xx:5x)
		if(!special6)
			return false;

		//Increment counters for each
		if (case6Red < 5 && color.Equals("Red"))
		{
			case6Red++;
			return true;
		}
		if (case6Red == 5 && case6Blue < 5 && color.Equals("Blue"))
		{
			case6Blue++;
			return true;
		}
		if (case6Blue == 5 && case6Green < 5 && color.Equals("Green"))
		{
			case6Green++;
			return true;
		}
		if (case6Green == 5 && case6Yellow < 5 && color.Equals("Yellow"))
		{
			case6Yellow++;
			if (case6Yellow == 5) //this is only true when all 4 ints are 5, aka module is done
				Done();
			return true;
		}

		//If here, the player input something incorrectly, so reset the counts for next attempts
		case6Red = 0;
		case6Blue = 0;
		case6Green = 0;
		case6Yellow = 0;
		return false;
	}

	private void NextPhase()
	{
		if (phase == 2)
			Done();
		else
		{
			++phase;
			currentPhrase = phrasesList[randomNums[phase]];
			CheckSplit();
			sequenceNum = 0;
            Debug.Log("[Simon's Satire #" + thisNum + "] Phase " + (phase + 1) + ". \"Phrase: " + unformattedPhrases[randomNums[phase]] + "\"");
		}
	}

	private void Done()
	{
		BombModule.HandlePass();
		Audio.PlaySoundAtTransform(winSound, transform);
		active = false;
		Debug.Log("[Simon's Satire #" + thisNum + "] Module defused. Simon is impressed.");
	}

	private void CheckSplit()
	{
		if (currentPhrase.IndexOf("SPLIT") != -1)
		{
			currentPhrase = currentPhrase.Substring(0, currentPhrase.IndexOf("SPLIT"));
			pageText.text = "1/2";
		}
		else
			pageText.text = "";
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To press the left/right button, use !{0} left/right | To press the colored buttons, use !{0} press red/blue/yellow/green (Use can use one letter for the color command, and the command can be chained. Example !{0} press red y yellow";
    #pragma warning restore 414
	
	string[] ValidColors = {"red", "blue", "yellow", "green", "r", "b", "y", "g"};
    
    IEnumerator ProcessTwitchCommand(string command)
    {
		string[] parameters = command.Split(' ');
		if (Regex.IsMatch(command, @"^\s*left\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			LeftArrow.OnInteract();
		}
		
		if (Regex.IsMatch(command, @"^\s*right\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			RightArrow.OnInteract();
		}
		
		if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length < 2)
			{
				yield return "sendtochaterror Parameter length invalid. Command ignored.";
				yield break;
			}
			
			for (int x = 1; x < parameters.Length; x++)
			{
				if (!ValidColors.Contains(parameters[x].ToLower()))
				{
					yield return "sendtochaterror Command contains an invalid color. Command ignored.";
					yield break;
				}
			}
			
			for (int y = 1; y < parameters.Length; y++)
			{
				switch (parameters[y].ToLower())
				{
					case "red":
					case "r":
						RedButton.OnInteract();
						Debug.Log("RED");
						break;
					case "blue":
					case "b":
						BlueButton.OnInteract();
						Debug.Log("BLUE");
						break;
					case "yellow":
					case "y":
						YellowButton.OnInteract();
						Debug.Log("YELLOW");
						break;
					case "green":
					case "g":
						GreenButton.OnInteract();
						Debug.Log("GREEN");
						break;
				}
				yield return new WaitForSecondsRealtime(0.1f);
			}
		}
	}
}