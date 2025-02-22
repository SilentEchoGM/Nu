﻿// Nu Game Engine.
// Copyright (C) Bryan Edds, 2013-2020.

namespace Nu
open System
open System.IO
open System.Numerics
open Prime
open Nu

[<AutoOpen; ModuleBinding>]
module WorldEntityModule =

    [<RequireQualifiedAccess>]
    module private Cached =
        let mutable Dispatcher = Unchecked.defaultof<Lens<EntityDispatcher, Entity, World>>
        let mutable Ecs = Unchecked.defaultof<Lens<Ecs.Ecs, Entity, World>>
        let mutable Facets = Unchecked.defaultof<Lens<Facet array, Entity, World>>
        let mutable Transform = Unchecked.defaultof<Lens<Transform, Entity, World>>
        let mutable PerimeterUnscaled = Unchecked.defaultof<Lens<Box3, Entity, World>>
        let mutable Perimeter = Unchecked.defaultof<Lens<Box3, Entity, World>>
        let mutable Center = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Bottom = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable PerimeterOriented = Unchecked.defaultof<Lens<Box3, Entity, World>>
        let mutable Bounds = Unchecked.defaultof<Lens<Box3, Entity, World>>
        let mutable Position = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable PositionLocal = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Rotation = Unchecked.defaultof<Lens<Quaternion, Entity, World>>
        let mutable RotationLocal = Unchecked.defaultof<Lens<Quaternion, Entity, World>>
        let mutable Scale = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable ScaleLocal = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Offset = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Angles = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable AnglesLocal = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Degrees = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable DegreesLocal = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Size = Unchecked.defaultof<Lens<Vector3, Entity, World>>
        let mutable Elevation = Unchecked.defaultof<Lens<single, Entity, World>>
        let mutable ElevationLocal = Unchecked.defaultof<Lens<single, Entity, World>>
        let mutable Overflow = Unchecked.defaultof<Lens<single, Entity, World>>
        let mutable AffineMatrix = Unchecked.defaultof<Lens<Matrix4x4, Entity, World>>
        let mutable AffineMatrixLocal = Unchecked.defaultof<Lens<Matrix4x4, Entity, World>>
        let mutable Presence = Unchecked.defaultof<Lens<Presence, Entity, World>>
        let mutable Absolute = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Imperative = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable MountOpt = Unchecked.defaultof<Lens<Entity Relation option, Entity, World>>
        let mutable Enabled = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable EnabledLocal = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Visible = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable VisibleLocal = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable AlwaysUpdate = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Protected = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Persistent = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Is2d = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Centered = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Static = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Light = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Physical = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Optimized = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable Destroying = Unchecked.defaultof<Lens<bool, Entity, World>>
        let mutable ScriptFrame = Unchecked.defaultof<Lens<Scripting.DeclarationFrame, Entity, World>>
        let mutable OverlayNameOpt = Unchecked.defaultof<Lens<string option, Entity, World>>
        let mutable FacetNames = Unchecked.defaultof<Lens<string Set, Entity, World>>
        let mutable Order = Unchecked.defaultof<Lens<int64, Entity, World>>
        let mutable Id = Unchecked.defaultof<Lens<uint64, Entity, World>>

    type Entity with
        member this.GetDispatcher world = World.getEntityDispatcher this world
        member this.Dispatcher = if notNull (this :> obj) then lensReadOnly (nameof this.Dispatcher) this this.GetDispatcher else Cached.Dispatcher
        member this.GetModelGeneric<'a> world = World.getEntityModel<'a> this world
        member this.SetModelGeneric<'a> value world = World.setEntityModel<'a> false value this world |> snd'
        member this.ModelGeneric<'a> () = lens Constants.Engine.ModelPropertyName this this.GetModelGeneric<'a> this.SetModelGeneric<'a>
        member this.GetEcs world = World.getScreenEcs this.Screen world
        member this.Ecs = if notNull (this :> obj) then lensReadOnly (nameof this.Ecs) this this.GetEcs else Cached.Ecs
        member this.GetFacets world = World.getEntityFacets this world
        member this.Facets = if notNull (this :> obj) then lensReadOnly (nameof this.Facets) this this.GetFacets else Cached.Facets
        member this.GetTransform world = World.getEntityTransform this world
        member this.SetTransform value world = let mutable value = value in World.setEntityTransformByRef (&value, World.getEntityState this world, this, world) |> snd'
        member this.Transform = if notNull (this :> obj) then lens (nameof this.Transform) this this.GetTransform this.SetTransform else Cached.Transform
        member this.SetPerimeterUnscaled value world = World.setEntityPerimeterUnscaled value this world |> snd'
        member this.GetPerimeterUnscaled world = World.getEntityPerimeterUnscaled this world
        member this.PerimeterUnscaled = if notNull (this :> obj) then lens (nameof this.PerimeterUnscaled) this this.GetPerimeterUnscaled this.SetPerimeterUnscaled else Cached.PerimeterUnscaled
        member this.SetPerimeter value world = World.setEntityPerimeter value this world |> snd'
        member this.GetPerimeter world = World.getEntityPerimeter this world
        member this.Perimeter = if notNull (this :> obj) then lens (nameof this.Perimeter) this this.GetPerimeter this.SetPerimeter else Cached.Perimeter
        member this.SetCenter value world = World.setEntityCenter value this world |> snd'
        member this.GetCenter world = World.getEntityCenter this world
        member this.Center = if notNull (this :> obj) then lens (nameof this.Center) this this.GetCenter this.SetCenter else Cached.Center
        member this.SetBottom value world = World.setEntityBottom value this world |> snd'
        member this.GetBottom world = World.getEntityBottom this world
        member this.Bottom = if notNull (this :> obj) then lens (nameof this.Bottom) this this.GetBottom this.SetBottom else Cached.Bottom
        member this.GetPerimeterOriented world = World.getEntityPerimeterOriented this world
        member this.PerimeterOriented = if notNull (this :> obj) then lensReadOnly (nameof this.PerimeterOriented) this this.GetPerimeterOriented else Cached.PerimeterOriented
        member this.GetBounds world = World.getEntityBounds this world
        member this.Bounds = if notNull (this :> obj) then lensReadOnly (nameof this.Bounds) this this.GetBounds else Cached.Bounds
        member this.GetPosition world = World.getEntityPosition this world
        member this.SetPosition value world = World.setEntityPosition value this world |> snd'
        member this.Position = if notNull (this :> obj) then lens (nameof this.Position) this this.GetPosition this.SetPosition else Cached.Position
        member this.GetPositionLocal world = World.getEntityPositionLocal this world
        member this.SetPositionLocal value world = World.setEntityPositionLocal value this world |> snd'
        member this.PositionLocal = if notNull (this :> obj) then lens (nameof this.PositionLocal) this this.GetPositionLocal this.SetPositionLocal else Cached.PositionLocal
        member this.GetRotation world = World.getEntityRotation this world
        member this.SetRotation value world = World.setEntityRotation value this world |> snd'
        member this.Rotation = if notNull (this :> obj) then lens (nameof this.Rotation) this this.GetRotation this.SetRotation else Cached.Rotation
        member this.GetRotationLocal world = World.getEntityRotationLocal this world
        member this.SetRotationLocal value world = World.setEntityRotationLocal value this world |> snd'
        member this.RotationLocal = if notNull (this :> obj) then lens (nameof this.RotationLocal) this this.GetRotationLocal this.SetRotationLocal else Cached.RotationLocal
        member this.GetScale world = World.getEntityScale this world
        member this.SetScale value world = World.setEntityScale value this world |> snd'
        member this.Scale = if notNull (this :> obj) then lens (nameof this.Scale) this this.GetScale this.SetScale else Cached.Scale
        member this.GetScaleLocal world = World.getEntityScaleLocal this world
        member this.SetScaleLocal value world = World.setEntityScaleLocal value this world |> snd'
        member this.ScaleLocal = if notNull (this :> obj) then lens (nameof this.ScaleLocal) this this.GetScaleLocal this.SetScaleLocal else Cached.ScaleLocal
        member this.GetOffset world = World.getEntityOffset this world
        member this.SetOffset value world = World.setEntityOffset value this world |> snd'
        member this.Offset = if notNull (this :> obj) then lens (nameof this.Offset) this this.GetOffset this.SetOffset else Cached.Offset
        member this.GetAngles world = World.getEntityAngles this world
        member this.SetAngles value world = World.setEntityAngles value this world |> snd'
        member this.Angles = if notNull (this :> obj) then lens (nameof this.Angles) this this.GetAngles this.SetAngles else Cached.Angles
        member this.GetAnglesLocal world = World.getEntityAnglesLocal this world
        member this.SetAnglesLocal value world = World.setEntityAnglesLocal value this world |> snd'
        member this.AnglesLocal = if notNull (this :> obj) then lens (nameof this.AnglesLocal) this this.GetAnglesLocal this.SetAnglesLocal else Cached.AnglesLocal
        member this.GetDegrees world = World.getEntityDegrees this world
        member this.SetDegrees value world = World.setEntityDegrees value this world |> snd'
        member this.Degrees = if notNull (this :> obj) then lens (nameof this.Degrees) this this.GetDegrees this.SetDegrees else Cached.Degrees
        member this.GetDegreesLocal world = World.getEntityDegreesLocal this world
        member this.SetDegreesLocal value world = World.setEntityDegreesLocal value this world |> snd'
        member this.DegreesLocal = if notNull (this :> obj) then lens (nameof this.DegreesLocal) this this.GetDegreesLocal this.SetDegreesLocal else Cached.DegreesLocal
        member this.GetSize world = World.getEntitySize this world
        member this.SetSize value world = World.setEntitySize value this world |> snd'
        member this.Size = if notNull (this :> obj) then lens (nameof this.Size) this this.GetSize this.SetSize else Cached.Size
        member this.GetElevation world = World.getEntityElevation this world
        member this.SetElevation value world = World.setEntityElevation value this world |> snd'
        member this.Elevation = if notNull (this :> obj) then lens (nameof this.Elevation) this this.GetElevation this.SetElevation else Cached.Elevation
        member this.GetElevationLocal world = World.getEntityElevationLocal this world
        member this.SetElevationLocal value world = World.setEntityElevationLocal value this world |> snd'
        member this.ElevationLocal = if notNull (this :> obj) then lens (nameof this.ElevationLocal) this this.GetElevationLocal this.SetElevationLocal else Cached.ElevationLocal
        member this.GetOverflow world = World.getEntityOverflow this world
        member this.SetOverflow value world = World.setEntityOverflow value this world |> snd'
        member this.Overflow = if notNull (this :> obj) then lens (nameof this.Overflow) this this.GetOverflow this.SetOverflow else Cached.Overflow
        member this.GetAffineMatrix world = World.getEntityAffineMatrix this world
        member this.AffineMatrix = if notNull (this :> obj) then lensReadOnly (nameof this.AffineMatrix) this this.GetAffineMatrix else Cached.AffineMatrix
        member this.GetAffineMatrixLocal world = World.getEntityAffineMatrixLocal this world
        member this.AffineMatrixLocal = if notNull (this :> obj) then lensReadOnly (nameof this.AffineMatrixLocal) this this.GetAffineMatrixLocal else Cached.AffineMatrixLocal
        member this.GetPresence world = World.getEntityPresence this world
        member this.SetPresence value world = World.setEntityPresence value this world |> snd'
        member this.Presence = if notNull (this :> obj) then lens (nameof this.Presence) this this.GetPresence this.SetPresence else Cached.Presence
        member this.GetAbsolute world = World.getEntityAbsolute this world
        member this.SetAbsolute value world = World.setEntityAbsolute value this world |> snd'
        member this.Absolute = if notNull (this :> obj) then lens (nameof this.Absolute) this this.GetAbsolute this.SetAbsolute else Cached.Absolute
        member this.GetImperative world = World.getEntityImperative this world
        member this.SetImperative value world = World.setEntityImperative value this world |> snd'
        member this.Imperative = if notNull (this :> obj) then lens (nameof this.Imperative) this this.GetImperative this.SetImperative else Cached.Imperative
        member this.GetMountOpt world = World.getEntityMountOpt this world
        member this.SetMountOpt value world = World.setEntityMountOpt value this world |> snd'
        member this.MountOpt = if notNull (this :> obj) then lens (nameof this.MountOpt) this this.GetMountOpt this.SetMountOpt else Cached.MountOpt
        member this.GetEnabled world = World.getEntityEnabled this world
        member this.SetEnabled value world = World.setEntityEnabled value this world |> snd'
        member this.Enabled = if notNull (this :> obj) then lens (nameof this.Enabled) this this.GetEnabled this.SetEnabled else Cached.Enabled
        member this.GetEnabledLocal world = World.getEntityEnabledLocal this world
        member this.SetEnabledLocal value world = World.setEntityEnabledLocal value this world |> snd'
        member this.EnabledLocal = if notNull (this :> obj) then lens (nameof this.EnabledLocal) this this.GetEnabledLocal this.SetEnabledLocal else Cached.EnabledLocal
        member this.GetVisible world = World.getEntityVisible this world
        member this.SetVisible value world = World.setEntityVisible value this world |> snd'
        member this.Visible = if notNull (this :> obj) then lens (nameof this.Visible) this this.GetVisible this.SetVisible else Cached.Visible
        member this.GetVisibleLocal world = World.getEntityVisibleLocal this world
        member this.SetVisibleLocal value world = World.setEntityVisibleLocal value this world |> snd'
        member this.VisibleLocal = if notNull (this :> obj) then lens (nameof this.VisibleLocal) this this.GetVisibleLocal this.SetVisibleLocal else Cached.VisibleLocal
        member this.GetAlwaysUpdate world = World.getEntityAlwaysUpdate this world
        member this.SetAlwaysUpdate value world = World.setEntityAlwaysUpdate value this world |> snd'
        member this.AlwaysUpdate = if notNull (this :> obj) then lens (nameof this.AlwaysUpdate) this this.GetAlwaysUpdate this.SetAlwaysUpdate else Cached.AlwaysUpdate
        member this.GetProtected world = World.getEntityProtected this world
        member this.Protected = if notNull (this :> obj) then lensReadOnly (nameof this.Protected) this this.GetProtected else Cached.Protected
        member this.GetPersistent world = World.getEntityPersistent this world
        member this.SetPersistent value world = World.setEntityPersistent value this world |> snd'
        member this.Persistent = if notNull (this :> obj) then lens (nameof this.Persistent) this this.GetPersistent this.SetPersistent else Cached.Persistent
        member this.GetIs2d world = World.getEntityIs2d this world
        member this.Is2d = if notNull (this :> obj) then lensReadOnly (nameof this.Is2d) this this.GetIs2d else Cached.Is2d
        member this.GetCentered world = World.getEntityCentered this world
        member this.SetCentered value world = World.setEntityCentered value this world |> snd'
        member this.Centered = if notNull (this :> obj) then lens (nameof this.Centered) this this.GetCentered this.SetCentered else Cached.Centered
        member this.GetStatic world = World.getEntityStatic this world
        member this.SetStatic value world = World.setEntityStatic value this world |> snd'
        member this.Static = if notNull (this :> obj) then lens (nameof this.Static) this this.GetStatic this.SetStatic else Cached.Static
        member this.GetLight world = World.getEntityLight this world
        member this.SetLight value world = World.setEntityLight value this world |> snd'
        member this.Light = if notNull (this :> obj) then lens (nameof this.Light) this this.GetLight this.SetLight else Cached.Light
        member this.GetPhysical world = World.getEntityPhysical this world
        member this.Physical = if notNull (this :> obj) then lensReadOnly (nameof this.Physical) this this.GetPhysical else Cached.Physical
        member this.GetOptimized world = World.getEntityOptimized this world
        member this.Optimized = if notNull (this :> obj) then lensReadOnly (nameof this.Optimized) this this.GetOptimized else Cached.Optimized
        member this.GetDestroying world = World.getEntityDestroying this world
        member this.Destroying = if notNull (this :> obj) then lensReadOnly (nameof this.Destroying) this this.GetDestroying else Cached.Destroying
        member this.GetScriptFrame world = World.getEntityScriptFrame this world
        member this.ScriptFrame = if notNull (this :> obj) then lensReadOnly (nameof this.ScriptFrame) this this.GetScriptFrame else Cached.ScriptFrame
        member this.GetOverlayNameOpt world = World.getEntityOverlayNameOpt this world
        member this.OverlayNameOpt = if notNull (this :> obj) then lensReadOnly (nameof this.OverlayNameOpt) this this.GetOverlayNameOpt else Cached.OverlayNameOpt
        member this.GetFacetNames world = World.getEntityFacetNames this world
        member this.FacetNames = if notNull (this :> obj) then lensReadOnly (nameof this.FacetNames) this this.GetFacetNames else Cached.FacetNames
        member this.GetOrder world = World.getEntityOrder this world
        member this.Order = if notNull (this :> obj) then lensReadOnly (nameof this.Order) this this.GetOrder else Cached.Order
        member this.GetId world = World.getEntityId this world
        member this.Id = if notNull (this :> obj) then lensReadOnly (nameof this.Id) this this.GetId else Cached.Id
        static member internal init () =
            Cached.Dispatcher <- lensReadOnly (nameof Cached.Dispatcher) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Ecs <- lensReadOnly (nameof Cached.Ecs) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Facets <- lensReadOnly (nameof Cached.Facets) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Transform <- lens (nameof Cached.Transform) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.PerimeterUnscaled <- lens (nameof Cached.PerimeterUnscaled) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Perimeter <- lens (nameof Cached.Perimeter) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Center <- lens (nameof Cached.Center) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Bottom <- lens (nameof Cached.Bottom) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.PerimeterOriented <- lensReadOnly (nameof Cached.PerimeterOriented) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Bounds <- lensReadOnly (nameof Cached.Bounds) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Position <- lens (nameof Cached.Position) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.PositionLocal <- lens (nameof Cached.PositionLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Rotation <- lens (nameof Cached.Rotation) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.RotationLocal <- lens (nameof Cached.RotationLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Scale <- lens (nameof Cached.Scale) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.ScaleLocal <- lens (nameof Cached.ScaleLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Offset <- lens (nameof Cached.Offset) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Angles <- lens (nameof Cached.Angles) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.AnglesLocal <- lens (nameof Cached.AnglesLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Degrees <- lens (nameof Cached.Degrees) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.DegreesLocal <- lens (nameof Cached.DegreesLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Size <- lens (nameof Cached.Size) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Elevation <- lens (nameof Cached.Elevation) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.ElevationLocal <- lens (nameof Cached.ElevationLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Overflow <- lens (nameof Cached.Overflow) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.AffineMatrix <- lensReadOnly (nameof Cached.AffineMatrix) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.AffineMatrixLocal <- lensReadOnly (nameof Cached.AffineMatrixLocal) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Presence <- lens (nameof Cached.Presence) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Absolute <- lens (nameof Cached.Absolute) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Imperative <- lens (nameof Cached.Imperative) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.MountOpt <- lens (nameof Cached.MountOpt) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Enabled <- lens (nameof Cached.Enabled) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.EnabledLocal <- lens (nameof Cached.EnabledLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Visible <- lens (nameof Cached.Visible) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.VisibleLocal <- lens (nameof Cached.VisibleLocal) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.AlwaysUpdate <- lens (nameof Cached.AlwaysUpdate) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Persistent <- lens (nameof Cached.Persistent) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Is2d <- lensReadOnly (nameof Cached.Is2d) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Centered <- lens (nameof Cached.Centered) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Static <- lens (nameof Cached.Static) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Light <- lens (nameof Cached.Light) Unchecked.defaultof<_> Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Physical <- lensReadOnly (nameof Cached.Physical) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Optimized <- lensReadOnly (nameof Cached.Optimized) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Destroying <- lensReadOnly (nameof Cached.Destroying) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.ScriptFrame <- lensReadOnly (nameof Cached.ScriptFrame) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.OverlayNameOpt <- lensReadOnly (nameof Cached.OverlayNameOpt) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.FacetNames <- lensReadOnly (nameof Cached.FacetNames) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Order <- lensReadOnly (nameof Cached.Order) Unchecked.defaultof<_> Unchecked.defaultof<_>
            Cached.Id <- lensReadOnly (nameof Cached.Id) Unchecked.defaultof<_> Unchecked.defaultof<_>

        member this.RegisterEvent = Events.Register --> this
        member this.UnregisteringEvent = Events.Unregistering --> this
        member this.ChangeEvent propertyName = Events.Change propertyName --> this
        member this.UpdateEvent = Events.Update --> this
#if !DISABLE_ENTITY_POST_UPDATE
        member this.PostUpdateEvent = Events.PostUpdate --> this
#endif
        member this.RenderEvent = Events.Render --> this

        /// The state of an entity.
        /// The only place this accessor should be used is in performance-sensitive code.
        /// Otherwise, you should get and set the required entity properties via the Entity interface.
        member this.State world =
            let entityState = World.getEntityState this world
#if DEBUG
            if World.getImperative world && not entityState.Optimized then
                failwith "Can get the entity state of an entity only if it is Optimized (Imperative, Omnipresent, and not PublishChangeEvents)."
#endif
            entityState

        /// The copied state of an entity.
        /// The only place this accessor should be used is in performance-sensitive code.
        /// Otherwise, you should get and set the required entity properties via the Entity interface.
        member this.StateReadOnly world =
            world |> World.getEntityState this |> EntityState.copy

        /// Optimize an entity by setting { Imperative = true; Omnipresent = true }.
        member this.Optimize world =
            let world = this.SetImperative true world
            let world = this.SetPresence Omnipresent world
            world

        /// Set the transform of an entity.
        member this.SetTransformByRef (value : Transform byref, world) =
            World.setEntityTransformByRef (&value, World.getEntityState this world, this, world)

        /// Set the transform of an entity without generating any change events.
        member this.SetTransformByRefWithoutEvent (value : Transform inref, world) =
            World.setEntityTransformByRefWithoutEvent (&value, World.getEntityState this world, this, world)

        /// Set the transform of an entity without generating any change events.
        member this.SetTransformWithoutEvent value world =
            World.setEntityTransformByRefWithoutEvent (&value, World.getEntityState this world, this, world)

        /// Set the transform of an entity snapped to the give position and rotation snaps.
        member this.SetTransformSnapped positionSnap degreesSnap scaleSnap (value : Transform) world =
            let mutable transform = value
            transform.Snap (positionSnap, degreesSnap, scaleSnap)
            this.SetTransform transform world

        /// Try to get a property value and type.
        member this.TryGetProperty propertyName world =
            let mutable property = Unchecked.defaultof<_>
            let found = World.tryGetEntityProperty (propertyName, this, world, &property)
            if found then Some property else None

        /// Get a property value and type.
        member this.GetProperty propertyName world =
            World.getEntityProperty propertyName this world

        /// Get an xtension property value.
        member this.TryGet<'a> propertyName world : 'a =
            World.tryGetEntityXtensionValue<'a> propertyName this world

        /// Get an xtension property value.
        member this.Get<'a> propertyName world : 'a =
            World.getEntityXtensionValue<'a> propertyName this world

        /// Try to set a property value with explicit type.
        member this.TrySetProperty propertyName property world =
            World.trySetEntityProperty propertyName property this world

        /// Set a property value with explicit type.
        member this.SetProperty propertyName property world =
            World.setEntityProperty propertyName property this world |> snd'

        /// To try set an xtension property value.
        member this.TrySet<'a> propertyName (value : 'a) world =
            let property = { PropertyType = typeof<'a>; PropertyValue = value }
            World.trySetEntityXtensionProperty propertyName property this world

        /// Set an xtension property value.
        member this.Set<'a> propertyName (value : 'a) world =
            World.setEntityXtensionValue<'a> propertyName value this world

        /// Set an xtension property value without publishing an event.
        member internal this.SetXtensionPropertyWithoutEvent<'a> propertyName (value : 'a) world =
            let property = { PropertyType = typeof<'a>; PropertyValue = value }
            let struct (_, _, world) = World.setEntityXtensionPropertyWithoutEvent propertyName property this world
            world

        /// Attach a property.
        member this.AttachProperty propertyName property world =
            World.attachEntityProperty propertyName property this world

        /// Detach a property.
        member this.DetachProperty propertyName world =
            World.detachEntityProperty propertyName this world

        /// Get an entity's sorting priority in 2d.
        member this.GetSortingPriority2d world = World.getEntitySortingPriority2d this world

        /// Get an entity's quick size.
        member this.GetQuickSize world = World.getEntityQuickSize this world

        /// Check that an entity is in the camera's view.
        member this.GetInView2d world = World.getEntityInView2d this world

        /// Check that an entity is in the camera's view.
        member this.GetInView3d world = World.getEntityInView3d this world

        /// Check that an entity is selected.
        member this.IsSelected world =
            let gameState = World.getGameState world
            match gameState.OmniScreenOpt with
            | Some omniScreen when Address.head this.EntityAddress = Address.head omniScreen.ScreenAddress -> true
            | _ ->
                match gameState.SelectedScreenOpt with
                | Some screen when Address.head this.EntityAddress = Address.head screen.ScreenAddress -> true
                | _ -> false

        /// Check that an entity exists in the world.
        member this.Exists world = World.getEntityExists this world

        /// Check if an entity is intersected by a ray.
        member this.RayCast ray world = World.rayCastEntity ray this world

        /// Get the entity's highlight bounds.
        member this.GetHighlightBounds world = World.getEntityHighlightBounds this world

        /// Set an entity's size by its quick size.
        member this.QuickSize world = World.setEntitySize (this.GetQuickSize world) this world |> snd'

        /// Set an entity's mount while adjusting its mount properties such that they do not change.
        member this.SetMountOptWithAdjustment (value : Entity Relation option) world =
            let world =
                match
                    (Option.bind (tryResolve this) (this.GetMountOpt world),
                     Option.bind (tryResolve this) value) with
                | (Some mountOld, Some mountNew) ->
                    if mountOld.Exists world && mountNew.Exists world then
                        let affineMatrixMount = World.getEntityAffineMatrix mountNew world
                        let affineMatrixMounter = World.getEntityAffineMatrix this world
                        let affineMatrixLocal = affineMatrixMounter * Matrix4x4.Inverse affineMatrixMount
                        let positionLocal = affineMatrixLocal.Translation
                        let rotationLocal = affineMatrixLocal.Rotation
                        let scaleLocal = affineMatrixLocal.Scale
                        let elevationLocal = this.GetElevation world - mountNew.GetElevation world
                        let world = this.SetPositionLocal positionLocal world
                        let world = this.SetRotationLocal rotationLocal world
                        let world = this.SetScaleLocal scaleLocal world
                        let world = this.SetElevationLocal elevationLocal world
                        let world = this.SetVisible (this.GetVisibleLocal world && mountNew.GetVisible world) world
                        let world = this.SetEnabled (this.GetEnabledLocal world && mountNew.GetEnabled world) world
                        world
                    else world
                | (Some mountOld, None) ->
                    if mountOld.Exists world then
                        let world = this.SetPositionLocal v3Zero world
                        let world = this.SetRotationLocal quatIdentity world
                        let world = this.SetScaleLocal v3Zero world
                        let world = this.SetElevationLocal 0.0f world
                        let world = this.SetVisible (this.GetVisibleLocal world) world
                        let world = this.SetEnabled (this.GetEnabledLocal world) world
                        world
                    else world
                | (None, Some mountNew) ->
                    if mountNew.Exists world then
                        let affineMatrixMount = World.getEntityAffineMatrix mountNew world
                        let affineMatrixMounter = World.getEntityAffineMatrix this world
                        let affineMatrixLocal = affineMatrixMounter * Matrix4x4.Inverse affineMatrixMount
                        let positionLocal = affineMatrixLocal.Translation
                        let rotationLocal = affineMatrixLocal.Rotation
                        let scaleLocal = affineMatrixLocal.Scale
                        let elevationLocal = this.GetElevation world - mountNew.GetElevation world
                        let world = this.SetPositionLocal positionLocal world
                        let world = this.SetRotationLocal rotationLocal world
                        let world = this.SetScaleLocal scaleLocal world
                        let world = this.SetElevationLocal elevationLocal world
                        let world = this.SetVisible (this.GetVisibleLocal world && mountNew.GetVisible world) world
                        let world = this.SetEnabled (this.GetEnabledLocal world && mountNew.GetEnabled world) world
                        world
                    else world
                | (None, None) -> world
            this.SetMountOpt value world

        /// Check whether the entity's mount exists.
        member this.MountExists world =
            match Option.bind (tryResolve this) (this.GetMountOpt world) with
            | Some mount -> mount.Exists world
            | None -> false

        /// Get an entity's mounters.
        member this.GetMounters world = World.getEntityMounters this world

        /// Traverse an entity's mounters.
        member this.TraverseMounters effect world = World.traverseEntityMounters effect this world

        /// Get an entity's children.
        member this.GetChildren world = World.getEntityEntities this world

        /// Traverse an entity's children.
        member this.TraverseChildren effect world = World.traverseEntityEntities effect this world

        /// Apply physics changes to an entity.
        member this.ApplyPhysics (position : Vector3) rotation linearVelocity angularVelocity world =
            let mutable oldTransform = this.GetTransform world
            let mutable newTransform = oldTransform
            let world =
                if  v3Neq oldTransform.Position position ||
                    quatNeq oldTransform.Rotation rotation then
                    newTransform.Position <- position
                    newTransform.Rotation <- rotation
                    this.SetTransformByRefWithoutEvent (&newTransform, world)
                else world
            let world = this.SetXtensionPropertyWithoutEvent "LinearVelocity" linearVelocity world
            let world = this.SetXtensionPropertyWithoutEvent "AngularVelocity" angularVelocity world
            let dispatcher = this.GetDispatcher world
            dispatcher.ApplyPhysics (position, rotation, linearVelocity, angularVelocity, this, world)

        /// Propagate entity physics properties into the physics system.
        member this.PropagatePhysics world =
            if WorldModule.isSelected this world
            then World.propagateEntityPhysics this world
            else world

        /// Check that an entity uses a facet of the given type.
        member this.Has (facetType, world) = Array.exists (fun facet -> getType facet = facetType) (this.GetFacets world)

        /// Check that an entity uses a facet of the given type.
        member this.Has<'a> world = this.Has (typeof<'a>, world)

        /// Check that an entity dispatches in the same manner as the dispatcher with the given type.
        member this.Is (dispatcherType, world) = Reflection.dispatchesAs dispatcherType (this.GetDispatcher world)

        /// Check that an entity dispatches in the same manner as the dispatcher with the given type.
        member this.Is<'a> world = this.Is (typeof<'a>, world)

        /// Get an entity's change event address.
        member this.GetChangeEvent propertyName = Events.Change propertyName --> this.EntityAddress

        /// Try to signal an entity.
        member this.TrySignal<'message, 'command> (signal : Signal) world =
            (this.GetDispatcher world).TrySignal (signal, this, world)

    type World with

        static member internal updateEntity (entity : Entity) world =
            let dispatcher = entity.GetDispatcher world
            let world = dispatcher.Update (entity, world)
            let facets = entity.GetFacets world
            let world =
                if Array.notEmpty facets // OPTIMIZATION: avoid lambda allocation.
                then Array.fold (fun world (facet : Facet) -> facet.Update (entity, world)) world facets
                else world
            if World.getEntityPublishUpdates entity world then
                let eventTrace = EventTrace.debug "World" "updateEntity" "" EventTrace.empty
                World.publishPlus () entity.UpdateEvent eventTrace Simulants.Game false false world
            else world

#if !DISABLE_ENTITY_POST_UPDATE
        static member internal postUpdateEntity (entity : Entity) world =
            let dispatcher = entity.GetDispatcher world
            let world = dispatcher.PostUpdate (entity, world)
            let facets = entity.GetFacets world
            let world =
                if Array.notEmpty facets // OPTIMIZATION: avoid lambda allocation.
                then Array.fold (fun world (facet : Facet) -> facet.PostUpdate (entity, world)) world facets
                else world
            if World.getEntityPublishPostUpdates entity world then
                let eventTrace = EventTrace.debug "World" "postUpdateEntity" "" EventTrace.empty
                World.publishPlus () entity.PostUpdateEvent eventTrace Simulants.Game false false world
            else world
#endif

        static member internal renderEntity (entity : Entity) world =
            let dispatcher = entity.GetDispatcher world
            let world = dispatcher.Render (entity, world)
            let facets = entity.GetFacets world
            let world =
                if Array.notEmpty facets // OPTIMIZATION: avoid lambda allocation.
                then Array.fold (fun world (facet : Facet) -> facet.Render (entity, world)) world facets
                else world
            if World.getEntityPublishRenders entity world then
                let eventTrace = EventTrace.debug "World" "renderEntity" "" EventTrace.empty
                World.publishPlus () entity.RenderEvent eventTrace Simulants.Game false false world
            else world

        /// Get all the entities in a group.
        [<FunctionBinding>]
        static member getEntitiesFlattened (group : Group) world =
            let rec getEntitiesRec parent world =
                let simulants = World.getSimulants world
                match simulants.TryGetValue parent with
                | (true, entitiesOpt) ->
                    match entitiesOpt with
                    | Some entities ->
                        seq {
                            yield! Seq.map cast<Entity> entities
                            for entity in entities do
                                yield! getEntitiesRec entity world }
                    | None -> Seq.empty
                | (false, _) -> Seq.empty
            getEntitiesRec (group :> Simulant) world |> SegmentedList.ofSeq |> seq

        /// Get all the entities directly parented by the group.
        [<FunctionBinding>]
        static member getEntities (group : Group) world =
            let simulants = World.getSimulants world
            match simulants.TryGetValue (group :> Simulant) with
            | (true, entitiesOpt) ->
                match entitiesOpt with
                | Some entities -> entities |> Seq.map cast<Entity> |> seq
                | None -> Seq.empty
            | (false, _) -> Seq.empty

        /// Get all the entities not mounting another entity in a group.
        [<FunctionBinding>]
        static member getEntitiesSovereign group world =
            World.getEntitiesFlattened group world |>
            Seq.filter (fun entity -> Option.isNone (entity.GetMountOpt world))

        /// Destroy an entity in the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyEntity (entity : Entity) world =
            World.addSimulantToDestruction entity world

        /// Destroy multiple entities in the world immediately. Can be dangerous if existing in-flight publishing
        /// depends on any of the entities' existences. Consider using World.destroyEntities instead.
        static member destroyEntitiesImmediate (entities : Entity seq) world =
            List.foldBack
                (fun entity world -> World.destroyEntityImmediate entity world)
                (List.ofSeq entities)
                world

        /// Destroy multiple entities in the world at the end of the current update.
        [<FunctionBinding>]
        static member destroyEntities entities world =
            World.frame (World.destroyEntitiesImmediate entities) Simulants.Game world

        /// Sort the given 2d entities.
        /// If there are a lot of entities, this may allocate in the LOH.
        static member sortEntities2d entities world =
            entities |>
            Array.ofSeq |>
            Array.rev |>
            Array.map (fun (entity : Entity) -> entity.GetSortingPriority2d world) |>
            Array.sortStableWith SortPriority.compare |>
            Array.map (fun p -> p.SortTarget :?> Entity)

        /// Try to pick an entity at the given position.
        [<FunctionBinding>]
        static member tryPickEntity2d position entities world =
            let entitiesSorted = World.sortEntities2d entities world
            Array.tryFind
                (fun (entity : Entity) ->
                    let viewport = World.getViewport world
                    let eyePosition = World.getEyePosition2d world
                    let eyeSize = World.getEyeSize2d world
                    let positionWorld = viewport.MouseToWorld2d (entity.GetAbsolute world, position, eyePosition, eyeSize)
                    let perimeterOriented = (entity.GetPerimeterOriented world).Box2
                    perimeterOriented.Intersects positionWorld)
                entitiesSorted

        /// Try to pick a 3d entity with the given ray.
        [<FunctionBinding>]
        static member tryPickEntity3d position entities world =
            let intersectionses =
                Seq.map
                    (fun (entity : Entity) ->
                        let viewport = World.getViewport world
                        let eyePosition = World.getEyePosition3d world
                        let eyeRotation = World.getEyeRotation3d world
                        let rayWorld = viewport.MouseToWorld3d (entity.GetAbsolute world, position, eyePosition, eyeRotation)
                        let entityBounds = entity.GetBounds world
                        let intersectionOpt = rayWorld.Intersects entityBounds
                        if intersectionOpt.HasValue then
                            let intersections = entity.RayCast rayWorld world
                            Array.map (fun intersection -> (intersection, entity)) intersections
                        else [||])
                    entities
            let intersections = intersectionses |> Seq.concat |> Seq.toArray
            let sorted = Array.sortBy fst intersections
            Array.tryHead sorted

        /// Try to find the entity in the given entity's group with the closest previous order.
        static member tryGetPreviousEntity (entity : Entity) world =
            let order = World.getEntityOrder entity world
            let mutable previousOrderDeltaOpt = ValueNone
            let mutable previousOpt = ValueNone
            let entities = World.getEntitiesFlattened entity.Group world
            for entity2 in entities do
                let order2 = World.getEntityOrder entity2 world
                let orderDelta = order - order2
                if orderDelta > 0L then
                    match previousOrderDeltaOpt with
                    | ValueSome orderDelta2 ->
                        if orderDelta < orderDelta2 then
                            previousOrderDeltaOpt <- ValueSome orderDelta
                            previousOpt <- ValueSome entity2
                    | ValueNone ->
                        previousOrderDeltaOpt <- ValueSome orderDelta
                        previousOpt <- ValueSome entity2
            match previousOpt with
            | ValueSome previous -> Some previous
            | ValueNone -> None

        /// Change an entity's order between that of before and after's.
        static member reorderEntity entityBefore entityAfter entity world =
            let orderBefore = World.getEntityOrder entityBefore world;
            let orderAfter = World.getEntityOrder entityAfter world;
            let order = (orderBefore + orderAfter) / 2L;
            World.setEntityOrder order entity world |> snd'

        /// Change an entity's order between that of target and its previous sibling's.
        static member insertEntity target entity world =
            match World.tryGetPreviousEntity target world with
            | Some previous -> World.reorderEntity previous target entity world
            | None -> world

        /// Write an entity to an entity descriptor.
        static member writeEntity (entity : Entity) (entityDescriptor : EntityDescriptor) world =
            let overlayer = World.getOverlayer world
            let entityState = World.getEntityState entity world
            let entityDispatcherName = getTypeName entityState.Dispatcher
            let entityDescriptor = { entityDescriptor with EntityDispatcherName = entityDispatcherName }
            let entityFacetNames = World.getEntityFacetNamesReflectively entityState
            let overlaySymbolsOpt =
                match entityState.OverlayNameOpt with
                | Some overlayName -> Some (Overlayer.getOverlaySymbols overlayName entityFacetNames overlayer)
                | None -> None
            let shouldWriteProperty = fun propertyName propertyType (propertyValue : obj) ->
                if propertyName = Constants.Engine.OverlayNameOptPropertyName && propertyType = typeof<string option> then
                    let defaultOverlayNameOpt = World.getEntityDefaultOverlayName entityDispatcherName world
                    defaultOverlayNameOpt <> (propertyValue :?> string option)
                else
                    match overlaySymbolsOpt with
                    | Some overlaySymbols -> Overlayer.shouldPropertySerialize propertyName propertyType entityState overlaySymbols
                    | None -> true
            let entityProperties = Reflection.writePropertiesFromTarget shouldWriteProperty entityDescriptor.EntityProperties entityState
            let entityDescriptor = { entityDescriptor with EntityProperties = entityProperties }
            let entityDescriptor =
                if not (Gen.isNameGenerated entity.Name)
                then EntityDescriptor.setNameOpt (Some entity.Name) entityDescriptor
                else entityDescriptor
            let entities = World.getEntityEntities entity world
            { entityDescriptor with EntityDescriptors = World.writeEntities entities world }

        /// Write multiple entities to a group descriptor.
        static member writeEntities entities world =
            entities |>
            Seq.sortBy (fun (entity : Entity) -> entity.GetOrder world) |>
            Seq.filter (fun (entity : Entity) -> entity.GetPersistent world) |>
            Seq.fold (fun entityDescriptors entity -> World.writeEntity entity EntityDescriptor.empty world :: entityDescriptors) [] |>
            Seq.rev |>
            Seq.toList

        /// Write an entity to a file.
        [<FunctionBinding>]
        static member writeEntityToFile (filePath : string) enity world =
            let filePathTmp = filePath + ".tmp"
            let prettyPrinter = (SyntaxAttribute.defaultValue typeof<GameDescriptor>).PrettyPrinter
            let enityDescriptor = World.writeEntity enity EntityDescriptor.empty world
            let enityDescriptorStr = scstring enityDescriptor
            let enityDescriptorPretty = PrettyPrinter.prettyPrint enityDescriptorStr prettyPrinter
            File.WriteAllText (filePathTmp, enityDescriptorPretty)
            File.Delete filePath
            File.Move (filePathTmp, filePath)

        /// Read an entity from an entity descriptor.
        static member readEntity entityDescriptor (nameOpt : string option) (parent : Simulant) world =

            // make the dispatcher
            let dispatcherName = entityDescriptor.EntityDispatcherName
            let dispatchers = World.getEntityDispatchers world
            let (dispatcherName, dispatcher) =
                match Map.tryFind dispatcherName dispatchers with
                | Some dispatcher -> (dispatcherName, dispatcher)
                | None -> failwith ("Could not find an EntityDispatcher named '" + dispatcherName + "'.")

            // get the default overlay name option
            let defaultOverlayNameOpt = World.getEntityDefaultOverlayName dispatcherName world

            // make the bare entity state with name as id
            let entityState = EntityState.make (World.getImperative world) None defaultOverlayNameOpt dispatcher

            // attach the entity state's intrinsic facets and their properties
            let entityState = World.attachIntrinsicFacetsViaNames entityState world

            // read the entity state's overlay and apply it to its facet names if applicable
            let overlayer = World.getOverlayer world
            let entityState = Reflection.tryReadOverlayNameOptToTarget id entityDescriptor.EntityProperties entityState
            let entityState = if Option.isNone entityState.OverlayNameOpt then { entityState with OverlayNameOpt = defaultOverlayNameOpt } else entityState
            let entityState =
                match (defaultOverlayNameOpt, entityState.OverlayNameOpt) with
                | (Some defaultOverlayName, Some overlayName) -> Overlayer.applyOverlayToFacetNames id defaultOverlayName overlayName entityState overlayer overlayer
                | (_, _) -> entityState

            // read the entity state's facet names
            let entityState = Reflection.readFacetNamesToTarget id entityDescriptor.EntityProperties entityState

            // attach the entity state's dispatcher properties
            let entityState = Reflection.attachProperties id entityState.Dispatcher entityState world
            
            // synchronize the entity state's facets (and attach their properties)
            let entityState =
                match World.trySynchronizeFacetsToNames Set.empty entityState None world with
                | Right (entityState, _) -> entityState
                | Left error -> Log.debug error; entityState

            // attempt to apply the entity state's overlay
            let entityState =
                match entityState.OverlayNameOpt with
                | Some overlayName ->
                    // OPTIMIZATION: applying overlay only when it will change something
                    if Overlay.dispatcherNameToOverlayName dispatcherName <> overlayName then
                        let facetNames = World.getEntityFacetNamesReflectively entityState
                        Overlayer.applyOverlay id dispatcherName overlayName facetNames entityState overlayer
                    else entityState
                | None -> entityState

            // try to read entity name
            let entityNameOpt = EntityDescriptor.getNameOpt entityDescriptor

            // read the entity state's values
            let entityState = Reflection.readPropertiesToTarget id entityDescriptor.EntityProperties entityState

            // configure the name and surnames
            let (name, surnames) =
                match nameOpt with
                | Some name -> (name, Array.add name parent.SimulantAddress.Names)
                | None ->
                    match entityNameOpt with
                    | Some entityName -> (entityName, Array.add entityName parent.SimulantAddress.Names)
                    | None ->
                        let name = Gen.name
                        let surnames = Array.add name parent.SimulantAddress.Names
                        (name, surnames)
            let entityState = { entityState with Surnames = surnames }

            // make entity address
            let entityAddress = parent.SimulantAddress.Names |> Array.add name |> rtoa

            // make entity reference
            let entity = Entity entityAddress

            // add entity's state to world
            let world =
                if World.getEntityExists entity world then
                    if World.getEntityDestroying entity world
                    then World.destroyEntityImmediate entity world
                    else failwith ("Entity '" + scstring entity + "' already exists and cannot be created."); world
                else world
            let world = World.addEntity true entityState entity world

            // update mount hierarchy
            let mountOpt = World.getEntityMountOpt entity world
            let world = World.addEntityToMounts mountOpt entity world
            
            // read the entity's children
            let world = World.readEntities entityDescriptor.EntityDescriptors entity world |> snd
            (entity, world)

        /// Read an entity from a file.
        [<FunctionBinding>]
        static member readEntityFromFile (filePath : string) nameOpt group world =
            let entityDescriptorStr = File.ReadAllText filePath
            let entityDescriptor = scvalue<EntityDescriptor> entityDescriptorStr
            World.readEntity entityDescriptor nameOpt group world

        /// Read multiple entities.
        static member readEntities (entityDescriptors : EntityDescriptor list) (parent : Simulant) world =
            let (entitiesRev, world) =
                List.fold
                    (fun (entities, world) entityDescriptor ->
                        let nameOpt = EntityDescriptor.getNameOpt entityDescriptor
                        let (entity, world) = World.readEntity entityDescriptor nameOpt parent world
                        (entity :: entities, world))
                        ([], world)
                        entityDescriptors
            (List.rev entitiesRev, world)