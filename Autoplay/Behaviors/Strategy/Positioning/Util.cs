using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Path = System.Collections.Generic.List<ClipperLib.IntPoint>;
using Paths = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;

namespace AIM.Autoplay.Behaviors.Strategy.Positioning
{
    public class Util
    {
        public Paths GetAllyPaths()
        {
            var allyPaths = new Paths(GetAllyPosList().Count);
            for (int i = 0; i < GetAllyPosList().Count; i++)
            {
                allyPaths[i].Add(new IntPoint(GetAllyPosList().ToArray()[i].X, GetAllyPosList().ToArray()[i].Y));
            }
            return allyPaths;
        }

        public Paths GetEnemyPaths()
        {
            var enemyPaths = new Paths(GetEnemyPosList().Count);
            for (int i = 0; i < GetEnemyPosList().Count; i++)
            {
                enemyPaths[i].Add(new IntPoint(GetEnemyPosList().ToArray()[i].X, GetEnemyPosList().ToArray()[i].Y));
            }
            return enemyPaths;
        }

        public List<Vector2> GetAllyPosList()
        {
            var allies = ObjectManager.Get<Obj_AI_Hero>().FindAll(h => h.IsAlly && !h.IsMe && !h.IsDead && !h.InFountain()).ToList();
            return allies.Select(ally => ally.ServerPosition.To2D()).ToList();
        }
        public List<Vector2> GetEnemyPosList()
        {
            var enemies = ObjectManager.Get<Obj_AI_Hero>().FindAll(h => h.IsEnemy && !h.IsDead && h.IsVisible).ToList();
            return enemies.Select(enemy => enemy.ServerPosition.To2D()).ToList();
        }
    }
}
