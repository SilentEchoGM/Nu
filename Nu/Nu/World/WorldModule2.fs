﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.Collections.Generic
open System.IO
open System.Numerics
open System.Threading
open SDL2
open Prime
open Nu
open Nu.Declarative

[<AutoOpen; ModuleBinding>]
module WorldModule2 =

    (* Performance Timers *)
    let private TotalTimer = Diagnostics.Stopwatch ()
    let private InputTimer = Diagnostics.Stopwatch ()
    let private PhysicsTimer = Diagnostics.Stopwatch ()
    let private UpdateTimer = Diagnostics.Stopwatch ()
    let private UpdateGatherTimer = Diagnostics.Stopwatch ()
    let private UpdateGameTimer = Diagnostics.Stopwatch ()
    let private UpdateScreensTimer = Diagnostics.Stopwatch ()
    let private UpdateGroupsTimer = Diagnostics.Stopwatch ()
    let private UpdateEntitiesTimer = Diagnostics.Stopwatch ()
    let private PostUpdateTimer = Diagnostics.Stopwatch ()
    let private PostUpdateGatherTimer = Diagnostics.Stopwatch ()
    let private PostUpdateGameTimer = Diagnostics.Stopwatch ()
    let private PostUpdateScreensTimer = Diagnostics.Stopwatch ()
    let private PostUpdateGroupsTimer = Diagnostics.Stopwatch ()
#if !DISABLE_ENTITY_POST_UPDATE
    let private PostUpdateEntitiesTimer = Diagnostics.Stopwatch ()
