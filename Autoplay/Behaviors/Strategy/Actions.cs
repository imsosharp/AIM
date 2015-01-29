using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIM.Autoplay.Util;
using AIM.Autoplay.Util.Data;
using AIM.Autoplay.Util.Objects;
using BehaviorSharp;
using BehaviorSharp.Components.Actions;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Orbwalking = AIM.Autoplay.Util.Orbwalking;

namespace AIM.Autoplay.Behaviors.Strategy
{
    internal class Actions
    {
        internal BehaviorAction PushLane = new BehaviorAction(
            () =>
            {
                try
                {
                    var objConstants = new Constants();
                    var isInDanger = ObjectManager.Player.UnderTurret(true) && Modes.Base.InDangerUnderEnemyTurret();
                    if (Heroes.Me.UnderTurret(true))
                    {
                        var turret = Turrets.EnemyTurrets.OrderBy(t => t.Distance(Heroes.Me)).FirstOrDefault();
                        Modes.Base.OrbW.ForceTarget(turret);
                    }
                    if (isInDanger)
                    {
                        var orbwalkingPos = new Vector2
                        {
                            X = ObjectManager.Player.Position.X + (objConstants.DefensiveAdditioner),
                            Y = ObjectManager.Player.Position.Y + (objConstants.DefensiveAdditioner)
                        };
                        ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, orbwalkingPos.To3D());
                        Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.None;
                        Modes.Base.OrbW.SetAttack(false);
                        Modes.Base.OrbW.SetMovement(false);
                        return BehaviorState.Success;
                    }
                    if (Modes.Base.LeadingMinion != null)
                    {
                        var orbwalkingPos = new Vector2
                        {
                            X =
                                Modes.Base.LeadingMinion.Position.X + (objConstants.DefensiveAdditioner / 8f) +
                                Randoms.Rand.Next(-100, 100),
                            Y =
                                Modes.Base.LeadingMinion.Position.Y + (objConstants.DefensiveAdditioner / 8f) +
                                Randoms.Rand.Next(-100, 100)
                        };
                        Utility.DelayAction.Add(new Random(Environment.TickCount).Next(500, 1500), () => Modes.Base.OrbW.SetOrbwalkingPoint(orbwalkingPos.To3D()));
                        Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.Mixed;
                        Modes.Base.OrbW.SetAttack(true);
                        Modes.Base.OrbW.SetMovement(true);
                        return BehaviorState.Success;
                    }
                    return BehaviorState.Failure;
                }
                catch (NullReferenceException e)
                {
                    Console.WriteLine(e);
                }
                return BehaviorState.Failure;
            });

