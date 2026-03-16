using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace SourceSoundScripter
{
	/// <summary>
	/// Interaction logic for SoundEdit.xaml
	/// </summary>
	public partial class SoundEdit : Window
	{
		public SoundEdit(MainWindow parentWindow)
		{
			InitializeComponent();
			WaveList.ItemsSource = WaveListData;
			WaveList.DataContext = WaveListData;

			Owner = parentWindow;

			// Set placeholders
			parentWindow.GetDefaultValues(ref Channel_Placeholder, ref Volume_Placeholder, ref Pitch_Placeholder, ref SndLvl_Placeholder, ref SndChars_Placeholder);
		}

		// Initializing with specific sound
		public SoundEdit(MainWindow parentWindow, SoundEntry sound) : this(parentWindow)
		{
			Title += " - Editing \"" + sound.Name + "\"";

			EntryName.Text = sound.Name;
			foreach (string wave in sound.Waves)
				WaveListData.Add(wave);

			Channel.Text = sound.Channel;
			Volume.Text = sound.Volume;
			Pitch.Text = sound.Pitch;
			SndLvl.Text = sound.SndLvl;
			SndChars.Text = sound.SndChars;

			SourceSoundEntries.Add(GetSoundEntryIndex(sound));
		}

		// Initializing with multiple sounds
		public SoundEdit(MainWindow parentWindow, IEnumerable<SoundEntry> soundList) : this(parentWindow)
		{
			Mode = SoundEditMode.Multiple;
			Title += " - Editing " + soundList.Count().ToString() + " sounds";

			// For now, disable the name field
			EntryName.IsEnabled = false;

			SaveButton.Content = "Save Entries";

			foreach (SoundEntry sound in soundList)
			{
				SetOrAmbiguateField(sound.Channel, ref Channel);
				SetOrAmbiguateField(sound.Volume, ref Volume);
				SetOrAmbiguateField(sound.Pitch, ref Pitch);
				SetOrAmbiguateField(sound.SndLvl, ref SndLvl);
				SetOrAmbiguateField(sound.SndChars, ref SndChars);

				WaveListData.Add(sound.Name);
				SourceSoundEntries.Add(GetSoundEntryIndex(sound));
			}
		}

		// Initializing with imported sound files
		public SoundEdit(MainWindow parentWindow, IEnumerable<string> soundList) : this(parentWindow)
		{
			Mode = SoundEditMode.Import;
			Title += " - Importing " + soundList.Count().ToString() + " sounds";

			// Show the tip text, collapse the wave buttons
			TipTxt.Visibility = Visibility.Visible;
			WaveAdd.Visibility = Visibility.Collapsed;
			WaveRemove.Visibility = Visibility.Collapsed;

			SaveButton.Content = "Save Entries";

			// Default format
			EntryName.Text = "my_sounds.{0}";

			foreach (string sound in soundList)
			{
				WaveListData.Add(String.Format(EntryName.Text, System.IO.Path.GetFileNameWithoutExtension(sound)));

				// Find the first folder that's just "sound" and cut it off
				// A bit hacky, but it'll do
				int idx = sound.IndexOf("sound");
				if (idx != -1)
					ImportedSoundWaves.Add(sound.Substring(idx + 6));
				else
					ImportedSoundWaves.Add(sound);
			}
		}

		private void SetOrAmbiguateField(string input, ref TextBox output)
		{
			if (output.Text == "")
			{
				output.Text = input;
			}
			else if (output.Text != input)
			{
				// Sounds have different values, so ignore this field
				output.Text = "...";
			}
		}

		//============================================================================
		// Entry Name
		//============================================================================
		private void EntryName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Mode == SoundEditMode.Import)
			{
				// Don't do this if we're in the midst of typing a format key
				int bracketIdx = EntryName.Text.IndexOf('{');
				while (bracketIdx != -1)
				{
					// String ends prematurely
					if (EntryName.Text.Length < bracketIdx + 3)
						return;

					if (EntryName.Text[bracketIdx+1] < '0' || EntryName.Text[bracketIdx+1] > '9')
						return;

					if (EntryName.Text[bracketIdx+2] != '}')
						return;

					bracketIdx = EntryName.Text.IndexOf('{', bracketIdx+3);
				}

				// Change the prefixes
				for (int i = 0; i < WaveListData.Count; i++)
				{
					try
					{
						WaveListData[i] = String.Format(EntryName.Text, System.IO.Path.GetFileNameWithoutExtension(ImportedSoundWaves[i]));
					}
					catch (FormatException)
					{
						// This shouldn't happen often, but it can if parameters are typed weirdly (e.g. "0}")
						// This should probably be fixed
					}
				}
			}
		}

		//============================================================================
		// Adding/Removing Waves
		//============================================================================
		private int GetSoundEntryIndex(SoundEntry sound)
		{
			if (Owner is MainWindow mainWindow)
			{
				return mainWindow.SoundEntries.IndexOf(sound);
			}
			return 0;
		}

		private void WaveAdd_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = ScriptFileUtils.GetModDirectory(openFileDialog.InitialDirectory, "sound");
			openFileDialog.Multiselect = true;
			openFileDialog.RestoreDirectory = true;
			openFileDialog.Filter = "Sound files (*.wav;*.mp3)|*.wav;*.mp3|All files (.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				foreach (string sound in openFileDialog.FileNames)
				{
					// Find the first folder that's just "sound" and cut it off
					// A bit hacky, but it'll do
					int idx = sound.IndexOf("sound");
					if (idx != -1)
						WaveListData.Add(sound.Substring(idx + 6));
				}

				ScriptFileUtils.UpdateModDirectory(openFileDialog.FileName);
			}

			HasChanged = true;
		}

		private void WaveRemove_Click(object sender, RoutedEventArgs e)
		{
			List<string> deleteList = new List<string>(WaveList.SelectedItems.Cast<string>());
			foreach (string sound in deleteList)
			{
				WaveListData.Remove(sound);
			}

			HasChanged = true;
		}

		//============================================================================
		// Saving
		//============================================================================
		void Save()
		{
			MainWindow mainWindow = (MainWindow)Owner;
			if (mainWindow == null)
				return;

			switch (Mode)
			{
				case SoundEditMode.Default:
					{
						SoundEntry sound = mainWindow.SoundEntries[SourceSoundEntries[0]];
						SetSoundValues(ref sound);

						sound.Waves.Clear();
						foreach (string wave in WaveListData)
							sound.Waves.Add(wave);

						if (WaveListData.Count > 1)
							sound.DisplayWave = "Multiple";
						if (WaveListData.Count > 0)
							sound.DisplayWave = WaveListData[0];
						else
							sound.DisplayWave = "";
					}
					break;
				case SoundEditMode.Multiple:
					{
						foreach (int idx in SourceSoundEntries)
						{
							SoundEntry sound = mainWindow.SoundEntries[idx];
							SetSoundValues(ref sound);
						}
					}
					break;
				case SoundEditMode.Import:
					{
						// Add new sounds
						for (int i = 0; i < WaveListData.Count; i++)
						{
							SoundEntry sound = new SoundEntry();
							SetSoundValues(ref sound);
							sound.Name = WaveListData[i];

							sound.Waves.Add(ImportedSoundWaves[i]);
							sound.DisplayWave = ImportedSoundWaves[i];

							mainWindow.SoundEntries.Add(sound);
						}
					}
					break;
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			Save();

			CanClose = true;
			Close();
		}
		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			CanClose = true;
			Close();
		}

		private void SetSoundValues(ref SoundEntry sound)
		{
			if (EntryName.Name != "...")
				sound.Name = EntryName.Text;
			if (Channel.Text != "...")
				sound.Channel = Channel.Text;
			if (Volume.Text != "...")
				sound.Volume = Volume.Text;
			if (Pitch.Text != "...")
				sound.Pitch = Pitch.Text;
			if (SndLvl.Text != "...")
				sound.SndLvl = SndLvl.Text;
			if (SndChars.Text != "...")
				sound.SndChars = SndChars.Text;
		}

		private bool SoundHasChanged()
		{
			MainWindow mainWindow = (MainWindow)Owner;
			if (mainWindow == null)
				return false;

			if (HasChanged)
				return true;

			switch (Mode)
			{
				case SoundEditMode.Default:
					{
						SoundEntry sound = mainWindow.SoundEntries[SourceSoundEntries[0]];

						if (EntryName.Text != sound.Name)
							return true;
						if (Channel.Text != sound.Channel)
							return true;
						if (Volume.Text != sound.Volume)
							return true;
						if (Pitch.Text != sound.Pitch)
							return true;
						if (SndLvl.Text != sound.SndLvl)
							return true;
						if (SndChars.Text != sound.SndChars)
							return true;
					}
					break;
				case SoundEditMode.Multiple:
					{
						SoundEntry sound = mainWindow.SoundEntries[SourceSoundEntries[0]];

						if (EntryName.Name != "..." && EntryName.Text != sound.Name)
							return true;
						if (Channel.Name != "..." && Channel.Text != sound.Channel)
							return true;
						if (Volume.Name != "..." && Volume.Text != sound.Volume)
							return true;
						if (Pitch.Name != "..." && Pitch.Text != sound.Pitch)
							return true;
						if (SndLvl.Name != "..." && SndLvl.Text != sound.SndLvl)
							return true;
						if (SndChars.Name != "..." && SndChars.Text != sound.SndChars)
							return true;
					}
					break;
				case SoundEditMode.Import:
					return true;
			}

			return false;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!CanClose)
			{
				// See if anything changed
				if (!SoundHasChanged())
					return;

				MessageBoxResult result = MessageBox.Show("Do you want to save changes to the sound?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
				if (result == MessageBoxResult.Yes)
				{
					Save();
				}
			}
		}

		//============================================================================

		enum SoundEditMode
		{
			Default = 0,	// One sound
			Multiple = 1,	// Multiple sounds
			Import = 2,		// New sound(s)
		}

		private ObservableCollection<string> WaveListData = new ObservableCollection<string>();

		private List<int> SourceSoundEntries = new List<int>(); // Used by Default and Multiple modes for getting sound indices
		private List<string> ImportedSoundWaves = new List<string>(); // Used by Import mode for new sounds

		private SoundEditMode Mode = SoundEditMode.Default;

		private bool CanClose = false;
		private bool HasChanged = false;
	}
}