#endif
    let private TaskletsTimer = Diagnostics.Stopwatch ()
    let private DestructionTimer = Diagnostics.Stopwatch ()
    let private PerProcessTimer = Diagnostics.Stopwatch ()
    let private PreProcessTimer = Diagnostics.Stopwatch ()
    let private PostProcessTimer = Diagnostics.Stopwatch ()
    let private RenderGatherTimer = Diagnostics.Stopwatch ()
    let private RenderEntitiesTimer = Diagnostics.Stopwatch ()
    let private RenderTimer = Diagnostics.Stopwatch ()
    let private AudioTimer = Diagnostics.Stopwatch ()

    (* Transition Values *)
    let private ScreenTransitionMouseLeftId = Gen.id
    let private ScreenTransitionMouseCenterId = Gen.id
    let private ScreenTransitionMouseRightId = Gen.id
    let private ScreenTransitionMouseX1Id = Gen.id
    let private ScreenTransitionMouseX2Id = Gen.id
    let private ScreenTransitionKeyboardKeyId = Gen.id

    (* Cached HashSets *)
    type OctelementComparer () =
        interface IEqualityComparer<Octelement<Entity>> with
            member this.GetHashCode element = element.HashCode
            member this.Equals (element, element2) = Address.equals element.Entry.EntityAddress element2.Entry.EntityAddress
    let private CachedHashSet3d = HashSet (OctelementComparer ())
    let private CachedHashSet2d = HashSet HashIdentity.Structural

    type World with

        static member internal makeQuadtree () =
            Quadtree.make Constants.Engine.QuadtreeGranularity Constants.Engine.QuadtreeDepth Constants.Engine.QuadtreeBounds

        static member internal makeOctree () =
            Octree.make Constants.Engine.OctreeGranularity Constants.Engine.OctreeDepth Constants.Engine.OctreeBounds

        static member internal rebuildQuadtree world =
            let omniEntities =
                match World.getOmniScreenOpt world with
                | Some screen -> World.getGroups screen world |> Seq.map (flip World.getEntitiesFlattened world) |> Seq.concat
                | None -> Seq.empty
            let selectedEntities =
                match World.getSelectedScreenOpt world with
                | Some screen -> World.getGroups screen world |> Seq.map (flip World.getEntitiesFlattened world) |> Seq.concat
                | None -> Seq.empty
            let entities = Seq.append omniEntities selectedEntities
            let quadtree = World.makeQuadtree ()
            for entity in entities do
                let bounds = entity.GetBounds world
                let presence = entity.GetPresence world
                if entity.GetIs2d world then
                    Quadtree.addElement presence bounds.Box2 entity quadtree
            quadtree

        static member internal rebuildOctree world =
            let omniEntities =
                match World.getOmniScreenOpt world with
                | Some screen -> World.getGroups screen world |> Seq.map (flip World.getEntitiesFlattened world) |> Seq.concat
                | None -> Seq.empty
            let selectedEntities =
                match World.getSelectedScreenOpt world with
                | Some screen -> World.getGroups screen world |> Seq.map (flip World.getEntitiesFlattened world) |> Seq.concat
                | None -> Seq.empty
            let entities = Seq.append omniEntities selectedEntities
            let octree = World.makeOctree ()
            for entity in entities do
                let bounds = entity.GetBounds world
                let static_ = entity.GetStatic world
                let light = entity.GetLight world
                let presence = entity.GetPresence world
                if not (entity.GetIs2d world) then
                    let element = Octelement.make static_ light presence entity
                    Octree.addElement bounds element octree
            octree

        /// Resolve a relation to an address in the current script context.
        static member resolve<'a> (relation : 'a Relation) world =
            let scriptContext = World.getScriptContext world
            let address = Relation.resolve scriptContext.SimulantAddress relation
            address
    
        /// Relate an address to the current script context.
        static member relate<'a> (address : 'a Address) world =
            let scriptContext = World.getScriptContext world
            let address = Relation.relate scriptContext.SimulantAddress address
            address

        /// Resolve a relation to an address in the current script context in script.
        [<FunctionBinding "resolve">]
        static member internal resolveViaScript (relation : obj Relation) world =
            World.resolve relation world

        /// Relate an address to the current script context in script.
        [<FunctionBinding "relate">]
        static member internal relateViaScript (address : obj Address) world =
            World.relate address world

        /// Select the given screen without transitioning, even if another transition is taking place.
        static member internal selectScreenOpt transitionStateAndScreenOpt world =
            let world =
                match World.getSelectedScreenOpt world with
                | Some selectedScreen ->
                    let eventTrace = EventTrace.debug "World" "selectScreen" "Deselecting" EventTrace.empty
                    World.publish () (Events.Deselecting --> selectedScreen) eventTrace selectedScreen world
                | None -> world
            match transitionStateAndScreenOpt with
            | Some (transitionState, screen) ->
                let world = World.setScreenTransitionStatePlus transitionState screen world
                let world = World.setSelectedScreen screen world
                let eventTrace = EventTrace.debug "World" "selectScreen" "Select" EventTrace.empty
                World.publish () (Events.Select --> screen) eventTrace screen world
            | None ->
                World.setSelectedScreenOpt None world

        /// Select the given screen without transitioning, even if another transition is taking place.
        [<FunctionBinding>]
        static member selectScreen transitionState screen world =
            World.selectScreenOpt (Some (transitionState, screen)) world

        /// Try to check that the selected screen is idling; that is, neither transitioning in or
        /// out via another screen.
        [<FunctionBinding>]
        static member tryGetIsSelectedScreenIdling world =
            match World.getSelectedScreenOpt world with
            | Some selectedScreen -> Some (selectedScreen.IsIdling world)
            | None -> None

        /// Try to check that the selected screen is transitioning.
        [<FunctionBinding>]
        static member tryGetIsSelectedScreenTransitioning world =
            Option.map not (World.tryGetIsSelectedScreenIdling world)

        /// Check that the selected screen is idling; that is, neither transitioning in or
        /// out via another screen (failing with an exception if no screen is selected).
        [<FunctionBinding>]
        static member isSelectedScreenIdling world =
            match World.tryGetIsSelectedScreenIdling world with
            | Some answer -> answer
            | None -> failwith "Cannot query state of non-existent selected screen."

        /// Check that the selected screen is transitioning (failing with an exception if no screen
        /// is selected).
        [<FunctionBinding>]
        static member isSelectedScreenTransitioning world =
            not (World.isSelectedScreenIdling world)

        /// Set screen transition state, enabling or disabling input events respectively.
        static member private setScreenTransitionStatePlus state (screen : Screen) world =
            let world = screen.SetTransitionState state world
            match state with
            | IdlingState ->
                let world = World.unsubscribe ScreenTransitionMouseLeftId world
                let world = World.unsubscribe ScreenTransitionMouseCenterId world
                let world = World.unsubscribe ScreenTransitionMouseRightId world
                let world = World.unsubscribe ScreenTransitionMouseX1Id world
                let world = World.unsubscribe ScreenTransitionMouseX2Id world
                let world = World.unsubscribe ScreenTransitionKeyboardKeyId world
                world
            | IncomingState
            | OutgoingState ->
                let world = World.subscribePlus ScreenTransitionMouseLeftId World.handleAsSwallow (stoa<MouseButtonData> "Mouse/Left/@/Event") Simulants.Game world |> snd
                let world = World.subscribePlus ScreenTransitionMouseCenterId World.handleAsSwallow (stoa<MouseButtonData> "Mouse/Center/@/Event") Simulants.Game world |> snd
                let world = World.subscribePlus ScreenTransitionMouseRightId World.handleAsSwallow (stoa<MouseButtonData> "Mouse/Right/@/Event") Simulants.Game world |> snd
                let world = World.subscribePlus ScreenTransitionMouseX1Id World.handleAsSwallow (stoa<MouseButtonData> "Mouse/X1/@/Event") Simulants.Game world |> snd
                let world = World.subscribePlus ScreenTransitionMouseX2Id World.handleAsSwallow (stoa<MouseButtonData> "Mouse/X2/@/Event") Simulants.Game world |> snd
                let world = World.subscribePlus ScreenTransitionKeyboardKeyId World.handleAsSwallow (stoa<KeyboardKeyData> "KeyboardKey/@/Event") Simulants.Game world |> snd
                world

        static member private updateScreenTransition3 transitionType (selectedScreen : Screen) world =
            // NOTE: we do not immediately transition when transition time is zero because we only want screen
            // transitions to happen outside the update loop!
            // NOTE: transitions always take one additional frame because it needs to render frame 0 and frame MAX + 1 for
            // full opacity if fading and and an extra frame for the render messages to actually get processed.
            let transition =
                match transitionType with
                | Incoming -> selectedScreen.GetIncoming world
                | Outgoing -> selectedScreen.GetOutgoing world
            let transitionUpdates = selectedScreen.GetTransitionUpdates world
            if transitionUpdates = transition.TransitionLifeTime + 1L then
                (true, selectedScreen.SetTransitionUpdates 0L world)
            elif transitionUpdates > transition.TransitionLifeTime then
                Log.debug ("TransitionLifeTime for screen '" + scstring selectedScreen.ScreenAddress + "' must be a consistent multiple of UpdateRate.")
                (true, selectedScreen.SetTransitionUpdates 0L world)
            else (false, selectedScreen.SetTransitionUpdates (transitionUpdates + World.getUpdateRate world) world)

        static member private updateScreenIdling3 slide (selectedScreen : Screen) world =
            // NOTE: we do not immediately transition when transition time is zero because we only want screen
            // transitions to happen outside the update loop!
            // NOTE: transitions always take one additional frame because it needs to render frame 0 and frame MAX + 1 for
            // full opacity if fading and and an extra frame for the render messages to actually get processed.
            let transitionUpdates = selectedScreen.GetTransitionUpdates world
            if transitionUpdates = slide.IdlingTime + 1L then
                (true, selectedScreen.SetTransitionUpdates 0L world)
            elif transitionUpdates > slide.IdlingTime then
                Log.debug ("IdlingTimeOpt for screen '" + scstring selectedScreen.ScreenAddress + "' must be Some consistent multiple of UpdateRate or None.")
                (true, selectedScreen.SetTransitionUpdates 0L world)
            else (false, selectedScreen.SetTransitionUpdates (transitionUpdates + World.getUpdateRate world) world)

        static member private updateScreenIncoming (selectedScreen : Screen) world =
            match World.getLiveness world with
            | Live ->
                let world =
                    if selectedScreen.GetTransitionUpdates world = 0L then
                        let world =
                            match (selectedScreen.GetIncoming world).SongOpt with
                            | Some playSong ->
                                match World.getCurrentSongOpt world with
                                | Some song when assetEq song.Song playSong.Song -> world // do nothing when song is the same
                                | _ -> World.playSong playSong.FadeInMs playSong.FadeOutMs playSong.Volume 0.0 playSong.Song world // play song when song is different
                            | None -> world
                        let eventTrace = EventTrace.debug "World" "updateScreenIncoming" "IncomingStart" EventTrace.empty
                        World.publish () (Events.IncomingStart --> selectedScreen) eventTrace selectedScreen world
                    else world
                match World.getLiveness world with
                | Live ->
                    match World.updateScreenTransition3 Incoming selectedScreen world with
                    | (true, world) ->
                        let eventTrace = EventTrace.debug "World" "updateScreenIncoming" "IncomingFinish" EventTrace.empty
                        let world = World.setScreenTransitionStatePlus IdlingState selectedScreen world
                        World.publish () (Events.IncomingFinish --> selectedScreen) eventTrace selectedScreen world
                    | (false, world) -> world
                | Dead -> world
            | Dead -> world

        static member private updateScreenIdling (selectedScreen : Screen) world =
            match World.getLiveness world with
            | Live ->
                match selectedScreen.GetSlideOpt world with
                | Some slide ->
                    match World.updateScreenIdling3 slide selectedScreen world with
                    | (true, world) -> World.setScreenTransitionStatePlus OutgoingState selectedScreen world
                    | (false, world) -> world
                | None ->
                    match Simulants.Game.GetDesiredScreen world with
                    | Desire desiredScreen ->
                        if desiredScreen <> selectedScreen then
                            if World.getStandAlone world || World.getAdvancing world then
                                let world = selectedScreen.SetTransitionUpdates 0L world
                                World.setScreenTransitionStatePlus OutgoingState selectedScreen world
                            else World.setSelectedScreenOpt (Some desiredScreen) world // quick cut such as when halted in editor
                        else world
                    | DesireNone -> World.setScreenTransitionStatePlus OutgoingState selectedScreen world
                    | DesireIgnore -> world
            | Dead -> world

        static member private updateScreenOutgoing (selectedScreen : Screen) world =
            let world =
                if selectedScreen.GetTransitionUpdates world = 0L then
                    let incoming = selectedScreen.GetIncoming world
                    let outgoing = selectedScreen.GetOutgoing world
                    let world =
                        match outgoing.SongOpt with
                        | Some playSong ->
                            let destinationOpt =
                                match selectedScreen.GetSlideOpt world with
                                | Some slide -> Some slide.Destination
                                | None ->
                                    match World.getScreenTransitionDestinationOpt world with
                                    | Some destination -> Some destination
                                    | None ->
                                        match Simulants.Game.GetDesiredScreen world with
                                        | Desire destination -> Some destination
                                        | DesireNone -> None
                                        | DesireIgnore -> None
                            match destinationOpt with
                            | Some destination ->
                                match (incoming.SongOpt, (destination.GetIncoming world).SongOpt) with
                                | (Some song, Some song2) when assetEq song.Song song2.Song -> world // do nothing when song is the same
                                | (None, None) -> world // do nothing when neither plays a song (allowing manual control)
                                | (_, _) -> World.fadeOutSong playSong.FadeOutMs world // fade out when song is different
                            | None ->
                                match incoming.SongOpt with
                                | Some _ -> World.fadeOutSong playSong.FadeOutMs world
                                | None -> world
                        | None -> world
                    let eventTrace = EventTrace.debug "World" "updateScreenTransition" "OutgoingStart" EventTrace.empty
                    World.publish () (Events.OutgoingStart --> selectedScreen) eventTrace selectedScreen world
                else world
            match World.getLiveness world with
            | Live ->
                match World.updateScreenTransition3 Outgoing selectedScreen world with
                | (true, world) ->
                    let world = World.setScreenTransitionStatePlus IdlingState selectedScreen world
                    let world =
                        match World.getLiveness world with
                        | Live ->
                            let eventTrace = EventTrace.debug "World" "updateScreenOutgoing" "OutgoingFinish" EventTrace.empty
                            World.publish () (Events.OutgoingFinish --> selectedScreen) eventTrace selectedScreen world
                        | Dead -> world
                    match World.getLiveness world with
                    | Live ->
                        let destinationOpt =
                            match selectedScreen.GetSlideOpt world with
                            | Some slide -> Some slide.Destination
                            | None ->
                                match World.getScreenTransitionDestinationOpt world with
                                | Some destination -> Some destination
                                | None ->
                                    match Simulants.Game.GetDesiredScreen world with
                                    | Desire destination -> Some destination
                                    | DesireNone -> None
                                    | DesireIgnore -> None
                        match destinationOpt with
                        | Some destination ->
                            if destination <> selectedScreen
                            then World.selectScreen IncomingState destination world
                            else world
                        | None ->
                            let world = World.selectScreenOpt None world
                            match Simulants.Game.GetDesiredScreen world with // handle the possibility that screen deselect event changed destination
                            | Desire destination -> World.selectScreen IncomingState destination world
                            | DesireNone -> world
                            | DesireIgnore -> world
                    | Dead -> world
                | (false, world) -> world
            | Dead -> world

        static member private updateScreenTransition world =
            match World.getSelectedScreenOpt world with
            | Some selectedScreen ->
                match selectedScreen.GetTransitionState world with
                | IncomingState -> World.updateScreenIncoming selectedScreen world
                | IdlingState -> World.updateScreenIdling selectedScreen world
                | OutgoingState -> World.updateScreenOutgoing selectedScreen world
            | None ->
                match World.getDesiredScreen world with
                | Desire desiredScreen -> World.transitionScreen desiredScreen world
                | DesireNone -> world
                | DesireIgnore -> world

        /// Try to transition to the given screen if no other transition is in progress.
        [<FunctionBinding>]
        static member tryTransitionScreen destination world =
            match World.getSelectedScreenOpt world with
            | Some selectedScreen ->
                if  selectedScreen <> destination &&
                    not (World.isSelectedScreenTransitioning world) then
                    let world = World.setScreenTransitionDestinationOpt (Some destination) world |> snd'
                    let world = World.setScreenTransitionStatePlus OutgoingState selectedScreen world
                    (true, world)
                else (false, world)
            | None ->
                let world = World.setScreenTransitionStatePlus IncomingState destination world
                let world = World.setSelectedScreen destination world
                (true, world)

        /// Transition to the given screen.
        [<FunctionBinding>]
        static member transitionScreen destination world =
            World.tryTransitionScreen destination world |> snd

        /// Set the slide aspects of a screen.
        [<FunctionBinding>]
        static member setScreenSlide (slideDescriptor : SlideDescriptor) destination (screen : Screen) world =

            // destroy existing slide group if any
            let slideGroup = screen / "SlideGroup"
            let slideSprite = slideGroup / "SlideSprite"
            let world = World.destroyGroupImmediate slideGroup world

            // create slide group
            let cameraEyeSize = World.getEyeSize2d world
            let world = screen.SetSlideOpt (Some { IdlingTime = slideDescriptor.IdlingTime; Destination = destination }) world
            let world = World.createGroup<GroupDispatcher> (Some slideGroup.Name) screen world |> snd
            let world = World.setGroupProtected true slideGroup world |> snd'
            let world = slideGroup.SetPersistent false world

            // create slide sprite
            let world = World.createEntity<StaticSpriteDispatcher> DefaultOverlay (Some slideSprite.Surnames) slideGroup world |> snd
            let world = World.setEntityProtected true slideSprite world |> snd'
            let world = slideSprite.SetPersistent false world
            let world = slideSprite.SetSize cameraEyeSize.V3 world
            let world = slideSprite.SetPosition (-cameraEyeSize.V3 * 0.5f) world
            let world = slideSprite.SetAbsolute true world
            let world =
                match slideDescriptor.SlideImageOpt with
                | Some slideImage ->
                    let world = slideSprite.SetStaticImage slideImage world
                    let world = slideSprite.SetVisible true world
                    world
                | None ->
                    let world = slideSprite.SetStaticImage Assets.Default.Image5 world
                    let world = slideSprite.SetVisible false world
                    world
            world

        /// Create a dissolve screen whose content is loaded from the given group file.
        [<FunctionBinding>]
        static member createDissolveScreenFromGroupFile6 dispatcherName nameOpt dissolveDescriptor songOpt groupFilePath world =
            let (dissolveScreen, world) = World.createDissolveScreen5 dispatcherName nameOpt dissolveDescriptor songOpt world
            let world = World.readGroupFromFile groupFilePath None dissolveScreen world |> snd
            (dissolveScreen, world)

        /// Create a dissolve screen whose content is loaded from the given group file.
        [<FunctionBinding>]
        static member createDissolveScreenFromGroupFile<'d when 'd :> ScreenDispatcher> nameOpt dissolveDescriptor songOpt groupFilePath world =
            World.createDissolveScreenFromGroupFile6 typeof<'d>.Name nameOpt dissolveDescriptor groupFilePath songOpt world

        /// Create a slide screen that transitions to the given destination upon completion.
        [<FunctionBinding>]
        static member createSlideScreen6 dispatcherName nameOpt slideDescriptor destination world =
            let (slideScreen, world) = World.createDissolveScreen5 dispatcherName nameOpt slideDescriptor.DissolveDescriptor None world
            let world = World.setScreenSlide slideDescriptor destination slideScreen world
            (slideScreen, world)

        /// Create a slide screen that transitions to the given destination upon completion.
        [<FunctionBinding>]
        static member createSlideScreen<'d when 'd :> ScreenDispatcher> nameOpt slideDescriptor destination world =
            World.createSlideScreen6 typeof<'d>.Name nameOpt slideDescriptor destination world

        static member internal makeIntrinsicOverlays facets entityDispatchers =
            let requiresFacetNames = fun sourceType -> sourceType = typeof<EntityDispatcher>
            let facets = facets |> Map.toValueList |> List.map box
            let entityDispatchers = entityDispatchers |> Map.toValueList |> List.map box
            let sources = facets @ entityDispatchers
            let sourceTypes = List.map (fun source -> source.GetType ()) sources
            Overlay.makeIntrinsicOverlays requiresFacetNames sourceTypes

        /// Try to reload the overlayer currently in use by the world.
        static member tryReloadOverlays inputDirectory outputDirectory world =
            
            // attempt to reload overlay file
            let inputOverlayerFilePath = inputDirectory + "/" + Assets.Global.OverlayerFilePath
            let outputOverlayerFilePath = outputDirectory + "/" + Assets.Global.OverlayerFilePath
            try File.Copy (inputOverlayerFilePath, outputOverlayerFilePath, true)

                // cache old overlayer and make new one
                let oldOverlayer = World.getOverlayer world
                let entityDispatchers = World.getEntityDispatchers world
                let facets = World.getFacets world
                let intrinsicOverlays = World.makeIntrinsicOverlays facets entityDispatchers
                match Overlayer.tryMakeFromFile intrinsicOverlays outputOverlayerFilePath with
                | Right overlayer ->

                    // update overlayer and overlay router
                    let overlays = Overlayer.getIntrinsicOverlays overlayer @ Overlayer.getExtrinsicOverlays overlayer
                    let overlayRoutes =
                        overlays |>
                        List.map (fun overlay -> overlay.OverlaidTypeNames |> List.map (fun typeName -> (typeName, overlay.OverlayName))) |>
                        List.concat
                    let overlayRouter = OverlayRouter.make overlayRoutes
                    let world = World.setOverlayer overlayer world
                    let world = World.setOverlayRouter overlayRouter world

                    // apply overlays to all entities
                    let entities = World.getEntities1 world
                    let world = Seq.fold (World.applyEntityOverlay oldOverlayer overlayer) world entities
                    (Right overlayer, world)

                // propagate errors
                | Left error -> (Left error, world)
            with exn -> (Left (scstring exn), World.choose world)

        /// Try to reload the prelude currently in use by the world.
        static member tryReloadPrelude inputDirectory outputDirectory world =
            let inputPreludeFilePath = inputDirectory + "/" + Assets.Global.PreludeFilePath
            let outputPreludeFilePath = outputDirectory + "/" + Assets.Global.PreludeFilePath
            try File.Copy (inputPreludeFilePath, outputPreludeFilePath, true)
                match World.tryEvalPrelude world with
                | Right struct (preludeStr, world) -> (Right preludeStr, world)
                | Left struct (error, world) -> (Left error, world)
            with exn -> (Left (scstring exn), World.choose world)

        /// Send a message to the subsystems to reload their existing assets.
        [<FunctionBinding>]
        static member reloadExistingAssets world =
            let world = World.reloadRenderAssets2d world
            let world = World.reloadRenderAssets3d world
            let world = World.reloadAudioAssets world
            let world = World.reloadSymbols world
            Metadata.regenerateMetadata ()
            world

        /// Attempt to reload the asset graph.
        /// Currently does not support reloading of song assets, and possibly others that are
        /// locked by the engine's subsystems.
        static member tryReloadAssetGraph inputDirectory outputDirectory refinementDirectory world =

            // attempt to reload asset graph file
            try File.Copy
                    (inputDirectory + "/" + Assets.Global.AssetGraphFilePath,
                     outputDirectory + "/" + Assets.Global.AssetGraphFilePath,
                     true)

                // attempt to load asset graph
                match AssetGraph.tryMakeFromFile (outputDirectory + "/" + Assets.Global.AssetGraphFilePath) with
                | Right assetGraph ->

                    // build assets reload asset metadata
                    AssetGraph.buildAssets inputDirectory outputDirectory refinementDirectory false assetGraph
                    Metadata.generateMetadata (World.getImperative world) assetGraph
                    let world = World.reloadExistingAssets world
                    let world = World.publish () Events.AssetsReload (EventTrace.debug "World" "publishAssetsReload" "" EventTrace.empty) Simulants.Game world
                    (Right assetGraph, world)

                // propagate errors
                | Left error -> (Left error, world)
            with exn -> (Left (scstring exn), World.choose world)

        /// Reload asset graph, build assets, then reload built assets.
        [<FunctionBinding>]
        static member tryReloadAssets world =
            let targetDir = AppDomain.CurrentDomain.BaseDirectory
            let assetSourceDir = Path.Simplify (targetDir + "../..")
            match World.tryReloadAssetGraph assetSourceDir targetDir Constants.Engine.RefinementDir world with
            | (Right _, world) -> (true, world)
            | (Left _, world) -> (false, world)

        /// Clear all messages in all subsystems.
        static member clearMessages world =
             let world = World.updatePhysicsEngine2d (fun physicsEngine -> physicsEngine.ClearMessages ()) world
             World.withRendererProcess (fun rendererProcess -> rendererProcess.ClearMessages ()) world
             World.withAudioPlayer (fun audioPlayer -> audioPlayer.ClearMessages ()) world
             world

        /// Shelve the a world for background storage.
        static member shelve world =
            world // nothing to do

        /// Unshelve the state of a world.
        static member unshelve world =

            // clear existing 2d physics messages
            let world = World.updatePhysicsEngine2d (fun physicsEngine -> physicsEngine.ClearMessages ()) world

            // rebuild 2d physics state
            let world = World.enqueuePhysicsMessage2d RebuildPhysicsHackMessage world

            // propagate current physics state
            let entities = World.getEntities1 world
            let world = Seq.fold (fun world (entity : Entity) -> entity.PropagatePhysics world) world entities
            world

        static member private processTasklet simulant tasklet (taskletsNotRun : OMap<Simulant, World Tasklet UList>) world =
            let time = World.getUpdateTime world
            if time = tasklet.ScheduledTime then
                let world = tasklet.ScheduledOp world
                (taskletsNotRun, world)
            elif time > tasklet.ScheduledTime then
                Log.debug ("Tasklet leak found for time '" + scstring time + "'.")
                (taskletsNotRun, world)
            else
                let taskletsNotRun =
                    match taskletsNotRun.TryGetValue simulant with
                    | (true, taskletList) -> OMap.add simulant (UList.add tasklet taskletList) taskletsNotRun
                    | (false, _) -> OMap.add simulant (UList.singleton (OMap.getConfig taskletsNotRun) tasklet) taskletsNotRun
                (taskletsNotRun, world)

        static member private processTasklets world =
            let tasklets = World.getTasklets world
            let world = World.clearTasklets world
            let (taskletsNotRun, world) =
                OMap.fold (fun (taskletsNotRun, world) simulant taskletList ->
                    UList.fold (fun (taskletsNotRun, world) tasklet ->
                        if World.getExists simulant world
                        then World.processTasklet simulant tasklet taskletsNotRun world
                        else (taskletsNotRun, world))
                        (taskletsNotRun, world)
                        taskletList)
                    (OMap.makeEmpty HashIdentity.Structural (OMap.getConfig tasklets), world)
                    tasklets
            let taskletsNotRun = OMap.filter (fun simulant _ -> World.getExists simulant world) taskletsNotRun
            World.restoreTasklets taskletsNotRun world

        static member private destroySimulants world =
            let destructionListRev = World.getDestructionListRev world
            let world = List.foldBack (fun simulant world -> World.destroyImmediate simulant world) destructionListRev world
            if List.notEmpty (World.getDestructionListRev world) then World.destroySimulants world else world

        /// Process an input event from SDL and ultimately publish any related game events.
        static member private processInput2 (evt : SDL.SDL_Event) world =
            let world =
                match evt.``type`` with
                | SDL.SDL_EventType.SDL_QUIT ->
                    World.exit world
                | SDL.SDL_EventType.SDL_MOUSEMOTION ->
                    let mousePosition = v2 (single evt.button.x) (single evt.button.y)
                    let world =
                        if World.isMouseButtonDown MouseLeft world then
                            let eventTrace = EventTrace.debug "World" "processInput" "MouseDrag" EventTrace.empty
                            World.publishPlus { MouseMoveData.Position = mousePosition } Events.MouseDrag eventTrace Simulants.Game true true world
                        else world
                    let eventTrace = EventTrace.debug "World" "processInput" "MouseMove" EventTrace.empty
                    World.publishPlus { MouseMoveData.Position = mousePosition } Events.MouseMove eventTrace Simulants.Game true true world
                | SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN ->
                    let mousePosition = World.getMousePosition world
                    let mouseButton = World.toNuMouseButton (uint32 evt.button.button)
                    let mouseButtonDownEvent = stoa<MouseButtonData> ("Mouse/" + MouseButton.toEventName mouseButton + "/Down/Event")
                    let mouseButtonChangeEvent = stoa<MouseButtonData> ("Mouse/" + MouseButton.toEventName mouseButton + "/Change/Event")
                    let eventData = { Position = mousePosition; Button = mouseButton; Down = true }
                    let eventTrace = EventTrace.debug "World" "processInput" "MouseButtonDown" EventTrace.empty
                    let world = World.publishPlus eventData mouseButtonDownEvent eventTrace Simulants.Game true true world
                    let eventTrace = EventTrace.debug "World" "processInput" "MouseButtonChange" EventTrace.empty
                    World.publishPlus eventData mouseButtonChangeEvent eventTrace Simulants.Game true true world
                | SDL.SDL_EventType.SDL_MOUSEBUTTONUP ->
                    let mousePosition = World.getMousePosition world
                    let mouseButton = World.toNuMouseButton (uint32 evt.button.button)
                    let mouseButtonUpEvent = stoa<MouseButtonData> ("Mouse/" + MouseButton.toEventName mouseButton + "/Up/Event")
                    let mouseButtonChangeEvent = stoa<MouseButtonData> ("Mouse/" + MouseButton.toEventName mouseButton + "/Change/Event")
                    let eventData = { Position = mousePosition; Button = mouseButton; Down = false }
                    let eventTrace = EventTrace.debug "World" "processInput" "MouseButtonUp" EventTrace.empty
                    let world = World.publishPlus eventData mouseButtonUpEvent eventTrace Simulants.Game true true world
                    let eventTrace = EventTrace.debug "World" "processInput" "MouseButtonChange" EventTrace.empty
                    World.publishPlus eventData mouseButtonChangeEvent eventTrace Simulants.Game true true world
                | SDL.SDL_EventType.SDL_KEYDOWN ->
                    let keyboard = evt.key
                    let key = keyboard.keysym
                    let eventData = { KeyboardKey = key.scancode |> int |> enum<KeyboardKey>; Repeated = keyboard.repeat <> byte 0; Down = true }
                    let eventTrace = EventTrace.debug "World" "processInput" "KeyboardKeyDown" EventTrace.empty
                    let world = World.publishPlus eventData Events.KeyboardKeyDown eventTrace Simulants.Game true true world
                    let eventTrace = EventTrace.debug "World" "processInput" "KeyboardKeyChange" EventTrace.empty
                    World.publishPlus eventData Events.KeyboardKeyChange eventTrace Simulants.Game true true world
                | SDL.SDL_EventType.SDL_KEYUP ->
                    let keyboard = evt.key
                    let key = keyboard.keysym
                    let eventData = { KeyboardKey = key.scancode |> int |> enum<KeyboardKey>; Repeated = keyboard.repeat <> byte 0; Down = false }
                    let eventTrace = EventTrace.debug "World" "processInput" "KeyboardKeyUp" EventTrace.empty
                    let world = World.publishPlus eventData Events.KeyboardKeyUp eventTrace Simulants.Game true true world
                    let eventTrace = EventTrace.debug "World" "processInput" "KeyboardKeyChange" EventTrace.empty
                    World.publishPlus eventData Events.KeyboardKeyChange eventTrace Simulants.Game true true world
                | SDL.SDL_EventType.SDL_JOYHATMOTION ->
                    let index = evt.jhat.which
                    let direction = evt.jhat.hatValue
                    let eventData = { GamepadDirection = GamepadState.toNuDirection direction }
                    let eventTrace = EventTrace.debug "World" "processInput" "GamepadDirectionChange" EventTrace.empty
                    World.publishPlus eventData (Events.GamepadDirectionChange index) eventTrace Simulants.Game true true world
                | SDL.SDL_EventType.SDL_JOYBUTTONDOWN ->
                    let index = evt.jbutton.which
                    let button = int evt.jbutton.button
                    if GamepadState.isSdlButtonSupported button then
                        let eventData = { GamepadButton = GamepadState.toNuButton button; Down = true }
                        let eventTrace = EventTrace.debug "World" "processInput" "GamepadButtonDown" EventTrace.empty
                        let world = World.publishPlus eventData (Events.GamepadButtonDown index) eventTrace Simulants.Game true true world
                        let eventTrace = EventTrace.debug "World" "processInput" "GamepadButtonChange" EventTrace.empty
                        World.publishPlus eventData (Events.GamepadButtonChange index) eventTrace Simulants.Game true true world
                    else world
                | SDL.SDL_EventType.SDL_JOYBUTTONUP ->
                    let index = evt.jbutton.which
                    let button = int evt.jbutton.button
                    if GamepadState.isSdlButtonSupported button then
                        let eventData = { GamepadButton = GamepadState.toNuButton button; Down = true }
                        let eventTrace = EventTrace.debug "World" "processInput" "GamepadButtonUp" EventTrace.empty
                        let world = World.publishPlus eventData (Events.GamepadButtonUp index) eventTrace Simulants.Game true true world
                        let eventTrace = EventTrace.debug "World" "processInput" "GamepadButtonChange" EventTrace.empty
                        World.publishPlus eventData (Events.GamepadButtonChange index) eventTrace Simulants.Game true true world
                    else world
                | _ -> world
            (World.getLiveness world, world)

        static member private processIntegrationMessage integrationMessage world =
            match World.getLiveness world with
            | Live ->
                match integrationMessage with
                | BodyCollisionMessage bodyCollisionMessage ->
                    let entity = bodyCollisionMessage.BodyShapeSource.Simulant :?> Entity
                    if entity.Exists world && entity.IsSelected world then
                        let collisionData =
                            { BodyCollider = BodyShapeSource.fromInternal bodyCollisionMessage.BodyShapeSource
                              BodyCollidee = BodyShapeSource.fromInternal bodyCollisionMessage.BodyShapeSource2
                              Normal = bodyCollisionMessage.Normal
                              Speed = bodyCollisionMessage.Speed }
                        let collisionAddress = Events.BodyCollision --> entity.EntityAddress
                        let eventTrace = EventTrace.debug "World" "processIntegrationMessage" "" EventTrace.empty
                        World.publish collisionData collisionAddress eventTrace Simulants.Game world
                    else world
                | BodySeparationMessage bodySeparationMessage ->
                    let entity = bodySeparationMessage.BodyShapeSource.Simulant :?> Entity
                    if entity.Exists world && entity.IsSelected world then
                        let explicit =
                            { BodySeparator = BodyShapeSource.fromInternal bodySeparationMessage.BodyShapeSource
                              BodySeparatee = BodyShapeSource.fromInternal bodySeparationMessage.BodyShapeSource2 }
                        let separationAddress = Events.BodySeparation --> entity.EntityAddress
                        let eventTrace = EventTrace.debug "World" "processIntegrationMessage" "" EventTrace.empty
                        World.publish (BodySeparationExplicit explicit) separationAddress eventTrace Simulants.Game world
                    else world
                | BodyTransformMessage bodyTransformMessage ->
                    let bodySource = bodyTransformMessage.BodySource
                    let entity = bodySource.Simulant :?> Entity
                    if entity.Exists world && entity.IsSelected world then
                        let entityState = World.getEntityState entity world // OPTIMIZATION: invoke entity state directly.
                        let centerOffset = if entityState.Centered then v3Zero else entityState.Size * v3UncenteredOffset
                        let position = bodyTransformMessage.Position - centerOffset
                        let rotation = bodyTransformMessage.Rotation
                        let linearVelocity = bodyTransformMessage.LinearVelocity
                        let angularVelocity = bodyTransformMessage.AngularVelocity
                        let world =
                            if bodySource.BodyId = 0UL
                            then entity.ApplyPhysics position rotation linearVelocity angularVelocity world
                            else world
                        // TODO: P1: don't publish if PublishBodyTransformEvent is false.
                        let transformData =
                            { BodyPosition = position
                              BodyRotation = rotation
                              BodyLinearVelocity = linearVelocity
                              BodyAngularVelocity = angularVelocity }
                        let transformAddress = Events.BodyTransform --> entity.EntityAddress
                        let eventTrace = EventTrace.debug "World" "processIntegrationMessage" "" EventTrace.empty
                        World.publish transformData transformAddress eventTrace Simulants.Game world
                    else world
            | Dead -> world

        static member private getEntities2dBy getElementsFromQuadtree world =
            let quadtree = World.getQuadtree world
            let (quadtree, quadtreeCache) = MutantCache.getMutant (fun () -> World.rebuildQuadtree world) quadtree
            let world = World.setQuadtree quadtreeCache world
            let entities : Entity seq = getElementsFromQuadtree quadtree
            (entities, world)

        /// Get all uncullable (non-cullable) 2d entities.
        static member getEntitiesOmnipresent2d set world =
            World.getEntities2dBy (Quadtree.getElementsOmnipresent set) world

        /// Get all 2d entities in the given bounds, including all uncullable entities.
        static member getEntitiesInBounds2d bounds set world =
            World.getEntities2dBy (Quadtree.getElementsInBounds bounds set) world

        /// Get all 2d entities at the given point, including all uncullable entities.
        static member getEntitiesAtPoint2d point set world =
            World.getEntities2dBy (Quadtree.getElementsAtPoint point set) world

        /// Get all 2d entities needing to update for the current 2d play zone, including all uncullable entities.
        static member getEntitiesInPlay2d set world =
            let playBounds = World.getPlayBounds2d world
            World.getEntities2dBy (Quadtree.getElementsInBounds playBounds set) world

        /// Get all 2d entities in the current 2d view, including all uncullable entities.
        static member getEntitiesInView2d set world =
            let viewBounds = World.getViewBounds2d world
            World.getEntities2dBy (Quadtree.getElementsInBounds viewBounds set) world

        static member private getEntities3dBy getElementsFromOctree world =
            let octree = World.getOctree world
            let (octree, octreeCache) = MutantCache.getMutant (fun () -> World.rebuildOctree world) octree
            let world = World.setOctree octreeCache world
            let elements = getElementsFromOctree octree
            let entities = Seq.map (fun element -> element.Entry) elements
            (entities, world)

        /// Get all uncullable 3d entities.
        static member getEntitiesOmnipresent3d set world =
            World.getEntities3dBy (Octree.getElementsOmnipresent set) world

        /// Get all 3d entities in the given bounds, including all uncullable entities.
        static member getEntitiesInBounds3d bounds set world =
            World.getEntities3dBy (Octree.getElementsInBounds bounds set) world

        /// Get all 3d entities at the given point, including all uncullable entities.
        static member getEntitiesAtPoint3d point set world =
            World.getEntities3dBy (Octree.getElementsAtPoint point set) world

        /// Get all 3d entities in the current 3d play zone, including all uncullable entities.
        static member getEntitiesInPlay3d set world =
            let struct (playBox, playFrustum) = World.getPlayBounds3d world
            World.getEntities3dBy (Octree.getElementsInPlay playBox playFrustum set) world

        /// Get all 3d entities in the current 3d view, including all uncullable entities.
        static member getEntitiesInView3d set world =
            let frustumEnclosed = World.getEyeFrustum3dEnclosed world
            let frustumExposed = World.getEyeFrustum3dExposed world
            let frustumImposter = World.getEyeFrustum3dImposter world
            let lightBox = World.getLightBox3d world
            World.getEntities3dBy (Octree.getElementsInView frustumEnclosed frustumExposed frustumImposter lightBox set) world

        static member private updateSimulants world =

            // gather simulants
            UpdateGatherTimer.Start ()
            let screens = match World.getOmniScreenOpt world with Some omniScreen -> [omniScreen] | None -> []
            let screens = match World.getSelectedScreenOpt world with Some selectedScreen -> selectedScreen :: screens | None -> screens
            let screens = List.rev screens
            let groups = Seq.concat (List.map (flip World.getGroups world) screens)
            let (entities3d, world) = World.getEntitiesInPlay3d CachedHashSet3d world
            let (entities2d, world) = World.getEntitiesInPlay2d CachedHashSet2d world
            let entities = Seq.append entities3d entities2d
            UpdateGatherTimer.Stop ()

            // update game
            UpdateGameTimer.Start ()
            let world = World.updateGame world
            UpdateGameTimer.Stop ()
            
            // update screens
            UpdateScreensTimer.Start ()
            let world = List.fold (fun world screen -> World.updateScreen screen world) world screens
            UpdateScreensTimer.Stop ()

            // update groups
            UpdateGroupsTimer.Start ()
            let world = Seq.fold (fun world group -> World.updateGroup group world) world groups
            UpdateGroupsTimer.Stop ()

            // update entities
            UpdateEntitiesTimer.Start ()
            let advancing = World.getAdvancing world
            let world =
                Seq.fold (fun world (entity : Entity) ->
                    if not (entity.GetStatic world) && (entity.GetAlwaysUpdate world || advancing)
                    then World.updateEntity entity world
                    else world)
                    world
                    entities
            UpdateEntitiesTimer.Stop ()

            // clear cached hash sets
            CachedHashSet3d.Clear ()
            CachedHashSet2d.Clear ()

            // fin
            world

        static member private postUpdateSimulants world =

            // gather simulants
            PostUpdateGatherTimer.Start ()
            let screens = match World.getOmniScreenOpt world with Some omniScreen -> [omniScreen] | None -> []
            let screens = match World.getSelectedScreenOpt world with Some selectedScreen -> selectedScreen :: screens | None -> screens
            let screens = List.rev screens
            let groups = Seq.concat (List.map (flip World.getGroups world) screens)
