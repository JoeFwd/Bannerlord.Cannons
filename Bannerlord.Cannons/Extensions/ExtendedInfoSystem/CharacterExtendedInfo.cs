using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    /// <summary>
    /// Contains Tow data of single unit or character template. 
    /// </summary>
    public class CharacterExtendedInfo
    {
        [XmlAttribute("id")]
        public string CharacterStringId;
        [XmlArray("Abilities")]
        public List<string> Abilities = new List<string>();
        [XmlArray("Attributes")]
        public List<string> CharacterAttributes = new List<string>();
    }
}
