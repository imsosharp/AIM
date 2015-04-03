#region AiM License
// Copyright 2015 LeagueSharp
// Program.cs is part of AiM.
//
// AiM is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// AiM is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with AiM. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using BehaviorSharp;
using BehaviorSharp.Components.Actions;
using BehaviorSharp.Components.Composites;
using BehaviorSharp.Components.Conditionals;
using BehaviorSharp.Components.Decorators;
using ClipperLib;
using Color = System.Drawing.Color;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using GamePath = System.Collections.Generic.List<SharpDX.Vector2>;
using AiM.Utils;
#endregion AiM License


namespace AiM.Behaviors.ARAM
{
    internal static class Orbwalking
    {
        #region Actions
        internal static BehaviorAction PushLaneAction = new BehaviorAction(
            () =>
            {
                var followminion = Wizard.GetFarthestMinion();
                var randomDist = new Random(Environment.TickCount).Next(-250, +250);
                if (followminion != null && followminion.IsValid && !followminion.IsDead && !followminion.IsHeroPet())
                {
                    AiMPlugin.Orbwalker.SetOrbwalkingPoint(
                        new Vector2(followminion.Position.X + randomDist, followminion.ServerPosition.Y + randomDist).To3D());
                    AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.LaneClear;
                    return BehaviorState.Success;
                }
                return BehaviorState.Failure;
            });

        internal static BehaviorAction TeamfightAction = new BehaviorAction(
            () =>
            {
                if (ObjectHandler.Player.IsMelee())
                {
                    var target = AiMPlugin.GetMeleeTarget();
                    AiMPlugin.Orbwalker.ForceTarget(target);
                    AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Combo;
                    if (!target.UnderTurret() && target != null)
                    {
                        AiMPlugin.Orbwalker.SetOrbwalkingPoint(target.ServerPosition);
                        return BehaviorState.Success;
                    }
                    return BehaviorState.Failure;
                }
                else
                {
                    var def = (ObjectHandler.Player.AttackRange - (new Random(Environment.TickCount).Next(50, 100)) * Wizard.GetDefensiveMultiplier());
                    var orbPos = new Vector2(EasyPositioning.TeamfightPosition.X + def, EasyPositioning.TeamfightPosition.Y + def).To3D();
                    var target = AiMPlugin.GetTarget(
                        ObjectHandler.Player.AttackRange, TargetSelector.DamageType.Physical);
                    AiMPlugin.Orbwalker.ForceTarget(target);
                    AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Combo;
                    AiMPlugin.Orbwalker.SetOrbwalkingPoint(orbPos);
                    return BehaviorState.Success;
                }
            });

        internal static BehaviorAction GoToLaneAction = new BehaviorAction(
            () =>
            {
                var turret = Wizard.GetFarthestAllyTurret();
                var rInt = new Random(Environment.TickCount).Next(100, 200) * Wizard.GetAggressiveMultiplier();
                var pos = new Vector2(turret.Position.X + rInt, turret.Position.Y + rInt).To3D();
                AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Mixed;
                AiMPlugin.Orbwalker.SetOrbwalkingPoint(pos);
                if (ObjectHandler.Player.Distance(pos) < 500)
                {
                    return BehaviorState.Success;
                }
                else if(ObjectHandler.Player.GetWaypoints().Any())
                {
                    return BehaviorState.Running;
                }
                return BehaviorState.Failure;
            });

        internal static BehaviorAction FarmAction = new BehaviorAction(
            () =>
            {
                var pos = new Vector2();
                var rInt = new Random(Environment.TickCount).Next(100, 200) * Wizard.GetAggressiveMultiplier();
                if (ObjectHandler.Player.UnderTurret(true) && ObjectHandler.Player.CountNearbyAllyMinions(800) < 2)
                {
                    var nearbyAllyTurret = Turrets.AllyTurrets.OrderBy(t => t.Distance(ObjectHandler.Player.ServerPosition)).FirstOrDefault();
                    pos.X = nearbyAllyTurret.Position.X + rInt;
                    pos.Y = nearbyAllyTurret.Position.Y + rInt;
                }
                else
                {
                    pos.X = ObjectHandler.Player.ServerPosition.X + rInt; 
                    pos.Y = ObjectHandler.Player.ServerPosition.Y + rInt;
                }
                AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.LaneClear;
                AiMPlugin.Orbwalker.SetOrbwalkingPoint(pos.To3D());
                return BehaviorState.Success;
            });

