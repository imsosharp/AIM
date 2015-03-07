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


namespace AiM.Plugins
{
    internal class Default : AiMPlugin
    {
        public Default()
        {
            //Initializing spells
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 450);
            E = new Spell(SpellSlot.E, 200);
            R = new Spell(SpellSlot.R, 600);

            //Get SpellData for spells
            var q = SpellData.GetSpellData(ObjectManager.Player.GetSpell(SpellSlot.Q).Name);
            var w = SpellData.GetSpellData(ObjectManager.Player.GetSpell(SpellSlot.W).Name);
            var e = SpellData.GetSpellData(ObjectManager.Player.GetSpell(SpellSlot.E).Name);
            var r = SpellData.GetSpellData(ObjectManager.Player.GetSpell(SpellSlot.R).Name);

            //Set spells
            Q.SetSkillshot(q.SpellCastTime, q.LineWidth, q.MissileSpeed, true, SkillshotType.SkillshotLine);
            W.SetSkillshot(w.SpellCastTime, w.LineWidth, w.MissileSpeed, true, SkillshotType.SkillshotLine);
            E.SetTargetted(e.SpellCastTime, e.SpellCastTime);
            R.SetTargetted(r.SpellCastTime, r.SpellCastTime);

            //Menu
            ComboConfig.AddBool("ComboQ", "Use Q", true);
            ComboConfig.AddBool("ComboW", "Use W", true);
            ComboConfig.AddBool("ComboE", "Use E", true);
            ComboConfig.AddBool("ComboR", "Use R", true);
        }

        public override void OnGameUpdate(EventArgs args)
        {
            if (GetTarget(600, Q.DamageType) == null)
            {
                return;
            }
            if (Q.CastCheck().Tick() == BehaviorState.Success)
            {
                Q.Cast(GetTarget(Q.Range, Q.DamageType));
            } 
            if (W.CastCheck().Tick() == BehaviorState.Success)
            {
                W.Cast(GetTarget(W.Range, W.DamageType));
            } 
            if (E.CastCheck().Tick() == BehaviorState.Success)
            {
                E.Cast(GetTarget(W.Range, W.DamageType));
            } 
            if (R.CastCheck().Tick() == BehaviorState.Success)
            {
                R.Cast(GetTarget(W.Range, W.DamageType));
            }
        }
    }
}
