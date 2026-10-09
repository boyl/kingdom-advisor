using System;
using System.Globalization;

namespace KingdomAdvisor
{
    public sealed class CompanionMountSlot
    {
        public int Kind,Serial;
        public float Cooldown,Age;
    }
    public static class CompanionMountWire
    {
        public static bool TrySlot(string value,out CompanionMountSlot slot)
        {
            slot=null;
            if(value==null || value.Length>128)return false;
            var parts=value.Split(',');
            if(parts.Length!=4 || !int.TryParse(parts[0],out int kind) || kind<0 || kind>2 || !int.TryParse(parts[1],out int serial) || serial<0 || !float.TryParse(parts[2],NumberStyles.Float,CultureInfo.InvariantCulture,out float cooldown) || !float.IsFinite(cooldown) || cooldown<0 || cooldown>12 || !float.TryParse(parts[3],NumberStyles.Float,CultureInfo.InvariantCulture,out float age) || !float.IsFinite(age) || age<0 || age>600)return false;
            slot=new CompanionMountSlot{Kind=kind,Serial=serial,Cooldown=cooldown,Age=age};return true;
        }
    }
}
