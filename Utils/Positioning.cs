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
using System.Linq.Expressions;
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

        internal static Vector2 TeamfightPosition { get; set; }
        internal static Vector2 ExpRangePosition { get; set; }


        /// <summary>
        /// Returns a random position in the team zone or the position of the ally champion farthest from base
        /// </summary>
        internal static void Update()
        {
            Positioning.Update();

            if (Wizard.GetClosestEnemyMinion() != null && Positioning.ExpZone != null)
            {
                var expPathList = new List<Vector2>();
                foreach (var pathList in Positioning.ExpZone)
                {
                    foreach (var path in pathList)
                    {
                        expPathList.Add(new Vector2(path.X, path.Y));
                    }
                }
                if (!expPathList.Any())
                {
                    return;
                }
                ExpRangePosition = expPathList.OrderBy(p => p.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
            }

            if (Game.MapId == GameMapId.HowlingAbyss && HeroManager.Allies.Count(h => !h.IsMe) >= 1)
                {
                    //define the path lists we'll use to store positions
                        var allyZonePathList =
                            Positioning.AllyZone.OrderBy(p => new Random(Environment.TickCount).Next()).FirstOrDefault();
                        var enemyZonePathList =
                            Positioning.EnemyZone.OrderBy(p => new Random(Environment.TickCount).Next()).FirstOrDefault();
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
                            allyZoneVectorList.OrderBy(v2 => v2.Distance(HeadQuarters.EnemyHQ.Position)).FirstOrDefault();

                        //remove people that just respawned from the point list
                        foreach (var v2 in allyZoneVectorList)
                        {
                            if (v2.Distance(pointClosestToEnemyHQ) > 1500)
                            {
                                allyZoneVectorList.Remove(v2);
                            }
                        }

                        //return a random orbwalk pos candidate from the list
                        TeamfightPosition = allyZoneVectorList.FirstOrDefault();

                        if (TeamfightPosition != null) {return;}
                }
            //for SR :s
            var minion = ObjectManager.Get<Obj_AI_Minion>().Where(m => m.IsAlly).OrderByDescending(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
            var farthestTurret =
                Turrets.AllyTurrets.OrderByDescending(t => t.Distance(HeadQuarters.AllyHQ))
                    .FirstOrDefault();
            if (farthestTurret == null) 
            { 
                Game.PrintChat("Couldn't find turrets in your game");
                return;
            }
            TeamfightPosition = (minion != null && minion.IsValid<Obj_AI_Minion>()) ? minion.Position.To2D() : farthestTurret.Position.To2D();
        }   
    }

    public static class Positioning
    {
        /// <summary>
        /// Returns a list of points in the Ally Zone
        /// </summary>
        public static Paths AllyZone { get; private set; }

        /// <summary>
        /// Returns a list of points in the Enemy Zone
        /// </summary>
        public static Paths EnemyZone { get; private set; }

        /// <summary>
        /// Returns a pathlist of the zone in which you will get exp, not sure if it will ever be used.
        /// </summary>
        public static Paths ExpZone { get; private set; }

        /// <summary>
        /// Updates positioning props
        /// </summary>
        internal static void Update()
        {
            #region Ally Zone
            var allyTeamPolygons = new List<Geometry.Polygon>();
            foreach (var hero in HeroManager.Allies.Where(h => !h.IsDead && !h.IsMe && !h.InFountain()))
            {
                allyTeamPolygons.Add(GetChampionRangeCircle(hero).ToPolygon());
            }
            AllyZone = Geometry.ClipPolygons(allyTeamPolygons);
            foreach (var pathList in AllyZone)
            {
                Path wall = new Path();
                foreach (var path in pathList)
                {
                    if ((new Vector2(path.X, path.Y)).IsWall())
                    {
                        wall.Add(path);
                    }
                }
                AllyZone.Remove(wall);
            }
            #endregion Ally Zone

            #region Enemy Zone
            
            var enemyTeamPolygons = new List<Geometry.Polygon>();
            foreach (var hero in HeroManager.Enemies.FindAll(h => !h.IsDead && h.IsVisible))
            {
                enemyTeamPolygons.Add(GetChampionRangeCircle(hero).ToPolygon());
            }
            EnemyZone = Geometry.ClipPolygons(enemyTeamPolygons);
            foreach (var pathList in EnemyZone)
            {
                Path wall = new Path();
                foreach (var path in pathList)
                {
                    if ((new Vector2(path.X, path.Y)).IsWall())
                    {
                        wall.Add(path);
                    }
                }
                EnemyZone.Remove(wall);
            }
            #endregion Enemy Zone

            #region ExpZone
            if (Wizard.GetClosestEnemyMinion() != null && Wizard.GetClosestEnemyMinion().IsVisible && !Wizard.GetClosestEnemyMinion().IsDead && Wizard.GetClosestEnemyMinion().IsValid<Obj_AI_Minion>())
            {
                var expRangeCircle = new Geometry.Circle(Wizard.GetClosestEnemyMinion().ServerPosition.To2D(), 1350);
                ExpZone.Add(expRangeCircle.ToPolygon().ToClipperPath());

                //remove walls
                foreach (var path in ExpZone)
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
                        ExpZone.Remove(path);
                    }
                }
                ExpZone = ExpZone;
            }
            #endregion ExpZone
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
    }
}
