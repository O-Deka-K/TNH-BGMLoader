﻿using System.Collections.Generic;
using UnityEngine;

namespace TNHBGLoader.Soundtrack {
	
	public class Track {
		public AudioClip Clip;
		public string    Situation;
		public string[]  Metadata; //sip (Skip In Place), dnf (Do Not Fade), loop, etc
		public string    Name;
		public string    Type;
	}

	public class TrackSet {
		public List<Track>  Tracks;
		public string   Situation;
		public string[] Metadata;
		public string   Name;
		public string   Type;
		public int      RandomWeight;
	}
	
	public class SoundtrackManifest
	{
		//Check to ensure the soundtrack is actually loaded
		public bool Loaded;
		//Name of the soundtrack
		public string         Name;
		//Path of the soundtrack in the filesystem
		public string         Path;
		//GUID of the soundtrack
		public string         Guid;
		//Location of where the soundtracks are located, relative to the yamlfest.
		public string		  Location;
		//List of sets
		public List<TrackSet> Sets;
		//tnh for Take and Hold
		public string GameMode;
	}
	
	
	/*
	//Audio files used during the Hold phase
	public class HoldData {
		//Timing dictates when and where the sequence may be played
		public string    Timing;
		//Name. Doesn't do much of anything, really.
		public string    Name;
		//Right when you touch the hold cube
		public Track[] Intro;
		//Plays after intro
		public Track[] Lo;
		//Transition to MedHi
		public Track[] Transition;
		//Plays at later stages of the hold
		public Track[] MedHi;
		//End of the hold
		public Track[] End;
		//Alt end song for if you FAIL the hold, you loser.
		public Track[] EndFail;

		public Track[] Take;
		
		//Alt system, Phases.
		//Eh. Fuck it. List time.
		public List<List<Track>> Phase;
		public List<List<Track>> PhaseTransition;

		//Replace sound of OrbTouch.
		public Track[] OrbActivate;
		public Track[] OrbHoldWave;
		public Track[] OrbSuccess;
		public Track[] OrbFailure;
	}
	
	//Metadata for the Take phase
	public class TakeData {
		//Timing dictates when and where the sequence may be played
		public string    Timing;
		//Name. Doesn't do much of anything, really.
		public string    Name;
		//Looping track during the take
		//Takes don't have any metadata, so don't worry about it not being a Track and being an AudioClip.
		public Track Track;
	}*/
}