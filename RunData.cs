using Newtonsoft.Json;
using System.Collections.Generic;

namespace RunWatcher
{
    internal class RunData
    {
        public static string SurvivorName { get; set; }
        public static List<object> Items { get; set; } = [];
        public static List<string> Equipment { get; set; } = [];

        //public static List<string> Loadout { get; set; }

        //public static List<string> Route { get; set; }

        public static string GetCurrentRunData()
        {
            if (SurvivorName == null) { return null; }   
            var currentRunData = new { SurvivorName, Items, Equipment };
            return JsonConvert.SerializeObject(currentRunData);
        }
    }
}