        internal static BehaviorAction MixedAction = new BehaviorAction(
            () =>
            {
                if (EasyPositioning.TeamfightPosition == null)
                {
                    Console.WriteLine("TF Pos null!"); return BehaviorState.Failure;
                }
                var minion = Wizard.GetClosestEnemyMinion();
                var rInt = new Random().Next(650, 1100) * Wizard.GetDefensiveMultiplier();
                Vector3 pos;
                if (minion == null)
                {
                    pos = EasyPositioning.TeamfightPosition.To3D();
                }
                else
                {
                    pos = EasyPositioning.TeamfightPosition.To3D();
                }
                AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Mixed;
                AiMPlugin.Orbwalker.SetOrbwalkingPoint(pos);
                return BehaviorState.Success;
            });
        #endregion Actions

        #region Conditionals

        internal static Conditional ShouldPushLane = new Conditional(
            () =>
            {
                if (ObjectHandler.Player.ServerPosition.CountNearbyEnemies(4000) == 0 && Wizard.GetFarthestMinion() != null)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional ShouldFarm = new Conditional(
            () =>
            {
                if (ObjectHandler.Player.UnderTurret() && ObjectHandler.Player.Distance(Wizard.GetFarthestAllyTurret().Position) < 800 && ObjectHandler.Player.CountEnemiesInRange(1000) > 1)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional ShouldGoToLane = new Conditional(
            () =>
            {
                if (ObjectHandler.Player.Level == 3 && Wizard.GetFarthestMinion() == null)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional ShouldTeamfight = new Conditional(
            () =>
            {
                var player = ObjectHandler.Player;
                var playerPos = ObjectHandler.Player.Position;
                if (HeroManager.Enemies.Count == 0 || playerPos.CountNearbyAllies(1000) < playerPos.CountNearbyEnemies(1000))
                {
                    return false;
                }
                if (player.IsLowHealth())
                {
                    return false;
                }
                if (Positioning.AllyZone.Intersect(Positioning.EnemyZone).Count() >= Positioning.AllyZone.Count / 3)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional MixedConditional = new Conditional(
            () =>
            {
                if (HeroManager.Allies.Count == 0)
                { return false; }
                if (ShouldFarm.Tick() == BehaviorState.Failure && ShouldGoToLane.Tick() == BehaviorState.Failure &&
                    ShouldPushLane.Tick() == BehaviorState.Failure && ShouldTeamfight.Tick() == BehaviorState.Failure)
                {
                    return true;
                }
                return false;
            });

        #endregion Conditionals

        #region Inverters
        internal static Inverter DontPushLane = new Inverter(new Conditional(() => ShouldPushLane.Tick() == BehaviorState.Failure));
        internal static Inverter DontFarm = new Inverter(new Conditional(() => ShouldFarm.Tick() == BehaviorState.Failure));
        internal static Inverter DontGoToLane = new Inverter(new Conditional(() => ShouldGoToLane.Tick() == BehaviorState.Failure));
        internal static Inverter DontTeamfight = new Inverter(new Conditional(() => ShouldTeamfight.Tick() == BehaviorState.Failure));
        internal static Inverter MixedInverter = new Inverter(new Conditional(() => MixedConditional.Tick() == BehaviorState.Failure));
        #endregion

        #region Sequences
        internal static Sequence Teamfight = new Sequence(ShouldTeamfight, DontTeamfight, TeamfightAction);
        internal static Sequence PushLane = new Sequence(ShouldPushLane, DontPushLane, PushLaneAction);
        internal static Sequence Farm = new Sequence(ShouldFarm, DontFarm, FarmAction);
        internal static Sequence GoToLane = new Sequence(ShouldGoToLane, DontGoToLane, GoToLaneAction);
        internal static Sequence Mixed = new Sequence(MixedConditional, MixedInverter, MixedAction);
        #endregion
    }
}
