using System;
using System.Linq;
using AIM.Autoplay.Util;
using AIM.Autoplay.Util.Data;
using AIM.Autoplay.Util.Helpers;
using AIM.Autoplay.Util.Objects;
using LeagueSharp;
using LeagueSharp.Common;
using Orbwalking = AIM.Autoplay.Util.Orbwalking;

namespace AIM.Autoplay.Modes
{
    public abstract class Base
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;
        public static int LastMove;
        public static Menu Menu;
        public static Menu Orbwalker;
        public static Obj_AI_Minion LeadingMinion;
        public static Obj_AI_Minion ClosestEnemyMinion;

        protected Base()
        {
            ObjConstants = new Constants();
            ObjHeroes = new Heroes();
            ObjMinions = new Minions();
            ObjTurrets = new Turrets();
            Menu = new Menu("AIM", "AIM", true);
            ObjHQ = new HQ();
            Orbwalker = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            OrbW = new Orbwalking.Orbwalker(Orbwalker);

            Obj_AI_Base.OnIssueOrder += Obj_AI_Base_OnIssueOrder;

        }

        public static Constants ObjConstants { get; protected set; }
        public static Heroes ObjHeroes { get; protected set; }
        public static Minions ObjMinions { get; protected set; }
        public static Turrets ObjTurrets { get; protected set; }
        public static HQ ObjHQ { get; protected set; }
        public static Orbwalking.Orbwalker OrbW { get; set; }
        public virtual void OnGameLoad(EventArgs args) { }
        public virtual void OnGameUpdate(EventArgs args) { }

        #region Minions

        private static void Obj_AI_Base_OnIssueOrder(Obj_AI_Base sender, GameObjectIssueOrderEventArgs args)
        {
            if (sender == null || !sender.IsValid || !sender.IsMe)
            {
                return;
            }
            if (args.Order == GameObjectOrder.MoveTo)
            {
                if (Environment.TickCount - LastMove < Menu.Item("MovementDelay").GetValue<Slider>().Value &&
                    Menu.Item("MovementEnabled").GetValue<bool>())
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
            if (args.Target.IsEnemy && sender.UnderTurret(true) && (args.Order == GameObjectOrder.AutoAttack || args.Order == GameObjectOrder.AttackUnit))
            {
                args.Process = false;
            }
        }

        public void RefreshMinions()
        {
            ObjMinions.UpdateMinions();
            LeadingMinion = Utility.Map.GetMap().Type == Utility.Map.MapType.SummonersRift
                ? ObjMinions.GetLeadMinion(SummonersRift.BottomLane.Bottom_Zone.CenterOfPolygone().To3D())
                : ObjMinions.GetLeadMinion();
            ClosestEnemyMinion = ObjMinions.GetClosestEnemyMinion();
        }

        public static bool InDangerUnderEnemyTurret()
        {
            var nearestTurret = Turrets.EnemyTurrets.FirstOrDefault(t => t.Distance(Player) < 800);
            if (nearestTurret != null)
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .Count(minion => minion.IsAlly && !minion.IsDead && minion.Distance(nearestTurret) < 650) <= 2;
            }
            return false;
        }

        #endregion Minions
    }
}