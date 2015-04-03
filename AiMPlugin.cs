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

namespace AiM
{
    internal abstract class AiMPlugin
    {
        protected AiMPlugin()
        {
            //initialize menu
            CreateMenu();
            //initialize events
            CustomEvents.Game.OnGameLoad += OnGameLoad;
            Game.OnUpdate += OnGameUpdate;
            Interrupter2.OnInterruptableTarget += OnPossibleToInterrupt;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpellCast;
            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;
        }

        public static Obj_AI_Hero Player = ObjectHandler.Player;
        public static string ChampionName = Player.BaseSkinName;
        public static int LastMove { get; protected set; }

        #region Menu
        internal static Menu Config;
        internal static Menu ComboConfig;
        internal static Menu LaningConfig;
        internal static Orbwalking.Orbwalker Orbwalker;

        public static TargetSelector Ts;

        public static Obj_AI_Base GetTarget(float range, TargetSelector.DamageType damageType)
        {
            return TargetSelector.GetTarget(range, damageType);
        }

        public static Obj_AI_Base GetMeleeTarget()
        {
            var dmgtype = Player.TotalAttackDamage() > Player.TotalMagicalDamage() ? (int)TargetSelector.DamageType.Physical : (int)TargetSelector.DamageType.Magical;
            return TargetSelector.GetTarget(Player.AttackRange, ((TargetSelector.DamageType)dmgtype));
        }

        public static string MenuName = "aim.menu." + ChampionName;

        internal void CreateMenu()
        {
            //root menu
            Config = new Menu("AiM: " + Player.ChampionName, MenuName, true);
            //Humanizer
            var move = Config.AddSubMenu(new Menu("Humanizer", "humanizer"));
            move.AddItem(new MenuItem("MovementEnabled", "Enabled").SetValue(true));
            move.AddItem(new MenuItem("MovementDelay", "Movement Delay")).SetValue(new Slider(400, 0, 1000));
            //Orbwalker
            Config.AddSubMenu(new Menu("Orbwalking", "orbwalkingmenu"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("orbwalkingmenu"));
            //TargetSelector
            TargetSelector.AddToMenu(Config.AddSubMenu(new Menu("Target Selector", "tsmenu")));
            ComboConfig = Config.AddSubMenu(new Menu("Combo", "combomenu"));
            LaningConfig = Config.AddSubMenu(new Menu("Laning", "laningmenu"));
            Config.AddToMainMenu();
        }
        #endregion Menu
        #region Spells
        internal static Spell Q;
        internal static Spell W;
        internal static Spell E;
        internal static Spell R;

        internal static Dictionary<SpellSlot, Spell> Spells = new Dictionary<SpellSlot, Spell>();
        public static IEnumerable<SpellDataInst> MainSpells
        {
            get { return Player.Spellbook.Spells.Where(spell => spell.Slot <= SpellSlot.R); }
        } 
        #endregion Spells
        #region Events

        public virtual void OnGameUpdate(EventArgs args)
        {
            
        }

        public virtual void OnGameLoad(EventArgs args)
        {
            
        }

        internal virtual void OnPossibleToInterrupt(Obj_AI_Hero hero, Interrupter2.InterruptableTargetEventArgs args)
        {
            
        }

        internal virtual void OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender is Obj_AI_Minion || sender is Obj_AI_Turret && args.Target.IsMe)
            {
                var closestAllyMinion = Minions.ClosestAllyMinions.OrderBy(m => new Random().Next()).FirstOrDefault();
                if (closestAllyMinion == null || !closestAllyMinion.IsValid) { return; }
                ObjectHandler.Player.IssueOrder(GameObjectOrder.MoveTo, closestAllyMinion.Position);
            }
        }

        //Used by Humanizer and checks for not attacking target under turret
        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.IsMe)
            {
                return;
            }
            if (args.Order == GameObjectOrder.MoveTo)
            {
                if (ObjectHandler.Player.HasBuff("SionR"))
                {
                    args.Process = false;
                    return;
                }
                if (args.TargetPosition.UnderTurret(true) && args.TargetPosition.CountNearbyAllyMinions(800) <= 2)
                {
                    args.Process = false;
                    return;
                }

                if (Environment.TickCount - LastMove < Config.Item("MovementDelay").GetValue<Slider>().Value &&
                    Config.Item("MovementEnabled").GetValue<bool>())
                {
                    args.Process = false;
                    return;
                }
                if (args.TargetPosition.GetClosestEnemyTurret().Distance(args.TargetPosition) < 800 && args.TargetPosition.GetClosestEnemyTurret().CountNearbyAllyMinions(800) <= 2)
                {
                    args.Process = false;
                    return;
                }
                LastMove = Environment.TickCount;
            }

            if (args.Target == null)
            {
                return;
            }
            if (args.Target.IsEnemy && args.Target is Obj_AI_Hero && sender.UnderTurret(true) && (args.Order == GameObjectOrder.AutoAttack || args.Order == GameObjectOrder.AttackUnit))
            {
                args.Process = false;
            }
        }
        #endregion Events
    }
}
