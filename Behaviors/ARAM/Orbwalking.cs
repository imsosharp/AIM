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
                if (ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.ServerPosition.CountEnemiesInRange(800) <= 2)
                {
                    AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.LaneClear;
                    AiMPlugin.Orbwalker.ForceTarget(ObjectManager.Player.ServerPosition.GetClosestEnemyTurret());
                }
                var followminion = Wizard.GetFarthestMinion();
                var randomDist = new Random(Environment.TickCount).Next(-250, +250);
                if (followminion != null && followminion.IsValid && !followminion.IsDead && !followminion.IsHeroPet())
                {
                    AiMPlugin.Orbwalker.SetOrbwalkingPoint(
                        new Vector2(followminion.Position.X + randomDist, followminion.ServerPosition.Y + randomDist)
                            .To3D());
                    if (ObjectManager.Player.ServerPosition.CountEnemiesInRange(800) == 0)
                    {
                        AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.LaneClear;
                    }
                    AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Mixed;
                    return BehaviorState.Success;
                }
                return BehaviorState.Failure;
            });

        internal static BehaviorAction TeamfightAction = new BehaviorAction(
            () =>
            {
                if (ObjectManager.Player.IsMelee())
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
                    //var def = (ObjectManager.Player.AttackRange - new Random(Environment.TickCount).Next(20, 70)) * Wizard.GetDefensiveMultiplier();
                    var target = AiMPlugin.GetTarget(
                        ObjectManager.Player.AttackRange, TargetSelector.DamageType.Physical);
                    AiMPlugin.Orbwalker.ForceTarget(target);
                    AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Combo;
                    AiMPlugin.Orbwalker.SetOrbwalkingPoint(EasyPositioning.Position.To3D());
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
                if (ObjectManager.Player.Distance(pos) < 500)
                {
                    return BehaviorState.Success;
                }
                else if(ObjectManager.Player.GetWaypoints().Any())
                {
                    return BehaviorState.Running;
                }
                return BehaviorState.Failure;
            });

        internal static BehaviorAction FarmAction = new BehaviorAction(
            () =>
            {
                var rInt = new Random(Environment.TickCount).Next(100, 200) * Wizard.GetAggressiveMultiplier();
                var pos = new Vector2(ObjectManager.Player.ServerPosition.X + rInt, ObjectManager.Player.Position.Y + rInt).To3D();
                AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.LaneClear;
                AiMPlugin.Orbwalker.SetOrbwalkingPoint(pos);
                return BehaviorState.Success;
            });

        internal static BehaviorAction MixedAction = new BehaviorAction(
            () =>
            {
                var minion = Wizard.GetClosestEnemyMinion();
                var rInt = new Random().Next(650, 1100) * Wizard.GetDefensiveMultiplier();
                Vector3 pos;
                if (minion == null)
                {
                    pos = EasyPositioning.Position.To3D();
                }
                else
                {
                    pos = EasyPositioning.Position.To3D();
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
                if (ObjectManager.Player.ServerPosition.CountNearbyEnemies(4000) == 0 && Wizard.GetFarthestMinion() != null)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional ShouldFarm = new Conditional(
            () =>
            {
                if (ObjectManager.Player.UnderTurret() && ObjectManager.Player.Distance(Wizard.GetFarthestAllyTurret()) < 800 && ObjectManager.Player.CountEnemiesInRange(1000) > 1)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional ShouldGoToLane = new Conditional(
            () =>
            {
                if (ObjectManager.Player.Level == 3 && Wizard.GetFarthestMinion() == null)
                {
                    return true;
                }
                return false;
            });

        internal static Conditional ShouldTeamfight = new Conditional(
            () =>
            {
                if (HeroManager.Enemies.Count == 0)
                {return false;}
                if (Positioning.AllyZone().Intersect(Positioning.EnemyZone()).Count() >= Positioning.AllyZone().Count / 3)
                {
                    return true;
                }
                var teamfightingAllies = 0;
                foreach (var ally in ObjectManager.Get<Obj_AI_Hero>().FindAll(h => h.IsAlly))
                {
                    var waypoints = ally.GetWaypoints().ToList();
                    var path = new List<IntPoint>();
                    foreach (var waypoint in waypoints)
                    {
                        path.Add(new IntPoint(waypoint.X, waypoint.Y));
                    }
                    if (Positioning.EnemyZone().Contains(path))
                    {
                        teamfightingAllies++;
                    }
                }
                if (teamfightingAllies >= 2)
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
