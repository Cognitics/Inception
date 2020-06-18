
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Cognitics.Unity
{
    public class SceneManager
    {
        public SceneManager(int num_threads = 1)
        {
            TaskScheduler = new LimitedConcurrencyTaskScheduler(num_threads + 1);
            TaskScheduler.Factory.StartNew(Run, RunTokenSource.Token);
        }

        public Task<Scene.Scene> FetchScene(string name, CancellationToken token)
        {
            if (TaskInfoByName.TryGetValue(name, out TaskInfo info))
            {
                if (!info.Task.IsCanceled)
                {
                    info.LastAccess = DateTime.UtcNow;
                    return info.Task;
                }
            }
            var taskinfo = new TaskInfo();
            // TODO: dispatch loader by file type
            taskinfo.Task = TaskScheduler.Factory.StartNew(() => SceneForOpenFlightFile(name), token);
            TaskInfoByName.TryRemove(name, out _);
            TaskInfoByName.TryAdd(name, taskinfo);
            return taskinfo.Task;
        }

        #region private

        LimitedConcurrencyTaskScheduler TaskScheduler;
        CancellationTokenSource RunTokenSource = new CancellationTokenSource();

        ~SceneManager()
        {
            RunTokenSource.Cancel();
        }

        class TaskInfo
        {
            internal Task<Scene.Scene> Task;
            internal DateTime LastAccess = DateTime.UtcNow;
        }
        ConcurrentDictionary<string, TaskInfo> TaskInfoByName = new ConcurrentDictionary<string, TaskInfo>();

        async void Run()
        {
            int interval_ms = 15000;
            int expiration_ms = 15000;
            while (true)
            {
                var now = DateTime.UtcNow;
                List<string> expired = new List<string>();
                foreach (var task_info_by_name in TaskInfoByName)
                {
                    if (!task_info_by_name.Value.Task.IsCompleted)
                        continue;
                    if ((now - task_info_by_name.Value.LastAccess).TotalMilliseconds < expiration_ms)
                        continue;
                    expired.Add(task_info_by_name.Key);
                }
                foreach (var name in expired)
                    TaskInfoByName.TryRemove(name, out _);
                await Task.Delay(interval_ms);
            }
        }

        Scene.Scene SceneForOpenFlightFile(string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);
            // TODO: zip handling
            var parser = new OpenFlight.Parser(filename);
            var scene = OpenFlightReader.Parse(parser);
            return scene;
        }

        #endregion
    }


    public class ModelManager : UnityEngine.MonoBehaviour
    {
        public WorldPlane WorldPlane;

        public SceneManager SceneManager;

        public void AddModel(string group_name, UnityEngine.Vector3 position, string filename)
        {
            var group = GetOrAddGroup(group_name);
            var entry = new Entry();
            entry.Position = position;
            entry.Filename = filename;
            group.Entries.Add(entry);
            entry.SceneTask = SceneManager.FetchScene(entry.Filename, group.TokenSource.Token);
            // TODO: material load
        }

        public void RemoveGroup(string name)
        {
            if (!Groups.TryGetValue(name, out Group group))
                return;
            group.TokenSource.Cancel();
            Groups.Remove(name);
        }

        #region private

        class Entry
        {
            public UnityEngine.GameObject GameObject;
            public UnityEngine.Vector3 Position;
            public string Filename;
            public Task<Scene.Scene> SceneTask;
        }

        class Group
        {
            public UnityEngine.GameObject GameObject;
            public List<Entry> Entries = new List<Entry>();
            public CancellationTokenSource TokenSource = new CancellationTokenSource();
            ~Group()
            {
                foreach (var entry in Entries)
                    Destroy(entry.GameObject);
                Destroy(GameObject);
            }
        }

        Dictionary<string, Group> Groups = new Dictionary<string, Group>();
        Group GetOrAddGroup(string name)
        {
            if (Groups.ContainsKey(name))
                return Groups[name];
            var group = new Group();
            group.GameObject = new UnityEngine.GameObject();
            group.GameObject.transform.SetParent(transform);
            Groups.Add(name, group);
            return group;
        }







        ConcurrentDictionary<string, MaterialEntry> MaterialTasks = new ConcurrentDictionary<string, MaterialEntry>();
        //ConcurrentDictionary<string, MeshEntry> MeshTasks = new ConcurrentDictionary<string, MeshEntry>();

        List<Task<string>> Tasks = new List<Task<string>>();

        CancellationTokenSource TokenSource = new CancellationTokenSource();


        async void Run()
        {
            //while (true)
            {
                //var task = await WhenAny(Tasks);
                /*

                maybe have our own variation of Task.WhenAny that tests to see if a model is ready to become a game object (scene and material are ready)

                var task = await Task.WhenAny(Tasks);
                var go = Scene.ModelGenerator.GameObjectForScene(task.Result);
                var parent_go = ParentGameObjectByTask[task];
                go.transform.SetParent(parent_go.transform);
                Tasks.Remove(task);
                */
            }
        }





        #endregion


    }


}