        internal BehaviorAction StayWithinExpRange = new BehaviorAction(
            () =>
            {
                var objConstants = new Constants();
                var isInDanger = ObjectManager.Player.UnderTurret(true) && Modes.Base.InDangerUnderEnemyTurret();
                if (Heroes.Me.UnderTurret(true))
                {
                    var turret = Turrets.EnemyTurrets.OrderBy(t => t.Distance(Heroes.Me)).FirstOrDefault();
                    Modes.Base.OrbW.ForceTarget(turret);
                }
                if (isInDanger)
                {
                    var orbwalkingPos = new Vector2
                    {
                        X = ObjectManager.Player.ServerPosition.X + objConstants.DefensiveAdditioner,
                        Y = ObjectManager.Player.ServerPosition.Y + objConstants.DefensiveAdditioner
                    };
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, orbwalkingPos.To3D());
                    Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.None;
                    Modes.Base.OrbW.SetAttack(false);
                    Modes.Base.OrbW.SetMovement(false);
                    return BehaviorState.Success;
                }

                if (Modes.Base.ClosestEnemyMinion != null)
                {
                    var orbwalkingPos = new Vector2
                    {
                        X =
                            Modes.Base.ClosestEnemyMinion.Position.X + objConstants.DefensiveAdditioner +
                            Randoms.Rand.Next(-150, 150),
                        Y =
                            Modes.Base.ClosestEnemyMinion.Position.Y + objConstants.DefensiveAdditioner +
                            Randoms.Rand.Next(-150, 150)
                    };
                    Utility.DelayAction.Add(new Random(Environment.TickCount).Next(500, 1500), () => Modes.Base.OrbW.SetOrbwalkingPoint(orbwalkingPos.To3D()));
                    Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.Mixed;
                    Modes.Base.OrbW.SetAttack(true);
                    Modes.Base.OrbW.SetMovement(true);
                    return BehaviorState.Success;
                }
                return BehaviorState.Success;
            });

        internal BehaviorAction KillEnemy = new BehaviorAction(
            () =>
            {
                var spells = new List<SpellSlot> { SpellSlot.Q, SpellSlot.W, SpellSlot.E };
                var heroes = new Heroes();
                var killableEnemy = heroes.EnemyHeroes.FirstOrDefault(h => h.Health < Heroes.Me.GetComboDamage(h, spells) + Heroes.Me.GetAutoAttackDamage(Heroes.Me));
                if (killableEnemy == null || killableEnemy.IsDead || !killableEnemy.IsValidTarget() ||
                    killableEnemy.IsInvulnerable || killableEnemy.UnderTurret(true) || Heroes.Me.IsDead)
                {
                    return BehaviorState.Success;
                }


                Modes.Base.OrbW.ForceTarget(killableEnemy);
                Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.Combo;
                var orbwalkingPos = new Vector3
                {
                    X =
                        killableEnemy.Position.X +
                        (Heroes.Me.AttackRange - 0.2f * Heroes.Me.AttackRange) *
                        Modes.Base.ObjConstants.DefensiveMultiplier,
                    Y =
                        killableEnemy.Position.Y +
                        (Heroes.Me.AttackRange - 0.2f * Heroes.Me.AttackRange) *
                        Modes.Base.ObjConstants.DefensiveMultiplier
                };
                Modes.Base.OrbW.SetOrbwalkingPoint(orbwalkingPos);
                return BehaviorState.Success;
            });
        internal BehaviorAction CollectHealthRelic = new BehaviorAction(
            () =>
            {
                if (Heroes.Me.Position != Relics.ClosestRelic().Position)
                {
                    Heroes.Me.IssueOrder(GameObjectOrder.MoveTo, Relics.ClosestRelic().Position);
                    Modes.Base.OrbW.SetAttack(false);
                    Modes.Base.OrbW.SetMovement(false);
                    return BehaviorState.Running;
                }
                Modes.Base.OrbW.SetAttack(true);
                Modes.Base.OrbW.SetMovement(true);
                return BehaviorState.Success;
            });
        internal BehaviorAction ProtectFarthestTurret = new BehaviorAction(
            () =>
            {
                var farthestTurret = Turrets.AllyTurrets.OrderByDescending(t => t.Distance(HQ.AllyHQ)).FirstOrDefault();
                var objConstants = new Constants();
                var orbwalkingPos = new Vector2();
                if (farthestTurret != null)
                {
                    orbwalkingPos.X = farthestTurret.Position.X + (objConstants.DefensiveAdditioner / 8f) +
                                      Randoms.Rand.Next(-100, 100);
                    orbwalkingPos.Y = farthestTurret.Position.Y + (objConstants.DefensiveAdditioner / 8f) +
                                      Randoms.Rand.Next(-100, 100);
                }
                else
                {
                    orbwalkingPos.X = HQ.AllyHQ.Position.X + (objConstants.DefensiveAdditioner / 8f) +
                                      Randoms.Rand.Next(-100, 100);
                    orbwalkingPos.Y = HQ.AllyHQ.Position.Y + (objConstants.DefensiveAdditioner / 8f) +
                                      Randoms.Rand.Next(-100, 100);
                }

                Modes.Base.OrbW.SetOrbwalkingPoint(orbwalkingPos.To3D());
                Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.Mixed;
                return BehaviorState.Success;
            });

        internal BehaviorAction Teamfight = new BehaviorAction(
            () =>
            {
                var orbwalkingPos = Positioning.Teamfight.GetPos();
                Modes.Base.OrbW.SetOrbwalkingPoint(orbwalkingPos.To3D());
                Modes.Base.OrbW.ActiveMode = Orbwalking.OrbwalkingMode.Mixed;
                return BehaviorState.Success;
            });
    }
}
