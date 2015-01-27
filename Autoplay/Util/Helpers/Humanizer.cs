﻿#region

using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

#endregion

namespace AIM.Autoplay.Util.Helpers
{
    public class Humanizer
    {
        private static Menu _menu;
        public Humanizer(Menu menu)
        {
            _menu = menu;
            Load();
        }
        public static float LastMove;
        public static Obj_AI_Base Player = ObjectManager.Player;
        public static List<String> SpellList = new List<string> { "Q", "W", "E", "R" };
        public static List<float> LastCast = new List<float>();


        private static void Load()
        {

            _menu.AddSubMenu(new Menu("Humanizer", "humanizer"));

            _menu.SubMenu("Humanizer").AddSubMenu(new Menu("Health", "Health"));

            var spells = _menu.SubMenu("humanizer").AddSubMenu(new Menu("Spells", "Spells"));

            for (var i = 0; i < 3; i++)
            {
                LastCast[i] = 0;
                var spell = SpellList[i];
                var menu = spells.AddSubMenu(new Menu(spell, spell));
                menu.AddItem(new MenuItem("Enabled" + i, "Delay " + spell, true).SetValue(true));
                menu.AddItem(new MenuItem("Delay" + i, "Cast Delay", true).SetValue(new Slider(80, 0, 400)));
            }

            var move = _menu.SubMenu("humanizer").AddSubMenu(new Menu("Movement", "Movement"));
            move.AddItem(new MenuItem("MovementEnabled", "Enabled").SetValue(true));
            move.AddItem(new MenuItem("MovementDelay", "Movement Delay")).SetValue(new Slider(80, 0, 400));

            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (sender == null || !sender.Owner.IsMe || _menu.Item("Enabled" + (int)args.Slot).GetValue<bool>())
            {
                return;
            }

            var delay = _menu.Item("Delay" + (int)args.Slot).GetValue<Slider>().Value;

            if (Environment.TickCount - LastCast[(int)args.Slot] < delay)
            {
                args.Process = false;
                return;
            }

            LastCast[(int)args.Slot] = Environment.TickCount;
        }

        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.IsMe || args.Order != GameObjectOrder.MoveTo ||
                !_menu.Item("MovementEnabled").GetValue<bool>())
            {
                return;
            }

            if (Environment.TickCount - LastMove < _menu.Item("MovementDelay").GetValue<Slider>().Value)
            {
                args.Process = false;
                return;
            }

            LastMove = Environment.TickCount;
        }
    }
}