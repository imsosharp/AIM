using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIM.Autoplay.Behaviors.Strategy;
using AIM.Autoplay.Util.Objects;
using BehaviorSharp;
using BehaviorSharp.Components.Composites;
using BehaviorSharp.Components.Conditionals;
using LeagueSharp.Common;

namespace AIM.Autoplay.Behaviors
{
    internal class MainBehavior
    {
        internal static Behavior Root = new Behavior(new IndexSelector(
            () =>
            {
                if (new Conditionals().NoMinions.Tick() == BehaviorState.Success)
                {
                    return 5;
                }
                if (new Conditionals().JoinTeamFight.Tick() == BehaviorState.Success && new Strategy.Positioning.Util().GetAllyPosList().Count != 0)
                {
                    return 1;
                }
                if (Heroes.Me.IsDead)
                {
                    return 0;
                }
                if (new Conditionals().ShouldPushLane.Tick() == BehaviorState.Success)
                {
                    return 2;
                }
                if (new Conditionals().ShouldCollectHealthRelic.Tick() == BehaviorState.Success)
                {
                    return 3;
                }
                return 4;
            }, new Sequence(), new Sequences().TeamFight, new Sequences().LanePush, new Sequences().CollectHealthPack, new Sequences().StayWithinExpRange, new Sequences().WalkToLane));
    }
}
