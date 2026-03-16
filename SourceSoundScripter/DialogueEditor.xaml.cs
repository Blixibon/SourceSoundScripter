using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.IO;
using Microsoft.Win32;

namespace SourceSoundScripter
{
	public class DialogueLine : INotifyPropertyChanged
	{
		public DialogueLine(string name = "", string wave = "", string caption = "")
		{
			Name = name; Wave = wave; Caption = caption; HasPhonemes = false;
		}

		public string Name { get; set; }
		public string Wave { get; set; }

		private string _caption;
		public string Caption { get { return _caption; } set { _caption = value; OnPropertyChanged("Caption"); } }

		private bool _hasphonemes;
		public bool HasPhonemes { get { return _hasphonemes; } set { _hasphonemes = value; OnPropertyChanged("HasPhonemes"); } }

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// Interaction logic for DialogueEditor.xaml
	/// </summary>
	public partial class DialogueEditor : Window
	{
		ObservableCollection<DialogueLine> DialogueLines = new ObservableCollection<DialogueLine>();

		public DialogueEditor()
		{
			InitializeComponent();
			DialogueList.DataContext = DialogueLines;
		}

		public DialogueEditor(ref ObservableCollection<SoundEntry> soundEntries) : this()
		{
			foreach (SoundEntry sound in soundEntries)
			{
				// Don't support rndwave right now
				if (sound.Waves.Count > 1)
					continue;

				// Guess absolute path
				string AbsSound = Path.Combine(ScriptFileUtils.GetModDirectory("", "sound"), sound.DisplayWave);
				if (!File.Exists(AbsSound))
					continue;

				DialogueLines.Add(new DialogueLine(sound.Name, AbsSound));
			}
		}

		//============================================================================
		// Dialogue List functions
		//============================================================================
		private void DialogueList_Paste(object sender, ExecutedRoutedEventArgs e)
		{
			string clipboardText = Clipboard.GetText();
			if (String.IsNullOrEmpty(clipboardText) || !clipboardText.Contains('\n'))
				return;

			if (DialogueList.SelectedIndex == -1)
				return;

			// Split newlines into different rows
			string[] rows = clipboardText.Replace("\r", "").Split('\n');
			for (int i = 0; i < rows.Length; i++)
			{
				int listIdx = DialogueList.SelectedIndex + i;
				if (listIdx == -1)
				{
					// No rows remaining
					break;
				}

				DialogueLines[listIdx].Caption = rows[i];
			}

			e.Handled = true;
		}

		private void DialogueList_Delete(object sender, ExecutedRoutedEventArgs e)
		{
			// Only delete line column
			foreach (DialogueLine line in DialogueList.SelectedItems)
			{
				line.Caption = "";
			}

			e.Handled = true;
		}

		//============================================================================
		// Phoneme functions
		//============================================================================
		private bool ScanWaveForPhonemes(string wave)
		{
			//FileStream stream = File.OpenRead(wave);

			// Cursed
			string file = File.ReadAllText(wave);
			if (file.LastIndexOf("WORDS") != -1)
				return true;

			return false;
		}

		private void PhonemeScan_Click(object sender, RoutedEventArgs e)
		{
			DialogueList_HasPhonemes.Visibility = Visibility.Visible;

			if (DialogueList.SelectedItems.Count > 0)
			{
				foreach (DialogueLine line in DialogueList.SelectedItems)
				{
					line.HasPhonemes = ScanWaveForPhonemes(line.Wave);
				}
			}
			else
			{
				// Scan all
				foreach (DialogueLine line in DialogueLines)
				{
					line.HasPhonemes = ScanWaveForPhonemes(line.Wave);
				}
			}
		}

		//============================================================================
		// Caption functions
		//============================================================================
		private void CaptionLoad_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.InitialDirectory = ScriptFileUtils.GetModDirectory(openFileDialog.InitialDirectory, "resource");
			openFileDialog.RestoreDirectory = true;
			openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				string? prefix = null;
				ScriptFileUtils.LoadCaptionFile(openFileDialog.FileName, ref DialogueLines, ref prefix);
				ScriptFileUtils.UpdateModDirectory(openFileDialog.FileName);

				if (prefix != null)
				{
					CaptionPrefix.Text = prefix;
				}
			}
		}

		private void CaptionCopy_Click(object sender, RoutedEventArgs e)
		{
			string captions = "";

			foreach (DialogueLine line in DialogueLines)
			{
				if (line.Caption == "")
					continue;

				captions += String.Format("	\"{0}\"		\"{1}{2}\"\n", line.Name, CaptionPrefix.Text, line.Caption);
			}

			Clipboard.SetText(captions);
		}
	}
}
