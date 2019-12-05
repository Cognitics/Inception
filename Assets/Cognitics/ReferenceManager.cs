using System;
using System.Collections.Generic;

namespace Cognitics
{
    public abstract class ReferenceEntry
    {
        public int Memory { get; protected set; } = 0;
        public int ReferenceCount { get; internal set; } = 0;
        public virtual bool Loaded => false;
        public abstract void Load(string name);
        public abstract void Unload();
    }

    public class ReferenceManager<T> where T : ReferenceEntry, new()
    {
        public bool Debug = false;
        private Dictionary<string, T> Entries = new Dictionary<string, T>();

        public T Entry(string name)
        {
            if (!Entries.ContainsKey(name))
            {
                Entries[name] = new T();
                Entries[name].Load(name);
                if(Debug)
                    Console.WriteLine("[ReferenceManager] oo " + name);
            }
            ++Entries[name].ReferenceCount;
            if(Debug)
                Console.WriteLine("[ReferenceManager] ++ " + name);
            return Entries[name];
        }

        public void Release(string name)
        {
            if (!Entries.ContainsKey(name))
                throw new ArgumentException();
            if (Entries[name].ReferenceCount < 1)
                throw new InvalidOperationException();
            --Entries[name].ReferenceCount;
            if(Debug)
                Console.WriteLine("[ReferenceManager] -- " + name);
            if (Entries[name].ReferenceCount > 0)
                return;
            if(Debug)
                Console.WriteLine("[ReferenceManager] xx " + name);
            Entries[name].Unload();
            Entries.Remove(name);
        }

        public long Memory()
        {
            long result = 0;
            foreach (var entry in Entries.Values)
                result += entry.Memory;
            return result;
        }

    }
}
