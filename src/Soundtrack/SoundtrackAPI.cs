﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FistVR;
using Sodalite.Utilities;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	
	//Handles much of the backend workings of soundtracks.
	public static class SoundtrackAPI {

		public static List<SoundtrackManifest> Soundtracks = new List<SoundtrackManifest>();
		//See PluginMain.IsSoundtrack.Value for var to check if is soundtrack.
		public static int  SelectedSoundtrackIndex;
		public static bool IsMix;

		public static SoundtrackManifest GetCurrentSoundtrack => Soundtracks[SelectedSoundtrackIndex];

		public static readonly int MaxRandomWeight = 16;


		//Assemble a complete soundtrack manifest using the path of the file.
		//Can be written as Ass Music for short, symbolizing what you're gonna do with it.
		public static void AssembleMusicData(this SoundtrackManifest manifest) {

			Stopwatch timer = new Stopwatch();
			timer.Start();
			
			//Get path of the soundtrack.
			string dirPath = Path.Combine(Path.GetDirectoryName(manifest.Path), manifest.Location);
			//Get a list of all the folders in the soundtrack folder.
			string[] rawSets = Directory.GetDirectories(dirPath);
			
			// threadpool info for loading audio from disk > audiofile
			// if you guys didnt make 1.5gb soundtracks this wouldnt be necessary now would it
			int tracksToLoad = 0;
			int tracksLoaded = 0;
			
			//All the sets assembled.
			var sets = new List<TrackSet>();
			foreach (var rawSet in rawSets) {
				//Standard format of a set [Type]_[Situation]_[Metadata1]-[Metadata2]..._[Name]
				//Metadata part is optional. Sans metadata, [Type]_[Situation]_[Name]
				string[] splitName = Path.GetFileName(rawSet).Split('_');
				if (File.Exists(rawSet) && !rawSet.Contains(".ogg")) //it was ingesting the fucking yaml :/
					continue;
				var set = new TrackSet();
				set.RandomWeight = MaxRandomWeight;
				set.Tracks = new List<Track>();
				set.Type = splitName[0];
				set.Situation = splitName[1];
				if (splitName.Length == 3) { //if metadata does not exist
					set.Metadata = new[] { "" }; //i don wanna deal w nulls
					set.Name = splitName[2];
				}
				else { //if metadata exists
					set.Metadata = splitName[2].Split('-');
					set.Name = splitName[3];
				}
				
				// Go thru all tracks in each folder
				var rawTrackLocations = Directory.GetFiles(rawSet, "*.ogg", SearchOption.TopDirectoryOnly);
				foreach (var rawTrackLocation in rawTrackLocations) {
					//Standard format of a track [Type]_[Metadata1]-[Metadata2]..._[Name]
					//Metadata part is optional. Sans metadata, [Type]_[Name]
					var fileName = Path.GetFileName(rawTrackLocation);
					var track = new Track();
					if (Path.GetExtension(fileName) != ".ogg")
						PluginMain.DebugLog.LogError($"{fileName} has an invalid extension! (Valid extensions: .ogg, file extension: {Path.GetExtension(fileName)})");
					else {
						tracksToLoad++;
						ThreadPool.QueueUserWorkItem(state =>
						{
							track.Clip = Common.LoadClip(rawTrackLocation);
							Interlocked.Increment(ref tracksLoaded);
						});
					}

					string[] splitTrackName = Path.GetFileNameWithoutExtension(rawTrackLocation).Split('_');
					track.Type = splitTrackName[0];
					track.Situation = set.Situation; //Copy over the set situation info into here, just in case its needed.
					if (splitTrackName.Length == 2) { //metadata does not exist
						track.Metadata = new[] { "" }; //i don wanna deal w nulls
						track.Name = splitTrackName[1];
					}
					else { //metadata exists
						track.Metadata = splitTrackName[1].Split('-');
						track.Name = splitTrackName[2];
					}
					set.Tracks.Add(track);
				}
				
				sets.Add(set);
			}
			
			for (int t = 0; t < 3000; t++) {
				if (tracksLoaded == tracksToLoad)
					break;
				Thread.Sleep(10);
				if(t == 2999)
					PluginMain.DebugLog.LogError("Was not able to load all tracks on time!");
			}
			
			//logging
			foreach (var set in sets) {
				PluginMain.DebugLog.LogInfo($"Loading set {set.Name}, {set.Type}, {set.Tracks.Count}, {set.Situation}");
			}
			
			timer.Stop();
			PluginMain.DebugLog.LogInfo($"Loaded soundtrack in {timer.Elapsed}");
			
			
			manifest.Sets = sets;
			manifest.Loaded = true;
		}

		//Convert a Yamlfest into a Manifest. As Yamlfest doesnt contain its path, it gotta be manually added.
		public static SoundtrackManifest ToManifest(this SoundtrackYamlfest yamlfest, string path) {
			var manifest = new SoundtrackManifest();
			manifest.Name = yamlfest.Name;
			manifest.Guid = yamlfest.Guid;
			manifest.Path = path;
			manifest.Location = yamlfest.Location;
			manifest.Loaded = false;
			manifest.GameMode = yamlfest.GameMode;
			return manifest;
		}
		
		//Loads new soundtrack to be ran.
		public static void LoadSoundtrack(int index) {
			//Flag the game that we're doing soundtrack. Unflagging is done in BankAPI.SwapBanks.
			PluginMain.IsSoundtrack.Value = true;
			SelectedSoundtrackIndex = index;
		}

		public static TrackSet[] GetAllSets(string type, int situation) {
			PluginMain.DebugLog.LogInfo($"Getting set of type {type}, {situation}. Cur soundtrack: {Soundtracks[SelectedSoundtrackIndex].Guid}");
			var soundtrack = Soundtracks[SelectedSoundtrackIndex];
			var sets = soundtrack.Sets
			   .Where(x => x.Type == type)
			   .Where(x => x.Situation.TimingsMatch(situation))
			   .ToArray();
			if(!sets.Any()) //If there are no sets that match, get fallback ones.
				sets = soundtrack.Sets
				   .Where(x => x.Type == type)
				   .Where(x => x.Situation == "fallback")
				   .ToArray();
			if(!sets.Any())
				PluginMain.DebugLog.LogError($"No set! Are you sure you have a set for the situation {situation}, or a fallback?");
			return sets;
		}

		public static TrackSet[] GetAllSetsWithMetadata(string type, int situation, string[] metadatas) {
			var sets = GetAllSets(type, situation);
			/*PluginMain.DebugLog.LogInfo("Available sets:");
			foreach (var set in sets) {
				PluginMain.DebugLog.LogInfo($"{set.Name}, {set.Type}, {set.Situation}, [{string.Join(", ", set.Metadata)}]");
			}*/
			List<TrackSet> filteredSet = new List<TrackSet>();
			foreach (var metadata in metadatas)
				filteredSet.AddRange(sets.Where(x => x.Metadata.Contains(metadata)));
			if (!filteredSet.Any()) { // Fallback if none exists 
				PluginMain.DebugLog.LogError("No set! Getting fallback,");
				return sets;
			}

			return filteredSet.ToArray();
		}

		public static TrackSet GetWeightedRandomSet(TrackSet[] sets)
		{
			int weightSum = sets.Sum(x => x.RandomWeight);
			int randomValue = UnityEngine.Random.Range(0, weightSum);
			int progressiveSum = 0;
			TrackSet? chosenSet = null;
			
			foreach (TrackSet set in sets) {
				progressiveSum += set.RandomWeight;
				if (randomValue < progressiveSum) {
					chosenSet = set;
					break;
				}
			}
			
			foreach (TrackSet set in sets) {
				// Adjust weights
				if (set == chosenSet)
					set.RandomWeight = 1;
				else if (set.RandomWeight < MaxRandomWeight)
					set.RandomWeight *= 2;
			}
			
			return chosenSet;
		}
		
		public static TrackSet GetSetWithMetadata(string type, int situation, string[] metadatas) {
			var sets = GetAllSetsWithMetadata(type, situation, metadatas);
			var item = GetWeightedRandomSet(sets);
			PluginMain.DebugLog.LogInfo($"Selecting trackset {item.Name}");
			return item;
		}


		public static TrackSet GetSet(string type, int situation) {
			var sets = GetAllSets(type, situation);
			var item = GetWeightedRandomSet(sets);
			PluginMain.DebugLog.LogInfo($"Selecting trackset {item.Name}");
			return item;
		}
		
		//Does not account for fallback.
		//Compares sequencetiming format (See _FORMAT.txt) and the current situation provided by TnH
		//Mainly to handle globs
		public static bool TimingsMatch(this string seqTiming, int situation) {
			//This is a parse hell.
			//There's probably a better way to do this
			if (seqTiming == "all")
				return true;
			
			if (seqTiming == "fallback")
				return false;
			
			string[] sequences = seqTiming.Split(',');
			foreach (string sequence in sequences)
			{
				if (sequence == "death") {
					if (situation == -1)
						return true;
				}
				else if (sequence == "win") {
					if (situation == -2)
						return true;
				}
				else if (sequence.StartsWith("ge")) {
					//This is above a number (inclusive). EG ge3
					if (situation >= int.Parse(sequence.Substring(2)))
						return true;
				}
				else if (sequence.StartsWith("le")) {
					//This is below a number (inclusive). EG le3
					if (situation <= int.Parse(sequence.Substring(2)))
						return true;
				}
				else if (sequence.Contains('-')) {
					//This is a range. EG 1-3
					string[] s = sequence.Split('-');
					
					if (int.TryParse(s[0], out int min) && int.TryParse(s[1], out int max)) {
						if (situation >= min && situation <= max)
							return true;
					}
				}
				
				if (sequence == situation.ToString())
					return true;
			}
			
			return false;
		}

		public static void EnableSoundtrackFromGUID(string guid) {
			for (int i = 0; i < Soundtracks.Count; i++)
				if (Soundtracks[i].Guid == guid)
					SelectedSoundtrackIndex = i;
		}

		public static Texture2D GetIcon(int soundtrack) {
			return GeneralAPI.GetIcon(Soundtracks[soundtrack].Guid, new[] { Path.Combine(Path.GetDirectoryName(Soundtracks[soundtrack].Path), "icon.png") });
		}

		
		
		public static ValueTuple<AudioClip?, bool> GetSnippet(SoundtrackManifest manifest) {
			bool isLoop = true;
			string pathOgg = Path.Combine(Path.Combine(Path.GetDirectoryName(manifest.Path)!, manifest.Location), "preview_loop.ogg");
			if (!File.Exists(pathOgg)) {
				pathOgg = Path.Combine(Path.Combine(Path.GetDirectoryName(manifest.Path)!, manifest.Location), "preview.ogg");
				isLoop = false;
			}

			AudioClip? clip = null;
			if(File.Exists(pathOgg))
				clip = Common.LoadClip(pathOgg);
			return (clip, isLoop);
		}
	}
}