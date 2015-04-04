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
            try
            {
                //Get SpellData for spells
                var q = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.Q).Name);
                var w = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.W).Name);
                var e = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.E).Name);
                var r = SpellData.GetSpellData(ObjectHandler.Player.GetSpell(SpellSlot.R).Name);

                //Initializing spells
                Q = new Spell(SpellSlot.Q, q.CastRange);
                W = new Spell(SpellSlot.W, w.CastRange);
                E = new Spell(SpellSlot.E, e.CastRange);
                R = new Spell(SpellSlot.R, e.CastRange);

                List<Data.SpellData> MySkillShots = Data.SpellDatabase.Spells.FindAll(s => s.ChampionName == ObjectHandler.Player.ChampionName);
                for (var i = 0; i < MySkillShots.Count(); i++)
                {
                    var ss = MySkillShots[i];
                    if (ss.Slot == SpellSlot.Q)
                    {
                        Q.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                        break;
                    }
                    if (ss.Slot == SpellSlot.W)
                    {
                        W.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                        break;
                    }
                    if (ss.Slot == SpellSlot.E)
                    {
                        E.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                        break;
                    }
                    if (ss.Slot == SpellSlot.R)
                    {
                        R.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                        break;
                    }
                }
                //Set spells
                if (!Q.IsSkillshot)
                {
                    Q.SetTargetted(q.DelayTotalTimePercent, q.SpellCastTime);
                }
                if (!W.IsSkillshot)
                {
                    W.SetTargetted(w.DelayTotalTimePercent, q.SpellCastTime);
                }
                if (!E.IsSkillshot)
                {
                    E.SetTargetted(e.DelayTotalTimePercent, e.SpellCastTime);
                }
                if (!R.IsSkillshot)
                {
                    R.SetTargetted(r.DelayTotalTimePercent, r.SpellCastTime);
                }

                Spells.Add(Q);
                Spells.Add(W);
                Spells.Add(E);
                Spells.Add(R);

                ComboConfig.AddBool("ComboQ", "Use Q", true);
                ComboConfig.AddBool("ComboW", "Use W", true);
                ComboConfig.AddBool("ComboE", "Use E", true);
                ComboConfig.AddBool("ComboR", "Use R", true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public override void OnGameUpdate(EventArgs args)
        {
            if (Heroes.EnemyHeroes.Count() == 0)
            {
                return;
            }
                if (TestedSpells.Count() != AvailableSpells.Count()
                    || AvailableSpells.Count() != Spells.Count())
                {
                    IndexSpells();
                }
                //pure SBTW logic right here
                foreach (var spell in Spells)
                {
                    var allyTarget = Heroes.AllyHeroes.OrderBy(h => h.Distance(ObjectHandler.Player.Position)).FirstOrDefault();
                    var target = GetTarget(spell.Range, TargetSelector.DamageType.Magical);
                    if (!spell.IsReady() || target == null)
                    {
                        return;
                    }
                    if (spell.IsSkillshot)
                    {
                        spell.Cast(target);
                        return;
                    }
                    if (CastableOnAllies.Contains(spell) && allyTarget != null)
                    {
                        spell.CastOnUnit(allyTarget);
                        return;
                    }
                    if (SelfCastable.Contains(spell))
                    {
                        spell.CastOnUnit(ObjectHandler.Player);
                        return;
                    }
                    spell.CastOnUnit(GetTarget(spell.Range, TargetSelector.DamageType.Magical));
                }
        }

        #region Index Spells To See If They Should Be Casted On Allies Or Enemies
        public static void IndexSpells()
        {
            
            if (Heroes.AllyHeroes.Count() == 0 || Heroes.EnemyHeroes.Count() == 0)
            {
                return;
            }
            //Store all available spells in a list
            if (AvailableSpells.Count() != Spells.Count())
            {
                foreach (var spell in Spells)
                {
                    if (AvailableSpells.Contains(spell))
                    { 
                        return;
                    }
                    if (spell.Level != 0)
                    {
                        AvailableSpells.Add(spell);
                        if (spell.IsSkillshot)
                        {
                            TestedSpells.Add(spell);
                        }
                    }
                }
            }
            //Test if it can be cast on self/ally
            foreach(var spell in AvailableSpells)
            {
                var allyTarget = Heroes.AllyHeroes.OrderBy(h => h.Distance(ObjectHandler.Player.Position)).FirstOrDefault();
                if (TestedSpells.Contains(spell))
                {
                    return;
                }
                if (spell.IsReady() && allyTarget != null)
                {
                    spell.CastOnUnit(allyTarget);
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
