using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class breakfastEggScript : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo bomb;
    //Buttons
	public KMSelectable confirmButton;
	public KMSelectable discardButton;

	public Material[] plateMaterials; //these are the materials for the plate: 0 = slate grey, 1 = pale teal, 2 = lavender, 3 = hazel-wood
	public MeshRenderer plateBase; //this is the plate object (base and rim)
	public MeshRenderer plateRim;

 	public Material[] yolkMaterials; //these are the colors for the egg yolk:  0 = crimson, 1 = orange, 2 = pink, 3 = beige, 4 = cyan, 5 = lime, 6 = petrol
	public MeshRenderer yolk; //holds the connection to the egg yolk object

	public Material[] shapeMaterials; //these are the shape textures 0 to 27
	public MeshRenderer eggShape; //holds connection tp the egg shape object

	private int yolkNumA; //these are the chosen yolk colors 0-7 (A and B)
	private int yolkNumB;
	private int plateNum; //this is the chosen plate manufacturer 0-3 (KF, KNT, WNF, WTF)
	private int batchNumber; //this is the batch for calculating the plate
	private int chosenEgg; //this is the # of the chosen egg from 0 to 27
	private float eggRotation; //this is a random y-axis rotation assigned to the egg shape

	private bool isEdible; //this declares if the egg is edible

	//Logging
	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved;
	private bool inStrike;

	private string[] yolkColorNames = new string[7] {"crimson", "orange", "pink", "beige", "cyan", "lime", "petrol"};
	private string[] yolkColorblindNames = new string[7] {"CRIM", "ORAN", "PINK", "BEIG", "CYAN", "LIME", "PETR"};
	private string[] plateColorNames = new string[4] {"slate grey", "pale teal", "lavender", "hazel wood"};
	private string[] manufacturerNames = new string[4] {"T.N.T", "K.T.N", "W.N.F", "W.T.F"};

	//angles for the material lerp
	private float tiltZ; //left-right tilt
	private float tiltX; //up-down tilt
	private const float _tiltXOffset = 76.5f; //this is the offset of the bomb: laying on the table vs. being upright in frot of the defuser.
	private float tiltMax;  //max of tiltX and tiltZ
	public GameObject plate; //platebase object for rotations

	public KMColorblindMode colorblindMode; //the colorblind object attached to the module
  	public TextMesh[] colorblindTexts; //the texts used to display the color if colorblind mode is enabled
  	private bool colorblindActive = false; //a boolean used for knowing if colorblind mode is active

	void Awake ()
	{
		moduleId = moduleIdCounter++;
		//check for colorblind mode
		colorblindActive = colorblindMode.ColorblindModeActive;

    	//delegate button press to method
    	discardButton.OnInteract += delegate () { PressDiscard(); return false; };
		confirmButton.OnInteract += delegate () { PressConfirm(); return false; };
		GetComponent<KMBombModule>().OnActivate += SetCB;
	}

	// Use this for initialization
	void Start ()
	{
		GetEdgework();
		RandomizePlate();
		RandomizeEgg();
	}

	void SetCB()
	{
		foreach (TextMesh text in colorblindTexts)
		{
			text.gameObject.SetActive(colorblindActive);
		}
	}

	// Update is called once per frame
	void Update ()
	{
		if (moduleSolved) {return;}

		//this makes the yolk color change when tilting the module left/right/up/down each other frame.
		tiltZ = plate.transform.rotation.eulerAngles.z; //z-angle of plate object
		//this code block makes it so that the top down view on the module results in an angle of 0°. And every offset from that will be positive, no matter whether the module spawns on the frontside or backside of the bomb.
		if (tiltZ >= 180f) { tiltZ = 360f - tiltZ; }
		if (tiltZ >= 90f) { tiltZ = 180f - tiltZ; }
		tiltX = (plate.transform.rotation.eulerAngles.x + _tiltXOffset) % 360; //x-angle of plate object (including offset)
		if (tiltX >= 180f) { tiltX = 360f - tiltX; }
		if (tiltX >= 90f) { tiltX = 180f - tiltX; }
		//take the maximum value
		tiltMax = Math.Max(tiltZ, tiltX);
		//Debug.LogFormat("[Breakfast Egg #{0}] Z value is: {1}", moduleId, tiltZ);
		//Debug.LogFormat("[Breakfast Egg #{0}] X value is: {1}", moduleId, tiltX);
		//Debug.LogFormat("[Breakfast Egg #{0}] Max value is: {1}", moduleId, tiltMax);
		//Debug.LogFormat("[Breakfast Egg #{0}] Lerp value is: {1}", moduleId, tiltMax / 60f);
		yolk.material.Lerp(yolkMaterials[yolkNumA], yolkMaterials[yolkNumB], tiltMax / 60f);
		//testing this in test harness is a pain because the root orientation of the bomb in TH is diffrent from the game (bomb spawning vertically vs. horizonally).
	}

	void GetEdgework()
	{
		int indicatorCount = bomb.GetIndicators().Count();
		int batteryCount = bomb.GetBatteryCount();
		int batteryHolderCount = bomb.GetBatteryCount(Battery.D) + bomb.GetBatteryCount(Battery.AA) / 2;
		int portCount = bomb.GetPortCount();
		int portPlateCount = bomb.GetPortPlates().Count();
		//Debug.LogFormat("[Breakfast Egg #{0}] Edgework is: {1} B in {2} H, {3} P on {4} Plates, {5} Ind.", moduleId, batteryCount, batteryHolderCount, portCount, portPlateCount, indicatorCount);

		batchNumber = (batteryHolderCount*portCount) + (portPlateCount*batteryCount) + indicatorCount + 1; //batch# will be always at least 1 (if the bomb has no edgework, only ports, only batteries)

		Debug.LogFormat("[Breakfast Egg #{0}] Batch# = ({1} Hold. * {2} Por.) + ({3} Pl. * {4} Bat.) + {5} Ind. + 1 = {6}", moduleId, batteryHolderCount, portCount, portPlateCount, batteryCount, indicatorCount, batchNumber);
	}

	void RandomizePlate()
	{
		//choose a random plate color
		plateNum = UnityEngine.Random.Range(0, 4);
		plateRim.material = plateMaterials[Mod((plateNum + batchNumber-1 ), 4)];
		plateBase.material = plateMaterials[Mod((plateNum + batchNumber-1 ), 4)]; //need to subtract 1 to make batchNumber count start at 0 not 1 so modulo works.

			colorblindTexts[2].text = plateColorNames[Mod((plateNum + batchNumber-1), 4)];

		Debug.LogFormat("[Breakfast Egg #{0}] Your plate color is {1}, from batch: {2} ({3}). The manufacturer is: {4}.", moduleId, plateColorNames[Mod((plateNum + batchNumber-1), 4)], batchNumber, Mod(batchNumber-1, 4)+1, manufacturerNames[plateNum]);
	}

	void RandomizeEgg()
	{
		//choose 2 random yolk colors that are not the same. So always 3-4 rows will be valid (3 if 2 colors share a row).
		yolkNumA = UnityEngine.Random.Range(0, 7);
		yolk.material = yolkMaterials[yolkNumA];
		do
		{
			yolkNumB = UnityEngine.Random.Range(0, 7);
		} while (yolkNumA == yolkNumB);

				colorblindTexts[0].text = yolkColorblindNames[yolkNumA];
				colorblindTexts[1].text = yolkColorblindNames[yolkNumB];
			
		Debug.LogFormat("[Breakfast Egg #{0}] HERE COMES THE EGG!", moduleId);
		Debug.LogFormat("[Breakfast Egg #{0}] Your egg yolk is {1} and {2}", moduleId, yolkColorNames[yolkNumA], yolkColorNames[yolkNumB]);

		//pick one radom egg between 0 and 27
		chosenEgg = UnityEngine.Random.Range(0, 28);
		eggShape.material = shapeMaterials[chosenEgg];
		Debug.LogFormat("[Breakfast Egg #{0}] Displayed egg is #{1}. (In the manual eggs are counted column wise, top to bottom beginning with the leftmost column.)", moduleId, chosenEgg+1);
		//find out if the chosen egg is edible (valid)
		if ((chosenEgg >= plateNum * 7) && (chosenEgg < ((plateNum+1) * 7))) //if the egg is on the correct plate/manufactuerer
		{
			if (Mod(chosenEgg, 7) == yolkNumA || Mod(chosenEgg, 7) == Mod(yolkNumA+1, 7) || Mod(chosenEgg, 7) == Mod(yolkNumB+1, 7) || Mod(chosenEgg, 7) == yolkNumB) // and if the egg is on one of the rows of the yolk color (the one in A/B and the one after that)
			{
				isEdible = true; //the egg is edible
				Debug.LogFormat("[Breakfast Egg #{0}] The egg is edible because both the manufacturer and a yolk color match.", moduleId);
			}
			else //if the maufact. matches but the colors don't
			{
				isEdible = false;
				Debug.LogFormat("[Breakfast Egg #{0}] The egg is NOT edible because the manufacturer matches but no yolk color does.", moduleId);
			}
		}
		else //if the egg is on the wrong plate/manufacurer
		{
			if (Mod(chosenEgg, 7) == yolkNumA || Mod(chosenEgg, 7) == Mod(yolkNumA+1, 7) || Mod(chosenEgg, 7) == Mod(yolkNumB+1, 7) || Mod(chosenEgg, 7) == yolkNumB) // and if the egg is on one of the rows of the yolk color (the one in A/B and the one after that)
			{
				isEdible = false;
				Debug.LogFormat("[Breakfast Egg #{0}] The egg is NOT edible because a yolk color matches but the manufacturer does not.", moduleId);
			}
			else //if the maufact. and  the colors don't match
			{
				isEdible = true; // the egg is edible
				Debug.LogFormat("[Breakfast Egg #{0}] The egg is edible because neither the manufacturer nor a yolk color match.", moduleId);
			}
		}
		//give the shape of the egg a random rotation
		eggRotation = UnityEngine.Random.Range(0, 365);
		eggShape.transform.Rotate(new Vector3(0f, eggRotation, 0f));
		//Debug.LogFormat("[Breakfast Egg #{0}] Egg Rotation is {1}°", moduleId, eggRotation);
	}

	int Mod(int x, int m) // modulo function that always gives a positive value back
	{
		return (x % m + m) % m;
	}

	void PressConfirm() //pressing the YUM button
	{
		if(moduleSolved || inStrike){return;}
		confirmButton.AddInteractionPunch();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, confirmButton.transform);
		StartCoroutine(ButtonAnimation(confirmButton));

		if (isEdible)
		{
			GetComponent<KMBombModule>().HandlePass();
			Audio.PlaySoundAtTransform("chew", transform);
			//hide the egg object
			yolk.gameObject.SetActive(false);
			eggShape.gameObject.SetActive(false);
			moduleSolved = true;
			foreach (TextMesh text in colorblindTexts)
			{
				text.gameObject.SetActive(false);
			}
			Debug.LogFormat("[Breakfast Egg #{0}] You chose to eat the egg. Since the egg was edible, that is a solved module!", moduleId);
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			Audio.PlaySoundAtTransform("frying", transform);
			StartCoroutine(Strike());
			Debug.LogFormat("[Breakfast Egg #{0}] You chose to eat the egg. Since the egg was NOT edible, that is a strike!", moduleId);
			RandomizeEgg(); //choose a completly new egg after strike
		}
	}

	void PressDiscard() //pressing the YUK button
	{
		if(moduleSolved || inStrike){return;}
		discardButton.AddInteractionPunch();
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, discardButton.transform);
		StartCoroutine(ButtonAnimation(discardButton));

		if (isEdible)
		{
			GetComponent<KMBombModule>().HandleStrike();
			Audio.PlaySoundAtTransform("frying", transform);
			StartCoroutine(Strike());
			Debug.LogFormat("[Breakfast Egg #{0}] You chose to trash the egg. Since the egg was edible, that is a strike!", moduleId);
			RandomizeEgg(); //choose a completly new egg after strike
		}
		else
		{
			GetComponent<KMBombModule>().HandlePass();
			Audio.PlaySoundAtTransform("dump", transform);
			//hide the egg object
			yolk.gameObject.SetActive(false);
			eggShape.gameObject.SetActive(false);
			moduleSolved = true;
			foreach (TextMesh text in colorblindTexts)
			{
				text.gameObject.SetActive(false);
			}
			Debug.LogFormat("[Breakfast Egg #{0}] You chose to trash the egg. Since the egg is NOT edible, that is a solved module!", moduleId);
		}
	}

	IEnumerator ButtonAnimation(KMSelectable pressedButton)
	{
		int movement = 0;
		while (movement < 4)
		{
			yield return new WaitForSeconds(0.0001f);
			pressedButton.transform.localPosition = pressedButton.transform.localPosition + Vector3.up * -0.001f;
			movement++;
		}
		yield return new WaitForSeconds(0.01f);
		movement = 0;
		while (movement < 4)
		{
			yield return new WaitForSeconds(0.0001f);
			pressedButton.transform.localPosition = pressedButton.transform.localPosition + Vector3.down * -0.001f;
			movement++;
		}
		StopCoroutine("buttonAnimation");
	}

    IEnumerator Strike()
	{
		inStrike = true;
		//hide the egg object
		yolk.gameObject.SetActive(false);
		eggShape.gameObject.SetActive(false);
		yield return new WaitForSeconds(.8f);
		//show the egg object
		yolk.gameObject.SetActive(true);
		eggShape.gameObject.SetActive(true);
		inStrike = false;
	}

	//twitch plays
	#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} press yum/yuk [Press the specified button; yum = eat the egg, yuk = trash the egg.]. Other aliases may be acceptable. Use !{0} colo(u)rblind to toggle colorblind mode. Use !{0} tilt l/r to tilt the plate.";
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string input)
	{
        string[] validCommands = new[] { "YUM", "EAT", "YES", "YUMMY", "FUCKYEA", "FUCKYEAH", "VOTEYEA", "YUK", "TRASH", "NO", "KILL", "DISPOSE", "FUCKYOU", "FUCKNO", "VOTENAY" };
        string command = input.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (new string[] { "COLORBLIND", "COLOURBLIND", "CB" }.Contains(command))
        {
            yield return null;
            colorblindActive = !colorblindActive;
            SetCB();
            yield break;
        }
        if (parameters[0] == "PRESS" || parameters[0] == "SUBMIT")
            parameters.RemoveAt(0);
        if (parameters.Count != 1)
        {
            yield return "sendtochaterror Invalid amount of parameters.";
            yield break;
        }
        if (!validCommands.Contains(parameters[0]))
        {
            yield return string.Format("sendtochaterror Invalid button name <{0}>.", parameters[0]);
            yield break;
        }
        yield return null;
        (Array.IndexOf(validCommands, parameters[0]) < 7 ? confirmButton : discardButton).OnInteract();
        yield return new WaitForSeconds(0.1f);
	}
    IEnumerator TwitchHandleForcedSolve()
    {
        (isEdible ? confirmButton : discardButton).OnInteract();
        yield return new WaitForSeconds(0.1f);
    }
}
