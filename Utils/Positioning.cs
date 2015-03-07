#region AiM License
// Copyright 2015 LeagueSharp
// Positioning.cs is part of AiM.
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
    public static class EasyPositioning
    {
        /// <summary>
        /// Returns a random position in the team zone or the position of the ally champion farthest from base
        /// </summary>
        internal static Vector2 GetPos()
        {
            if (Game.MapId == GameMapId.HowlingAbyss)
            {
                if (Positioning.GetAllyZone() != null)
                {
                    //define the path lists we'll use to store positions
                    var allyZonePathList =
                        Positioning.GetAllyZone().OrderBy(p => new Random(Environment.TickCount).Next()).FirstOrDefault();
                    var enemyZonePathList =
                        Positioning.GetEnemyZone().OrderBy(p => new Random(Environment.TickCount).Next()).FirstOrDefault();
                    //create empty vector2 lists, we'll add vectors to it after performing additional checks.
                    var allyZoneVectorList = new List<Vector2>();
                    var enemyZoneVectorList = new List<Vector2>();

                    //create vectors from points and remove walls.
                    foreach (var point in allyZonePathList)
                    {
                        var v2 = new Vector2(point.X, point.Y);
                        if (!v2.IsWall())
                        {
                            allyZoneVectorList.Add(v2);
                        }
                    }

                    var pointClosestToEnemyHQ =
                        allyZoneVectorList.OrderBy(p => p.Distance(HeadQuarters.EnemyHQ.Position)).FirstOrDefault();

                    //remove people that just respawned from the equation
                    foreach (var v2 in allyZoneVectorList)
                    {
                        if (v2.Distance(pointClosestToEnemyHQ) > 1500)
                        {
                            allyZoneVectorList.Remove(v2);
                        }
                    }

                    //return a random orbwalk pos candidate from the list
                    return allyZoneVectorList.FirstOrDefault();
                }
            }

            //for SR :s
            var minion = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsAlly).OrderByDescending(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
            var farthestTurretPos = ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsAlly).OrderByDescending(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault().Position.To2D();
            return (minion != null && minion.IsValid<Obj_AI_Minion>()) ? minion.Position.To2D() : farthestTurretPos;
        }   
    }

    public static class Positioning
    {
        /// <summary>
        /// Returns a list of points in the Ally Zone
        /// </summary>
        internal static Paths GetAllyZone()
        {
            var teamPolygons = new List<Geometry.Polygon>();
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(h => h.IsAlly && !h.IsDead && !h.IsMe && !(h.InFountain() || h.InShop())))
            {
                teamPolygons.Add(GetChampionRangeCircle(hero).ToPolygon());
            }
            var teamPaths = Geometry.ClipPolygons(teamPolygons);
            var newTeamPaths = teamPaths;
            foreach (var pathList in teamPaths)
            {
                Path wall = new Path();
                foreach (var path in pathList)
                {
                    if ((new Vector2(path.X, path.Y)).IsWall())
                    {
                        wall.Add(path);
                    }
                }
                newTeamPaths.Remove(wall);
            }
            return newTeamPaths;
        }

        /// <summary>
        /// Returns a list of points in the Enemy Zone
        /// </summary>
        internal static Paths GetEnemyZone()
        {
            var teamPolygons = new List<Geometry.Polygon>();
            foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().FindAll(h => !h.IsAlly && !h.IsDead && h.IsVisible))
            {
                teamPolygons.Add(GetChampionRangeCircle(hero).ToPolygon());
            }
            var teamPaths = Geometry.ClipPolygons(teamPolygons);
            var newTeamPaths = teamPaths;
            foreach (var pathList in teamPaths)
            {
                Path wall = new Path();
                foreach (var path in pathList)
                {
                    if (Utility.IsWall(new Vector2(path.X, path.Y)))
                    {
                        wall.Add(path);
                    }
                }
                newTeamPaths.Remove(wall);
            }
            return newTeamPaths;
        }

        /// <summary>
        /// Returns a circle with center at hero position and radius of the highest impact range a hero has.
        /// </summary>
        /// <param name="hero">The target hero.</param>
        internal static Geometry.Circle GetChampionRangeCircle(Obj_AI_Hero hero)
        {
            var heroSpells = new List<SpellData>
            {
                SpellData.GetSpellData(hero.GetSpell(SpellSlot.Q).Name),
                SpellData.GetSpellData(hero.GetSpell(SpellSlot.W).Name),
                SpellData.GetSpellData(hero.GetSpell(SpellSlot.E).Name)
            };
            var spellsOrderedByRange = heroSpells.OrderBy(s => s.CastRange);
            if (spellsOrderedByRange.FirstOrDefault() != null)
            {
                var highestSpellRange = spellsOrderedByRange.FirstOrDefault().CastRange;
                return new Geometry.Circle(
                    hero.ServerPosition.To2D(),
                    highestSpellRange > hero.AttackRange ? highestSpellRange : hero.AttackRange);
            }
            return new Geometry.Circle(hero.ServerPosition.To2D(), hero.AttackRange);
        }

        /// <summary>
        /// Returns a pathlist of the zone in which you will get exp, not sure if it will ever be used.
        /// </summary>
        internal static Paths ExpZone()
        {
            //define the minions
            var farthestAllyMinion = 
                ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsAlly).OrderByDescending(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
            var closestEnemyMinion =
                ObjectManager.Get<Obj_AI_Minion>().Where(m => !m.IsAlly).OrderBy(m => m.Distance(HeadQuarters.AllyHQ)).FirstOrDefault();

            //create a new empty path list to which we're going to add paths later on
            var paths = new Paths();

            //create a circle to of the exp range


            //if there's an enemy minion visible, we're going to use it
            if (closestEnemyMinion != null && closestEnemyMinion.IsVisible && !closestEnemyMinion.IsDead && closestEnemyMinion.IsValid<Obj_AI_Minion>())
            {
                var expRangeCircle = new Geometry.Circle(closestEnemyMinion.Position.To2D(), 1350);
                paths.Add(expRangeCircle.ToPolygon().ToClipperPath());

                //remove walls
                foreach (var path in paths)
                {
                    foreach (var point in path)
                    {
                        if (new Vector2(point.X, point.Y).IsWall())
                        {
                            path.Remove(point);
                        }
                    }
                    if (path.Count == 0)
                    {
                        paths.Remove(path);
                    }
                }
            }
            return paths;
        }

    }
}