#if !DISABLE_ENTITY_POST_UPDATE
            let (entities3d, world) = World.getEntitiesInPlay3d CachedHashSet3d world
            let (entities2d, world) = World.getEntitiesInPlay2d CachedHashSet2d world
            let entities = Seq.append entities3d entities2d
#endif
            PostUpdateGatherTimer.Stop ()

            // post-update game
            PostUpdateGameTimer.Start ()
            let world = World.postUpdateGame world
            PostUpdateGameTimer.Stop ()

            // post-update screens
            PostUpdateScreensTimer.Start ()
            let world = List.fold (fun world screen -> World.postUpdateScreen screen world) world screens
            PostUpdateScreensTimer.Stop ()

            // post-update groups
            PostUpdateGroupsTimer.Start ()
            let world = Seq.fold (fun world group -> World.postUpdateGroup group world) world groups
            PostUpdateGroupsTimer.Stop ()

#if !DISABLE_ENTITY_POST_UPDATE
            // post-update entities
            PostUpdateEntitiesTimer.Start ()
            let advancing = World.getAdvancing world
            let world =
                Seq.fold (fun world (entity : Entity) ->
                    if not (entity.GetStatic world) && (entity.GetAlwaysUpdate world || advancing)
                    then World.postUpdateEntity entity world
                    else world)
                    world
                    entities
            PostUpdateEntitiesTimer.Stop ()

            // clear cached hash sets
            CachedHashSet3d.Clear ()
            CachedHashSet2d.Clear ()
