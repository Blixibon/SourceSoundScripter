using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
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
			KVObject? data = null;

			try
			{
				data = kv.Deserialize(stream);
			}
			catch (Exception e)
			{
				// Catch exceptions as nonfatal load failures and try to give hints for common cases
				string msg = "";
				string fileName = Path.GetFileName(path);

				if (fileName.StartsWith("closecaption_"))
				{
					msg = String.Format("{0} is a caption file, not a soundscript file.\n\nTo load a caption file, use the Dialogue Editor.", fileName);
				}
				else
				{
					msg = String.Format("Failed to load {0}:\n\n\"{1}\"", fileName, e.Message);
				}

				MessageBoxResult result = MessageBox.Show(msg, "Cannot Load", MessageBoxButton.OK, MessageBoxImage.Error);
				stream.Close();
				return;
			}

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

		public static void LoadCaptionFile(string path, ref ObservableCollection<DialogueLine> dialogueLines)
		{
			Stream stream = File.OpenRead(path);

			bool usingCorrectedChars = false;

			// HACKHACK (and it's a bad one!)
			// ValveKeyValue, or at least this version of it, has issues with [] or # in value contents.
			// So make sure the file doesn't have any first
			StreamReader reader = new StreamReader(stream);
			char fileChar = '\0';
			int intChar = 0;
			while ((intChar = reader.Read()) != -1)
			{
				fileChar = (char)intChar;
				switch (fileChar)
				{
					case '#':
					case '[':
					case ']':
						usingCorrectedChars = true;
						break;
				}
			}

			stream.Position = 0;

			if (usingCorrectedChars)
			{
				// We need to temporarily correct these characters
				// Create a new string and reassign the stream to it
				StringBuilder correctedFileContent = new StringBuilder();
				while ((intChar = reader.Read()) != -1)
				{
					fileChar = (char)intChar;
					switch (fileChar)
					{
						case '#':
							fileChar = '\a';
							break;
						case '[':
							fileChar = '\uFFFE';
							break;
						case ']':
							fileChar = '\uFFFF';
							break;
					}

					correctedFileContent.Append(fileChar);
				}

				stream.Close();
				stream = new MemoryStream(Encoding.Unicode.GetBytes(correctedFileContent.ToString()));
			}

			var options = new KVSerializerOptions
			{
				HasEscapeSequences = true,
			};

			KVSerializer kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
			KVObject? data = null;

			try
			{
				data = kv.Deserialize(stream, options);
			}
			catch (Exception e)
			{
				// Catch exceptions as nonfatal load failures and try to give hints for common cases
				string msg = String.Format("Failed to load {0}:\n\n\"{1}\"", Path.GetFileName(path), e.Message);

				if (e.Message == "Found end of file while trying to read token.")
					msg += "\n\nCheck for stray double quotes. (e.g. \"alert1\"\t\"\"Alert!\")";

				MessageBoxResult result = MessageBox.Show(msg, "Cannot Load", MessageBoxButton.OK, MessageBoxImage.Error);
				stream.Close();
				return;
			}

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

						// Find first instance of > that isn't succeeded by a <
						int cmdEnd = valueStr.IndexOf('>');
						if (cmdEnd >= 0)
						{
							while (cmdEnd < valueStr.Length && valueStr[cmdEnd+1] == '<')
								cmdEnd = valueStr.IndexOf('>', cmdEnd+1);

							line.Prefix = valueStr.Substring(0, cmdEnd+1);
							line.Caption = valueStr.Substring(cmdEnd+1);
						}
						else
						{
							line.Caption = valueStr;
						}

						if (usingCorrectedChars)
						{
							line.Caption = line.Caption.Replace('\a', '#').Replace('\uFFFE', '[').Replace('\uFFFF', ']');
						}

						// Strip the commands
						//line.Caption = Regex.Replace(valueStr, @"\<.*?\>", string.Empty);
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
