using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ValveKeyValue;

namespace SourceSoundScripter
{
	public static class ScriptFileUtils
	{
		public static void LoadFile(string path, ref ObservableCollection<SoundEntry> soundEntries)
		{
			string fileText = File.ReadAllText(path);

			string fullScript = "SoundscriptFile\n{\n" + fileText + "\n}\n";

			byte[] fullScriptBytes = Encoding.UTF8.GetBytes(fullScript);
			MemoryStream stream = new MemoryStream(fullScriptBytes);

			KVSerializer kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
			KVObject data = kv.Deserialize(stream);

			foreach (KVObject sound in data.Children)
			{
				SoundEntry soundEntry = new SoundEntry(sound.Name);

				foreach (KVObject key in sound.Children)
				{
					switch (key.Name)
					{
						case "channel":
							if (key.Value != null)
								soundEntry.Channel = key.Value.ToString();
							break;
						case "volume":
							if (key.Value != null)
								soundEntry.Volume = key.Value.ToString();
							break;
						case "pitch":
							if (key.Value != null)
								soundEntry.Pitch = key.Value.ToString();
							break;
						case "soundlevel":
							if (key.Value != null)
								soundEntry.SndLvl = key.Value.ToString();
							break;
						case "wave":
							if (key.Value != null)
								soundEntry.Waves.Add(StripSoundChars(key.Value.ToString(), ref soundEntry));
							break;
						case "rndwave":
							foreach (KVObject wave in key.Children)
							{
								if (wave.Value != null)
									soundEntry.Waves.Add(StripSoundChars(wave.Value.ToString(), ref soundEntry));
							}
							break;
					}
				}

				if (soundEntry.Waves.Count > 1)
					soundEntry.DisplayWave = "Multiple";
				if (soundEntry.Waves.Count > 0)
					soundEntry.DisplayWave = soundEntry.Waves[0];
				else
					soundEntry.DisplayWave = "";

				soundEntries.Add(soundEntry);
			}

			stream.Close();
		}

		private static string StripSoundChars(string wave, ref SoundEntry soundEntry)
		{
			if (wave.Length <= 3)
				return wave;

			int newStart = 0;
			for (int i = 0; i < 2; i++)
			{
				switch (wave[i])
				{
					case '*':
					case '#':
					case '@':
					case '>':
					case '<':
					case '^':
					case ')':
					case '}':
					case '$':
					case '!':
						soundEntry.SndChars += wave[i];
						newStart++;
						break;
				}
			}

			if (newStart > 0)
				return wave.Substring(newStart);

			return wave;
		}

		private static string GetStringArg(string argName, string argValue, string defaultValue)
		{
			if (argValue == "")
				argValue = defaultValue;

			if (argValue != "")
				return String.Format("	\"{0}\"	\"{1}\"\n", argName, argValue);

			return "";
		}

		public static void SaveFile(MainWindow mainWindow, string path, ref ObservableCollection<SoundEntry> soundEntries)
		{
			// Get defaults
			string channel = "", volume = "", pitch = "", sndlvl = "", chars = "";
			mainWindow.GetDefaultValues(ref channel, ref volume, ref pitch, ref sndlvl, ref chars);

			FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
			using (StreamWriter writer = new StreamWriter(fileStream, Encoding.UTF8))
			{
				// Write header
				writer.Write(
@"//==================================================
// AUTO-GENERATED SOUNDSCRIPTS
// https://github.com/Blixibon/SourceSoundScripter
//==================================================

");

				// Write sounds
				foreach (SoundEntry sound in soundEntries)
				{
					// Skip empty sounds
					if (sound.Waves.Count == 0)
						continue;

					writer.Write("\"{0}\"\n{{\n", sound.Name);

					writer.Write(GetStringArg("channel", sound.Channel, channel));
					writer.Write(GetStringArg("volume", sound.Volume, volume));
					writer.Write(GetStringArg("pitch", sound.Pitch, pitch));
					writer.Write(GetStringArg("soundlevel", sound.SndLvl, sndlvl));

					string sndchars = sound.SndChars;
					if (sndchars == "")
						sndchars = chars;

					if (sound.Waves.Count > 1)
					{
						writer.Write("	\"rndwave\"\n{{\n");
						foreach (string wave in sound.Waves)
						{
							writer.Write("		\"soundlevel\"	\"{1}{0}\"\n", wave, sndchars);
						}
						writer.Write("	}}\n");
					}
					else
					{
						writer.Write("	\"wave\"	\"{1}{0}\"\n", sound.Waves[0], sndchars);
					}

					writer.Write("}}\n\n", sound.Name);
				}
			}
		}

		//================================================

		public static void LoadCaptionFile(string path, ref ObservableCollection<DialogueLine> dialogueLines, ref string? prefix)
		{
			FileStream stream = File.OpenRead(path);

			var options = new KVSerializerOptions
			{
				HasEscapeSequences = true,
			};

			KVSerializer kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
			KVObject data = kv.Deserialize(stream, options);

			foreach (KVObject subkey in data.Children)
			{
				if (subkey.Name == "Tokens")
				{
					// Now try to find tokens for each of our dialogue lines
					foreach (DialogueLine line in dialogueLines)
					{
						KVValue value = subkey[line.Name];
						if (value == null)
							continue;

						string valueStr = value.ToString();
						if (valueStr == null)
							continue;

						if (prefix == null)
							prefix = valueStr;
						else if (prefix != String.Empty)
						{
							// Compare until divergence is found
							int i = 0;
							while (i < prefix.Length && i < valueStr.Length && prefix[i] == valueStr[i])
								i++;

							prefix = prefix.Substring(0, i);
						}

						// Strip the commands
						line.Caption = Regex.Replace(valueStr, @"\<.*?\>", string.Empty);
					}
					break;
				}
			}

			stream.Close();
		}

		//================================================

		// Try to guess mod directory based on what folders are being used
		public static string ModDirectory = null;

		public static string GetModDirectory(string path, string folder)
		{
			if (ModDirectory != null && !path.Contains(folder))
			{
				return Path.Combine(ModDirectory, folder);
			}

			return path;
		}

		public static void UpdateModDirectory(string path)
		{
			// Look for common folders
			string[] commonFolders = { "sound", "scripts", "resource" };

			foreach (string folder in commonFolders)
			{
				int idx = path.IndexOf(Path.DirectorySeparatorChar + folder + Path.DirectorySeparatorChar);
				if (idx == -1)
					idx = path.IndexOf(Path.AltDirectorySeparatorChar + folder + Path.AltDirectorySeparatorChar);

				if (idx != -1)
				{
					// Get the base path
					ModDirectory = path.Substring(0,idx);
					break;
				}
			}
		}
	}
}
