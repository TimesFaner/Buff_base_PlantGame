using System.Collections.Generic;

namespace QFramework
{
    public class ConsoleKit
    {
        private static readonly List<ConsoleModule> mModules = new()
        {
            new LogModule()
        };

        public static IReadOnlyList<ConsoleModule> Modules => mModules;

        public static void InitModules()
        {
            Modules.ForEach(m => m.OnInit());
        }

        public static void AddModule(ConsoleModule module)
        {
            mModules.Add(module);
        }

        public static void DestroyModules()
        {
            Modules.ForEach(m => m.OnDestroy());
        }
    }
}