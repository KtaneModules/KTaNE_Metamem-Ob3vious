using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class metamemScript : MonoBehaviour
{
	public KMBombModule Module;
	public KMAudio Audio;
	public AudioClip[] Sounds;
	public KMSelectable[] Buttons;
	public TextMesh[] Texts;
	public MeshRenderer[] Colourables;

	private static List<int> memoryData = new List<int> { };

	private int[] colourCodes = new int[] { 255, 255, 255 };
	private bool solved = false;
    private static int input = 0;

	private static int inputs = 0;
	private static int latestInputs = 0;
	private bool check = true;

	private static int _moduleIdCounter = 1;
	private int _moduleId;

	private KMSelectable.OnInteractHandler Buttonpress(int pos)
	{
		return delegate
		{
			Buttons[pos].AddInteractionPunch();
			if (!solved)
			{
				if (pos == 0)
				{
					input = (input + 1) % 10;
					Texts[1].text = input.ToString();
					Audio.PlaySoundAtTransform("Press", Module.transform);
				}
				else if (pos == 1)
				{
					input = (input + 9) % 10;
					Texts[1].text = input.ToString();
					Audio.PlaySoundAtTransform("Press", Module.transform);
				}
				else
				{
					if (input == Calc(memoryData.Take(inputs + 1).ToList()))
					{
						inputs++;
						Debug.LogFormat("[Metamem #{0}] Inputted digit {1} correctly.", _moduleId, inputs);
						if (inputs == latestInputs + 3)
						{
							latestInputs = inputs;
							Module.HandlePass();
							Debug.LogFormat("[Metamem #{0}] Module solved!", _moduleId);
							Texts[0].text = "";
							Texts[1].text = "!";
							Texts[2].text = "";
							solved = true;
							Audio.PlaySoundAtTransform("Solve", Module.transform);
						}
						else
							Audio.PlaySoundAtTransform("Press", Module.transform);
					}
					else
					{
						Module.HandleStrike();
						Debug.LogFormat("[Metamem #{0}] Inputted digit {1} incorrectly.", _moduleId, inputs + 1);
					}
				}
			}
			return false;
		};
	}

	void Awake()
	{
		memoryData = new List<int> { };
		latestInputs = 0;
		inputs = 0;
		input = 0;
		_moduleId = _moduleIdCounter++;
		for (int i = 0; i < Buttons.Length; i++)
			Buttons[i].OnInteract += Buttonpress(i);
	}

	void Start()
	{
		for (int i = 0; i < 3; i++)
			memoryData.Add(Rnd.Range(0, 10));
		Texts[0].text = memoryData[inputs].ToString();
		for (int i = 0; i < 3; i++)
			colourCodes[i] = Rnd.Range(0, 255);
	}

	void Update()
	{
		if (!solved)
		{
			if (check)
			{
				Debug.LogFormat("[Metamem #{0}] Sequence is {1}, solution is {2}", _moduleId, memoryData.Join(""), Enumerable.Range(0, memoryData.Count()).Select(k => Calc(memoryData.Take(k + 1).ToList())).Join(""));
				check = false;
			}
			Texts[0].text = memoryData[inputs].ToString();
			Texts[1].text = input.ToString();
			Texts[2].text = (inputs + 1).ToString();
			for (int i = 0; i < Colourables.Length; i++)
				Colourables[i].material.color = new Color(colourCodes[0] / 255f, colourCodes[1] / 255f, colourCodes[2] / 255f, 0.5f);
			for (int i = 0; i < Texts.Length; i++)
				Texts[i].color = new Color(colourCodes[0] / 255f, colourCodes[1] / 255f, colourCodes[2] / 255f, 0.5f);
		}
	}

	private int Calc(List<int> sequence)
	{
		List<List<int>> table = new List<List<int>> { sequence };
		while (table.Last().Count() != 1)
		{
			List<int> next = new List<int> { };
			for (int i = 0; i < table.Last().Count() - 1; i++)
				next.Add((table.Last()[i + 1] - table.Last()[i] + 10) % 10);
			table.Add(next);
		}
		return table.Select(x => x.Last()).Sum() % 10;
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} recover' to reset all metamems (will require more inputs to solve). '!{0} submit #' to submit a digit.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant();
		string[] commandArray = command.Split(' ');
		if (commandArray.Length > 2)
		{
			yield return "sendtochaterror Invalid command.";
			yield break;
		}
		if (command == "recover")
		{
			inputs = 0;
		}
		else if (commandArray[0] == "submit" && commandArray.Length == 2 && commandArray[1].Length == 1)
		{
			while (commandArray[1].ToString() != input.ToString())
			{
				Buttons[0].OnInteract();
				yield return null;
			}
			Buttons[2].OnInteract();
		}
		else
		{
			yield return "sendtochaterror Invalid command.";
			yield break;
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		while (!solved)
		{
			while (input != Calc(memoryData.Take(inputs + 1).ToList()))
			{
				Buttons[0].OnInteract();
				yield return true;
			}
			Buttons[2].OnInteract();
			yield return true;
		}
	}
}