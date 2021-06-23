using System;
using System.Collections.Generic;

namespace W3ChampionsStatisticService.Achievements.Models {
    public class Achievement {
        public long Id {get; set;}
        public string Title {get; set;}
        public string Caption {get; set;}
        public long ProgressCurrent {get; set;}
        public long ProgressEnd {get; set;}
        public bool Completed {get; set;}
        public Dictionary<string,int> Counter {get; set;}
    }
}