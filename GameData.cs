using System;
using System.Collections.Generic;
using System.Text;

namespace RunWatcher
{
    public class ItemCollection
    {
        public List<string> Common { get; set; } = [];
        public List<string> Uncommon { get; set; } = [];
        public List<string> Legendary { get; set; } = [];
        public List<string> Boss { get; set; } = [];
        public List<string> Lunar { get; set; } = [];
        public List<string> Void { get; set; } = [];
    }

    public class GameData
    {
        public ItemCollection Items { get; set; } = new ItemCollection();
        public List<string> Equipment { get; set; } = [];
        public List<string> Survivors { get; set; } = [];
    }
}