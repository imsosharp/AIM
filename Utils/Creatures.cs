﻿#region AiM License
// Copyright 2015 LeagueSharp
// Creatures.cs is part of AiM.
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
    public static class Creatures
    {
        private static int LastUpdate = 0;
        public static void UpdateAll()
        {
            if (Environment.TickCount - LastUpdate >= 500)
            {
                Minions.Update();
                Heroes.Update();
                LastUpdate = Environment.TickCount;
            }
        }
    }

    public static class Minions
    {
        public static List<Obj_AI_Base> AllyMinions { get; private set; }
        public static List<Obj_AI_Base> EnemyMinions { get; private set; }
        public static List<Obj_AI_Base> ClosestAllyMinions { get; private set; }

        public static void Update()
        {
            AllyMinions = MinionManager.GetMinions(int.MaxValue).FindAll(m => m.IsAlly && !m.IsDead && !m.IsHeroPet());
            EnemyMinions = MinionManager.GetMinions(int.MaxValue).FindAll(m => m.IsValidTarget() && !m.IsDead && !m.IsHeroPet());
            ClosestAllyMinions = MinionManager.GetMinions(2000).FindAll(m => m.IsAlly && !m.IsDead && !m.IsHeroPet());
        }
    }

    public static class Heroes
    {
        public static List<Obj_AI_Hero> AllyHeroes { get; private set; }
        public static List<Obj_AI_Hero> EnemyHeroes { get; private set; }

        public static void Update()
        {
            AllyHeroes = HeroManager.Allies;
            EnemyHeroes = HeroManager.Enemies;
        }
    }
}