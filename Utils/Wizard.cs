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
#endregion AiM License


namespace AiM.Utils
{
    internal static class Wizard
    {
        public static Obj_AI_Turret GetClosestEnemyTurret(this Vector3 point)
        {
            return Turrets.EnemyTurrets.OrderBy(t => t.Distance(point)).FirstOrDefault();
        }

        public static Obj_AI_Turret GetFarthestAllyTurret()
        {
            return Turrets.AllyTurrets.OrderByDescending(t => t.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
        }

        public static Obj_AI_Minion GetFarthestMinion()
        {
            return Minions.AllyMinions.OrderByDescending(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
        }

        public static Obj_AI_Minion GetFarthestMinionOnLane(Vector3 lanepos)
        {
            return Minions.AllyMinions.OrderByDescending(m => m.Distance(lanepos)).FirstOrDefault();
        }

        public static Obj_AI_Minion GetClosestEnemyMinion()
        {
            return Minions.EnemyMinions.OrderBy(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
        }

        public static int CountNearbyAllyMinions(this Obj_AI_Base x, int distance)
        {
            return Minions.AllyMinions.Count(minion => minion.Distance(x.Position) < distance);
        }

        public static int CountNearbyAllyMinions(this Vector3 x, int distance)
        {
            return Minions.AllyMinions.Count(minion => minion.Distance(x) < distance);
        }

        public static int CountNearbyAllies(this Obj_AI_Base x, int distance)
        {
            return Heroes.AllyHeroes.Count(hero => !hero.IsDead && !hero.IsMe && hero.Distance(x.Position) < distance);
        }

        public static int CountNearbyAllies(this Vector3 x, int distance)
        {
            return Heroes.AllyHeroes.Count( hero => !hero.IsDead && !hero.IsMe && hero.Distance(x) < distance);
        }

        public static int CountNearbyEnemies(this Vector3 x, int distance)
        {
            return Heroes.EnemyHeroes.Count(hero => !hero.IsDead && !hero.IsMe && hero.Distance(x) < distance);
        }

        public static bool IsHeroPet(this Obj_AI_Base minion)
        {
            var mn = minion.BaseSkinName;
            if (mn.Contains("teemo") || mn.Contains("shroom") || mn.Contains("turret") || mn.Contains("clone") || mn.Contains("blanc") || mn.Contains("trap") || mn.Contains("mine"))
            {
                return true;
            }
            return false;
        }

        public static bool IsAvoidable(this GameObject obj)
        {
            if (obj.IsAlly)
            {
                return false;
            }
            var on = obj.Name;
            if (on.Contains("teemo") || on.Contains("shroom") || on.Contains("turret") || on.Contains("clone") || on.Contains("blanc") || on.Contains("trap") || on.Contains("mine") || on.Contains("nida") || on.Contains("morg") || on.Contains("ziggs"))
            {
                return true;
            }
            return false;
        }

        public static int GetAggressiveMultiplier()
        {
            if (ObjectHandler.Player.Team == GameObjectTeam.Order)
            {
                return 1;
            }
            return -1;
        }

        public static int GetDefensiveMultiplier()
        {
            if (ObjectHandler.Player.Team == GameObjectTeam.Order)
            {
                return -1;
            }
            return 1;
        }

        public static bool IsLowHealth(this Obj_AI_Base x)
        {
            return x.HealthPercent < 30f;
        }

        public static Vector3 RandomizePosition(this GameObject o)
        {
            var r = new Random(Environment.TickCount);
            var randBy = AiMPlugin.Config.Item("RandBy").GetValue<Slider>().Value;
            return new Vector2(o.Position.X + r.Next(randBy, randBy), o.Position.Y + r.Next(randBy, randBy)).To3D();
        }

        public static Vector3 RandomizePosition(this Vector3 v)
        {
            var r = new Random(Environment.TickCount);
            var randBy = AiMPlugin.Config.Item("RandBy").GetValue<Slider>().Value;
            return new Vector2(v.X + r.Next(randBy, randBy), v.Y + r.Next(randBy, randBy)).To3D();
        }

        public static Vector3 RandomizePosition(this Vector2 v)
        {
            var r = new Random(Environment.TickCount);
            var randBy = AiMPlugin.Config.Item("RandBy").GetValue<Slider>().Value;
            return new Vector2(v.X + r.Next(randBy, randBy), v.Y + r.Next(randBy, randBy)).To3D();
        }

        public static void MoveToClosestAllyMinion()
        {
            AiMPlugin.Orbwalker.ActiveMode = LeagueSharp.Common.Orbwalking.OrbwalkingMode.Mixed;
            AiMPlugin.Orbwalker.SetAttack(false);
            AiMPlugin.Orbwalker.SetOrbwalkingPoint(GetFarthestMinion().RandomizePosition());
        }
    }
}
