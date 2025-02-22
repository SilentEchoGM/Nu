﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.IO
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldGroupModule =

    type Group with
    
        member this.GetDispatcher world = World.getGroupDispatcher this world
        member this.Dispatcher = lensReadOnly (nameof this.Dispatcher) this this.GetDispatcher
        member this.GetModelGeneric<'a> world = World.getGroupModel<'a> this world
        member this.SetModelGeneric<'a> value world = World.setGroupModel<'a> false value this world |> snd'
        member this.ModelGeneric<'a> () = lens Constants.Engine.ModelPropertyName this this.GetModelGeneric<'a> this.SetModelGeneric<'a>
        member this.GetEcs world = World.getScreenEcs this.Screen world
        member this.Ecs = lensReadOnly (nameof this.Ecs) this this.GetEcs
        member this.GetVisible world = World.getGroupVisible this world
        member this.SetVisible value world = World.setGroupVisible value this world |> snd'
        member this.Visible = lens (nameof this.Visible) this this.GetVisible this.SetVisible
        member this.GetProtected world = World.getGroupProtected this world
        member this.Protected = lensReadOnly (nameof this.Protected) this this.GetProtected
        member this.GetPersistent world = World.getGroupPersistent this world
        member this.SetPersistent value world = World.setGroupPersistent value this world |> snd'
        member this.Persistent = lens (nameof this.Persistent) this this.GetPersistent this.SetPersistent
        member this.GetDestroying world = World.getGroupDestroying this world
        member this.Destroying = lensReadOnly (nameof this.Destroying) this this.GetDestroying
        member this.GetScriptFrame world = World.getGroupScriptFrame this world
        member this.ScriptFrame = lensReadOnly (nameof this.ScriptFrame) this this.GetScriptFrame
        member this.GetOrder world = World.getGroupOrder this world
        member this.Order = lensReadOnly (nameof this.Order) this this.GetOrder
        member this.GetId world = World.getGroupId this world
        member this.Id = lensReadOnly (nameof this.Id) this this.GetId

        member this.RegisterEvent = Events.Register --> this
        member this.UnregisteringEvent = Events.Unregistering --> this
        member this.ChangeEvent propertyName = Events.Change propertyName --> this
        member this.UpdateEvent = Events.Update --> this
        member this.PostUpdateEvent = Events.PostUpdate --> this
        member this.RenderEvent = Events.Render --> this

        /// Try to get a property value and type.
        member this.TryGetProperty propertyName world =
            let mutable property = Unchecked.defaultof<_>
            let found = World.tryGetGroupProperty (propertyName, this, world, &property)
            if found then Some property else None

        /// Get a property value and type.
        member this.GetProperty propertyName world =
            World.getGroupProperty propertyName this world

        /// Get an xtension property value.
        member this.TryGet<'a> propertyName world : 'a =
            World.tryGetGroupXtensionValue<'a> propertyName this world

        /// Get an xtension property value.
        member this.Get<'a> propertyName world : 'a =
            World.getGroupXtensionValue<'a> propertyName this world

        /// Try to set a property value with explicit type.
        member this.TrySetProperty propertyName property world =
            World.trySetGroupProperty propertyName property this world

        /// Set a property value with explicit type.
        member this.SetProperty propertyName property world =
            World.setGroupProperty propertyName property this world |> snd'

        /// To try set an xtension property value.
        member this.TrySet<'a> propertyName (value : 'a) world =
            let property = { PropertyType = typeof<'a>; PropertyValue = value }
            World.trySetGroupXtensionProperty propertyName property this world

        /// Set an xtension property value.
        member this.Set<'a> propertyName (value : 'a) world =
            let property = { PropertyType = typeof<'a>; PropertyValue = value }
            World.setGroupXtensionProperty propertyName property this world

        /// Check that a group is selected.
        member this.IsSelected world =
            let gameState = World.getGameState world
            match gameState.OmniScreenOpt with
            | Some omniScreen when Address.head this.GroupAddress = Address.head omniScreen.ScreenAddress -> true
            | _ ->
                match gameState.SelectedScreenOpt with
                | Some screen when Address.head this.GroupAddress = Address.head screen.ScreenAddress -> true
                | _ -> false

        /// Check that a group exists in the world.
        member this.Exists world = World.getGroupExists this world

        /// Check that a group dispatches in the same manner as the dispatcher with the given type.
        member this.Is (dispatcherType, world) = Reflection.dispatchesAs dispatcherType (this.GetDispatcher world)

        /// Check that a group dispatches in the same manner as the dispatcher with the given type.
        member this.Is<'a> world = this.Is (typeof<'a>, world)

        /// Get a group's change event address.
        member this.GetChangeEvent propertyName = Events.Change propertyName --> this.GroupAddress

        /// Try to signal a group.
        member this.TrySignal<'message, 'command> (signal : Signal) world =
            (this.GetDispatcher world).TrySignal (signal, this, world)

    type World with

        static member internal updateGroup (group : Group) world =

            // update via dispatcher
            let dispatcher = group.GetDispatcher world
            let world = dispatcher.Update (group, world)

            // publish update event
            let eventTrace = EventTrace.debug "World" "updateGroup" "" EventTrace.empty
            World.publishPlus () (Events.Update --> group) eventTrace Simulants.Game false false world

        static member internal postUpdateGroup (group : Group) world =

            // post-update via dispatcher
            let dispatcher = group.GetDispatcher world
            let world = dispatcher.PostUpdate (group, world)

            // publish post-update event
            let eventTrace = EventTrace.debug "World" "postUpdateGroup" "" EventTrace.empty
            World.publishPlus () (Events.PostUpdate --> group) eventTrace Simulants.Game false false world

        static member internal renderGroup (group : Group) world =

            // render via dispatcher
            let dispatcher = group.GetDispatcher world
            let world = dispatcher.Render (group, world)

            // publish render event
            let eventTrace = EventTrace.debug "World" "renderGroup" "" EventTrace.empty
            World.publishPlus () (Events.Render --> group) eventTrace Simulants.Game false false world

        /// Get all the groups in a screen.
        [<FunctionBinding>]
        static member getGroups (screen : Screen) world =
            let simulants = World.getSimulants world
            match simulants.TryGetValue (screen :> Simulant) with
            | (true, groupsOpt) ->
                match groupsOpt with
                | Some groups -> groups |> Seq.map cast<Group>
                | None -> Seq.empty
            | (false, _) -> Seq.empty

        /// Create a group and add it to the world.
        [<FunctionBinding "createGroup">]
        static member createGroup4 dispatcherName nameOpt (screen : Screen) world =
            let dispatchers = World.getGroupDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None -> failwith ("Could not find a GroupDispatcher named '" + dispatcherName + "'.")
            let groupState = GroupState.make nameOpt dispatcher
            let groupState = Reflection.attachProperties GroupState.copy groupState.Dispatcher groupState world
            let group = Group (screen.ScreenAddress <-- ntoa<Group> groupState.Name)
            let world =
                if World.getGroupExists group world then
                    if group.GetDestroying world
                    then World.destroyGroupImmediate group world
                    else failwith ("Group '" + scstring group + "' already exists and cannot be created."); world
                else world
            let world = World.addGroup false groupState group world
            (group, world)

        /// Create a group from a simulant descriptor.
        static member createGroup3 descriptor screen world =
            let (group, world) =
                let groupNameOpt =
                    match descriptor.SimulantSurnamesOpt with
                    | None -> None
                    | Some [|name|] -> Some name
                    | Some _ -> failwith "Group cannot have multiple names."
                World.createGroup4 descriptor.SimulantDispatcherName groupNameOpt screen world
            let world =
                List.fold (fun world (propertyName, property) ->
                    World.setGroupProperty propertyName property group world |> snd')
                    world descriptor.SimulantProperties
            let world =
                List.fold (fun world childDescriptor ->
                    let (entity, world) = World.createEntity4 DefaultOverlay childDescriptor group world
                    // quick size entity if a size was not specified by the descriptor properties
                    if not (List.exists (fun (name, _) -> name = nameof entity.Size) childDescriptor.SimulantProperties) then
                        let quickSize = entity.GetQuickSize world
                        entity.SetSize quickSize world
                    else world)
                    world descriptor.SimulantChildren
            (group, world)

        /// Create a group and add it to the world.
        static member createGroup<'d when 'd :> GroupDispatcher> nameOpt screen world =
            World.createGroup4 typeof<'d>.Name nameOpt screen world

        /// Destroy a group in the world immediately. Can be dangerous if existing in-flight publishing depends on the
        /// group's existence. Consider using World.destroyGroup instead.
        static member destroyGroupImmediate (group : Group) world =
            let world = World.tryRemoveSimulantFromDestruction group world
            EventSystemDelegate.cleanEventAddressCache group.GroupAddress
            if World.getGroupExists group world then
                let entities = World.getEntities group world
                let world = World.unregisterGroup group world
                let world = World.removeTasklets group world
                let world = World.destroyEntitiesImmediate entities world
                World.removeGroupState group world
            else world

        /// Destroy a group in the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyGroup (group : Group) world =
            World.addSimulantToDestruction group world

        /// Destroy multiple groups in the world immediately. Can be dangerous if existing in-flight publishing depends
        /// on any of the groups' existences. Consider using World.destroyGroups instead.
        static member destroyGroupsImmediate (groups : Group seq) world =
            List.foldBack
                (fun group world -> World.destroyGroupImmediate group world)
                (List.ofSeq groups)
                world

        /// Destroy multiple groups from the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyGroups groups world =
            World.frame (World.destroyGroupsImmediate groups) Simulants.Game world

        /// Write a group to a group descriptor.
        static member writeGroup group (groupDescriptor : GroupDescriptor) world =
            let groupState = World.getGroupState group world
            let groupDispatcherName = getTypeName groupState.Dispatcher
            let groupDescriptor = { groupDescriptor with GroupDispatcherName = groupDispatcherName }
            let getGroupProperties = Reflection.writePropertiesFromTarget tautology3 groupDescriptor.GroupProperties groupState
            let groupDescriptor = { groupDescriptor with GroupProperties = getGroupProperties }
            let entities = World.getEntities group world
            { groupDescriptor with EntityDescriptors = World.writeEntities entities world }

        /// Write multiple groups to a screen descriptor.
        static member writeGroups groups world =
            groups |>
            Seq.sortBy (fun (group : Group) -> group.GetOrder world) |>
            Seq.filter (fun (group : Group) -> group.GetPersistent world) |>
            Seq.fold (fun groupDescriptors group -> World.writeGroup group GroupDescriptor.empty world :: groupDescriptors) [] |>
            Seq.rev |>
            Seq.toList

        /// Write a group to a file.
        [<FunctionBinding>]
        static member writeGroupToFile (filePath : string) group world =
            let filePathTmp = filePath + ".tmp"
            let prettyPrinter = (SyntaxAttribute.defaultValue typeof<GameDescriptor>).PrettyPrinter
            let groupDescriptor = World.writeGroup group GroupDescriptor.empty world
            let groupDescriptorStr = scstring groupDescriptor
            let groupDescriptorPretty = PrettyPrinter.prettyPrint groupDescriptorStr prettyPrinter
            File.WriteAllText (filePathTmp, groupDescriptorPretty)
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read a group from a group descriptor.
        static member readGroup groupDescriptor nameOpt (screen : Screen) world =

            // make the dispatcher
            let dispatcherName = groupDescriptor.GroupDispatcherName
            let dispatchers = World.getGroupDispatchers world
            let dispatcher =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> dispatcher
                | None -> failwith ("Could not find a GroupDispatcher named '" + dispatcherName + "'.")

            // make the group state and populate its properties
            let groupState = GroupState.make None dispatcher
            let groupState = Reflection.attachProperties GroupState.copy groupState.Dispatcher groupState world
            let groupState = Reflection.readPropertiesToTarget GroupState.copy groupDescriptor.GroupProperties groupState

            // apply the name if one is provided
            let groupState =
                match nameOpt with
                | Some name -> { groupState with Name = name }
                | None -> groupState

            // add the group's state to the world
            let group = Group (screen.ScreenAddress <-- ntoa<Group> groupState.Name)
            let world = World.addGroup true groupState group world

            // read the group's entities
            let world = World.readEntities groupDescriptor.EntityDescriptors group world |> snd
            (group, world)

        /// Read multiple groups from a screen descriptor.
        static member readGroups groupDescriptors screen world =
            let (groupsRev, world) =
                List.fold
                    (fun (groups, world) groupDescriptor ->
                        let groupNameOpt = GroupDescriptor.getNameOpt groupDescriptor
                        let (group, world) = World.readGroup groupDescriptor groupNameOpt screen world
                        (group :: groups, world))
                    ([], world)
                    groupDescriptors
            (List.rev groupsRev, world)

        /// Read a group from a file.
        [<FunctionBinding>]
        static member readGroupFromFile (filePath : string) nameOpt screen world =
            let groupDescriptorStr = File.ReadAllText filePath
            let groupDescriptor = scvalue<GroupDescriptor> groupDescriptorStr
            World.readGroup groupDescriptor nameOpt screen world