#endif

            // fin
            world

        static member private renderScreenTransition5 (_ : Vector2) (eyeSize : Vector2) (screen : Screen) transition world =
            match transition.DissolveImageOpt with
            | Some dissolveImage ->
                let progress = single (screen.GetTransitionUpdates world) / single (inc transition.TransitionLifeTime)
                let alpha = match transition.TransitionType with Incoming -> 1.0f - progress | Outgoing -> progress
                let color = Color.One.WithA alpha
                let position = -eyeSize.V3 * 0.5f
                let size = eyeSize.V3
                let mutable transform = Transform.makeDefault false
                transform.Position <- position
                transform.Size <- size
                transform.Elevation <- Single.MaxValue
                transform.Absolute <- true
                World.enqueueRenderLayeredMessage2d
                    { Elevation = transform.Elevation
                      Horizon = transform.Perimeter.Position.Y
                      AssetTag = AssetTag.generalize dissolveImage
                      RenderDescriptor2d =
                        SpriteDescriptor
                            { Transform = transform
                              InsetOpt = ValueNone
                              Image = dissolveImage
                              Color = color
                              Blend = Transparent
                              Glow = Color.Zero
                              Flip = FlipNone }}
                    world
            | None -> world

        static member private renderScreenTransition (screen : Screen) world =
            match screen.GetTransitionState world with
            | IncomingState -> World.renderScreenTransition5 (World.getEyePosition2d world) (World.getEyeSize2d world) screen (screen.GetIncoming world) world
            | OutgoingState -> World.renderScreenTransition5 (World.getEyePosition2d world) (World.getEyeSize2d world) screen (screen.GetOutgoing world) world
            | IdlingState -> world

        static member private renderSimulants world =

            // gather simulants
            RenderGatherTimer.Start ()
            let screens = match World.getOmniScreenOpt world with Some omniScreen -> [omniScreen] | None -> []
            let screens = match World.getSelectedScreenOpt world with Some selectedScreen -> selectedScreen :: screens | None -> screens
            let screens = List.rev screens
            let groups = Seq.concat (List.map (flip World.getGroups world) screens)
            let (entities3d, world) = World.getEntitiesInView3d CachedHashSet3d world
            let (entities2d, world) = World.getEntitiesInView2d CachedHashSet2d world
            let entities = Seq.append entities3d entities2d
            RenderGatherTimer.Stop ()

            // render simulants breadth-first
            let world = World.renderGame world
            let world = List.fold (fun world screen -> World.renderScreen screen world) world screens
            let world = match World.getSelectedScreenOpt world with Some selectedScreen -> World.renderScreenTransition selectedScreen world | None -> world
            let world = Seq.fold (fun world (group : Group) -> if group.GetVisible world then World.renderGroup group world else world) world groups

            // render entities
            RenderEntitiesTimer.Start ()
            let world =
                if World.getStandAlone world then
                    Seq.fold (fun world (entity : Entity) ->
                        if entity.GetVisible world
                        then World.renderEntity entity world
                        else world)
                        world entities
                else
                    Seq.fold (fun world (entity : Entity) ->
                        let group = entity.Group
                        if group.GetVisible world && entity.GetVisible world
                        then World.renderEntity entity world
                        else world)
                        world entities
            RenderEntitiesTimer.Stop ()

            // clear cached hash sets
            CachedHashSet3d.Clear ()
            CachedHashSet2d.Clear ()

            // fin
            world

        static member private processInput world =
            if SDL.SDL_WasInit SDL.SDL_INIT_TIMER <> 0u then
                let mutable result = (Live, world)
                let mutable polledEvent = SDL.SDL_Event ()
                while
                    SDL.SDL_PollEvent &polledEvent <> 0 &&
                    (match fst result with Live -> true | Dead -> false) do
                    result <- World.processInput2 polledEvent (snd result)
                result
            else (Dead, world)

        static member private processPhysics world =
            let physicsEngine = World.getPhysicsEngine2d world
            let (physicsMessages, physicsEngine) = physicsEngine.PopMessages ()
            let world = World.setPhysicsEngine2d physicsEngine world
            let integrationMessages = physicsEngine.Integrate (World.getUpdateRate world) physicsMessages
            let world = Seq.fold (flip World.processIntegrationMessage) world integrationMessages
            world

        static member private cleanUp world =
            let world = World.unregisterGame world
            World.cleanUpSubsystems world |> ignore

        /// Run the game engine with threading with the given handlers, but don't clean up at the end, and return the world.
        static member runWithoutCleanUp runWhile preProcess perProcess postProcess (sdlDeps : SdlDeps) liveness firstFrame world =
            TotalTimer.Start ()
            if runWhile world then
                if World.shouldSleep world then Thread.Sleep (1000 / Constants.Engine.DesiredFpsI) // don't let game run too fast while full screen unfocused
                PreProcessTimer.Start ()
                let world = preProcess world
                PreProcessTimer.Stop ()
                match liveness with
                | Live ->                
                    let world = World.updateScreenTransition world
                    match World.getLiveness world with
                    | Live ->
                        InputTimer.Start ()
                        let (liveness, world) = World.processInput world
                        InputTimer.Stop ()
                        match liveness with
                        | Live ->
                            PhysicsTimer.Start ()
                            let world = World.processPhysics world
                            PhysicsTimer.Stop ()
                            match World.getLiveness world with
                            | Live ->
                                UpdateTimer.Start ()
                                let world = World.updateSimulants world
                                UpdateTimer.Stop ()
                                match World.getLiveness world with
                                | Live ->
                                    PostUpdateTimer.Start ()
                                    let world = World.postUpdateSimulants world
                                    PostUpdateTimer.Stop ()
                                    match World.getLiveness world with
                                    | Live ->
                                        PerProcessTimer.Start ()
                                        let world = perProcess world
                                        PerProcessTimer.Stop ()
                                        match World.getLiveness world with
                                        | Live ->
                                            TaskletsTimer.Start ()
                                            WorldModule.TaskletProcessingStarted <- true
                                            let world = World.processTasklets world
                                            TaskletsTimer.Stop ()
                                            match World.getLiveness world with
                                            | Live ->
                                                DestructionTimer.Start ()
                                                let world = World.destroySimulants world
                                                DestructionTimer.Stop ()
                                                match World.getLiveness world with
                                                | Live ->
                                                    PostProcessTimer.Start ()
                                                    let world = World.postProcess world
                                                    let world = postProcess world
                                                    PostProcessTimer.Stop ()
                                                    match World.getLiveness world with
                                                    | Live ->
                                                        RenderTimer.Start ()
                                                        let world = World.renderSimulants world
                                                        RenderTimer.Stop ()
                                                        match World.getLiveness world with
                                                        | Live ->

                                                            // process audio
                                                            AudioTimer.Start ()
                                                            let world =
                                                                if SDL.SDL_WasInit SDL.SDL_INIT_AUDIO <> 0u then
                                                                    let audioPlayer = World.getAudioPlayer world
                                                                    let audioMessages = audioPlayer.PopMessages ()
                                                                    audioPlayer.Play audioMessages
                                                                    world
                                                                else world
                                                            AudioTimer.Stop ()

                                                            // process rendering
                                                            let rendererProcess = World.getRendererProcess world
                                                            if not firstFrame then rendererProcess.Swap ()
                                                            rendererProcess.SubmitMessages
                                                                (World.getEyePosition2d world)
                                                                (World.getEyeSize2d world)
                                                                (World.getEyePosition3d world)
                                                                (World.getEyeRotation3d world)
                                                                (World.getWindowSize world)

                                                            // update counters and recur
                                                            TotalTimer.Stop ()
                                                            let world = World.updateTime world
                                                            WorldModule.TaskletProcessingStarted <- false
                                                            World.runWithoutCleanUp runWhile preProcess perProcess postProcess sdlDeps liveness false world

                                                        | Dead -> world
                                                    | Dead -> world
                                                | Dead -> world
                                            | Dead -> world
                                        | Dead -> world
                                    | Dead -> world
                                | Dead -> world
                            | Dead -> world
                        | Dead -> world
                    | Dead -> world
                | Dead -> world
            else world

        /// Run the game engine with the given handler.
        static member run4 runWhile (sdlDeps : SdlDeps) liveness world =
            let result =
                try let world = World.runWithoutCleanUp runWhile id id id sdlDeps liveness true world
                    World.cleanUp world
                    Constants.Engine.ExitCodeSuccess
                with exn ->
                    let world = World.choose world
                    Log.trace (scstring exn)
                    World.cleanUp world
                    Constants.Engine.ExitCodeFailure
