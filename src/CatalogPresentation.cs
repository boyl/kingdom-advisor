using System;
using System.Linq;
using System.Collections.Generic;
namespace KingdomAdvisor
{
    // Identity mappings are explicit: categories must never silently choose an icon.
    // Composite icons read left to right: object, then purpose or distinguishing trait.
    public static class CatalogIcons
    {
        public static string Kind(Entry e)=>e.Key switch
        {
            "hermitfire"=>"person+flame", "hermitknight"=>"person+flag",
            "hermitbaker"=>"person+bread", "hermitballista"=>"person+bow",
            "hermithorn"=>"person+horn", "hermitstable"=>"person+horse",
            "statuepike"=>"statue+pike", "hourglass"=>"statue+hourglass",
            "hornwall"=>"wall+horn", "berserkertower"=>"tower+axe",
            "firetower"=>"tower+flame", "knighttower"=>"tower+flag",
            "shieldshop"=>"house+shield", "sword"=>"sword",
            "bomb"=>"bomb", "bombpurchase"=>"flag+bomb",
            "wall"=>"wall", "tower"=>"tower+bow",
            "castle"=>"castle", "campfire"=>"camp+flame",
            "farmhouse"=>"house+wheat", "farm"=>"field",
            "beggarcamp"=>"camp+person", "beggar"=>"person",
            "citizenhouse"=>"house+person", "bow"=>"bow",
            "hammer"=>"hammer", "scythe"=>"scythe", "pike"=>"pike",
            "shield"=>"flag+shield", "ninja"=>"house+star",
            "forge"=>"anvil+sword", "workshop"=>"house+catapult",
            "catapult"=>"catapult", "ballista"=>"bow+wheel",
            "baker"=>"house+bread", "lighthouse"=>"tower+sun",
            "wharf"=>"pier", "boat"=>"boat", "shipyard"=>"house+boat",
            "teleporter"=>"portal+arrow", "portal"=>"portal+claw", "cave"=>"cave",
            "mine"=>"pick+anvil", "quarry"=>"pick+rock",
            "statuearcher"=>"statue+bow", "statueworker"=>"statue+hammer",
            "statuefarmer"=>"statue+wheat", "statueknight"=>"statue+sword",
            "statue"=>"statue+question", "monument"=>"pillar+question",
            "hermit"=>"person+question", "cabin"=>"house+hat",
            "chest"=>"chest", "bank"=>"house+coin", "merchant"=>"person+coin",
            "berry"=>"berry", "tree"=>"tree",
            "advisorcat"=>"cat+bomb", "advisordog"=>"hound+bolt",
            "steed"=>"horse+question", "horse"=>"horse", "griffin"=>"eagle+wing",
            "bear"=>"bear", "stag"=>"antler", "warhorse"=>"horse+shield",
            "lizard"=>"reptile+flame", "unicorn"=>"horse+horn",
            "drafthorse"=>"horse+heart", "fasthorse"=>"horse+arrow",
            "bursthorse"=>"horse+bolt", "undeadhorse"=>"horse+skull",
            "wolf"=>"hound+moon", "reindeer"=>"antler+snow",
            "gamigin"=>"demon+arrow", "golem"=>"rock+root", "beetle"=>"beetle+egg",
            "sleipnir"=>"horse+flame", "catcart"=>"cat+wheel", "kelpie"=>"horse+water",
            "goldenboar"=>"boar+coin", "daynight"=>"horse+sunmoon",
            "hippocampus"=>"fish+horse", "cerberus"=>"hound+ghost",
            "spider"=>"spider+web", "chariotday"=>"wheel+sun", "chariotnight"=>"wheel+moon",
            "pegasus"=>"horse+wing", "donkey"=>"donkey+bread", "argos"=>"hound+arrow",
            "chimera"=>"demon+flame", "rainbowpony"=>"horse+rainbow", "stable"=>"house+horse",
            "trojan"=>"horse+wood", "serpent"=>"reptile+claw",
            "hephaestus"=>"anvil+flame", "persephone"=>"person+apple", "artemis"=>"bow+antler",
            "thor"=>"hammer+bolt", "hel"=>"skull+flag",
            "bell"=>"bell", "barrel"=>"barrel+flame",
            "gembank"=>"chest+gem", "gemguard"=>"person+gem",
            "grave"=>"grave", "border"=>"flag+arrow",
            _=>"question"
        };
        public static string[] Pattern(string kind)
        {
            var parts=kind.Split('+');var main=Glyph(parts[0]);var badge=parts.Length>1?Glyph(parts[1]):null;
            return Enumerable.Range(0,5).Select(row=>main[row]+"0"+(badge==null?"00000":badge[row])).ToArray();
        }
        private static string[] Glyph(string kind)=>kind switch
        {
            "bomb"=>new[]{"00011","00100","01110","11111","01110"},
            "flag"=>new[]{"11110","10111","11110","10000","10000"},
            "wall"=>new[]{"10101","11111","11011","01110","11111"},
            "field"=>new[]{"00000","10101","10101","10101","11111"},
            "camp"=>new[]{"00100","01010","01010","10001","11011"},
            "horn"=>new[]{"00011","01111","11110","01000","01100"},
            "flame"=>new[]{"00100","00110","01101","11111","01110"},
            "bow"=>new[]{"01100","01010","01001","01010","01100"},
            "bread"=>new[]{"01110","11111","10101","11111","11111"},
            "sword"=>new[]{"00010","00110","01100","11100","10010"},
            "horse"=>new[]{"00011","00111","11110","11110","10010"},
            "statue"=>new[]{"00100","01110","00100","01110","11111"},
            "hammer"=>new[]{"11110","11110","00100","00100","00100"},
            "shield"=>new[]{"11111","10101","10101","01110","00100"},
            "tower"=>new[]{"10101","11111","11111","11011","11011"},
            "wheat"=>new[]{"10101","01110","10101","01110","00100"},
            "person"=>new[]{"01110","01110","00100","11111","01010"},
            "boat"=>new[]{"00100","00110","00111","11111","01110"},
            "house"=>new[]{"00100","01110","11111","11011","11011"},
            "tree"=>new[]{"00100","01110","11111","00100","00100"},
            "question"=>new[]{"01110","10001","00010","00100","00100"},
            "hourglass"=>new[]{"11111","01010","00100","01010","11111"},
            "pike"=>new[]{"00100","01110","00100","00100","00100"},
            "axe"=>new[]{"00110","11111","00110","00100","00100"},
            "castle"=>new[]{"10101","11111","10101","11111","11011"},
            "scythe"=>new[]{"01111","10001","10000","10000","10000"},
            "star"=>new[]{"00100","10101","01110","10101","00100"},
            "anvil"=>new[]{"11111","01110","00100","01110","11111"},
            "catapult"=>new[]{"10000","01001","00110","11110","01010"},
            "wheel"=>new[]{"01110","10101","11011","10101","01110"},
            "sun"=>new[]{"10101","01110","11011","01110","10101"},
            "pier"=>new[]{"00000","11111","01010","01010","10101"},
            "portal"=>new[]{"01110","11011","10001","11011","01110"},
            "arrow"=>new[]{"00100","00010","11111","00010","00100"},
            "claw"=>new[]{"10101","10101","00000","01110","11111"},
            "cave"=>new[]{"01110","11111","11011","10001","10001"},
            "pick"=>new[]{"11110","00101","00100","00100","00100"},
            "rock"=>new[]{"01100","11110","10111","11101","11111"},
            "pillar"=>new[]{"11111","01110","01110","01110","11111"},
            "hat"=>new[]{"00100","01110","11111","00000","00000"},
            "chest"=>new[]{"01110","11111","00100","11111","11111"},
            "coin"=>new[]{"01110","10101","10101","10101","01110"},
            "berry"=>new[]{"00110","00100","11011","11011","01110"},
            "eagle"=>new[]{"00111","01110","11110","01000","10100"},
            "wing"=>new[]{"10001","11011","11111","01110","00100"},
            "bear"=>new[]{"10001","11111","10101","11011","01110"},
            "antler"=>new[]{"10101","11011","01010","01110","00100"},
            "reptile"=>new[]{"00011","01111","11100","10100","10010"},
            "heart"=>new[]{"01010","11111","11111","01110","00100"},
            "bolt"=>new[]{"00111","01110","11111","00110","01100"},
            "skull"=>new[]{"01110","11111","10101","01110","01010"},
            "hound"=>new[]{"10001","11011","10101","01110","00100"},
            "moon"=>new[]{"00110","01100","11000","01100","00110"},
            "snow"=>new[]{"10101","01110","11111","01110","10101"},
            "demon"=>new[]{"10001","01010","11111","10101","01110"},
            "root"=>new[]{"00100","00100","01110","10101","10001"},
            "beetle"=>new[]{"01010","10101","11111","10101","01010"},
            "egg"=>new[]{"00100","01110","11111","11111","01110"},
            "cat"=>new[]{"10001","11111","10101","01110","10000"},
            "water"=>new[]{"00000","01010","10101","01010","10101"},
            "boar"=>new[]{"01010","11111","10101","11111","11011"},
            "sunmoon"=>new[]{"10110","01100","11100","01100","10110"},
            "fish"=>new[]{"00100","01110","11111","01110","10101"},
            "ghost"=>new[]{"01110","10101","11111","11111","10101"},
            "spider"=>new[]{"10001","01110","11111","01110","10001"},
            "web"=>new[]{"10101","01010","10101","01010","10101"},
            "donkey"=>new[]{"01010","01010","11110","11110","10010"},
            "rainbow"=>new[]{"01110","10001","01110","01010","00000"},
            "wood"=>new[]{"11111","10001","11111","10001","11111"},
            "apple"=>new[]{"00110","00100","01110","11111","01110"},
            "bell"=>new[]{"00100","01110","01110","11111","00100"},
            "barrel"=>new[]{"01110","11111","10101","11111","01110"},
            "gem"=>new[]{"01110","10101","01010","01010","00100"},
            "grave"=>new[]{"01110","11011","11111","11111","11111"},
            _=>throw new ArgumentException("Unknown catalog glyph: "+kind,nameof(kind))
        };
    }
    public static class LocationPresentation
    {
        public static string Identity(MapPoint p)=>p.Key+"|"+p.Name+"|"+Math.Round(p.X,1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static int Find(MapPoint[] points,MapPoint target)=>Array.FindIndex(points,p=>p.Key==target.Key&&p.Name==target.Name&&Math.Abs(p.X-target.X)<.1f);
        public static Entry Entry(MapPoint point)=>Catalog.Entries.FirstOrDefault(e=>e.Key==point.Key);
        public static string Distance(MapPoint point,float x)=>Math.Abs(point.X-x)<1?"当前位置":(point.X<x?"左侧":"右侧")+" · 距离 "+Math.Abs(point.X-x).ToString("F0");
        public static string Detail(MapPoint point,PlayerInfo player)
        {
            point.PlayerStates.TryGetValue(player.Id,out var state);
            var entry=Entry(point);
            var description=state!=null?state.Description:entry?.Description??"该地点尚无对应图鉴条目。";
            var rows=new List<string>{Distance(point,player.X),"",description};
            if(point.CampPeople>=0)rows.Add("招募 "+point.CampPeople);
            if(state!=null)
            {
                if(state.Cost>=0)rows.Add("费用  "+state.Cost+" "+state.Currency);
                rows.Add((state.Locked?"暂不可用  ":"可交互  ")+state.Lock);
                if(state.Details.Length>0)rows.Add(state.Details);
                if(state.Next.Length>0)rows.Add("升级目标  "+state.Next);
            }
            return string.Join("\n",rows);
        }
    }
}
