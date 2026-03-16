using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;


namespace SourceSoundScripter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		public ObservableCollection<SoundEntry> SoundEntries = new ObservableCollection<SoundEntry>();

		public MainWindow()
        {
            InitializeComponent();

			SoundscriptList.ItemsSource = SoundEntries;
			SoundscriptList.DataContext = SoundEntries;
		}

		//============================================================================
		// List Drag
		//============================================================================
		private void Grid_DragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}

		private void Grid_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

				// Test the first file
				if (Path.GetExtension(files[0]) == ".wav" || Path.GetExtension(files[0]) == ".mp3")
				{
					ScriptFileUtils.UpdateModDirectory(files[0]);

					// Add dropped sounds to our list
					SoundEdit soundEditWindow = new SoundEdit(this, files);
					if (soundEditWindow.ShowDialog() == true)
					{
						// Sounds have been added
					}
				}
				else if (Path.GetExtension(files[0]) == ".txt")
				{

				}
			}
		}


		//============================================================================
		// List Buttons
		//============================================================================
		private void SoundscriptAdd_Click(object sender, RoutedEventArgs e)
		{
			SoundEntries.Add(new SoundEntry("my_sounds.new"));
			SpoilSave();
		}

		private void SoundscriptAddSounds_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = ScriptFileUtils.GetModDirectory(openFileDialog.InitialDirectory, "sound");
			openFileDialog.Multiselect = true;
			openFileDialog.RestoreDirectory = true;
			openFileDialog.Filter = "Sound files (*.wav;*.mp3)|*.wav;*.mp3|All files (.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				ScriptFileUtils.UpdateModDirectory(openFileDialog.FileName);

				SoundEdit soundEditWindow = new SoundEdit(this, openFileDialog.FileNames);
				if (soundEditWindow.ShowDialog() == true)
				{
					// Sounds have been added
				}
			}
		}
		private void SoundscriptEdit_Click(object sender, RoutedEventArgs e)
		{
			SoundEdit soundEditWindow = null;
			if (SoundscriptList.SelectedItems.Count > 1)
			{
				// Multiple sounds
				soundEditWindow = new SoundEdit(this, SoundscriptList.SelectedItems.Cast<SoundEntry>());
			}
			else if (SoundscriptList.SelectedItem != null)
			{
				// One sound
				soundEditWindow = new SoundEdit(this, (SoundEntry)SoundscriptList.SelectedItem);
			}
			else
			{
				// No sound
				return;
			}

			if (soundEditWindow.ShowDialog() == true)
			{
				// Edits have been made
			}
		}
		private void SoundscriptDelete_Click(object sender, RoutedEventArgs e)
		{
			List<SoundEntry> deleteList = new List<SoundEntry>(SoundscriptList.SelectedItems.Cast<SoundEntry>());
			foreach (SoundEntry sound in deleteList)
			{
				SoundEntries.Remove(sound);
			}
		}

		//============================================================================
		// Defaults
		//============================================================================
		public void GetDefaultValues(ref string channel, ref string volume, ref string pitch, ref string sndlvl, ref string chars)
		{
			channel = Default_Channel.Text;
			volume = Default_Volume.Text;
			pitch = Default_Pitch.Text;
			sndlvl = Default_SndLvl.Text;
			chars = Default_SndChars_Custom.Text;

			if (channel == "Custom")
				channel = Default_Channel_Custom.Text;
			if (volume == "Custom")
				volume = Default_Volume_Custom.Text;
			if (pitch == "Custom")
				pitch = Default_Pitch_Custom.Text;
			if (sndlvl == "Custom")
				sndlvl = Default_SndLvl_Custom.Text;
		}

		public void GetDefaultValues(ref TextBlock channel, ref TextBlock volume, ref TextBlock pitch, ref TextBlock sndlvl, ref TextBlock chars)
		{
			channel.Text = Default_Channel.Text;
			volume.Text = Default_Volume.Text;
			pitch.Text = Default_Pitch.Text;
			sndlvl.Text = Default_SndLvl.Text;
			chars.Text = Default_SndChars_Custom.Text;

			if (channel.Text == "Custom")
				channel.Text = Default_Channel_Custom.Text;
			if (volume.Text == "Custom")
				volume.Text = Default_Volume_Custom.Text;
			if (pitch.Text == "Custom")
				pitch.Text = Default_Pitch_Custom.Text;
			if (sndlvl.Text == "Custom")
				sndlvl.Text = Default_SndLvl_Custom.Text;
		}

		private void Default_Channel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Default_Channel_Custom != null && Default_Channel != null && e.AddedItems != null && e.AddedItems[0] != null)
				Default_Channel_Custom.IsEnabled = (e.AddedItems[0].ToString().EndsWith("Custom"));
		}

		private void Default_Volume_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Default_Volume_Custom != null && Default_Volume != null && e.AddedItems != null && e.AddedItems[0] != null)
				Default_Volume_Custom.IsEnabled = (e.AddedItems[0].ToString().EndsWith("Custom"));
		}

		private void Default_Pitch_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Default_Pitch_Custom != null && Default_Pitch != null && e.AddedItems != null && e.AddedItems[0] != null)
				Default_Pitch_Custom.IsEnabled = (e.AddedItems[0].ToString().EndsWith("Custom"));
		}

		private void Default_SndLvl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (Default_SndLvl_Custom != null && Default_SndLvl != null && e.AddedItems != null && e.AddedItems[0] != null)
				Default_SndLvl_Custom.IsEnabled = (e.AddedItems[0].ToString().EndsWith("Custom"));
		}

		//============================================================================
		// Dialogue Editor
		//============================================================================
		private void DialogueEditorButton_Click(object sender, RoutedEventArgs e)
		{
			DialogueEditor dialogueEditWindow = new DialogueEditor(ref SoundEntries);
			if (dialogueEditWindow.ShowDialog() == true)
			{

			}
		}

		//============================================================================
		// Save/Load
		//============================================================================
		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (SoundEntries.Count == 0)
				return;

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.InitialDirectory = ScriptFileUtils.GetModDirectory(saveFileDialog.InitialDirectory, "scripts");
			saveFileDialog.RestoreDirectory = true;
			saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (.*)|*.*";

			if (saveFileDialog.ShowDialog() == true)
			{
				ScriptFileUtils.SaveFile(this, saveFileDialog.FileName, ref SoundEntries);
				ScriptFileUtils.UpdateModDirectory(saveFileDialog.FileName);
			}
		}
		private void LoadButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = ScriptFileUtils.GetModDirectory(openFileDialog.InitialDirectory, "scripts");
			openFileDialog.RestoreDirectory = true;
			openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				if (Path.GetFileNameWithoutExtension(openFileDialog.FileName) == "game_sounds_manifest")
				{
					MessageBoxResult result = MessageBox.Show("This program is not designed to use game_sounds_manifest.txt. Please select one of the files it mounts instead (e.g. game_sounds_world.txt).", "Cannot Load", MessageBoxButton.OK, MessageBoxImage.Information);
					LoadButton_Click(sender, e);
					return;
				}
				else if (SoundEntries.Count > 0)
				{
					MessageBoxResult result = MessageBox.Show("Do you want to discard your existing sound entries?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
					if (result != MessageBoxResult.Yes)
						return;
				}

				SoundEntries.Clear();
				ScriptFileUtils.LoadFile(openFileDialog.FileName, ref SoundEntries);
				ScriptFileUtils.UpdateModDirectory(openFileDialog.FileName);

				// Set defaults to standard
				Default_Channel.SelectedItem = Default_Channel_Norm;
				Default_Volume.SelectedItem = Default_Volume_Norm;
				Default_Pitch.SelectedItem = Default_Pitch_Norm;
				Default_SndLvl.SelectedItem = Default_SndLvl_Norm;
				Default_SndChars_Custom.Text = "";
			}
		}
		private void SpoilSave()
		{

		}
	}
}