#if MULTITHREAD_RUN_LOOP
            // stops background threads
            Environment.Exit result
#endif
            result

[<AutoOpen>]
module EntityDispatcherModule2 =

    type World with

        static member internal signalEntity<'model, 'message, 'command when 'message :> Message and 'command :> Command> (signal : Signal) (entity : Entity) world =
            match entity.GetDispatcher world with
            | :? EntityDispatcher<'model, 'message, 'command> as dispatcher ->
                Signal.processSignal dispatcher.Message dispatcher.Command (entity.ModelGeneric<'model> ()) signal entity world
            | _ ->
                Log.info "Failed to send signal to entity."
                world

    and Entity with

        /// Send a signal to the entity.
        member this.Signal<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal world =
            World.signalEntity<'model, 'message, 'command> signal this world

    /// The elmish / MMCC dispatcher for entities.
    and [<AbstractClass>] EntityDispatcher<'model, 'message, 'command when 'message :> Message and 'command :> Command>
        (is2d, centered, physical, makeInitial : World -> 'model) =
        inherit EntityDispatcher (is2d, centered, physical)

        new (is2d, centered, physical, initial : 'model) =
            EntityDispatcher<'model, 'message, 'command> (is2d, centered, physical, fun _ -> initial)

        member this.GetModel (entity : Entity) world : 'model =
            entity.GetModelGeneric<'model> world

        member this.SetModel (model : 'model) (entity : Entity) world =
            entity.SetModelGeneric<'model> model world

        member this.Model (entity : Entity) =
            lens (nameof this.Model) entity (this.GetModel entity) (flip this.SetModel entity)

        override this.Register (entity, world) =
            let property = World.getEntityModelProperty entity world
            if property.DesignerType = typeof<unit>
            then World.setEntityModel<'model> true (makeInitial world) entity world |> snd'
            else world

        override this.ApplyPhysics (position, rotation, linearVelocity, angularVelocity, entity, world) =
            let model = this.GetModel entity world
            let (signals, model) = this.Physics (position, rotation, linearVelocity, angularVelocity, model, entity, world)
            let world = this.SetModel model entity world
            Signal.processSignals this.Message this.Command (this.Model entity) signals entity world

        override this.Render (entity, world) =
            let view = this.View (this.GetModel entity world, entity, world)
            World.renderView view world

        override this.TrySignal (signalObj, entity, world) =
            match signalObj with
            | :? 'message as message -> entity.Signal<'model, 'message, 'command> message world
            | :? 'command as command -> entity.Signal<'model, 'message, 'command> command world
            | _ -> Log.info ("Incorrect signal type received by entity (signal = '" + scstring signalObj + "'; entity = '" + scstring entity + "')."); world

        override this.TryGetInitialModelValue<'a> world =
            makeInitial world :> obj :?> 'a |> Some

        override this.TrySynchronize (initializing, entity, world) =
            let contentOld = World.getEntityContent entity world
            let model = this.GetModel entity world
            let initializers = this.Initialize (model, entity)
            let entities = this.Content (model, entity)
            let content = Content.composite entity.Name initializers entities
            let world = Content.synchronizeEntity initializing contentOld content entity entity world
            World.setEntityContent content entity world

        /// Initialize the game's own content.
        abstract member Initialize : 'model * Entity -> InitializerContent list
        default this.Initialize (_, _) = []

        /// The physics synchronization handler for the elmish / MMCC programming model.
        abstract member Physics : Vector3 * Quaternion * Vector3 * Vector3 * 'model * Entity * World -> Signal list * 'model
        default this.Physics (_, _, _, _, model, _, _) = just model

        /// The message handler of the elmish / MMCC programming model.
        abstract member Message : 'model * 'message * Entity * World -> Signal list * 'model
        default this.Message (model, _, _, _) = just model

        /// The command handler of the elmish / MMCC programming model.
        abstract member Command : 'model * 'command * Entity * World -> Signal list * World
        default this.Command (_, _, _, world) = just world

        /// The content specifier of the elmish / MMCC programming model.
        abstract member Content : 'model * Entity -> EntityContent list
        default this.Content (_, _) = []

        /// Describes how the entity is to be viewed using the View API.
        abstract member View : 'model * Entity * World -> View
        default this.View (_, _, _) = View.empty

    and [<AbstractClass>] EntityDispatcher2d<'model, 'message, 'command when 'message :> Message and 'command :> Command> (centered, physical, makeInitial : World -> 'model) =
        inherit EntityDispatcher<'model, 'message, 'command> (true, centered, physical, makeInitial)

        new (centered, physical, initial) =
            EntityDispatcher2d<'model, 'message, 'command> (centered, physical, fun _ -> initial)

        static member Properties =
            [define Entity.Centered false
             define Entity.Size Constants.Engine.EntitySize2dDefault]

    and [<AbstractClass>] EntityDispatcher3d<'model, 'message, 'command when 'message :> Message and 'command :> Command> (centered, physical, makeInitial : World -> 'model) =
        inherit EntityDispatcher<'model, 'message, 'command> (false, centered, physical, makeInitial)

        new (centered, physical, initial) =
            EntityDispatcher3d<'model, 'message, 'command> (centered, physical, fun _ -> initial)

        static member Properties =
            [define Entity.Size Constants.Engine.EntitySize3dDefault]

[<AutoOpen>]
module GuiDispatcherModule2 =

    type [<AbstractClass>] GuiDispatcher<'model, 'message, 'command when 'message :> Message and 'command :> Command> (makeInitial : World -> 'model) =
        inherit EntityDispatcher2d<'model, 'message, 'command> (false, false, makeInitial)

        new (initial : 'model) =
            GuiDispatcher<'model, 'message, 'command> (fun _ -> initial)

        static member Properties =
            [define Entity.Presence Omnipresent
             define Entity.Absolute true
             define Entity.AlwaysUpdate true
             define Entity.Size Constants.Engine.EntitySizeGuiDefault
             define Entity.DisabledColor (Color (0.75f, 0.75f, 0.75f, 0.75f))]

[<AutoOpen>]
module GroupDispatcherModule =

    type World with

        static member internal signalGroup<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal (group : Group) world =
            match group.GetDispatcher world with
            | :? GroupDispatcher<'model, 'message, 'command> as dispatcher ->
                Signal.processSignal dispatcher.Message dispatcher.Command (group.ModelGeneric<'model> ()) signal group world
            | _ ->
                Log.info "Failed to send signal to group."
                world

    and Group with

        /// Send a signal to the group.
        member this.Signal<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal world =
            World.signalGroup<'model, 'message, 'command> signal this world

    /// The elmish / MMCC dispatcher for groups.
    and [<AbstractClass>] GroupDispatcher<'model, 'message, 'command when 'message :> Message and 'command :> Command> (makeInitial : World -> 'model) =
        inherit GroupDispatcher ()

        new (initial : 'model) =
            GroupDispatcher<'model, 'message, 'command> (fun _ -> initial)

        member this.GetModel (group : Group) world : 'model =
            group.GetModelGeneric<'model> world

        member this.SetModel (model : 'model) (group : Group) world =
            group.SetModelGeneric<'model> model world

        member this.Model (group : Group) =
            lens (nameof this.Model) group (this.GetModel group) (flip this.SetModel group)

        override this.Register (group, world) =
            let property = World.getGroupModelProperty group world
            if property.DesignerType = typeof<unit>
            then World.setGroupModel<'model> true (makeInitial world) group world |> snd'
            else world

        override this.Render (group, world) =
            let view = this.View (this.GetModel group world, group, world)
            World.renderView view world

        override this.TrySignal (signalObj : obj, group, world) =
            match signalObj with
            | :? 'message as message -> group.Signal<'model, 'message, 'command> message world
            | :? 'command as command -> group.Signal<'model, 'message, 'command> command world
            | _ -> Log.info ("Incorrect signal type received by group (signal = '" + scstring signalObj + "'; group = '" + scstring group + "')."); world

        override this.TryGetInitialModelValue<'a> world =
            makeInitial world :> obj :?> 'a |> Some

        override this.TrySynchronize (initializing, group, world) =
            let contentOld = World.getGroupContent group world
            let model = this.GetModel group world
            let initializers = this.Initialize (model, group)
            let entities = this.Content (model, group)
            let content = Content.group group.Name initializers entities
            let world = Content.synchronizeGroup initializing contentOld content group group world
            World.setGroupContent content group world

        /// Initialize the group's own content.
        abstract member Initialize : 'model * Group -> InitializerContent list
        default this.Initialize (_, _) = []

        /// The message handler of the elmish / MMCC programming model.
        abstract member Message : 'model * 'message * Group * World -> Signal list * 'model
        default this.Message (model, _, _, _) = just model

        /// The command handler of the elmish / MMCC programming model.
        abstract member Command : 'model * 'command * Group * World -> Signal list * World
        default this.Command (_, _, _, world) = just world

        /// The content specifier of the elmish / MMCC programming model.
        abstract member Content : 'model * Group -> EntityContent list
        default this.Content (_, _) = []

        /// Describes how the group is to be viewed using the View API.
        abstract member View : 'model * Group * World -> View
        default this.View (_, _, _) = View.empty

[<AutoOpen>]
module ScreenDispatcherModule =

    type World with

        static member internal signalScreen<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal (screen : Screen) world =
            match screen.GetDispatcher world with
            | :? ScreenDispatcher<'model, 'message, 'command> as dispatcher ->
                Signal.processSignal dispatcher.Message dispatcher.Command (screen.ModelGeneric<'model> ()) signal screen world
            | _ ->
                Log.info "Failed to send signal to screen."
                world

    and Screen with

        /// Send a signal to the screen.
        member this.Signal<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal world =
            World.signalScreen<'model, 'message, 'command> signal this world

    /// The elmish / MMCC dispatcher for screens.
    and [<AbstractClass>] ScreenDispatcher<'model, 'message, 'command when 'message :> Message and 'command :> Command> (makeInitial : World -> 'model) =
        inherit ScreenDispatcher ()

        new (initial : 'model) =
            ScreenDispatcher<'model, 'message, 'command> (fun _ -> initial)

        member this.GetModel (screen : Screen) world : 'model =
            screen.GetModelGeneric<'model> world

        member this.SetModel (model : 'model) (screen : Screen) world =
            screen.SetModelGeneric<'model> model world

        member this.Model (screen : Screen) =
            lens (nameof this.Model) screen (this.GetModel screen) (flip this.SetModel screen)

        override this.Register (screen, world) =
            let property = World.getScreenModelProperty screen world
            if property.DesignerType = typeof<unit>
            then World.setScreenModel<'model> true (makeInitial world) screen world |> snd'
            else world

        override this.Render (screen, world) =
            let view = this.View (this.GetModel screen world, screen, world)
            World.renderView view world

        override this.TrySignal (signalObj : obj, screen, world) =
            match signalObj with
            | :? 'message as message -> screen.Signal<'model, 'message, 'command> message world
            | :? 'command as command -> screen.Signal<'model, 'message, 'command> command world
            | _ -> Log.info ("Incorrect signal type received by screen (signal = '" + scstring signalObj + "'; screen = '" + scstring screen + "')."); world

        override this.TryGetInitialModelValue<'a> world =
            makeInitial world :> obj :?> 'a |> Some

        override this.TrySynchronize (initializing, screen, world) =
            let contentOld = World.getScreenContent screen world
            let model = this.GetModel screen world
            let initializers = this.Initialize (model, screen)
            let group = this.Content (model, screen)
            let content = Content.screen screen.Name Vanilla initializers group
            let world = Content.synchronizeScreen initializing contentOld content screen screen world
            World.setScreenContent content screen world

        /// Initialize the screen's own content.
        abstract member Initialize : 'model * Screen -> InitializerContent list
        default this.Initialize (_, _) = []

        /// The message handler of the elmish / MMCC programming model.
        abstract member Message : 'model * 'message * Screen * World -> Signal list * 'model
        default this.Message (model, _, _, _) = just model

        /// The command handler of the elmish / MMCC programming model.
        abstract member Command : 'model * 'command * Screen * World -> Signal list * World
        default this.Command (_, _, _, world) = just world

        /// The content specifier of the elmish / MMCC programming model.
        abstract member Content : 'model * Screen -> GroupContent list
        default this.Content (_, _) = []

        /// Describes how the screen is to be viewed using the View API.
        abstract member View : 'model * Screen * World -> View
        default this.View (_, _, _) = View.empty

[<AutoOpen>]
module GameDispatcherModule =

    type World with

        static member internal signalGame<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal (game : Game) world =
            match game.GetDispatcher world with
            | :? GameDispatcher<'model, 'message, 'command> as dispatcher ->
                Signal.processSignal dispatcher.Message dispatcher.Command (game.ModelGeneric<'model> ()) signal game world
            | _ -> Log.info "Failed to send signal to game."; world

    and Game with

        /// Send a signal to the game.
        member this.Signal<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal world =
            World.signalGame<'model, 'message, 'command> signal this world

    /// The elmish / MMCC dispatcher for games.
    and [<AbstractClass>] GameDispatcher<'model, 'message, 'command when 'message :> Message and 'command :> Command> (makeInitial : World -> 'model) =
        inherit GameDispatcher ()

        static let synchronize initializing game world (this : GameDispatcher<'model, 'message, 'command>) =
            let contentOld = World.getGameContent world
            let model = this.GetModel game world
            let initializers = this.Initialize (model, game)
            let screens = this.Content (model, game)
            let content = Content.game initializers screens
            let (initialScreenOpt, world) = Content.synchronizeGame World.setScreenSlide initializing contentOld content game world
            (initialScreenOpt, World.setGameContent content world)

        new (initial : 'model) =
            GameDispatcher<'model, 'message, 'command> (fun _ -> initial)

        member this.GetModel (game : Game) world : 'model =
            game.GetModelGeneric<'model> world

        member this.SetModel (model : 'model) (game : Game) world =
            game.SetModelGeneric<'model> model world

        member this.Model (game : Game) =
            lens (nameof this.Model) game (this.GetModel game) (flip this.SetModel game)

        override this.Register (_, world) =
            let property = World.getGameModelProperty world
            if property.DesignerType = typeof<unit>
            then World.setGameModel<'model> true (makeInitial world) world |> snd'
            else world

        override this.Render (game, world) =
            let view = this.View (this.GetModel game world, game, world)
            World.renderView view world

        override this.TrySignal (signalObj : obj, game, world) =
            match signalObj with
            | :? 'message as message -> game.Signal<'model, 'message, 'command> message world
            | :? 'command as command -> game.Signal<'model, 'message, 'command> command world
            | _ -> Log.info ("Incorrect signal type received by game (signal = '" + scstring signalObj + "'; game = '" + scstring game + "')."); world

        override this.TryGetInitialModelValue<'a> world =
            makeInitial world :> obj :?> 'a |> Some

        override this.TrySynchronize (initializing, game, world) =
            synchronize initializing game world this |> snd

        /// Initialize the game's own content.
        abstract member Initialize : 'model * Game -> InitializerContent list
        default this.Initialize (_, _) = []

        /// The message handler of the elmish / MMCC programming model.
        abstract member Message : 'model * 'message * Game * World -> Signal list * 'model
        default this.Message (model, _, _, _) = just model

        /// The command handler of the elmish / MMCC programming model.
        abstract member Command : 'model * 'command * Game * World -> Signal list * World
        default this.Command (_, _, _, world) = just world

        /// The content specifier of the elmish / MMCC programming model.
        abstract member Content : 'model * Game -> ScreenContent list
        default this.Content (_, _) = []

        /// Describes how the game is to be viewed using the View API.
        abstract member View : 'model * Game * World -> View
        default this.View (_, _, _) = View.empty

[<AutoOpen>]
module WorldModule2' =

    type World with

        /// Send a signal to a simulant.
        static member trySignal (signal : Signal) (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> entity.TrySignal signal world
            | :? Group as group -> group.TrySignal signal world
            | :? Screen as screen -> screen.TrySignal signal world
            | :? Game as game -> game.TrySignal signal world
            | _ -> failwithumf ()

        /// Send a signal to a simulant.
        static member signal<'model, 'message, 'command when 'message :> Message and 'command :> Command> signal (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity -> entity.Signal<'model, 'message, 'command> signal world
            | :? Group as group -> group.Signal<'model, 'message, 'command> signal world
            | :? Screen as screen -> screen.Signal<'model, 'message, 'command> signal world
            | :? Game as game -> game.Signal<'model, 'message, 'command> signal world
            | _ -> failwithumf ()

        static member internal updateLateBindings3 (latebindings : LateBindings) (simulant : Simulant) world =
            match simulant with
            | :? Entity as entity ->
                let entityState = World.getEntityState entity world
                match latebindings with
                | :? Facet as facet ->
                    match Array.tryFindIndex (fun (facet2 : Facet) -> getTypeName facet2 = getTypeName facet) entityState.Facets with
                    | Some index ->
                        if entityState.Imperative
                        then entityState.Facets.[index] <- facet; world
                        else
                            let facets = entityState.Facets.Clone () :?> Facet array
                            facets.[index] <- facet
                            let entityState = { entityState with Facets = Array.ofSeq entityState.Facets }
                            World.setEntityState entityState entity world
                    | None -> world
                | :? EntityDispatcher as entityDispatcher ->
                    if getTypeName entityState.Dispatcher = getTypeName entityDispatcher then
                        if entityState.Imperative
                        then entityState.Dispatcher <- entityDispatcher; world
                        else World.setEntityState { entityState with Dispatcher = entityDispatcher } entity world
                    else world
                | _ -> world
            | :? Group as group ->
                let groupState = World.getGroupState group world
                match latebindings with
                | :? GroupDispatcher as groupDispatcher ->
                    if getTypeName groupState.Dispatcher = getTypeName groupDispatcher
                    then World.setGroupState { groupState with Dispatcher = groupDispatcher } group world
                    else world
                | _ -> world
            | :? Screen as screen ->
                let screenState = World.getScreenState screen world
                match latebindings with
                | :? ScreenDispatcher as screenDispatcher ->
                    if getTypeName screenState.Dispatcher = getTypeName screenDispatcher
                    then World.setScreenState { screenState with Dispatcher = screenDispatcher } screen world
                    else world
                | _ -> world
            | :? Game ->
                let gameState = World.getGameState world
                match latebindings with
                | :? GameDispatcher as gameDispatcher ->
                    if getTypeName gameState.Dispatcher = getTypeName gameDispatcher
                    then World.setGameState { gameState with Dispatcher = gameDispatcher } world
                    else world
                | _ -> world
            | _ -> failwithumf ()