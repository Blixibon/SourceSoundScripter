# Source Sound Scripter

This is a tool that can save and load soundscript files. It's intended to serve as a more stable alternative to [VSoundEdit](https://developer.valvesoftware.com/wiki/VSoundEdit), a partially released Valve tool with a similar purpose.

This tool's main features include:

- Adding and modifying soundscripts en masse
- Exporting sounds into caption entries

[ValveKeyValue](https://github.com/ValveResourceFormat/ValveKeyValue) is used for keyvalue parsing.

---

## Adding and modifying soundscripts

See [Soundscripts on the VDC](https://developer.valvesoftware.com/wiki/Soundscripts) for more documentation regarding soundscripts themselves.

### How to create soundscripts from sounds

1. Click on the "Add Sounds..." button in the top right corner and select all of the relevant sound files.
2. Edit the defaults at the bottom of the window according to what you want the soundscripts to use. If specific entries need different commands, right click on them and select Edit.
3. Click on the "Save" button in the bottom right corner and save to the desired file.
4. Add your new file to `game_sounds_manifest.txt`, if needed.

### How to edit an existing soundscript file

1. Click on the "Load" button in the bottom right corner and select the soundscript file you would like to use.
2. Make any changes you'd like to make to the sound entries, or add new ones.
3. Click on the "Save" button in the bottom right corner and save the file.

Note that any comments (lines preceded by `//`) within the file will be lost.

## Exporting sounds into captions

The caption component of this tool is done through the Dialogue Editor, which can be opened from the bottom left corner once you have created or loaded your desired soundscripts. The "Line" column is the caption itself, while the "Prefix" column is used for commands at the beginning of each caption (e.g. `<clr:255,51,0>`).

The main attraction of the Dialogue Editor is that you can paste lines directly from another spreadsheet or text document. For example:

```
This is a line.
This is another line.
This is yet another line.
```

This can be pasted directly into the desired rows, and then you can copy a standard prefix into the rows' prefix cells.

You can turn this data to usable lines by clicking on the "Copy Captions" button in the bottom right corner, which will copy everything to your clipboard so that you can paste it into your desired caption file. You can also load existing caption data by clicking on the adjacent "Load Captions" button.

The Dialogue Editor also has a "Scan Phonemes" feature, which just adds a new column that indicates whether the sound files referenced by each soundscript has embedded phonemes.
