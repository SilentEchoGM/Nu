﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldAudio =

    type World with

        static member internal getAudioPlayer world =
            world.Subsystems.AudioPlayer

        static member internal withAudioPlayer updater world =
            updater (World.getAudioPlayer world)

        /// Enqueue an audio message to the world.
        static member enqueueAudioMessage (message : AudioMessage) world =
            world.Subsystems.AudioPlayer.EnqueueMessage message
            world

        /// Enqueue multiple audio messages to the world.
        static member enqueueAudioMessages (messages : AudioMessage seq) world =
            let audioPlayer = World.getAudioPlayer world
            for message in messages do audioPlayer.EnqueueMessage message
            world

        /// Get the currently playing song, if any.
        [<FunctionBinding>]
        static member getCurrentSongOpt world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.CurrentSongOpt
            
        /// Get the currently playing song's position or 0.0.
        [<FunctionBinding>]
        static member getCurrentSongPosition world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.CurrentSongPosition

        /// Get the master volume.
        [<FunctionBinding>]
        static member getMasterAudioVolume world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.MasterAudioVolume

        /// Get the master sound volume.
        [<FunctionBinding>]
        static member getMasterSoundVolume world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.MasterSoundVolume

        /// Get the master song volume.
        [<FunctionBinding>]
        static member getMasterSongVolume world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.MasterSongVolume

        /// Set the master volume.
        [<FunctionBinding>]
        static member setMasterAudioVolume volume world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.MasterAudioVolume <- volume
            world

        /// Set the master sound volume.
        [<FunctionBinding>]
        static member setMasterSoundVolume volume world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.MasterSoundVolume <- volume
            world

        /// Set the master song volume.
        [<FunctionBinding>]
        static member setMasterSongVolume volume world =
            let audioPlayer = World.getAudioPlayer world
            audioPlayer.MasterSongVolume <- volume
            world

        /// Send a message to the audio system to play a song.
        [<FunctionBinding>]
        static member playSong timeToFadeInSongMs timeToFadeOutSongMs volume start song world =
            let playSongMessage = PlaySongMessage { FadeInMs = timeToFadeInSongMs; FadeOutMs = timeToFadeOutSongMs; Volume = volume; Start = start; Song = song }
            World.enqueueAudioMessage playSongMessage world

        /// Send a message to the audio system to play a song.
        [<FunctionBinding "playSong6">]
        static member playSong7 timeToFadeInSongMs timeToFadeOutSongMs volume start songPackageName songAssetName world =
            let song = AssetTag.make<Song> songPackageName songAssetName
            World.playSong timeToFadeInSongMs timeToFadeOutSongMs volume start song world

        /// Send a message to the audio system to play a sound.
        [<FunctionBinding>]
        static member playSound volume sound world =
            let playSoundMessage = PlaySoundMessage { Sound = sound; Volume = volume }
            World.enqueueAudioMessage playSoundMessage world

        /// Send a message to the audio system to play a sound.
        [<FunctionBinding "playSound3">]
        static member playSound4 volume soundPackageName soundAssetName world =
            let sound = AssetTag.make<Sound> soundPackageName soundAssetName
            World.playSound volume sound world

        /// Send a message to the audio system to fade out any current song.
        [<FunctionBinding>]
        static member fadeOutSong timeToFadeOutSongMs world =
            let fadeOutSongMessage = FadeOutSongMessage timeToFadeOutSongMs
            World.enqueueAudioMessage fadeOutSongMessage world

        /// Send a message to the audio system to stop a song.
        [<FunctionBinding>]
        static member stopSong world =
            World.enqueueAudioMessage StopSongMessage world
            
        /// Load an audio asset package. Should be used to avoid loading assets at inconvenient times (such as in the
        /// middle of game play!)
        [<FunctionBinding>]
        static member loadAudioPackage packageName world =
            let loadAudioPackageMessage = LoadAudioPackageMessage packageName
            World.enqueueAudioMessage loadAudioPackageMessage world
            
        /// Unload an audio package should be unloaded since its assets will not be used again soon.
        [<FunctionBinding>]
        static member unloadAudioPackage packageName world =
            let unloadAudioPackageMessage = UnloadAudioPackageMessage packageName
            World.enqueueAudioMessage unloadAudioPackageMessage world

        /// Send a message to the audio player to reload its audio assets.
        [<FunctionBinding>]
        static member reloadAudioAssets world =
            let reloadAudioAssetsMessage = ReloadAudioAssetsMessage
            World.enqueueAudioMessage reloadAudioAssetsMessage world