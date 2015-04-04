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
        internal static List<Spell> AvailableSpells = new List<Spell>();
        internal static List<Spell> TestedSpellsOnSelf = new List<Spell>();
        internal static List<Spell> TestedSpellsOnAllies = new List<Spell>();
        internal static List<Spell> SpellsCastableOnSelf = new List<Spell>();
        internal static List<Spell> SpellsCastableOnAllies = new List<Spell>();
        internal static List<Spell> SpellsCastableOnEnemies = new List<Spell>();

        public Default()
        {
            //initializing spells
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);

            //add them to a list to process them later on for voodoo magic
            Spells.Add(Q);
            Spells.Add(W);
            Spells.Add(E);
            Spells.Add(R);

            //find skillshots in evade spelldb
            List<Data.SpellData> MySkillShots =
                Data.SpellDatabase.Spells.FindAll(s => s.ChampionName == ObjectHandler.Player.ChampionName);

            //set skillshots
            foreach (var ss in MySkillShots)
            {
                var theSpell = Spells.First(s => s.Slot == ss.Slot);
                if (theSpell != null)
                {
                    theSpell.Range = ss.Range;
                    theSpell.SetSkillshot(ss.Delay, ss.RawRadius, ss.MissileSpeed, true, ss.Type);
                }
            }

            //spell is not a skillshot? make it targettable.
            foreach (var spell in Spells)
            {
                var sd = SpellData.GetSpellData(ObjectManager.Player.GetSpell(spell.Slot).Name);
                if (!spell.IsSkillshot && sd != null)
                {
                    spell.Range = sd.CastRange;
                    spell.SetTargetted(sd.DelayTotalTimePercent, sd.SpellCastTime);
                }
            }

            //initialize combo menu
            ComboConfig.AddBool("ComboQ", "Use Q", true);
            ComboConfig.AddBool("ComboW", "Use W", true);
            ComboConfig.AddBool("ComboE", "Use E", true);
            ComboConfig.AddBool("ComboR", "Use R", true);
        }

        public override void OnGameUpdate(EventArgs args)
        {
            if (!Heroes.EnemyHeroes.Any())
            {
                return;
            }
            if (TestedSpellsOnSelf.Count() != AvailableSpells.Count()
                || TestedSpellsOnAllies.Count() != AvailableSpells.Count()
                || AvailableSpells.Count() != Spells.Count())
            {
                IndexSpells();
            }
            //pure SBTW logic right here
            foreach (var spell in Spells)
            {
                if (!spell.IsReady())
                {
                    break;
                }

                #region Aggressive Spells
                var target = GetTarget(spell.Range, TargetSelector.DamageType.Magical);

                if (spell.IsSkillshot && target != null)
                {
                    spell.Cast(target);
                    break;
                }

                if (SpellsCastableOnEnemies.Contains(spell) && target != null)
                {
                    spell.CastOnUnit(target);
                    break;
                }
                #endregion

                #region Defensive Spells
                var allyTarget =
                    Heroes.AllyHeroes.OrderBy(h => h.Distance(ObjectHandler.Player.Position)).FirstOrDefault();

                if (SpellsCastableOnAllies.Contains(spell) && allyTarget != null)
                {
                    spell.CastOnUnit(allyTarget);
                    break;
                }
                if (SpellsCastableOnSelf.Contains(spell))
                {
                    spell.CastOnUnit(ObjectHandler.Player);
                    break;
                }
                #endregion
            }
        }

        #region Advanced and patented algorithms that tests a spell
        public static void IndexSpells()
        {
            
            if (!Heroes.AllyHeroes.Any() || !Heroes.EnemyHeroes.Any())
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
                        break;
                    }
                    if (spell.Level != 0)
                    {
                        AvailableSpells.Add(spell);
                        //if they're skillshots, we arleady know how to use them
                        if (spell.IsSkillshot)
                        {
                            TestedSpellsOnAllies.Add(spell);
                            TestedSpellsOnSelf.Add(spell);
                        }
                    }
                }
            }
            //Test if it can be cast on self/ally
            foreach(var spell in AvailableSpells)
            {
                //if we arleady tested the spell, skip it
                if (TestedSpellsOnAllies.Contains(spell) && TestedSpellsOnSelf.Contains(spell))
                {
                    break;
                }

                //our lab rat
                var allyTarget = Heroes.AllyHeroes.OrderBy(h => h.Distance(ObjectHandler.Player.Position)).FirstOrDefault();

                //test if we have to pet the rat
                if (!SpellsCastableOnAllies.Contains(spell))
                {
                    //pet the lab rat
                    if (spell.IsReady() && allyTarget != null)
                    {
                        spell.CastOnUnit(allyTarget);
                    }

                    //lab rat has been successfuly tested
                    if (!spell.IsReady())
                    {
                        SpellsCastableOnAllies.Add(spell);
                    }
                    TestedSpellsOnAllies.Add(spell);
                }

                //lets see what else we can do with this spell, maybe it can gib me wings like redbull :DDDD
                if (!TestedSpellsOnSelf.Contains(spell))
                {
                    //try to cast it
                    if (spell.IsReady())
                    {
                        spell.CastOnUnit(ObjectHandler.Player);
                    }
                    //it did go on cooldown so I can use it, cool
                    if (!spell.IsReady())
                    {
                        SpellsCastableOnSelf.Add(spell);
                    }
                    TestedSpellsOnSelf.Add(spell);
                }

                //now we know
                if (TestedSpellsOnAllies.Contains(spell) && TestedSpellsOnSelf.Contains(spell))
                {
                    if (!SpellsCastableOnAllies.Contains(spell) && !SpellsCastableOnSelf.Contains(spell))
                    {
                        SpellsCastableOnEnemies.Add(spell);
                    }
                }
            }
#endregion

        }
    }
}
