using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

class CommandLine
{
    public static XDocument Document(string[] args)
    {
        return args.Length < 1 ? XDocument.Load("models.xml", LoadOptions.SetLineInfo) : XDocument.Load(args[0]);
    }

    public static Dictionary<char, int> Pallete(string[] args)
    {
        XDocument doc = args.Length < 2 ? XDocument.Load("resources/palette.xml") : XDocument.Load(args[1]);
        return doc.Root.Elements("color").ToDictionary(x => x.Get<char>("symbol"), x => Convert.ToInt32(x.Get<string>("value"), 16) + (255 << 24));
    }
}