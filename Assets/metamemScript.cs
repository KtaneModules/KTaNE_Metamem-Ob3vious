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

	private static string[] memoryData = { "" };
	private static string[] twitchProfiles = { "NULL" };
	private static int[][] colourCodes = { new int[] { 255, 255, 255 } };
	private int loginIndex;

	private string display;
	private bool solved = false;
	private int[] input = new int[3];
	private string inputstring;
	private int inputindex;

	private static int _moduleIdCounter = 1;
	private int _moduleId;

	private KMSelectable.OnInteractHandler Buttonpress(int pos)
	{
		return delegate
		{
			Buttons[pos].AddInteractionPunch();
			if (!solved)
			{
				if (pos < 3)
				{
					input[pos] = (input[pos] + 1) % 10;
					Texts[1].text = input[0].ToString() + input[1].ToString() + input[2].ToString();
					Audio.PlaySoundAtTransform("Press", Module.transform);
				}
				else if (pos < 6)
				{
					input[pos - 3] = (input[pos - 3] + 9) % 10;
					Texts[1].text = input[0].ToString() + input[1].ToString() + input[2].ToString();
					Audio.PlaySoundAtTransform("Press", Module.transform);
				}
				else
				{
					if (Calc(memoryData[loginIndex] + display).Substring(0, inputindex + 3) == inputstring + input[0].ToString() + input[1].ToString() + input[2].ToString())
					{
						inputindex += 3;
						inputstring += input[0].ToString() + input[1].ToString() + input[2].ToString();
						Texts[1].text = "000";
						Texts[2].text = inputindex.ToString();
						input = new int[3];
					}
					else
					{
						Debug.LogFormat("[Metamem #{0}] {1} was not a correct input. Expected {2}.", _moduleId, inputstring, Calc(memoryData[loginIndex] + display).Substring(0, inputindex + 3));
						Module.HandleStrike();
						inputindex = 0;
						inputstring = "";
						Texts[1].text = "000";
						input = new int[3];
					}
					if (inputstring == Calc(memoryData[loginIndex] + display))
					{
						memoryData[loginIndex] += display;
						Module.HandlePass();
						Texts[0].text = "";
						Texts[1].text = "GG!";
						Texts[2].text = "";
						Texts[3].text = "";
						solved = true;
						Audio.PlaySoundAtTransform("Solve", Module.transform);
					}
					else
						Audio.PlaySoundAtTransform("Press", Module.transform);
				}
			}
			return false;
		};
	}

	void Awake()
	{
		_moduleId = _moduleIdCounter++;
		for (int i = 0; i < Buttons.Length; i++)
		{
			Buttons[i].OnInteract += Buttonpress(i);
		}
	}

	void Start()
	{
		display = Rnd.Range(0, 1000).ToString("000");
		Texts[0].text = display;
		Debug.LogFormat("[Metamem #{0}] Displayed {1}", _moduleId, display);
		inputstring = "";
	}

	void Update()
	{
		if (!solved)
		{
			Texts[2].text = inputindex.ToString();
			Texts[3].text = (memoryData[loginIndex].Length + 3).ToString();
			for (int i = 0; i < Colourables.Length; i++)
			{
				Colourables[i].material.color = new Color(colourCodes[loginIndex][0] / 255f, colourCodes[loginIndex][1] / 255f, colourCodes[loginIndex][2] / 255f, 0.5f);
			}
			for (int i = 0; i < Texts.Length; i++)
			{
				Texts[i].color = new Color(colourCodes[loginIndex][0] / 255f, colourCodes[loginIndex][1] / 255f, colourCodes[loginIndex][2] / 255f, 0.5f);
			}
		}
	}

	private string Calc(string sequence)
	{
		int[] numbers = new int[sequence.Length];
		int[] answer = new int[sequence.Length];
		string chars = "0123456789";
		for (int i = 0; i < sequence.Length; i++)
		{
			for (int j = 0; j < 10; j++)
			{
				if (chars[j] == sequence[i])
				{
					numbers[i] = j;
				}
			}
		}
		answer[0] = numbers[0];
		answer[1] = (numbers[0] + numbers[1]) % 10;
		for (int i = 2; i < sequence.Length; i++)
		{
			int[][] numbercheck = new int[3][];
			numbercheck[0] = new int[] { answer[i - 2], answer[i - 1], numbers[i] };
			numbercheck[1] = new int[] { 10 + numbercheck[0][1] - numbercheck[0][0], 10 + numbercheck[0][2] - numbercheck[0][1] };
			numbercheck[2] = new int[] { 10 + numbercheck[1][1] - numbercheck[1][0] };
			answer[i] = (numbercheck[2][0] + numbercheck[1][1] + numbercheck[0][2]) % 10;
		}
		return answer.Join("");
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} submit 420' to submit 420. Submissions must be entered all at once. '!{0} accounthelp' to get info on using accounts on this module.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		memoryData[0] = "";
		command = command.ToLowerInvariant();
		if (command == "accounthelp")
		{
			yield return "sendtochat '!{0} create ACCOUNT' to create an account called 'account' and automatically log in. '!{0} login ACCOUNT' to log in on the account called 'account'. '!{0} logout'. '!{0} colour F42069' to colour the account logged in on with the given hex code. Capitalisation doesn't matter in accounts.";
			yield break;
		}
		else
		{
			string[] commandArray = command.Split(' ');
			if (commandArray.Length > 2)
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
			if (commandArray[0] == "create" && commandArray.Length == 2)
			{
				if (twitchProfiles.Contains(commandArray[1]))
				{
					yield return "sendtochaterror This account already exists.";
					yield break;
				}
				loginIndex = twitchProfiles.Length;
				twitchProfiles = twitchProfiles.Concat(new string[] { commandArray[1] }).ToArray();
				memoryData = memoryData.Concat(new string[] { "" }).ToArray();
				colourCodes = colourCodes.Concat(new int[][] { new int[] { Rnd.Range(0, 256), Rnd.Range(0, 256), Rnd.Range(0, 256) } }).ToArray();
				inputindex = 0;
				inputstring = "";
				Texts[1].text = "000";
				input = new int[3];
			}
			else if (commandArray[0] == "login" && commandArray.Length == 2)
			{
				if (!twitchProfiles.Contains(commandArray[1]))
				{
					yield return "sendtochaterror This account does not exist.";
					yield break;
				}
				for (int i = 0; i < twitchProfiles.Length; i++)
				{
					if (twitchProfiles[i] == commandArray[1])
					{
						loginIndex = i;
					}
				}
				inputindex = 0;
				inputstring = "";
				Texts[1].text = "000";
				input = new int[3];
			}
			else if (commandArray[0] == "colour" && commandArray.Length == 2 && commandArray[1].Length == 6)
			{
				if (loginIndex == 0)
				{
					yield return "sendtochaterror You are not logged in.";
					yield break;
				}
				string hexchars = "0123456789abcdef";
				for (int i = 0; i < 6; i++)
				{
					if (!hexchars.Contains(commandArray[1][i]))
					{
						yield return "sendtochaterror Invalid command.";
						yield break;
					}
				}
				for (int i = 0; i < 3; i++)
				{
					colourCodes[loginIndex][i] = hexchars.IndexOf(commandArray[1][i * 2]) * 16 + hexchars.IndexOf(commandArray[1][i * 2 + 1]);
				}
			}
			else if (command == "logout")
			{
				loginIndex = 0;
				inputindex = 0;
				inputstring = "";
				Texts[1].text = "000";
				input = new int[3];
			}
			else if (commandArray[0] == "submit" && commandArray.Length == 2)
			{
				string chars = "0123456789";
				for (int i = 0; i < commandArray[1].Length; i++)
				{
					if (!chars.Contains(commandArray[1][i]) || commandArray[1].Length != memoryData[loginIndex].Length + 3)
					{
						yield return "sendtochaterror Invalid command.";
						yield break;
					}
				}
				for (int i = 0; i < memoryData[loginIndex].Length + 3; i++)
				{
					while (chars[input[i % 3]] != commandArray[1][i])
					{
						Buttons[i % 3].OnInteract();
						yield return null;
					}
					if (i % 3 == 2)
					{
						Buttons[6].OnInteract();
						yield return null;
					}
				}
			}
			else
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
		}
		memoryData[0] = "";
	}
}