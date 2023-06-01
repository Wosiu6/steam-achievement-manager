using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAM.API.Types
{
    // 
    public class Achievement
    {
        public string Name { get; set; }
        public double Percent { get; set; }
    }

    public class Achievementpercentages
    {
        public List<Achievement> Achievements { get; set; }
    }

    public class Root
    {
        public Achievementpercentages Achievementpercentages { get; set; }
    }
}
