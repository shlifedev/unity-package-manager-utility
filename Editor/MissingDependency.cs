using System;

namespace LD.Editor
{
    [Serializable]
    public struct MissingDependency
    {
        public string Name;
        public string[] Versions;
    }
}