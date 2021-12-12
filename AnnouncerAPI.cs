﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FistVR;
using TNHBGLoader;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace TNH_BGLoader
{
	public class AnnouncerAPI
	{
		//Announcer Manifests, Announcer GUIDs, and Announcer Index - USE GUIDS!
		public static List<AnnouncerManifest> Announcers = new List<AnnouncerManifest>();
		public static int LoadedAnnouncerIndex = 0;
		public static AnnouncerManifest CurrentAnnouncer => Announcers[LoadedAnnouncerIndex];
		
		public static void SwapAnnouncer(string GUID)
		{
			LoadedAnnouncerIndex = GetAnnouncerIndexFromGUID(GUID);
			PluginMain.LastLoadedAnnouncer.Value = CurrentAnnouncer.GUID;
		}
		
		public static int GetAnnouncerIndexFromGUID(string GUID) => Announcers.IndexOf(GetManifestFromGUID(GUID));
		public static int GetAnnouncerIndexFromManifest(AnnouncerManifest manifest) => Announcers.IndexOf(manifest);
		public static AnnouncerManifest GetManifestFromGUID(string GUID) => Announcers.FindAll(a => a.GUID == GUID).First();
		
		//I should probably also co-routine this, but co-routine throws a hissyfit whenever i do for some reason.
		public static Texture2D GetAnnouncerTexture(AnnouncerManifest announcer)
		{
			PluginMain.LogSpam("Loading image for " + announcer.GUID);
			var pbase = Path.GetDirectoryName(announcer.Location);
			//assembles all the potential locations for the icon, in descending order of importance.
			string[] paths = new string[0];
			if (announcer.GUID == "h3vr.default" || announcer.GUID == "h3vr.corrupted")
				paths = new string[] { Path.Combine(PluginMain.AssemblyDirectory, "default/announcer_default.png") };
			else paths = new string[] { pbase + "/icon.png", Directory.GetParent(pbase) + "/icon.png" };

			//iterate through all paths, get the first one that exists
			foreach (var path in paths)
			{
				if (File.Exists(path))
				{
					byte[] byteArray = File.ReadAllBytes(path);
					Texture2D tex = new Texture2D(1, 1);
					tex.LoadImage(byteArray);
					if (tex != null)
					{
						PluginMain.LogSpam("Loading icon from " + path);
						return tex;
					}
				}
			}
			PluginMain.DebugLog.LogError("Cannot find icon for " + announcer.GUID + "!\nPossible locations:\n" + String.Join("\n", paths));
			return null;
		}

		public static AudioClip GetRandomPreview(string guid)
		{
			if (guid == "h3vr.default") return GetAudioFromFile(Path.Combine(PluginMain.AssemblyDirectory, "default/announcer_default.wav"));
			if (guid == "h3vr.corrupted") return GetAudioFromFile(Path.Combine(PluginMain.AssemblyDirectory, "default/announcer_corrupted.wav"));
			var manifest = GetManifestFromGUID(guid);
			int rand = Random.Range(0, manifest.Previews.Count);
			return GetAudioFromFile(manifest.Previews[rand]);
		}
		public static AnnouncerManifest YamlfestToManifest(AnnouncerYamlfest yamlfest)
		{
			yamlfest.VoiceLines = Path.Combine(Path.GetDirectoryName(yamlfest.Location), yamlfest.VoiceLines);
			AnnouncerManifest manifest = new AnnouncerManifest();
			manifest.VoiceLines = new List<VoiceLine>();
			manifest.Name = yamlfest.Name;
			manifest.GUID = yamlfest.GUID;
			manifest.Location = yamlfest.Location;
			manifest.Previews = Directory.GetFiles(yamlfest.VoiceLines, "example*.wav", SearchOption.AllDirectories).ToList();
			var files = Directory.GetFiles(yamlfest.VoiceLines, "*.wav", SearchOption.AllDirectories).ToList();
			foreach (var song in files) //iterate through and handle all lines found
			{
				VoiceLine vl = new VoiceLine();
				var songname = song;
				vl.ID = NameToID(songname);
				if (!validid) continue;
				vl.ClipPath = song;
				manifest.VoiceLines.Add(vl);
			}
			return manifest;
		}

		/*public static IEnumerator AddVoiceLinesToDB(ref TNH_VoiceDatabase db, VoiceLine line)
		{
			//i know there's a special place in hell for my naming scheme. dont care
			var sawww = GetAudioFromFile(line.StandardAudioClipPath);
			var cawww = GetAudioFromFile(line.CorruptedAudioClipPath);
			yield return sawww;
			yield return cawww;
			var sa = sawww.GetAudioClip();
			var ca = cawww.GetAudioClip();
			
			var vl = new TNH_VoiceDatabase.TNH_VoiceLine();
			vl.ID = line.ID;
			vl.Clip_Standard = sa;
			vl.Clip_Corrupted = ca;
			db.Lines.Add(vl);
		}*/

		private static bool validid = true;
		public static TNH_VoiceLineID NameToID(string song)
		{
			var songname = Path.GetFileName(song);
			validid = true;
			//i dont know how to switch this. cope!
			if (songname.Contains("game_intro")) return TNH_VoiceLineID.AI_UplinkSuccessfulTargetSystemDetectedTakeIt;
			if (songname.Contains("hold_intro")) return TNH_VoiceLineID.AI_InterfacingWithSystemNode;
			if (songname.Contains("hold_analyze")) return TNH_VoiceLineID.AI_AnalyzingSystem;
			if (songname.Contains("encryption_static")) return TNH_VoiceLineID.AI_EncryptionType_0;
			if (songname.Contains("encryption_hardened")) return TNH_VoiceLineID.AI_EncryptionType_1;
			if (songname.Contains("encryption_swarm")) return TNH_VoiceLineID.AI_EncryptionType_2;
			if (songname.Contains("encryption_recursive")) return TNH_VoiceLineID.AI_EncryptionType_3;
			if (songname.Contains("encryption_stealth")) return TNH_VoiceLineID.AI_EncryptionType_4;
			if (songname.Contains("encryption_agile")) return TNH_VoiceLineID.AI_EncryptionType_5;
			if (songname.Contains("encryption_regen")) return TNH_VoiceLineID.AI_EncryptionType_6;
			if (songname.Contains("encryption_polymorphic")) return TNH_VoiceLineID.AI_EncryptionType_7;
			if (songname.Contains("encryption_cascading")) return TNH_VoiceLineID.AI_EncryptionType_8;
			if (songname.Contains("encryption_orthagonal")) return TNH_VoiceLineID.AI_EncryptionType_9;
			if (songname.Contains("encryption_refractive")) return TNH_VoiceLineID.AI_EncryptionType_10;
			if (songname.Contains("encryption_unknown")) return TNH_VoiceLineID.AI_EncryptionType_Unknown;
			if (songname.Contains("hold_encryption_win")) return TNH_VoiceLineID.AI_Encryption_Neutralized;
			if (songname.Contains("hold_reminder_early")) return TNH_VoiceLineID.AI_Encryption_Reminder1;
			if (songname.Contains("hold_reminder_late")) return TNH_VoiceLineID.AI_Encryption_Reminder2;
			if (songname.Contains("hold_next_layer")) return TNH_VoiceLineID.AI_AdvancingToNextSystemLayer;
			if (songname.Contains("hold_win")) return TNH_VoiceLineID.AI_HoldSuccessfulDataExtracted;
			if (songname.Contains("hold_failure")) return TNH_VoiceLineID.AI_HoldFailedNodeConnectionTerminated;
			if (songname.Contains("hold_finish")) return TNH_VoiceLineID.AI_AdvanceToNextSystemNodeAndTakeIt;
			if (songname.Contains("loot_token1")) return TNH_VoiceLineID.AI_OverrideTokenFound_1;
			if (songname.Contains("loot_token2")) return TNH_VoiceLineID.AI_OverrideTokenFound_2;
			if (songname.Contains("loot_token3")) return TNH_VoiceLineID.AI_OverrideTokenFound_3;
			if (songname.Contains("loot_token4")) return TNH_VoiceLineID.AI_OverrideTokenFound_4;
			if (songname.Contains("loot_token5")) return TNH_VoiceLineID.AI_OverrideTokenFound_5;
			if (songname.Contains("loot_resource")) return TNH_VoiceLineID.AI_ResourceStorageFound;
			if (songname.Contains("loot_tool")) return TNH_VoiceLineID.AI_ToolConstructorFound;
			if (songname.Contains("loot_regen")) return TNH_VoiceLineID.AI_RegenerationModuleFound;
			if (songname.Contains("core_exposed")) return TNH_VoiceLineID.AI_SystemCoreExposedDestroyIt;
			if (songname.Contains("core_destroyed")) return TNH_VoiceLineID.AI_SystemCoreDestroyedWellDone;
			if (songname.Contains("game_lose_connection")) return TNH_VoiceLineID.AI_PlayerConnectionFailure;
			if (songname.Contains("game_end")) return TNH_VoiceLineID.AI_ReturningToInterface;
			if (songname.Contains("game_lose_operator")) return TNH_VoiceLineID.AI_TerminalOperatorFailureSystemConnectionLost;
			//I don't believe the Base lines can actually be changed. Can they? Please check that sometime
			if (songname.Contains("base_lockdown")) return TNH_VoiceLineID.BASE_IntrusionDetectedInitiatingLockdown;
			if (songname.Contains("base_response")) return TNH_VoiceLineID.BASE_ResponseTeamEnRoute;
			if (songname.Contains("base_dispatch")) return TNH_VoiceLineID.BASE_DispatchingAdditionalResponseTeam;
			if (songname.Contains("base_compromised")) return TNH_VoiceLineID.BASE_EncryptionCompromised;
			if (songname.Contains("base_fabricating")) return TNH_VoiceLineID.BASE_FabricatingDefenseSystems;
			if (songname.Contains("base_error")) return TNH_VoiceLineID.BASE_ErrorSystemFailure;

			validid = false;
			if(!songname.Contains("example")) PluginMain.DebugLog.LogError(song + " is not a valid announcer file!");
			return TNH_VoiceLineID.AI_Encryption_Neutralized;
		}
		
		//why does this field even exist lol
		public static AudioClip GetAudioFromFile(string path) => WavUtility.ToAudioClip(path);
	}
}