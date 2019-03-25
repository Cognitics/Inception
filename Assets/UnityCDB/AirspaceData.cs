using System.Collections.Generic;
using UnityEngine;

namespace Cognitics.UnityCDB
{
    public class AirspaceData : ScriptableObject
    {
        public List<Color> ClassColors = new List<Color>();
        public AirspaceData()
        {
            ClassColors.Add(new Color(177f / 255f, 119f / 255f, 176f / 255f, 64f / 255f));
            ClassColors.Add(new Color(253f / 255f, 168f / 255f, 76f / 255f, 64f / 255f));
            ClassColors.Add(new Color(67f / 255f, 177f / 255f, 216f / 255f, 64f / 255f));
            ClassColors.Add(new Color(185f / 255f, 209f / 255f, 94f / 255f, 64f / 255f));
            ClassColors.Add(new Color(249f / 255f, 161f / 255f, 224f / 255f, 64f / 255f));
            ClassColors.Add(new Color(255f / 255f, 229f / 255f, 101f / 255f, 64f / 255f));
            ClassColors.Add(new Color(241f / 255f, 116f / 255f, 116f / 255f, 64f / 255f));
            ClassColors.Add(new Color(197f / 255f, 154f / 255f, 110f / 255f, 64f / 255f));
            ClassColors.Add(new Color(99f / 255f, 191f / 255f, 173f / 255f, 64f / 255f));
            ClassColors.Add(new Color(137f / 255f, 137f / 255f, 222f / 255f, 64f / 255f));
            ClassColors.Add(new Color(187f / 255f, 187f / 255f, 187f / 255f, 64f / 255f));
        }
    }
}
