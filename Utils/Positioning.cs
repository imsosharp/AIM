﻿#region AiM License
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
#endregion AiM License

namespace AiM.Utils
{
    public static class EasyPositioning
    {
        private static int LastUpdate = 0;
        public static Vector3 TeamfightPosition { get; private set; }
        public static Vector3 ExpRangePosition { get; private set; }


        /// <summary>
        /// Returns a random position in the team zone or the position of the ally champion farthest from base
        /// </summary>
        internal static void Update()
        {
            if (Environment.TickCount - LastUpdate >= 500)
            {
                LastUpdate = Environment.TickCount;
                Positioning.Update();

                ExpRangePosition =
                    Positioning.ExpZone.OrderBy(p => p.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault().RandomizePosition();

                if (Game.MapId == GameMapId.HowlingAbyss && HeroManager.Allies.Count(h => !h.IsMe) >= 1)
                {
                    var pointClosestToEnemyHQ =
                                Positioning.AllyZone.OrderBy(v2 => v2.Distance(HeadQuarters.EnemyHQ.Position)).FirstOrDefault();
                    var positioningCandidates = new List<Vector2>();
                    //remove people that just respawned from the point list
                    foreach (var v2 in Positioning.AllyZone)
                    {
                        if (!(v2.Distance(pointClosestToEnemyHQ) > 1500))
                        {
                            positioningCandidates.Add(v2);
                        }
                    }

                    //return a random orbwalk pos candidate from the list
                    TeamfightPosition = positioningCandidates
                        .OrderBy(p => new Random(Environment.TickCount).Next())
                            .FirstOrDefault()
                                .RandomizePosition();

                    if (TeamfightPosition.IsValid()) { return; }
                }
                //for SR :s
                var minion = ObjectHandler.Get<Obj_AI_Minion>().Where(m => m.IsAlly).OrderByDescending(m => m.Distance(HeadQuarters.AllyHQ.Position)).FirstOrDefault();
                var farthestTurret =
                    Turrets.AllyTurrets.OrderByDescending(t => t.Distance(HeadQuarters.AllyHQ))
                        .FirstOrDefault();
                TeamfightPosition = (minion != null && minion.IsValid<Obj_AI_Minion>()) ? minion.Position.RandomizePosition() : farthestTurret.Position.RandomizePosition();
            }
        }
    }

    public static class Positioning
    {
        /// <summary>
        /// Returns a list of points in the Ally Zone
        /// </summary>
        public static List<Vector2> AllyZone = new List<Vector2>();

        /// <summary>
        /// Returns a list of points in the Enemy Zone
        /// </summary>
        public static List<Vector2> EnemyZone = new List<Vector2>();

        /// <summary>
        /// Returns a pathlist of the zone in which you will get exp, not sure if it will ever be used.
        /// </summary>
        public static List<Vector2> ExpZone = new List<Vector2>();

        /// <summary>
        /// Updates positioning props
        /// </summary>
        internal static void Update()
        {
            #region Ally Zone
            AllyZone.Clear();
            //advanced algorithms
            var allyZonePaths = Geometry.ClipPolygons(HeroManager.Allies.Where(h => !h.IsDead && !h.IsMe && !h.InFountain()).Select(hero => GetChampionRangeCircle(hero).ToPolygon()).ToList());

            //create v2 from paths, if it isn't a wall
            foreach (var pathList in allyZonePaths)
            {
                foreach (var path in pathList)
                {
                    var v2 = new Vector2(path.X, path.Y);
                    if (!v2.IsWall())
                        AllyZone.Add(v2);
                }
            }
            #endregion Ally Zone

            #region Enemy Zone
            EnemyZone.Clear();
            //advanced algorithms
            var enemyZonePaths = Geometry.ClipPolygons(HeroManager.Enemies.FindAll(h => !h.IsDead && h.IsVisible).Select(hero => GetChampionRangeCircle(hero).ToPolygon()).ToList());

            //create v2 from paths, if it isn't a wall
            foreach (var pathList in enemyZonePaths)
            {
                foreach (var path in pathList)
                {
                    var v2 = new Vector2(path.X, path.Y);
                    if (!v2.IsWall())
                        EnemyZone.Add(v2);
                }
            }
            #endregion Enemy Zone

            #region ExpZone
            ExpZone.Clear();
            //update only if enemy minion exists, if not, keep old values.
            if (Wizard.GetClosestEnemyMinion() != null && Wizard.GetClosestEnemyMinion().IsVisible && !Wizard.GetClosestEnemyMinion().IsDead && Wizard.GetClosestEnemyMinion().IsValid<Obj_AI_Minion>())
            {
                //advanced algorithms
                var expZonePaths = (new Geometry.Circle(Wizard.GetClosestEnemyMinion().Position.To2D(), 1350)).ToPolygon().ToClipperPath();

                //remove walls
                foreach (var path in expZonePaths)
                {
                        var v2 = new Vector2(path.X, path.Y);
                        if (!v2.IsWall())
                            ExpZone.Add(v2);
                }
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
                LeagueSharp.SpellData.GetSpellData(hero.GetSpell(SpellSlot.Q).Name),
                LeagueSharp.SpellData.GetSpellData(hero.GetSpell(SpellSlot.W).Name),
                LeagueSharp.SpellData.GetSpellData(hero.GetSpell(SpellSlot.E).Name)
            };
            var spellsOrderedByRange = heroSpells.OrderBy(s => s.CastRange);
            if (spellsOrderedByRange.FirstOrDefault() != null)
            {
                var highestSpellRange = spellsOrderedByRange.FirstOrDefault().CastRange;
                return new Geometry.Circle(
                    hero.Position.To2D(),
                    highestSpellRange > hero.AttackRange ? highestSpellRange : hero.AttackRange);
            }
            return new Geometry.Circle(hero.Position.To2D(), hero.AttackRange);
        }
    }
}
