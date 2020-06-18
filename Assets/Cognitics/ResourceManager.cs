
/*
Cognitics.ResourceManager is a generalized manager for background loading of
external resources. By default, it creates one worker thread, but this can
be set during construction:

    var manager = new ResourceManager(thread_count: 2);

Each resource type requires a delegate implementation for a synchronous load operation:

    bool MyLoad(string name, ResourceEntry re)
    {
        // get entry as something we recognize (see below)
        var entry = ResourceEntry as MyEntry;
        entry.MyData = ...

        // some synchronous load operation
        // return true on success, false on failure
    }

The delegate implementation is assigned to a regex match expression that is
applied to the name of the resource (typically a filename or URI):

    manager.SetLoadDelegate(new Regex(".ext$", RegexOptions.Compiled | RegexOptions.IgnoreCase), MyLoad);

A ResourceEntry is a generic concept of a reference counted asynchronously loaded
data container. To load data, a subclass must be created:

    public class MyEntry : ResourceEntry
    {
        public object MyData = null;

        ~MyEntry()
        {
            // clean up
        }
    }

To load a resource, a ResourceEntry is fetched from the ResourceManager:

    var entry = manager.Fetch<MyEntry>("something.ext");

The entry is essentially a future. The LoadComplete property will be true when the
resource is available for consumption:

    if(entry.LoadComplete)
    {
        var data = entry.MyData;
        // do something with it
    }

When finished with a resource, it should be set to null and released via
the ResourceManager:

    entry = null;
    manager.Release("something.ext");

If no other operations have a reference to the entry, it will be dereferenced
by the manager, allowing GC to clean it up.

Requests are maintained in a stack (first in, last out). This prioritizes recent
requests over older requests. The request stack may contain duplicate entries in
order to facilitate pushing items up in priority. To add a duplicate:

    manager.Prod("something.ext");


*/

using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Cognitics
{
    public abstract class ResourceEntry
    {
        public int ReferenceCount { get; internal set; } = 0;
        public bool LoadComplete { get; internal set; } = false;
    }

    public class ResourceManager
    {
        public bool Debug = false;

        public ConcurrentStack<string> Requests = new ConcurrentStack<string>();
        public ConcurrentDictionary<string, ResourceEntry> Entries = new ConcurrentDictionary<string, ResourceEntry>();

        private ResourceThread[] Threads;
        public ResourceManager(int thread_count = 1)
        {
            Threads = new ResourceThread[thread_count];
            for (int i = 0; i < thread_count; ++i)
                Threads[i] = new ResourceThread(this);
        }

        internal ConcurrentDictionary<Regex, Action<string, ResourceEntry>> LoadDelegates = new ConcurrentDictionary<Regex, Action<string, ResourceEntry>>();
        public void SetLoadDelegate(Regex regex, Action<string, ResourceEntry> loader) => LoadDelegates[regex] = loader;

        public T Fetch<T>(string name) where T : ResourceEntry, new()
        {
            if (!Entries.ContainsKey(name))
            {
                Entries[name] = new T();
                Requests.Push(name);
                if(Debug)
                    Console.WriteLine("[ResourceManager] oo " + name);
            }
            ++Entries[name].ReferenceCount;
            if(Debug)
                Console.WriteLine("[ResourceManager] ++ " + name);
            return Entries[name] as T;
        }

        public void Release(string name)
        {
            if (!Entries.TryGetValue(name, out ResourceEntry entry))
            {
                // TODO: calling Release is an error but it doesn't actually hurt anything; however, at some point we need to figure out why the app is calling Release on a name without an entry
                return;
                throw new ArgumentException("ResourceManager.Release(): entry not found: " + name);
            }
            if (entry.ReferenceCount < 1)
                throw new InvalidOperationException("ResourceManager.Release(): attempted to decrement reference count below zero");
            --entry.ReferenceCount;
            if (Debug)
                Console.WriteLine("[ResourceManager] -- " + name + " (" + entry.ReferenceCount + ")");
            if (entry.ReferenceCount > 0)
                return;
            if (Debug)
                Console.WriteLine("[ResourceManager] xx " + name);
            Entries.TryRemove(name, out ResourceEntry _);
        }

        public void Prod(string name) => Requests.Push(name);

        public void DumpToConsole()
        {
            Console.WriteLine(string.Format("[ResourceManager] {0} threads ; {1} requests ; {2} entries", Threads.Length, Requests.Count, Entries.Count));
        }

    }

    public class ResourceThread
    {
        private ResourceManager Manager;
        private Thread Thread;

        internal ResourceThread(ResourceManager manager)
        {
            Manager = manager;
            Thread = new Thread(ThreadRun);
            Thread.Start();
        }

        ~ResourceThread() => Thread.Abort();

        private void ThreadRun()
        {
            while (true)
            {
                try
                {
                    if (!Manager.Requests.TryPop(out string name))
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    if (!Manager.Entries.TryGetValue(name, out ResourceEntry entry))
                        continue;
                    if(Manager.Debug)
                        Console.WriteLine("[ResourceThread] ThreadRun(): " + name);
                    foreach (var kv in Manager.LoadDelegates)
                    {
                        var regex = kv.Key;
                        var loader = kv.Value;
                        if (regex.IsMatch(name))
                        {
                            loader(name, entry);
                            entry.LoadComplete = true;
                            break;
                        }
                    }
                    if(!entry.LoadComplete)
                        throw new ArgumentException("No resource loader: " + name);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ResourceThread] (" + e.GetType() + ") " + e.Message);
                }
            }
        }
    }


}
