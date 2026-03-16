using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SourceSoundScripter
{
	public class SoundEntry : INotifyPropertyChanged
	{
		public SoundEntry(string name = "", string wave = "", string chan = "", string vol = "", string p = "", string lvl = "", string chars = "")
		{
			Name = name; DisplayWave = wave;
			Channel = chan; Volume = vol; Pitch = p; SndLvl = lvl; SndChars = chars;

			Waves = new List<string>();
			if (wave != "")
				Waves.Add(wave);
		}

		private string _name;
		public string Name { get { return _name; } set { _name = value; OnPropertyChanged("Name"); } }

		private string _displaywave; // What's shown on the main window
		public string DisplayWave { get { return _displaywave; } set { _displaywave = value; OnPropertyChanged("DisplayWave"); } }

		public List<string> Waves { get; set; }

		public string Channel { get; set; }
		public string Volume { get; set; }
		public string Pitch { get; set; }
		public string SndLvl { get; set; }
		public string SndChars { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
