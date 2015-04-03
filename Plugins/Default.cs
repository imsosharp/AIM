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
using Data = AiM.Utils.Data;
#endregion AiM License


namespace AiM.Plugins
{
    internal class Default : AiMPlugin
    {
        public Default()
        {
            //Initializing spells
            List<Data.SpellData> MySkillShots = new List<Data.SpellData>();
            MySkillShots = Data.SpellDatabase.Spells.FindAll(s => s.ChampionName == ObjectHandler.Player.ChampionName);
            foreach(var ss in MySkillShots)
            {
                if (ss.Slot == SpellSlot.Q)
                {
                    Q = new Spell(SpellSlot.Q, ss.Range);
                    Q.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                    return;
                } 
                if (ss.Slot == SpellSlot.W)
                {
                    W = new Spell(SpellSlot.Q, ss.Range);
                    W.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                    return;
                }
                if (ss.Slot == SpellSlot.E)
                {
                    E = new Spell(SpellSlot.Q, ss.Range);
                    E.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                    return;
                }
                if (ss.Slot == SpellSlot.R)
                {
                    R = new Spell(SpellSlot.Q, ss.Range);
                    R.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                }
            }

            //Get SpellData for spells
            var q = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.Q).Name);
            var w = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.W).Name);
            var e = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.E).Name);
            var r = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.R).Name);

            //Set spells
            if (Q == null || !Q.IsSkillshot)
            {
                Q = new Spell(SpellSlot.Q, q.CastRange);
                Q.SetTargetted(q.DelayTotalTimePercent, q.SpellCastTime);
            }
            if (W == null || !W.IsSkillshot)
            {
                W = new Spell(SpellSlot.W, w.CastRange);
                W.SetTargetted(w.DelayTotalTimePercent, q.SpellCastTime);
            }
            if (E == null || !E.IsSkillshot)
            {
                E = new Spell(SpellSlot.E, e.CastRange);
                E.SetTargetted(e.DelayTotalTimePercent, e.SpellCastTime);
            }
            if(R == null || !R.IsSkillshot)
            {
                R = new Spell(SpellSlot.R, r.CastRange);
                R.SetTargetted(r.DelayTotalTimePercent, r.SpellCastTime);
            }

            Spells.Add(SpellSlot.Q, Q);
            Spells.Add(SpellSlot.W, W);
            Spells.Add(SpellSlot.E, E);
            Spells.Add(SpellSlot.R, R);

            //Menu
            ComboConfig.AddBool("ComboQ", "Use Q", true);
            ComboConfig.AddBool("ComboW", "Use W", true);
            ComboConfig.AddBool("ComboE", "Use E", true);
            ComboConfig.AddBool("ComboR", "Use R", true);
        }

        public override void OnGameUpdate(EventArgs args)
        {
            if (PreventCodeFromExecuting)
            { 
                return;
            }
            try
            {
                if (TestedSpells.Count() != AvailableSpells.Count()
                    || AvailableSpells.Count() != Spells.Count())
                {
                    IndexSpells();
                }
                //pure SBTW logic right here
                foreach (var spell in Spells)
                {
                    if (!spell.Key.IsReady())
                    {
                        return;
                    }
                    if (spell.Value.IsSkillshot)
                    {
                        //what is this? XD
                        spell.Value.CastOnBestTarget();
                        if (spell.Key.IsReady())
                            spell.Value.Cast(GetTarget(spell.Value.Range, TargetSelector.DamageType.Magical));
                        return;
                    }
                    if (CastableOnAllies.Contains(spell.Value))
                    {
                        spell.Value.CastOnUnit(Heroes.AllyHeroes.OrderBy(h => h.Distance(ObjectHandler.Player.Position)).FirstOrDefault());
                        return;
                    }
                    if (SelfCastable.Contains(spell.Value))
                    {
                        spell.Value.CastOnUnit(ObjectHandler.Player);
                        return;
                    }
                    spell.Value.CastOnUnit(GetTarget(spell.Value.Range, TargetSelector.DamageType.Magical));
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                PreventCodeFromExecuting = true;
            }
        }

        #region Index Spells To See If They Should Be Casted On Allies Or Enemies
        public static void IndexSpells()
        {
            
            if (Heroes.AllyHeroes.Count() == 0)
            {
                return;
            }
            //Store all available spells in a list
            if (AvailableSpells.Count() != Spells.Count())
            {
                foreach (var spell in Spells)
                {
                    if (AvailableSpells.Contains(spell.Value))
                    { 
                        return;
                    }
                    if (spell.Value.Level != 0)
                    {
                        AvailableSpells.Add(spell.Value);
                        if (spell.Value.IsSkillshot)
                        {
                            TestedSpells.Add(spell.Value);
                        }
                    }
                }
            }
            //Test if it can be cast on self/ally
            foreach(var spell in AvailableSpells)
            {
                if (TestedSpells.Contains(spell))
                {
                    return;
                }
                if (spell.IsReady())
                {
                    spell.CastOnUnit(Heroes.AllyHeroes.OrderBy(h => h.Distance(ObjectHandler.Player.Position)).FirstOrDefault());
                }
                if (!spell.IsReady())
                {
                    CastableOnAllies.Add(spell);
                    TestedSpells.Add(spell);
                }
                if (spell.IsReady())
                {
                    spell.CastOnUnit(ObjectHandler.Player);
                }
                if (!spell.IsReady())
                {
                    SelfCastable.Add(spell);
                }
                TestedSpells.Add(spell);
            }
#endregion

        }
    }
}
