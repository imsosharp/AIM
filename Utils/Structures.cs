#region AiM License
// Copyright 2015 LeagueSharp
// Structures.cs is part of AiM.
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
    /// <summary>
    /// Class that helps managing all structures
    /// </summary>
    public static class Structures
    {
        /// <summary>
        /// A function to update all structures, called prefferably onupdate
        /// </summary>
        public static void UpdateAll()
        {
            HeadQuarters.Update();
            Turrets.Update();
        }
    }

    /// <summary>
    /// A class containing ally and enemy HQ
    /// </summary>
    public static class HeadQuarters
    {
        /// <summary>
        /// Returns Ally HQ
        /// </summary>
        public static Obj_HQ AllyHQ { get; private set; }

        /// <summary>
        /// Returns Enemy HQ
        /// </summary>
        public static Obj_HQ EnemyHQ { get; private set; }

        /// <summary>
        /// A function used to update HQs
        /// </summary>
        public static void Update()
        {
            AllyHQ = ObjectManager.Get<Obj_HQ>().FirstOrDefault(hq => hq.IsAlly);
            EnemyHQ = ObjectManager.Get<Obj_HQ>().FirstOrDefault(hq => !hq.IsAlly);
        }
    }

    /// <summary>
    /// A class containing ally and enemy turrets
    /// </summary>
    public static class Turrets
    {
        /// <summary>
        /// Returns Ally Turrets
        /// </summary>
        public static List<Obj_AI_Turret> AllyTurrets { get; private set; }

        /// <summary>
        /// Returns Enemy Turrets
        /// </summary>
        public static List<Obj_AI_Turret> EnemyTurrets { get; private set; }

        /// <summary>
        /// A function used to update all turrets
        /// </summary>
        public static void Update()
        {
            AllyTurrets = ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsAlly).ToList();
            EnemyTurrets = ObjectManager.Get<Obj_AI_Turret>().Where(t => !t.IsAlly).ToList();
        }
    }
}
