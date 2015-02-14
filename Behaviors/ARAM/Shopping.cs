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



#endregion AiM License

using System.Runtime.Remoting.Messaging;
using BehaviorSharp;
using BehaviorSharp.Components.Actions;
using BehaviorSharp.Components.Composites;
using BehaviorSharp.Components.Conditionals;
using BehaviorSharp.Components.Decorators;
using LeagueSharp;
using LeagueSharp.Common;

namespace AiM.Behaviors.ARAM
{
    internal static class Shopping
    {
        internal static Conditional ShoppingConditional = new Conditional(() => ObjectManager.Player.InFountain());
        internal static Inverter ShopppingInverter = new Inverter(new Conditional(() => ShoppingConditional.Tick() != BehaviorState.Success));
        //#TODO Implement Shopping Logic
        internal static BehaviorAction ShoppingAction = new BehaviorAction(() => Orbwalking.Mixed.Tick());
        internal static Sequence ShoppingSequence = new Sequence(ShoppingConditional, ShopppingInverter, ShoppingAction);

    }
}
