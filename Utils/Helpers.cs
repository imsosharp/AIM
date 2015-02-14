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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
using Version = System.Version;
#endregion AiM License

namespace AiM.Utils
{
    internal class Helpers
    {
        internal static void Print(string message, params object[] @params)
        {
            if (!@params.Any())
            {
                Game.PrintChat("<font color='#D859CD'>AiM: </font>" + "<font color='#ADEC00'>" + message + "</font>");
            }
            else
            {
                var newmessage = message;
                for (var i = 0; i < 10; i++)
                {
                    newmessage.Replace("{" + i + "}", @params[i].ToString());
                }
                message.Replace("{0}", @params[0].ToString());
                Game.PrintChat("<font color='#D859CD'>AiM: </font>" + "<font color='#ADEC00'>" + newmessage + "</font>");
            }
        }

        internal static void PrintWarning(string message, params object[] @params)
        {
            if (!@params.Any())
            {
                Game.PrintChat("<font color='#D859CD'>AiM: </font>" + "<font color='FF0000'>" + message + "</font>");
            }
            else
            {
                var newmessage = message;
                for (var i = 0; i < @params.Count(); i++)
                {
                    newmessage.Replace("{" + i + "}", @params[i].ToString());
                }
                message.Replace("{0}", @params[0].ToString());
                Game.PrintChat("<font color='#D859CD'>AiM: </font>" + "<font color='#FF0000'>" + newmessage + "</font>");
            }
        }

        internal static void Updater()
        {
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        var installedVersion = Assembly.GetExecutingAssembly().GetName().Version;
                        var request = WebRequest.Create("https://raw.githubusercontent.com/trees-software/AIM/master/Properties/AssemblyInfo.cs");
                        var response = request.GetResponse();
                        if (response.GetResponseStream() == null) { PrintWarning("Network unreacheable"); return; }

                        var streamReader = new StreamReader(response.GetResponseStream());
                        var versionPattern = @"\[assembly\: AssemblyVersion\(""(\d{1,})\.(\d{1,})\.(\d{1,})\.(\d{1,})""\)\]";
                        Match match;
                        using (streamReader)
                        {
                            match = new Regex(versionPattern).Match(streamReader.ReadToEnd());
                            Version latestVersion;
                            if (match.Success)
                            {
                                latestVersion =
                                    new Version(
                                        string.Format(
                                            "{0}.{1}.{2}.{3}", match.Groups[1], match.Groups[2], match.Groups[3],
                                            match.Groups[4]));
                                if (installedVersion != latestVersion)
                                {
                                    PrintWarning("A new AiM version has been released. Please update to v.{0}!</font>", latestVersion);
                                    PrintWarning("Outdated AiM version loaded!");
                                }
                                else
                                {
                                    Print(@"Version {0} loaded. Enjoy!", installedVersion);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
        }
    }
}
