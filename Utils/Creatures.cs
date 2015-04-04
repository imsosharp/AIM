#region AiM License
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

        public static void Load()
        {
            Heroes.Load();
            GameObject.OnCreate += OnCreate;
            GameObject.OnDelete += OnDelete;
        }

        #region GameObject Events subscribed to determine when to update cached minions
        private static void OnCreate(GameObject sender, EventArgs args)
        {
            if (sender is Obj_AI_Minion)
            {
                Minions.Update();
            }
            if (AiMPlugin.Config.Item("AvoidEnabled").GetValue<bool>() && sender.IsAvoidable())
            {
                GameObjects.AvoidableObjects.Add(sender, sender.Position);
            }
        }
        private static void OnDelete(GameObject sender, EventArgs args)
        {
            if (sender is Obj_AI_Minion)
            {
                Minions.Update();
            }
            if (AiMPlugin.Config.Item("AvoidEnabled").GetValue<bool>() && GameObjects.AvoidableObjects.Any(o => o.Key == sender))
            {
                GameObjects.AvoidableObjects.Remove(sender);
            }
        }
        #endregion
    }

    public static class GameObjects
    {
        public static Dictionary<GameObject, Vector3> AvoidableObjects = new Dictionary<GameObject, Vector3>();
    }

    public static class Minions
    {
        public static List<Obj_AI_Minion> AllyMinions = new List<Obj_AI_Minion>();
        public static List<Obj_AI_Minion> EnemyMinions = new List<Obj_AI_Minion>();

        public static void Update()
        {
            AllyMinions.Clear();
            AllyMinions = ObjectHandler.Get<Obj_AI_Minion>().FindAll(m => m.IsAlly && !m.IsDead && !m.IsHeroPet());

            EnemyMinions.Clear();
            EnemyMinions = ObjectHandler.Get<Obj_AI_Minion>().FindAll(m => m.IsValidTarget() && !m.IsDead && !m.IsHeroPet());
        }
    }

    public static class Heroes
    {
        public static Obj_AI_Hero Me { get; set; }
        public static List<Obj_AI_Hero> AllyHeroes = new List<Obj_AI_Hero>();
        public static List<Obj_AI_Hero> EnemyHeroes = new List<Obj_AI_Hero>();

        public static void Load()
        {
            Me = ObjectHandler.Player;
            AllyHeroes = ObjectHandler.Get<Obj_AI_Hero>().FindAll(h => h.IsAlly);
            EnemyHeroes = ObjectHandler.Get<Obj_AI_Hero>().FindAll(h => !h.IsAlly);
        }
    }
}
